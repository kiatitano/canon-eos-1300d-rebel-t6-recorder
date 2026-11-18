using System;
using System.Runtime.InteropServices;
public static class CameraSdk {
 [StructLayout(LayoutKind.Sequential)] public struct PropertyDesc {
  public int Form; public int Access; public int Count;
  [MarshalAs(UnmanagedType.ByValArray,SizeConst=128)] public int[] Values;
 }
 [DllImport("EDSDK.dll")] public static extern uint EdsGetPropertyDesc(IntPtr camera,uint property,out PropertyDesc desc);
 [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)] public struct ItemInfo {
  public ulong Size; public int IsFolder; public uint Group; public uint Option;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string Name;
  public uint Format; public uint DateTime;
 }
 public delegate uint StateHandler(uint evt,uint param,IntPtr context);
 [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)] public struct VolumeInfo {
  public uint StorageType; public uint Access; public ulong Capacity; public ulong FreeSpace;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string Label;
 }
 [DllImport("EDSDK.dll")] public static extern uint EdsGetVolumeInfo(IntPtr volume,out VolumeInfo info);
 public delegate uint ObjectHandler(uint evt,IntPtr item,IntPtr context);
 [DllImport("EDSDK.dll")] public static extern uint EdsSetCameraStateEventHandler(IntPtr camera,uint evt,StateHandler callback,IntPtr context);
 [DllImport("EDSDK.dll")] public static extern uint EdsSetObjectEventHandler(IntPtr camera,uint evt,ObjectHandler callback,IntPtr context);
 [DllImport("EDSDK.dll")] public static extern uint EdsGetDirectoryItemInfo(IntPtr item,out ItemInfo info);
 [DllImport("EDSDK.dll")] public static extern uint EdsSendCommand(IntPtr camera,uint command,int parameter);
 public static bool Fatal;
 public static string LastError;
 public static string LastMovie;
 public static ulong LastMovieSize;
 public static DateTime LastMovieSeen;
 public static uint LastMovieCameraTime;
 public static bool ShutdownWarning;
 public static volatile bool KeepAliveAllowed=true;
 public static StateHandler StateCallback = OnState;
 public static ObjectHandler ObjectCallback = OnObject;
 static uint OnState(uint evt,uint param,IntPtr context) {
  if(evt==0x303) { ShutdownWarning=true; if(KeepAliveAllowed && context!=IntPtr.Zero) EdsSendCommand(context,1,0); }
  if(evt==0x301 || evt==0x305 || evt==0x306) { Fatal=true; LastError="Camera event 0x"+evt.ToString("X")+", parameter 0x"+param.ToString("X"); }
  return 0;
 }
 static uint OnObject(uint evt,IntPtr item,IntPtr context) {
  try { if(item!=IntPtr.Zero && (evt==0x204 || evt==0x206 || evt==0x207)) {
   ItemInfo info; if(EdsGetDirectoryItemInfo(item,out info)==0 && info.IsFolder==0 && info.Name!=null && info.Name.EndsWith(".MOV",StringComparison.OrdinalIgnoreCase)) {
    LastMovie=info.Name; LastMovieSize=info.Size; LastMovieSeen=DateTime.UtcNow; LastMovieCameraTime=info.DateTime;
   }
  }} finally { if(item!=IntPtr.Zero) EdsRelease(item); }
  return 0;
 }
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] public static extern bool SetDllDirectory(string path);
 [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
 [DllImport("EDSDK.dll")] public static extern uint EdsInitializeSDK();
 [DllImport("EDSDK.dll")] public static extern uint EdsTerminateSDK();
 [DllImport("EDSDK.dll")] public static extern uint EdsGetCameraList(out IntPtr list);
 [DllImport("EDSDK.dll")] public static extern uint EdsGetChildCount(IntPtr list,out int count);
 [DllImport("EDSDK.dll")] public static extern uint EdsGetChildAtIndex(IntPtr list,int index,out IntPtr camera);
 [DllImport("EDSDK.dll")] public static extern uint EdsOpenSession(IntPtr camera);
 [DllImport("EDSDK.dll")] public static extern uint EdsCloseSession(IntPtr camera);
 [DllImport("EDSDK.dll")] public static extern uint EdsRelease(IntPtr reference);
 [DllImport("EDSDK.dll")] public static extern uint EdsGetPropertySize(IntPtr camera,uint property,int parameter,out uint type,out int size);
 [DllImport("EDSDK.dll")] public static extern uint EdsGetPropertyData(IntPtr camera,uint property,int parameter,int size,byte[] data);
 [DllImport("EDSDK.dll")] public static extern uint EdsSetPropertyData(IntPtr camera,uint property,int parameter,int size,ref uint data);
 [DllImport("EDSDK.dll",EntryPoint="EdsSetPropertyData")] public static extern uint EdsSetPropertyBytes(IntPtr camera,uint property,int parameter,int size,byte[] data);
 [DllImport("EDSDK.dll")] public static extern uint EdsGetEvent();
 public static void Check(uint code,string operation) { if(code!=0) throw new Exception(operation+": 0x"+code.ToString("X8")); }
 public static byte[] Read(IntPtr camera,uint property) {
  uint type; int size; Check(EdsGetPropertySize(camera,property,0,out type,out size),"PropertySize "+property.ToString("X"));
  var bytes=new byte[size]; Check(EdsGetPropertyData(camera,property,0,size,bytes),"PropertyData "+property.ToString("X")); return bytes;
 }
 public static uint ReadNumber(IntPtr camera,uint property) { return BitConverter.ToUInt32(Read(camera,property),0); }
 public static void WriteNumber(IntPtr camera,uint property,uint value) { Check(EdsSetPropertyData(camera,property,0,4,ref value),"SetProperty "+property.ToString("X")); }
 public static void Pump(int milliseconds) {
  var until=DateTime.UtcNow.AddMilliseconds(milliseconds);
  do { Check(EdsGetEvent(),"Events"); System.Threading.Thread.Sleep(50); } while(DateTime.UtcNow<until);
 }
}
