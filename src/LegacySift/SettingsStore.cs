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
                return ParseLanguageSetting(File.ReadAllLines(SettingsPath), L10n.DetectDefaultLanguage());
            }
            catch { }
            return L10n.DetectDefaultLanguage();
        }

        public static void SaveLanguage(AppLanguage language)
        {
            try
            {
                // Rewriting only the supported language preference also
                // removes obsolete theme=* lines from development builds.
                File.WriteAllText(SettingsPath, SerializeLanguageSetting(language));
            }
            catch { }
        }

        internal static AppLanguage ParseLanguageSetting(string[] lines, AppLanguage fallback)
        {
            if (lines == null) return fallback;
            foreach (var line in lines)
            {
                if (line == null || !line.StartsWith("language=", StringComparison.OrdinalIgnoreCase)) continue;
                return L10n.FromCode(line.Substring("language=".Length));
            }
            return fallback;
        }

        internal static string SerializeLanguageSetting(AppLanguage language)
        {
            return "language=" + L10n.ToCode(language) + "\r\n";
        }

    }
}
