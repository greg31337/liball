using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal static class ZipPackageWriter
{
    internal static byte[] WriteBytes(
        ZipEntryIndex index,
        OdfSaveOptions options)
    {
        using (MemoryStream output = new MemoryStream())
        {
            WriteArchive(index, output, options, true);
            return output.ToArray();
        }
    }

    internal static void WriteStream(
        ZipEntryIndex index,
        Stream output,
        OdfSaveOptions options)
    {
        if (options.LeaveOutputOpen)
        {
            WriteStreamKeepingOutputOpen(index, output, options);
            return;
        }
        using (output)
        {
            WriteStreamKeepingOutputOpen(index, output, options);
        }
    }

    static void WriteStreamKeepingOutputOpen(
        ZipEntryIndex index,
        Stream output,
        OdfSaveOptions options)
    {
        if (!output.CanWrite)
        {
            throw new OdfPackageException("The package output stream is not writable.");
        }
        if (!output.CanSeek)
        {
            byte[] data = WriteBytes(index, options);
            output.Write(data, 0, data.Length);
            output.Flush();
            return;
        }
        WriteArchive(index, output, options, true);
    }

    static void WriteArchive(
        ZipEntryIndex index,
        Stream output,
        OdfSaveOptions options,
        bool leaveOutputOpen)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException("Package writing was cancelled.");
        }
        EditablePackageEntry[] entries = PrepareEntries(index, options);
        if (output.CanSeek
            && output.Position == 0
            && ZipCopyThroughWriter.CanWrite(entries, options))
        {
            WriteCopyThrough(output, entries, options, leaveOutputOpen);
            return;
        }
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            leaveOutputOpen))
        {
            foreach (EditablePackageEntry item in entries)
            {
                if (options.Cancellation.IsCancellationRequested)
                {
                    throw new OdfPackageException("Package writing was cancelled.");
                }
                EnsureContentAvailable(item);
                CompressionLevel compression = SelectCompression(
                    item.Entry,
                    options);
                ZipArchiveEntry archiveEntry = archive.CreateEntry(
                    item.Entry.Path,
                    compression);
                if (options.Deterministic)
                {
                    archiveEntry.LastWriteTime = new DateTimeOffset(
                        1980,
                        1,
                        1,
                        0,
                        0,
                        0,
                        TimeSpan.Zero);
                }
                using (Stream entryOutput = archiveEntry.Open())
                {
                    entryOutput.Write(item.Data, 0, item.Data.Length);
                }
            }
        }
    }

    static EditablePackageEntry[] PrepareEntries(
        ZipEntryIndex index,
        OdfSaveOptions options)
    {
        EditablePackageEntry[] source = index.GetEditableEntries();
        List<EditablePackageEntry> prepared = new List<EditablePackageEntry>();
        EditablePackageEntry? mimetype = null;
        bool hasManifest = false;
        Dictionary<string, bool> paths = new Dictionary<string, bool>(
            StringComparer.Ordinal);
        foreach (EditablePackageEntry item in source)
        {
            string path = item.Entry.Path;
            PackagePathValue packagePath = PackagePathRules.CanonicalizeEntryPath(path);
            if (!packagePath.IsValid || !packagePath.IsCanonical)
            {
                throw new OdfPackageException(
                    $"Package entry path '{path}' is not canonical and safe.");
            }
            if (paths.ContainsKey(path))
            {
                throw new OdfPackageException(
                    $"Package entry path '{path}' occurs more than once.");
            }
            paths.Add(path, true);
            if (path == OdfPartNames.Mimetype)
            {
                mimetype = PrepareMimetypeEntry(item, index.Manifest);
            }
            else if (path == OdfPartNames.Manifest && index.Manifest != null)
            {
                prepared.Add(PrepareManifestEntry(item, index.Manifest, options));
                hasManifest = true;
            }
            else
            {
                prepared.Add(item);
                if (path == OdfPartNames.Manifest)
                {
                    hasManifest = true;
                }
            }
        }
        if (mimetype == null)
        {
            mimetype = CreateMimetypeEntry(index.Manifest);
        }
        if (!hasManifest && index.Manifest != null)
        {
            prepared.Add(CreateManifestEntry(index.Manifest, options));
        }
        if (options.Deterministic)
        {
            prepared = SortPreparedEntries(prepared);
        }

        EditablePackageEntry[] result = new EditablePackageEntry[prepared.Count + 1];
        result[0] = mimetype;
        int indexValue = 0;
        while (indexValue < prepared.Count)
        {
            result[indexValue + 1] = prepared[indexValue];
            indexValue = indexValue + 1;
        }
        return result;
    }

    static EditablePackageEntry PrepareMimetypeEntry(
        EditablePackageEntry source,
        OdfManifest? manifest)
    {
        EnsureContentAvailable(source);
        EditablePackageEntry result = CopyEditableEntry(source);
        bool outputMetadataChanged = result.Entry.CompressionMethod
            != OdfCompressionMethod.Stored
            || result.Entry.CentralExtraFieldLength != 0
            || result.Entry.LocalExtraFieldLength != 0;
        result.Entry.CompressionMethod = OdfCompressionMethod.Stored;
        result.Entry.CompressionMethodCode = 0;
        result.Entry.CentralExtraFieldLength = 0;
        result.Entry.LocalExtraFieldLength = 0;
        if (result.Data.Length == 0 && manifest != null)
        {
            string mediaType = FindRootMediaType(manifest);
            result.Data = Encoding.UTF8.GetBytes(mediaType);
            outputMetadataChanged = true;
        }
        if (!IsOdfMediaType(Encoding.UTF8.GetString(result.Data)))
        {
            throw new OdfPackageException(
                "The package mimetype entry is not a valid OpenDocument media type.");
        }
        result.IsModified = result.IsModified || outputMetadataChanged;
        return result;
    }

    static EditablePackageEntry CreateMimetypeEntry(OdfManifest? manifest)
    {
        string mediaType = "";
        if (manifest != null)
        {
            mediaType = FindRootMediaType(manifest);
        }
        if (mediaType.Length == 0)
        {
            throw new OdfPackageException(
                "A package mimetype entry or root manifest media type is required.");
        }
        if (!IsOdfMediaType(mediaType))
        {
            throw new OdfPackageException(
                "The root manifest media type is not a valid OpenDocument media type.");
        }
        EditablePackageEntry result = CreateOutputEntry(
            OdfPartNames.Mimetype,
            Encoding.UTF8.GetBytes(mediaType),
            OdfCompressionMethod.Stored);
        return result;
    }

    static EditablePackageEntry PrepareManifestEntry(
        EditablePackageEntry source,
        OdfManifest manifest,
        OdfSaveOptions options)
    {
        EditablePackageEntry result = CopyEditableEntry(source);
        byte[] data = ManifestWriter.WriteBytes(manifest, options);
        if (!ContentMatches(result.Data, data))
        {
            result.IsModified = true;
        }
        result.Data = data;
        result.IsContentAvailable = true;
        if (result.Entry.CompressionMethod != OdfCompressionMethod.Stored
            && result.Entry.CompressionMethod != OdfCompressionMethod.Deflated)
        {
            result.Entry.CompressionMethod = OdfCompressionMethod.Deflated;
            result.Entry.CompressionMethodCode = 8;
            result.IsModified = true;
        }
        return result;
    }

    static EditablePackageEntry CreateManifestEntry(
        OdfManifest manifest,
        OdfSaveOptions options)
    {
        return CreateOutputEntry(
            OdfPartNames.Manifest,
            ManifestWriter.WriteBytes(manifest, options),
            OdfCompressionMethod.Deflated);
    }

    static EditablePackageEntry CreateOutputEntry(
        string path,
        byte[] data,
        OdfCompressionMethod compressionMethod)
    {
        EditablePackageEntry result = new EditablePackageEntry();
        result.Entry.Path = path;
        result.Entry.CanonicalPath = path;
        result.Entry.IsCanonicalPath = true;
        result.Entry.CompressionMethod = compressionMethod;
        result.Entry.CompressionMethodCode = compressionMethod
            == OdfCompressionMethod.Stored
            ? 0
            : 8;
        result.Entry.ExpandedSize = data.LongLength;
        result.Data = data;
        result.IsContentAvailable = true;
        result.IsModified = true;
        return result;
    }

    static string FindRootMediaType(OdfManifest manifest)
    {
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == OdfPartNames.Root)
            {
                return entry.MediaType;
            }
        }
        return "";
    }

    static bool IsOdfMediaType(string mediaType)
    {
        return mediaType.StartsWith(
            "application/vnd.oasis.opendocument.",
            StringComparison.Ordinal)
            && OdfMediaTypeRules.ParseMediaType(mediaType).IsValid;
    }

    static void EnsureContentAvailable(EditablePackageEntry item)
    {
        if (!item.IsContentAvailable)
        {
            throw new OdfPackageException(
                $"Package entry '{item.Entry.Path}' content is unavailable.");
        }
    }

    static CompressionLevel SelectCompression(
        OdfPackageEntry entry,
        OdfSaveOptions options)
    {
        if (entry.Path == OdfPartNames.Mimetype || entry.IsDirectory)
        {
            return CompressionLevel.NoCompression;
        }
        if (entry.CompressionMethod == OdfCompressionMethod.Stored)
        {
            return CompressionLevel.NoCompression;
        }
        if (entry.CompressionMethod == OdfCompressionMethod.Deflated)
        {
            return CompressionLevel.SmallestSize;
        }
        if (options.CompressionMethod == OdfCompressionMethod.Stored)
        {
            return CompressionLevel.NoCompression;
        }
        return CompressionLevel.SmallestSize;
    }

    static List<EditablePackageEntry> SortPreparedEntries(
        List<EditablePackageEntry> source)
    {
        List<EditablePackageEntry> result = new List<EditablePackageEntry>();
        foreach (EditablePackageEntry item in source)
        {
            int destination = 0;
            while (destination < result.Count
                && string.CompareOrdinal(
                    result[destination].Entry.Path,
                    item.Entry.Path) <= 0)
            {
                destination = destination + 1;
            }
            result.Insert(destination, item);
        }
        return result;
    }

    static EditablePackageEntry CopyEditableEntry(EditablePackageEntry source)
    {
        EditablePackageEntry result = new EditablePackageEntry();
        result.Entry = CopyPackageEntry(source.Entry);
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        result.OriginalLocalRecord = new byte[source.OriginalLocalRecord.Length];
        Array.Copy(
            source.OriginalLocalRecord,
            result.OriginalLocalRecord,
            source.OriginalLocalRecord.Length);
        result.OriginalCentralRecord = new byte[source.OriginalCentralRecord.Length];
        Array.Copy(
            source.OriginalCentralRecord,
            result.OriginalCentralRecord,
            source.OriginalCentralRecord.Length);
        result.IsContentAvailable = source.IsContentAvailable;
        result.IsModified = source.IsModified;
        return result;
    }

    static void WriteCopyThrough(
        Stream output,
        EditablePackageEntry[] entries,
        OdfSaveOptions options,
        bool leaveOutputOpen)
    {
        if (leaveOutputOpen)
        {
            ZipCopyThroughWriter.WriteStream(entries, output, options);
            return;
        }
        using (output)
        {
            ZipCopyThroughWriter.WriteStream(entries, output, options);
        }
    }

    static bool ContentMatches(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static OdfPackageEntry CopyPackageEntry(OdfPackageEntry source)
    {
        OdfPackageEntry result = new OdfPackageEntry();
        result.Path = source.Path;
        result.CanonicalPath = source.CanonicalPath;
        result.CompressionMethod = source.CompressionMethod;
        result.CompressionMethodCode = source.CompressionMethodCode;
        result.CompressedSize = source.CompressedSize;
        result.ExpandedSize = source.ExpandedSize;
        result.Crc32 = source.Crc32;
        result.LocalHeaderOffset = source.LocalHeaderOffset;
        result.CentralExtraFieldLength = source.CentralExtraFieldLength;
        result.LocalExtraFieldLength = source.LocalExtraFieldLength;
        result.UsesUtf8Name = source.UsesUtf8Name;
        result.UsesZip64 = source.UsesZip64;
        result.UsesDataDescriptor = source.UsesDataDescriptor;
        result.IsDirectory = source.IsDirectory;
        result.IsFirstEntry = source.IsFirstEntry;
        result.IsCanonicalPath = source.IsCanonicalPath;
        result.IsEncrypted = source.IsEncrypted;
        result.HasValidCrc = source.HasValidCrc;
        return result;
    }
}
