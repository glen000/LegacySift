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
        Ukrainian,
        ChineseSimplified,
        Japanese,
        Hindi,
        Romanian,
        Czech,
        Greek,
        Hungarian,
        Swedish,
        Korean,
        Indonesian,
        Vietnamese,
        Danish,
        NorwegianBokmal,
        Finnish,
        Slovak,
        Bulgarian,
        Croatian,
        Bengali,
        Thai,
        Malay,
        Filipino,
        Estonian,
        Latvian,
        Lithuanian
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
            // Language recovery controls deliberately stay bilingual and script-independent.
            // A user who accidentally chooses an unfamiliar language must still be able to find the way back.
            if (string.Equals(key, "LanguageButton", StringComparison.Ordinal))
                return "Language / Lingua…";
            if (string.Equals(key, "ThemeButton", StringComparison.Ordinal))
                return "Theme / Tema…";
            if (string.Equals(key, "LanguageRestartTitle", StringComparison.Ordinal))
                return "Language / Lingua";
            if (string.Equals(key, "LanguageRestart", StringComparison.Ordinal))
                return "LegacySift will restart to apply the selected language. Current results will be cleared.\n\nLegacySift verrà riavviato per applicare la lingua scelta. I risultati attuali verranno azzerati.";

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
            return DetectLanguage(CultureInfo.CurrentUICulture);
        }

        internal static AppLanguage DetectLanguage(CultureInfo uiCulture)
        {
            if (uiCulture == null) return AppLanguage.English;
            var name = (uiCulture.Name ?? string.Empty).ToLowerInvariant();
            var two = (uiCulture.TwoLetterISOLanguageName ?? string.Empty).ToLowerInvariant();

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
                case "uk": return AppLanguage.Ukrainian;
                case "zh": return AppLanguage.ChineseSimplified;
                case "ja": return AppLanguage.Japanese;
                case "hi": return AppLanguage.Hindi;
                case "ro": return AppLanguage.Romanian;
                case "cs": return AppLanguage.Czech;
                case "el": return AppLanguage.Greek;
                case "hu": return AppLanguage.Hungarian;
                case "sv": return AppLanguage.Swedish;
                case "ko": return AppLanguage.Korean;
                case "id": return AppLanguage.Indonesian;
                case "vi": return AppLanguage.Vietnamese;
                case "da": return AppLanguage.Danish;
                case "nb":
                case "no":
                case "nn": return AppLanguage.NorwegianBokmal;
                case "fi": return AppLanguage.Finnish;
                case "sk": return AppLanguage.Slovak;
                case "bg": return AppLanguage.Bulgarian;
                case "hr": return AppLanguage.Croatian;
                case "bn": return AppLanguage.Bengali;
                case "th": return AppLanguage.Thai;
                case "ms": return AppLanguage.Malay;
                case "fil": return AppLanguage.Filipino;
                case "et": return AppLanguage.Estonian;
                case "lv": return AppLanguage.Latvian;
                case "lt": return AppLanguage.Lithuanian;
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
                case AppLanguage.Ukrainian: return "uk";
                case AppLanguage.ChineseSimplified: return "zh-Hans";
                case AppLanguage.Japanese: return "ja";
                case AppLanguage.Hindi: return "hi";
                case AppLanguage.Romanian: return "ro";
                case AppLanguage.Czech: return "cs";
                case AppLanguage.Greek: return "el";
                case AppLanguage.Hungarian: return "hu";
                case AppLanguage.Swedish: return "sv";
                case AppLanguage.Korean: return "ko";
                case AppLanguage.Indonesian: return "id";
                case AppLanguage.Vietnamese: return "vi";
                case AppLanguage.Danish: return "da";
                case AppLanguage.NorwegianBokmal: return "nb";
                case AppLanguage.Finnish: return "fi";
                case AppLanguage.Slovak: return "sk";
                case AppLanguage.Bulgarian: return "bg";
                case AppLanguage.Croatian: return "hr";
                case AppLanguage.Bengali: return "bn";
                case AppLanguage.Thai: return "th";
                case AppLanguage.Malay: return "ms";
                case AppLanguage.Filipino: return "fil";
                case AppLanguage.Estonian: return "et";
                case AppLanguage.Latvian: return "lv";
                case AppLanguage.Lithuanian: return "lt";
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
                case "uk": return AppLanguage.Ukrainian;
                case "zh":
                case "zh-cn":
                case "zh-hans": return AppLanguage.ChineseSimplified;
                case "ja": return AppLanguage.Japanese;
                case "hi": return AppLanguage.Hindi;
                case "ro":
                case "ro-ro": return AppLanguage.Romanian;
                case "cs":
                case "cs-cz": return AppLanguage.Czech;
                case "el":
                case "el-gr": return AppLanguage.Greek;
                case "hu":
                case "hu-hu": return AppLanguage.Hungarian;
                case "sv":
                case "sv-se": return AppLanguage.Swedish;
                case "ko":
                case "ko-kr": return AppLanguage.Korean;
                case "id":
                case "id-id":
                case "in": return AppLanguage.Indonesian;
                case "vi":
                case "vi-vn": return AppLanguage.Vietnamese;
                case "da":
                case "da-dk": return AppLanguage.Danish;
                case "nb":
                case "nb-no":
                case "no":
                case "no-no":
                case "nn":
                case "nn-no": return AppLanguage.NorwegianBokmal;
                case "fi":
                case "fi-fi": return AppLanguage.Finnish;
                case "sk":
                case "sk-sk": return AppLanguage.Slovak;
                case "bg":
                case "bg-bg": return AppLanguage.Bulgarian;
                case "hr":
                case "hr-hr": return AppLanguage.Croatian;
                case "bn":
                case "bn-bd":
                case "bn-in": return AppLanguage.Bengali;
                case "th":
                case "th-th": return AppLanguage.Thai;
                case "ms":
                case "ms-my": return AppLanguage.Malay;
                case "fil":
                case "fil-ph": return AppLanguage.Filipino;
                case "et":
                case "et-ee": return AppLanguage.Estonian;
                case "lv":
                case "lv-lv": return AppLanguage.Latvian;
                case "lt":
                case "lt-lt": return AppLanguage.Lithuanian;
                default: return AppLanguage.English;
            }
        }

        internal static IDictionary<string, string> TranslationForTests(AppLanguage language)
        {
            return GetDictionary(language);
        }
    }
}
