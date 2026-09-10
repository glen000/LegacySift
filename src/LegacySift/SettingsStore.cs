using System;
using System.IO;

namespace LegacySift
{
    internal static class SettingsStore
    {
        private static string SettingsPath
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LegacySift");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "settings.ini");
            }
        }

        public static AppLanguage LoadLanguage()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return L10n.DetectDefaultLanguage();
                foreach (var line in File.ReadAllLines(SettingsPath))
                {
                    if (!line.StartsWith("language=", StringComparison.OrdinalIgnoreCase)) continue;
                    return L10n.FromCode(line.Substring("language=".Length));
                }
            }
            catch { }
            return L10n.DetectDefaultLanguage();
        }

        public static void SaveLanguage(AppLanguage language)
        {
            try
            {
                File.WriteAllText(SettingsPath, "language=" + L10n.ToCode(language) + "\r\n");
            }
            catch { }
        }
    }
}
