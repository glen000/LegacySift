using System;
using System.IO;
using System.Text;

namespace LegacySift.Core
{
    internal static class ReportWriter
    {
        public static string WriteAnalysisReport(AnalysisResult result)
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LegacySift", "Reports");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "Analysis_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("Kind;SourceRelativePath;SourceFullPath;SizeBytes;ReferencePath;SHA256;Note");
                foreach (var item in result.Items)
                {
                    writer.WriteLine(
                        Csv(item.Kind.ToString()) + ";" +
                        Csv(item.Source?.RelativePath) + ";" +
                        Csv(item.Source?.FullPath) + ";" +
                        Csv(item.Source == null ? string.Empty : item.Source.Length.ToString()) + ";" +
                        Csv(item.ReferencePath) + ";" +
                        Csv(item.Sha256) + ";" +
                        Csv(item.Note));
                }
                foreach (var error in result.ScanErrors)
                {
                    writer.WriteLine(Csv("ScanError") + ";;" + Csv(error.Path) + ";;;;" + Csv(error.Message));
                }
            }
            return path;
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
