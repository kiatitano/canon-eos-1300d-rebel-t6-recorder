using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
namespace RecorderStudio {
 public static class AudioOutput {
  static readonly object gate=new object();
  static readonly Queue<byte[]> pending=new Queue<byte[]>();
  static bool running;
  public static event Action<string> PlaybackError;
  public static bool Play(byte[] wave,bool queueIfBusy) {
   lock(gate) {
    if((!queueIfBusy && running) || pending.Count>=8) return false;
    pending.Enqueue(wave);
    if(running) return true;
    running=true; ThreadPool.QueueUserWorkItem(delegate {Drain();}); return true;
   }
  }
  static void Drain() {
   while(true) {
    byte[] wave;
    lock(gate) {if(pending.Count==0){running=false;return;}wave=pending.Dequeue();}
    try {using(var stream=new MemoryStream(wave))using(var player=new SoundPlayer(stream)){player.Load();player.PlaySync();}}
    catch(Exception ex) {try {var handler=PlaybackError;if(handler!=null)handler(ex.Message);}catch {}}
   }
  }
 }
}
