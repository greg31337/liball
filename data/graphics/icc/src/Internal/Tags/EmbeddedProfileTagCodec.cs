namespace icc;

internal static class EmbeddedProfileTagCodec
{
    internal const int TypeHeaderSize = 8;

    internal static void DecodeEmbeddedProfile(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (state.HasProfileContext && state.TagSignature != IccTagSignatures.EmbeddedIccMaxProfile)
        {
            CompleteFallback(state, payload, typeSignature, IccOperationStatus.InvalidData, "The embeddedProfileType is permitted only in the ICC5 embeddedProfileTag; the containing ICC.1 profile remains available as the fallback.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteFallback(state, payload, typeSignature, IccOperationStatus.InvalidData, "The embeddedProfileType reserved bytes shall be zero; the ICC.1 profile remains available as the fallback.");
            return;
        }

        if (state.CurrentNestingDepth >= state.MaximumNestingDepth)
        {
            CompleteFallback(state, payload, typeSignature, IccOperationStatus.ResourceLimitExceeded, $"Decoding the embedded ICC.2 profile would exceed the configured nesting depth of {state.MaximumNestingDepth}; the ICC.1 profile remains available as the fallback.");
            return;
        }

        System.ReadOnlyMemory<byte> profileBytes = payload.Slice(TypeHeaderSize);
        IccReadOptions options = CreateNestedReadOptions(state);
        long absoluteProfileOffset = CheckedSize.AddOffsets(state.AbsolutePayloadOffset, TypeHeaderSize, long.MaxValue);
        IccOperationResult probe = ProfileReaderCore.ProbePrefix(profileBytes.Span, absoluteProfileOffset, profileBytes.Length, true, options);
        if (probe.Status != IccOperationStatus.Success)
        {
            CompleteFallback(state, payload, typeSignature, probe.Status, $"The embedded ICC.2 profile cannot be used: {probe.Message} The ICC.1 profile remains available as the fallback.");
            return;
        }

        options.Mode = IccReadMode.Recovery;

        IccVersion version = HeaderCodec.DecodeVersion(ReadUInt32(profileBytes, 8));
        if (version.Kind != IccVersionKind.Version5)
        {
            CompleteFallback(state, payload, typeSignature, IccOperationStatus.UnsupportedFeature, "The embeddedProfileType does not contain an ICC.2 version 5 profile; the ICC.1 profile remains available as the fallback.");
            return;
        }

        IccProfile embedded = ProfileCodec.ReadEnvelopeWithDepth(profileBytes, 0, absoluteProfileOffset, options, state.CurrentNestingDepth + 1);
        if (embedded.ValidationIssues.Length != 0)
        {
            CompleteFallback(state, payload, typeSignature, IccOperationStatus.InvalidData, "The embedded ICC.2 profile has structural or header diagnostics; the ICC.1 profile remains available as the fallback.");
            return;
        }

        string contextError = ValidateProfileContext(state, embedded.Header);
        if (contextError.Length != 0)
        {
            CompleteFallback(state, payload, typeSignature, IccOperationStatus.UnsupportedFeature, contextError + " The ICC.1 profile remains available as the fallback.");
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.EmbeddedProfile);
        data.EmbeddedProfile = embedded;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded an embedded {embedded.Header.DeclaredSize}-byte ICC.2 profile that can replace the ICC.1 fallback when explicitly enabled.");
    }

    internal static byte[] SerializeEmbeddedProfile(IccProfile profile)
    {
        ValidateProfileForWrite(profile);
        byte[] profileBytes = profile.OriginalBytes;
        if (profileBytes.Length > System.Array.MaxLength - TypeHeaderSize)
        {
            throw new IccProfileWriteException("The embeddedProfileType exceeds the supported managed array size.");
        }

        byte[] payload = new byte[TypeHeaderSize + profileBytes.Length];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.EmbeddedProfile);
        writer.WriteZeroBytes(4);
        writer.WriteExactBytes(profileBytes);
        return payload;
    }

    internal static byte[] SerializeEmbeddedProfileForContext(IccProfile profile,
        IccVersionKind outerVersion, uint outerProfileClassSignature,
        uint outerDataColorSpaceSignature, int maximumProfileBytes)
    {
        if (maximumProfileBytes < IccConstants.MinimumProfileSize || profile == null
            || profile.Header == null || profile.OriginalBytes == null
            || profile.OriginalBytes.Length > maximumProfileBytes)
        {
            throw new IccProfileWriteException("The embedded ICC.2 profile exceeds the configured profile-size limit.");
        }

        if (outerVersion != IccVersionKind.Version2
            && outerVersion != IccVersionKind.Version4)
        {
            throw new IccProfileWriteException("An embedded ICC.2 replacement requires an ICC.1 version 2 or version 4 container.");
        }

        if (!EmbeddedProfileValidationCore.IsReplacementClass(
            HeaderCodec.DecodeProfileClass(outerProfileClassSignature)))
        {
            throw new IccProfileWriteException("The containing profile class cannot have an ICC.2 logical replacement.");
        }

        if (profile.Header.ProfileClassSignature != outerProfileClassSignature)
        {
            throw new IccProfileWriteException("The embedded ICC.2 profile class shall match the containing ICC.1 profile class.");
        }

        if (profile.Header.DataColorSpaceSignature != outerDataColorSpaceSignature)
        {
            throw new IccProfileWriteException("The embedded ICC.2 device colour space shall match the containing ICC.1 profile device colour space.");
        }

        return SerializeEmbeddedProfile(profile);
    }

    internal static IccReadOptions CreateNestedReadOptions(TagDecodeState state)
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Strict;
        options.CopyInputBytes = true;
        options.DecodeTagsLazily = true;
        options.RequireExactProfileSize = true;
        options.PreserveOriginalBytes = state.PreserveOriginalBytes;
        options.ResourceLimits.MaximumProfileBytes = state.MaximumProfileBytes;
        options.ResourceLimits.MaximumTagCount = state.MaximumTagCount;
        options.ResourceLimits.MaximumTagBytes = state.MaximumTagBytes;
        options.ResourceLimits.MaximumTotalTagBytes = state.MaximumTotalTagBytes;
        options.ResourceLimits.MaximumNestingDepth = state.MaximumNestingDepth;
        options.ResourceLimits.MaximumDecompressedBytes = state.MaximumDecompressedBytes;
        options.ResourceLimits.MaximumCalculatorInstructions = state.MaximumCalculatorInstructions;
        options.ResourceLimits.MaximumCalculatorStackDepth = state.MaximumCalculatorStackDepth;
        options.ResourceLimits.MaximumCalculatorStorageValues = state.MaximumCalculatorStorageValues;
        options.ResourceLimits.MaximumDiagnosticCount = state.MaximumDiagnosticCount;
        return options;
    }

    internal static string ValidateProfileContext(TagDecodeState state, IccProfileHeader header)
    {
        if (state.HasProfileContext && state.ProfileVersion != IccVersionKind.Version2
            && state.ProfileVersion != IccVersionKind.Version4)
        {
            return "The embedded ICC.2 profile requires an ICC.1 version 2 or version 4 container.";
        }

        if (!EmbeddedProfileValidationCore.IsReplacementClass(header.ProfileClass))
        {
            return "The embedded ICC.2 profile class is not representable by an ICC.1 fallback profile.";
        }

        if (!header.IsEmbedded)
        {
            return "The embedded ICC.2 profile does not set its embedded-profile flag.";
        }

        if (header.RequiresEmbeddedData)
        {
            return "The embedded ICC.2 profile sets the use-independent-of-data restriction, which is not permitted for a profile embedded in another profile.";
        }

        if (state.HasProfileContext && header.ProfileClassSignature != state.ProfileClassSignature)
        {
            return "The embedded ICC.2 profile class does not match the containing ICC.1 profile class.";
        }

        if (state.HasProfileContext && header.DataColorSpaceSignature != state.DataColorSpaceSignature)
        {
            return "The embedded ICC.2 device colour space does not match the containing ICC.1 profile device colour space.";
        }

        return string.Empty;
    }

    internal static void ValidateProfileForWrite(IccProfile profile)
    {
        if (profile == null || profile.Header == null || profile.OriginalBytes == null)
        {
            throw new IccProfileWriteException("An embedded ICC.2 profile, header, and encoded profile bytes are required.");
        }

        byte[] bytes = profile.OriginalBytes;
        if (bytes.Length < IccConstants.MinimumProfileSize || ReadUInt32(bytes, 0) != bytes.Length
            || ReadUInt32(bytes, 36) != IccConstants.ProfileFileSignature)
        {
            throw new IccProfileWriteException("The retained embedded ICC.2 profile bytes do not contain one complete ICC profile.");
        }

        IccVersion encodedVersion = HeaderCodec.DecodeVersion(ReadUInt32(bytes, 8));
        if (profile.Header.Version == null || encodedVersion.Kind != IccVersionKind.Version5
            || !encodedVersion.IsSupported
            || profile.Header.Version.Kind != IccVersionKind.Version5
            || !profile.Header.Version.IsSupported
            || encodedVersion.PackedValue != profile.Header.Version.PackedValue)
        {
            throw new IccProfileWriteException("An embeddedProfileType shall contain an ICC.2 version 5 profile.");
        }

        if (!EmbeddedProfileValidationCore.IsReplacementClass(profile.Header.ProfileClass))
        {
            throw new IccProfileWriteException("The embedded ICC.2 profile class is not representable by an ICC.1 fallback profile.");
        }

        uint encodedClass = ReadUInt32(bytes, 12);
        uint encodedDataSpace = ReadUInt32(bytes, 16);
        uint encodedFlags = ReadUInt32(bytes, 44);
        if (encodedClass != profile.Header.ProfileClassSignature || encodedDataSpace != profile.Header.DataColorSpaceSignature
            || encodedFlags != profile.Header.ProfileFlags
            || profile.Header.DeclaredSize != bytes.Length
            || profile.Header.ProfileClass != HeaderCodec.DecodeProfileClass(encodedClass)
            || profile.Header.DataColorSpace != HeaderCodec.DecodeDataColorSpace(
                encodedDataSpace, IccVersionKind.Version5))
        {
            throw new IccProfileWriteException("The retained embedded profile bytes do not match the profile header class or device colour space.");
        }

        if ((encodedFlags & IccConstants.EmbeddedProfileFlag) == 0 || (encodedFlags & IccConstants.UseIndependentOfDataFlag) != 0
            || !profile.Header.IsEmbedded || profile.Header.RequiresEmbeddedData)
        {
            throw new IccProfileWriteException("An embedded ICC.2 profile shall set its embedded flag and clear its use-independent-of-data restriction.");
        }
    }

    internal static uint ReadUInt32(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        return ((uint)values[offset] << 24) | ((uint)values[offset + 1] << 16) | ((uint)values[offset + 2] << 8) | values[offset + 3];
    }

    internal static void CompleteFallback(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, IccOperationStatus status, string message)
    {
        IccTagData data = new IccTagData();
        UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, status);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }
}
