namespace icc;

internal static class NamedColorTagCodec
{
    internal static void DecodeNamedColor(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 18)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A namedColorType payload requires its 16-byte header and two null-terminated names, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The namedColorType reserved bytes shall be zero.");
            return;
        }

        if (!state.HasProfileContext)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "namedColorType requires the profile data colour-space signature to determine its coordinate count.");
            return;
        }

        int channelCount = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        if (channelCount < 1 || channelCount > IccConstants.MaximumIcc1ChannelCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The profile data colour-space signature 0x{state.DataColorSpaceSignature:X8} does not define a valid namedColorType channel count.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 8), state.AbsolutePayloadOffset + 8);
        uint vendorFlags = headerReader.ReadUInt32();
        uint encodedCount = headerReader.ReadUInt32();
        int offset = 16;
        int prefixEnd = FindVariableNameEnd(payload, offset);
        if (prefixEnd < 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The namedColorType prefix shall be null terminated within 32 bytes and contain only 7-bit ASCII.");
            return;
        }

        string prefix = DecodeAscii(payload.Slice(offset, prefixEnd - offset));
        offset = prefixEnd + 1;
        int suffixEnd = FindVariableNameEnd(payload, offset);
        if (suffixEnd < 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The namedColorType suffix shall be null terminated within 32 bytes and contain only 7-bit ASCII.");
            return;
        }

        string suffix = DecodeAscii(payload.Slice(offset, suffixEnd - offset));
        offset = suffixEnd + 1;
        int minimumRecordSize = 1 + channelCount;
        int maximumCount = (payload.Length - offset) / minimumRecordSize;
        if (encodedCount > int.MaxValue || encodedCount > (uint)maximumCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The namedColorType count {encodedCount} cannot fit in the remaining payload using {channelCount} coordinates per colour.");
            return;
        }

        int colorCount = (int)encodedCount;
        IccNamedColor[] colors = new IccNamedColor[colorCount];
        System.Collections.Generic.Dictionary<string, int> exactNames = new System.Collections.Generic.Dictionary<string, int>();
        int duplicateCount = 0;
        int colorIndex = 0;
        while (colorIndex < colorCount)
        {
            int nameEnd = FindVariableNameEnd(payload, offset);
            if (nameEnd < 0)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"namedColorType root name {colorIndex} shall be null terminated within 32 bytes and contain only 7-bit ASCII.");
                return;
            }

            string rootName = DecodeAscii(payload.Slice(offset, nameEnd - offset));
            offset = nameEnd + 1;
            if (channelCount > payload.Length - offset)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"namedColorType colour {colorIndex} is truncated before its {channelCount} data-space coordinates.");
                return;
            }

            double[] tints = new double[channelCount];
            double[] deviceComponents = new double[channelCount];
            int channelIndex = 0;
            while (channelIndex < channelCount)
            {
                double value = payload.Span[offset + channelIndex] / 255.0;
                tints[channelIndex] = value;
                deviceComponents[channelIndex] = value;
                channelIndex = channelIndex + 1;
            }

            offset = offset + channelCount;
            IccNamedColor color = new IccNamedColor();
            color.Name = rootName;
            color.Tints = tints;
            color.DeviceColor.ColorSpace = HeaderCodec.DecodeColorSpace(state.DataColorSpaceSignature);
            color.DeviceColor.ColorSpaceSignature = state.DataColorSpaceSignature;
            color.DeviceColor.Components = deviceComponents;
            PrepareDescription(color, prefix + rootName + suffix, false);
            if (exactNames.ContainsKey(color.Description.Name))
            {
                duplicateCount = duplicateCount + 1;
            }
            else
            {
                exactNames.Add(color.Description.Name, colorIndex);
            }

            colors[colorIndex] = color;
            colorIndex = colorIndex + 1;
        }

        if (offset != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The namedColorType records end at byte {offset}, leaving {payload.Length - offset} unexpected bytes.");
            return;
        }

        IccNamedColorSet set = new IccNamedColorSet();
        set.VendorFlags = vendorFlags;
        set.Prefix = prefix;
        set.Suffix = suffix;
        set.DeviceChannelCount = channelCount;
        set.Colors = colors;
        CompleteNamedColorDecode(state, payload, typeSignature, set, "namedColorType", duplicateCount);
    }

    internal static void DecodeNamedColor2(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 84)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A namedColor2Type payload requires at least 84 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The namedColor2Type reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 12), state.AbsolutePayloadOffset + 8);
        uint vendorFlags = headerReader.ReadUInt32();
        uint encodedCount = headerReader.ReadUInt32();
        uint encodedDeviceCount = headerReader.ReadUInt32();
        if (encodedDeviceCount > IccConstants.MaximumIcc1ChannelCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The namedColor2Type device-coordinate count {encodedDeviceCount} exceeds the ICC.1 maximum of {IccConstants.MaximumIcc1ChannelCount}.");
            return;
        }

        int deviceCount = (int)encodedDeviceCount;
        if (state.HasProfileContext && deviceCount != 0)
        {
            int expectedDeviceCount = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
            if (expectedDeviceCount == 0 || deviceCount != expectedDeviceCount)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The namedColor2Type device-coordinate count {deviceCount} does not match data colour-space signature 0x{state.DataColorSpaceSignature:X8}, which defines {expectedDeviceCount} channels.");
                return;
            }
        }

        IccPcsKind pcsKind = state.ProfileConnectionSpace;
        if (pcsKind != IccPcsKind.Xyz && pcsKind != IccPcsKind.Lab)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "namedColor2Type requires PCSXYZ or PCSLAB profile context.");
            return;
        }

        string? prefix = DecodeFixedName(payload.Slice(20, 32));
        string? suffix = DecodeFixedName(payload.Slice(52, 32));
        if (prefix == null || suffix == null)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The namedColor2Type prefix and suffix shall be null-terminated 7-bit ASCII within their fixed 32-byte fields.");
            return;
        }

        int recordSize = 38 + (deviceCount * 2);
        int maximumCount = (payload.Length - 84) / recordSize;
        if (encodedCount > int.MaxValue || encodedCount > (uint)maximumCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The namedColor2Type count {encodedCount} cannot fit in the payload with {deviceCount} device coordinates per record.");
            return;
        }

        int colorCount = (int)encodedCount;
        long expectedSize = 84L + ((long)colorCount * recordSize);
        if (expectedSize != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The namedColor2Type count and device-coordinate count require exactly {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        IccNamedColor[] colors = new IccNamedColor[colorCount];
        System.Collections.Generic.Dictionary<string, int> exactNames = new System.Collections.Generic.Dictionary<string, int>();
        int duplicateCount = 0;
        int colorIndex = 0;
        while (colorIndex < colorCount)
        {
            int recordOffset = 84 + (colorIndex * recordSize);
            string? rootName = DecodeFixedName(payload.Slice(recordOffset, 32));
            if (rootName == null)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"namedColor2Type root name {colorIndex} shall be null-terminated 7-bit ASCII within its fixed 32-byte field.");
                return;
            }

            BigEndianReader coordinateReader = new BigEndianReader(payload.Slice(recordOffset + 32, recordSize - 32), state.AbsolutePayloadOffset + recordOffset + 32);
            ushort first = coordinateReader.ReadUInt16();
            ushort second = coordinateReader.ReadUInt16();
            ushort third = coordinateReader.ReadUInt16();
            if (pcsKind == IccPcsKind.Lab && first > 65280)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Named colour {colorIndex} has a legacy PCSLAB L* encoding greater than FF00h, which exceeds L*=100.");
                return;
            }

            IccNamedColor color = new IccNamedColor();
            color.Name = rootName;
            color.PcsColor.ColorSpace = pcsKind == IccPcsKind.Xyz ? IccColorSpaceKind.Xyz : IccColorSpaceKind.Lab;
            color.PcsColor.ColorSpaceSignature = pcsKind == IccPcsKind.Xyz ? IccColorSpaceSignatures.Xyz : IccColorSpaceSignatures.Lab;
            color.PcsColor.Components = DecodePcsCoordinates(first, second, third, pcsKind);
            color.DeviceColor.ColorSpace = HeaderCodec.DecodeColorSpace(state.DataColorSpaceSignature);
            color.DeviceColor.ColorSpaceSignature = state.DataColorSpaceSignature;
            color.DeviceColor.Components = new double[deviceCount];
            int channelIndex = 0;
            while (channelIndex < deviceCount)
            {
                color.DeviceColor.Components[channelIndex] = coordinateReader.ReadUInt16() / 65535.0;
                channelIndex = channelIndex + 1;
            }

            PrepareDescription(color, prefix + rootName + suffix, true);
            if (exactNames.ContainsKey(color.Description.Name))
            {
                duplicateCount = duplicateCount + 1;
            }
            else
            {
                exactNames.Add(color.Description.Name, colorIndex);
            }

            colors[colorIndex] = color;
            colorIndex = colorIndex + 1;
        }

        IccNamedColorSet set = new IccNamedColorSet();
        set.VendorFlags = vendorFlags;
        set.Prefix = prefix;
        set.Suffix = suffix;
        set.DeviceChannelCount = deviceCount;
        set.Colors = colors;
        CompleteNamedColorDecode(state, payload, typeSignature, set, "namedColor2Type", duplicateCount);
    }

    internal static byte[] SerializeNamedColor(IccNamedColorSet set, uint dataColorSpaceSignature)
    {
        ValidateSet(set, "namedColorType");
        int channelCount = ColorSpaceChannelCount.Decode(dataColorSpaceSignature);
        if (channelCount < 1 || channelCount > IccConstants.MaximumIcc1ChannelCount || set.DeviceChannelCount != channelCount)
        {
            throw new IccProfileWriteException($"namedColorType requires a 1-15 channel data colour space and exactly that many tint coordinates; signature 0x{dataColorSpaceSignature:X8} defines {channelCount} channels.");
        }

        ValidateVariableName(set.Prefix, "prefix");
        ValidateVariableName(set.Suffix, "suffix");
        int payloadSize = 18 + set.Prefix.Length + set.Suffix.Length;
        int colorIndex = 0;
        while (colorIndex < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[colorIndex];
            ValidateNamedColor(color, colorIndex, false, channelCount);
            ValidateVariableName(color.Name, $"root name {colorIndex}");
            payloadSize = CheckedSize.AccumulateBytes(payloadSize, color.Name.Length + 1 + channelCount, int.MaxValue);
            colorIndex = colorIndex + 1;
        }

        byte[] payload = new byte[payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.NamedColor);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32(set.VendorFlags);
        writer.WriteUInt32((uint)set.Colors.Length);
        WriteVariableName(writer, set.Prefix);
        WriteVariableName(writer, set.Suffix);
        colorIndex = 0;
        while (colorIndex < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[colorIndex];
            WriteVariableName(writer, color.Name);
            int channelIndex = 0;
            while (channelIndex < channelCount)
            {
                writer.WriteUInt8(EncodeUnit8(color.Tints[channelIndex], colorIndex, channelIndex));
                channelIndex = channelIndex + 1;
            }

            colorIndex = colorIndex + 1;
        }

        return payload;
    }

    internal static byte[] SerializeNamedColor2(IccNamedColorSet set, IccVersionKind profileVersion, IccPcsKind pcsKind, uint dataColorSpaceSignature)
    {
        ValidateSet(set, "namedColor2Type");
        if (profileVersion != IccVersionKind.Version2 && profileVersion != IccVersionKind.Version4)
        {
            throw new IccProfileWriteException("namedColor2Type is supported only by ICC.1 version 2 and version 4 profiles.");
        }

        if (pcsKind != IccPcsKind.Xyz && pcsKind != IccPcsKind.Lab)
        {
            throw new IccProfileWriteException("namedColor2Type requires PCSXYZ or PCSLAB coordinates.");
        }

        int deviceCount = set.DeviceChannelCount;
        if (deviceCount < 0 || deviceCount > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"The namedColor2Type device-coordinate count shall be between zero and {IccConstants.MaximumIcc1ChannelCount}.");
        }

        if (deviceCount != 0)
        {
            int expectedCount = ColorSpaceChannelCount.Decode(dataColorSpaceSignature);
            if (expectedCount == 0 || deviceCount != expectedCount)
            {
                throw new IccProfileWriteException($"The namedColor2Type device-coordinate count {deviceCount} does not match data colour-space signature 0x{dataColorSpaceSignature:X8}, which defines {expectedCount} channels.");
            }
        }

        ValidateFixedName(set.Prefix, "prefix");
        ValidateFixedName(set.Suffix, "suffix");
        int recordSize = 38 + (deviceCount * 2);
        int recordBytes = CheckedSize.MultiplyCount(set.Colors.Length, recordSize, int.MaxValue);
        int payloadSize = CheckedSize.AccumulateBytes(84, recordBytes, int.MaxValue);
        byte[] payload = new byte[payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.NamedColor2);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32(set.VendorFlags);
        writer.WriteUInt32((uint)set.Colors.Length);
        writer.WriteUInt32((uint)deviceCount);
        WriteFixedName(writer, set.Prefix);
        WriteFixedName(writer, set.Suffix);
        int colorIndex = 0;
        while (colorIndex < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[colorIndex];
            ValidateNamedColor(color, colorIndex, true, deviceCount);
            ValidateFixedName(color.Name, $"root name {colorIndex}");
            WriteFixedName(writer, color.Name);
            WritePcsCoordinates(writer, color.PcsColor.Components, pcsKind, colorIndex);
            int channelIndex = 0;
            while (channelIndex < deviceCount)
            {
                writer.WriteUInt16(EncodeUnit16(color.DeviceColor.Components[channelIndex], colorIndex, channelIndex));
                channelIndex = channelIndex + 1;
            }

            colorIndex = colorIndex + 1;
        }

        return payload;
    }

    internal static void CompleteNamedColorDecode(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, IccNamedColorSet set, string typeName, int duplicateCount)
    {
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.NamedColors);
        data.NamedColors = set;
        string message = $"Decoded {set.Colors.Length} named colours from {typeName}.";
        if (duplicateCount != 0)
        {
            message = $"Decoded {set.Colors.Length} named colours from {typeName} and found {duplicateCount} duplicate exact names.";
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static int FindVariableNameEnd(System.ReadOnlyMemory<byte> payload, int offset)
    {
        if (offset < 0 || offset >= payload.Length)
        {
            return -1;
        }

        int maximumEnd = offset + 32;
        if (maximumEnd > payload.Length)
        {
            maximumEnd = payload.Length;
        }

        int index = offset;
        while (index < maximumEnd)
        {
            byte value = payload.Span[index];
            if (value == 0)
            {
                return index;
            }

            if (value > 127)
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string? DecodeFixedName(System.ReadOnlyMemory<byte> bytes)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        int terminator = -1;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] > 127)
            {
                return null;
            }

            if (values[index] == 0 && terminator < 0)
            {
                terminator = index;
            }

            index = index + 1;
        }

        if (terminator < 0)
        {
            return null;
        }

        return DecodeAscii(bytes.Slice(0, terminator));
    }

    internal static string DecodeAscii(System.ReadOnlyMemory<byte> bytes)
    {
        return TagCodecSupport.DecodeSevenBitAscii(bytes, bytes.Length);
    }

    internal static double[] DecodePcsCoordinates(ushort first, ushort second, ushort third, IccPcsKind pcsKind)
    {
        if (pcsKind == IccPcsKind.Xyz)
        {
            return new double[] { first / 32768.0, second / 32768.0, third / 32768.0 };
        }

        return new double[] { first * 100.0 / 65280.0, (second / 256.0) - 128.0, (third / 256.0) - 128.0 };
    }

    internal static void PrepareDescription(IccNamedColor color, string exactName, bool hasPcs)
    {
        color.Description.Name = exactName;
        if (color.DeviceColor.Components.Length != 0)
        {
            IccColorVector device = new IccColorVector();
            device.ColorSpace = color.DeviceColor.ColorSpace;
            device.ColorSpaceSignature = color.DeviceColor.ColorSpaceSignature;
            device.Components = CopyComponents(color.DeviceColor.Components);
            color.Description.DeviceColor = device;
        }

        if (!hasPcs)
        {
            return;
        }

        if (color.PcsColor.ColorSpace == IccColorSpaceKind.Lab)
        {
            IccLabColor lab = new IccLabColor();
            lab.L = color.PcsColor.Components[0];
            lab.A = color.PcsColor.Components[1];
            lab.B = color.PcsColor.Components[2];
            color.Description.Lab = lab;

            double[] xyzValues = ColorMathCore.ConvertLabToXyz(
                new double[] { lab.L, lab.A, lab.B },
                new double[] { IccStandardColorimetry.D50X, IccStandardColorimetry.D50Y, IccStandardColorimetry.D50Z });
            IccXyzColor convertedXyz = new IccXyzColor();
            convertedXyz.X = xyzValues[0];
            convertedXyz.Y = xyzValues[1];
            convertedXyz.Z = xyzValues[2];
            color.Description.Xyz = convertedXyz;
            PrepareColorEquivalents(color.Description);
            return;
        }

        IccXyzColor xyz = new IccXyzColor();
        xyz.X = color.PcsColor.Components[0];
        xyz.Y = color.PcsColor.Components[1];
        xyz.Z = color.PcsColor.Components[2];
        color.Description.Xyz = xyz;
        color.Description.Lab = ConvertXyzToD50Lab(xyz);
        PrepareColorEquivalents(color.Description);
    }

    internal static void PrepareColorEquivalents(IccColorDescription description)
    {
        if (description.Xyz == null || description.Lab == null)
        {
            return;
        }

        double[] xyYValues = ColorMathCore.ConvertXyzToXyY(new double[]
        {
            description.Xyz.X,
            description.Xyz.Y,
            description.Xyz.Z
        });
        IccXyYColor xyY = new IccXyYColor();
        xyY.X = xyYValues[0];
        xyY.Y = xyYValues[1];
        xyY.Luminance = xyYValues[2];
        description.XyY = xyY;

        double[] lchValues = ColorMathCore.ConvertLabToLch(new double[]
        {
            description.Lab.L,
            description.Lab.A,
            description.Lab.B
        });
        IccLchColor lch = new IccLchColor();
        lch.L = lchValues[0];
        lch.C = lchValues[1];
        lch.HueDegrees = lchValues[2];
        description.Lch = lch;
    }

    internal static IccLabColor ConvertXyzToD50Lab(IccXyzColor xyz)
    {
        double x = ConvertLabFunction(xyz.X / IccStandardColorimetry.D50X);
        double y = ConvertLabFunction(xyz.Y / IccStandardColorimetry.D50Y);
        double z = ConvertLabFunction(xyz.Z / IccStandardColorimetry.D50Z);
        IccLabColor lab = new IccLabColor();
        lab.L = (116.0 * y) - 16.0;
        lab.A = 500.0 * (x - y);
        lab.B = 200.0 * (y - z);
        return lab;
    }

    internal static double ConvertLabFunction(double value)
    {
        if (value > IccStandardColorimetry.LabEpsilon)
        {
            return System.Math.Pow(value, 1.0 / 3.0);
        }

        return ((IccStandardColorimetry.LabKappa * value) + 16.0) / 116.0;
    }

    internal static double[] CopyComponents(double[] source)
    {
        double[] copy = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateSet(IccNamedColorSet set, string typeName)
    {
        if (set == null || set.Colors == null || set.Prefix == null || set.Suffix == null)
        {
            throw new IccProfileWriteException($"{typeName} data, colours, prefix, and suffix cannot be null.");
        }
    }

    internal static void ValidateNamedColor(IccNamedColor color, int colorIndex, bool requirePcs, int deviceCount)
    {
        if (color == null || color.Name == null || color.Tints == null || color.PcsColor == null || color.DeviceColor == null || color.PcsColor.Components == null || color.DeviceColor.Components == null)
        {
            throw new IccProfileWriteException($"Named colour {colorIndex} and its coordinate values cannot be null.");
        }

        if (requirePcs && color.PcsColor.Components.Length != 3)
        {
            throw new IccProfileWriteException($"Named colour {colorIndex} shall provide exactly three PCS coordinates.");
        }

        if (!requirePcs && color.Tints.Length != deviceCount)
        {
            throw new IccProfileWriteException($"Legacy named colour {colorIndex} shall provide exactly {deviceCount} tint coordinates.");
        }

        if (requirePcs && color.DeviceColor.Components.Length != deviceCount)
        {
            throw new IccProfileWriteException($"Named colour {colorIndex} shall provide exactly {deviceCount} device coordinates.");
        }
    }

    internal static void ValidateVariableName(string name, string fieldName)
    {
        if (name == null || name.Length > 31)
        {
            throw new IccProfileWriteException($"The namedColorType {fieldName} shall contain at most 31 characters before its null terminator.");
        }

        ValidateAsciiCharacters(name, fieldName);
    }

    internal static void ValidateFixedName(string name, string fieldName)
    {
        if (name == null || name.Length > 31)
        {
            throw new IccProfileWriteException($"The namedColor2Type {fieldName} shall contain at most 31 characters before its null terminator.");
        }

        ValidateAsciiCharacters(name, fieldName);
    }

    internal static void ValidateAsciiCharacters(string name, string fieldName)
    {
        int index = 0;
        while (index < name.Length)
        {
            if (name[index] == 0 || name[index] > 127)
            {
                throw new IccProfileWriteException($"Named-colour {fieldName} shall contain only non-null 7-bit ASCII characters.");
            }

            index = index + 1;
        }
    }

    internal static void WriteVariableName(BigEndianWriter writer, string name)
    {
        int index = 0;
        while (index < name.Length)
        {
            writer.WriteUInt8((byte)name[index]);
            index = index + 1;
        }

        writer.WriteUInt8(0);
    }

    internal static void WriteFixedName(BigEndianWriter writer, string name)
    {
        int index = 0;
        while (index < name.Length)
        {
            writer.WriteUInt8((byte)name[index]);
            index = index + 1;
        }

        writer.WriteZeroBytes(32 - name.Length);
    }

    internal static void WritePcsCoordinates(BigEndianWriter writer, double[] components, IccPcsKind pcsKind, int colorIndex)
    {
        if (pcsKind == IccPcsKind.Xyz)
        {
            writer.WriteUInt16(EncodeScaledCoordinate(components[0], 32768.0, 65535.0, colorIndex, "X"));
            writer.WriteUInt16(EncodeScaledCoordinate(components[1], 32768.0, 65535.0, colorIndex, "Y"));
            writer.WriteUInt16(EncodeScaledCoordinate(components[2], 32768.0, 65535.0, colorIndex, "Z"));
            return;
        }

        writer.WriteUInt16(EncodeScaledCoordinate(components[0], 652.8, 65280.0, colorIndex, "L*"));
        writer.WriteUInt16(EncodeScaledCoordinate(components[1] + 128.0, 256.0, 65535.0, colorIndex, "a*"));
        writer.WriteUInt16(EncodeScaledCoordinate(components[2] + 128.0, 256.0, 65535.0, colorIndex, "b*"));
    }

    internal static ushort EncodeScaledCoordinate(double value, double scale, double maximum, int colorIndex, string componentName)
    {
        if (System.Double.IsNaN(value) || System.Double.IsInfinity(value))
        {
            throw new IccProfileWriteException($"Named colour {colorIndex} PCS component {componentName} shall be finite.");
        }

        double encoded = System.Math.Round(value * scale, System.MidpointRounding.AwayFromZero);
        if (encoded < 0.0 || encoded > maximum)
        {
            throw new IccProfileWriteException($"Named colour {colorIndex} PCS component {componentName} cannot be represented by namedColor2Type.");
        }

        return (ushort)encoded;
    }

    internal static byte EncodeUnit8(double value, int colorIndex, int channelIndex)
    {
        if (System.Double.IsNaN(value) || System.Double.IsInfinity(value) || value < 0.0 || value > 1.0)
        {
            throw new IccProfileWriteException($"Legacy named colour {colorIndex} tint coordinate {channelIndex} shall be finite and between zero and one.");
        }

        return (byte)System.Math.Round(value * 255.0, System.MidpointRounding.AwayFromZero);
    }

    internal static ushort EncodeUnit16(double value, int colorIndex, int channelIndex)
    {
        if (System.Double.IsNaN(value) || System.Double.IsInfinity(value) || value < 0.0 || value > 1.0)
        {
            throw new IccProfileWriteException($"Named colour {colorIndex} device coordinate {channelIndex} shall be finite and between zero and one.");
        }

        return (ushort)System.Math.Round(value * 65535.0, System.MidpointRounding.AwayFromZero);
    }
}
