using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LanguageCodes.DataGenerator.Emission;

internal static class DeterministicSourceEmitter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false, true);

    internal static RenderedSourceFile[] Render(GeneratedSourceFile[] sourceFiles)
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);

        GeneratedSourceFile[] orderedFiles = new GeneratedSourceFile[sourceFiles.Length];
        Array.Copy(sourceFiles, orderedFiles, sourceFiles.Length);
        Array.Sort(
            orderedFiles,
            delegate (GeneratedSourceFile left, GeneratedSourceFile right)
            {
                return string.CompareOrdinal(left.RelativePath, right.RelativePath);
            });

        List<RenderedSourceFile> renderedFiles = new List<RenderedSourceFile>(orderedFiles.Length);
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedSourceFile sourceFile in orderedFiles)
        {
            ArgumentNullException.ThrowIfNull(sourceFile);
            ValidateRelativePath(sourceFile.RelativePath);
            if (!paths.Add(sourceFile.RelativePath))
            {
                throw new InvalidDataException($"Generated source path '{sourceFile.RelativePath}' is duplicated.");
            }

            string content = NormalizeLineEndings(sourceFile.Content);
            byte[] bytes = Utf8WithoutBom.GetBytes(content);
            byte[] hash = SHA256.HashData(bytes);
            string sha256 = Convert.ToHexString(hash).ToLowerInvariant();
            renderedFiles.Add(new RenderedSourceFile(sourceFile.RelativePath, bytes, sha256));
        }

        return renderedFiles.ToArray();
    }

    internal static EmissionFileResult[] Write(string outputDirectory, GeneratedSourceFile[] sourceFiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        RenderedSourceFile[] renderedFiles = Render(sourceFiles);
        string outputRoot = Path.GetFullPath(outputDirectory);
        string outputRootPrefix = outputRoot.EndsWith(Path.DirectorySeparatorChar)
            ? outputRoot
            : outputRoot + Path.DirectorySeparatorChar;
        List<EmissionFileResult> results = new List<EmissionFileResult>(renderedFiles.Length);

        foreach (RenderedSourceFile renderedFile in renderedFiles)
        {
            string relativePath = renderedFile.RelativePath.Replace('/', Path.DirectorySeparatorChar);
            string targetPath = Path.GetFullPath(Path.Combine(outputRoot, relativePath));
            if (!targetPath.StartsWith(outputRootPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Generated source path '{renderedFile.RelativePath}' escapes the output directory.");
            }

            bool changed = !File.Exists(targetPath) ||
                !BytesEqual(File.ReadAllBytes(targetPath), renderedFile.Bytes);
            if (changed)
            {
                string? parentDirectory = Path.GetDirectoryName(targetPath);
                if (parentDirectory == null)
                {
                    throw new InvalidDataException(
                        $"Generated source path '{renderedFile.RelativePath}' has no parent directory.");
                }

                Directory.CreateDirectory(parentDirectory);
                WriteAtomically(targetPath, renderedFile.Bytes);
            }

            results.Add(new EmissionFileResult(
                targetPath,
                renderedFile.Sha256,
                renderedFile.Bytes.Length,
                changed));
        }

        return results.ToArray();
    }

    private static string NormalizeLineEndings(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
        {
            throw new InvalidDataException("Generated source content cannot be empty.");
        }

        if (content[0] == '\ufeff')
        {
            throw new InvalidDataException("Generated source content cannot contain a byte-order mark.");
        }

        StringBuilder normalized = new StringBuilder(content.Length + 1);
        for (int index = 0; index < content.Length; index++)
        {
            char character = content[index];
            if (character == '\r')
            {
                if (index + 1 < content.Length && content[index + 1] == '\n')
                {
                    index++;
                }

                normalized.Append('\n');
            }
            else
            {
                normalized.Append(character);
            }
        }

        if (normalized[normalized.Length - 1] != '\n')
        {
            normalized.Append('\n');
        }

        return normalized.ToString();
    }

    private static void ValidateRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath) || relativePath.Contains('\\', StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Generated source path '{relativePath}' must be a portable relative path.");
        }

        string[] segments = relativePath.Split('/');
        foreach (string segment in segments)
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                throw new InvalidDataException(
                    $"Generated source path '{relativePath}' contains an invalid segment.");
            }
        }

        if (!relativePath.EndsWith(".cs", StringComparison.Ordinal) &&
            !relativePath.EndsWith(".csx", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Generated source path '{relativePath}' must name a C# source file.");
        }
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

    private static void WriteAtomically(string targetPath, byte[] bytes)
    {
        string temporaryPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, targetPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
