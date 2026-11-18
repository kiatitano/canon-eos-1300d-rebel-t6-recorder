using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace RecorderStudio {
 public sealed partial class StudioController {
  int settingsTab;
  bool changingLanguage;
  void LoadAlarmPreferences() {
   alarmSound=AppPreferences.Current.SoundAlerts; alarmVolume=AppPreferences.Current.AlertVolume; batteryThreshold=AppPreferences.Current.BatteryThreshold;
  }
  void SaveAlarmPreferences() {
   AppPreferences.Current.SoundAlerts=alarmSound; AppPreferences.Current.AlertVolume=alarmVolume; AppPreferences.Current.BatteryThreshold=batteryThreshold; TrySavePreferences();
  }
  void TrySavePreferences() {
   try { AppPreferences.Current.Save(); } catch(Exception ex) { audioError=UiText.T("Could not save preferences. ")+ex.Message; }
  }
  void InitializeLanguage() {
   var picker=Get<ComboBox>("LanguagePicker"); picker.SelectedIndex=UiText.IsPersian?1:0;
   picker.SelectionChanged+=delegate { if(!changingLanguage) ChangeLanguage(picker.SelectedIndex==1?"fa":"en"); };
  }
  void ChangeLanguage(string language) {
   AppPreferences.Current.Language=language; TrySavePreferences();
   changingLanguage=true; Get<ComboBox>("LanguagePicker").SelectedIndex=UiText.IsPersian?1:0; changingLanguage=false;
   UiText.Apply(window); Render(latest);
   if(settingsWindow!=null) window.Dispatcher.BeginInvoke(new Action(delegate { if(settingsWindow!=null) settingsWindow.Close(); OpenSettings(); }));
  }
  TextBlock SettingText(string text,int size=14) { return new TextBlock {Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray,Margin=new Thickness(0,0,0,10)}; }
  Button SettingButton(string text,Action action) { var button=new Button {Content=text,Padding=new Thickness(14,8,14,8),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,10,10)}; button.Click+=delegate {action();}; return button; }
  void OpenSettings() {
   if(settingsWindow!=null) { settingsWindow.Activate(); engine.RefreshSettings(); return; }
   settingsWindow=new Window {Title="Settings — EOS 1300D Recorder",Owner=window,Width=820,Height=820,MinWidth=680,MinHeight=600,Background=new SolidColorBrush(Color.FromRgb(28,31,35)),Foreground=Brushes.White,FontFamily=new FontFamily("Segoe UI"),FontSize=14,FlowDirection=UiText.Flow,WindowStartupLocation=WindowStartupLocation.CenterOwner};
   settingsWindow.Resources.MergedDictionaries.Add(window.Resources);
   var outer=new DockPanel {Margin=new Thickness(26)};
   var heading=new StackPanel {Margin=new Thickness(0,0,0,18)};
   heading.Children.Add(new TextBlock {Text="Settings",FontSize=28});
   heading.Children.Add(SettingText("Camera controls, recording preferences and verified file transfers."));
   DockPanel.SetDock(heading,Dock.Top); outer.Children.Add(heading);
   var tabs=new TabControl {Background=Brushes.Transparent,BorderThickness=new Thickness(0)};
   tabs.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='TabControl'><DockPanel><TabPanel IsItemsHost='True' DockPanel.Dock='Top' Margin='0,0,0,18'/><ContentPresenter ContentSource='SelectedContent'/></DockPanel></ControlTemplate>");
   var tabStyle=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='TabItem'><Setter Property='Foreground' Value='#F4F3EE'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TabItem'><Border x:Name='Surface' CornerRadius='7' Background='#24282D' Padding='16,10' Margin='0,0,8,0'><ContentPresenter ContentSource='Header'/></Border><ControlTemplate.Triggers><Trigger Property='IsSelected' Value='True'><Setter TargetName='Surface' Property='Background' Value='#424A54'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Surface' Property='BorderBrush' Value='White'/><Setter TargetName='Surface' Property='BorderThickness' Value='1'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");
   tabs.Resources.Add(typeof(TabItem),tabStyle);
   var cameraPanel=new StackPanel();
   cameraPanel.Children.Add(SettingText("Supported camera controls. Apply changes before recording."));
   cameraPanel.Children.Add(SettingButton("Refresh camera settings",delegate {engine.RefreshSettings();}));
   settingsRows=new StackPanel(); cameraPanel.Children.Add(settingsRows);
   tabs.Items.Add(new TabItem {Header=UiText.T("Camera"),Content=new ScrollViewer {Content=cameraPanel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
   tabs.Items.Add(new TabItem {Header=UiText.T("App preferences"),Content=new ScrollViewer {Content=BuildAppPreferences(),VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
   var about=new StackPanel();
   about.Children.Add(new TextBlock {Text="EOS 1300D Recorder Studio",FontSize=24,Margin=new Thickness(0,0,0,12)});
   about.Children.Add(SettingText("Version 1.1.0 · Windows · English / Persian"));
   about.Children.Add(SettingText("An unofficial USB recording companion for the Canon EOS 1300D / Rebel T6 / Kiss X80. Not affiliated with Canon."));
   about.Children.Add(SettingText("Records original movies to the camera card. Clip restart leaves a short gap. USB preview is lower resolution than the original movie. This app does not turn this camera into a direct-to-PC Full HD recorder."));
   about.Children.Add(SettingText("Transfers read each completed movie twice and compare SHA-256 before removing its card original. Choose No to keep originals. Keep the app open if you want to transfer its pending takes later."));
   about.Children.Add(SettingText("No accounts, telemetry or automatic uploads. Your video stays on your devices."));
   about.Children.Add(SettingText("Shortcuts: Ctrl+R record · Ctrl+S stop · F8 preview"));
   about.Children.Add(SettingButton("Open project on GitHub",delegate {Process.Start("https://github.com/kiatitano/canon-eos-1300d-rebel-t6-recorder");}));
   about.Children.Add(SettingButton("Open log folder",delegate {Process.Start("explorer.exe",Path.GetDirectoryName(engine.LogPath));}));
   tabs.Items.Add(new TabItem {Header=UiText.T("About & help"),Content=new ScrollViewer {Content=about,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
   tabs.SelectedIndex=Math.Min(settingsTab,2); tabs.SelectionChanged+=delegate {settingsTab=tabs.SelectedIndex;}; outer.Children.Add(tabs);
   settingsWindow.Content=outer;
   settingsWindow.KeyDown+=delegate(object sender,KeyEventArgs e) {if(e.Key==Key.Escape) settingsWindow.Close();};
   settingsWindow.Closed+=delegate {settingsWindow=null;settingsRows=null;settingButtons.Clear();preferenceButtons.Clear();};
   UiText.Apply(settingsWindow); settingsWindow.Show(); engine.RefreshSettings();
  }
  StackPanel BuildAppPreferences() {
   var panel=new StackPanel(); var prefs=AppPreferences.Current;
   panel.Children.Add(SettingText("Language",18));
   var language=new ComboBox {Width=240,HorizontalAlignment=HorizontalAlignment.Left,Foreground=Brushes.Black,Margin=new Thickness(0,0,0,18)};
   language.Items.Add("English"); language.Items.Add("فارسی"); language.SelectedIndex=UiText.IsPersian?1:0;
   language.SelectionChanged+=delegate {ChangeLanguage(language.SelectedIndex==1?"fa":"en");}; panel.Children.Add(language);
   panel.Children.Add(SettingText("Transfer destination",18));
   panel.Children.Add(new TextBox {Text=prefs.OutputDirectory,IsReadOnly=true,FlowDirection=FlowDirection.LeftToRight,Padding=new Thickness(10),Margin=new Thickness(0,0,0,10)});
   var folders=new StackPanel {Orientation=Orientation.Horizontal};
   var browse=SettingButton("Choose folder",delegate {
    if(latest.Recording || latest.Transferring) return;
    using(var dialog=new System.Windows.Forms.FolderBrowserDialog {SelectedPath=prefs.OutputDirectory,Description=UiText.T("Choose a local folder for verified video transfers")}) {
     if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK) {prefs.OutputDirectory=Path.GetFullPath(dialog.SelectedPath); TrySavePreferences(); settingsWindow.Close(); OpenSettings();}
    }
   }); browse.Tag=true; preferenceButtons.Add(browse); folders.Children.Add(browse);
   folders.Children.Add(SettingButton("Open folder",delegate {Directory.CreateDirectory(prefs.OutputDirectory);Process.Start("explorer.exe",prefs.OutputDirectory);})); panel.Children.Add(folders);
   panel.Children.Add(SettingText("Original files are only removed from the card after verification and your confirmation."));
   panel.Children.Add(SettingText("Battery alerts",18));
   var sound=new CheckBox {Content="Battery sound alerts",IsChecked=alarmSound,Margin=new Thickness(0,0,0,10)};
   sound.Checked+=delegate {alarmSound=true;SaveAlarmPreferences();}; sound.Unchecked+=delegate {alarmSound=false;SaveAlarmPreferences();}; panel.Children.Add(sound);
   panel.Children.Add(SettingButton("Test beep alert",delegate {audioError="";BatteryBeeper.Play(false,alarmVolume);}));
   panel.Children.Add(SettingText("Alert volume"));
   var volume=new Slider {Minimum=0.2,Maximum=1,Value=alarmVolume,Width=300,HorizontalAlignment=HorizontalAlignment.Left,TickFrequency=0.1,IsSnapToTickEnabled=true,Margin=new Thickness(0,0,0,14)};
   volume.ValueChanged+=delegate {alarmVolume=volume.Value;SaveAlarmPreferences();}; panel.Children.Add(volume);
   var threshold=new ComboBox {Width=300,HorizontalAlignment=HorizontalAlignment.Left,Foreground=Brushes.Black,Margin=new Thickness(0,0,0,12)};
   threshold.Items.Add(UiText.T("Warn at the low level (30%)")); threshold.Items.Add(UiText.T("Early warning at 50%")); threshold.SelectedIndex=batteryThreshold==50?1:0;
   threshold.SelectionChanged+=delegate {batteryThreshold=threshold.SelectedIndex==1?50:30;SaveAlarmPreferences();}; panel.Children.Add(threshold);
   panel.Children.Add(SettingText("This camera reports coarse battery levels, not an exact percentage. Critical alerts repeat at the last level. Keepalive cannot prevent an empty battery or thermal shutdown."));
   panel.Children.Add(SettingText("Recording workflow",18));
   var reconnect=new CheckBox {Content="Reconnect automatically",IsChecked=prefs.AutoReconnect,Margin=new Thickness(0,0,0,10)};
   reconnect.Checked+=delegate {prefs.AutoReconnect=true;TrySavePreferences();}; reconnect.Unchecked+=delegate {prefs.AutoReconnect=false;TrySavePreferences();};panel.Children.Add(reconnect);
   var preview=new CheckBox {Content="Enable preview when the app opens",IsChecked=prefs.PreviewOnStart,Margin=new Thickness(0,0,0,14)};
   preview.Checked+=delegate {prefs.PreviewOnStart=true;TrySavePreferences();}; preview.Unchecked+=delegate {prefs.PreviewOnStart=false;TrySavePreferences();};panel.Children.Add(preview);
   var timeoutLabel=SettingText(UiText.F("Face autofocus timeout: {0} s",prefs.FocusTimeoutSeconds)); panel.Children.Add(timeoutLabel);
   var timeout=new Slider {Minimum=4,Maximum=15,Value=prefs.FocusTimeoutSeconds,TickFrequency=1,IsSnapToTickEnabled=true,Width=300,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,14)};
   timeout.ValueChanged+=delegate {prefs.FocusTimeoutSeconds=(int)timeout.Value;UiText.SetText(timeoutLabel,UiText.F("Face autofocus timeout: {0} s",prefs.FocusTimeoutSeconds));TrySavePreferences();};panel.Children.Add(timeout);
   panel.Children.Add(SettingText("Canon SDK location",18));
   panel.Children.Add(new TextBox {Text=SdkLocator.Find()??"Not found",IsReadOnly=true,FlowDirection=FlowDirection.LeftToRight,Padding=new Thickness(10),Margin=new Thickness(0,0,0,10)});
   var sdk=SettingButton("Choose 32-bit EDSDK folder",delegate {
    if(latest.Recording || latest.Transferring) return;
    using(var dialog=new System.Windows.Forms.FolderBrowserDialog {Description="Choose the folder containing the 32-bit EDSDK.dll"}) {
     if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK) {
      if(!SdkLocator.IsCompatible(dialog.SelectedPath)) {MessageBox.Show(settingsWindow,UiText.T("That folder does not contain a compatible 32-bit EDSDK.dll."));return;}
      prefs.SdkDirectory=dialog.SelectedPath;TrySavePreferences();engine.Connect();settingsWindow.Close();OpenSettings();
     }
    }
   }); sdk.Tag=true;preferenceButtons.Add(sdk);panel.Children.Add(sdk);
   panel.Children.Add(SettingText("Install Canon EOS Utility separately. Canon DLLs are not included in this app. A 64-bit-only SDK cannot be loaded by this x86 build."));
   return panel;
  }
  void BuildSettingRows(System.Collections.Generic.List<CameraSetting> settings) {
   if(settingsRows==null) return; settingsRows.Children.Clear(); settingButtons.Clear();
   foreach(var setting in settings) {
    var row=new Grid {Margin=new Thickness(0,0,0,16)}; row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(230)}); row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(72)});
    var label=new StackPanel {Margin=new Thickness(0,0,10,0)}; label.Children.Add(new TextBlock {Text=setting.Name,FontWeight=FontWeights.SemiBold});
    label.Children.Add(new TextBlock {Text=setting.Available?"Current: "+CameraSettings.Label(setting.Id,setting.Value):"Use the camera menu",FontSize=12,Foreground=Brushes.LightGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,0)}); row.Children.Add(label);
    var combo=new ComboBox {ItemsSource=setting.Choices,Foreground=Brushes.Black,Background=Brushes.White,MinHeight=32,VerticalAlignment=VerticalAlignment.Top,FlowDirection=UiText.Flow,Margin=new Thickness(0,0,10,0),IsEnabled=setting.Choices.Count>1};
    foreach(var choice in setting.Choices) if(choice.Value==setting.Value) combo.SelectedItem=choice;
    if(setting.Choices.Count==0) { combo.ItemsSource=null; combo.Items.Add(setting.Hint); combo.SelectedIndex=0; }
    combo.ToolTip=setting.Hint;
    System.Windows.Automation.AutomationProperties.SetName(combo,setting.Name);
    Grid.SetColumn(combo,1); row.Children.Add(combo);
    var apply=new Button {Content="Apply",Padding=new Thickness(8,6,8,6),VerticalAlignment=VerticalAlignment.Top,IsEnabled=latest.Connected && !latest.Recording && !latest.Transferring && setting.Choices.Count>1,Tag=setting.Choices.Count>1};
    uint id=setting.Id; apply.Click+=delegate { var choice=combo.SelectedItem as SettingChoice; if(choice!=null) engine.ApplySetting(id,choice.Value); };
    System.Windows.Automation.AutomationProperties.SetName(apply,"Apply "+setting.Name);
    Grid.SetColumn(apply,2); row.Children.Add(apply); settingButtons.Add(apply); settingsRows.Children.Add(row);
   }
   var focus=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,6,0,16)};
   foreach(int direction in new[]{-1,1}) { int value=direction; var button=new Button {Content=direction<0?"Focus nearer":"Focus farther",Padding=new Thickness(14,8,14,8),Margin=new Thickness(0,0,10,0),Tag=true,IsEnabled=latest.Connected && !latest.Recording && !latest.Transferring}; button.Click+=delegate {engine.Focus(value);}; focus.Children.Add(button); settingButtons.Add(button); }
   settingsRows.Children.Add(focus);
   settingsRows.Children.Add(new TextBlock {Text="Set resolution, frame rate, audio and movie exposure mode on the camera. Set AF/MF on the lens. Auto exposure may prevent manual ISO, shutter or aperture changes.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray,FontSize=12});
   UiText.Apply(settingsRows);
  }
  void UpdateBatteryAlarm(CameraStatus status) {
   int severity=BatteryPolicy.Severity(status.BatteryLevel,batteryThreshold);
   var alarm=Get<TextBlock>("BatteryAlarm"); alarm.Visibility=severity>0?Visibility.Visible:Visibility.Collapsed;
   UiText.SetText(alarm,severity==2?"Battery critical. Save your recording and replace or charge the battery now.":"Battery low. Prepare a replacement. The reported percentage is approximate.");
   Get<TextBlock>("BatteryText").ToolTip="Coarse camera-reported level, not an exact percentage. Updated every 5 seconds.";
   Get<TextBlock>("BatteryText").Foreground=severity>0?Brushes.Salmon:Brushes.White;
   if(severity>0 && alarmSound && (severity!=lastAlarmSeverity || alarmClock.ElapsedMilliseconds-lastAlarmAt>=(severity==2?30000:180000))) {
    BatteryBeeper.Play(severity==2,alarmVolume); lastAlarmAt=alarmClock.ElapsedMilliseconds;
   }
   lastAlarmSeverity=severity;
  }
 }
}
