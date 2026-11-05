namespace uspto;

internal class ArchiveDestinationValidation
{
    internal string? NormalizedName = null;
    internal string? DestinationPath = null;
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal static class ArchivePathValidator
{
    internal static ArchiveDestinationValidation NormalizeRelativeName(string name, bool isDirectory)
    {
        ArchiveDestinationValidation result = new ArchiveDestinationValidation();
        if (global::System.String.IsNullOrWhiteSpace(name))
        {
            return Fail(result, "ARCHIVE_ENTRY_NAME_INVALID", "The archive entry name is empty.");
        }

        string normalized = name.Replace('\\', '/');
        if (normalized.StartsWith("/", global::System.StringComparison.Ordinal) || global::System.IO.Path.IsPathRooted(normalized))
        {
            return Fail(result, "ARCHIVE_PATH_TRAVERSAL", "The archive entry uses an absolute path.");
        }

        string[] segments = normalized.Split('/', global::System.StringSplitOptions.None);
        global::System.Collections.Generic.List<string> accepted = new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < segments.Length; index += 1)
        {
            string segment = segments[index];
            bool finalDirectorySeparator = isDirectory && index == segments.Length - 1 && segment.Length == 0;
            if (finalDirectorySeparator)
            {
                continue;
            }

            if (segment.Length == 0)
            {
                return Fail(result, "ARCHIVE_ENTRY_NAME_INVALID", "The archive entry contains an empty path segment.");
            }

            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                return Fail(result, "ARCHIVE_PATH_TRAVERSAL", "The archive entry attempts to leave the destination directory.");
            }

            if (!IsSafeSegment(segment))
            {
                return Fail(result, "ARCHIVE_ENTRY_NAME_INVALID", "The archive entry contains a non-portable path segment.");
            }

            if (BulkFileNameSelector.IsBulkDeviceName(segment))
            {
                return Fail(result, "ARCHIVE_DEVICE_NAME_REJECTED", "The archive entry contains a reserved device name.");
            }

            accepted.Add(segment);
        }

        if (accepted.Count == 0)
        {
            return Fail(result, "ARCHIVE_ENTRY_NAME_INVALID", "The archive entry does not identify a destination.");
        }

        result.NormalizedName = global::System.String.Join("/", accepted);
        if (isDirectory)
        {
            result.NormalizedName += "/";
        }

        result.IsValid = true;
        return result;
    }

    internal static ArchiveDestinationValidation ValidateDestination(string destinationRoot, string name, bool isDirectory)
    {
        ArchiveDestinationValidation result = NormalizeRelativeName(name, isDirectory);
        if (!result.IsValid || result.NormalizedName == null)
        {
            return result;
        }

        string root = global::System.IO.Path.GetFullPath(destinationRoot);
        string relative = result.NormalizedName.Replace('/', global::System.IO.Path.DirectorySeparatorChar);
        string destination = global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(root, relative));
        string rootPrefix = root.EndsWith(global::System.IO.Path.DirectorySeparatorChar.ToString(), global::System.StringComparison.Ordinal) ? root : root + global::System.IO.Path.DirectorySeparatorChar;
        if (!global::System.String.Equals(destination, root, global::System.StringComparison.OrdinalIgnoreCase) && !destination.StartsWith(rootPrefix, global::System.StringComparison.OrdinalIgnoreCase))
        {
            return Fail(result, "ARCHIVE_PATH_TRAVERSAL", "The archive entry resolves outside the destination directory.");
        }

        if (ContainsReparsePoint(root, destination, isDirectory))
        {
            return Fail(result, "ARCHIVE_REPARSE_POINT_REJECTED", "The archive entry destination contains a symbolic link or reparse point.");
        }

        result.DestinationPath = destination;
        return result;
    }

    internal static bool IsSafeSegment(string segment)
    {
        if (segment.EndsWith(" ", global::System.StringComparison.Ordinal) || segment.EndsWith(".", global::System.StringComparison.Ordinal))
        {
            return false;
        }

        int index;
        for (index = 0; index < segment.Length; index += 1)
        {
            char value = segment[index];
            if (global::System.Char.IsControl(value) || global::System.Char.GetUnicodeCategory(value) == global::System.Globalization.UnicodeCategory.Format || value == ':' || value == '<' || value == '>' || value == '"' || value == '|' || value == '?' || value == '*')
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ContainsReparsePoint(string root, string destination, bool isDirectory)
    {
        if (IsReparsePoint(root))
        {
            return true;
        }

        string relative = global::System.IO.Path.GetRelativePath(root, destination);
        string[] segments = relative.Split(global::System.IO.Path.DirectorySeparatorChar, global::System.StringSplitOptions.RemoveEmptyEntries);
        string current = root;
        int count = isDirectory ? segments.Length : segments.Length - 1;
        int index;
        for (index = 0; index < count; index += 1)
        {
            current = global::System.IO.Path.Combine(current, segments[index]);
            if (IsReparsePoint(current))
            {
                return true;
            }
        }

        if (!isDirectory && IsReparsePoint(destination))
        {
            return true;
        }

        return false;
    }

    internal static bool IsReparsePoint(string path)
    {
        if (!global::System.IO.File.Exists(path) && !global::System.IO.Directory.Exists(path))
        {
            return false;
        }

        global::System.IO.FileAttributes attributes = global::System.IO.File.GetAttributes(path);
        return (attributes & global::System.IO.FileAttributes.ReparsePoint) != 0;
    }

    internal static ArchiveDestinationValidation Fail(ArchiveDestinationValidation result, string code, string message)
    {
        result.IsValid = false;
        result.DestinationPath = null;
        result.FailureCode = code;
        result.FailureMessage = message;
        return result;
    }
}

internal class AtomicArchiveExtractionFile : global::System.IDisposable
{
    internal string DestinationPath;
    internal string TemporaryPath;
    internal global::System.IO.FileStream? Output = null;
    internal bool IsCommitted = false;

    internal AtomicArchiveExtractionFile(string destinationPath)
    {
        DestinationPath = destinationPath;
        TemporaryPath = destinationPath + "." + global::System.Guid.NewGuid().ToString("N") + ".partial";
        Output = new global::System.IO.FileStream(TemporaryPath, global::System.IO.FileMode.CreateNew, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None, UsptoDefaults.DownloadBufferSize, global::System.IO.FileOptions.SequentialScan);
    }

    internal void Commit(bool overwrite)
    {
        if (Output == null)
        {
            throw new ValidationException("The archive extraction temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
        }

        Output.Flush(true);
        Output.Dispose();
        Output = null;
        global::System.IO.File.Move(TemporaryPath, DestinationPath, overwrite);
        IsCommitted = true;
    }

    public void Dispose()
    {
        if (Output != null)
        {
            Output.Dispose();
            Output = null;
        }

        if (!IsCommitted && global::System.IO.File.Exists(TemporaryPath))
        {
            global::System.IO.File.Delete(TemporaryPath);
        }
    }
}

internal class ArchiveExtractionState
{
    internal ExtractionOptions Options;
    internal string DestinationRoot;
    internal global::System.Collections.Generic.HashSet<string> SelectedNames;
    internal global::System.Collections.Generic.HashSet<string> MatchedNames = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
    internal global::System.Collections.Generic.HashSet<string> DestinationNames = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
    internal global::System.Collections.Generic.List<ExtractionEntryResult> EntryResults = new global::System.Collections.Generic.List<ExtractionEntryResult>();
    internal long ObservedEntries = 0L;
    internal long ObservedTotalBytes = 0L;
    internal long ObservedMaximumEntryBytes = 0L;
    internal long ObservedMaximumRatio = 0L;
    internal ArchiveEntry? MaximumEntry = null;
    internal ArchiveEntry? MaximumRatioEntry = null;
    internal long BytesWritten = 0L;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
    internal bool IsCancelled = false;

    internal ArchiveExtractionState(ExtractionOptions options, string destinationRoot, global::System.Collections.Generic.HashSet<string> selectedNames)
    {
        Options = options;
        DestinationRoot = destinationRoot;
        SelectedNames = selectedNames;
    }

    internal bool ObserveEntry(ArchiveEntry entry)
    {
        ObservedEntries += 1L;
        if (ObservedEntries > Options.Limits.MaximumEntries)
        {
            SetFailure("ARCHIVE_ENTRY_LIMIT_EXCEEDED", "The archive contains more entries than the configured limit.");
            return false;
        }

        long size = entry.HasUncompressedSize ? entry.UncompressedSize : 0L;
        if (size > ObservedMaximumEntryBytes)
        {
            ObservedMaximumEntryBytes = size;
            MaximumEntry = entry;
        }

        if (size < 0L || size > Options.Limits.MaximumEntryBytes)
        {
            SetFailure("ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED", "An archive entry exceeds the configured uncompressed-size limit.");
            return false;
        }

        if (size > Options.Limits.MaximumTotalBytes || ObservedTotalBytes > Options.Limits.MaximumTotalBytes - size)
        {
            ObservedTotalBytes = global::System.Int64.MaxValue;
            SetFailure("ARCHIVE_TOTAL_SIZE_LIMIT_EXCEEDED", "The archive exceeds the configured total uncompressed-size limit.");
            return false;
        }

        ObservedTotalBytes += size;
        long ratio = CalculateRatio(entry);
        if (ratio > ObservedMaximumRatio)
        {
            ObservedMaximumRatio = ratio;
            MaximumRatioEntry = entry;
        }

        if (ratio > Options.Limits.MaximumCompressionRatio)
        {
            SetFailure("ARCHIVE_COMPRESSION_RATIO_EXCEEDED", "A ZIP archive entry exceeds the configured compression-ratio limit.");
            return false;
        }

        return true;
    }

    internal long CalculateRatio(ArchiveEntry entry)
    {
        if (!entry.HasCompressedSize || !entry.HasUncompressedSize || entry.UncompressedSize == 0L)
        {
            return 0L;
        }

        if (entry.CompressedSize <= 0L)
        {
            return global::System.Int64.MaxValue;
        }

        long ratio = entry.UncompressedSize / entry.CompressedSize;
        if (entry.UncompressedSize % entry.CompressedSize != 0L)
        {
            ratio += 1L;
        }

        return ratio;
    }

    internal bool ShouldExtract(string normalizedName)
    {
        if (Options.ExtractAll)
        {
            return true;
        }

        if (SelectedNames.Contains(normalizedName))
        {
            MatchedNames.Add(normalizedName);
            return true;
        }

        return false;
    }

    internal bool CheckStopped()
    {
        if (FailureCode != null || IsCancelled)
        {
            return true;
        }

        if (Options.Context.CancellationToken.IsCancellationRequested)
        {
            IsCancelled = true;
            FailureCode = "CANCELLED";
            FailureMessage = "The archive extraction was cancelled.";
            return true;
        }

        if (Options.Context.HasDeadline && global::System.DateTimeOffset.UtcNow >= Options.Context.Deadline)
        {
            SetFailure("DEADLINE_EXCEEDED", "The archive extraction deadline was exceeded.");
            return true;
        }

        return false;
    }

    internal void SetFailure(string code, string message)
    {
        if (FailureCode == null)
        {
            FailureCode = code;
            FailureMessage = message;
        }
    }

    internal ArchiveSafetyLimitResult[] CreateSafetyResults()
    {
        ArchiveSafetyLimitResult entries = new ArchiveSafetyLimitResult();
        entries.LimitName = "MaximumEntries";
        entries.ObservedValue = ObservedEntries;
        entries.LimitValue = Options.Limits.MaximumEntries;
        entries.IsAccepted = ObservedEntries <= Options.Limits.MaximumEntries && FailureCode != "ARCHIVE_ENTRY_LIMIT_EXCEEDED";

        ArchiveSafetyLimitResult entryBytes = new ArchiveSafetyLimitResult();
        entryBytes.LimitName = "MaximumEntryBytes";
        entryBytes.ObservedValue = ObservedMaximumEntryBytes;
        entryBytes.LimitValue = Options.Limits.MaximumEntryBytes;
        entryBytes.IsAccepted = ObservedMaximumEntryBytes <= Options.Limits.MaximumEntryBytes && FailureCode != "ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED";
        entryBytes.Entry = MaximumEntry;

        ArchiveSafetyLimitResult totalBytes = new ArchiveSafetyLimitResult();
        totalBytes.LimitName = "MaximumTotalBytes";
        totalBytes.ObservedValue = ObservedTotalBytes;
        totalBytes.LimitValue = Options.Limits.MaximumTotalBytes;
        totalBytes.IsAccepted = ObservedTotalBytes <= Options.Limits.MaximumTotalBytes && FailureCode != "ARCHIVE_TOTAL_SIZE_LIMIT_EXCEEDED";

        ArchiveSafetyLimitResult ratio = new ArchiveSafetyLimitResult();
        ratio.LimitName = "MaximumCompressionRatio";
        ratio.ObservedValue = ObservedMaximumRatio;
        ratio.LimitValue = Options.Limits.MaximumCompressionRatio;
        ratio.IsAccepted = ObservedMaximumRatio <= Options.Limits.MaximumCompressionRatio && FailureCode != "ARCHIVE_COMPRESSION_RATIO_EXCEEDED";
        ratio.Entry = MaximumRatioEntry;
        return new ArchiveSafetyLimitResult[] { entries, entryBytes, totalBytes, ratio };
    }
}

internal static class ZipExtractionOperation
{
    internal static ExtractionResult Extract(ExtractionOptions options)
    {
        ValidateOptions(options);
        string archivePath = global::System.IO.Path.GetFullPath(options.ArchivePath ?? string.Empty);
        string destinationRoot = global::System.IO.Path.GetFullPath(options.DestinationDirectory ?? string.Empty);
        CompressionDetectionResult detection = ArchiveFormatDetector.Detect(archivePath);
        ExtractionResult result = new ExtractionResult();
        result.Options = options;
        result.Detection = detection;
        if (detection.Format != CompressionFormat.Zip || !detection.IsSupported)
        {
            result.FailureCode = "UNSUPPORTED_ARCHIVE_FORMAT";
            result.FailureMessage = "ZIP extraction requires a ZIP archive.";
            return result;
        }

        if (global::System.IO.File.Exists(destinationRoot))
        {
            throw new ValidationException("The archive destination must be a directory.", "DestinationDirectory", "ARCHIVE_DESTINATION_INVALID", null);
        }

        global::System.Collections.Generic.HashSet<string> selectedNames = CreateSelectedNames(options);
        ArchiveExtractionState state = new ArchiveExtractionState(options, destinationRoot, selectedNames);
        if (!state.CheckStopped())
        {
            global::System.IO.Directory.CreateDirectory(destinationRoot);
            ExtractArchive(archivePath, state);
        }

        if (!state.CheckStopped() && !options.ExtractAll && state.MatchedNames.Count != state.SelectedNames.Count)
        {
            state.SetFailure("ARCHIVE_ENTRY_NOT_FOUND", "One or more selected ZIP entries were not found.");
        }

        result.Entries = state.EntryResults.ToArray();
        result.SafetyLimits = state.CreateSafetyResults();
        result.BytesWritten = state.BytesWritten;
        result.IsCancelled = state.IsCancelled;
        result.FailureCode = state.FailureCode;
        result.FailureMessage = state.FailureMessage;
        result.IsComplete = state.FailureCode == null && !state.IsCancelled;
        return result;
    }

    internal static void ExtractArchive(string archivePath, ArchiveExtractionState state)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        using (global::System.IO.Compression.ZipArchive archive = new global::System.IO.Compression.ZipArchive(input, global::System.IO.Compression.ZipArchiveMode.Read, false))
        {
            foreach (global::System.IO.Compression.ZipArchiveEntry source in archive.Entries)
            {
                if (state.CheckStopped())
                {
                    return;
                }

                ArchiveEntry entry = CreateArchiveEntry(source);
                ExtractionEntryResult entryResult = new ExtractionEntryResult();
                entryResult.Entry = entry;
                state.EntryResults.Add(entryResult);
                if (!state.ObserveEntry(entry))
                {
                    ApplyFailure(entryResult, state.FailureCode ?? "ARCHIVE_LIMIT_EXCEEDED", state.FailureMessage ?? "A ZIP archive safety limit was exceeded.");
                    return;
                }

                ArchiveDestinationValidation normalizedEntry = ArchivePathValidator.NormalizeRelativeName(source.FullName, entry.IsDirectory);
                string normalizedName = normalizedEntry.IsValid ? normalizedEntry.NormalizedName ?? string.Empty : entry.Name ?? string.Empty;
                if (!state.ShouldExtract(normalizedName))
                {
                    entryResult.IsSkipped = true;
                    continue;
                }

                ArchiveDestinationValidation validation = ArchivePathValidator.ValidateDestination(state.DestinationRoot, source.FullName, entry.IsDirectory);
                if (!validation.IsValid || validation.DestinationPath == null)
                {
                    ApplyFailure(entryResult, validation.FailureCode ?? "ARCHIVE_ENTRY_NAME_INVALID", validation.FailureMessage ?? "The ZIP entry destination is invalid.");
                    state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_ENTRY_NAME_INVALID", entryResult.FailureMessage ?? "The ZIP entry destination is invalid.");
                    return;
                }

                entry.Name = validation.NormalizedName;
                entryResult.DestinationPath = validation.DestinationPath;
                if (!state.DestinationNames.Add(validation.DestinationPath))
                {
                    if (state.Options.Limits.RejectDuplicateNames)
                    {
                        ApplyFailure(entryResult, "ARCHIVE_DUPLICATE_ENTRY", "The ZIP archive contains duplicate canonical entry names.");
                        state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_DUPLICATE_ENTRY", entryResult.FailureMessage ?? "The ZIP archive contains duplicate canonical entry names.");
                        return;
                    }

                    entryResult.IsSkipped = true;
                    continue;
                }

                int unixType = GetUnixType(source);
                if (entry.IsLink)
                {
                    ApplyFailure(entryResult, "ARCHIVE_LINK_REJECTED", "ZIP symbolic links are not materialized.");
                    state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_LINK_REJECTED", entryResult.FailureMessage ?? "ZIP symbolic links are not materialized.");
                    return;
                }

                if (unixType != 0 && unixType != 0x4000 && unixType != 0x8000)
                {
                    ApplyFailure(entryResult, "ARCHIVE_SPECIAL_ENTRY_REJECTED", "The ZIP entry represents an unsupported filesystem object.");
                    state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_SPECIAL_ENTRY_REJECTED", entryResult.FailureMessage ?? "The ZIP entry represents an unsupported filesystem object.");
                    return;
                }

                if (entry.IsDirectory)
                {
                    ExtractDirectory(entryResult, state);
                }
                else
                {
                    ExtractFile(source, entryResult, state);
                }

                if (entryResult.FailureCode != null || state.CheckStopped())
                {
                    return;
                }
            }
        }
    }

    internal static ArchiveEntry CreateArchiveEntry(global::System.IO.Compression.ZipArchiveEntry source)
    {
        ArchiveEntry entry = new ArchiveEntry();
        entry.Name = ArchiveListingOperation.NormalizeName(source.FullName);
        entry.CompressedSize = source.CompressedLength;
        entry.HasCompressedSize = true;
        entry.UncompressedSize = source.Length;
        entry.HasUncompressedSize = true;
        entry.IsDirectory = source.Name.Length == 0 || source.FullName.EndsWith("/", global::System.StringComparison.Ordinal) || source.FullName.EndsWith("\\", global::System.StringComparison.Ordinal);
        entry.IsLink = GetUnixType(source) == 0xa000;
        entry.LastModified = source.LastWriteTime;
        entry.HasLastModified = true;
        return entry;
    }

    internal static int GetUnixType(global::System.IO.Compression.ZipArchiveEntry source)
    {
        return source.ExternalAttributes >> 16 & 0xf000;
    }

    internal static void ExtractDirectory(ExtractionEntryResult entryResult, ArchiveExtractionState state)
    {
        string destination = entryResult.DestinationPath ?? string.Empty;
        if (global::System.IO.File.Exists(destination))
        {
            ApplyFailure(entryResult, "ARCHIVE_DESTINATION_CONFLICT", "A file already occupies the archive directory destination.");
            state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_DESTINATION_CONFLICT", entryResult.FailureMessage ?? "The archive directory destination is unavailable.");
            return;
        }

        global::System.IO.Directory.CreateDirectory(destination);
        entryResult.IsExtracted = true;
    }

    internal static void ExtractFile(global::System.IO.Compression.ZipArchiveEntry source, ExtractionEntryResult entryResult, ArchiveExtractionState state)
    {
        string destination = entryResult.DestinationPath ?? string.Empty;
        if (global::System.IO.Directory.Exists(destination))
        {
            ApplyFailure(entryResult, "ARCHIVE_DESTINATION_CONFLICT", "A directory already occupies the ZIP file destination.");
            state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_DESTINATION_CONFLICT", entryResult.FailureMessage ?? "The ZIP file destination is unavailable.");
            return;
        }

        if (global::System.IO.File.Exists(destination) && !state.Options.OverwriteExisting)
        {
            ApplyFailure(entryResult, "ARCHIVE_DESTINATION_EXISTS", "The ZIP file destination already exists.");
            state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_DESTINATION_EXISTS", entryResult.FailureMessage ?? "The ZIP file destination already exists.");
            return;
        }

        string? parent = global::System.IO.Path.GetDirectoryName(destination);
        if (parent == null)
        {
            ApplyFailure(entryResult, "ARCHIVE_DESTINATION_INVALID", "The ZIP file destination directory is invalid.");
            state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_DESTINATION_INVALID", entryResult.FailureMessage ?? "The ZIP file destination directory is invalid.");
            return;
        }

        global::System.IO.Directory.CreateDirectory(parent);
        using (global::System.IO.Stream input = source.Open())
        using (AtomicArchiveExtractionFile atomicFile = new AtomicArchiveExtractionFile(destination))
        {
            if (atomicFile.Output == null)
            {
                throw new ValidationException("The archive extraction temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
            }

            byte[] buffer = new byte[UsptoDefaults.DownloadBufferSize];
            long written = 0L;
            while (true)
            {
                if (state.CheckStopped())
                {
                    ApplyFailure(entryResult, state.FailureCode ?? "CANCELLED", state.FailureMessage ?? "The ZIP extraction was stopped.");
                    return;
                }

                int read = input.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                if (written > state.Options.Limits.MaximumEntryBytes - read || written > source.Length - read)
                {
                    ApplyFailure(entryResult, "ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED", "The ZIP entry produced more bytes than permitted or declared.");
                    state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED", entryResult.FailureMessage ?? "The ZIP entry size is invalid.");
                    return;
                }

                atomicFile.Output.Write(buffer, 0, read);
                written += read;
            }

            if (written != source.Length)
            {
                ApplyFailure(entryResult, "ARCHIVE_ENTRY_LENGTH_MISMATCH", "The extracted ZIP entry length does not match its declared size.");
                state.SetFailure(entryResult.FailureCode ?? "ARCHIVE_ENTRY_LENGTH_MISMATCH", entryResult.FailureMessage ?? "The extracted ZIP entry length is invalid.");
                return;
            }

            atomicFile.Commit(state.Options.OverwriteExisting);
            state.BytesWritten += written;
            entryResult.IsExtracted = true;
        }
    }

    internal static global::System.Collections.Generic.HashSet<string> CreateSelectedNames(ExtractionOptions options)
    {
        global::System.Collections.Generic.HashSet<string> result = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < options.SelectedEntries.Length; index += 1)
        {
            string? selected = options.SelectedEntries[index];
            if (selected == null)
            {
                throw new ValidationException("Selected archive entry names cannot be null.", "SelectedEntries", "ARCHIVE_SELECTION_INVALID", null);
            }

            bool isDirectory = selected.EndsWith("/", global::System.StringComparison.Ordinal) || selected.EndsWith("\\", global::System.StringComparison.Ordinal);
            ArchiveDestinationValidation normalized = ArchivePathValidator.NormalizeRelativeName(selected, isDirectory);
            if (!normalized.IsValid || normalized.NormalizedName == null)
            {
                throw new ValidationException("A selected archive entry name is invalid.", "SelectedEntries", "ARCHIVE_SELECTION_INVALID", null);
            }

            result.Add(normalized.NormalizedName);
        }

        return result;
    }

    internal static void ApplyFailure(ExtractionEntryResult result, string code, string message)
    {
        result.IsExtracted = false;
        result.IsSkipped = false;
        result.FailureCode = code;
        result.FailureMessage = message;
    }

    internal static void ValidateOptions(ExtractionOptions options)
    {
        if (options == null)
        {
            throw new ValidationException("Archive extraction options are required.", "options", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(options.ArchivePath) || !global::System.IO.File.Exists(options.ArchivePath))
        {
            throw new ValidationException("A ZIP archive file is required.", "ArchivePath", "FILE_NOT_FOUND", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(options.DestinationDirectory))
        {
            throw new ValidationException("A ZIP extraction destination directory is required.", "DestinationDirectory", "REQUIRED", null);
        }

        if (options.Context == null)
        {
            throw new ValidationException("A ZIP extraction request context is required.", "Context", "REQUIRED", null);
        }

        if (options.Limits == null || options.Limits.MaximumEntries <= 0 || options.Limits.MaximumEntryBytes < 0L || options.Limits.MaximumTotalBytes < 0L || options.Limits.MaximumCompressionRatio <= 0)
        {
            throw new ValidationException("Valid ZIP extraction limits are required.", "Limits", "INVALID_ARCHIVE_LIMIT", null);
        }

        if (options.SelectedEntries == null)
        {
            throw new ValidationException("Selected archive entry names are required.", "SelectedEntries", "ARCHIVE_SELECTION_INVALID", null);
        }
    }
}
