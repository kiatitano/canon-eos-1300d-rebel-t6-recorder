using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace RecorderStudio {
 internal sealed class UserActionException : Exception { public UserActionException(string message):base(message) {} }
 public sealed class CameraEngine : IDisposable {
  public event Action<CameraStatus> Status;
  public event Action<byte[]> Frame;
  public event Action<string> Take;
  public event Action<int> TransferOffer;
  public event Action<bool,string> TransferFinished;
  public event Action<List<CameraSetting>> SettingsChanged;
  readonly List<RecordedTake> pending=new List<RecordedTake>();
  readonly ConcurrentQueue<Action> commands=new ConcurrentQueue<Action>();
  readonly Thread worker;
  readonly string logPath;
  readonly Stopwatch clip=new Stopwatch(), total=new Stopwatch();
  IntPtr camera, list;
  bool initialized, opened, ownRecording, preview, outputCaptured;
  volatile bool quitting;
  volatile bool transferCancelled;
  bool transferring,focusing;
  int focusConfirmation=-1;
  string lastFocusInfo="";
  double transferProgress;
  int batteryLevel=-1,healthFailures;
  long nextKeepAlive,nextReconnect;
  bool recoverRecording;
  uint lastKeepResult=uint.MaxValue;
  uint oldOutput;
  double unit, freeBytes;
  string battery="—", card="—", error="", message="Connecting…", lastFile="No saved takes yet";
  int restarts, frames, previewFailures;
  DateTime clipStarted;
  long nextPreview, nextStatus, nextHealth, lastFrameAt;
  readonly Stopwatch clock=Stopwatch.StartNew();
  public CameraEngine() {
   string logDir=Path.Combine(AppPreferences.DataDirectory,"Logs");
   Directory.CreateDirectory(logDir); logPath=Path.Combine(logDir,DateTime.Now.ToString("yyyyMMdd-HHmmss")+".log");
   nextReconnect=5000;
   worker=new Thread(Run); worker.IsBackground=true; worker.SetApartmentState(ApartmentState.STA); worker.Start();
  }
  public static bool IsSupportedModel(string model) { if(model==null) return false; string name=model.ToUpperInvariant().Trim(); return name=="CANON EOS 1300D" || name=="CANON EOS REBEL T6" || name=="CANON EOS KISS X80"; }
  public string LogPath { get { return logPath; } }
  void Log(string text) { try { File.AppendAllText(logPath,DateTime.Now.ToString("s")+" "+text+Environment.NewLine,Encoding.UTF8); } catch {} }
  public void Connect() { commands.Enqueue(delegate { ConnectCore(); }); }
  public void RefreshSettings() { commands.Enqueue(delegate { if(opened && SettingsChanged!=null) SettingsChanged(CameraSettings.Read(camera)); }); }


  public void ApplySetting(uint id,uint value) { commands.Enqueue(delegate {
   if(!opened || ownRecording || transferring) throw new UserActionException("Change camera settings before recording.");
   bool allowed=false;
   foreach(var setting in CameraSettings.Read(camera)) if(setting.Id==id) foreach(var choice in setting.Choices) if(choice.Value==value) allowed=true;
   if(!allowed) throw new UserActionException("The camera does not allow this value in its current mode.");
   try {
    CameraSettings.WriteValue(camera,id,value); CameraSdk.Pump(350);
    if(CameraSettings.ReadValue(camera,id)!=value) throw new Exception("The camera did not confirm the new value.");
    error=""; Log("SETTING VERIFIED 0x"+id.ToString("X")+"="+value); message="Camera setting updated";
   } catch(Exception ex) { error="The camera rejected this change. Check its exposure or focus mode. "+ex.Message; Log("SETTING REJECTED "+ex.Message); }
   if(SettingsChanged!=null) SettingsChanged(CameraSettings.Read(camera)); Publish();
  }); }
  public void Focus(int direction) { commands.Enqueue(delegate {
   if(!opened || ownRecording || transferring) throw new UserActionException("Stop recording before adjusting focus.");
   uint result=CameraSdk.EdsSendCommand(camera,0x103,direction<0?1:0x8001);
   if(result!=0) throw new UserActionException("Lens focus control is unavailable. Set the lens to AF and enable live view.");
  }); }
  public void AutoFocusFace() { commands.Enqueue(delegate {
   if(!opened || ownRecording || transferring) throw new UserActionException("Use autofocus before recording.");
   if(CameraSdk.ReadNumber(camera,0x404)==3) throw new UserActionException("The lens switch is on MF. Switch the lens to AF first.");
   bool supported=false; foreach(var setting in CameraSettings.Read(camera)) if(setting.Id==0x50e) foreach(var choice in setting.Choices) if(choice.Value==2) supported=true;
   if(!supported) throw new UserActionException("Face-detection AF is not available in the current camera mode.");
   bool started=false; focusing=true; message="Focusing on a face…"; Publish();
   try {
    if(CameraSdk.ReadNumber(camera,0x50e)!=2) { CameraSdk.WriteNumber(camera,0x50e,2); CameraSdk.Pump(500); }
    focusConfirmation=-1;
    PreviewFrame();
    CameraSdk.Check(CameraSdk.EdsSendCommand(camera,0x102,1),"Start face AF"); started=true;
    var afWatch=Stopwatch.StartNew(); int stable=0;
    while(afWatch.Elapsed.TotalSeconds<AppPreferences.Current.FocusTimeoutSeconds && !quitting) {
     CameraSdk.Pump(90); PreviewFrame();
     if(CameraSdk.Fatal) throw new IOException(CameraSdk.LastError);
     if(afWatch.ElapsedMilliseconds>=800 && focusConfirmation==1) stable++; else stable=0;
     if(stable>=3) break;
    }
    message=stable>=3?"The camera reported focus confirmation. Check face sharpness":"Focus confirmation was not reported. Check sharpness, light and distance";
    Log("FACE AF result confirmed="+(stable>=3)+" seconds="+afWatch.Elapsed.TotalSeconds.ToString("0.00")+" feedback="+focusConfirmation);
   } catch(Exception ex) { throw new UserActionException("Autofocus was rejected: "+ex.Message); }
   finally { if(started) CameraSdk.EdsSendCommand(camera,0x102,0); focusing=false; Publish(); }
   if(SettingsChanged!=null) SettingsChanged(CameraSettings.Read(camera));
  }); }
  public void SetPreview(bool on) { commands.Enqueue(delegate { preview=on; previewFailures=0; frames=0; lastFrameAt=clock.ElapsedMilliseconds; if(!on && Frame!=null) Frame(null); Publish(); }); }
  public void Start() { commands.Enqueue(delegate {
   if(!opened) throw new UserActionException("Camera disconnected. Choose Reconnect.");
   if(ownRecording) return;
   if(CameraSdk.ReadNumber(camera,0x400)!=20) throw new UserActionException("Set the camera's physical mode dial to Movie.");
   if(CameraSdk.ReadNumber(camera,0x510)==4) throw new UserActionException("The camera is already recording. Stop that recording first.");
   if(CameraSdk.ReadNumber(camera,0xb)!=1) throw new UserActionException("The camera save destination must be its memory card.");
   Health(); if(unit==0) throw new UserActionException("Card capacity could not be verified. Reconnect the camera.");
   if(freeBytes<5L*1024*1024*1024) throw new UserActionException("At least 5 GiB of free card space is required to start.");
   error=""; restarts=0; total.Restart(); BeginClip(); message="Recording to the camera card"; Publish();
  }); }
  public void Stop() { commands.Enqueue(delegate { bool wasRecording=ownRecording; StopCore(); Publish(); if(wasRecording) OfferTransfer(); }); }
  void OfferTransfer() { if(TransferOffer!=null) TransferOffer(pending.Count); }
  public void CancelTransfer() { transferCancelled=true; }
  public void MovePending() { transferCancelled=false; commands.Enqueue(delegate {
   if(!opened || ownRecording || transferring) throw new UserActionException("Stop recording and keep the camera connected before transferring.");
   if(pending.Count==0) return;
   if(CameraSdk.ReadNumber(camera,0x510)==4) throw new UserActionException("The camera is recording. Stop it first.");
   transferring=true; transferProgress=0; error="";
   CameraSdk.SetThreadExecutionState(0x80000001); message="Preparing file transfer…"; Publish();
   bool success=false; string result;
   try {
    var transfer=new MovieTransfer(camera,delegate { return transferCancelled || quitting; },delegate(double value,string detail) { transferProgress=value; message=detail; Publish(); },Log);
    string folder=transfer.Move(new List<RecordedTake>(pending),delegate(RecordedTake take) { pending.Remove(take); });
    success=true; result="Transfer and verification completed. Verified originals were removed from the card.\n"+folder;
    message="Transfer complete. Originals are saved on your computer"; Health();
   } catch(Exception ex) {
    result="Transfer incomplete. Unverified originals remain on the card.\n"+ex.Message;
    error=result; message=ex is OperationCanceledException?"Transfer cancelled":"Transfer needs attention"; Log("TRANSFER ERROR "+ex);
   } finally { transferring=false; CameraSdk.SetThreadExecutionState(0x80000000); Publish(); }
   if(TransferFinished!=null) TransferFinished(success,result);
  }); }
  void BeginClip() {
   if(CameraSdk.ReadNumber(camera,0x500)!=3) { CameraSdk.WriteNumber(camera,0x500,3); CameraSdk.Pump(700); }
   CameraSdk.WriteNumber(camera,0x510,4); ownRecording=true;
   CameraSdk.SetThreadExecutionState(0x80000001);
   CameraSdk.Pump(500);
   if(CameraSdk.Fatal) throw new Exception(CameraSdk.LastError);
   if(CameraSdk.ReadNumber(camera,0x510)!=4) throw new Exception("Recording did not start. Check the card and camera state.");
   clip.Restart(); clipStarted=DateTime.UtcNow;
   Log("RECORD START, restart count "+restarts);
  }
  void StopCore() {
   if(ownRecording && opened) {
    if(CameraSdk.ReadNumber(camera,0x510)==4) CameraSdk.WriteNumber(camera,0x510,0);
    CameraSdk.Pump(1500); SaveTake();
   }
   ownRecording=false; clip.Stop(); total.Stop(); CameraSdk.SetThreadExecutionState(0x80000000);
   message="Clip saved. Ready to record"; Log("STOP");
  }
  double WindowsCapacity() {
   try {
    Type shellType=Type.GetTypeFromProgID("Shell.Application"); dynamic shell=Activator.CreateInstance(shellType);
    foreach(dynamic item in shell.NameSpace(17).Items()) {
     if(!IsSupportedModel((string)item.Name)) continue;
     foreach(dynamic storage in item.GetFolder.Items()) {
      object cap=storage.ExtendedProperty("System.Capacity"); if(cap!=null && Convert.ToDouble(cap)>0) return Convert.ToDouble(cap);
     }
    }
   } catch(Exception ex) { Log("Windows capacity: "+ex.Message); }
   return 0;
  }
  void ConnectCore() {
   if(ownRecording) return;
   CloseCore(); error=""; message="Connecting…"; Publish();
   double windowsCapacity=WindowsCapacity();
   CameraSdk.Fatal=false; CameraSdk.ShutdownWarning=false; CameraSdk.LastError=null; CameraSdk.LastMovie=null; CameraSdk.LastMovieSize=0;
   string sdk=SdkLocator.Find();
   if(sdk==null) throw new Exception("Canon EOS Utility or a compatible 32-bit EDSDK was not found. Choose its folder in Settings.");
   CameraSdk.SetDllDirectory(sdk);
   CameraSdk.Check(CameraSdk.EdsInitializeSDK(),"Initialize"); initialized=true;
   CameraSdk.Check(CameraSdk.EdsGetCameraList(out list),"CameraList"); int count;
   CameraSdk.Check(CameraSdk.EdsGetChildCount(list,out count),"CameraCount");
   if(count!=1) throw new Exception("Turn on the camera, connect USB, then choose Reconnect.");
   CameraSdk.Check(CameraSdk.EdsGetChildAtIndex(list,0,out camera),"Camera");
   CameraSdk.Check(CameraSdk.EdsOpenSession(camera),"Open session: close Canon EOS Utility and other camera apps"); opened=true;
   CameraSdk.Check(CameraSdk.EdsSetCameraStateEventHandler(camera,0x300,CameraSdk.StateCallback,camera),"StateHandler");
   CameraSdk.Check(CameraSdk.EdsSetObjectEventHandler(camera,0x200,CameraSdk.ObjectCallback,IntPtr.Zero),"ObjectHandler");
   CameraSdk.Pump(1000);
   string model=Encoding.ASCII.GetString(CameraSdk.Read(camera,2)).Split('\0')[0];
   if(!IsSupportedModel(model)) throw new Exception("This version supports the Canon EOS 1300D / Rebel T6 only.");
   oldOutput=CameraSdk.ReadNumber(camera,0x500);
   outputCaptured=true;
   bool stillRecording=CameraSdk.ReadNumber(camera,0x510)==4;
   if(stillRecording && !recoverRecording) throw new Exception("Stop the camera's existing recording, then reconnect.");
   IntPtr volume=IntPtr.Zero;
   try {
    CameraSdk.Check(CameraSdk.EdsGetChildAtIndex(camera,0,out volume),"Card"); CameraSdk.VolumeInfo vi;
    CameraSdk.Check(CameraSdk.EdsGetVolumeInfo(volume,out vi),"CardInfo");
    double candidate=vi.Capacity>0?windowsCapacity/vi.Capacity:0;
    unit=(candidate==1 || candidate==512 || candidate==1024)?candidate:0;
   } finally { if(volume!=IntPtr.Zero) CameraSdk.EdsRelease(volume); }
   CameraSdk.WriteNumber(camera,0x500,3); CameraSdk.Pump(700);
   Health(); healthFailures=0; message="Camera connected. Ready to record"; nextHealth=clock.ElapsedMilliseconds+5000;
   if(recoverRecording && stillRecording) { ownRecording=true; clip.Start(); total.Start(); CameraSdk.SetThreadExecutionState(0x80000001); message="Connection restored; the existing recording is still running"; }
   recoverRecording=false; nextKeepAlive=clock.ElapsedMilliseconds;
   Log("CONNECTED, storage unit "+unit); Publish();
  }
  void Health() {
   uint level=CameraSdk.ReadNumber(camera,8); batteryLevel=level<=100?(int)level:-1; battery=level<=100?"≈ "+level+"%":(level==0xFFFFFFFF?"AC power":"Unknown");
   IntPtr volume=IntPtr.Zero;
   try {
    CameraSdk.Check(CameraSdk.EdsGetChildAtIndex(camera,0,out volume),"Card"); CameraSdk.VolumeInfo vi;
    CameraSdk.Check(CameraSdk.EdsGetVolumeInfo(volume,out vi),"CardInfo"); freeBytes=vi.FreeSpace*unit;
    card=unit>0?(freeBytes/(1024*1024*1024)).ToString("0.0")+" GB":"Unknown";
   } finally { if(volume!=IntPtr.Zero) CameraSdk.EdsRelease(volume); }
  }
  void SaveTake() {
   if(CameraSdk.LastMovie==null || CameraSdk.LastMovieSeen<clipStarted) return;
   lastFile=CameraSdk.LastMovie;
   bool known=false; foreach(var take in pending) if(take.Name==lastFile && take.Size==CameraSdk.LastMovieSize && take.CameraTime==CameraSdk.LastMovieCameraTime) known=true;
   if(!known) pending.Add(new RecordedTake {Name=lastFile,Size=CameraSdk.LastMovieSize,CameraTime=CameraSdk.LastMovieCameraTime});
   string line=lastFile+"   ·   "+(CameraSdk.LastMovieSize/1000000d).ToString("0.0")+" MB";
   Log("SAVED "+line); if(Take!=null) Take(line);
  }
  void CheckRecording() {
   if(!ownRecording || CameraSdk.ReadNumber(camera,0x510)==4) return;
   DateTime stopped=DateTime.UtcNow; double duration=clip.Elapsed.TotalSeconds;
   message="Saving and preparing the next clip…"; Publish(true); CameraSdk.Pump(1500);
   if(CameraSdk.Fatal) throw new Exception(CameraSdk.LastError);
   bool sizeLimit=CameraSdk.LastMovieSeen>clipStarted && CameraSdk.LastMovieSize>=4000000000;
   SaveTake();
   if(!sizeLimit && duration<1790) {
    ownRecording=false; clip.Stop(); total.Stop(); CameraSdk.SetThreadExecutionState(0x80000000);
    message="Recording stopped on the camera"; Log("EARLY STOP; no restart"); Publish(); OfferTransfer(); return;
   }
   Health(); if(unit==0 || freeBytes<256*1024*1024) throw new Exception("The card does not have enough space for another clip.");
   if(++restarts>100) throw new Exception("The 100-restart session limit has been reached.");
   BeginClip(); message="Recording continues in a new clip";
   Log("AUTO RESTART verified; gap "+(DateTime.UtcNow-stopped).TotalSeconds.ToString("0.00")+"s"); Publish();
  }
  void PreviewFrame() {
   IntPtr stream=IntPtr.Zero,image=IntPtr.Zero;
   try {
    CameraSdk.Check(PreviewSdk.EdsCreateMemoryStream(0,out stream),"PreviewStream");
    CameraSdk.Check(PreviewSdk.EdsCreateEvfImageRef(stream,out image),"PreviewImage");
    uint result=PreviewSdk.EdsDownloadEvfImage(camera,image);
    if(result==0xA102 || result==0x81 || result==0x61) { previewFailures++; return; }
    CameraSdk.Check(result,"PreviewDownload"); ulong length; IntPtr pointer;
    ReadFocusFeedback(image);
    CameraSdk.Check(PreviewSdk.EdsGetLength(stream,out length),"PreviewLength");
    if(length==0 || length>16*1024*1024) return;
    CameraSdk.Check(PreviewSdk.EdsGetPointer(stream,out pointer),"PreviewPointer");
    byte[] data=new byte[(int)length]; Marshal.Copy(pointer,data,0,data.Length); frames++; previewFailures=0; lastFrameAt=clock.ElapsedMilliseconds;
    if(Frame!=null) Frame(data);
   } catch(Exception ex) { previewFailures++; if(previewFailures==1) Log("Preview: "+ex.Message); }
   finally { if(image!=IntPtr.Zero) CameraSdk.EdsRelease(image); if(stream!=IntPtr.Zero) CameraSdk.EdsRelease(stream); }
  }
  void ReadFocusFeedback(IntPtr image) {
   byte[] info=null;
   try { info=CameraSdk.Read(image,0x104); } catch { try { info=CameraSdk.Read(camera,0x104); } catch { focusConfirmation=-1; return; } }
   if(info.Length<20) { focusConfirmation=-1; return; }
   int count=(int)BitConverter.ToUInt32(info,16),valid=0,selected=0,locked=0;
   if(count<0 || count>128 || info.Length<20+count*32) { focusConfirmation=-1; return; }
   for(int i=0;i<count;i++) {
    int offset=20+i*32;
    if(BitConverter.ToUInt32(info,offset)==0) continue;
    valid++; if(BitConverter.ToUInt32(info,offset+4)!=0) selected++;
    if(BitConverter.ToUInt32(info,offset+8)==1) locked++;
   }
   focusConfirmation=valid>0?(locked>0?1:0):-1;
   string description="points="+count+" valid="+valid+" selected="+selected+" locked="+locked;
   if(description!=lastFocusInfo) { Log("FOCUS FEEDBACK "+description); lastFocusInfo=description; }
  }
  void Publish(bool restarting=false) {
   if(Status==null) return;
   string msg=message;
   if(preview && previewFailures>50 && !ownRecording) msg="Live view is not ready. Check the camera's movie mode";
   Status(new CameraStatus {Connected=opened, Recording=ownRecording, Preview=preview, CanStart=opened && !ownRecording && !transferring && !focusing && unit>0,
    Message=msg,Battery=battery,Card=card,LastFile=lastFile,Error=error,Restarts=restarts,PreviewFrames=frames,
    ClipSeconds=clip.Elapsed.TotalSeconds,TotalSeconds=total.Elapsed.TotalSeconds,Restarting=restarting,PreviewStale=preview && clock.ElapsedMilliseconds-lastFrameAt>3500,
    Transferring=transferring,TransferProgress=transferProgress,PendingTransfers=pending.Count,BatteryLevel=batteryLevel,Focusing=focusing});
  }
  void Fail(Exception ex) {
   error=ex.Message; Log("ERROR "+ex.ToString());
   recoverRecording=recoverRecording || ownRecording;
   ownRecording=false; total.Stop(); clip.Stop(); CameraSdk.SetThreadExecutionState(0x80000000);
   message="Connection lost. Waiting to reconnect to the camera…"; CloseCore(false); nextReconnect=clock.ElapsedMilliseconds+5000; Publish();
  }
  void Run() {
   try {
    while(!quitting) {
     Action command; while(commands.TryDequeue(out command)) { try { command(); } catch(UserActionException ex) { error=ex.Message; Log("VALIDATION: "+ex.Message); Publish(); } catch(Exception ex) { Fail(ex); } }
     if(opened) {
      try {
       CameraSdk.Check(CameraSdk.EdsGetEvent(),"Camera events"); if(CameraSdk.Fatal) throw new Exception(CameraSdk.LastError);
       CheckRecording(); long now=clock.ElapsedMilliseconds;
       if(now>=nextKeepAlive || CameraSdk.ShutdownWarning) {
        uint keep=CameraSdk.EdsSendCommand(camera,1,0);
        if(keep!=lastKeepResult) { Log("KEEPALIVE RESULT 0x"+keep.ToString("X")); lastKeepResult=keep; }
        if(CameraSdk.ShutdownWarning) Log("AUTO POWER-OFF WARNING; extend result 0x"+keep.ToString("X"));
        CameraSdk.ShutdownWarning=false; nextKeepAlive=now+15000;
       }
       if(preview && now>=nextPreview) { PreviewFrame(); nextPreview=now+100; }
       if(now>=nextHealth) {
        try { Health(); healthFailures=0; }
        catch(Exception healthError) { batteryLevel=-1; battery="Unknown"; Log("Telemetry unavailable: "+healthError.Message); if(++healthFailures>=3) throw; }
        nextHealth=now+5000;
       }
      } catch(Exception ex) { Fail(ex); }
     } else if(AppPreferences.Current.AutoReconnect && clock.ElapsedMilliseconds>=nextReconnect) {
      try { ConnectCore(); } catch(Exception ex) { Log("Reconnect: "+ex.Message); CloseCore(false); message="Waiting for the camera to turn on and connect…"; error=ex.Message; }
      nextReconnect=clock.ElapsedMilliseconds+5000;
     }
     if(clock.ElapsedMilliseconds>=nextStatus) { Publish(); nextStatus=clock.ElapsedMilliseconds+200; }
     Thread.Sleep(20);
    }
   } finally { try { StopCore(); } catch {} CloseCore(); }
  }
  void CloseCore(bool restoreOutput=true) {
   if(opened) { if(outputCaptured && restoreOutput) { try { CameraSdk.WriteNumber(camera,0x500,oldOutput); } catch {} } try { CameraSdk.EdsCloseSession(camera); } catch {} }
   outputCaptured=false;
   opened=false;
   if(camera!=IntPtr.Zero) { CameraSdk.EdsRelease(camera); camera=IntPtr.Zero; }
   if(list!=IntPtr.Zero) { CameraSdk.EdsRelease(list); list=IntPtr.Zero; }
   if(initialized) { CameraSdk.EdsTerminateSDK(); initialized=false; }
   unit=0;
   batteryLevel=-1; battery="Unknown";
  }
  public void Dispose() { quitting=true; if(!worker.Join(7000)) Log("Worker did not finish within shutdown timeout"); }
 }
}
