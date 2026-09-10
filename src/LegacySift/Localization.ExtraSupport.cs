using System;
using System.Collections.Generic;

namespace LegacySift
{
    internal static partial class L10n
    {
        private static Dictionary<string, string> Make(params string[] pairs)
        {
            if (pairs == null || pairs.Length % 2 != 0)
                throw new ArgumentException("Translation pairs must contain key/value pairs.");

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < pairs.Length; i += 2)
                result[pairs[i]] = pairs[i + 1];
            return result;
        }

        private static Dictionary<string, string> GetDictionary(AppLanguage language)
        {
            switch (language)
            {
                case AppLanguage.Italian: return It;
                case AppLanguage.German: return De;
                case AppLanguage.French: return Fr;
                case AppLanguage.Spanish: return Es;
                case AppLanguage.Portuguese: return Pt;
                case AppLanguage.Polish: return Pl;
                case AppLanguage.Dutch: return Nl;
                case AppLanguage.Turkish: return Tr;
                case AppLanguage.Russian: return Ru;
                case AppLanguage.Ukrainian: return Uk;
                case AppLanguage.ChineseSimplified: return ZhHans;
                case AppLanguage.Japanese: return Ja;
                case AppLanguage.Hindi: return Hi;
                default: return En;
            }
        }
    }
}
