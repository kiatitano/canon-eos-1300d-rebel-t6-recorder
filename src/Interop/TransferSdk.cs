using System;
using System.Runtime.InteropServices;

namespace RecorderStudio {
 internal static class TransferSdk {
  [DllImport("EDSDK.dll", CharSet=CharSet.Ansi)] internal static extern uint EdsCreateFileStream(string path,uint disposition,uint access,out IntPtr stream);
  [DllImport("EDSDK.dll")] internal static extern uint EdsDownload(IntPtr item,ulong size,IntPtr stream);
  [DllImport("EDSDK.dll")] internal static extern uint EdsDownloadComplete(IntPtr item);
  [DllImport("EDSDK.dll")] internal static extern uint EdsDownloadCancel(IntPtr item);
  [DllImport("EDSDK.dll")] internal static extern uint EdsDeleteDirectoryItem(IntPtr item);
  [DllImport("EDSDK.dll")] internal static extern uint EdsRetain(IntPtr item);
 }
}
