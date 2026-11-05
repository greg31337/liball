using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using LanguageCodes.DataGenerator.Emission;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class AcceptUpdatesResult
{
    internal AcceptUpdatesResult(
        int sourceCount,
        int changedSourceCount,
        int generatedFileCount,
        int changedGeneratedFileCount,
        int changedFileCount)
    {
        SourceCount = sourceCount;
        ChangedSourceCount = changedSourceCount;
        GeneratedFileCount = generatedFileCount;
        ChangedGeneratedFileCount = changedGeneratedFileCount;
        ChangedFileCount = changedFileCount;
    }

    internal int SourceCount { get; }

    internal int ChangedSourceCount { get; }

    internal int GeneratedFileCount { get; }

    internal int ChangedGeneratedFileCount { get; }

    internal int ChangedFileCount { get; }
}

internal sealed class AcceptedSource
{
    internal AcceptedSource(
        DataManifestSource source,
        StagingFetchSourceMetadata metadata,
        byte[] bytes)
    {
        Source = source;
        Metadata = metadata;
        Bytes = bytes;
    }

    internal DataManifestSource Source { get; }

    internal StagingFetchSourceMetadata Metadata { get; }

    internal byte[] Bytes { get; }
}

internal sealed class PreparedAcceptanceFile
{
    internal PreparedAcceptanceFile(string targetPath, byte[] bytes)
    {
        TargetPath = targetPath;
        Bytes = bytes;
    }

    internal string TargetPath { get; }

    internal byte[] Bytes { get; }
}

internal sealed class AcceptanceBackup
{
    internal AcceptanceBackup(string targetPath, byte[]? bytes)
    {
        TargetPath = targetPath;
        Bytes = bytes;
    }

    internal string TargetPath { get; }

    internal byte[]? Bytes { get; }
}

internal static class AcceptUpdatesCommand
{
    internal static AcceptUpdatesResult Execute(
        string manifestPath,
        string stagingPath,
        string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        string manifestFullPath = Path.GetFullPath(manifestPath);
        string stagingFullPath = Path.GetFullPath(stagingPath);
        string outputFullPath = Path.GetFullPath(outputDirectory);
        if (!File.Exists(manifestFullPath))
        {
            throw new FileNotFoundException("The pinned data manifest was not found.", manifestFullPath);
        }

        if (!Directory.Exists(stagingFullPath))
        {
            throw new DirectoryNotFoundException(
                $"The staging directory does not exist: {stagingFullPath}");
        }

        if (!Directory.Exists(outputFullPath))
        {
            throw new DirectoryNotFoundException(
                $"The generated output directory does not exist: {outputFullPath}");
        }

        DataManifest pinnedManifest = DataManifestReader.Read(manifestFullPath);
        string sourceRoot = GeneratorInputLoader.FindSourceRoot(manifestFullPath, pinnedManifest);
        string stagedManifestPath = Path.Combine(stagingFullPath, "manifest.json");
        if (!File.Exists(stagedManifestPath) ||
            !BytesEqual(File.ReadAllBytes(manifestFullPath), File.ReadAllBytes(stagedManifestPath)))
        {
            throw new InvalidDataException(
                "The staged manifest snapshot is missing or no longer matches the pinned manifest.");
        }

        string metadataPath = Path.Combine(stagingFullPath, "fetch-metadata.json");
        if (!File.Exists(metadataPath))
        {
            throw new FileNotFoundException("The staging fetch metadata was not found.", metadataPath);
        }

        StagingFetchMetadata metadata = StagingFetchMetadataReader.Read(metadataPath);
        AcceptedSource[] acceptedSources = ValidateSources(
            pinnedManifest,
            metadata,
            sourceRoot,
            stagingFullPath);
        int changedSourceCount = CountChangedSources(acceptedSources);
        string stagedFilesRoot = Path.Combine(stagingFullPath, "files");
        ValidatedGeneratorData generatorData = GeneratorInputLoader.LoadAndValidate(
            pinnedManifest,
            stagedFilesRoot);
        DataManifest acceptedManifest = changedSourceCount == 0
            ? pinnedManifest
            : CreateAcceptedManifest(
                pinnedManifest,
                metadata.CompletedAt,
                acceptedSources,
                generatorData.Registry.FileDate);
        byte[] acceptedManifestBytes = changedSourceCount == 0
            ? File.ReadAllBytes(manifestFullPath)
            : DataManifestWriter.Render(acceptedManifest);

        GeneratedSourceFile[] generatedSourceFiles = RuntimeTableGenerator.CreateFiles(
            generatorData.IsoData,
            generatorData.Names,
            generatorData.Mappings,
            generatorData.Retirements,
            generatorData.Hierarchy,
            generatorData.Registry,
            generatorData.Aliases,
            acceptedManifest);
        RenderedSourceFile[] renderedFiles = DeterministicSourceEmitter.Render(generatedSourceFiles);
        List<PreparedAcceptanceFile> preparedFiles = PrepareFiles(
            acceptedSources,
            sourceRoot,
            manifestFullPath,
            acceptedManifestBytes,
            renderedFiles,
            outputFullPath);
        int generatedChangeCount = CountGeneratedChanges(renderedFiles, outputFullPath);
        int changedFileCount = Commit(preparedFiles);

        return new AcceptUpdatesResult(
            acceptedSources.Length,
            changedSourceCount,
            renderedFiles.Length,
            generatedChangeCount,
            changedFileCount);
    }

    private static int CountChangedSources(AcceptedSource[] acceptedSources)
    {
        int count = 0;
        foreach (AcceptedSource acceptedSource in acceptedSources)
        {
            if (acceptedSource.Metadata.Changed)
            {
                count++;
            }
        }

        return count;
    }

    private static AcceptedSource[] ValidateSources(
        DataManifest manifest,
        StagingFetchMetadata metadata,
        string sourceRoot,
        string stagingRoot)
    {
        if (metadata.Sources.Length != manifest.Sources.Length)
        {
            throw new InvalidDataException(
                "The staging fetch metadata does not contain every manifest source.");
        }

        Dictionary<string, StagingFetchSourceMetadata> metadataById =
            new Dictionary<string, StagingFetchSourceMetadata>(StringComparer.Ordinal);
        foreach (StagingFetchSourceMetadata sourceMetadata in metadata.Sources)
        {
            metadataById.Add(sourceMetadata.Id, sourceMetadata);
        }

        List<AcceptedSource> accepted = new List<AcceptedSource>();
        foreach (DataManifestSource source in manifest.Sources)
        {
            if (!metadataById.TryGetValue(source.Id, out StagingFetchSourceMetadata? sourceMetadata))
            {
                throw new InvalidDataException(
                    $"Staging fetch metadata for source '{source.Id}' is missing.");
            }

            ValidateSourceMetadata(source, sourceMetadata, metadata.CompletedAt);
            string pinnedPath = ResolveManifestPath(sourceRoot, source.Path);
            byte[] pinnedBytes = File.ReadAllBytes(pinnedPath);
            ValidateHashAndLength(
                source.Id,
                "pinned",
                pinnedBytes,
                source.Sha256,
                source.ByteLength);

            string expectedStagedPath = "files/" + source.Path;
            string stagedPath = ResolveStagedPath(
                stagingRoot,
                sourceMetadata.StagedPath,
                expectedStagedPath);
            byte[] stagedBytes = File.ReadAllBytes(stagedPath);
            ValidateHashAndLength(
                source.Id,
                "staged",
                stagedBytes,
                sourceMetadata.Sha256,
                sourceMetadata.ByteLength);
            StagingFetchCommand.ValidateContent(source, stagedBytes);
            accepted.Add(new AcceptedSource(source, sourceMetadata, stagedBytes));
        }

        return accepted.ToArray();
    }

    private static void ValidateSourceMetadata(
        DataManifestSource source,
        StagingFetchSourceMetadata metadata,
        DateTimeOffset completedAt)
    {
        string expectedStagedPath = "files/" + source.Path;
        bool changed = metadata.Sha256 != source.Sha256;
        if (metadata.RequestedUrl != source.Url ||
            metadata.ManifestPath != source.Path ||
            metadata.StagedPath != expectedStagedPath ||
            metadata.PreviousSha256 != source.Sha256 ||
            metadata.PreviousByteLength != source.ByteLength ||
            !string.Equals(metadata.MediaType, source.MediaType, StringComparison.OrdinalIgnoreCase) ||
            metadata.StatusCode < 200 || metadata.StatusCode > 299 ||
            metadata.RetrievedAt > completedAt ||
            metadata.Changed != changed)
        {
            throw new InvalidDataException(
                $"Staging fetch metadata for source '{source.Id}' is inconsistent with the manifest or response.");
        }
    }

    private static void ValidateHashAndLength(
        string sourceId,
        string kind,
        byte[] bytes,
        string expectedHash,
        long expectedLength)
    {
        string actualHash = Sha256(bytes);
        if (bytes.LongLength != expectedLength || actualHash != expectedHash)
        {
            throw new InvalidDataException(
                $"Source '{sourceId}' {kind} checksum or byte length does not match its metadata.");
        }
    }

    private static string ResolveStagedPath(
        string stagingRoot,
        string stagedPath,
        string expectedStagedPath)
    {
        if (stagedPath != expectedStagedPath)
        {
            throw new InvalidDataException(
                $"Staged source path '{stagedPath}' does not match '{expectedStagedPath}'.");
        }

        string path = Path.GetFullPath(
            Path.Combine(stagingRoot, stagedPath.Replace('/', Path.DirectorySeparatorChar)));
        string prefix = EnsureDirectoryPrefix(stagingRoot);
        if (!path.StartsWith(prefix, StringComparison.Ordinal) || !File.Exists(path))
        {
            throw new InvalidDataException(
                $"Staged source path '{stagedPath}' is missing or escapes the staging directory.");
        }

        return path;
    }

    private static DataManifest CreateAcceptedManifest(
        DataManifest pinned,
        DateTimeOffset completedAt,
        AcceptedSource[] acceptedSources,
        DateOnly ianaFileDate)
    {
        DateTimeOffset retrievalTime = new DateTimeOffset(
            completedAt.Year,
            completedAt.Month,
            completedAt.Day,
            completedAt.Hour,
            completedAt.Minute,
            completedAt.Second,
            completedAt.Offset);
        DataManifestSource[] sources = new DataManifestSource[acceptedSources.Length];
        for (int index = 0; index < acceptedSources.Length; index++)
        {
            AcceptedSource accepted = acceptedSources[index];
            DataManifestSource source = accepted.Source;
            string upstreamVersion = source.UpstreamVersion;
            string? upstreamDate = source.UpstreamDate;
            string upstreamDateBasis = source.UpstreamDateBasis;
            string expectedHeader = source.ExpectedHeader;
            if (source.Id == "iana-language-subtag-registry")
            {
                string date = ianaFileDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                upstreamVersion = "IANA Language Subtag Registry " + date;
                upstreamDate = date;
                upstreamDateBasis = "File-Date field";
                expectedHeader = "File-Date: " + date;
            }
            else if (accepted.Metadata.Changed && accepted.Metadata.LastModified != null)
            {
                upstreamDate = accepted.Metadata.LastModified.Value.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);
                upstreamDateBasis = "HTTP Last-Modified header";
            }

            sources[index] = new DataManifestSource(
                source.Id,
                source.Url,
                source.Path,
                retrievalTime,
                upstreamVersion,
                upstreamDate,
                upstreamDateBasis,
                accepted.Metadata.Sha256,
                accepted.Metadata.ByteLength,
                source.Encoding,
                source.MediaType,
                expectedHeader);
        }

        return new DataManifest(
            pinned.ManifestVersion,
            pinned.GeneratorSchemaVersion,
            DateOnly.FromDateTime(retrievalTime.Date),
            sources);
    }

    private static string ResolveManifestPath(string sourceRoot, string relativePath)
    {
        string path = Path.GetFullPath(
            Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(EnsureDirectoryPrefix(sourceRoot), StringComparison.Ordinal) ||
            !File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Manifest source '{relativePath}' is missing or escapes its source root.",
                path);
        }

        return path;
    }

    private static List<PreparedAcceptanceFile> PrepareFiles(
        AcceptedSource[] sources,
        string sourceRoot,
        string manifestPath,
        byte[] manifestBytes,
        RenderedSourceFile[] generatedFiles,
        string outputRoot)
    {
        List<PreparedAcceptanceFile> files = new List<PreparedAcceptanceFile>();
        HashSet<string> targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string outputPrefix = EnsureDirectoryPrefix(outputRoot);
        foreach (RenderedSourceFile generatedFile in generatedFiles)
        {
            string target = Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    generatedFile.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(outputPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Generated path '{generatedFile.RelativePath}' escapes its output directory.");
            }

            AddPreparedFile(files, targets, target, generatedFile.Bytes);
        }

        foreach (AcceptedSource source in sources)
        {
            AddPreparedFile(
                files,
                targets,
                ResolveManifestPath(sourceRoot, source.Source.Path),
                source.Bytes);
        }

        AddPreparedFile(files, targets, manifestPath, manifestBytes);
        return files;
    }

    private static void AddPreparedFile(
        List<PreparedAcceptanceFile> files,
        HashSet<string> targets,
        string targetPath,
        byte[] bytes)
    {
        if (!targets.Add(targetPath))
        {
            throw new InvalidDataException(
                $"Acceptance target '{targetPath}' is listed more than once.");
        }

        files.Add(new PreparedAcceptanceFile(targetPath, bytes));
    }

    private static int CountGeneratedChanges(
        RenderedSourceFile[] generatedFiles,
        string outputRoot)
    {
        int count = 0;
        foreach (RenderedSourceFile generatedFile in generatedFiles)
        {
            string path = Path.Combine(
                outputRoot,
                generatedFile.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || !BytesEqual(File.ReadAllBytes(path), generatedFile.Bytes))
            {
                count++;
            }
        }

        return count;
    }

    private static int Commit(List<PreparedAcceptanceFile> files)
    {
        List<PreparedAcceptanceFile> changed = new List<PreparedAcceptanceFile>();
        List<AcceptanceBackup> backups = new List<AcceptanceBackup>();
        foreach (PreparedAcceptanceFile file in files)
        {
            byte[]? original = File.Exists(file.TargetPath)
                ? File.ReadAllBytes(file.TargetPath)
                : null;
            if (original == null || !BytesEqual(original, file.Bytes))
            {
                changed.Add(file);
                backups.Add(new AcceptanceBackup(file.TargetPath, original));
            }
        }

        int committedCount = 0;
        try
        {
            foreach (PreparedAcceptanceFile file in changed)
            {
                string? parent = Path.GetDirectoryName(file.TargetPath);
                if (parent == null)
                {
                    throw new InvalidDataException(
                        $"Acceptance target '{file.TargetPath}' has no parent directory.");
                }

                Directory.CreateDirectory(parent);
                WriteAtomically(file.TargetPath, file.Bytes);
                committedCount++;
            }
        }
        catch (Exception exception)
        {
            Exception? rollbackFailure = RollBack(backups, committedCount);
            if (rollbackFailure != null)
            {
                throw new IOException(
                    "Update acceptance failed and rollback was incomplete.",
                    new AggregateException(exception, rollbackFailure));
            }

            throw new IOException("Update acceptance failed; all writes were rolled back.", exception);
        }

        return changed.Count;
    }

    private static Exception? RollBack(List<AcceptanceBackup> backups, int committedCount)
    {
        Exception? failure = null;
        for (int index = committedCount - 1; index >= 0; index--)
        {
            AcceptanceBackup backup = backups[index];
            try
            {
                if (backup.Bytes == null)
                {
                    File.Delete(backup.TargetPath);
                }
                else
                {
                    WriteAtomically(backup.TargetPath, backup.Bytes);
                }
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }

        return failure;
    }

    private static void WriteAtomically(string targetPath, byte[] bytes)
    {
        string temporaryPath = targetPath + ".accept-" + Guid.NewGuid().ToString("N") + ".tmp";
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
