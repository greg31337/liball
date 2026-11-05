namespace icc;

internal static class HeaderCodec
{
    internal const int D50IlluminantX = 0x0000f6d6;
    internal const int D50IlluminantY = 0x00010000;
    internal const int D50IlluminantZ = 0x0000d32d;

    internal static IccProfileHeader ReadHeader(System.ReadOnlyMemory<byte> memory, long absoluteBaseOffset)
    {
        return ReadHeaderWithMode(memory, absoluteBaseOffset, IccReadMode.Recovery);
    }

    internal static IccProfileHeader ReadHeaderWithMode(System.ReadOnlyMemory<byte> memory, long absoluteBaseOffset, IccReadMode readMode)
    {
        BigEndianReader sourceReader = new BigEndianReader(memory, absoluteBaseOffset);
        byte[] rawBytes = sourceReader.ReadExactBytes(IccConstants.HeaderSize);
        BigEndianReader reader = new BigEndianReader(rawBytes, absoluteBaseOffset);
        IccProfileHeader header = new IccProfileHeader();
        header.RawBytes = rawBytes;
        header.DeclaredSize = reader.ReadInt32();
        header.PreferredCmmSignature = reader.ReadUInt32();
        header.Version = DecodeVersion(reader.ReadUInt32());
        EnforceVersionPolicy(header.Version, readMode, CheckedSize.AddOffsets(absoluteBaseOffset, 8, long.MaxValue));
        header.ProfileClassSignature = reader.ReadUInt32();
        header.ProfileClass = DecodeProfileClass(header.ProfileClassSignature);
        header.DataColorSpaceSignature = reader.ReadUInt32();
        header.DataColorSpace = DecodeDataColorSpace(header.DataColorSpaceSignature, header.Version.Kind);
        header.ProfileConnectionSpaceSignature = reader.ReadUInt32();
        header.ProfileConnectionSpace = DecodePcs(header.ProfileConnectionSpaceSignature);
        header.CreationDateTime = IccNumberCodec.ReadDateTimeNumber(reader);
        header.ProfileFileSignature = reader.ReadUInt32();
        header.PrimaryPlatformSignature = reader.ReadUInt32();
        header.ProfileFlags = reader.ReadUInt32();
        header.IsEmbedded = (header.ProfileFlags & IccConstants.EmbeddedProfileFlag) != 0;
        header.RequiresEmbeddedData = (header.ProfileFlags & IccConstants.UseIndependentOfDataFlag) != 0;
        header.RequiresMcsSubset = header.Version.Kind == IccVersionKind.Version5 && (header.ProfileFlags & IccConstants.McsSubsetFlag) != 0;
        header.UsesExtendedPcsRange = header.Version.Kind == IccVersionKind.Version5 && (header.ProfileFlags & IccConstants.ExtendedPcsRangeFlag) != 0;
        header.DeviceManufacturerSignature = reader.ReadUInt32();
        header.DeviceModelSignature = reader.ReadUInt32();
        header.DeviceAttributes = reader.ReadUInt64();
        header.RenderingIntent = reader.ReadInt32();
        header.RenderingIntentKind = DecodeRenderingIntent(header.RenderingIntent);
        header.PcsIlluminantX = reader.ReadInt32();
        header.PcsIlluminantY = reader.ReadInt32();
        header.PcsIlluminantZ = reader.ReadInt32();
        header.ProfileCreatorSignature = reader.ReadUInt32();
        header.ProfileId.Bytes = reader.ReadExactBytes(IccConstants.ProfileIdSize);
        header.ProfileId.IsPresent = (header.Version.Kind == IccVersionKind.Version4 || header.Version.Kind == IccVersionKind.Version5) && ContainsNonZeroBytes(header.ProfileId.Bytes, 0, IccConstants.ProfileIdSize);
        header.ContextualBytes = CopyHeaderBytes(rawBytes, 100, 28);
        if (header.Version.Kind == IccVersionKind.Version5)
        {
            header.SpectralPcsSignature = reader.ReadUInt32();
            header.SpectralPcs = DecodeSpectralPcs(header.SpectralPcsSignature);
            IccSpectralRangeNumber spectralRange = IccNumberCodec.ReadSpectralRange(reader);
            header.SpectralPcsRangeStart = spectralRange.StartWavelength;
            header.SpectralPcsRangeEnd = spectralRange.EndWavelength;
            header.SpectralPcsRangeSteps = spectralRange.Steps;
            IccSpectralRangeNumber biSpectralRange = IccNumberCodec.ReadSpectralRange(reader);
            header.BiSpectralPcsRangeStart = biSpectralRange.StartWavelength;
            header.BiSpectralPcsRangeEnd = biSpectralRange.EndWavelength;
            header.BiSpectralPcsRangeSteps = biSpectralRange.Steps;
            header.MaterialConnectionSpaceSignature = reader.ReadUInt32();
            header.MaterialConnectionSpace = DecodeMaterialConnectionSpace(header.MaterialConnectionSpaceSignature);
            header.ProfileSubclassSignature = reader.ReadUInt32();
            header.IccMaxReservedValue = reader.ReadUInt32();
            if (header.ProfileConnectionSpace == IccPcsKind.None)
            {
                header.ProfileConnectionSpace = DecodeExtendedPcs(header.SpectralPcs, header.MaterialConnectionSpace);
            }
        }
        else
        {
            reader.SkipExactBytes(28);
        }

        header.ValidationIssues = FindHeaderIssues(rawBytes, header.Version, absoluteBaseOffset);
        return header;
    }

    internal static byte[] SerializeHeader(IccProfileHeader header, IccWriteOptions options, int profileSize)
    {
        ValidateHeaderForWrite(header, options, profileSize);
        IccVersionKind versionKind = header.Version.Kind;
        byte[] bytes = new byte[IccConstants.HeaderSize];
        BigEndianWriter writer = new BigEndianWriter(bytes, 0);
        writer.WriteUInt32((uint)profileSize);
        writer.WriteUInt32(header.PreferredCmmSignature);
        writer.WriteUInt32(EncodeVersionForWrite(header.Version));
        writer.WriteUInt32(header.ProfileClassSignature);
        writer.WriteUInt32(header.DataColorSpaceSignature);
        writer.WriteUInt32(header.ProfileConnectionSpaceSignature);
        WriteCreationDateTime(writer, header, options);
        writer.WriteUInt32(IccConstants.ProfileFileSignature);
        writer.WriteUInt32(header.PrimaryPlatformSignature);
        writer.WriteUInt32(ResolveProfileFlags(header, options.Mode, versionKind));
        writer.WriteUInt32(header.DeviceManufacturerSignature);
        writer.WriteUInt32(header.DeviceModelSignature);
        writer.WriteUInt64(ResolveDeviceAttributes(header.DeviceAttributes, options.Mode));
        writer.WriteInt32(header.RenderingIntent);
        writer.WriteInt32(header.PcsIlluminantX);
        writer.WriteInt32(header.PcsIlluminantY);
        writer.WriteInt32(header.PcsIlluminantZ);
        writer.WriteUInt32(header.ProfileCreatorSignature);
        if (versionKind == IccVersionKind.Version2)
        {
            writer.WriteZeroBytes(44);
            return bytes;
        }

        writer.WriteExactBytes(header.ProfileId.Bytes);
        if (versionKind == IccVersionKind.Version4)
        {
            writer.WriteZeroBytes(28);
            return bytes;
        }

        writer.WriteUInt32(header.SpectralPcsSignature);
        IccNumberCodec.WriteSpectralRange(writer, CreateSpectralRangeForWrite(header.SpectralPcsRangeStart, header.SpectralPcsRangeEnd, header.SpectralPcsRangeSteps, "spectral PCS"));
        IccNumberCodec.WriteSpectralRange(writer, CreateSpectralRangeForWrite(header.BiSpectralPcsRangeStart, header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps, "bi-spectral PCS"));
        writer.WriteUInt32(header.MaterialConnectionSpaceSignature);
        writer.WriteUInt32(header.ProfileSubclassSignature);
        writer.WriteUInt32(0);
        return bytes;
    }

    internal static void ValidateHeaderForWrite(IccProfileHeader header, IccWriteOptions options, int profileSize)
    {
        if (header == null || header.Version == null || header.CreationDateTime == null || header.ProfileId == null || header.ProfileId.Bytes == null)
        {
            throw new IccProfileWriteException("A profile header and all required header fields are needed for writing.");
        }

        if (options == null)
        {
            throw new IccProfileWriteException("Header writing options cannot be null.");
        }

        if (profileSize < IccConstants.MinimumProfileSize)
        {
            throw new IccProfileWriteException("The encoded profile size cannot be smaller than a complete ICC profile envelope.");
        }

        EncodeVersionForWrite(header.Version);
        if (header.Version.Kind != IccVersionKind.Version2 && header.ProfileId.Bytes.Length != IccConstants.ProfileIdSize)
        {
            throw new IccProfileWriteException("An ICC v4 or v5 header requires exactly 16 Profile ID bytes.");
        }

        if (options.PreserveOriginalCreationDateTime)
        {
            if (header.RawBytes == null || header.RawBytes.Length != IccConstants.HeaderSize)
            {
                throw new IccProfileWriteException("Original creation-time preservation requires a retained 128-byte source header.");
            }
        }
        else if (!DateTimeTagCodec.IsValidDateTime(header.CreationDateTime)
            && (header.ProfileClass != IccProfileClass.ColorEncodingSpace
                || !HeaderValidationCore.IsZeroDateTime(header.CreationDateTime)))
        {
            throw new IccProfileWriteException("The profile creation time does not contain a valid ICC dateTimeNumber.");
        }

        if (header.Version.Kind == IccVersionKind.Version5)
        {
            if (header.ProfileSubclassSignature == 0 && (header.Version.SubclassMajor != 0 || header.Version.SubclassMinor != 0))
            {
                throw new IccProfileWriteException("An ICC.2 profile-subclass version requires a profile-subclass signature.");
            }

            CreateSpectralRangeForWrite(header.SpectralPcsRangeStart, header.SpectralPcsRangeEnd, header.SpectralPcsRangeSteps, "spectral PCS");
            CreateSpectralRangeForWrite(header.BiSpectralPcsRangeStart, header.BiSpectralPcsRangeEnd, header.BiSpectralPcsRangeSteps, "bi-spectral PCS");
        }
    }

    internal static uint EncodeVersionForWrite(IccVersion version)
    {
        int expectedMajor;
        if (version.Kind == IccVersionKind.Version2)
        {
            expectedMajor = IccConstants.Version2Major;
        }
        else if (version.Kind == IccVersionKind.Version4)
        {
            expectedMajor = IccConstants.Version4Major;
        }
        else if (version.Kind == IccVersionKind.Version5)
        {
            expectedMajor = IccConstants.Version5Major;
        }
        else
        {
            throw new IccProfileWriteException("The profile header version is not a supported ICC generation.");
        }

        if ((version.Major != 0 && version.Major != expectedMajor) || version.Minor < 0 || version.Minor > 9 || version.BugFix < 0 || version.BugFix > 9)
        {
            throw new IccProfileWriteException("The profile header version fields do not form a supported binary-coded-decimal version.");
        }

        if ((version.Kind == IccVersionKind.Version4 && version.Minor > 4) || (version.Kind == IccVersionKind.Version5 && version.Minor != 0))
        {
            throw new IccProfileWriteException("The profile header version exceeds the directly supported ICC generation range.");
        }

        uint packed = ((uint)expectedMajor << 24) | ((uint)version.Minor << 20) | ((uint)version.BugFix << 16);
        if (version.Kind == IccVersionKind.Version5)
        {
            if (version.SubclassMajor < 0 || version.SubclassMajor > 255 || version.SubclassMinor < 0 || version.SubclassMinor > 255)
            {
                throw new IccProfileWriteException("The ICC.2 profile-subclass version components shall fit in unsigned bytes.");
            }

            packed = packed | ((uint)version.SubclassMajor << 8) | (uint)version.SubclassMinor;
        }

        return packed;
    }

    internal static void WriteCreationDateTime(BigEndianWriter writer, IccProfileHeader header, IccWriteOptions options)
    {
        if (options.PreserveOriginalCreationDateTime)
        {
            writer.WriteExactBytes(CopyHeaderBytes(header.RawBytes, 24, 12));
            return;
        }

        IccNumberCodec.WriteDateTimeNumber(writer, header.CreationDateTime);
    }

    internal static uint ResolveProfileFlags(IccProfileHeader header, IccWriteMode mode, IccVersionKind versionKind)
    {
        uint flags = header.ProfileFlags;
        flags = ResolveFlag(flags, IccConstants.EmbeddedProfileFlag, header.IsEmbedded);
        flags = ResolveFlag(flags, IccConstants.UseIndependentOfDataFlag, header.RequiresEmbeddedData);
        if (versionKind == IccVersionKind.Version5)
        {
            flags = ResolveFlag(flags, IccConstants.McsSubsetFlag, header.RequiresMcsSubset);
            flags = ResolveFlag(flags, IccConstants.ExtendedPcsRangeFlag, header.UsesExtendedPcsRange);
        }
        else
        {
            flags = flags & ~(IccConstants.McsSubsetFlag | IccConstants.ExtendedPcsRangeFlag);
        }

        if (mode == IccWriteMode.Canonical)
        {
            uint mask = 0xffff0000 | IccConstants.EmbeddedProfileFlag | IccConstants.UseIndependentOfDataFlag;
            if (versionKind == IccVersionKind.Version5)
            {
                mask = mask | IccConstants.McsSubsetFlag | IccConstants.ExtendedPcsRangeFlag;
            }

            flags = flags & mask;
        }

        return flags;
    }

    internal static uint ResolveFlag(uint flags, uint mask, bool enabled)
    {
        if (enabled)
        {
            return flags | mask;
        }

        return flags & ~mask;
    }

    internal static ulong ResolveDeviceAttributes(ulong attributes, IccWriteMode mode)
    {
        if (mode == IccWriteMode.Canonical)
        {
            return attributes & 0xffffffff0000000f;
        }

        return attributes;
    }

    internal static IccSpectralRangeNumber CreateSpectralRangeForWrite(double start, double end, int steps, string description)
    {
        if (steps < 0 || steps > ushort.MaxValue)
        {
            throw new IccProfileWriteException($"The {description} range sample count does not fit in the ICC header field.");
        }

        float encodedStart = IccNumberCodec.ConvertDoubleToFloat16Source(start);
        float encodedEnd = IccNumberCodec.ConvertDoubleToFloat16Source(end);
        return new IccSpectralRangeNumber(encodedStart, encodedEnd, (ushort)steps);
    }

    internal static IccVersion DecodeVersion(uint packedValue)
    {
        IccVersion version = new IccVersion();
        version.PackedValue = packedValue;
        int majorByte = (int)((packedValue >> 24) & 0xff);
        version.Major = ((majorByte >> 4) * 10) + (majorByte & 0x0f);
        version.Minor = (int)((packedValue >> 20) & 0x0f);
        version.BugFix = (int)((packedValue >> 16) & 0x0f);
        if (version.Major == IccConstants.Version2Major)
        {
            version.Kind = IccVersionKind.Version2;
            version.IsSupported = HasValidVersionDigits(version);
        }
        else if (version.Major == IccConstants.Version4Major)
        {
            version.Kind = IccVersionKind.Version4;
            version.IsSupported = HasValidVersionDigits(version) && version.Minor <= 4;
            version.IsFutureMinor = HasValidVersionDigits(version) && version.Minor > 4;
        }
        else if (version.Major == IccConstants.Version5Major)
        {
            version.Kind = IccVersionKind.Version5;
            version.SubclassMajor = (int)((packedValue >> 8) & 0xff);
            version.SubclassMinor = (int)(packedValue & 0xff);
            version.IsSupported = HasValidVersionDigits(version) && version.Minor == 0;
            version.IsFutureMinor = HasValidVersionDigits(version) && version.Minor > 0;
        }
        else
        {
            version.Kind = IccVersionKind.Unknown;
        }

        return version;
    }

    internal static bool HasValidVersionDigits(IccVersion version)
    {
        int majorByte = (int)((version.PackedValue >> 24) & 0xff);
        int majorHighDigit = (majorByte >> 4) & 0x0f;
        int majorLowDigit = majorByte & 0x0f;
        return majorHighDigit <= 9 && majorLowDigit <= 9 && version.Minor <= 9 && version.BugFix <= 9;
    }

    internal static void EnforceVersionPolicy(IccVersion version, IccReadMode readMode, long absoluteVersionOffset)
    {
        if (readMode != IccReadMode.Strict)
        {
            return;
        }

        if (!HasValidVersionDigits(version))
        {
            throw new IccProfileFormatException($"The ICC profile version at byte offset {absoluteVersionOffset} is not valid binary-coded decimal (0x{version.PackedValue:X8}).");
        }

        if (version.Kind == IccVersionKind.Unknown)
        {
            throw new IccUnsupportedFeatureException($"ICC profile major version {version.Major} at byte offset {absoluteVersionOffset} is unsupported (0x{version.PackedValue:X8}).");
        }

        if (version.IsFutureMinor)
        {
            throw new IccUnsupportedFeatureException($"ICC profile version {GetVersionText(version)} at byte offset {absoluteVersionOffset} is newer than the supported {GetSupportedGenerationText(version.Kind)} range (0x{version.PackedValue:X8}).");
        }
    }

    internal static IccProfileClass DecodeProfileClass(uint signature)
    {
        switch (signature)
        {
            case IccProfileClassSignatures.Input:
                return IccProfileClass.Input;
            case IccProfileClassSignatures.Display:
                return IccProfileClass.Display;
            case IccProfileClassSignatures.Output:
                return IccProfileClass.Output;
            case IccProfileClassSignatures.DeviceLink:
                return IccProfileClass.DeviceLink;
            case IccProfileClassSignatures.ColorSpace:
                return IccProfileClass.ColorSpace;
            case IccProfileClassSignatures.Abstract:
                return IccProfileClass.Abstract;
            case IccProfileClassSignatures.NamedColor:
                return IccProfileClass.NamedColor;
            case IccProfileClassSignatures.ColorEncodingSpace:
                return IccProfileClass.ColorEncodingSpace;
            case IccProfileClassSignatures.MultiplexIdentification:
                return IccProfileClass.MultiplexIdentification;
            case IccProfileClassSignatures.MultiplexLink:
                return IccProfileClass.MultiplexLink;
            case IccProfileClassSignatures.MultiplexVisualization:
                return IccProfileClass.MultiplexVisualization;
            default:
                return IccProfileClass.Unknown;
        }
    }

    internal static IccColorSpaceKind DecodeColorSpace(uint signature)
    {
        switch (signature)
        {
            case IccColorSpaceSignatures.Xyz:
                return IccColorSpaceKind.Xyz;
            case IccColorSpaceSignatures.Lab:
                return IccColorSpaceKind.Lab;
            case IccColorSpaceSignatures.Luv:
                return IccColorSpaceKind.Luv;
            case IccColorSpaceSignatures.YCbCr:
                return IccColorSpaceKind.YCbCr;
            case IccColorSpaceSignatures.Yxy:
                return IccColorSpaceKind.Yxy;
            case IccColorSpaceSignatures.Rgb:
                return IccColorSpaceKind.Rgb;
            case IccColorSpaceSignatures.Gray:
                return IccColorSpaceKind.Gray;
            case IccColorSpaceSignatures.Hsv:
                return IccColorSpaceKind.Hsv;
            case IccColorSpaceSignatures.Hls:
                return IccColorSpaceKind.Hls;
            case IccColorSpaceSignatures.Cmyk:
                return IccColorSpaceKind.Cmyk;
            case IccColorSpaceSignatures.Cmy:
                return IccColorSpaceKind.Cmy;
            case IccColorSpaceSignatures.NamedColor:
                return IccColorSpaceKind.NamedColor;
            case IccColorSpaceSignatures.OneColor:
            case IccColorSpaceSignatures.TwoColor:
            case IccColorSpaceSignatures.ThreeColor:
            case IccColorSpaceSignatures.FourColor:
            case IccColorSpaceSignatures.FiveColor:
            case IccColorSpaceSignatures.SixColor:
            case IccColorSpaceSignatures.SevenColor:
            case IccColorSpaceSignatures.EightColor:
            case IccColorSpaceSignatures.NineColor:
            case IccColorSpaceSignatures.TenColor:
            case IccColorSpaceSignatures.ElevenColor:
            case IccColorSpaceSignatures.TwelveColor:
            case IccColorSpaceSignatures.ThirteenColor:
            case IccColorSpaceSignatures.FourteenColor:
            case IccColorSpaceSignatures.FifteenColor:
                return IccColorSpaceKind.DeviceChannels;
            default:
                break;
        }

        uint prefix = signature & 0xffff0000;
        uint channelCount = signature & 0x0000ffff;
        if (prefix == IccColorSpaceSignatures.ExtendedDevicePrefix && channelCount != 0)
        {
            return IccColorSpaceKind.ExtendedDevice;
        }

        return IccColorSpaceKind.Unknown;
    }

    internal static IccColorSpaceKind DecodeSpectralPcs(uint signature)
    {
        uint prefix = signature & 0xffff0000;
        uint channelCount = signature & 0x0000ffff;
        if (channelCount == 0)
        {
            return IccColorSpaceKind.Unknown;
        }

        if (prefix == IccColorSpaceSignatures.BiSpectralPrefix || prefix == IccColorSpaceSignatures.SparseMatrixSpectralPrefix)
        {
            return IccColorSpaceKind.BiSpectral;
        }

        if (prefix == IccColorSpaceSignatures.ReflectanceSpectralPrefix || prefix == IccColorSpaceSignatures.TransmissionSpectralPrefix || prefix == IccColorSpaceSignatures.RadiantSpectralPrefix)
        {
            return IccColorSpaceKind.Spectral;
        }

        return IccColorSpaceKind.Unknown;
    }

    internal static IccColorSpaceKind DecodeDataColorSpace(uint signature, IccVersionKind version)
    {
        IccColorSpaceKind colorSpace = DecodeColorSpace(signature);
        if (colorSpace != IccColorSpaceKind.Unknown || version != IccVersionKind.Version5)
        {
            return colorSpace;
        }

        return DecodeSpectralPcs(signature);
    }

    internal static IccColorSpaceKind DecodeMaterialConnectionSpace(uint signature)
    {
        uint prefix = signature & 0xffff0000;
        uint channelCount = signature & 0x0000ffff;
        if (prefix == IccColorSpaceSignatures.MultiplexPrefix && channelCount != 0)
        {
            return IccColorSpaceKind.Multiplex;
        }

        return IccColorSpaceKind.Unknown;
    }

    internal static IccPcsKind DecodeExtendedPcs(IccColorSpaceKind spectralPcs, IccColorSpaceKind materialConnectionSpace)
    {
        if (spectralPcs == IccColorSpaceKind.BiSpectral)
        {
            return IccPcsKind.BiSpectral;
        }

        if (spectralPcs == IccColorSpaceKind.Spectral)
        {
            return IccPcsKind.Spectral;
        }

        if (materialConnectionSpace == IccColorSpaceKind.Multiplex)
        {
            return IccPcsKind.Material;
        }

        return IccPcsKind.None;
    }

    internal static IccPcsKind DecodePcs(uint signature)
    {
        if (signature == IccColorSpaceSignatures.Xyz)
        {
            return IccPcsKind.Xyz;
        }

        if (signature == IccColorSpaceSignatures.Lab)
        {
            return IccPcsKind.Lab;
        }

        return IccPcsKind.None;
    }

    internal static IccRenderingIntent DecodeRenderingIntent(int value)
    {
        return (IccRenderingIntent)value;
    }

    internal static IccValidationIssue[] FindHeaderIssues(byte[] rawBytes, IccVersion version, long absoluteBaseOffset)
    {
        System.Collections.Generic.List<IccValidationIssue> issues = new System.Collections.Generic.List<IccValidationIssue>();
        if (version.IsFutureMinor)
        {
            issues.Add(CreateVersionIssue(version, absoluteBaseOffset, IccValidationSeverity.Warning, $"ICC profile version {GetVersionText(version)} is newer than the directly supported {GetSupportedGenerationText(version.Kind)} range."));
        }
        else if (version.Kind == IccVersionKind.Unknown)
        {
            issues.Add(CreateVersionIssue(version, absoluteBaseOffset, IccValidationSeverity.Error, $"ICC profile major version {version.Major} is unsupported."));
        }
        else if (!version.IsSupported)
        {
            issues.Add(CreateVersionIssue(version, absoluteBaseOffset, IccValidationSeverity.Error, $"ICC profile version {GetVersionText(version)} is not valid binary-coded decimal."));
        }

        IccVersionKind versionKind = version.Kind;
        if (versionKind == IccVersionKind.Version2 || versionKind == IccVersionKind.Version4)
        {
            if (ContainsNonZeroBytes(rawBytes, 10, 2))
            {
                issues.Add(CreateReservedIssue(absoluteBaseOffset, 10, 2, "The ICC.1 profile-version reserved bytes are non-zero."));
            }
        }

        if (versionKind == IccVersionKind.Version2)
        {
            if (ContainsNonZeroBytes(rawBytes, 84, 44))
            {
                issues.Add(CreateReservedIssue(absoluteBaseOffset, 84, 44, "The ICC v2 reserved header tail is non-zero."));
            }
        }
        else if (versionKind == IccVersionKind.Version4)
        {
            if (ContainsNonZeroBytes(rawBytes, 100, 28))
            {
                issues.Add(CreateReservedIssue(absoluteBaseOffset, 100, 28, "The ICC v4 reserved header field is non-zero."));
            }
        }
        else if (versionKind == IccVersionKind.Version5)
        {
            if (!ContainsNonZeroBytes(rawBytes, 120, 4) && ContainsNonZeroBytes(rawBytes, 10, 2))
            {
                issues.Add(CreateReservedIssue(absoluteBaseOffset, 10, 2, "The ICC.2 profile-subclass version is non-zero without a profile-subclass signature."));
            }

            if (ContainsNonZeroBytes(rawBytes, 124, 4))
            {
                issues.Add(CreateReservedIssue(absoluteBaseOffset, 124, 4, "The ICC.2 reserved header field is non-zero."));
            }
        }

        return issues.ToArray();
    }

    internal static IccValidationIssue CreateVersionIssue(IccVersion version, long absoluteBaseOffset, IccValidationSeverity severity, string message)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.HeaderVersion;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = "Header.Version";
        issue.Location.Offset = CheckedSize.AddOffsets(absoluteBaseOffset, 8, long.MaxValue);
        issue.Location.Length = 4;
        issue.Remediation = $"Use a supported ICC profile version ({GetSupportedVersionText()}); packed value 0x{version.PackedValue:X8} was preserved.";
        return issue;
    }

    internal static string GetVersionText(IccVersion version)
    {
        return $"{version.Major}.{version.Minor}.{version.BugFix}";
    }

    internal static string GetSupportedGenerationText(IccVersionKind kind)
    {
        if (kind == IccVersionKind.Version4)
        {
            return "4.0 through 4.4";
        }

        if (kind == IccVersionKind.Version5)
        {
            return "5.0";
        }

        return GetSupportedVersionText();
    }

    internal static string GetSupportedVersionText()
    {
        return "2.x, 4.0 through 4.4, or 5.0";
    }

    internal static bool ContainsNonZeroBytes(byte[] bytes, int offset, int count)
    {
        int end = (int)CheckedSize.AddOffsets(offset, count, bytes.Length);
        int index = offset;
        while (index < end)
        {
            if (bytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static byte[] CopyHeaderBytes(byte[] bytes, int offset, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static IccValidationIssue CreateReservedIssue(long absoluteBaseOffset, int relativeOffset, int length, string message)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.HeaderReserved;
        issue.Severity = IccValidationSeverity.Warning;
        issue.Message = message;
        issue.Path = "Header";
        issue.Location.Offset = CheckedSize.AddOffsets(absoluteBaseOffset, relativeOffset, long.MaxValue);
        issue.Location.Length = length;
        issue.Remediation = "Set reserved header bytes to zero.";
        return issue;
    }
}
