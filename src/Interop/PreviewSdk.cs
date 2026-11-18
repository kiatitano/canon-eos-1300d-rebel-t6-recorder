using System;
using System.Runtime.InteropServices;

namespace RecorderStudio {
 internal static class PreviewSdk {
  [DllImport("EDSDK.dll")] internal static extern uint EdsCreateMemoryStream(ulong size,out IntPtr stream);
  [DllImport("EDSDK.dll")] internal static extern uint EdsCreateEvfImageRef(IntPtr stream,out IntPtr image);
  [DllImport("EDSDK.dll")] internal static extern uint EdsDownloadEvfImage(IntPtr camera,IntPtr image);
  [DllImport("EDSDK.dll")] internal static extern uint EdsGetPointer(IntPtr stream,out IntPtr pointer);
  [DllImport("EDSDK.dll")] internal static extern uint EdsGetLength(IntPtr stream,out ulong length);
 }
}
