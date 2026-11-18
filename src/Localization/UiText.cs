using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace RecorderStudio
{
    /// <summary>English source strings with a Persian catalog. Unknown SDK diagnostics remain unchanged.</summary>
    public static class UiText
    {
        private static readonly Dictionary<string, string> Persian = Load();
        private static readonly string[] Fragments = Persian.Keys.Where(k => k.Length >= 4 &&
            (char.IsWhiteSpace(k[0]) || char.IsWhiteSpace(k[k.Length - 1]))).OrderByDescending(k => k.Length).ToArray();
        private static readonly DependencyProperty SourceText = DependencyProperty.RegisterAttached("SourceText", typeof(string), typeof(UiText));
        private static readonly DependencyProperty SourceContent = DependencyProperty.RegisterAttached("SourceContent", typeof(string), typeof(UiText));
        private static readonly DependencyProperty SourceHint = DependencyProperty.RegisterAttached("SourceHint", typeof(string), typeof(UiText));
        private static readonly DependencyProperty SourceTitle = DependencyProperty.RegisterAttached("SourceTitle", typeof(string), typeof(UiText));

        public static bool IsPersian { get { return AppPreferences.Current.Language == "fa"; } }
        public static FlowDirection Flow { get { return IsPersian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; } }
        public static MessageBoxOptions DialogOptions { get { return IsPersian ? MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading : MessageBoxOptions.None; } }

        private static Dictionary<string, string> Load()
        {
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("fa.json"))
            {
                var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                return (Dictionary<string, string>)serializer.ReadObject(resource);
            }
        }

        public static string T(string source) { return Translate(source, 0); }
        private static string Translate(string source, int depth)
        {
            if (source == null || !IsPersian || depth > 5) return source;
            string translated;
            if (Persian.TryGetValue(source, out translated)) return translated;
            Match copying = Regex.Match(source, @"^Copying file (\d+) of (\d+): (.+)$");
            if (copying.Success) return string.Format(Persian["Copying file {0} of {1}: {2}"], copying.Groups[1].Value, copying.Groups[2].Value, copying.Groups[3].Value);
            // Legacy messages are composed from known sentence fragments plus filenames/numbers.
            // Translate only boundary fragments; never replace words inside a path or SDK identifier.
            foreach (string key in Fragments)
            {
                if (source.StartsWith(key, StringComparison.Ordinal)) return Persian[key] + Translate(source.Substring(key.Length), depth + 1);
                if (source.EndsWith(key, StringComparison.Ordinal)) return Translate(source.Substring(0, source.Length - key.Length), depth + 1) + Persian[key];
            }
            return source;
        }

        public static string F(string format, params object[] args) { return string.Format(T(format), args); }
        public static void SetText(TextBlock block, string source) { block.SetValue(SourceText, source); block.Text = T(source); }
        public static void SetContent(ContentControl control, string source) { control.SetValue(SourceContent, source); control.Content = T(source); }

        public static void Apply(DependencyObject element)
        {
            var window = element as Window;
            if (window != null)
            {
                string source = window.GetValue(SourceTitle) as string;
                if (source == null) { source = window.Title; window.SetValue(SourceTitle, source); }
                window.Title = T(source);
            }
            var text = element as TextBlock;
            if (text != null)
            {
                string source = text.GetValue(SourceText) as string;
                if (source == null) { source = text.Text; text.SetValue(SourceText, source); }
                text.Text = T(source);
            }
            var content = element as ContentControl;
            if (content != null && content.Content is string)
            {
                string source = content.GetValue(SourceContent) as string;
                if (source == null) { source = (string)content.Content; content.SetValue(SourceContent, source); }
                content.Content = T(source);
            }
            var framework = element as FrameworkElement;
            if (framework != null)
            {
                if ((framework.Tag as string) == "localize-flow") framework.FlowDirection = Flow;
                if (framework.ToolTip is string)
                {
                    string source = framework.GetValue(SourceHint) as string;
                    if (source == null) { source = (string)framework.ToolTip; framework.SetValue(SourceHint, source); }
                    framework.ToolTip = T(source);
                }
            }
            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                var dependency = child as DependencyObject;
                if (dependency != null) Apply(dependency);
            }
        }
    }
}
