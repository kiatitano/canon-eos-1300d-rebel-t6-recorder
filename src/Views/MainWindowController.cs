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

 public sealed partial class StudioController {
  readonly Window window; readonly CameraEngine engine;
  readonly IUserDialogs dialogs;
  CameraStatus latest=new CameraStatus(); bool closing,allowClose,framePending;
  bool closeAfterStop,transferRequested;

  Window settingsWindow;
  StackPanel settingsRows;
  readonly System.Collections.Generic.List<Button> settingButtons=new System.Collections.Generic.List<Button>();
  readonly System.Collections.Generic.List<Button> preferenceButtons=new System.Collections.Generic.List<Button>();
  bool alarmSound=true;
  int batteryThreshold=30,lastAlarmSeverity;
  double alarmVolume=0.55;
  string audioError="";
  readonly Stopwatch alarmClock=Stopwatch.StartNew();
  long lastAlarmAt=-999999;
  readonly System.Collections.Generic.List<string> takes=new System.Collections.Generic.List<string>();
  [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
  T Get<T>(string name) where T:class { return window.FindName(name) as T; }
  void Text(string name,string value) { UiText.SetText(Get<TextBlock>(name),value); }
  public StudioController(Window host, IUserDialogs userDialogs=null) {
   window=host; dialogs=userDialogs??new UserDialogs(); engine=new CameraEngine();
   InitializeLanguage();
   UiText.Apply(window);
   LoadAlarmPreferences();
   BatteryBeeper.PlaybackError+=delegate(string detail) { window.Dispatcher.BeginInvoke(new Action(delegate { audioError="Could not play the alert. Check your audio output. "+detail; })); };
   engine.SettingsChanged+=delegate(System.Collections.Generic.List<CameraSetting> settings) { window.Dispatcher.BeginInvoke(new Action(delegate { BuildSettingRows(settings); })); };
   using(var icon=Assembly.GetExecutingAssembly().GetManifestResourceStream("App.ico")) { if(icon!=null) window.Icon=BitmapFrame.Create(icon); }
   window.SourceInitialized+=delegate { try { int dark=1; DwmSetWindowAttribute(new WindowInteropHelper(window).Handle,20,ref dark,4); } catch {} };
   engine.Status+=delegate(CameraStatus status) { window.Dispatcher.BeginInvoke(new Action(delegate { if(!closing) Render(status); })); };
   engine.Frame+=delegate(byte[] bytes) {
    if(framePending || closing) return; framePending=true;
    window.Dispatcher.BeginInvoke(new Action(delegate {
     try {
      if(bytes==null || Get<CheckBox>("PreviewToggle").IsChecked!=true) { Get<Image>("PreviewImage").Source=null; return; }
      BitmapImage image=new BitmapImage(); image.BeginInit(); image.CacheOption=BitmapCacheOption.OnLoad;
      using(var stream=new MemoryStream(bytes)) { image.StreamSource=stream; image.EndInit(); } image.Freeze();
      Get<Image>("PreviewImage").Source=image; Get<StackPanel>("EmptyPreview").Visibility=Visibility.Collapsed;
      Text("PreviewLabel",image.PixelWidth+" × "+image.PixelHeight+"  ·  USB LIVE");
     } catch {} finally { framePending=false; }
    }));
   };
   engine.Take+=delegate(string take) { window.Dispatcher.BeginInvoke(new Action(delegate {
    if(!takes.Contains(take)) takes.Insert(0,take);
    Text("TakeHistory",string.Join("\n",takes.GetRange(0,Math.Min(3,takes.Count)).ToArray())); Get<TextBlock>("TakeHistory").FontFamily=new FontFamily("Consolas");
   })); };
   engine.TransferOffer+=delegate(int count) { window.Dispatcher.BeginInvoke(new Action(delegate {

    ShowTransferOffer(count);
   })); };
   engine.TransferFinished+=delegate(bool success,string result) { window.Dispatcher.BeginInvoke(new Action(delegate {
    transferRequested=false;

    dialogs.Inform(window,result,"Video transfer",!success);
    if(closeAfterStop) FinishClose();
   })); };
   Get<Button>("StartButton").Click+=delegate { engine.Start(); };
   Get<Button>("StopButton").Click+=delegate { engine.Stop(); };
   Get<Button>("ReconnectButton").Click+=delegate { engine.Connect(); };
   Get<Button>("SettingsButton").Click+=delegate { OpenSettings(); };
   Get<Button>("FaceFocusButton").Click+=delegate { Get<CheckBox>("PreviewToggle").IsChecked=true; engine.AutoFocusFace(); };
   Get<Button>("TransferButton").Click+=delegate { if(latest.Transferring || transferRequested) engine.CancelTransfer(); else ShowTransferOffer(latest.PendingTransfers); };
   Get<CheckBox>("PreviewToggle").Checked+=delegate { Preview(true); };
   Get<CheckBox>("PreviewToggle").Unchecked+=delegate { Preview(false); };
   Get<CheckBox>("MirrorToggle").Checked+=delegate { Get<Image>("PreviewImage").RenderTransform=new ScaleTransform(-1,1); AppPreferences.Current.MirrorPreview=true; TrySavePreferences(); };
   Get<CheckBox>("MirrorToggle").Unchecked+=delegate { Get<Image>("PreviewImage").RenderTransform=Transform.Identity; AppPreferences.Current.MirrorPreview=false; TrySavePreferences(); };
   Get<Button>("LogsButton").Click+=delegate { Process.Start("explorer.exe","/select,\""+engine.LogPath+"\""); };
   window.PreviewKeyDown+=delegate(object sender,KeyEventArgs e) {
    if(e.Key==Key.F8) { var toggle=Get<CheckBox>("PreviewToggle"); toggle.IsChecked=toggle.IsChecked!=true; e.Handled=true; }
    if(Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.R && !latest.Transferring && !transferRequested) { engine.Start(); e.Handled=true; }
    if(Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.S) { engine.Stop(); e.Handled=true; }
   };
   window.SizeChanged+=delegate { Adapt(); };
   window.Loaded+=delegate { engine.Connect(); Adapt(); Get<CheckBox>("MirrorToggle").IsChecked=AppPreferences.Current.MirrorPreview; if(AppPreferences.Current.PreviewOnStart) Get<CheckBox>("PreviewToggle").IsChecked=true; };
   window.Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs e) {
    if(allowClose) return;
    if(latest.Transferring || transferRequested) { e.Cancel=true; dialogs.Inform(window,"A transfer is running. Wait for it to finish or choose Cancel transfer.","Video transfer",false); return; }
    if(latest.Recording) {
     e.Cancel=true;
     if(dialogs.Confirm(window,"Stop recording and save the current clip?","Close Recorder Studio")) { closeAfterStop=true; engine.Stop(); }
     return;
    }
    closing=true; engine.Dispose(); allowClose=true;
   };

  }

  void FinishClose() { closing=true; engine.Dispose(); allowClose=true; window.Close(); }
  void ShowTransferOffer(int count) {
   if(count==0) { if(closeAfterStop) FinishClose(); return; }
   string question=UiText.F("Move {0} recorded video file(s)?\n\nDestination:\n{1}\n\nThe app copies and verifies the original files before deleting them from the card.\nChoose No to keep them on the card.",count,MovieTransfer.DestinationRoot);
   bool choice=dialogs.Confirm(window,question,"Transfer recorded videos");
   if(choice) { transferRequested=true; Get<Button>("StartButton").IsEnabled=false; Get<Button>("ReconnectButton").IsEnabled=false; engine.MovePending(); }
   else if(closeAfterStop) FinishClose();
  }
  void Preview(bool on) {
   engine.SetPreview(on); Get<Image>("PreviewImage").Source=null;
   Get<StackPanel>("EmptyPreview").Visibility=Visibility.Visible;
   Text("EmptyTitle",on?"Starting live preview…":"Preview is off");
   Text("EmptySubtitle",on?"Live view should appear shortly":"Enable live preview to frame yourself");
   Text("PreviewLabel",on?"CONNECTING LIVE VIEW":"PREVIEW OFF");
  }
  void Adapt() {
   var grid=Get<Grid>("Workspace"); bool narrow=window.ActualWidth<1030;
   if(narrow) {
    grid.ColumnDefinitions[1].Width=new GridLength(18); grid.ColumnDefinitions[2].Width=new GridLength(255);
   } else { grid.ColumnDefinitions[1].Width=new GridLength(24); grid.ColumnDefinitions[2].Width=new GridLength(280); }
  }

  static string Time(double seconds) { return TimeSpan.FromSeconds(Math.Max(0,seconds)).ToString(@"hh\:mm\:ss"); }
  void Render(CameraStatus status) {
   latest=status;
   UpdateBatteryAlarm(status);
   foreach(var button in preferenceButtons) button.IsEnabled=!status.Recording && !status.Transferring;
   foreach(var button in settingButtons) button.IsEnabled=status.Connected && !status.Recording && !status.Transferring && (bool)button.Tag;
   if(!status.Transferring && !string.IsNullOrEmpty(status.Error)) transferRequested=false;
   Get<Button>("StartButton").IsEnabled=status.CanStart && !transferRequested;
   Get<Button>("StopButton").IsEnabled=status.Recording;
   Get<Button>("FaceFocusButton").IsEnabled=status.Connected && !status.Recording && !status.Transferring && !status.Focusing;
   Get<Button>("ReconnectButton").IsEnabled=!status.Recording && !status.Transferring && !transferRequested;
   Get<Button>("TransferButton").IsEnabled=status.Transferring || (status.Connected && !status.Recording && status.PendingTransfers>0);
   UiText.SetContent(Get<Button>("TransferButton"),status.Transferring?"Cancel transfer":"Transfer files ("+status.PendingTransfers+")");
   Get<System.Windows.Controls.ProgressBar>("TransferProgressBar").Visibility=status.Transferring?Visibility.Visible:Visibility.Collapsed;
   Get<System.Windows.Controls.ProgressBar>("TransferProgressBar").Value=status.TransferProgress*100;
   Get<CheckBox>("PreviewToggle").IsEnabled=!status.Transferring;
   Text("StatusText",status.Message); Text("ErrorText",string.IsNullOrEmpty(status.Error)?audioError:status.Error);
   Text("ConnectionLabel",status.Connected?"USB connected · Battery "+status.Battery:"Camera disconnected");
   Get<System.Windows.Shapes.Ellipse>("ConnectionDot").Fill=new SolidColorBrush((Color)ColorConverter.ConvertFromString(status.Connected?"#80C7A3":"#B6BAC2"));
   Get<System.Windows.Shapes.Ellipse>("RecordDot").Fill=new SolidColorBrush((Color)ColorConverter.ConvertFromString(status.Recording?"#EB6258":"#B6BAC2"));
   Text("RecordLabel",status.Restarting?"SAVING / RESTART":status.Recording?"● REC":"STANDBY");
   Text("ClipTimer",Time(status.ClipSeconds)); Text("TotalTimer",Time(status.TotalSeconds));
   Text("BatteryText",status.Battery); Text("CardText",status.Card);
   Text("RestartText",status.Restarts+" automatic restarts");
   if(status.Transferring) {
    Get<Image>("PreviewImage").Source=null; Get<StackPanel>("EmptyPreview").Visibility=Visibility.Visible;
    Text("EmptyTitle","Transferring original files"); Text("EmptySubtitle",Math.Round(status.TransferProgress*100)+"% — "+status.Message); Text("PreviewLabel","VERIFYING ORIGINAL FILES");
   } else if(!status.Connected && Get<CheckBox>("PreviewToggle").IsChecked==true) {
    Get<Image>("PreviewImage").Source=null; Get<StackPanel>("EmptyPreview").Visibility=Visibility.Visible;
    Text("EmptyTitle","Camera image unavailable"); Text("EmptySubtitle","Check the USB cable and turn the camera on"); Text("PreviewLabel","DISCONNECTED");
   } else if(status.PreviewStale && Get<CheckBox>("PreviewToggle").IsChecked==true) {
    Get<Image>("PreviewImage").Source=null; Get<StackPanel>("EmptyPreview").Visibility=Visibility.Visible;
    Text("EmptyTitle","Live view interrupted"); Text("EmptySubtitle","Trying to receive live view again"); Text("PreviewLabel","LIVE VIEW INTERRUPTED");
   } else if(Get<CheckBox>("PreviewToggle").IsChecked!=true) {
    Get<StackPanel>("EmptyPreview").Visibility=Visibility.Visible;
    Text("EmptyTitle","Preview is off"); Text("EmptySubtitle","Enable live preview to frame yourself"); Text("PreviewLabel","PREVIEW OFF");
   }
   window.Title=UiText.T((status.Recording?"● Recording — ":"")+"EOS 1300D Recorder");
  }
 }
}
