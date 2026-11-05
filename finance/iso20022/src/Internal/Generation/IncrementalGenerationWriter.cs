using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace iso20022;

internal static class IncrementalGenerationWriter
{
    internal static bool ApplyOutput(GenerationResult result)
    {
        List<GeneratedFile> previousFiles = CreatePreviousOwnedFiles(
            result.Plan.PreviousManifest);
        MarkUnchangedFiles(result.Files, previousFiles);
        string outputRootPath = result.Plan.Options.OutputRootPath;
        if (outputRootPath.Length == 0)
        {
            return true;
        }

        string rootPath = Path.GetFullPath(outputRootPath);
        if (!PreviousRootMatches(result.Plan.PreviousManifest, rootPath))
        {
            return SetIncrementalFailure(
                result,
                "generation.incremental.root_mismatch",
                "The previous manifest belongs to a different output root.");
        }
        if (result.Plan.PreviousManifest != null &&
            result.Plan.PreviousManifest.GeneratorIdentifier.Length > 0 &&
            result.Plan.PreviousManifest.GeneratorIdentifier !=
                result.Manifest.GeneratorIdentifier)
        {
            return SetIncrementalFailure(
                result,
                "generation.incremental.generator_mismatch",
                "The previous manifest was produced by a different generator.");
        }
        if (!ValidateOwnedPaths(result, rootPath, result.Files, previousFiles))
        {
            return false;
        }
        if (!ValidateExistingFiles(result, rootPath, result.Files, previousFiles))
        {
            return false;
        }

        Directory.CreateDirectory(rootPath);
        string stagingPath = Path.Combine(
            rootPath,
            ".iso20022-generation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingPath);
        StageGeneratedFiles(stagingPath, result.Files);
        ReplaceGeneratedFiles(rootPath, stagingPath, result.Files);
        RemoveStaleOwnedFiles(rootPath, result.Files, previousFiles);
        ReplaceManifestFile(rootPath, stagingPath, result.Files);
        Directory.Delete(stagingPath, true);
        return true;
    }

    internal static List<GeneratedFile> CreatePreviousOwnedFiles(
        GenerationManifest? previousManifest)
    {
        List<GeneratedFile> files = new List<GeneratedFile>();
        if (previousManifest == null)
        {
            return files;
        }
        files.AddRange(previousManifest.Files);
        if (previousManifest.Options.EmitManifest)
        {
            string manifestPath = GenerationManifestEmitter.CreateManifestPath(
                previousManifest.GeneratorIdentifier);
            if (FindFileByPath(files, manifestPath) == null)
            {
                byte[] content = GenerationManifestEmitter.CreateManifestContent(
                    previousManifest,
                    true);
                GeneratedFile file = new GeneratedFile();
                file.Kind = GenerationFileKind.Manifest;
                file.RelativePath = manifestPath;
                file.MediaType = Iso20022MediaTypes.Json;
                file.Content = content;
                file.HashAlgorithm = "SHA-256";
                GenerationManifestEmitter.SetBinaryHash(file.Hash, SHA256.HashData(content));
                files.Add(file);
            }
        }
        GenerationManifestEmitter.SortGeneratedFiles(files);
        return files;
    }

    internal static void MarkUnchangedFiles(
        List<GeneratedFile> currentFiles,
        List<GeneratedFile> previousFiles)
    {
        int index = 0;
        while (index < currentFiles.Count)
        {
            GeneratedFile? previous = FindFileByPath(
                previousFiles,
                currentFiles[index].RelativePath);
            currentFiles[index].IsUnchanged = previous != null &&
                HashesMatch(currentFiles[index].Hash.Value, previous.Hash.Value);
            index++;
        }
    }

    internal static bool PreviousRootMatches(
        GenerationManifest? previousManifest,
        string currentRootPath)
    {
        if (previousManifest == null ||
            previousManifest.Options.OutputRootPath.Length == 0)
        {
            return true;
        }
        string previousRootPath = Path.GetFullPath(
            previousManifest.Options.OutputRootPath);
        StringComparison comparison = StringComparison.Ordinal;
        if (OperatingSystem.IsWindows())
        {
            comparison = StringComparison.OrdinalIgnoreCase;
        }
        return string.Equals(previousRootPath, currentRootPath, comparison);
    }

    internal static bool ValidateOwnedPaths(
        GenerationResult result,
        string rootPath,
        List<GeneratedFile> currentFiles,
        List<GeneratedFile> previousFiles)
    {
        int index = 0;
        while (index < currentFiles.Count)
        {
            if (!RelativePathIsSafe(rootPath, currentFiles[index].RelativePath))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.output_path_invalid",
                    "A generated output path escapes or aliases the output root.");
            }
            if (FindDuplicatePath(currentFiles, index))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.output_path_duplicate",
                    "Generated output contains a duplicate relative path.");
            }
            index++;
        }
        index = 0;
        while (index < previousFiles.Count)
        {
            if (!RelativePathIsSafe(rootPath, previousFiles[index].RelativePath))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.owned_path_invalid",
                    "The previous manifest contains an unsafe owned path.");
            }
            if (FindDuplicatePath(previousFiles, index))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.owned_path_duplicate",
                    "The previous manifest contains a duplicate owned path.");
            }
            index++;
        }
        return true;
    }

    internal static bool ValidateExistingFiles(
        GenerationResult result,
        string rootPath,
        List<GeneratedFile> currentFiles,
        List<GeneratedFile> previousFiles)
    {
        if (File.Exists(rootPath))
        {
            return SetIncrementalFailure(
                result,
                "generation.incremental.output_root_invalid",
                "The generation output root is an existing file.");
        }
        int index = 0;
        while (index < currentFiles.Count)
        {
            GeneratedFile current = currentFiles[index];
            string path = ResolveOutputPath(rootPath, current.RelativePath);
            if (PathHasReparsePoint(rootPath, path))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.symlink_path",
                    "A generated output path traverses a symbolic link.");
            }
            if (PathHasFileAncestor(rootPath, path))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.unowned_collision",
                    "A generated output path has an existing file as an ancestor.");
            }
            if (Directory.Exists(path))
            {
                return SetIncrementalFailure(
                    result,
                    "generation.incremental.unowned_collision",
                    "A generated output path collides with an existing directory.");
            }
            if (File.Exists(path))
            {
                GeneratedFile? previous = FindFileByPath(
                    previousFiles,
                    current.RelativePath);
                if (previous == null)
                {
                    return SetIncrementalFailure(
                        result,
                        "generation.incremental.unowned_collision",
                        "A generated output path collides with an unowned file.");
                }
                if (!FileMatchesOwnedHash(path, previous, result.Plan.Options.Limits))
                {
                    return SetIncrementalFailure(
                        result,
                        "generation.incremental.owned_file_changed",
                        "A previously owned file changed after its manifest was created.");
                }
            }
            index++;
        }

        index = 0;
        while (index < previousFiles.Count)
        {
            GeneratedFile previous = previousFiles[index];
            if (FindFileByPath(currentFiles, previous.RelativePath) == null)
            {
                string path = ResolveOutputPath(rootPath, previous.RelativePath);
                if (PathHasReparsePoint(rootPath, path))
                {
                    return SetIncrementalFailure(
                        result,
                        "generation.incremental.symlink_path",
                        "A previously owned path traverses a symbolic link.");
                }
                if (Directory.Exists(path))
                {
                    return SetIncrementalFailure(
                        result,
                        "generation.incremental.owned_path_changed",
                        "A previously owned file path is now a directory.");
                }
                if (File.Exists(path) &&
                    !FileMatchesOwnedHash(path, previous, result.Plan.Options.Limits))
                {
                    return SetIncrementalFailure(
                        result,
                        "generation.incremental.owned_file_changed",
                        "A stale previously owned file changed after its manifest was created.");
                }
            }
            index++;
        }
        return true;
    }

    internal static bool RelativePathIsSafe(string rootPath, string relativePath)
    {
        if (relativePath.Length == 0 || Path.IsPathRooted(relativePath) ||
            relativePath.Contains('\\') || relativePath.Contains(':'))
        {
            return false;
        }
        string[] parts = relativePath.Split('/', StringSplitOptions.None);
        int index = 0;
        while (index < parts.Length)
        {
            if (parts[index].Length == 0 || parts[index] == "." || parts[index] == "..")
            {
                return false;
            }
            index++;
        }
        string resolved = ResolveOutputPath(rootPath, relativePath);
        string rootPrefix = rootPath;
        if (!rootPrefix.EndsWith(Path.DirectorySeparatorChar))
        {
            rootPrefix += Path.DirectorySeparatorChar;
        }
        StringComparison comparison = StringComparison.Ordinal;
        if (OperatingSystem.IsWindows())
        {
            comparison = StringComparison.OrdinalIgnoreCase;
        }
        return resolved.StartsWith(rootPrefix, comparison);
    }

    internal static string ResolveOutputPath(string rootPath, string relativePath)
    {
        string platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(rootPath, platformPath));
    }

    internal static bool PathHasReparsePoint(string rootPath, string outputPath)
    {
        string? parent = Path.GetDirectoryName(outputPath);
        if (parent == null)
        {
            return true;
        }
        string relative = Path.GetRelativePath(rootPath, parent);
        if (relative == ".")
        {
            return false;
        }
        string[] parts = relative.Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);
        string current = rootPath;
        int index = 0;
        while (index < parts.Length)
        {
            current = Path.Combine(current, parts[index]);
            if ((Directory.Exists(current) || File.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool PathHasFileAncestor(string rootPath, string outputPath)
    {
        string? parent = Path.GetDirectoryName(outputPath);
        if (parent == null)
        {
            return true;
        }
        string relative = Path.GetRelativePath(rootPath, parent);
        if (relative == ".")
        {
            return false;
        }
        string[] parts = relative.Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);
        string current = rootPath;
        int index = 0;
        while (index < parts.Length)
        {
            current = Path.Combine(current, parts[index]);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool FileMatchesOwnedHash(
        string path,
        GeneratedFile previous,
        ResourceLimits limits)
    {
        if (previous.Hash.Value.Length == 0 || previous.HashAlgorithm != "SHA-256")
        {
            return false;
        }
        FileInfo info = new FileInfo(path);
        if (info.Length > limits.MaxTotalBytes)
        {
            return false;
        }
        using (FileStream stream = File.OpenRead(path))
        {
            byte[] hash = SHA256.HashData(stream);
            return HashesMatch(hash, previous.Hash.Value);
        }
    }

    internal static bool HashesMatch(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(first, second);
    }

    internal static GeneratedFile? FindFileByPath(
        List<GeneratedFile> files,
        string relativePath)
    {
        int index = 0;
        while (index < files.Count)
        {
            if (files[index].RelativePath == relativePath)
            {
                return files[index];
            }
            index++;
        }
        return null;
    }

    internal static bool FindDuplicatePath(List<GeneratedFile> files, int selectedIndex)
    {
        int index = 0;
        while (index < selectedIndex)
        {
            if (files[index].RelativePath == files[selectedIndex].RelativePath)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void StageGeneratedFiles(
        string stagingPath,
        List<GeneratedFile> files)
    {
        int index = 0;
        while (index < files.Count)
        {
            string path = ResolveOutputPath(stagingPath, files[index].RelativePath);
            string directory = Path.GetDirectoryName(path) ?? stagingPath;
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, files[index].Content);
            index++;
        }
    }

    internal static void ReplaceGeneratedFiles(
        string rootPath,
        string stagingPath,
        List<GeneratedFile> files)
    {
        int index = 0;
        while (index < files.Count)
        {
            string target = ResolveOutputPath(rootPath, files[index].RelativePath);
            if (files[index].Kind != GenerationFileKind.Manifest &&
                (!files[index].IsUnchanged || !File.Exists(target)))
            {
                string source = ResolveOutputPath(stagingPath, files[index].RelativePath);
                string directory = Path.GetDirectoryName(target) ?? rootPath;
                Directory.CreateDirectory(directory);
                File.Move(source, target, true);
            }
            index++;
        }
    }

    internal static void ReplaceManifestFile(
        string rootPath,
        string stagingPath,
        List<GeneratedFile> files)
    {
        int index = 0;
        while (index < files.Count)
        {
            GeneratedFile file = files[index];
            if (file.Kind == GenerationFileKind.Manifest)
            {
                string target = ResolveOutputPath(rootPath, file.RelativePath);
                if (!file.IsUnchanged || !File.Exists(target))
                {
                    string source = ResolveOutputPath(stagingPath, file.RelativePath);
                    string directory = Path.GetDirectoryName(target) ?? rootPath;
                    Directory.CreateDirectory(directory);
                    File.Move(source, target, true);
                }
            }
            index++;
        }
    }

    internal static void RemoveStaleOwnedFiles(
        string rootPath,
        List<GeneratedFile> currentFiles,
        List<GeneratedFile> previousFiles)
    {
        int index = 0;
        while (index < previousFiles.Count)
        {
            if (FindFileByPath(currentFiles, previousFiles[index].RelativePath) == null)
            {
                string path = ResolveOutputPath(rootPath, previousFiles[index].RelativePath);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            index++;
        }
    }

    internal static bool SetIncrementalFailure(
        GenerationResult result,
        string errorCode,
        string errorMessage)
    {
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return false;
    }
}
