using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace iso20022;

internal static class RepositoryArtifactOpener
{
    internal static RepositoryArtifactOpenResult OpenArtifact(Stream source, RepositoryArtifactOpenOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateOptions(options);
        BoundedArtifactCopyResult input = CopyInputBounded(source, options.MaxArtifactBytes);
        if (!input.Success)
        {
            return CreateFailure(input.ErrorCode, input.ErrorMessage);
        }

        string mediaType = DetectMediaType(input.Bytes);
        ArtifactManifest manifest = CreateManifest(input.Bytes, mediaType, options);
        if (mediaType == Iso20022MediaTypes.Xml)
        {
            return OpenRawArtifact(input.Bytes, manifest);
        }

        if (mediaType == Iso20022MediaTypes.Zip)
        {
            return OpenZipArtifact(input.Bytes, manifest, options);
        }

        RepositoryArtifactOpenResult unsupported = CreateFailure(
            "artifact.media.unsupported",
            "The artifact is neither XML nor a ZIP archive.");
        unsupported.ArtifactByteCount = input.Bytes.LongLength;
        unsupported.Manifest = manifest;
        unsupported.Provenance = CreateProvenance(manifest, string.Empty);
        return unsupported;
    }

    internal static void ValidateOptions(RepositoryArtifactOpenOptions options)
    {
        ValidatePositive(options.MaxArtifactBytes, "MaxArtifactBytes");
        ValidatePositive(options.MaxArchiveEntryCount, "MaxArchiveEntryCount");
        ValidatePositive(options.MaxArchiveEntryBytes, "MaxArchiveEntryBytes");
        ValidatePositive(options.MaxArchiveExpandedBytes, "MaxArchiveExpandedBytes");
        ValidatePositive(options.MaxArchiveCompressionRatio, "MaxArchiveCompressionRatio");
        if (options.MaxArchiveEntryBytes > options.MaxArchiveExpandedBytes)
        {
            throw new ArgumentOutOfRangeException(
                "options",
                "MaxArchiveEntryBytes cannot exceed MaxArchiveExpandedBytes.");
        }
    }

    internal static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException("options", name + " must be positive.");
        }
    }

    internal static BoundedArtifactCopyResult CopyInputBounded(Stream source, int maximumBytes)
    {
        MemoryStream buffer = new MemoryStream();
        byte[] block = new byte[81920];
        long total = 0;
        int count = source.Read(block, 0, block.Length);
        while (count > 0)
        {
            if (total > maximumBytes - count)
            {
                buffer.Dispose();
                BoundedArtifactCopyResult failure = new BoundedArtifactCopyResult();
                failure.ErrorCode = "artifact.input.limit";
                failure.ErrorMessage = "The artifact exceeds MaxArtifactBytes.";
                return failure;
            }

            buffer.Write(block, 0, count);
            total = total + count;
            count = source.Read(block, 0, block.Length);
        }

        BoundedArtifactCopyResult result = new BoundedArtifactCopyResult();
        result.Success = true;
        result.Bytes = buffer.ToArray();
        buffer.Dispose();
        return result;
    }

    internal static RepositoryArtifactOpenResult OpenRawArtifact(byte[] bytes, ArtifactManifest manifest)
    {
        RepositoryArtifactOpenResult result = new RepositoryArtifactOpenResult();
        result.Success = true;
        result.Content = new MemoryStream(bytes, false);
        result.ContentMediaType = Iso20022MediaTypes.Xml;
        result.ArtifactByteCount = bytes.LongLength;
        result.ContentByteCount = bytes.LongLength;
        result.Manifest = manifest;
        result.Provenance = CreateProvenance(manifest, string.Empty);
        result.ContentHash = CreateHash(bytes);
        return result;
    }

    internal static RepositoryArtifactOpenResult OpenZipArtifact(
        byte[] bytes,
        ArtifactManifest manifest,
        RepositoryArtifactOpenOptions options)
    {
        using (MemoryStream source = new MemoryStream(bytes, false))
        using (ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, false))
        {
            if (archive.Entries.Count > options.MaxArchiveEntryCount)
            {
                return CreateArchiveFailure(
                    "artifact.archive.entry_count",
                    "The archive exceeds MaxArchiveEntryCount.",
                    bytes.LongLength,
                    manifest);
            }

            ZipArchiveEntry? selected = null;
            long totalExpandedBytes = 0;
            int candidateCount = 0;
            int index = 0;
            while (index < archive.Entries.Count)
            {
                ZipArchiveEntry entry = archive.Entries[index];
                if (!IsSafeEntryName(entry.FullName))
                {
                    return CreateArchiveFailure(
                        "artifact.archive.path",
                        "The archive contains an unsafe entry path.",
                        bytes.LongLength,
                        manifest);
                }

                if (!IsDirectoryEntry(entry.FullName))
                {
                    if (entry.Length > options.MaxArchiveEntryBytes)
                    {
                        return CreateArchiveFailure(
                            "artifact.archive.entry_size",
                            "An archive entry exceeds MaxArchiveEntryBytes.",
                            bytes.LongLength,
                            manifest);
                    }

                    if (totalExpandedBytes > options.MaxArchiveExpandedBytes - entry.Length)
                    {
                        return CreateArchiveFailure(
                            "artifact.archive.total_size",
                            "The archive exceeds MaxArchiveExpandedBytes.",
                            bytes.LongLength,
                            manifest);
                    }

                    totalExpandedBytes = totalExpandedBytes + entry.Length;
                    if (!IsCompressionRatioAllowed(entry, options.MaxArchiveCompressionRatio))
                    {
                        return CreateArchiveFailure(
                            "artifact.archive.ratio",
                            "An archive entry exceeds MaxArchiveCompressionRatio.",
                            bytes.LongLength,
                            manifest);
                    }

                    bool candidate = IsSelectedEntry(entry.FullName, options.EntryName);
                    if (candidate)
                    {
                        candidateCount++;
                        selected = entry;
                    }
                }

                index++;
            }

            if (candidateCount == 0 || selected == null)
            {
                return CreateArchiveFailure(
                    "artifact.archive.entry_not_found",
                    "The requested or supported repository entry was not found.",
                    bytes.LongLength,
                    manifest);
            }

            if (candidateCount > 1)
            {
                return CreateArchiveFailure(
                    "artifact.archive.entry_ambiguous",
                    "The archive contains multiple supported repository entries; select one explicitly.",
                    bytes.LongLength,
                    manifest);
            }

            using (Stream entryStream = selected.Open())
            {
                BoundedArtifactCopyResult content = CopyEntryBounded(entryStream, options.MaxArchiveEntryBytes);
                if (!content.Success)
                {
                    return CreateArchiveFailure(
                        content.ErrorCode,
                        content.ErrorMessage,
                        bytes.LongLength,
                        manifest);
                }

                if (content.Bytes.LongLength != selected.Length)
                {
                    return CreateArchiveFailure(
                        "artifact.archive.entry_length",
                        "The expanded entry length does not match the archive metadata.",
                        bytes.LongLength,
                        manifest);
                }

                string contentMediaType = DetectMediaType(content.Bytes);
                if (contentMediaType != Iso20022MediaTypes.Xml)
                {
                    return CreateArchiveFailure(
                        "artifact.archive.entry_media",
                        "The selected repository entry is not XML.",
                        bytes.LongLength,
                        manifest);
                }

                RepositoryArtifactOpenResult result = new RepositoryArtifactOpenResult();
                result.Success = true;
                result.Content = new MemoryStream(content.Bytes, false);
                result.WasArchive = true;
                result.EntryName = selected.FullName;
                result.ContentMediaType = contentMediaType;
                result.ArtifactByteCount = bytes.LongLength;
                result.CompressedEntryByteCount = selected.CompressedLength;
                result.ContentByteCount = content.Bytes.LongLength;
                result.Manifest = manifest;
                result.Provenance = CreateProvenance(manifest, selected.FullName);
                result.ContentHash = CreateHash(content.Bytes);
                return result;
            }
        }
    }

    internal static BoundedArtifactCopyResult CopyEntryBounded(Stream source, int maximumBytes)
    {
        MemoryStream buffer = new MemoryStream();
        byte[] block = new byte[81920];
        long total = 0;
        int count = source.Read(block, 0, block.Length);
        while (count > 0)
        {
            if (total > maximumBytes - count)
            {
                buffer.Dispose();
                BoundedArtifactCopyResult failure = new BoundedArtifactCopyResult();
                failure.ErrorCode = "artifact.archive.entry_size";
                failure.ErrorMessage = "The expanded archive entry exceeds MaxArchiveEntryBytes.";
                return failure;
            }

            buffer.Write(block, 0, count);
            total = total + count;
            count = source.Read(block, 0, block.Length);
        }

        BoundedArtifactCopyResult result = new BoundedArtifactCopyResult();
        result.Success = true;
        result.Bytes = buffer.ToArray();
        buffer.Dispose();
        return result;
    }

    internal static bool IsCompressionRatioAllowed(ZipArchiveEntry entry, int maximumRatio)
    {
        if (entry.Length == 0)
        {
            return true;
        }

        if (entry.CompressedLength <= 0)
        {
            return false;
        }

        return entry.Length <= entry.CompressedLength * maximumRatio;
    }

    internal static bool IsSelectedEntry(string entryName, string requestedEntryName)
    {
        if (requestedEntryName.Length > 0)
        {
            return entryName == requestedEntryName;
        }

        return entryName.EndsWith(".iso20022", StringComparison.OrdinalIgnoreCase) ||
            entryName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
            entryName.EndsWith(".ecore", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsDirectoryEntry(string entryName)
    {
        return entryName.EndsWith("/", StringComparison.Ordinal) ||
            entryName.EndsWith("\\", StringComparison.Ordinal);
    }

    internal static bool IsSafeEntryName(string entryName)
    {
        if (entryName.Length == 0 || entryName.IndexOf('\0') >= 0)
        {
            return false;
        }

        string normalized = entryName.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (normalized.Length >= 2 && normalized[1] == ':' &&
            ((normalized[0] >= 'A' && normalized[0] <= 'Z') ||
            (normalized[0] >= 'a' && normalized[0] <= 'z')))
        {
            return false;
        }

        string[] segments = normalized.Split('/');
        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index] == ".." || segments[index] == ".")
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static string DetectMediaType(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 80 && bytes[1] == 75 &&
            ((bytes[2] == 3 && bytes[3] == 4) ||
            (bytes[2] == 5 && bytes[3] == 6) ||
            (bytes[2] == 7 && bytes[3] == 8)))
        {
            return Iso20022MediaTypes.Zip;
        }

        if (bytes.Length >= 4 &&
            ((bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 60 && bytes[3] == 0) ||
            (bytes[0] == 254 && bytes[1] == 255 && bytes[2] == 0 && bytes[3] == 60)))
        {
            return Iso20022MediaTypes.Xml;
        }

        if (bytes.Length >= 8 &&
            ((bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 0 && bytes[3] == 0 &&
            bytes[4] == 60 && bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 0) ||
            (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 254 && bytes[3] == 255 &&
            bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 60)))
        {
            return Iso20022MediaTypes.Xml;
        }

        int index = 0;
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            index = 3;
        }

        while (index < bytes.Length &&
            (bytes[index] == 32 || bytes[index] == 9 || bytes[index] == 13 || bytes[index] == 10))
        {
            index++;
        }

        if (index < bytes.Length && bytes[index] == 60)
        {
            return Iso20022MediaTypes.Xml;
        }

        return string.Empty;
    }

    internal static ArtifactManifest CreateManifest(
        byte[] artifactBytes,
        string mediaType,
        RepositoryArtifactOpenOptions options)
    {
        ArtifactManifest manifest = new ArtifactManifest();
        manifest.Coordinate = options.Coordinate;
        manifest.OriginUri = options.OriginUri;
        manifest.RetrievedAt = options.RetrievedAt;
        manifest.PublicationDate = options.PublicationDate;
        manifest.Hash = CreateHash(artifactBytes);
        manifest.HashAlgorithm = "SHA-256";
        manifest.MediaType = mediaType;
        manifest.LicenseNotice = options.LicenseNotice;
        manifest.IsApproved = options.IsApproved;
        return manifest;
    }

    internal static RepositoryProvenance CreateProvenance(ArtifactManifest manifest, string entryName)
    {
        RepositoryProvenance provenance = new RepositoryProvenance();
        provenance.Artifact = manifest;
        provenance.SourceEntryName = entryName;
        return provenance;
    }

    internal static IsoBinaryValue CreateHash(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        IsoBinaryValue value = new IsoBinaryValue();
        value.Value = hash;
        value.Encoding = BinaryEncodingKind.Hexadecimal;
        value.LexicalValue = BinaryLexical.FormatHexadecimal(hash);
        return value;
    }

    internal static RepositoryArtifactOpenResult CreateArchiveFailure(
        string errorCode,
        string errorMessage,
        long artifactByteCount,
        ArtifactManifest manifest)
    {
        RepositoryArtifactOpenResult result = CreateFailure(errorCode, errorMessage);
        result.WasArchive = true;
        result.ArtifactByteCount = artifactByteCount;
        result.Manifest = manifest;
        result.Provenance = CreateProvenance(manifest, string.Empty);
        return result;
    }

    internal static RepositoryArtifactOpenResult CreateFailure(string errorCode, string errorMessage)
    {
        RepositoryArtifactOpenResult result = new RepositoryArtifactOpenResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
