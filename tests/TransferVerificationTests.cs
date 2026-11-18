using System;
using System.IO;
using RecorderStudio;
public static class TransferVerificationTests {
 public static void Main() {
  string folder=Path.Combine(Path.GetTempPath(),"1300D-transfer-test-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
  string first=Path.Combine(folder,"source.bin"),second=Path.Combine(folder,"copy.bin");
  byte[] data=new byte[4096]; new Random(42).NextBytes(data); File.WriteAllBytes(first,data); File.WriteAllBytes(second,data);
  string trusted=MovieTransfer.VerifyCopies(first,second,4096); if(trusted!=MovieTransfer.Hash(second)) throw new Exception("Verified hash not anchored to camera copy"); Console.WriteLine("PASS identical full-size copies accepted and trusted hash returned");
  data[2048]^=1; File.WriteAllBytes(second,data); bool rejected=false;
  try { MovieTransfer.VerifyCopies(first,second,4096); } catch(IOException) { rejected=true; }
  if(!rejected || !File.Exists(first)) throw new Exception("Hash mismatch was not rejected safely"); Console.WriteLine("PASS same-length bit corruption rejected; source retained");
  File.WriteAllBytes(second,new byte[7]); rejected=false;
  try { MovieTransfer.VerifyCopies(first,second,4096); } catch(IOException) { rejected=true; }
  if(!rejected || !File.Exists(first)) throw new Exception("Truncation was not rejected safely"); Console.WriteLine("PASS truncated transfer rejected; source retained");
  byte[] changed=File.ReadAllBytes(first); changed[50]^=1; File.WriteAllBytes(first,changed);
  if(MovieTransfer.Hash(first)==trusted) throw new Exception("Post-verification modification escaped the trusted hash"); Console.WriteLine("PASS post-verification modification differs from the anchored hash");
  bool moved=false,cancelled=false;
  AppPreferences.Current.OutputDirectory=Path.Combine(folder,"transfers");
  var transfer=new MovieTransfer(IntPtr.Zero,delegate { return true; },delegate(double value,string text) {},delegate(string text) {});
  try { transfer.Move(new System.Collections.Generic.List<RecordedTake> {new RecordedTake {Name="TEST.MOV",Size=4096}},delegate(RecordedTake take) { moved=true; }); } catch(OperationCanceledException) { cancelled=true; }
  if(!cancelled || moved) throw new Exception("Cancellation reached source deletion"); Console.WriteLine("PASS cancelled move exits before camera access or source deletion");
  File.Delete(first); File.Delete(second);
  foreach(string child in Directory.GetDirectories(AppPreferences.Current.OutputDirectory)) Directory.Delete(child);
  Directory.Delete(AppPreferences.Current.OutputDirectory); Directory.Delete(folder);
 }
}
