using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RecorderStudio;
public static class ThermalUiTests {
 [STAThread] public static int Main() {
  bool oldReconnect=AppPreferences.Current.AutoReconnect;double oldVolume=AppPreferences.Current.AlertVolume;
  CameraEngine engine=null;
  try {
   AppPreferences.Current.AutoReconnect=false;AppPreferences.Current.AlertVolume=0;
   var app=new Application();Window window;
   using(Stream stream=typeof(UiText).Assembly.GetManifestResourceStream("MainWindow.xaml"))window=(Window)XamlReader.Load(stream);
   var controller=new StudioController(window);
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   engine=(CameraEngine)typeof(StudioController).GetField("engine",flags).GetValue(controller);
   typeof(StudioController).GetMethod("Render",flags).Invoke(controller,new object[]{new CameraStatus {CoolingLock=true,TemperatureAvailable=true,Temperature="Warning reported"}});
   foreach(string name in new[]{"StartButton","ReconnectButton","FaceFocusButton"})if(((Button)window.FindName(name)).IsEnabled)throw new Exception(name+" must be disabled");
   if(((CheckBox)window.FindName("PreviewToggle")).IsEnabled)throw new Exception("Preview must be disabled");
   if(((TextBlock)window.FindName("TemperatureAlarm")).Visibility!=Visibility.Visible)throw new Exception("Persistent alarm required");
   typeof(StudioController).GetMethod("Render",flags).Invoke(controller,new object[]{new CameraStatus {PowerRest=true,CanStart=true,PendingTransfers=1}});
   if(!((Button)window.FindName("StartButton")).IsEnabled || !((CheckBox)window.FindName("PreviewToggle")).IsEnabled)throw new Exception("Resting camera must remain wakeable");
   if(!((Button)window.FindName("TransferButton")).IsEnabled)throw new Exception("Pending takes must be accessible after resting");
   Console.WriteLine("PASS cooling UI blocks Start, reconnect, focus and preview and shows a persistent warning; resting camera permits wake and pending-transfer actions; no camera accessed");return 0;
  } catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  finally {if(engine!=null)engine.Dispose();AppPreferences.Current.AutoReconnect=oldReconnect;AppPreferences.Current.AlertVolume=oldVolume;}
 }
}
