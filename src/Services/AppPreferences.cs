using System;
using System.IO;
using System.Xml;
using System.Globalization;

namespace RecorderStudio
{
    public sealed class AppPreferences
    {
        public static readonly AppPreferences Current = Load();
        public string Language = "en";
        public string OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "EOS 1300D Recorder");
        public string SdkDirectory = "";
        public bool SoundAlerts = true;
        public bool AutoReconnect = true;
        public bool PreviewOnStart;
        public bool MirrorPreview;
        public int BatteryThreshold = 30;
        public int FocusTimeoutSeconds = 12;
        public double AlertVolume = 0.55;

        public static string DataDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EOS 1300D Recorder"); } }
        public static string FilePath { get { return Path.Combine(DataDirectory, "settings.xml"); } }

        private static AppPreferences Load()
        {
            var prefs = new AppPreferences();
            try
            {
                if (File.Exists(FilePath))
                {
                    var document = new XmlDocument();
                    document.Load(FilePath);
                    XmlElement root = document.DocumentElement;
                    prefs.Language = root.GetAttribute("language") == "fa" ? "fa" : "en";
                    string output = root.GetAttribute("outputDirectory");
                    if (!string.IsNullOrWhiteSpace(output) && Path.IsPathRooted(output)) prefs.OutputDirectory = Path.GetFullPath(output);
                    prefs.SdkDirectory = root.GetAttribute("sdkDirectory");
                    prefs.SoundAlerts = root.GetAttribute("soundAlerts") != "false";
                    prefs.AutoReconnect = root.GetAttribute("autoReconnect") != "false";
                    prefs.PreviewOnStart = root.GetAttribute("previewOnStart") == "true";
                    prefs.MirrorPreview = root.GetAttribute("mirrorPreview") == "true";
                    int number;
                    if (int.TryParse(root.GetAttribute("batteryThreshold"), out number) && (number == 30 || number == 50)) prefs.BatteryThreshold = number;
                    if (int.TryParse(root.GetAttribute("focusTimeout"), out number)) prefs.FocusTimeoutSeconds = Math.Max(4, Math.Min(15, number));
                    double volume;
                    if (double.TryParse(root.GetAttribute("volume"), NumberStyles.Float, CultureInfo.InvariantCulture, out volume)) prefs.AlertVolume = Math.Max(0.2, Math.Min(1, volume));
                }
                else
                {
                    // Migrate the original private prototype without changing fresh-install defaults.
                    string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1300D Recorder", "alarm.txt");
                    if (File.Exists(legacy))
                    {
                        string[] lines = File.ReadAllLines(legacy);
                        if (lines.Length > 0) prefs.SoundAlerts = lines[0] != "off";
                        if (Directory.Exists(@"D:\1300D Videos")) prefs.OutputDirectory = @"D:\1300D Videos";
                    }
                }
            }
            catch { /* Corrupt preferences must never prevent opening the app. */ }
            return prefs;
        }

        public void Save()
        {
            Directory.CreateDirectory(DataDirectory);
            var document = new XmlDocument();
            XmlElement root = document.CreateElement("settings");
            document.AppendChild(root);
            root.SetAttribute("language", Language == "fa" ? "fa" : "en");
            root.SetAttribute("outputDirectory", OutputDirectory);
            root.SetAttribute("sdkDirectory", SdkDirectory);
            root.SetAttribute("soundAlerts", SoundAlerts ? "true" : "false");
            root.SetAttribute("autoReconnect", AutoReconnect ? "true" : "false");
            root.SetAttribute("previewOnStart", PreviewOnStart ? "true" : "false");
            root.SetAttribute("mirrorPreview", MirrorPreview ? "true" : "false");
            root.SetAttribute("batteryThreshold", BatteryThreshold.ToString(CultureInfo.InvariantCulture));
            root.SetAttribute("focusTimeout", FocusTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            root.SetAttribute("volume", AlertVolume.ToString(CultureInfo.InvariantCulture));
            string temporary = FilePath + ".tmp";
            document.Save(temporary);
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
            else File.Move(temporary, FilePath);
        }
    }
}
