namespace RecorderStudio {
 public enum RecordingStopReason { Manual, CameraStopped, ClipLimit, ConnectionLost, Temperature }
 public sealed class RecordingStopTracker {
  bool active;
  public void Arm() {active=true;}
  public bool Stop() {if(!active)return false;active=false;return true;}
  public static bool IsUnexpected(RecordingStopReason reason) {return reason==RecordingStopReason.CameraStopped || reason==RecordingStopReason.ConnectionLost || reason==RecordingStopReason.Temperature;}
 }
}
