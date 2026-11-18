using System;
using System.Text;
using RecorderStudio;
public static class BeeperTests {
 public static void Main() {
  foreach(bool critical in new[]{false,true}) {
   byte[] wave=BatteryBeeper.CreateWave(critical,0.55);
   if(Encoding.ASCII.GetString(wave,0,4)!="RIFF" || Encoding.ASCII.GetString(wave,8,4)!="WAVE" || BitConverter.ToInt32(wave,4)!=wave.Length-8 || BitConverter.ToInt32(wave,40)!=wave.Length-44) throw new Exception("Invalid WAV");
   int bursts=0; bool active=false; int silence=1000;
   for(int i=44;i<wave.Length;i+=2) {
    int value=Math.Abs((int)BitConverter.ToInt16(wave,i)); if(value>0) { if(!active && silence>1000) bursts++; active=true; silence=0; } else if(++silence>1000) active=false;
    if(value>20000) throw new Exception("Amplitude exceeded test volume");
   }
   if(bursts!=(critical?15:9)) throw new Exception("Wrong beep count "+bursts);
   Console.WriteLine("PASS "+(critical?"critical 15":"warning 9")+" custom beeps; valid PCM WAV, bounded amplitude");
  }
  byte[] silent=BatteryBeeper.CreateWave(false,0); for(int i=44;i<silent.Length;i++) if(silent[i]!=0) throw new Exception("Zero volume not silent");
  Console.WriteLine("PASS volume control");
 }
}
