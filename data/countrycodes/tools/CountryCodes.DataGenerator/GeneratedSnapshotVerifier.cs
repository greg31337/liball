using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CountryCodes.DataGenerator.Emission;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator;

internal static class GeneratedSnapshotVerifier
{
    private const int RunsPerCulture = 2;

    private static readonly string[] CultureNames =
    {
        "",
        "en-US",
        "tr-TR",
        "ar-SA",
        "ja-JP"
    };

    internal static GeneratedSnapshotVerificationSummary Verify(
        OverrideApplicationResult input,
        SourceManifestInfo manifest,
        string checkedInDirectory)
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "countrycodes-generated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);

        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo? previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo? previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            string? baselineDirectory = null;
            int generationCount = 0;
            for (int cultureIndex = 0; cultureIndex < CultureNames.Length; cultureIndex += 1)
            {
                CultureInfo culture = CreateCulture(CultureNames[cultureIndex]);
                ApplyCulture(culture);
                for (int run = 0; run < RunsPerCulture; run += 1)
                {
                    string outputDirectory = Path.Combine(
                        temporaryRoot,
                        cultureIndex.ToString(CultureInfo.InvariantCulture) + "-" +
                        run.ToString(CultureInfo.InvariantCulture));
                    CountryRowEmitter.Emit(input, manifest, outputDirectory);
                    generationCount += 1;

                    if (baselineDirectory == null)
                    {
                        baselineDirectory = outputDirectory;
                        CompareDirectories(
                            baselineDirectory,
                            checkedInDirectory,
                            "checked-in generated output");
                    }
                    else
                    {
                        string label = CultureLabel(culture) + " run " +
                                       (run + 1).ToString(CultureInfo.InvariantCulture);
                        CompareDirectories(baselineDirectory, outputDirectory, label);
                    }
                }
            }

            if (baselineDirectory == null)
            {
                throw new GeneratedSnapshotVerificationException(
                    "No reproducibility baseline was generated.");
            }

            return new GeneratedSnapshotVerificationSummary(
                CultureNames.Length,
                generationCount,
                GetGeneratedFiles(baselineDirectory).Count);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUiCulture;
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, true);
            }
        }
    }

    private static CultureInfo CreateCulture(string name)
    {
        return name.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static string CultureLabel(CultureInfo culture)
    {
        return culture.Equals(CultureInfo.InvariantCulture) ? "invariant culture" : culture.Name;
    }

    private static void CompareDirectories(
        string expectedDirectory,
        string actualDirectory,
        string actualLabel)
    {
        Dictionary<string, string> expectedFiles = GetGeneratedFiles(expectedDirectory);
        Dictionary<string, string> actualFiles = GetGeneratedFiles(actualDirectory);
        List<string> issues = new List<string>();

        foreach (KeyValuePair<string, string> expected in expectedFiles)
        {
            if (!actualFiles.TryGetValue(expected.Key, out string? actualPath))
            {
                issues.Add($"{actualLabel} is missing '{expected.Key}'.");
                continue;
            }

            if (!FilesEqual(expected.Value, actualPath))
            {
                issues.Add($"{actualLabel} differs in '{expected.Key}'.");
            }
        }

        foreach (string actualName in actualFiles.Keys)
        {
            if (!expectedFiles.ContainsKey(actualName))
            {
                issues.Add($"{actualLabel} contains unexpected file '{actualName}'.");
            }
        }

        if (issues.Count != 0)
        {
            issues.Sort(StringComparer.Ordinal);
            throw new GeneratedSnapshotVerificationException(string.Join(" ", issues));
        }
    }

    private static Dictionary<string, string> GetGeneratedFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new GeneratedSnapshotVerificationException(
                $"Generated output directory does not exist: '{directory}'.");
        }

        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(directory, "*.g.cs", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(path);
            if (!result.TryAdd(name, path))
            {
                throw new GeneratedSnapshotVerificationException(
                    $"Generated output repeats file name '{name}'.");
            }
        }

        return result;
    }

    private static bool FilesEqual(string leftPath, string rightPath)
    {
        FileInfo leftInfo = new FileInfo(leftPath);
        FileInfo rightInfo = new FileInfo(rightPath);
        if (leftInfo.Length != rightInfo.Length)
        {
            return false;
        }

        using FileStream left = File.OpenRead(leftPath);
        using FileStream right = File.OpenRead(rightPath);
        byte[] leftBuffer = new byte[64 * 1024];
        byte[] rightBuffer = new byte[64 * 1024];
        while (true)
        {
            int leftRead = left.Read(leftBuffer, 0, leftBuffer.Length);
            int rightRead = right.Read(rightBuffer, 0, rightBuffer.Length);
            if (leftRead != rightRead)
            {
                return false;
            }

            if (leftRead == 0)
            {
                return true;
            }

            for (int index = 0; index < leftRead; index += 1)
            {
                if (leftBuffer[index] != rightBuffer[index])
                {
                    return false;
                }
            }
        }
    }
}

internal sealed class GeneratedSnapshotVerificationSummary
{
    internal GeneratedSnapshotVerificationSummary(
        int cultureCount,
        int generationCount,
        int fileCount)
    {
        CultureCount = cultureCount;
        GenerationCount = generationCount;
        FileCount = fileCount;
    }

    internal int CultureCount { get; }

    internal int GenerationCount { get; }

    internal int FileCount { get; }
}

internal sealed class GeneratedSnapshotVerificationException : Exception
{
    internal GeneratedSnapshotVerificationException(string message)
        : base(message)
    {
    }
}
