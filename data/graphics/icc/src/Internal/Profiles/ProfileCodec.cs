namespace icc;

internal static class ProfileCodec
{
    internal static IccProfile ReadEnvelope(System.ReadOnlyMemory<byte> source, int profileOffset, long absoluteBaseOffset, IccReadOptions options)
    {
        return ReadEnvelopeWithDepth(source, profileOffset, absoluteBaseOffset, options, 0);
    }

    internal static IccProfile ReadEnvelopeWithDepth(System.ReadOnlyMemory<byte> source, int profileOffset, long absoluteBaseOffset, IccReadOptions options, int currentNestingDepth)
    {
        ValidateEnvelopeOptions(options);
        ReadDiagnosticCollector diagnostics = new ReadDiagnosticCollector(options.ResourceLimits.MaximumDiagnosticCount);
        if (profileOffset < 0 || profileOffset > source.Length)
        {
            throw new IccProfileFormatException($"The embedded profile offset {profileOffset} is outside the {source.Length}-byte input.");
        }

        CheckedSize.AddOffsets(absoluteBaseOffset, source.Length, long.MaxValue);
        long absoluteProfileOffset = CheckedSize.AddOffsets(absoluteBaseOffset, profileOffset, long.MaxValue);
        int availableBytes = source.Length - profileOffset;
        if (availableBytes < IccConstants.HeaderSize)
        {
            throw new IccProfileFormatException($"The profile at byte offset {absoluteProfileOffset} is truncated: {availableBytes} bytes are available, but a complete ICC header requires {IccConstants.HeaderSize} bytes.");
        }

        uint encodedSize = ReadEncodedProfileSize(source, profileOffset, absoluteProfileOffset);
        int declaredSize = ConvertDeclaredProfileSize(encodedSize, options.ResourceLimits.MaximumProfileBytes, absoluteProfileOffset);
        if (declaredSize < IccConstants.MinimumProfileSize)
        {
            throw new IccProfileFormatException($"The profile at byte offset {absoluteProfileOffset} declares {declaredSize} bytes; the minimum complete ICC profile size is {IccConstants.MinimumProfileSize} bytes.");
        }

        if (declaredSize > availableBytes)
        {
            throw new IccProfileFormatException($"The profile at byte offset {absoluteProfileOffset} is truncated: it declares {declaredSize} bytes, but only {availableBytes} bytes are available.");
        }

        long absoluteProfileEnd = CheckedSize.AddOffsets(absoluteProfileOffset, declaredSize, long.MaxValue);
        int trailingCount = availableBytes - declaredSize;
        if (trailingCount != 0 && options.RequireExactProfileSize)
        {
            string trailingUnit = trailingCount == 1 ? "byte" : "bytes";
            throw new IccProfileFormatException($"The profile declared at byte offset {absoluteProfileOffset} ends at byte offset {absoluteProfileEnd}, but {trailingCount} trailing {trailingUnit} remain while exact profile size is required.");
        }

        System.ReadOnlyMemory<byte> headerMemory = source.Slice(profileOffset, IccConstants.HeaderSize);
        IccProfileHeader header = HeaderCodec.ReadHeaderWithMode(headerMemory, absoluteProfileOffset, options.Mode);
        ValidateProfileFileSignature(header, options.Mode, absoluteProfileOffset);
        ParserPolicy.ApplyHeaderIssues(header, options.Mode, diagnostics, absoluteProfileOffset);

        IccProfile profile = new IccProfile();
        profile.Header = header;
        TagTableCodec.ReadTable(source, profileOffset, absoluteProfileOffset, declaredSize, options, profile, diagnostics, currentNestingDepth);
        profile.ValidationIssues = diagnostics.GetIssues();
        if (options.PreserveOriginalBytes)
        {
            profile.OriginalBytes = source.Slice(profileOffset, declaredSize).ToArray();
        }

        if (trailingCount != 0)
        {
            int trailingOffset = profileOffset + declaredSize;
            profile.TrailingBytes = source.Slice(trailingOffset, trailingCount).ToArray();
            string trailingUnit = trailingCount == 1 ? "byte" : "bytes";
            profile.ReadResult.Message = $"The declared profile was read and {trailingCount} trailing {trailingUnit} were retained.";
        }
        else
        {
            if (profile.ValidationIssues.Length == 0)
            {
                profile.ReadResult.Message = "The profile envelope was read successfully.";
            }
            else
            {
                profile.ReadResult.Message = $"The profile envelope was read with {profile.ValidationIssues.Length} retained diagnostics.";
            }
        }

        profile.Location.Offset = absoluteProfileOffset;
        profile.Location.Length = declaredSize;
        profile.ReadResult.Status = IccOperationStatus.Success;
        profile.ReadResult.Location.Offset = absoluteProfileOffset;
        profile.ReadResult.Location.Length = declaredSize;
        return profile;
    }

    internal static void ValidateEnvelopeOptions(IccReadOptions options)
    {
        if (options == null)
        {
            throw new IccProfileValidationException("Profile read options cannot be null.");
        }

        if (options.ResourceLimits == null)
        {
            throw new IccProfileValidationException("Profile read resource limits cannot be null.");
        }

        if (options.ResourceLimits.MaximumProfileBytes < IccConstants.MinimumProfileSize)
        {
            throw new IccProfileValidationException($"The maximum profile size must be at least {IccConstants.MinimumProfileSize} bytes.");
        }

        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        ParserPolicy.ValidateReadMode(options.Mode);
        if (options.ResourceLimits.MaximumDiagnosticCount < 0)
        {
            throw new IccProfileValidationException("The maximum retained diagnostic count cannot be negative.");
        }
    }

    internal static uint ReadEncodedProfileSize(System.ReadOnlyMemory<byte> source, int profileOffset, long absoluteProfileOffset)
    {
        BigEndianReader reader = new BigEndianReader(source.Slice(profileOffset, 4), absoluteProfileOffset);
        return reader.ReadUInt32();
    }

    internal static int ConvertDeclaredProfileSize(uint encodedSize, int maximumProfileBytes, long absoluteProfileOffset)
    {
        if (encodedSize > int.MaxValue)
        {
            throw new IccProfileFormatException($"The profile size at byte offset {absoluteProfileOffset} is {encodedSize} bytes and cannot be represented by the managed profile model.");
        }

        int declaredSize = (int)encodedSize;
        if (declaredSize > maximumProfileBytes)
        {
            throw new IccProfileFormatException($"The profile at byte offset {absoluteProfileOffset} declares {declaredSize} bytes, exceeding the configured maximum of {maximumProfileBytes} bytes.");
        }

        return declaredSize;
    }

    internal static void ValidateProfileFileSignature(IccProfileHeader header, IccReadMode readMode, long absoluteProfileOffset)
    {
        if (header.ProfileFileSignature == IccConstants.ProfileFileSignature)
        {
            return;
        }

        long signatureOffset = CheckedSize.AddOffsets(absoluteProfileOffset, 36, long.MaxValue);
        if (readMode != IccReadMode.Recovery)
        {
            throw new IccProfileFormatException($"The ICC profile file signature at byte offset {signatureOffset} is 0x{header.ProfileFileSignature:X8}; expected 'acsp' (0x{IccConstants.ProfileFileSignature:X8}).");
        }

        IccValidationIssue issue = CreateFileSignatureIssue(header.ProfileFileSignature, signatureOffset);
        AppendHeaderIssue(header, issue);
    }

    internal static IccValidationIssue CreateFileSignatureIssue(uint signature, long absoluteSignatureOffset)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.HeaderFileSignature;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = $"The ICC profile file signature is 0x{signature:X8}; expected 'acsp' (0x{IccConstants.ProfileFileSignature:X8}).";
        issue.Path = "Header.ProfileFileSignature";
        issue.Location.Offset = absoluteSignatureOffset;
        issue.Location.Length = 4;
        issue.Remediation = "Set header bytes 36 through 39 to the 'acsp' profile file signature.";
        return issue;
    }

    internal static void AppendHeaderIssue(IccProfileHeader header, IccValidationIssue issue)
    {
        IccValidationIssue[] issues = new IccValidationIssue[header.ValidationIssues.Length + 1];
        int index = 0;
        while (index < header.ValidationIssues.Length)
        {
            issues[index] = header.ValidationIssues[index];
            index = index + 1;
        }

        issues[index] = issue;
        header.ValidationIssues = issues;
    }
}
