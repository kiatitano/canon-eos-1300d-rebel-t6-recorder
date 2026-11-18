using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RecorderStudio {
 internal sealed class MovieTransfer {
  public static string DestinationRoot { get { return AppPreferences.Current.OutputDirectory; } }
  readonly IntPtr camera;
  readonly Func<bool> cancelled;
  readonly Action<double,string> progress;
  readonly Action<string> log;
  public MovieTransfer(IntPtr camera,Func<bool> cancelled,Action<double,string> progress,Action<string> log) {
   this.camera=camera; this.cancelled=cancelled; this.progress=progress; this.log=log;
  }
  void CheckCancel() { if(cancelled()) throw new OperationCanceledException("Transfer cancelled. Unverified originals were kept on the card."); }
  public static string Hash(string path) { using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-",""); }
  public static string VerifyCopies(string first,string second,ulong expected) {
   if((ulong)new FileInfo(first).Length!=expected || (ulong)new FileInfo(second).Length!=expected) throw new IOException("File sizes do not match. The card original was kept.");
   string hash=Hash(first);
   if(!string.Equals(hash,Hash(second),StringComparison.Ordinal)) throw new IOException("SHA-256 verification failed. The card original was kept.");
   return hash;
  }
  bool Matches(CameraSdk.ItemInfo info,RecordedTake take) { return info.IsFolder==0 && info.Name==take.Name && info.Size==take.Size && info.DateTime==take.CameraTime; }
  void Walk(IntPtr parent,RecordedTake take,List<IntPtr> matches,int depth) {
   CheckCancel(); if(depth>8) throw new IOException("Unexpected card structure. No files were deleted.");
   int count; CameraSdk.Check(CameraSdk.EdsGetChildCount(parent,out count),"List card files");
   for(int i=0;i<count;i++) {
    CheckCancel(); IntPtr child=IntPtr.Zero;
    try {
     CameraSdk.Check(CameraSdk.EdsGetChildAtIndex(parent,i,out child),"Card item"); CameraSdk.ItemInfo info;
     CameraSdk.Check(CameraSdk.EdsGetDirectoryItemInfo(child,out info),"Card item info");
     if(info.IsFolder!=0) Walk(child,take,matches,depth+1);
     else if(Matches(info,take)) { TransferSdk.EdsRetain(child); matches.Add(child); }
    } finally { if(child!=IntPtr.Zero) CameraSdk.EdsRelease(child); }
   }
  }
  IntPtr FindUnique(RecordedTake take) {
   var matches=new List<IntPtr>();
   try {
    int count; CameraSdk.Check(CameraSdk.EdsGetChildCount(camera,out count),"Card list");
    for(int i=0;i<count;i++) {
     IntPtr volume=IntPtr.Zero;
     try { CameraSdk.Check(CameraSdk.EdsGetChildAtIndex(camera,i,out volume),"Card"); Walk(volume,take,matches,0); }
     finally { if(volume!=IntPtr.Zero) CameraSdk.EdsRelease(volume); }
    }
    if(matches.Count!=1) throw new IOException("A unique match for "+take.Name+" was not found on the card. No transfer or deletion was performed.");
    IntPtr result=matches[0]; matches.Clear(); return result;
   } finally { foreach(IntPtr item in matches) CameraSdk.EdsRelease(item); }
  }
  void Download(IntPtr item,string path,ulong expected,Action<double> report) {
   IntPtr stream=IntPtr.Zero; bool complete=false,started=false;
   try {
    CameraSdk.Check(TransferSdk.EdsCreateFileStream(path,0,2,out stream),"Create destination");
    ulong done=0;
    while(done<expected) {
     CheckCancel(); ulong chunk=Math.Min((ulong)(2*1024*1024),expected-done); started=true;
     CameraSdk.Check(TransferSdk.EdsDownload(item,chunk,stream),"Download original movie"); done+=chunk;
     report(done/(double)expected); CameraSdk.Check(CameraSdk.EdsGetEvent(),"Transfer events");
     if(CameraSdk.Fatal) throw new IOException(CameraSdk.LastError);
    }
    CameraSdk.Check(TransferSdk.EdsDownloadComplete(item),"Complete download"); complete=true;
   } finally {
    if(started && !complete) TransferSdk.EdsDownloadCancel(item);
    if(stream!=IntPtr.Zero) CameraSdk.EdsRelease(stream);
   }
   using(var disk=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.Read)) disk.Flush(true);
   if((ulong)new FileInfo(path).Length!=expected) throw new IOException("The downloaded file size is incorrect. The card original was kept.");
  }
  public string Move(List<RecordedTake> takes,Action<RecordedTake> moved) {
   Directory.CreateDirectory(DestinationRoot);
   string folder=Path.Combine(DestinationRoot,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+"_"+Guid.NewGuid().ToString("N").Substring(0,6));
   ulong total=0,max=0; foreach(var take in takes) { total+=take.Size; max=Math.Max(max,take.Size); }
   if((ulong)new DriveInfo(Path.GetPathRoot(DestinationRoot)).AvailableFreeSpace<total+max+256UL*1024*1024) throw new IOException("The destination drive needs more space for transfer and verification. Card originals were kept.");
   Directory.CreateDirectory(folder); log("TRANSFER DESTINATION "+folder);
   for(int index=0;index<takes.Count;index++) {
    CheckCancel(); RecordedTake take=takes[index];
    if(Path.GetFileName(take.Name)!=take.Name || !take.Name.EndsWith(".MOV",StringComparison.OrdinalIgnoreCase) || take.Size==0) throw new IOException("Invalid file metadata for transfer.");
    IntPtr item=FindUnique(take); string final=Path.Combine(folder,take.Name);
    string partial=final+".partial", verification=final+".verify";
    int current=index;
    try {
     Download(item,partial,take.Size,delegate(double part) { progress((current+part*0.45)/takes.Count,"Copying file "+(current+1)+" of "+takes.Count+": "+take.Name); });
     CheckCancel();
     Download(item,verification,take.Size,delegate(double part) { progress((current+0.45+part*0.45)/takes.Count,"Reading the original again to verify: "+take.Name); });
     string hash=VerifyCopies(partial,verification,take.Size); CheckCancel();
     File.Move(partial,final); File.Delete(verification);
     // Hold an exclusive-to-writers read handle through final verification and source deletion.
     using(var local=new FileStream(final,FileMode.Open,FileAccess.Read,FileShare.Read)) using(var sha=SHA256.Create()) {
      string finalHash=BitConverter.ToString(sha.ComputeHash(local)).Replace("-","");
      if((ulong)local.Length!=take.Size || finalHash!=hash) throw new IOException("Final-file verification failed. The card original was kept.");
      CheckCancel(); CameraSdk.ItemInfo remote;
      CameraSdk.Check(CameraSdk.EdsGetDirectoryItemInfo(item,out remote),"Recheck source");
      if(!Matches(remote,take)) throw new IOException("The card file changed. It was not deleted.");
      CameraSdk.Check(TransferSdk.EdsDeleteDirectoryItem(item),"Delete verified source");
      // The SDK releases the directory item implicitly after successful deletion.
      item=IntPtr.Zero;
     }
     moved(take); log("VERIFIED AND MOVED "+take.Name+" size="+take.Size+" SHA256="+hash);
     try { File.AppendAllText(Path.Combine(folder,"transfer-verification.txt"),take.Name+" | "+take.Size+" bytes | SHA256 "+hash+" | source removed after verification"+Environment.NewLine,Encoding.UTF8); } catch(Exception ex) { log("Verification receipt: "+ex.Message); }
     progress((index+1d)/takes.Count,"Transfer verified: "+take.Name);
    } finally { if(item!=IntPtr.Zero) CameraSdk.EdsRelease(item); }
   }
   return folder;
  }
 }
}
