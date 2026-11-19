using System;
using System.IO;
using System.Text;
namespace RecorderStudio {
 public static class RecordingChime {
  public static byte[] CreateWave(bool unexpected,double volume) {
   const int rate=44100;int tones=unexpected?3:2;
   int toneSamples=(int)(rate*0.075),gapSamples=(int)(rate*0.035);
   int count=tones*(toneSamples+gapSamples);
   double amplitude=Math.Max(0,Math.Min(1,volume))*0.55*short.MaxValue;
   using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)) {
    writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
    writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
    writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
    for(int note=0;note<tones;note++) {
     double frequency=unexpected?1000-note*180:880-note*220;
     for(int i=0;i<toneSamples;i++) {
      double fade=Math.Min(1,Math.Min(i/(rate*0.008),(toneSamples-i)/(rate*0.012)));
      writer.Write((short)(amplitude*fade*Math.Sin(2*Math.PI*frequency*i/rate)));
     }
     for(int i=0;i<gapSamples;i++)writer.Write((short)0);
    }
    writer.Flush();return stream.ToArray();
   }
  }
  public static bool Play(bool unexpected,double volume){return AudioOutput.Play(CreateWave(unexpected,volume),true);}
 }
}
