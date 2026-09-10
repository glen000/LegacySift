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
                    if (line.Equals("language=it", StringComparison.OrdinalIgnoreCase)) return AppLanguage.Italian;
                    if (line.Equals("language=en", StringComparison.OrdinalIgnoreCase)) return AppLanguage.English;
                }
            }
            catch { }
            return L10n.DetectDefaultLanguage();
        }

        public static void SaveLanguage(AppLanguage language)
        {
            try
            {
                File.WriteAllText(SettingsPath, language == AppLanguage.Italian ? "language=it\r\n" : "language=en\r\n");
            }
            catch { }
        }
    }
}
