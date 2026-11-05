using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using LanguageCodes.DataGenerator.Emission;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class VerifyGenerationResult
{
    internal VerifyGenerationResult(int sourceCount, int generatedFileCount)
    {
        SourceCount = sourceCount;
        GeneratedFileCount = generatedFileCount;
    }

    internal int SourceCount { get; }

    internal int GeneratedFileCount { get; }
}

internal static class VerifyGenerationCommand
{
    internal static VerifyGenerationResult Execute(
        string manifestPath,
        string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        string manifestFullPath = Path.GetFullPath(manifestPath);
        string outputFullPath = Path.GetFullPath(outputDirectory);
        if (!File.Exists(manifestFullPath))
        {
            throw new FileNotFoundException("The pinned data manifest was not found.", manifestFullPath);
        }

        if (!Directory.Exists(outputFullPath))
        {
            throw new DirectoryNotFoundException(
                $"The generated output directory does not exist: {outputFullPath}");
        }

        DataManifest manifest = DataManifestReader.Read(manifestFullPath);
        string sourceRoot = GeneratorInputLoader.FindSourceRoot(manifestFullPath, manifest);
        ValidatePinnedSources(manifest, sourceRoot);
        ValidatedGeneratorData data = GeneratorInputLoader.LoadAndValidate(manifest, sourceRoot);
        GeneratedSourceFile[] sourceFiles = RuntimeTableGenerator.CreateFiles(
            data.IsoData,
            data.Names,
            data.Mappings,
            data.Retirements,
            data.Hierarchy,
            data.Registry,
            data.Aliases,
            manifest);
        RenderedSourceFile[] renderedFiles = DeterministicSourceEmitter.Render(sourceFiles);
        string[] differences = FindDifferences(renderedFiles, outputFullPath);
        if (differences.Length > 0)
        {
            throw new InvalidDataException(
                "Generated output verification failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, differences));
        }

        return new VerifyGenerationResult(manifest.Sources.Length, renderedFiles.Length);
    }

    private static void ValidatePinnedSources(DataManifest manifest, string sourceRoot)
    {
        string rootPrefix = EnsureDirectoryPrefix(sourceRoot);
        List<string> violations = new List<string>();
        foreach (DataManifestSource source in manifest.Sources)
        {
            string path = Path.GetFullPath(
                Path.Combine(
                    sourceRoot,
                    source.Path.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(rootPrefix, StringComparison.Ordinal) || !File.Exists(path))
            {
                violations.Add($"Pinned source '{source.Id}' is missing or escapes the source root.");
                continue;
            }

            byte[] bytes = File.ReadAllBytes(path);
            string actualHash = Sha256(bytes);
            if (bytes.LongLength != source.ByteLength || actualHash != source.Sha256)
            {
                violations.Add(
                    $"Pinned source '{source.Id}' does not match manifest checksum/length " +
                    $"(expected {source.Sha256}/{source.ByteLength}, " +
                    $"actual {actualHash}/{bytes.LongLength}).");
                continue;
            }

            try
            {
                StagingFetchCommand.ValidateContent(source, bytes);
            }
            catch (InvalidDataException exception)
            {
                violations.Add(exception.Message);
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidDataException(
                "Pinned input verification failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, violations));
        }
    }

    private static string[] FindDifferences(
        RenderedSourceFile[] renderedFiles,
        string outputRoot)
    {
        string outputPrefix = EnsureDirectoryPrefix(outputRoot);
        HashSet<string> expectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> generatedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> differences = new List<string>();
        foreach (RenderedSourceFile renderedFile in renderedFiles)
        {
            string path = Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    renderedFile.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(outputPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Generated path '{renderedFile.RelativePath}' escapes its output directory.");
            }

            expectedPaths.Add(path);
            string? directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                generatedDirectories.Add(directory);
            }

            if (!File.Exists(path))
            {
                differences.Add($"Missing generated file: {renderedFile.RelativePath}");
                continue;
            }

            byte[] actualBytes = File.ReadAllBytes(path);
            if (!BytesEqual(actualBytes, renderedFile.Bytes))
            {
                differences.Add(
                    $"Generated file differs: {renderedFile.RelativePath} " +
                    $"(expected {renderedFile.Sha256}, actual {Sha256(actualBytes)}).");
            }
        }

        foreach (string directory in generatedDirectories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string path in Directory.GetFiles(directory, "Generated*.csx"))
            {
                string fullPath = Path.GetFullPath(path);
                if (!expectedPaths.Contains(fullPath))
                {
                    differences.Add(
                        "Unexpected generated file: " + Path.GetRelativePath(outputRoot, fullPath));
                }
            }
        }

        differences.Sort(StringComparer.Ordinal);
        return differences.ToArray();
    }

    private static string EnsureDirectoryPrefix(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }
}
