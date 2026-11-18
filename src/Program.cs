using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RecorderStudio {
 public static class Program {
  [STAThread] public static void Main() {
   RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
   bool created; using(var mutex=new Mutex(true,"Local\\Canon1300DRecorderStudio",out created)) {
    if(!created) { MessageBox.Show(UiText.T("Recorder Studio is already open."),"1300D Recorder"); return; }
    var app=new Application();
    app.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
     MessageBox.Show(e.Exception.Message,"1300D Recorder"); e.Handled=true;
    };
    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("MainWindow.xaml")) {
     Window window=(Window)XamlReader.Load(stream); window.MinHeight=600; window.Width=Math.Min(window.Width,SystemParameters.WorkArea.Width-30); window.Height=Math.Min(window.Height,SystemParameters.WorkArea.Height-30); var controller=new StudioController(window); window.Show(); window.Activate(); app.Run(); GC.KeepAlive(controller);
    }
   }
  }
 }
}
