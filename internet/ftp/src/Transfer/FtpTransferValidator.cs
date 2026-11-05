namespace ftp;

internal static class FtpTransferValidator
{
    internal static void ValidateTransferOptions(FtpTransferOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        ValidateTransferEnums(options);
        FtpConfigurationValidator.ValidateTimeoutOptions(options.Timeouts);

        if (options.BufferBytes <= 0)
        {
            throw new FtpConfigurationException("The transfer buffer size must be positive.", null);
        }

        if (options.KeepAliveIntervalMilliseconds <= 0)
        {
            throw new FtpConfigurationException(
                "The transfer keepalive interval must be positive.", null);
        }

        if (options.Type == FtpTransferType.LocalByte)
        {
            if (!options.HasLocalByteSize || options.LocalByteSize < 1 || options.LocalByteSize > 255)
            {
                throw new FtpConfigurationException("TYPE L requires a local byte size between 1 and 255.", null);
            }
        }
        else if (options.HasLocalByteSize || options.LocalByteSize != 0)
        {
            throw new FtpConfigurationException("A local byte size is valid only for TYPE L.", null);
        }

        if ((options.Type == FtpTransferType.Image || options.Type == FtpTransferType.LocalByte) &&
            options.TextFormat != FtpTextFormat.NonPrint)
        {
            throw new FtpConfigurationException("Binary and local-byte transfers cannot use text format controls.", null);
        }

        ValidateTransferCombination(options);

        bool hasChecksumAlgorithm = !string.IsNullOrWhiteSpace(options.ExpectedChecksumAlgorithm);
        bool hasChecksum = !string.IsNullOrWhiteSpace(options.ExpectedChecksum);
        if (hasChecksumAlgorithm != hasChecksum)
        {
            throw new FtpConfigurationException("Expected checksum algorithm and value must be supplied together.", null);
        }

        if (hasChecksum && options.VerificationMode != FtpVerificationMode.Checksum)
        {
            throw new FtpConfigurationException("Expected checksum data requires checksum verification mode.", null);
        }

        if (options.VerificationMode == FtpVerificationMode.Checksum && !hasChecksumAlgorithm)
        {
            throw new FtpConfigurationException("Checksum verification requires an expected algorithm and value.", null);
        }

        if (FtpConfigurationValidator.HasInvalidOptionalText(options.ExpectedChecksumAlgorithm) ||
            FtpConfigurationValidator.HasInvalidOptionalText(options.ExpectedChecksum))
        {
            throw new FtpConfigurationException("Checksum configuration cannot contain control characters.", null);
        }

        if (options.AtomicUpload &&
            (options.CollisionPolicy == FtpCollisionPolicy.Append || options.CollisionPolicy == FtpCollisionPolicy.Resume))
        {
            throw new FtpConfigurationException("Atomic upload cannot be combined with append or resume collision behavior.", null);
        }
    }

    internal static void ValidateTransferRequest(FtpTransferRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException("A transfer request is required.", null);
        }

        ValidateDirection(request.Direction);
        ValidateRequiredPath(request.RemotePath, "remote transfer");
        ValidateTransferOptions(request.Options);

        bool hasLocalPath = !string.IsNullOrWhiteSpace(request.LocalPath);
        bool hasContent = request.Content != null;
        if (hasLocalPath == hasContent)
        {
            throw new FtpConfigurationException("A transfer requires exactly one local path or content stream.", null);
        }

        if (request.LocalPath != null && FtpConfigurationValidator.HasInvalidText(request.LocalPath))
        {
            throw new FtpConfigurationException("A local transfer path cannot contain control characters.", null);
        }

        if (request.HasRestartOffset)
        {
            if (request.RestartOffset < 0)
            {
                throw new FtpConfigurationException("A restart offset cannot be negative.", null);
            }

            if (request.Options.Mode == FtpTransferMode.Stream &&
                request.Options.Structure != FtpFileStructure.File)
            {
                throw new FtpConfigurationException("RFC 3659 stream restart offsets require file structure.", null);
            }

            if (request.Options.Mode == FtpTransferMode.Deflate &&
                request.Options.Structure != FtpFileStructure.File)
            {
                throw new FtpConfigurationException(
                    "MODE Z restart offsets require file structure.", null);
            }
        }
        else if (request.RestartOffset != 0)
        {
            throw new FtpConfigurationException("A restart offset value requires its availability flag.", null);
        }

        if (request.HasExpectedSize)
        {
            if (request.ExpectedSize < 0)
            {
                throw new FtpConfigurationException("An expected transfer size cannot be negative.", null);
            }
        }
        else if (request.ExpectedSize != 0)
        {
            throw new FtpConfigurationException("An expected size value requires its availability flag.", null);
        }

        if (request.Options.AtomicUpload && request.Direction != FtpTransferDirection.Upload)
        {
            throw new FtpConfigurationException("Atomic upload is valid only for upload requests.", null);
        }
    }

    internal static void ValidateDirectoryTransferOptions(FtpDirectoryTransferOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Directory-transfer options are required.", null);
        }

        ValidateTransferOptions(options.Transfer);
        if (options.SyncMode != FtpDirectorySyncMode.Copy &&
            options.SyncMode != FtpDirectorySyncMode.Update &&
            options.SyncMode != FtpDirectorySyncMode.Mirror)
        {
            throw new FtpConfigurationException("The directory synchronization mode is invalid.", null);
        }

        if (options.SymlinkPolicy != FtpSymlinkPolicy.Skip &&
            options.SymlinkPolicy != FtpSymlinkPolicy.Follow &&
            options.SymlinkPolicy != FtpSymlinkPolicy.Preserve &&
            options.SymlinkPolicy != FtpSymlinkPolicy.Fail)
        {
            throw new FtpConfigurationException("The symbolic-link policy is invalid.", null);
        }

        if (options.FailurePolicy != FtpFailurePolicy.Stop && options.FailurePolicy != FtpFailurePolicy.Continue)
        {
            throw new FtpConfigurationException("The directory failure policy is invalid.", null);
        }

        if (options.MaximumDepth < 0 || options.MaximumEntryCount <= 0)
        {
            throw new FtpConfigurationException("Directory traversal limits must be non-negative and bounded.", null);
        }

        if (options.DeleteExtraneous && options.SyncMode != FtpDirectorySyncMode.Mirror)
        {
            throw new FtpConfigurationException("Deleting extraneous entries requires mirror synchronization mode.", null);
        }

        if (options.DeleteExtraneous && !options.DryRun && !options.AllowRecursiveDeletion)
        {
            throw new FtpConfigurationException("Destructive directory synchronization requires explicit recursive-deletion opt-in.", null);
        }
    }

    internal static void ValidateDirectoryTransferRequest(FtpDirectoryTransferRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException("A directory-transfer request is required.", null);
        }

        ValidateDirection(request.Direction);
        ValidateRequiredPath(request.LocalPath, "local directory");
        ValidateRequiredPath(request.RemotePath, "remote directory");
        ValidateDirectoryTransferOptions(request.Options);
    }

    internal static void ValidateServerTransferRequest(FtpServerTransferRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException("A server-to-server transfer request is required.", null);
        }

        if (request.SourceClient == null || request.TargetClient == null)
        {
            throw new FtpConfigurationException("Server-to-server transfer requires source and target clients.", null);
        }

        if (object.ReferenceEquals(request.SourceClient, request.TargetClient))
        {
            throw new FtpConfigurationException("Server-to-server transfer requires distinct client sessions.", null);
        }

        ValidateRequiredPath(request.SourcePath, "source server");
        ValidateRequiredPath(request.TargetPath, "target server");
        ValidateTransferOptions(request.Options);

        if (request.Append && request.Unique)
        {
            throw new FtpConfigurationException("Append and unique server-to-server storage modes are mutually exclusive.", null);
        }
    }

    internal static void ValidateTransferEnums(FtpTransferOptions options)
    {
        if (options.Type != FtpTransferType.Ascii &&
            options.Type != FtpTransferType.Ebcdic &&
            options.Type != FtpTransferType.Image &&
            options.Type != FtpTransferType.LocalByte)
        {
            throw new FtpConfigurationException("The transfer representation type is invalid.", null);
        }

        if (options.TextFormat != FtpTextFormat.NonPrint &&
            options.TextFormat != FtpTextFormat.Telnet &&
            options.TextFormat != FtpTextFormat.CarriageControl)
        {
            throw new FtpConfigurationException("The transfer text format is invalid.", null);
        }

        if (options.Structure != FtpFileStructure.File &&
            options.Structure != FtpFileStructure.Record &&
            options.Structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("The FTP file structure is invalid.", null);
        }

        if (options.Mode != FtpTransferMode.Stream &&
            options.Mode != FtpTransferMode.Block &&
            options.Mode != FtpTransferMode.Compressed &&
            options.Mode != FtpTransferMode.Deflate)
        {
            throw new FtpConfigurationException("The FTP transfer mode is invalid.", null);
        }

        if (options.CollisionPolicy != FtpCollisionPolicy.Overwrite &&
            options.CollisionPolicy != FtpCollisionPolicy.Skip &&
            options.CollisionPolicy != FtpCollisionPolicy.Resume &&
            options.CollisionPolicy != FtpCollisionPolicy.Append &&
            options.CollisionPolicy != FtpCollisionPolicy.Fail &&
            options.CollisionPolicy != FtpCollisionPolicy.Unique)
        {
            throw new FtpConfigurationException("The transfer collision policy is invalid.", null);
        }

        if (options.ResumePolicy != FtpResumePolicy.Disabled &&
            options.ResumePolicy != FtpResumePolicy.IfSafe &&
            options.ResumePolicy != FtpResumePolicy.Required)
        {
            throw new FtpConfigurationException("The transfer resume policy is invalid.", null);
        }

        if (options.VerificationMode != FtpVerificationMode.None &&
            options.VerificationMode != FtpVerificationMode.Size &&
            options.VerificationMode != FtpVerificationMode.ModifiedTime &&
            options.VerificationMode != FtpVerificationMode.SizeAndModifiedTime &&
            options.VerificationMode != FtpVerificationMode.Checksum)
        {
            throw new FtpConfigurationException("The transfer verification mode is invalid.", null);
        }
    }

    internal static void ValidateTransferCombination(FtpTransferOptions options)
    {
        if (options.Structure == FtpFileStructure.Record &&
            options.Type != FtpTransferType.Ascii &&
            options.Type != FtpTransferType.Ebcdic)
        {
            throw new FtpConfigurationException("Record structure requires TYPE A or TYPE E.", null);
        }

        if (options.Structure == FtpFileStructure.Page &&
            options.Type == FtpTransferType.LocalByte &&
            options.LocalByteSize < 3)
        {
            throw new FtpConfigurationException("Page structure requires a logical byte size of at least three bits.", null);
        }
    }

    internal static void ValidateDirection(FtpTransferDirection direction)
    {
        if (direction != FtpTransferDirection.Download && direction != FtpTransferDirection.Upload)
        {
            throw new FtpConfigurationException("The transfer direction is invalid.", null);
        }
    }

    internal static void ValidateRequiredPath(string path, string pathName)
    {
        if (string.IsNullOrWhiteSpace(path) || FtpConfigurationValidator.HasInvalidText(path))
        {
            throw new FtpConfigurationException($"A valid {pathName} path is required.", null);
        }
    }
}
