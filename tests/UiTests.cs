using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RecorderStudio;

public static class UiTests
{
    [STAThread]
    public static int Main(string[] args)
    {
        string originalLanguage = AppPreferences.Current.Language;
        try
        {
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            var application = new Application();
            Assembly app = typeof(UiText).Assembly;
            Window window;
            using (Stream stream = app.GetManifestResourceStream("MainWindow.xaml")) window = (Window)XamlReader.Load(stream);
            if (args.Length > 0 && args[0] == "--render-fa") { AppPreferences.Current.Language="fa"; UiText.Apply(window); Render(window,"screenshot-fa.png"); return 0; }
            AppPreferences.Current.Language = "en";
            UiText.Apply(window);
            Assert(((TextBlock)window.FindName("EmptyTitle")).Text == "Frame your shot", "English is the source/default language");
            Render(window, "screenshot-en.png");
            AppPreferences.Current.Language = "fa";
            UiText.Apply(window);
            Assert(((TextBlock)window.FindName("EmptyTitle")).Text == "کادر خودت را تنظیم کن", "Persian main UI translated");
            Assert(((FrameworkElement)window.FindName("ControlPanel")).FlowDirection == FlowDirection.RightToLeft, "Persian direction");
            Assert(UiText.T("Current: Neutral").Contains("خنثی"), "Dynamic camera label translated");
            string fileLabel = UiText.T("Copying file 2 of 3: MVI_0001.MOV");
            Assert(fileLabel.Contains("MVI_0001.MOV") && !fileLabel.Contains(" of "), "Transfer translation preserves filenames");
            AppPreferences.Current.Language = "en";
            UiText.Apply(window);
            Assert(((TextBlock)window.FindName("EmptyTitle")).Text == "Frame your shot", "Live switch back to English");
            Assert(((FrameworkElement)window.FindName("ControlPanel")).FlowDirection == FlowDirection.LeftToRight, "English direction restored");
            Console.WriteLine("PASS bilingual UI, live language switching, direction and filename preservation");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { AppPreferences.Current.Language = originalLanguage; }
    }

    private static void Assert(bool condition, string label) { if (!condition) throw new Exception(label); }
    private static void Render(Window window, string name)
    {
        var surface = (FrameworkElement)window.Content;
        if (surface is Panel) ((Panel)surface).Background = window.Background;
        ((ComboBox)window.FindName("LanguagePicker")).SelectedIndex=UiText.IsPersian?1:0;
        var size = new Size(1220, 880);
        surface.Measure(size);
        surface.Arrange(new Rect(size));
        surface.UpdateLayout();
        window.Show();
        window.Dispatcher.Invoke(new Action(delegate {}), System.Windows.Threading.DispatcherPriority.ContextIdle);
        surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1220, 880, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "assets");
        Directory.CreateDirectory(directory);
        using (FileStream output = File.Create(Path.Combine(directory, name))) encoder.Save(output);
        window.Hide();
    }
}
