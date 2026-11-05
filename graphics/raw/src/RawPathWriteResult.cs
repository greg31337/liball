namespace raw;

internal static class RawStagedPathWriter
{
    internal static RawPathWriteResult Write(string destinationPath, byte[] bytes, RawFormat format, System.Func<string, bool> validator)
    {
        string destination = NormalizeDestination(destinationPath);
        if (bytes == null || bytes.Length == 0 || format != RawFormat.GenericRaw && format != RawFormat.Cr2 && format != RawFormat.Nef || validator == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A nonempty generic RAW, CR2, or NEF output and staged-file validator are required.", -1);
        }
        string temporary = CreateTemporaryPath(destination);
        using (System.IO.FileStream output = CreateTemporaryFile(temporary))
        {
            output.Write(bytes, 0, bytes.Length);
            output.Flush(true);
        }
        if (!validator(temporary))
        {
            RawDiagnostic failure = new RawDiagnostic(RawDiagnosticCode.OutputValidationFailed, RawSeverity.Error, format, -1, string.Empty, -1, "DestinationPathWriter", "The sibling temporary file failed validation; the destination was not changed and the temporary path remains recoverable.", false);
            return new RawPathWriteResult(destination, temporary, false, false, new RawDiagnostic[] { failure });
        }
        System.IO.File.Move(temporary, destination, true);
        return new RawPathWriteResult(destination, string.Empty, true, true, null);
    }

    internal static RawPathWriteResult WriteValidated(string destinationPath, byte[] bytes, RawFormat format, RawWriteOptions options)
    {
        if (options == null || options.Limits == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Destination-path writing requires complete write options.", -1);
        }
        System.Func<string, bool> validator = path => Validate(path, bytes, format, options);
        return Write(destinationPath, bytes, format, validator);
    }

    internal static RawPathWriteResult WriteGenericValidated(string destinationPath, byte[] bytes, RawDescriptor descriptor, RawGenericEncodeOptions options)
    {
        if (options == null || options.Limits == null || descriptor == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Generic destination-path writing requires a descriptor and complete encoding options.", -1);
        }
        System.Func<string, bool> validator = path => ValidateGeneric(path, bytes, descriptor, options);
        return Write(destinationPath, bytes, RawFormat.GenericRaw, validator);
    }

    internal static string NormalizeDestination(string destinationPath)
    {
        if (destinationPath == null || destinationPath.Length == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A nonempty destination path is required.", -1);
        }
        string destination = System.IO.Path.GetFullPath(destinationPath);
        string fileName = System.IO.Path.GetFileName(destination);
        string? directory = System.IO.Path.GetDirectoryName(destination);
        if (fileName.Length == 0 || directory == null || directory.Length == 0 || !System.IO.Directory.Exists(directory))
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "The destination must name a file in an existing directory.", -1);
        }
        return destination;
    }

    internal static string CreateTemporaryPath(string destination)
    {
        string? directory = System.IO.Path.GetDirectoryName(destination);
        string fileName = System.IO.Path.GetFileName(destination);
        string token = System.Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        if (directory == null)
        {
            throw new System.InvalidOperationException("The absolute destination path has no directory.");
        }
        return System.IO.Path.Combine(directory, "." + fileName + "." + token + ".tmp");
    }

    internal static System.IO.FileStream CreateTemporaryFile(string path)
    {
        System.IO.FileStreamOptions options = new System.IO.FileStreamOptions();
        options.Mode = System.IO.FileMode.CreateNew;
        options.Access = System.IO.FileAccess.Write;
        options.Share = System.IO.FileShare.None;
        options.BufferSize = 81920;
        options.Options = System.IO.FileOptions.SequentialScan;
        if (!System.OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserWrite;
        }
        return new System.IO.FileStream(path, options);
    }

    internal static bool Validate(string path, byte[] expected, RawFormat format, RawWriteOptions options)
    {
        System.IO.FileInfo information = new System.IO.FileInfo(path);
        if (!information.Exists || information.Length != expected.LongLength || !SameFileBytes(path, expected))
        {
            return false;
        }
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.Limits = options.Limits;
        openOptions.CancellationToken = options.CancellationToken;
        openOptions.ReadMetadata = true;
        openOptions.ReadPreviews = true;
        openOptions.ReadRawPayload = true;
        openOptions.AllowExperimentalProfiles = true;
        RawDocument reopened = RawCodec.Open(path, null, openOptions);
        if (reopened.Format != format || reopened.TiffGraph == null)
        {
            return false;
        }
        if (format == RawFormat.Cr2)
        {
            return (reopened.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header != null && (reopened.FormatDetails.Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath.Length != 0;
        }
        return reopened.Metadata != null && reopened.PayloadMap != null && reopened.PayloadMap.Find(RawPayloadKind.RawImage).Length != 0;
    }

    internal static bool ValidateGeneric(string path, byte[] expected, RawDescriptor descriptor, RawGenericEncodeOptions options)
    {
        System.IO.FileInfo information = new System.IO.FileInfo(path);
        if (!information.Exists || information.Length != expected.LongLength || !SameFileBytes(path, expected) || !descriptor.ValidateSourceLength(expected.LongLength, options.Limits).IsValid())
        {
            return false;
        }
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.Limits = options.Limits;
        openOptions.CancellationToken = options.CancellationToken;
        openOptions.ReadMetadata = false;
        openOptions.ReadPreviews = false;
        openOptions.ReadRawPayload = true;
        RawDocument reopened = RawCodec.Open(path, descriptor, openOptions);
        RawValidationOptions validationOptions = new RawValidationOptions();
        validationOptions.Limits = options.Limits;
        validationOptions.CancellationToken = options.CancellationToken;
        validationOptions.ValidateProfile = false;
        validationOptions.ValidateMetadata = false;
        validationOptions.ValidateWriter = false;
        return reopened.Format == RawFormat.GenericRaw && RawCodec.Validate(reopened, validationOptions).IsValid();
    }

    internal static bool SameFileBytes(string path, byte[] expected)
    {
        byte[] buffer = new byte[81920];
        int position = 0;
        using (System.IO.FileStream input = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, buffer.Length, System.IO.FileOptions.SequentialScan))
        {
            int count = input.Read(buffer, 0, buffer.Length);
            while (count != 0)
            {
                int index = 0;
                while (index < count)
                {
                    if (position >= expected.Length || buffer[index] != expected[position])
                    {
                        return false;
                    }
                    position++;
                    index++;
                }
                count = input.Read(buffer, 0, buffer.Length);
            }
        }
        return position == expected.Length;
    }
}
