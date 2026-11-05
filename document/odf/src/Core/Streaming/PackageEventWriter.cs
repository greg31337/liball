using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal class PackageEventWriter
{
    Stream Output;
    OdfSaveOptions Options;
    ZipArchive Archive;
    string RootMediaType;
    string ManifestVersion;
    Dictionary<string, bool> Paths = new Dictionary<string, bool>(
        StringComparer.Ordinal);
    List<OdfManifestEntry> ManifestEntries =
        new List<OdfManifestEntry>();
    Stream? CurrentOutput;
    string CurrentPath = "";
    string CurrentMediaType = "";
    bool Finished;

    internal PackageEventWriter(
        Stream output,
        string rootMediaType,
        string manifestVersion,
        OdfSaveOptions options)
    {
        ValidatePackageWriterInputs(
            output,
            rootMediaType,
            manifestVersion,
            options);
        Output = output;
        Options = options;
        RootMediaType = rootMediaType;
        ManifestVersion = manifestVersion;
        Archive = new ZipArchive(output, ZipArchiveMode.Create, true);
        WriteMimetypeEntry();
    }

    internal Stream BeginPackageEntry(
        string path,
        string mediaType,
        OdfCompressionMethod compressionMethod)
    {
        CheckPackageWriteState();
        if (CurrentOutput != null)
        {
            throw new OdfPackageException(
                "The current package entry must be completed before another begins.");
        }
        ValidatePackageEntry(path, compressionMethod);
        if (path == OdfPartNames.Mimetype
            || path == OdfPartNames.Manifest)
        {
            throw new OdfPackageException(
                "The package mimetype and manifest entries are managed by the streaming writer.");
        }
        AddPackagePath(path);
        CompressionLevel compression = SelectPackageCompression(
            path,
            compressionMethod);
        ZipArchiveEntry entry = Archive.CreateEntry(path, compression);
        SetPackageTimestamp(entry);
        CurrentOutput = entry.Open();
        CurrentPath = path;
        CurrentMediaType = mediaType;
        return CurrentOutput;
    }

    internal void WritePackageContent(
        byte[] content,
        int offset,
        int count)
    {
        CheckPackageWriteState();
        if (CurrentOutput == null || content == null
            || offset < 0 || count < 0
            || offset > content.Length - count)
        {
            throw new OdfPackageException(
                "A current package entry and valid content range are required.");
        }
        CurrentOutput.Write(content, offset, count);
    }

    internal void EndPackageEntry()
    {
        CheckPackageWriteState();
        if (CurrentOutput == null)
        {
            throw new OdfPackageException(
                "There is no current package entry to complete.");
        }
        CurrentOutput.Dispose();
        CurrentOutput = null;
        OdfManifestEntry manifestEntry = new OdfManifestEntry();
        manifestEntry.FullPath = CurrentPath;
        manifestEntry.MediaType = CurrentMediaType;
        ManifestEntries.Add(manifestEntry);
        CurrentPath = "";
        CurrentMediaType = "";
    }

    internal void CompletePackage()
    {
        if (Finished)
        {
            return;
        }
        CheckPackageCancellation();
        if (CurrentOutput != null)
        {
            throw new OdfPackageException(
                "The current package entry must be completed before the package.");
        }
        WriteFinalManifest();
        Archive.Dispose();
        Finished = true;
        if (!Options.LeaveOutputOpen)
        {
            Output.Dispose();
        }
    }

    void WriteMimetypeEntry()
    {
        AddPackagePath(OdfPartNames.Mimetype);
        ZipArchiveEntry entry = Archive.CreateEntry(
            OdfPartNames.Mimetype,
            CompressionLevel.NoCompression);
        SetPackageTimestamp(entry);
        using (Stream output = entry.Open())
        {
            byte[] content = Encoding.UTF8.GetBytes(RootMediaType);
            output.Write(content, 0, content.Length);
        }
    }

    void WriteFinalManifest()
    {
        AddPackagePath(OdfPartNames.Manifest);
        OdfManifest manifest = new OdfManifest();
        manifest.Version = ManifestVersion;
        OdfManifestEntry root = new OdfManifestEntry();
        root.FullPath = OdfPartNames.Root;
        root.MediaType = RootMediaType;
        root.Version = ManifestVersion;
        OdfManifestEntry[] entries =
            new OdfManifestEntry[ManifestEntries.Count + 1];
        entries[0] = root;
        int index = 0;
        while (index < ManifestEntries.Count)
        {
            entries[index + 1] = ManifestEntries[index];
            index = index + 1;
        }
        manifest.Entries = entries;
        byte[] content = ManifestWriter.WriteBytes(manifest, Options);
        ZipArchiveEntry entry = Archive.CreateEntry(
            OdfPartNames.Manifest,
            CompressionLevel.SmallestSize);
        SetPackageTimestamp(entry);
        using (Stream output = entry.Open())
        {
            output.Write(content, 0, content.Length);
        }
    }

    void AddPackagePath(string path)
    {
        if (Paths.ContainsKey(path))
        {
            throw new OdfPackageException(
                "A package entry path cannot be written more than once.");
        }
        Paths.Add(path, true);
    }

    void SetPackageTimestamp(ZipArchiveEntry entry)
    {
        if (Options.Deterministic)
        {
            entry.LastWriteTime = new DateTimeOffset(
                1980,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);
        }
    }

    static CompressionLevel SelectPackageCompression(
        string path,
        OdfCompressionMethod compressionMethod)
    {
        if (path.EndsWith("/", StringComparison.Ordinal)
            || compressionMethod == OdfCompressionMethod.Stored)
        {
            return CompressionLevel.NoCompression;
        }
        return CompressionLevel.SmallestSize;
    }

    void CheckPackageWriteState()
    {
        if (Finished)
        {
            throw new OdfPackageException(
                "The package event writer has already completed.");
        }
        CheckPackageCancellation();
    }

    void CheckPackageCancellation()
    {
        if (Options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException(
                "Package event writing was cancelled.");
        }
    }

    static void ValidatePackageWriterInputs(
        Stream output,
        string rootMediaType,
        string manifestVersion,
        OdfSaveOptions options)
    {
        if (output == null || !output.CanWrite || rootMediaType == null
            || manifestVersion == null || options == null
            || options.Cancellation == null)
        {
            throw new OdfPackageException(
                "A writable output, root media type, manifest version, and save options are required.");
        }
        OdfMediaTypeValue mediaType =
            OdfMediaTypeRules.ParseMediaType(rootMediaType);
        if (!mediaType.IsValid
            || !rootMediaType.StartsWith(
                "application/vnd.oasis.opendocument.",
                StringComparison.Ordinal))
        {
            throw new OdfPackageException(
                "The package root media type is not a valid OpenDocument media type.");
        }
        if (manifestVersion != "1.2" && manifestVersion != "1.3"
            && manifestVersion != "1.4")
        {
            throw new OdfPackageException(
                "The streaming package manifest version must be 1.2, 1.3, or 1.4.");
        }
    }

    void ValidatePackageEntry(
        string path,
        OdfCompressionMethod compressionMethod)
    {
        if (path == null)
        {
            throw new OdfPackageException(
                "A package entry path is required.");
        }
        PackagePathValue value =
            PackagePathRules.CanonicalizeEntryPath(path);
        if (!value.IsValid || !value.IsCanonical)
        {
            throw new OdfPackageException(
                "A package entry path must be canonical and safe.");
        }
        if (compressionMethod != OdfCompressionMethod.Stored
            && compressionMethod != OdfCompressionMethod.Deflated)
        {
            throw new OdfPackageException(
                "A streaming package entry must use stored or deflated compression.");
        }
    }
}
