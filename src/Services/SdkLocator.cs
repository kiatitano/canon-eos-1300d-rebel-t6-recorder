using System;
using System.IO;

namespace RecorderStudio
{
    public static class SdkLocator
    {
        public static bool IsCompatible(string folder)
        {
            try
            {
                string path = Path.Combine(folder, "EDSDK.dll");
                using (var reader = new BinaryReader(File.OpenRead(path)))
                {
                    if (reader.ReadUInt16() != 0x5A4D) return false;
                    reader.BaseStream.Position = 60;
                    int offset = reader.ReadInt32();
                    reader.BaseStream.Position = offset;
                    return reader.ReadUInt32() == 0x4550 && reader.ReadUInt16() == 0x14C;
                }
            }
            catch { return false; }
        }

        public static string Find()
        {
            string custom = AppPreferences.Current.SdkDirectory;
            if (!string.IsNullOrWhiteSpace(custom) && IsCompatible(custom)) return custom;
            foreach (string programFiles in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) })
                foreach (string relative in new[] { @"Canon\EOS Utility\EU3", @"Canon\EOS Utility" })
                {
                    string folder = Path.Combine(programFiles, relative);
                    if (IsCompatible(folder)) return folder;
                }
            return null;
        }
    }
}
