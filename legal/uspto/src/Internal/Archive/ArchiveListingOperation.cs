namespace uspto;

internal static class ArchiveFormatDetector
{
    internal const int PrefixBytes = 1024;

    internal static CompressionDetectionResult Detect(string archivePath)
    {
        if (global::System.String.IsNullOrWhiteSpace(archivePath) || !global::System.IO.File.Exists(archivePath))
        {
            throw new ValidationException("An archive file is required.", "archivePath", "FILE_NOT_FOUND", null);
        }

        byte[] prefix;
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read, PrefixBytes, global::System.IO.FileOptions.SequentialScan))
        {
            prefix = ReadPrefix(input, PrefixBytes);
        }

        CompressionDetectionResult result = new CompressionDetectionResult();
        if (HasZipSignature(prefix))
        {
            ApplySignatureResult(result, CompressionFormat.Zip, MatchesExtension(archivePath, CompressionFormat.Zip));
            return result;
        }

        if (HasGzipSignature(prefix))
        {
            CompressionFormat gzipFormat = DetectGzipFormat(archivePath);
            ApplySignatureResult(result, gzipFormat, MatchesExtension(archivePath, gzipFormat));
            return result;
        }

        if (HasTarSignature(prefix))
        {
            ApplySignatureResult(result, CompressionFormat.Tar, MatchesExtension(archivePath, CompressionFormat.Tar));
            return result;
        }

        int unsupportedKind = GetUnsupportedSignatureKind(prefix);
        if (unsupportedKind != 0)
        {
            ApplySignatureResult(result, CompressionFormat.Unsupported, MatchesUnsupportedExtension(archivePath, unsupportedKind));
            result.IsSupported = false;
            return result;
        }

        result.Format = DetectExtension(archivePath);
        result.DetectedBySignature = false;
        result.ExtensionMatches = false;
        result.IsSupported = IsSupported(result.Format);
        return result;
    }

    internal static byte[] ReadPrefix(global::System.IO.Stream input, int maximumBytes)
    {
        byte[] buffer = new byte[maximumBytes];
        int total = 0;
        while (total < buffer.Length)
        {
            int read = input.Read(buffer, total, buffer.Length - total);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        if (total == buffer.Length)
        {
            return buffer;
        }

        byte[] result = new byte[total];
        global::System.Array.Copy(buffer, result, total);
        return result;
    }

    internal static void ApplySignatureResult(CompressionDetectionResult result, CompressionFormat format, bool extensionMatches)
    {
        result.Format = format;
        result.DetectedBySignature = true;
        result.ExtensionMatches = extensionMatches;
        result.IsSupported = IsSupported(format);
    }

    internal static CompressionFormat DetectGzipFormat(string archivePath)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read, PrefixBytes, global::System.IO.FileOptions.SequentialScan))
        using (global::System.IO.Compression.GZipStream gzip = new global::System.IO.Compression.GZipStream(input, global::System.IO.Compression.CompressionMode.Decompress, false))
        {
            byte[] prefix = ReadPrefix(gzip, PrefixBytes);
            if (HasTarSignature(prefix))
            {
                return CompressionFormat.TarGzip;
            }
        }

        return CompressionFormat.Gzip;
    }

    internal static bool HasZipSignature(byte[] prefix)
    {
        if (prefix.Length < 4 || prefix[0] != 0x50 || prefix[1] != 0x4b)
        {
            return false;
        }

        bool localHeader = prefix[2] == 0x03 && prefix[3] == 0x04;
        bool emptyArchive = prefix[2] == 0x05 && prefix[3] == 0x06;
        bool spanningHeader = prefix[2] == 0x07 && prefix[3] == 0x08;
        return localHeader || emptyArchive || spanningHeader;
    }

    internal static bool HasGzipSignature(byte[] prefix)
    {
        return prefix.Length >= 3 && prefix[0] == 0x1f && prefix[1] == 0x8b && prefix[2] == 0x08;
    }

    internal static bool HasTarSignature(byte[] prefix)
    {
        if (prefix.Length < 512)
        {
            return false;
        }

        if (prefix.Length >= PrefixBytes && IsZeroBlock(prefix, 0) && IsZeroBlock(prefix, 512))
        {
            return true;
        }

        if (prefix[257] == 0x75 && prefix[258] == 0x73 && prefix[259] == 0x74 && prefix[260] == 0x61 && prefix[261] == 0x72)
        {
            return true;
        }

        long storedChecksum = ParseTarChecksum(prefix);
        if (storedChecksum < 0L)
        {
            return false;
        }

        long calculatedChecksum = 0L;
        int index;
        for (index = 0; index < 512; index += 1)
        {
            if (index >= 148 && index < 156)
            {
                calculatedChecksum += 32L;
            }
            else
            {
                calculatedChecksum += prefix[index];
            }
        }

        return storedChecksum == calculatedChecksum;
    }

    internal static bool IsZeroBlock(byte[] prefix, int offset)
    {
        int index;
        for (index = offset; index < offset + 512; index += 1)
        {
            if (prefix[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static long ParseTarChecksum(byte[] prefix)
    {
        long result = 0L;
        bool hasDigit = false;
        int index;
        for (index = 148; index < 156; index += 1)
        {
            byte value = prefix[index];
            if (value == 0 || value == 32)
            {
                if (hasDigit)
                {
                    break;
                }

                continue;
            }

            if (value < 0x30 || value > 0x37)
            {
                return -1L;
            }

            hasDigit = true;
            result = result * 8L + value - 0x30;
        }

        return hasDigit ? result : -1L;
    }

    internal static int GetUnsupportedSignatureKind(byte[] prefix)
    {
        if (prefix.Length >= 3 && prefix[0] == 0x42 && prefix[1] == 0x5a && prefix[2] == 0x68)
        {
            return 1;
        }

        if (prefix.Length >= 6 && prefix[0] == 0x37 && prefix[1] == 0x7a && prefix[2] == 0xbc && prefix[3] == 0xaf && prefix[4] == 0x27 && prefix[5] == 0x1c)
        {
            return 2;
        }

        if (prefix.Length >= 6 && prefix[0] == 0xfd && prefix[1] == 0x37 && prefix[2] == 0x7a && prefix[3] == 0x58 && prefix[4] == 0x5a && prefix[5] == 0x00)
        {
            return 3;
        }

        if (prefix.Length >= 7 && prefix[0] == 0x52 && prefix[1] == 0x61 && prefix[2] == 0x72 && prefix[3] == 0x21 && prefix[4] == 0x1a && prefix[5] == 0x07 && (prefix[6] == 0x00 || prefix[6] == 0x01))
        {
            return 4;
        }

        if (prefix.Length >= 4 && prefix[0] == 0x28 && prefix[1] == 0xb5 && prefix[2] == 0x2f && prefix[3] == 0xfd)
        {
            return 5;
        }

        return 0;
    }

    internal static CompressionFormat DetectExtension(string archivePath)
    {
        if (archivePath.EndsWith(".tar.gz", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".tgz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.TarGzip;
        }

        if (archivePath.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Zip;
        }

        if (archivePath.EndsWith(".gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Gzip;
        }

        if (archivePath.EndsWith(".tar", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Tar;
        }

        if (archivePath.EndsWith(".bz2", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".bzip2", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".7z", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".xz", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".rar", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".zst", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".zstd", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Unsupported;
        }

        return CompressionFormat.Unknown;
    }

    internal static bool MatchesExtension(string archivePath, CompressionFormat format)
    {
        return DetectExtension(archivePath) == format;
    }

    internal static bool MatchesUnsupportedExtension(string archivePath, int signatureKind)
    {
        if (signatureKind == 1)
        {
            return archivePath.EndsWith(".bz2", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".bzip2", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (signatureKind == 2)
        {
            return archivePath.EndsWith(".7z", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (signatureKind == 3)
        {
            return archivePath.EndsWith(".xz", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (signatureKind == 4)
        {
            return archivePath.EndsWith(".rar", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (signatureKind == 5)
        {
            return archivePath.EndsWith(".zst", global::System.StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".zstd", global::System.StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    internal static bool IsSupported(CompressionFormat format)
    {
        return format == CompressionFormat.Zip || format == CompressionFormat.Gzip || format == CompressionFormat.Tar || format == CompressionFormat.TarGzip;
    }
}

internal class ArchiveListingBuilder
{
    internal ExtractionLimits Limits;
    internal global::System.Collections.Generic.List<ArchiveEntry> Entries = new global::System.Collections.Generic.List<ArchiveEntry>();
    internal long TotalUncompressedBytes = 0L;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;

    internal ArchiveListingBuilder(ExtractionLimits limits)
    {
        Limits = limits;
    }

    internal bool TryAdd(ArchiveEntry entry)
    {
        if (Entries.Count >= Limits.MaximumEntries)
        {
            Fail("ARCHIVE_ENTRY_LIMIT_EXCEEDED", "The archive contains more entries than the configured listing limit.");
            return false;
        }

        if (entry.HasUncompressedSize)
        {
            if (entry.UncompressedSize < 0L || entry.UncompressedSize > Limits.MaximumEntryBytes)
            {
                Fail("ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED", "An archive entry exceeds the configured uncompressed-size limit.");
                return false;
            }

            if (TotalUncompressedBytes > Limits.MaximumTotalBytes - entry.UncompressedSize)
            {
                Fail("ARCHIVE_TOTAL_SIZE_LIMIT_EXCEEDED", "The archive exceeds the configured total uncompressed-size limit.");
                return false;
            }
        }

        if (entry.HasCompressedSize && entry.HasUncompressedSize && entry.UncompressedSize > 0L)
        {
            if (entry.CompressedSize == 0L)
            {
                Fail("ARCHIVE_COMPRESSION_RATIO_EXCEEDED", "An archive entry exceeds the configured compression-ratio limit.");
                return false;
            }

            if (entry.CompressedSize <= global::System.Int64.MaxValue / Limits.MaximumCompressionRatio && entry.UncompressedSize > entry.CompressedSize * Limits.MaximumCompressionRatio)
            {
                Fail("ARCHIVE_COMPRESSION_RATIO_EXCEEDED", "An archive entry exceeds the configured compression-ratio limit.");
                return false;
            }
        }

        Entries.Add(entry);
        if (entry.HasUncompressedSize)
        {
            TotalUncompressedBytes += entry.UncompressedSize;
        }

        return true;
    }

    internal void Fail(string code, string message)
    {
        if (FailureCode == null)
        {
            FailureCode = code;
            FailureMessage = message;
        }
    }
}

internal static class ArchiveListingOperation
{
    internal static ArchiveListingResult List(string archivePath, RequestContext context)
    {
        return ListWithLimits(archivePath, context, new ExtractionLimits());
    }

    internal static ArchiveListingResult ListWithLimits(string archivePath, RequestContext context, ExtractionLimits limits)
    {
        ValidateRequest(archivePath, context, limits);
        ThrowIfStopped(context);
        CompressionDetectionResult detection = ArchiveFormatDetector.Detect(archivePath);
        ArchiveListingResult result = new ArchiveListingResult();
        result.SourceName = archivePath;
        result.Detection = detection;
        if (!detection.IsSupported)
        {
            result.FailureCode = "UNSUPPORTED_ARCHIVE_FORMAT";
            result.FailureMessage = detection.Format == CompressionFormat.Unsupported ? "The archive uses a recognized but unsupported compression format." : "The archive format could not be determined or is not supported.";
            return result;
        }

        ArchiveListingBuilder builder = new ArchiveListingBuilder(limits);
        switch (detection.Format)
        {
            case CompressionFormat.Zip:
                ListZip(archivePath, context, builder);
                break;
            case CompressionFormat.Gzip:
                ListGzip(archivePath, context, builder);
                break;
            case CompressionFormat.Tar:
                ListTarFile(archivePath, context, builder);
                break;
            case CompressionFormat.TarGzip:
                ListTarGzip(archivePath, context, builder);
                break;
            default:
                builder.Fail("UNSUPPORTED_ARCHIVE_FORMAT", "The archive format is not supported.");
                break;
        }

        result.Entries = builder.Entries.ToArray();
        result.FailureCode = builder.FailureCode;
        result.FailureMessage = builder.FailureMessage;
        result.IsComplete = builder.FailureCode == null;
        return result;
    }

    internal static void ListZip(string archivePath, RequestContext context, ArchiveListingBuilder builder)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        using (global::System.IO.Compression.ZipArchive archive = new global::System.IO.Compression.ZipArchive(input, global::System.IO.Compression.ZipArchiveMode.Read, false))
        {
            foreach (global::System.IO.Compression.ZipArchiveEntry source in archive.Entries)
            {
                ThrowIfStopped(context);
                ArchiveEntry entry = new ArchiveEntry();
                entry.Name = NormalizeName(source.FullName);
                entry.CompressedSize = source.CompressedLength;
                entry.HasCompressedSize = true;
                entry.UncompressedSize = source.Length;
                entry.HasUncompressedSize = true;
                entry.IsDirectory = source.Name.Length == 0 || source.FullName.EndsWith("/", global::System.StringComparison.Ordinal) || source.FullName.EndsWith("\\", global::System.StringComparison.Ordinal);
                int unixType = source.ExternalAttributes >> 16 & 0xf000;
                entry.IsLink = unixType == 0xa000;
                entry.LastModified = source.LastWriteTime;
                entry.HasLastModified = true;
                if (!builder.TryAdd(entry))
                {
                    return;
                }
            }
        }
    }

    internal static void ListGzip(string archivePath, RequestContext context, ArchiveListingBuilder builder)
    {
        global::System.IO.FileInfo information = new global::System.IO.FileInfo(archivePath);
        ArchiveEntry entry = new ArchiveEntry();
        entry.Name = GetGzipEntryName(archivePath);
        entry.CompressedSize = information.Length;
        entry.HasCompressedSize = true;
        long total = 0L;
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        using (global::System.IO.Compression.GZipStream gzip = new global::System.IO.Compression.GZipStream(input, global::System.IO.Compression.CompressionMode.Decompress, false))
        {
            byte[] buffer = new byte[UsptoDefaults.DownloadBufferSize];
            while (true)
            {
                ThrowIfStopped(context);
                int read = gzip.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                if (total > builder.Limits.MaximumEntryBytes - read || total > builder.Limits.MaximumTotalBytes - read)
                {
                    builder.Fail("ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED", "The gzip content exceeds the configured uncompressed-size limit.");
                    return;
                }

                total += read;
            }
        }

        entry.UncompressedSize = total;
        entry.HasUncompressedSize = true;
        builder.TryAdd(entry);
    }

    internal static void ListTarFile(string archivePath, RequestContext context, ArchiveListingBuilder builder)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        {
            ListTarStream(input, context, builder);
        }
    }

    internal static void ListTarGzip(string archivePath, RequestContext context, ArchiveListingBuilder builder)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        using (global::System.IO.Compression.GZipStream gzip = new global::System.IO.Compression.GZipStream(input, global::System.IO.Compression.CompressionMode.Decompress, false))
        {
            ListTarStream(gzip, context, builder);
        }
    }

    internal static void ListTarStream(global::System.IO.Stream input, RequestContext context, ArchiveListingBuilder builder)
    {
        using (global::System.Formats.Tar.TarReader reader = new global::System.Formats.Tar.TarReader(input, true))
        {
            while (true)
            {
                ThrowIfStopped(context);
                global::System.Formats.Tar.TarEntry? source = reader.GetNextEntry(false);
                if (source == null)
                {
                    break;
                }

                ArchiveEntry entry = new ArchiveEntry();
                entry.Name = NormalizeName(source.Name);
                entry.UncompressedSize = source.Length;
                entry.HasUncompressedSize = true;
                entry.IsDirectory = source.EntryType == global::System.Formats.Tar.TarEntryType.Directory;
                entry.IsLink = source.EntryType == global::System.Formats.Tar.TarEntryType.SymbolicLink || source.EntryType == global::System.Formats.Tar.TarEntryType.HardLink;
                entry.LastModified = source.ModificationTime;
                entry.HasLastModified = true;
                if (!builder.TryAdd(entry))
                {
                    return;
                }
            }
        }
    }

    internal static string NormalizeName(string name)
    {
        string result = name.Replace('\\', '/');
        while (result.StartsWith("./", global::System.StringComparison.Ordinal))
        {
            result = result.Substring(2);
        }

        return result;
    }

    internal static string GetGzipEntryName(string archivePath)
    {
        string name = global::System.IO.Path.GetFileName(archivePath);
        if (name.EndsWith(".gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return name.Substring(0, name.Length - 3);
        }

        return name;
    }

    internal static void ValidateRequest(string archivePath, RequestContext context, ExtractionLimits limits)
    {
        if (global::System.String.IsNullOrWhiteSpace(archivePath) || !global::System.IO.File.Exists(archivePath))
        {
            throw new ValidationException("An archive file is required.", "archivePath", "FILE_NOT_FOUND", null);
        }

        if (context == null)
        {
            throw new ValidationException("An archive request context is required.", "context", "REQUIRED", null);
        }

        if (limits == null || limits.MaximumEntries <= 0 || limits.MaximumEntryBytes < 0L || limits.MaximumTotalBytes < 0L || limits.MaximumCompressionRatio <= 0)
        {
            throw new ValidationException("Valid archive listing limits are required.", "limits", "INVALID_ARCHIVE_LIMIT", null);
        }
    }

    internal static void ThrowIfStopped(RequestContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (context.HasDeadline && global::System.DateTimeOffset.UtcNow >= context.Deadline)
        {
            throw new ValidationException("The archive listing deadline was exceeded.", "Deadline", "DEADLINE_EXCEEDED", null);
        }
    }
}
