namespace icc;

internal static class HeaderValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile)
    {
        System.Collections.Generic.List<IccValidationIssue> issues = new System.Collections.Generic.List<IccValidationIssue>();
        IccProfileHeader header = profile.Header;
        long baseOffset = -1;
        if (profile.Location != null && profile.Location.Offset >= 0)
        {
            baseOffset = profile.Location.Offset;
        }

        ValidateSize(profile, header, baseOffset, issues);
        if (header.Version == null)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderVersion, IccValidationSeverity.Fatal,
                "The profile version model is missing.", "Header.Version", baseOffset, 8, 4,
                "Provide a complete decoded ICC profile version."));
            return issues.ToArray();
        }

        ValidateVersion(header, baseOffset, issues);
        ValidateClass(header, baseOffset, issues);
        ValidateFileSignature(header, baseOffset, issues);
        if (header.Version.Kind == IccVersionKind.Version5
            && header.ProfileClass == IccProfileClass.ColorEncodingSpace)
        {
            ValidateColorEncodingHeader(header, baseOffset, issues);
        }
        else
        {
            ValidateSpaces(header, baseOffset, issues);
            ValidateDateTime(header, baseOffset, issues);
            ValidatePlatformAndSignatures(header, baseOffset, issues);
            ValidateFlags(header, baseOffset, issues);
            ValidateAttributes(header, baseOffset, issues);
            ValidateIntent(header, baseOffset, issues);
            ValidateIlluminant(header, baseOffset, issues);
            ValidateProfileId(profile, header, baseOffset, issues);
            ValidateExtendedFields(header, baseOffset, issues);
        }
        ValidateReservedBytes(header, baseOffset, issues);
        return issues.ToArray();
    }

    internal static void ValidateSize(IccProfile profile, IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.DeclaredSize < IccConstants.MinimumProfileSize)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileSize, IccValidationSeverity.Error,
                $"The declared profile size {header.DeclaredSize} is smaller than the {IccConstants.MinimumProfileSize}-byte minimum.",
                "Header.DeclaredSize", baseOffset, 0, 4,
                "Set the size to the exact complete profile length, including padding."));
        }
        else if ((header.DeclaredSize & 3) != 0)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileSize, IccValidationSeverity.Error,
                $"The declared profile size {header.DeclaredSize} is not a multiple of four bytes.",
                "Header.DeclaredSize", baseOffset, 0, 4,
                "Pad the last tag data element and set the size to the aligned profile length."));
        }

        if (profile.OriginalBytes != null && profile.OriginalBytes.Length != 0
            && header.DeclaredSize != profile.OriginalBytes.Length)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileSize, IccValidationSeverity.Error,
                $"The header declares {header.DeclaredSize} bytes but the retained profile contains {profile.OriginalBytes.Length} bytes.",
                "Header.DeclaredSize", baseOffset, 0, 4,
                "Set the declared size to the exact retained profile length."));
        }
        if (profile.Location != null && profile.Location.Length > 0
            && header.DeclaredSize != profile.Location.Length)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileSize, IccValidationSeverity.Error,
                $"The header declares {header.DeclaredSize} bytes but the profile location spans {profile.Location.Length} bytes.",
                "Header.DeclaredSize", baseOffset, 0, 4,
                "Make the declared size agree with the complete profile byte range."));
        }
    }

    internal static void ValidateVersion(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccVersion version = header.Version;
        IccVersion decoded = HeaderCodec.DecodeVersion(version.PackedValue);
        bool supportedKind = version.Kind == IccVersionKind.Version2
            || version.Kind == IccVersionKind.Version4 || version.Kind == IccVersionKind.Version5;
        bool inconsistent = decoded.Kind != version.Kind || decoded.Major != version.Major
            || decoded.Minor != version.Minor || decoded.BugFix != version.BugFix
            || decoded.SubclassMajor != version.SubclassMajor || decoded.SubclassMinor != version.SubclassMinor
            || decoded.IsSupported != version.IsSupported || decoded.IsFutureMinor != version.IsFutureMinor;
        if (!supportedKind || !version.IsSupported || inconsistent)
        {
            IccValidationSeverity severity = version.IsFutureMinor && !inconsistent
                ? IccValidationSeverity.Warning : IccValidationSeverity.Error;
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderVersion, severity,
                inconsistent
                    ? "The decoded version fields are inconsistent with the packed header version."
                    : $"ICC profile version {version.Major}.{version.Minor}.{version.BugFix} is not directly supported.",
                "Header.Version", baseOffset, 8, 4,
                "Use a consistent binary-coded-decimal ICC version 2.x, 4.0 through 4.4, or 5.0."));
        }
        if ((version.Kind == IccVersionKind.Version2 || version.Kind == IccVersionKind.Version4)
            && (version.PackedValue & 0x0000ffffu) != 0)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Warning,
                "The ICC.1 profile-version reserved bytes are non-zero.", "Header",
                baseOffset, 10, 2, "Set profile-version bytes 10 and 11 to zero."));
        }
    }

    internal static void ValidateClass(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccProfileClass decoded = HeaderCodec.DecodeProfileClass(header.ProfileClassSignature);
        bool extendedClass = decoded == IccProfileClass.ColorEncodingSpace
            || decoded == IccProfileClass.MultiplexIdentification
            || decoded == IccProfileClass.MultiplexLink
            || decoded == IccProfileClass.MultiplexVisualization;
        if (decoded == IccProfileClass.Unknown || decoded != header.ProfileClass
            || (extendedClass && header.Version.Kind != IccVersionKind.Version5))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileClass, IccValidationSeverity.Error,
                "The profile class signature is unknown, generation-inappropriate, or inconsistent with its decoded value.",
                "Header.ProfileClass", baseOffset, 12, 4,
                "Use a registered profile-class signature permitted by the selected ICC generation."));
        }
    }

    internal static void ValidateFileSignature(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.ProfileFileSignature != IccConstants.ProfileFileSignature)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderFileSignature, IccValidationSeverity.Error,
                "The profile file signature is not 'acsp'.", "Header.ProfileFileSignature",
                baseOffset, 36, 4, "Set header bytes 36 through 39 to the ICC 'acsp' signature."));
        }
    }

    internal static void ValidateSpaces(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccColorSpaceKind decodedData = HeaderCodec.DecodeDataColorSpace(
            header.DataColorSpaceSignature, header.Version.Kind);
        int dataChannels = ColorSpaceChannelCount.Decode(header.DataColorSpaceSignature);
        bool dataMayBeZero = header.Version.Kind == IccVersionKind.Version5
            && (header.ProfileClass == IccProfileClass.Abstract
                || header.ProfileClass == IccProfileClass.MultiplexLink
                || header.ProfileClass == IccProfileClass.MultiplexVisualization);
        bool invalidData = decodedData != header.DataColorSpace
            || (header.DataColorSpaceSignature == 0 && !dataMayBeZero)
            || (header.DataColorSpaceSignature != 0 && dataChannels == 0)
            || (header.Version.Kind != IccVersionKind.Version5
                && (dataChannels > IccConstants.MaximumIcc1ChannelCount
                    || decodedData == IccColorSpaceKind.ExtendedDevice
                    || decodedData == IccColorSpaceKind.Spectral
                    || decodedData == IccColorSpaceKind.BiSpectral));
        if (invalidData)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderColorSpace, IccValidationSeverity.Error,
                "The data colour-space signature, decoded family, channel count, and profile generation are not consistent.",
                "Header.DataColorSpace", baseOffset, 16, 4,
                "Use a permitted colour-space signature with a non-zero generation-appropriate channel count."));
        }

        bool linkClass = header.ProfileClass == IccProfileClass.DeviceLink
            || header.ProfileClass == IccProfileClass.MultiplexLink;
        IccPcsKind expectedPcs = HeaderCodec.DecodePcs(header.ProfileConnectionSpaceSignature);
        bool invalidPcs;
        if (linkClass)
        {
            invalidPcs = header.ProfileConnectionSpaceSignature == 0
                || ColorSpaceChannelCount.Decode(header.ProfileConnectionSpaceSignature) == 0
                || header.ProfileConnectionSpace != IccPcsKind.None;
        }
        else if (header.ProfileConnectionSpaceSignature == 0)
        {
            expectedPcs = HeaderCodec.DecodeExtendedPcs(header.SpectralPcs, header.MaterialConnectionSpace);
            invalidPcs = header.Version.Kind != IccVersionKind.Version5
                || expectedPcs == IccPcsKind.None || header.ProfileConnectionSpace != expectedPcs;
        }
        else
        {
            invalidPcs = expectedPcs == IccPcsKind.None || header.ProfileConnectionSpace != expectedPcs;
        }
        if (invalidPcs)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderPcs, IccValidationSeverity.Error,
                "The PCS signature is not permitted for the profile class or is inconsistent with its decoded connection space.",
                "Header.ProfileConnectionSpace", baseOffset, 20, 4,
                "Use XYZ or Lab for a colorimetric PCS, zero for a defined ICC.2 extended PCS, or the B-side data space for a link profile."));
        }
    }

    internal static void ValidateDateTime(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.CreationDateTime == null || !IsValidDateTime(header.CreationDateTime))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderDateTime,
                header.CreationDateTime == null ? IccValidationSeverity.Fatal : IccValidationSeverity.Error,
                "The creation time is missing or is not a valid Gregorian ICC dateTimeNumber.",
                "Header.CreationDateTime", baseOffset, 24, 12,
                "Encode a valid UTC year, month, day, hour, minute, and second."));
        }
    }

    internal static void ValidatePlatformAndSignatures(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.PrimaryPlatformSignature != 0
            && !LegacyStructuredTagCodec.IsRegisteredPlatform(header.PrimaryPlatformSignature))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderPlatform, IccValidationSeverity.Error,
                "The primary platform signature is not zero or a registered ICC platform.",
                "Header.PrimaryPlatformSignature", baseOffset, 40, 4,
                "Use APPL, MSFT, SGI , SUNW, or zero when no primary platform is identified."));
        }
        ValidateRegisteredSignature(header.PreferredCmmSignature, "Header.PreferredCmmSignature", 4, baseOffset, issues);
        ValidateRegisteredSignature(header.DeviceManufacturerSignature, "Header.DeviceManufacturerSignature", 48, baseOffset, issues);
        ValidateRegisteredSignature(header.DeviceModelSignature, "Header.DeviceModelSignature", 52, baseOffset, issues);
        ValidateRegisteredSignature(header.ProfileCreatorSignature, "Header.ProfileCreatorSignature", 80, baseOffset, issues);
    }

    internal static void ValidateRegisteredSignature(uint signature, string path, int relativeOffset,
        long baseOffset, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (signature != 0 && !IsPrintableSignature(signature))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderRegisteredSignature, IccValidationSeverity.Error,
                "A non-zero registered header signature contains bytes outside the ICC signature character range.",
                path, baseOffset, relativeOffset, 4,
                "Use a registered four-character signature or set the optional field to zero."));
        }
    }

    internal static void ValidateFlags(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        uint allowed = IccConstants.EmbeddedProfileFlag | IccConstants.UseIndependentOfDataFlag;
        if (header.Version.Kind == IccVersionKind.Version5)
        {
            allowed = allowed | IccConstants.McsSubsetFlag | IccConstants.ExtendedPcsRangeFlag;
        }
        bool fieldsMatch = header.IsEmbedded == ((header.ProfileFlags & IccConstants.EmbeddedProfileFlag) != 0)
            && header.RequiresEmbeddedData == ((header.ProfileFlags & IccConstants.UseIndependentOfDataFlag) != 0)
            && header.RequiresMcsSubset == (header.Version.Kind == IccVersionKind.Version5
                && (header.ProfileFlags & IccConstants.McsSubsetFlag) != 0)
            && header.UsesExtendedPcsRange == (header.Version.Kind == IccVersionKind.Version5
                && (header.ProfileFlags & IccConstants.ExtendedPcsRangeFlag) != 0);
        if ((header.ProfileFlags & (0x0000ffffu & ~allowed)) != 0 || !fieldsMatch
            || (header.RequiresMcsSubset && header.MaterialConnectionSpaceSignature == 0))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderFlags, IccValidationSeverity.Error,
                "The ICC-reserved flag bits, decoded flag values, or ICC.2 MCS flag usage are inconsistent.",
                "Header.ProfileFlags", baseOffset, 44, 4,
                "Clear reserved bits and make the decoded flags agree with their generation-specific bit masks."));
        }
    }

    internal static void ValidateAttributes(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        ulong reservedMask = header.Version.Kind == IccVersionKind.Version5
            ? 0x00000000ffffff00UL : 0x00000000fffffff0UL;
        if ((header.DeviceAttributes & reservedMask) != 0)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderDeviceAttributes, IccValidationSeverity.Error,
                "The ICC-reserved device-attribute bits are non-zero.", "Header.DeviceAttributes",
                baseOffset, 56, 8, "Clear reserved attribute bits while preserving defined and vendor-specific bits."));
        }
    }

    internal static void ValidateIntent(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.RenderingIntent < 0 || header.RenderingIntent > 3
            || (int)header.RenderingIntentKind != header.RenderingIntent)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderRenderingIntent, IccValidationSeverity.Error,
                "The rendering intent shall be an integer from zero through three and agree with its decoded value.",
                "Header.RenderingIntent", baseOffset, 64, 4,
                "Use perceptual, media-relative colorimetric, saturation, or ICC-absolute colorimetric intent."));
        }
    }

    internal static void ValidateIlluminant(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid;
        if (header.Version.Kind == IccVersionKind.Version2 || header.Version.Kind == IccVersionKind.Version4)
        {
            invalid = !IsCloseFixed16(header.PcsIlluminantX, IccStandardColorimetry.D50EncodedX)
                || !IsCloseFixed16(header.PcsIlluminantY, IccStandardColorimetry.D50EncodedY)
                || !IsCloseFixed16(header.PcsIlluminantZ, IccStandardColorimetry.D50EncodedZ);
        }
        else
        {
            invalid = header.PcsIlluminantX < 0 || header.PcsIlluminantY <= 0 || header.PcsIlluminantZ < 0;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderPcsIlluminant, IccValidationSeverity.Error,
                "The PCS illuminant is not valid for the selected ICC generation.", "Header.PcsIlluminant",
                baseOffset, 68, 12,
                "Use the encoded D50 reference illuminant for ICC.1 or valid positive ICC.2 PCS colorimetry."));
        }
    }

    internal static void ValidateProfileId(IccProfile profile, IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.ProfileId == null || header.ProfileId.Bytes == null)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileId, IccValidationSeverity.Fatal,
                "The Profile ID model or byte array is missing.", "Header.ProfileId",
                baseOffset, 84, 16, "Provide the complete 16-byte Profile ID field."));
            return;
        }
        bool correctLength = header.ProfileId.Bytes.Length == IccConstants.ProfileIdSize;
        bool nonZero = correctLength && HeaderCodec.ContainsNonZeroBytes(
            header.ProfileId.Bytes, 0, IccConstants.ProfileIdSize);
        bool v2Invalid = header.Version.Kind == IccVersionKind.Version2
            && (header.ProfileId.IsPresent || nonZero);
        if (!correctLength || header.ProfileId.IsPresent != nonZero || v2Invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileId, IccValidationSeverity.Error,
                "The Profile ID length, presence indicator, bytes, or profile generation are inconsistent.",
                "Header.ProfileId", baseOffset, 84, 16,
                "Use 16 zero bytes for an absent ID, and store a computed ID only in ICC v4 or v5."));
            return;
        }
        if (nonZero && profile.OriginalBytes != null
            && profile.OriginalBytes.Length == header.DeclaredSize
            && profile.OriginalBytes.Length >= IccConstants.MinimumProfileSize
            && header.ProfileFileSignature == IccConstants.ProfileFileSignature)
        {
            IccVersion encodedVersion = HeaderCodec.DecodeVersion(
                ProfileIdCore.ReadUInt32(profile.OriginalBytes, 8));
            bool digestInputValid = ProfileIdCore.ReadUInt32(profile.OriginalBytes, 0)
                    == (uint)profile.OriginalBytes.Length
                && ProfileIdCore.ReadUInt32(profile.OriginalBytes, 36)
                    == IccConstants.ProfileFileSignature
                && encodedVersion.IsSupported
                && (encodedVersion.Kind == IccVersionKind.Version4
                    || encodedVersion.Kind == IccVersionKind.Version5);
            if (digestInputValid)
            {
                IccProfileId computed = ProfileIdCore.Compute(profile.OriginalBytes);
                if (!ProfileIdCore.BytesEqual(header.ProfileId.Bytes, computed.Bytes))
                {
                    issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileId, IccValidationSeverity.Error,
                        "The stored Profile ID does not match the MD5 digest of the retained profile bytes.",
                        "Header.ProfileId", baseOffset, 84, 16,
                        "Recompute the Profile ID with the flags, rendering intent, and ID fields temporarily zeroed."));
                }
            }
        }
    }

    internal static void ValidateExtendedFields(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.Version.Kind != IccVersionKind.Version5)
        {
            bool nonZero = header.SpectralPcsSignature != 0 || header.SpectralPcs != IccColorSpaceKind.Unknown
                || !IsZeroRange(header.SpectralPcsRangeStart, header.SpectralPcsRangeEnd, header.SpectralPcsRangeSteps)
                || !IsZeroRange(header.BiSpectralPcsRangeStart, header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps)
                || header.MaterialConnectionSpaceSignature != 0
                || header.MaterialConnectionSpace != IccColorSpaceKind.Unknown
                || header.ProfileSubclassSignature != 0 || header.Version.SubclassMajor != 0
                || header.Version.SubclassMinor != 0 || header.IccMaxReservedValue != 0;
            if (nonZero)
            {
                issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Error,
                    "ICC.2-only header fields are non-zero in an ICC.1 profile.", "Header.VersionSpecificFields",
                    baseOffset, header.Version.Kind == IccVersionKind.Version2 ? 84 : 100,
                    header.Version.Kind == IccVersionKind.Version2 ? 44 : 28,
                    "Clear all generation-inapplicable header fields."));
            }
            return;
        }

        ValidateSpectralFields(header, baseOffset, issues);
        ValidateMcsFields(header, baseOffset, issues);
        bool hasSubclass = header.ProfileSubclassSignature != 0;
        bool hasSubclassVersion = header.Version.SubclassMajor != 0 || header.Version.SubclassMinor != 0;
        if (hasSubclass != hasSubclassVersion || (hasSubclass && !IsPrintableSignature(header.ProfileSubclassSignature)))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderProfileSubclass, IccValidationSeverity.Error,
                "The ICC.2 profile subclass signature and subclass version shall both be present or both be absent.",
                "Header.ProfileSubclass", baseOffset, 120, 4,
                "Use a registered subclass signature with its ICS version, or clear both fields."));
        }
        if (header.IccMaxReservedValue != 0)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Warning,
                "The ICC.2 reserved header field is non-zero.", "Header", baseOffset, 124, 4,
                "Set reserved header bytes to zero."));
        }
    }

    internal static void ValidateSpectralFields(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccColorSpaceKind decoded = HeaderCodec.DecodeSpectralPcs(header.SpectralPcsSignature);
        bool invalid = decoded != header.SpectralPcs;
        if (header.SpectralPcsSignature == 0)
        {
            invalid = invalid || !IsZeroRange(header.SpectralPcsRangeStart,
                header.SpectralPcsRangeEnd, header.SpectralPcsRangeSteps)
                || !IsZeroRange(header.BiSpectralPcsRangeStart,
                    header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps);
        }
        else
        {
            int channels = ColorSpaceChannelCount.Decode(header.SpectralPcsSignature);
            bool spectralRangeValid = IsValidRange(header.SpectralPcsRangeStart,
                header.SpectralPcsRangeEnd, header.SpectralPcsRangeSteps);
            if (decoded == IccColorSpaceKind.Spectral)
            {
                invalid = invalid || !spectralRangeValid
                    || channels != header.SpectralPcsRangeSteps
                    || !IsZeroRange(header.BiSpectralPcsRangeStart,
                        header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps);
            }
            else if (decoded == IccColorSpaceKind.BiSpectral)
            {
                bool biRangeValid = IsValidRange(header.BiSpectralPcsRangeStart,
                    header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps);
                long samples = (long)header.SpectralPcsRangeSteps * header.BiSpectralPcsRangeSteps;
                invalid = invalid || !spectralRangeValid || !biRangeValid || channels != samples;
            }
            else
            {
                invalid = true;
            }
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderSpectralPcs, IccValidationSeverity.Error,
                "The ICC.2 spectral PCS signature, channel count, or wavelength ranges are inconsistent.",
                "Header.SpectralPcs", baseOffset, 100, 16,
                "Use a registered spectral signature, valid increasing ranges, and matching sample counts."));
        }
    }

    internal static void ValidateMcsFields(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccColorSpaceKind decoded = HeaderCodec.DecodeMaterialConnectionSpace(
            header.MaterialConnectionSpaceSignature);
        bool allowedClass = header.ProfileClass == IccProfileClass.Input
            || header.ProfileClass == IccProfileClass.MultiplexIdentification
            || header.ProfileClass == IccProfileClass.MultiplexVisualization;
        bool invalid = decoded != header.MaterialConnectionSpace
            || (header.MaterialConnectionSpaceSignature != 0
                && (decoded != IccColorSpaceKind.Multiplex || !allowedClass))
            || (header.MaterialConnectionSpaceSignature == 0
                && header.MaterialConnectionSpace != IccColorSpaceKind.Unknown)
            || ((header.ProfileClass == IccProfileClass.MultiplexIdentification
                    || header.ProfileClass == IccProfileClass.MultiplexVisualization)
                && header.MaterialConnectionSpaceSignature == 0);
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderMcs, IccValidationSeverity.Error,
                "The ICC.2 MCS signature, channel count, decoded family, and profile class are inconsistent.",
                "Header.MaterialConnectionSpace", baseOffset, 116, 4,
                "Use a non-zero mcXXXX MCS for multiplex classes or an allowed optional MCS for an input profile."));
        }
    }

    internal static void ValidateColorEncodingHeader(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if ((header.DataColorSpaceSignature != IccColorSpaceSignatures.Rgb
                && header.DataColorSpaceSignature != IccColorSpaceSignatures.YCbCr)
            || header.DataColorSpace != HeaderCodec.DecodeDataColorSpace(
                header.DataColorSpaceSignature, IccVersionKind.Version5))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderColorSpace, IccValidationSeverity.Error,
                "A ColorEncodingSpace profile shall use RGB or YCbCr data.", "Header.DataColorSpace",
                baseOffset, 16, 4, "Use the RGB or YCbCr colour-space signature."));
        }
        bool firstReserved = header.PreferredCmmSignature != 0;
        bool middleReserved = header.ProfileConnectionSpaceSignature != 0
            || !IsZeroDateTime(header.CreationDateTime);
        bool tailReserved = header.PrimaryPlatformSignature != 0 || header.ProfileFlags != 0
            || header.DeviceManufacturerSignature != 0 || header.DeviceModelSignature != 0
            || header.DeviceAttributes != 0 || header.RenderingIntent != 0
            || header.PcsIlluminantX != 0 || header.PcsIlluminantY != 0 || header.PcsIlluminantZ != 0
            || header.ProfileCreatorSignature != 0 || HasProfileIdData(header.ProfileId)
            || header.SpectralPcsSignature != 0 || !IsZeroRange(header.SpectralPcsRangeStart,
                header.SpectralPcsRangeEnd, header.SpectralPcsRangeSteps)
            || !IsZeroRange(header.BiSpectralPcsRangeStart,
                header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps)
            || header.MaterialConnectionSpaceSignature != 0 || header.ProfileSubclassSignature != 0
            || header.IccMaxReservedValue != 0 || header.IsEmbedded || header.RequiresEmbeddedData
            || header.RequiresMcsSubset || header.UsesExtendedPcsRange
            || header.ProfileConnectionSpace != IccPcsKind.None
            || header.SpectralPcs != IccColorSpaceKind.Unknown
            || header.MaterialConnectionSpace != IccColorSpaceKind.Unknown
            || header.RenderingIntentKind != IccRenderingIntent.Perceptual
            || header.Version.SubclassMajor != 0 || header.Version.SubclassMinor != 0;
        if (firstReserved)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Error,
                "ColorEncodingSpace header bytes 4 through 7 shall be zero.", "Header.ColorEncodingReserved",
                baseOffset, 4, 4, "Clear the reserved ColorEncodingSpace header bytes."));
        }
        if (middleReserved)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Error,
                "ColorEncodingSpace header bytes 20 through 35 shall be zero.", "Header.ColorEncodingReserved",
                baseOffset, 20, 16, "Clear the reserved ColorEncodingSpace header bytes."));
        }
        if (tailReserved)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Error,
                "ColorEncodingSpace header bytes 40 through 127 shall be zero.", "Header.ColorEncodingReserved",
                baseOffset, 40, 88, "Clear the reserved ColorEncodingSpace header bytes."));
        }
    }

    internal static void ValidateReservedBytes(IccProfileHeader header, long baseOffset,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (header.RawBytes == null || header.RawBytes.Length != IccConstants.HeaderSize)
        {
            return;
        }
        int offset = header.Version.Kind == IccVersionKind.Version2 ? 84
            : header.Version.Kind == IccVersionKind.Version4 ? 100
            : header.Version.Kind == IccVersionKind.Version5 ? 124 : 128;
        int length = IccConstants.HeaderSize - offset;
        if (length > 0 && HeaderCodec.ContainsNonZeroBytes(header.RawBytes, offset, length))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.HeaderReserved, IccValidationSeverity.Warning,
                "Reserved header bytes are non-zero.", "Header", baseOffset, offset, length,
                "Set reserved header bytes to zero."));
        }
    }

    internal static bool IsValidDateTime(IccDateTime value)
    {
        if (!DateTimeTagCodec.IsValidDateTime(value) || value.Year < 1 || value.Year > ushort.MaxValue)
        {
            return false;
        }
        int days = 31;
        if (value.Month == 4 || value.Month == 6 || value.Month == 9 || value.Month == 11)
        {
            days = 30;
        }
        else if (value.Month == 2)
        {
            bool leap = value.Year % 4 == 0 && (value.Year % 100 != 0 || value.Year % 400 == 0);
            days = leap ? 29 : 28;
        }
        return value.Day <= days;
    }

    internal static bool IsZeroDateTime(IccDateTime value)
    {
        return value == null || (value.Year == 0 && value.Month == 0 && value.Day == 0
            && value.Hour == 0 && value.Minute == 0 && value.Second == 0);
    }

    internal static bool IsValidRange(double start, double end, int steps)
    {
        return !double.IsNaN(start) && !double.IsInfinity(start)
            && !double.IsNaN(end) && !double.IsInfinity(end)
            && start >= 0 && end > start && steps >= 2 && steps <= ushort.MaxValue;
    }

    internal static bool IsZeroRange(double start, double end, int steps)
    {
        return start == 0 && end == 0 && steps == 0;
    }

    internal static bool HasProfileIdData(IccProfileId profileId)
    {
        if (profileId == null || profileId.Bytes == null)
        {
            return true;
        }
        if (profileId.IsPresent || profileId.Bytes.Length != IccConstants.ProfileIdSize)
        {
            return true;
        }
        return HeaderCodec.ContainsNonZeroBytes(profileId.Bytes, 0, IccConstants.ProfileIdSize);
    }

    internal static bool IsPrintableSignature(uint signature)
    {
        uint first = (signature >> 24) & 0xff;
        uint second = (signature >> 16) & 0xff;
        uint third = (signature >> 8) & 0xff;
        uint fourth = signature & 0xff;
        return IsSignatureCharacter(first) && IsSignatureCharacter(second)
            && IsSignatureCharacter(third) && IsSignatureCharacter(fourth);
    }

    internal static bool IsSignatureCharacter(uint value)
    {
        return value == 0x20 || (value >= 0x30 && value <= 0x39)
            || (value >= 0x41 && value <= 0x5a) || (value >= 0x61 && value <= 0x7a);
    }

    internal static bool IsCloseFixed16(int actual, int expected)
    {
        long difference = (long)actual - expected;
        if (difference < 0)
        {
            difference = -difference;
        }
        return difference <= 33;
    }

    internal static IccValidationIssue CreateIssue(string ruleId, IccValidationSeverity severity,
        string message, string path, long baseOffset, int relativeOffset, int length, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = path;
        issue.Location.Offset = baseOffset < 0 || baseOffset > long.MaxValue - relativeOffset
            ? -1 : baseOffset + relativeOffset;
        issue.Location.Length = length;
        issue.Remediation = remediation;
        return issue;
    }
}
