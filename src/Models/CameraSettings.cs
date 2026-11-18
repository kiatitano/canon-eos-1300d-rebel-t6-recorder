using System;
using System.Collections.Generic;
namespace RecorderStudio {
 public static class BatteryPolicy {
  public static int Severity(int level,int warningThreshold) { if(level<0 || level>100) return 0; if(level<=1) return 2; return level<=warningThreshold?1:0; }
 }
 public sealed class SettingChoice {
  public uint Value; public string Label;
  public override string ToString() { return UiText.T(Label); }
 }
 public sealed class CameraSetting {
  public uint Id,Value; public string Name,Hint; public bool Available;
  public List<SettingChoice> Choices=new List<SettingChoice>();
 }
 public static class CameraSettings {
  public static readonly uint[] Ids={0x402,0x406,0x405,0x407,0x106,0x107,0x114,0x50e,0x436};
  public static readonly string[] Names={"ISO sensitivity","Shutter speed","Aperture","Exposure compensation","White balance","Color temperature (K)","Picture style","Live-view AF method","Movie exposure mode"};
  static double Nearest(double value,double[] values) { double best=values[0]; foreach(double n in values) if(Math.Abs(Math.Log(value/n))<Math.Abs(Math.Log(value/best))) best=n; return best; }
  public static string Label(uint id,uint value) {
   if(id>=0xF100 && id<=0xF201) {
    if(id==0xF104) return new[]{"No filter","Yellow","Orange","Red","Green"}[Math.Min(value,4)];
    if(id==0xF105) return new[]{"None","Sepia","Blue","Purple","Green"}[Math.Min(value,4)];
    return unchecked((int)value).ToString("0");
   }
   if(id==0x402) return value==0?"Auto":Math.Round(100*Math.Pow(2,(value-72d)/8)).ToString("0");
   if(id==0x405) return "f/"+Nearest(Math.Pow(2,(value-8d)/16),new double[]{1,1.2,1.4,1.6,1.8,2,2.2,2.5,2.8,3.2,3.5,4,4.5,5,5.6,6.3,7.1,8,9,10,11,13,14,16,18,20,22,25,29,32,36,40,45,51,57,64,80,91}).ToString("0.#");
   if(id==0x406) {
    if(value==0xC) return "Bulb";
    double seconds=Math.Pow(2,(56d-value)/8);
    double[] speeds={1,1.3,1.5,1.6,2,2.5,3,3.2,4,5,6,8,10,13,15,20,25,30,40,45,50,60,80,90,100,125,160,180,200,250,320,350,400,500,640,750,800,1000,1250,1500,1600,2000,2500,3000,3200,4000,5000,6000,6400,8000};
    return seconds>=1?Nearest(seconds,speeds).ToString("0.#")+" s":"1/"+Nearest(1/seconds,speeds).ToString("0");
   }
   if(id==0x407) return (((sbyte)(value&255))/8d).ToString("+0.##;-0.##;0")+" EV";
   if(id==0x106) { string[] wb={"Auto — ambience priority","Daylight","Cloudy","Tungsten","White fluorescent","Flash","Custom","Unknown","Shade","Color temperature"}; if(value<wb.Length) return wb[value]; if(value==23) return "Auto — white priority"; }
   if(id==0x107) return value+" K";
   if(id==0x114) { switch(value) {case 0x87:return "Auto";case 0x81:return "Standard";case 0x82:return "Portrait";case 0x83:return "Landscape";case 0x84:return "Neutral";case 0x85:return "Faithful";case 0x86:return "Monochrome";case 0x21:return "User defined 1";case 0x22:return "User defined 2";case 0x23:return "User defined 3";} }
   if(id==0x50e) { if(value==0)return "Quick AF";if(value==1)return "Live AF — single point";if(value==2)return "Live AF — face detection"; }
   if(id==0x436 && value==20) return "Camera movie mode";
   return "Camera code: 0x"+value.ToString("X");
  }
  public static List<CameraSetting> Read(IntPtr camera) {
   var result=new List<CameraSetting>();
   for(int i=0;i<Ids.Length;i++) {
    var setting=new CameraSetting {Id=Ids[i],Name=Names[i],Hint="Set this on the camera"};
    try {
     setting.Value=CameraSdk.ReadNumber(camera,setting.Id); setting.Available=true;
     CameraSdk.PropertyDesc desc;
     if(CameraSdk.EdsGetPropertyDesc(camera,setting.Id,out desc)==0 && desc.Count>0 && desc.Count<=128) {
      var seen=new HashSet<uint>();
      for(int n=0;n<desc.Count;n++) { uint value=unchecked((uint)desc.Values[n]); if(seen.Add(value)) setting.Choices.Add(new SettingChoice {Value=value,Label=Label(setting.Id,value)}); }
      setting.Hint="Only values reported by the camera";
     }
    } catch { setting.Hint="This model does not expose this control over USB"; }
    result.Add(setting);
   }
   try {
    byte[] details=CameraSdk.Read(camera,0x115); uint style=CameraSdk.ReadNumber(camera,0x114);
    string[] labels={"Contrast","Sharpness","Saturation","Color tone","Monochrome filter","Monochrome toning"};
    int fields=style==0x86?6:4;
    if(details.Length>=24) for(int field=0;field<fields;field++) {
     var setting=new CameraSetting {Id=(uint)(0xF100+field),Name=labels[field],Value=BitConverter.ToUInt32(details,field*4),Available=true,Hint="Documented picture-style range"};
     int min=field==1 || field>=4?0:-4,max=field==1?7:4;
     for(int n=min;n<=max;n++) { uint value=unchecked((uint)n); setting.Choices.Add(new SettingChoice {Value=value,Label=Label(setting.Id,value)}); }
     result.Add(setting);
    }
   } catch {}
   return result;
  }
  public static uint ReadValue(IntPtr camera,uint id) { return id>=0xF100 && id<=0xF105?BitConverter.ToUInt32(CameraSdk.Read(camera,0x115),(int)(id-0xF100)*4):CameraSdk.ReadNumber(camera,id); }
  public static void WriteValue(IntPtr camera,uint id,uint value) {
   if(id>=0xF100 && id<=0xF105) {
    byte[] raw=CameraSdk.Read(camera,0x115); Array.Copy(BitConverter.GetBytes(value),0,raw,(int)(id-0xF100)*4,4);
    CameraSdk.Check(CameraSdk.EdsSetPropertyBytes(camera,0x115,0,raw.Length,raw),"Set picture style detail");
   } else CameraSdk.WriteNumber(camera,id,value);
  }
 }
}
