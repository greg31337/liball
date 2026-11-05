using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace iso20022;

internal static class ExternalCodeArtifactOpener
{
    internal static ExternalCodeArtifactOpenResult OpenXlsxArtifact(
        Stream input,
        ExternalCodeImportRequest request)
    {
        BoundedArtifactCopyResult copied = CopyArtifactInput(input, request);
        if (!copied.Success)
        {
            return CreateOpenFailure(copied.ErrorCode, copied.ErrorMessage, new ArtifactManifest());
        }

        byte[] hash = SHA256.HashData(copied.Bytes);
        ArtifactManifest manifest = CreateArtifactManifest(request.Artifact, hash);
        if (request.Artifact.Hash.Value.Length > 0 &&
            !CryptographicOperations.FixedTimeEquals(request.Artifact.Hash.Value, hash))
        {
            return CreateOpenFailure(
                "external-code.artifact.hash_mismatch",
                "The external-code artifact does not match its supplied SHA-256 hash.",
                manifest);
        }

        if (!IsZipContent(copied.Bytes))
        {
            manifest.MediaType = Iso20022MediaTypes.OpenXmlWorkbook;
            return CreateOpenFailure(
                "external-code.xlsx.media",
                "The external-code artifact is neither an XLSX workbook nor a ZIP archive containing one.",
                manifest);
        }

        if (IsXlsxPackage(copied.Bytes))
        {
            manifest.MediaType = Iso20022MediaTypes.OpenXmlWorkbook;
            ExternalCodeArtifactOpenResult packageResult = new ExternalCodeArtifactOpenResult();
            packageResult.Success = true;
            packageResult.Content = copied.Bytes;
            packageResult.EntryName = request.SourceName;
            packageResult.Artifact = manifest;
            return packageResult;
        }

        manifest.MediaType = Iso20022MediaTypes.Zip;
        return OpenXlsxOuterArchive(copied.Bytes, request, manifest);
    }

    internal static ExternalCodeArtifactOpenResult OpenXsdArtifact(
        Stream input,
        ExternalCodeImportRequest request)
    {
        BoundedArtifactCopyResult copied = CopyArtifactInput(input, request);
        if (!copied.Success)
        {
            return CreateOpenFailure(copied.ErrorCode, copied.ErrorMessage, new ArtifactManifest());
        }

        byte[] hash = SHA256.HashData(copied.Bytes);
        ArtifactManifest manifest = CreateArtifactManifest(request.Artifact, hash);
        if (request.Artifact.Hash.Value.Length > 0 &&
            !CryptographicOperations.FixedTimeEquals(request.Artifact.Hash.Value, hash))
        {
            return CreateOpenFailure(
                "external-code.artifact.hash_mismatch",
                "The external-code artifact does not match its supplied SHA-256 hash.",
                manifest);
        }

        if (IsZipContent(copied.Bytes))
        {
            manifest.MediaType = Iso20022MediaTypes.Zip;
            return OpenXsdArchive(copied.Bytes, request, manifest);
        }

        manifest.MediaType = Iso20022MediaTypes.XmlSchema;
        if (!LooksLikeXml(copied.Bytes))
        {
            return CreateOpenFailure(
                "external-code.xsd.media",
                "The external-code artifact is neither XML nor a ZIP archive containing an XSD.",
                manifest);
        }

        ExternalCodeArtifactOpenResult result = new ExternalCodeArtifactOpenResult();
        result.Success = true;
        result.Content = copied.Bytes;
        result.EntryName = request.SourceName;
        result.Artifact = manifest;
        return result;
    }

    internal static ExternalCodeArtifactOpenResult OpenJsonArtifact(
        Stream input,
        ExternalCodeImportRequest request)
    {
        BoundedArtifactCopyResult copied = CopyArtifactInput(input, request);
        if (!copied.Success)
        {
            return CreateOpenFailure(copied.ErrorCode, copied.ErrorMessage, new ArtifactManifest());
        }

        byte[] hash = SHA256.HashData(copied.Bytes);
        ArtifactManifest manifest = CreateArtifactManifest(request.Artifact, hash);
        if (request.Artifact.Hash.Value.Length > 0 &&
            !CryptographicOperations.FixedTimeEquals(request.Artifact.Hash.Value, hash))
        {
            return CreateOpenFailure(
                "external-code.artifact.hash_mismatch",
                "The external-code artifact does not match its supplied SHA-256 hash.",
                manifest);
        }

        if (IsZipContent(copied.Bytes))
        {
            manifest.MediaType = Iso20022MediaTypes.Zip;
            return OpenJsonArchive(copied.Bytes, request, manifest);
        }

        manifest.MediaType = Iso20022MediaTypes.Json;
        if (!LooksLikeJsonObject(copied.Bytes))
        {
            return CreateOpenFailure(
                "external-code.json.media",
                "The external-code artifact is neither JSON nor a ZIP archive containing JSON.",
                manifest);
        }

        ExternalCodeArtifactOpenResult result = new ExternalCodeArtifactOpenResult();
        result.Success = true;
        result.Content = copied.Bytes;
        result.EntryName = request.SourceName;
        result.Artifact = manifest;
        return result;
    }

    internal static ExternalCodeArtifactOpenResult OpenJsonArchive(
        byte[] bytes,
        ExternalCodeImportRequest request,
        ArtifactManifest manifest)
    {
        using (MemoryStream source = new MemoryStream(bytes, false))
        using (ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, false))
        {
            if (archive.Entries.Count > request.MaxArchiveEntryCount)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_count",
                    "The external-code archive exceeds the configured entry-count limit.",
                    manifest);
            }

            ZipArchiveEntry? selected = null;
            int candidateCount = 0;
            long totalExpandedBytes = 0;
            int index = 0;
            while (index < archive.Entries.Count)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                ZipArchiveEntry entry = archive.Entries[index];
                if (!RepositoryArtifactOpener.IsSafeEntryName(entry.FullName))
                {
                    return CreateOpenFailure(
                        "external-code.archive.path",
                        "The external-code archive contains an unsafe entry path.",
                        manifest);
                }

                if (!RepositoryArtifactOpener.IsDirectoryEntry(entry.FullName))
                {
                    if (entry.Length > request.MaxArchiveEntryBytes)
                    {
                        return CreateOpenFailure(
                            "external-code.archive.entry_size",
                            "An external-code archive entry exceeds the configured byte limit.",
                            manifest);
                    }

                    if (totalExpandedBytes > request.MaxArchiveExpandedBytes - entry.Length)
                    {
                        return CreateOpenFailure(
                            "external-code.archive.total_size",
                            "The external-code archive exceeds the configured expanded-byte limit.",
                            manifest);
                    }

                    totalExpandedBytes = totalExpandedBytes + entry.Length;
                    if (!CompressionRatioIsAllowed(entry, request.MaxArchiveCompressionRatio))
                    {
                        return CreateOpenFailure(
                            "external-code.archive.ratio",
                            "An external-code archive entry exceeds the configured compression ratio.",
                            manifest);
                    }

                    if (JsonEntryIsSelected(entry.FullName, request.ArchiveEntryName))
                    {
                        selected = entry;
                        candidateCount++;
                    }
                }

                index++;
            }

            if (candidateCount == 0 || selected == null)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_not_found",
                    "The requested or sole JSON archive entry was not found.",
                    manifest);
            }

            if (candidateCount > 1)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_ambiguous",
                    "The archive contains multiple JSON entries; select one explicitly.",
                    manifest);
            }

            using (Stream entryStream = selected.Open())
            {
                BoundedArtifactCopyResult content = CopyArchiveEntry(entryStream, request);
                if (!content.Success)
                {
                    return CreateOpenFailure(content.ErrorCode, content.ErrorMessage, manifest);
                }

                if (content.Bytes.LongLength != selected.Length)
                {
                    return CreateOpenFailure(
                        "external-code.archive.entry_length",
                        "The expanded JSON length does not match the archive metadata.",
                        manifest);
                }

                if (!LooksLikeJsonObject(content.Bytes))
                {
                    return CreateOpenFailure(
                        "external-code.archive.entry_media",
                        "The selected external-code archive entry is not a JSON object.",
                        manifest);
                }

                ExternalCodeArtifactOpenResult result = new ExternalCodeArtifactOpenResult();
                result.Success = true;
                result.Content = content.Bytes;
                result.EntryName = selected.FullName;
                result.Artifact = manifest;
                return result;
            }
        }
    }

    internal static ExternalCodeArtifactOpenResult OpenXsdArchive(
        byte[] bytes,
        ExternalCodeImportRequest request,
        ArtifactManifest manifest)
    {
        using (MemoryStream source = new MemoryStream(bytes, false))
        using (ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, false))
        {
            if (archive.Entries.Count > request.MaxArchiveEntryCount)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_count",
                    "The external-code archive exceeds the configured entry-count limit.",
                    manifest);
            }

            ZipArchiveEntry? selected = null;
            int candidateCount = 0;
            long totalExpandedBytes = 0;
            int index = 0;
            while (index < archive.Entries.Count)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                ZipArchiveEntry entry = archive.Entries[index];
                if (!RepositoryArtifactOpener.IsSafeEntryName(entry.FullName))
                {
                    return CreateOpenFailure(
                        "external-code.archive.path",
                        "The external-code archive contains an unsafe entry path.",
                        manifest);
                }

                if (!RepositoryArtifactOpener.IsDirectoryEntry(entry.FullName))
                {
                    if (entry.Length > request.MaxArchiveEntryBytes)
                    {
                        return CreateOpenFailure(
                            "external-code.archive.entry_size",
                            "An external-code archive entry exceeds the configured byte limit.",
                            manifest);
                    }

                    if (totalExpandedBytes > request.MaxArchiveExpandedBytes - entry.Length)
                    {
                        return CreateOpenFailure(
                            "external-code.archive.total_size",
                            "The external-code archive exceeds the configured expanded-byte limit.",
                            manifest);
                    }

                    totalExpandedBytes = totalExpandedBytes + entry.Length;
                    if (!CompressionRatioIsAllowed(entry, request.MaxArchiveCompressionRatio))
                    {
                        return CreateOpenFailure(
                            "external-code.archive.ratio",
                            "An external-code archive entry exceeds the configured compression ratio.",
                            manifest);
                    }

                    if (XsdEntryIsSelected(entry.FullName, request.ArchiveEntryName))
                    {
                        selected = entry;
                        candidateCount++;
                    }
                }

                index++;
            }

            if (candidateCount == 0 || selected == null)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_not_found",
                    "The requested or sole XSD archive entry was not found.",
                    manifest);
            }

            if (candidateCount > 1)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_ambiguous",
                    "The archive contains multiple XSD entries; select one explicitly.",
                    manifest);
            }

            using (Stream entryStream = selected.Open())
            {
                BoundedArtifactCopyResult content = CopyArchiveEntry(entryStream, request);
                if (!content.Success)
                {
                    return CreateOpenFailure(content.ErrorCode, content.ErrorMessage, manifest);
                }

                if (content.Bytes.LongLength != selected.Length)
                {
                    return CreateOpenFailure(
                        "external-code.archive.entry_length",
                        "The expanded XSD length does not match the archive metadata.",
                        manifest);
                }

                if (!LooksLikeXml(content.Bytes))
                {
                    return CreateOpenFailure(
                        "external-code.archive.entry_media",
                        "The selected external-code archive entry is not XML.",
                        manifest);
                }

                ExternalCodeArtifactOpenResult result = new ExternalCodeArtifactOpenResult();
                result.Success = true;
                result.Content = content.Bytes;
                result.EntryName = selected.FullName;
                result.Artifact = manifest;
                return result;
            }
        }
    }

    internal static ExternalCodeArtifactOpenResult OpenXlsxOuterArchive(
        byte[] bytes,
        ExternalCodeImportRequest request,
        ArtifactManifest manifest)
    {
        using (MemoryStream source = new MemoryStream(bytes, false))
        using (ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, false))
        {
            if (archive.Entries.Count > request.MaxArchiveEntryCount)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_count",
                    "The external-code archive exceeds the configured entry-count limit.",
                    manifest);
            }

            ZipArchiveEntry? selected = null;
            int candidateCount = 0;
            long totalExpandedBytes = 0;
            int index = 0;
            while (index < archive.Entries.Count)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                ZipArchiveEntry entry = archive.Entries[index];
                if (!RepositoryArtifactOpener.IsSafeEntryName(entry.FullName))
                {
                    return CreateOpenFailure(
                        "external-code.archive.path",
                        "The external-code archive contains an unsafe entry path.",
                        manifest);
                }

                if (!RepositoryArtifactOpener.IsDirectoryEntry(entry.FullName))
                {
                    if (entry.Length > request.MaxArchiveEntryBytes)
                    {
                        return CreateOpenFailure(
                            "external-code.archive.entry_size",
                            "An external-code archive entry exceeds the configured byte limit.",
                            manifest);
                    }

                    if (totalExpandedBytes > request.MaxArchiveExpandedBytes - entry.Length)
                    {
                        return CreateOpenFailure(
                            "external-code.archive.total_size",
                            "The external-code archive exceeds the configured expanded-byte limit.",
                            manifest);
                    }

                    totalExpandedBytes = totalExpandedBytes + entry.Length;
                    if (!CompressionRatioIsAllowed(entry, request.MaxArchiveCompressionRatio))
                    {
                        return CreateOpenFailure(
                            "external-code.archive.ratio",
                            "An external-code archive entry exceeds the configured compression ratio.",
                            manifest);
                    }

                    if (XlsxEntryIsSelected(entry.FullName, request.ArchiveEntryName))
                    {
                        selected = entry;
                        candidateCount++;
                    }
                }

                index++;
            }

            if (candidateCount == 0 || selected == null)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_not_found",
                    "The requested or sole XLSX archive entry was not found.",
                    manifest);
            }

            if (candidateCount > 1)
            {
                return CreateOpenFailure(
                    "external-code.archive.entry_ambiguous",
                    "The archive contains multiple XLSX entries; select one explicitly.",
                    manifest);
            }

            using (Stream entryStream = selected.Open())
            {
                BoundedArtifactCopyResult content = CopyArchiveEntry(entryStream, request);
                if (!content.Success)
                {
                    return CreateOpenFailure(content.ErrorCode, content.ErrorMessage, manifest);
                }

                if (content.Bytes.LongLength != selected.Length)
                {
                    return CreateOpenFailure(
                        "external-code.archive.entry_length",
                        "The expanded XLSX length does not match the archive metadata.",
                        manifest);
                }

                if (!IsZipContent(content.Bytes) || !IsXlsxPackage(content.Bytes))
                {
                    return CreateOpenFailure(
                        "external-code.archive.entry_media",
                        "The selected external-code archive entry is not an XLSX workbook.",
                        manifest);
                }

                ExternalCodeArtifactOpenResult result = new ExternalCodeArtifactOpenResult();
                result.Success = true;
                result.Content = content.Bytes;
                result.EntryName = selected.FullName;
                result.Artifact = manifest;
                return result;
            }
        }
    }

    internal static BoundedArtifactCopyResult CopyArtifactInput(
        Stream input,
        ExternalCodeImportRequest request)
    {
        MemoryStream buffer = new MemoryStream();
        byte[] block = new byte[81920];
        long total = 0;
        int count = input.Read(block, 0, block.Length);
        while (count > 0)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            if (total > request.MaxArtifactBytes - count)
            {
                buffer.Dispose();
                BoundedArtifactCopyResult failure = new BoundedArtifactCopyResult();
                failure.ErrorCode = "external-code.artifact.size";
                failure.ErrorMessage = "The external-code artifact exceeds the configured byte limit.";
                return failure;
            }

            buffer.Write(block, 0, count);
            total = total + count;
            count = input.Read(block, 0, block.Length);
        }

        BoundedArtifactCopyResult result = new BoundedArtifactCopyResult();
        result.Success = true;
        result.Bytes = buffer.ToArray();
        buffer.Dispose();
        return result;
    }

    internal static BoundedArtifactCopyResult CopyArchiveEntry(
        Stream input,
        ExternalCodeImportRequest request)
    {
        MemoryStream buffer = new MemoryStream();
        byte[] block = new byte[81920];
        long total = 0;
        int count = input.Read(block, 0, block.Length);
        while (count > 0)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            if (total > request.MaxArchiveEntryBytes - count ||
                total > request.Limits.MaxTotalBytes - count)
            {
                buffer.Dispose();
                BoundedArtifactCopyResult failure = new BoundedArtifactCopyResult();
                failure.ErrorCode = "external-code.archive.entry_size";
                failure.ErrorMessage = "The expanded JSON entry exceeds the configured byte limit.";
                return failure;
            }

            buffer.Write(block, 0, count);
            total = total + count;
            count = input.Read(block, 0, block.Length);
        }

        BoundedArtifactCopyResult result = new BoundedArtifactCopyResult();
        result.Success = true;
        result.Bytes = buffer.ToArray();
        buffer.Dispose();
        return result;
    }

    internal static ArtifactManifest CreateArtifactManifest(
        ArtifactManifest source,
        byte[] hash)
    {
        ArtifactManifest manifest = new ArtifactManifest();
        manifest.Coordinate = source.Coordinate;
        manifest.OriginUri = source.OriginUri;
        manifest.RetrievedAt = source.RetrievedAt;
        manifest.PublicationDate = source.PublicationDate;
        manifest.Hash = new IsoBinaryValue();
        manifest.Hash.Value = hash;
        manifest.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        manifest.Hash.LexicalValue = BinaryLexical.FormatHexadecimal(hash);
        manifest.HashAlgorithm = "SHA-256";
        manifest.MediaType = source.MediaType;
        manifest.LicenseNotice = source.LicenseNotice;
        manifest.IsApproved = source.IsApproved;
        return manifest;
    }

    internal static bool IsZipContent(byte[] bytes)
    {
        return bytes.Length >= 4 && bytes[0] == 80 && bytes[1] == 75 &&
            ((bytes[2] == 3 && bytes[3] == 4) ||
            (bytes[2] == 5 && bytes[3] == 6) ||
            (bytes[2] == 7 && bytes[3] == 8));
    }

    internal static bool IsXlsxPackage(byte[] bytes)
    {
        using (MemoryStream source = new MemoryStream(bytes, false))
        using (ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, false))
        {
            bool hasContentTypes = false;
            bool hasWorkbook = false;
            int index = 0;
            while (index < archive.Entries.Count)
            {
                string name = archive.Entries[index].FullName;
                if (name == "[Content_Types].xml")
                {
                    hasContentTypes = true;
                }
                else if (name == "xl/workbook.xml")
                {
                    hasWorkbook = true;
                }

                index++;
            }

            return hasContentTypes && hasWorkbook;
        }
    }

    internal static bool LooksLikeJsonObject(byte[] bytes)
    {
        int index = 0;
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            index = 3;
        }

        while (index < bytes.Length &&
            (bytes[index] == 32 || bytes[index] == 9 || bytes[index] == 10 || bytes[index] == 13))
        {
            index++;
        }

        return index < bytes.Length && bytes[index] == 123;
    }

    internal static bool LooksLikeXml(byte[] bytes)
    {
        int index = 0;
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 &&
            bytes[2] == 254 && bytes[3] == 255)
        {
            return bytes.Length >= 8 && bytes[4] == 0 && bytes[5] == 0 &&
                bytes[6] == 0 && bytes[7] == 60;
        }

        if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 254 &&
            bytes[2] == 0 && bytes[3] == 0)
        {
            return bytes.Length >= 8 && bytes[4] == 60 && bytes[5] == 0 &&
                bytes[6] == 0 && bytes[7] == 0;
        }

        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return bytes.Length >= 4 && bytes[2] == 0 && bytes[3] == 60;
        }

        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return bytes.Length >= 4 && bytes[2] == 60 && bytes[3] == 0;
        }

        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            index = 3;
        }

        while (index < bytes.Length &&
            (bytes[index] == 32 || bytes[index] == 9 || bytes[index] == 10 || bytes[index] == 13))
        {
            index++;
        }

        if (index < bytes.Length && bytes[index] == 60)
        {
            return true;
        }

        return bytes.Length >= 4 &&
            ((bytes[0] == 0 && bytes[1] == 60 && bytes[2] == 0) ||
            (bytes[0] == 60 && bytes[1] == 0 && bytes[2] == 0));
    }

    internal static bool JsonEntryIsSelected(string entryName, string requestedEntryName)
    {
        if (requestedEntryName.Length > 0)
        {
            return entryName == requestedEntryName;
        }

        return entryName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool XsdEntryIsSelected(string entryName, string requestedEntryName)
    {
        if (requestedEntryName.Length > 0)
        {
            return entryName == requestedEntryName;
        }

        return entryName.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool XlsxEntryIsSelected(string entryName, string requestedEntryName)
    {
        if (requestedEntryName.Length > 0)
        {
            return entryName == requestedEntryName;
        }

        return entryName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CompressionRatioIsAllowed(ZipArchiveEntry entry, int maximumRatio)
    {
        if (entry.Length == 0)
        {
            return true;
        }

        if (entry.CompressedLength <= 0)
        {
            return false;
        }

        long quotient = entry.Length / entry.CompressedLength;
        long remainder = entry.Length % entry.CompressedLength;
        return quotient < maximumRatio || (quotient == maximumRatio && remainder == 0);
    }

    internal static ExternalCodeArtifactOpenResult CreateOpenFailure(
        string errorCode,
        string errorMessage,
        ArtifactManifest artifact)
    {
        ExternalCodeArtifactOpenResult result = new ExternalCodeArtifactOpenResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.Artifact = artifact;
        return result;
    }
}
