using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace fix;

internal static class GenerationManifest
{
    internal static void ApplyManifest(List<GeneratedFile> files, ModelGenerationOptions options, List<GenerationDiagnostic> diagnostics)
    {
        Dictionary<string, string> previous = options.DetectUnchangedFiles ? ParsePreviousManifest(options.PreviousManifest, diagnostics) : new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < files.Count; index++)
        {
            GeneratedFile file = files[index];
            file.ContentHash = ComputeContentHash(file.Content);
            string? priorHash;
            file.IsUnchanged = options.DetectUnchangedFiles && previous.TryGetValue(file.Path, out priorHash) && string.Equals(file.ContentHash, priorHash, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static string CreateManifest(List<GeneratedFile> files)
    {
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < files.Count; index++)
        {
            if (index != 0) builder.Append('\n');
            builder.Append(files[index].ContentHash).Append(' ').Append(files[index].Path);
        }
        return builder.ToString();
    }

    internal static string ComputeContentHash(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static Dictionary<string, string> ParsePreviousManifest(string manifest, List<GenerationDiagnostic> diagnostics)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        string value = manifest == null ? "" : manifest.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string[] lines = value.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line.Length == 0) continue;
            int separator = line.IndexOf(' ');
            if (separator != 64 || line.Length <= 65 || !IsManifestHash(line.Substring(0, separator)))
            {
                AddManifestDiagnostic(diagnostics, ValidationSeverity.Warning, "invalid-previous-manifest-line", "A prior manifest line was ignored because it is malformed.", "/manifest/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }
            string hash = line.Substring(0, separator).ToLowerInvariant();
            string path = line.Substring(separator + 1).Trim();
            if (path.Length == 0)
            {
                AddManifestDiagnostic(diagnostics, ValidationSeverity.Warning, "invalid-previous-manifest-line", "A prior manifest line was ignored because it has no path.", "/manifest/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }
            if (result.ContainsKey(path))
            {
                AddManifestDiagnostic(diagnostics, ValidationSeverity.Warning, "duplicate-previous-manifest-path", "A duplicate prior manifest path was ignored.", path);
                continue;
            }
            result.Add(path, hash);
        }
        return result;
    }

    internal static bool IsManifestHash(string value)
    {
        if (value.Length != 64) return false;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (!(character >= '0' && character <= '9' || character >= 'a' && character <= 'f' || character >= 'A' && character <= 'F')) return false;
        }
        return true;
    }

    internal static void AddManifestDiagnostic(List<GenerationDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }
}
