using System;
using RecorderStudio;
public static class RecordingChimeTests {
 static void Assert(bool ok,string label){if(!ok)throw new Exception(label);}
 static void Check(bool unexpected) {
  byte[] wave=RecordingChime.CreateWave(unexpected,0.35);int rate=BitConverter.ToInt32(wave,24),length=BitConverter.ToInt32(wave,40);
  Assert(rate==44100 && length==wave.Length-44 && BitConverter.ToInt16(wave,34)==16,"Valid 16-bit PCM WAV");
  double duration=length/2.0/rate;Assert(duration>0.15&&duration<0.4,"Stop chime must remain brief");
  int tone=(int)(rate*0.075),gap=(int)(rate*0.035),notes=unexpected?3:2;
  Assert(length/2==notes*(tone+gap),"Distinct two/three note pattern");
  int peak=0;
  for(int note=0;note<notes;note++) {
   bool audible=false;for(int i=0;i<tone;i++){short value=BitConverter.ToInt16(wave,44+2*(note*(tone+gap)+i));peak=Math.Max(peak,Math.Abs((int)value));if(value!=0)audible=true;}
   Assert(audible,"Each note carries audio");
   for(int i=0;i<gap;i++)Assert(BitConverter.ToInt16(wave,44+2*(note*(tone+gap)+tone+i))==0,"Silence between notes");
  }
  Assert(peak>1000&&peak<7000,"Soft bounded default amplitude");
  byte[] silent=RecordingChime.CreateWave(unexpected,0);for(int i=44;i<silent.Length;i++)Assert(silent[i]==0,"Zero volume mute");
 }
 public static int Main(){
  Check(false);Check(true);var tracker=new RecordingStopTracker();Assert(!tracker.Stop(),"Idle must not emit stop notifications");
  tracker.Arm();Assert(tracker.Stop(),"Confirmed recording stop emits once");Assert(!tracker.Stop(),"Duplicate stop/failure notifications suppressed");
  tracker.Arm();Assert(tracker.Stop(),"A restarted clip arms the next stop");
  Assert(!RecordingStopTracker.IsUnexpected(RecordingStopReason.Manual)&&!RecordingStopTracker.IsUnexpected(RecordingStopReason.ClipLimit),"Manual and rollover are normal");
  foreach(var reason in new[]{RecordingStopReason.CameraStopped,RecordingStopReason.ConnectionLost,RecordingStopReason.Temperature})Assert(RecordingStopTracker.IsUnexpected(reason),"Unexpected classification");
  Console.WriteLine("PASS short two/three-note chimes, soft amplitude, mute and exactly-once per-clip stop tracking");return 0;
 }
}
