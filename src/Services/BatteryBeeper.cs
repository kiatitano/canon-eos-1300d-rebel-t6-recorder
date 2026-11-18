using System;
using System.IO;
using System.Media;
using System.Text;
using System.Threading;
namespace RecorderStudio {
 public static class BatteryBeeper {
  static int playing;
  public static event Action<string> PlaybackError;
  public static byte[] CreateWave(bool critical,double volume) {
   const int rate=44100; int groups=critical?5:3;
   double tone=critical?0.16:0.18,gap=critical?0.065:0.09,groupGap=0.25;
   int toneSamples=(int)(tone*rate),gapSamples=(int)(gap*rate),groupSamples=(int)(groupGap*rate);
   int count=groups*(3*(toneSamples+gapSamples)+groupSamples);
   double amplitude=Math.Max(0,Math.Min(1,volume))*0.8*short.MaxValue;
   using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream)) {
    writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+count*2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
    writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16);
    writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(count*2);
    for(int group=0;group<groups;group++) {
     for(int note=0;note<3;note++) {
      double frequency=note==2?1250:1000;
      for(int i=0;i<toneSamples;i++) {
       double fade=Math.Min(1,Math.Min(i/(rate*0.008),(toneSamples-1-i)/(rate*0.008)));
       writer.Write((short)(amplitude*fade*Math.Sin(2*Math.PI*frequency*i/rate)));
      }
      for(int i=0;i<gapSamples;i++) writer.Write((short)0);
     }
     for(int i=0;i<groupSamples;i++) writer.Write((short)0);
    }
    writer.Flush(); return stream.ToArray();
   }
  }
  public static bool Play(bool critical,double volume) {
   if(Interlocked.CompareExchange(ref playing,1,0)!=0) return false;
   ThreadPool.QueueUserWorkItem(delegate {
    try { using(var stream=new MemoryStream(CreateWave(critical,volume))) using(var player=new SoundPlayer(stream)) { player.Load(); player.PlaySync(); } }
    catch(Exception ex) { if(PlaybackError!=null) PlaybackError(ex.Message); }
    finally { Interlocked.Exchange(ref playing,0); }
   });
   return true;
  }
 }
}
