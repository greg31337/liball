namespace uspto;

internal static class TarExtractionOperation
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
        if ((detection.Format != CompressionFormat.Tar && detection.Format != CompressionFormat.TarGzip) || !detection.IsSupported)
        {
            result.FailureCode = "UNSUPPORTED_ARCHIVE_FORMAT";
            result.FailureMessage = "Tar extraction requires a tar or gzip-compressed tar archive.";
            return result;
        }

        if (global::System.IO.File.Exists(destinationRoot))
        {
            throw new ValidationException("The archive destination must be a directory.", "DestinationDirectory", "ARCHIVE_DESTINATION_INVALID", null);
        }

        global::System.Collections.Generic.HashSet<string> selectedNames = ZipExtractionOperation.CreateSelectedNames(options);
        ArchiveExtractionState state = new ArchiveExtractionState(options, destinationRoot, selectedNames);
        if (!state.CheckStopped())
        {
            global::System.IO.Directory.CreateDirectory(destinationRoot);
            if (detection.Format == CompressionFormat.Tar)
            {
                ExtractTarFile(archivePath, state);
            }
            else
            {
                ExtractTarGzip(archivePath, state);
            }
        }

        if (!state.CheckStopped() && !options.ExtractAll && state.MatchedNames.Count != state.SelectedNames.Count)
        {
            state.SetFailure("ARCHIVE_ENTRY_NOT_FOUND", "One or more selected tar entries were not found.");
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

    internal static void ExtractTarFile(string archivePath, ArchiveExtractionState state)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        {
            ExtractTarStream(input, state);
        }
    }

    internal static void ExtractTarGzip(string archivePath, ArchiveExtractionState state)
    {
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(archivePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        using (global::System.IO.Compression.GZipStream gzip = new global::System.IO.Compression.GZipStream(input, global::System.IO.Compression.CompressionMode.Decompress, false))
        {
            ExtractTarStream(gzip, state);
        }
    }

    internal static void ExtractTarStream(global::System.IO.Stream input, ArchiveExtractionState state)
    {
        using (global::System.Formats.Tar.TarReader reader = new global::System.Formats.Tar.TarReader(input, true))
        {
            while (true)
            {
                if (state.CheckStopped())
                {
                    return;
                }

                global::System.Formats.Tar.TarEntry? source = reader.GetNextEntry(false);
                if (source == null)
                {
                    return;
                }

                ArchiveEntry entry = CreateArchiveEntry(source);
                ExtractionEntryResult entryResult = new ExtractionEntryResult();
                entryResult.Entry = entry;
                state.EntryResults.Add(entryResult);
                if (!state.ObserveEntry(entry))
                {
                    ZipExtractionOperation.ApplyFailure(entryResult, state.FailureCode ?? "ARCHIVE_LIMIT_EXCEEDED", state.FailureMessage ?? "A tar archive safety limit was exceeded.");
                    return;
                }

                ArchiveDestinationValidation normalizedEntry = ArchivePathValidator.NormalizeRelativeName(source.Name, entry.IsDirectory);
                string normalizedName = normalizedEntry.IsValid ? normalizedEntry.NormalizedName ?? string.Empty : entry.Name ?? string.Empty;
                if (!state.ShouldExtract(normalizedName))
                {
                    entryResult.IsSkipped = true;
                    continue;
                }

                ArchiveDestinationValidation validation = ArchivePathValidator.ValidateDestination(state.DestinationRoot, source.Name, entry.IsDirectory);
                if (!validation.IsValid || validation.DestinationPath == null)
                {
                    SetEntryFailure(entryResult, state, validation.FailureCode ?? "ARCHIVE_ENTRY_NAME_INVALID", validation.FailureMessage ?? "The tar entry destination is invalid.");
                    return;
                }

                entry.Name = validation.NormalizedName;
                entryResult.DestinationPath = validation.DestinationPath;
                if (!state.DestinationNames.Add(validation.DestinationPath))
                {
                    if (state.Options.Limits.RejectDuplicateNames)
                    {
                        SetEntryFailure(entryResult, state, "ARCHIVE_DUPLICATE_ENTRY", "The tar archive contains duplicate canonical entry names.");
                        return;
                    }

                    entryResult.IsSkipped = true;
                    continue;
                }

                if (entry.IsLink)
                {
                    SetEntryFailure(entryResult, state, "ARCHIVE_LINK_REJECTED", "Tar symbolic and hard links are not materialized.");
                    return;
                }

                if (IsDeviceEntry(source.EntryType))
                {
                    SetEntryFailure(entryResult, state, "ARCHIVE_DEVICE_ENTRY_REJECTED", "Tar device and FIFO entries are not materialized.");
                    return;
                }

                if (entry.IsDirectory)
                {
                    ZipExtractionOperation.ExtractDirectory(entryResult, state);
                }
                else if (IsRegularFile(source.EntryType))
                {
                    ExtractRegularFile(source, entryResult, state);
                }
                else
                {
                    SetEntryFailure(entryResult, state, "ARCHIVE_SPECIAL_ENTRY_REJECTED", "The tar entry type is not safe to materialize.");
                    return;
                }

                if (entryResult.FailureCode != null || state.CheckStopped())
                {
                    return;
                }
            }
        }
    }

    internal static ArchiveEntry CreateArchiveEntry(global::System.Formats.Tar.TarEntry source)
    {
        ArchiveEntry entry = new ArchiveEntry();
        entry.Name = ArchiveListingOperation.NormalizeName(source.Name);
        entry.UncompressedSize = source.Length;
        entry.HasUncompressedSize = true;
        entry.IsDirectory = source.EntryType == global::System.Formats.Tar.TarEntryType.Directory;
        entry.IsLink = source.EntryType == global::System.Formats.Tar.TarEntryType.SymbolicLink || source.EntryType == global::System.Formats.Tar.TarEntryType.HardLink;
        entry.LastModified = source.ModificationTime;
        entry.HasLastModified = true;
        return entry;
    }

    internal static bool IsRegularFile(global::System.Formats.Tar.TarEntryType entryType)
    {
        return entryType == global::System.Formats.Tar.TarEntryType.RegularFile || entryType == global::System.Formats.Tar.TarEntryType.V7RegularFile || entryType == global::System.Formats.Tar.TarEntryType.ContiguousFile;
    }

    internal static bool IsDeviceEntry(global::System.Formats.Tar.TarEntryType entryType)
    {
        return entryType == global::System.Formats.Tar.TarEntryType.BlockDevice || entryType == global::System.Formats.Tar.TarEntryType.CharacterDevice || entryType == global::System.Formats.Tar.TarEntryType.Fifo;
    }

    internal static void ExtractRegularFile(global::System.Formats.Tar.TarEntry source, ExtractionEntryResult entryResult, ArchiveExtractionState state)
    {
        string destination = entryResult.DestinationPath ?? string.Empty;
        if (global::System.IO.Directory.Exists(destination))
        {
            SetEntryFailure(entryResult, state, "ARCHIVE_DESTINATION_CONFLICT", "A directory already occupies the tar file destination.");
            return;
        }

        if (global::System.IO.File.Exists(destination) && !state.Options.OverwriteExisting)
        {
            SetEntryFailure(entryResult, state, "ARCHIVE_DESTINATION_EXISTS", "The tar file destination already exists.");
            return;
        }

        string? parent = global::System.IO.Path.GetDirectoryName(destination);
        if (parent == null)
        {
            SetEntryFailure(entryResult, state, "ARCHIVE_DESTINATION_INVALID", "The tar file destination directory is invalid.");
            return;
        }

        global::System.IO.Directory.CreateDirectory(parent);
        using (AtomicArchiveExtractionFile atomicFile = new AtomicArchiveExtractionFile(destination))
        {
            if (atomicFile.Output == null)
            {
                throw new ValidationException("The archive extraction temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
            }

            global::System.IO.Stream? input = source.DataStream;
            if (input == null && source.Length != 0L)
            {
                SetEntryFailure(entryResult, state, "ARCHIVE_ENTRY_DATA_MISSING", "The tar entry data stream is unavailable.");
                return;
            }

            byte[] buffer = new byte[UsptoDefaults.DownloadBufferSize];
            long written = 0L;
            while (input != null)
            {
                if (state.CheckStopped())
                {
                    ZipExtractionOperation.ApplyFailure(entryResult, state.FailureCode ?? "CANCELLED", state.FailureMessage ?? "The tar extraction was stopped.");
                    return;
                }

                int read = input.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                if (written > state.Options.Limits.MaximumEntryBytes - read || written > source.Length - read)
                {
                    SetEntryFailure(entryResult, state, "ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED", "The tar entry produced more bytes than permitted or declared.");
                    return;
                }

                atomicFile.Output.Write(buffer, 0, read);
                written += read;
            }

            if (written != source.Length)
            {
                SetEntryFailure(entryResult, state, "ARCHIVE_ENTRY_LENGTH_MISMATCH", "The extracted tar entry length does not match its declared size.");
                return;
            }

            atomicFile.Commit(state.Options.OverwriteExisting);
            state.BytesWritten += written;
            entryResult.IsExtracted = true;
        }
    }

    internal static void SetEntryFailure(ExtractionEntryResult entryResult, ArchiveExtractionState state, string code, string message)
    {
        ZipExtractionOperation.ApplyFailure(entryResult, code, message);
        state.SetFailure(code, message);
    }

    internal static void ValidateOptions(ExtractionOptions options)
    {
        if (options == null)
        {
            throw new ValidationException("Archive extraction options are required.", "options", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(options.ArchivePath) || !global::System.IO.File.Exists(options.ArchivePath))
        {
            throw new ValidationException("A tar archive file is required.", "ArchivePath", "FILE_NOT_FOUND", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(options.DestinationDirectory))
        {
            throw new ValidationException("A tar extraction destination directory is required.", "DestinationDirectory", "REQUIRED", null);
        }

        if (options.Context == null)
        {
            throw new ValidationException("A tar extraction request context is required.", "Context", "REQUIRED", null);
        }

        if (options.Limits == null || options.Limits.MaximumEntries <= 0 || options.Limits.MaximumEntryBytes < 0L || options.Limits.MaximumTotalBytes < 0L || options.Limits.MaximumCompressionRatio <= 0)
        {
            throw new ValidationException("Valid tar extraction limits are required.", "Limits", "INVALID_ARCHIVE_LIMIT", null);
        }

        if (options.SelectedEntries == null)
        {
            throw new ValidationException("Selected archive entry names are required.", "SelectedEntries", "ARCHIVE_SELECTION_INVALID", null);
        }
    }
}
