
namespace RecorderStudio {
 public sealed class CameraStatus {
  public bool Connected, Recording, Preview, CanStart, Restarting, PreviewStale, Transferring, Focusing;
  public string Message="Connect a camera", Model="Canon EOS 1300D", Battery="—", Card="—", LastFile="No saved takes yet", Error="";
  public int Restarts, PreviewFrames, PendingTransfers, BatteryLevel=-1;
  public double ClipSeconds, TotalSeconds, TransferProgress;
 }
}
