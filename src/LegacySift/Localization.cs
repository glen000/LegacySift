using System;
using System.Collections.Generic;
using System.Globalization;

namespace LegacySift
{
    internal enum AppLanguage
    {
        English,
        Italian,
        German,
        French,
        Spanish,
        Portuguese,
        Polish,
        Dutch,
        Turkish,
        Russian,
        Ukrainian,
        ChineseSimplified,
        Japanese,
        Hindi
    }

    internal static partial class L10n
    {
        public static AppLanguage CurrentLanguage { get; private set; } = DetectDefaultLanguage();

        public static void SetLanguage(AppLanguage language)
        {
            CurrentLanguage = language;
        }

        public static string T(string key, params object[] args)
        {
            string value;
            var dict = GetDictionary(CurrentLanguage);
            if (!dict.TryGetValue(key, out value))
            {
                if (!En.TryGetValue(key, out value)) value = key;
            }
            return args != null && args.Length > 0
                ? string.Format(CultureInfo.CurrentCulture, value, args)
                : value;
        }

        public static AppLanguage DetectDefaultLanguage()
        {
            var name = (CultureInfo.CurrentUICulture.Name ?? string.Empty).ToLowerInvariant();
            var two = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName ?? string.Empty).ToLowerInvariant();

            switch (two)
            {
                case "it": return AppLanguage.Italian;
                case "de": return AppLanguage.German;
                case "fr": return AppLanguage.French;
                case "es": return AppLanguage.Spanish;
                case "pt": return AppLanguage.Portuguese;
                case "pl": return AppLanguage.Polish;
                case "nl": return AppLanguage.Dutch;
                case "tr": return AppLanguage.Turkish;
                case "ru": return AppLanguage.Russian;
                case "uk": return AppLanguage.Ukrainian;
                case "zh": return AppLanguage.ChineseSimplified;
                case "ja": return AppLanguage.Japanese;
                case "hi": return AppLanguage.Hindi;
                default:
                    if (name.StartsWith("zh-", StringComparison.Ordinal)) return AppLanguage.ChineseSimplified;
                    return AppLanguage.English;
            }
        }

        public static string ToCode(AppLanguage language)
        {
            switch (language)
            {
                case AppLanguage.Italian: return "it";
                case AppLanguage.German: return "de";
                case AppLanguage.French: return "fr";
                case AppLanguage.Spanish: return "es";
                case AppLanguage.Portuguese: return "pt";
                case AppLanguage.Polish: return "pl";
                case AppLanguage.Dutch: return "nl";
                case AppLanguage.Turkish: return "tr";
                case AppLanguage.Russian: return "ru";
                case AppLanguage.Ukrainian: return "uk";
                case AppLanguage.ChineseSimplified: return "zh-Hans";
                case AppLanguage.Japanese: return "ja";
                case AppLanguage.Hindi: return "hi";
                default: return "en";
            }
        }

        public static AppLanguage FromCode(string code)
        {
            var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "it": return AppLanguage.Italian;
                case "de": return AppLanguage.German;
                case "fr": return AppLanguage.French;
                case "es": return AppLanguage.Spanish;
                case "pt": return AppLanguage.Portuguese;
                case "pl": return AppLanguage.Polish;
                case "nl": return AppLanguage.Dutch;
                case "tr": return AppLanguage.Turkish;
                case "ru": return AppLanguage.Russian;
                case "uk": return AppLanguage.Ukrainian;
                case "zh":
                case "zh-cn":
                case "zh-hans": return AppLanguage.ChineseSimplified;
                case "ja": return AppLanguage.Japanese;
                case "hi": return AppLanguage.Hindi;
                default: return AppLanguage.English;
            }
        }

        internal static IDictionary<string, string> TranslationForTests(AppLanguage language)
        {
            return GetDictionary(language);
        }
    }
}
