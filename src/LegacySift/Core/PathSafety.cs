using System;
using System.Collections.Generic;
using System.IO;

namespace LegacySift.Core
{
    internal static class PathSafety
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var full = Path.GetFullPath(path.Trim());
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string ValidatePair(string sourceRoot, string referenceRoot)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot)) return L10n.T("OldRequired");
            if (string.IsNullOrWhiteSpace(referenceRoot)) return L10n.T("CurrentRequired");

            string source;
            string reference;
            try
            {
                source = Normalize(sourceRoot);
                reference = Normalize(referenceRoot);
            }
            catch (Exception ex)
            {
                return L10n.T("InvalidPath", ex.Message);
            }

            if (!Directory.Exists(source)) return L10n.T("OldMissing");
            if (!Directory.Exists(reference)) return L10n.T("CurrentMissing");

            if (StringComparer.OrdinalIgnoreCase.Equals(source, reference))
                return L10n.T("SameFolder");

            if (IsRoot(source)) return L10n.T("OldRootBlocked");
            if (IsRoot(reference)) return L10n.T("CurrentRootBlocked");

            if (IsInside(source, reference) || IsInside(reference, source))
                return L10n.T("NestedBlocked");

            if (IsKnownSystemFolder(source))
                return L10n.T("SystemFolderBlocked");

            return null;
        }

        public static bool IsRoot(string path)
        {
            var normalized = Normalize(path);
            var root = Path.GetPathRoot(normalized)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !string.IsNullOrEmpty(root) && StringComparer.OrdinalIgnoreCase.Equals(normalized, root);
        }

        public static bool IsInside(string child, string parent)
        {
            child = Normalize(child) + Path.DirectorySeparatorChar;
            parent = Normalize(parent) + Path.DirectorySeparatorChar;
            return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsKnownSystemFolder(string path)
        {
            var p = Normalize(path);
            var blocked = new List<string>();
            AddIfNotEmpty(blocked, Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            AddIfNotEmpty(blocked, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            AddIfNotEmpty(blocked, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            AddIfNotEmpty(blocked, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

            foreach (var item in blocked)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(p, Normalize(item))) return true;
            }
            return false;
        }

        private static void AddIfNotEmpty(List<string> list, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) list.Add(value);
        }

        public static string GetRelativePath(string root, string fullPath)
        {
            var r = Normalize(root) + Path.DirectorySeparatorChar;
            var f = Path.GetFullPath(fullPath);
            if (!f.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(L10n.T("OutsideOld"));
            return f.Substring(r.Length);
        }
    }
}
