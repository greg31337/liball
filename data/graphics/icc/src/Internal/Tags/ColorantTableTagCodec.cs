namespace icc;

internal static class ColorantTableTagCodec
{
    internal static void DecodeColorantTable(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A colorantTableType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The colorantTableType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint encodedCount = headerReader.ReadUInt32();
        if (encodedCount < 1 || encodedCount > IccConstants.MaximumIcc1ChannelCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The colorantTableType channel count {encodedCount} shall be between 1 and {IccConstants.MaximumIcc1ChannelCount}.");
            return;
        }

        int channelCount = (int)encodedCount;
        int expectedSize = 12 + (channelCount * 38);
        if (payload.Length != expectedSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The colorantTableType channel count {channelCount} requires exactly {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        uint associatedSignature = state.DataColorSpaceSignature;
        if (state.TagSignature == IccTagSignatures.ColorantTableOut)
        {
            if (state.HasProfileContext && state.ProfileClass != IccProfileClass.DeviceLink)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The colorantTableOutTag is permitted only in a DeviceLink profile.");
                return;
            }

            associatedSignature = state.ProfileConnectionSpaceSignature;
        }

        if (state.HasProfileContext)
        {
            int expectedCount = ColorSpaceChannelCount.Decode(associatedSignature);
            if (expectedCount == 0 || channelCount != expectedCount)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The colorantTableType channel count {channelCount} does not match the associated colour-space signature 0x{associatedSignature:X8}, which defines {expectedCount} channels.");
                return;
            }
        }

        IccPcsKind pcsKind = state.ProfileConnectionSpace;
        if (state.ProfileClass == IccProfileClass.DeviceLink)
        {
            pcsKind = IccPcsKind.Lab;
        }

        if (pcsKind != IccPcsKind.Xyz && pcsKind != IccPcsKind.Lab)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "colorantTableType coordinates require either PCSXYZ or PCSLAB interpretation.");
            return;
        }

        IccColorant[] colorants = new IccColorant[channelCount];
        System.Collections.Generic.Dictionary<string, int> names = new System.Collections.Generic.Dictionary<string, int>();
        int duplicateCount = 0;
        int index = 0;
        while (index < channelCount)
        {
            int recordOffset = 12 + (index * 38);
            string? name = DecodeName(payload.Slice(recordOffset, 32));
            if (name == null)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Colorant name {index} shall be 7-bit ASCII, null terminated, and zero-filled through its fixed 32-byte field.");
                return;
            }

            if (names.ContainsKey(name))
            {
                duplicateCount = duplicateCount + 1;
            }
            else
            {
                names.Add(name, index);
            }

            BigEndianReader pcsReader = new BigEndianReader(payload.Slice(recordOffset + 32, 6), state.AbsolutePayloadOffset + recordOffset + 32);
            ushort first = pcsReader.ReadUInt16();
            ushort second = pcsReader.ReadUInt16();
            ushort third = pcsReader.ReadUInt16();
            if (pcsKind == IccPcsKind.Lab && state.ProfileVersion == IccVersionKind.Version2 && first > 65280)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Colorant {index} has a legacy PCSLAB L* encoding greater than FF00h, which exceeds L*=100.");
                return;
            }

            IccColorant colorant = new IccColorant();
            colorant.Name = name;
            colorant.PcsColor.ColorSpace = pcsKind == IccPcsKind.Xyz ? IccColorSpaceKind.Xyz : IccColorSpaceKind.Lab;
            colorant.PcsColor.ColorSpaceSignature = pcsKind == IccPcsKind.Xyz ? IccColorSpaceSignatures.Xyz : IccColorSpaceSignatures.Lab;
            colorant.PcsColor.Components = DecodePcs(first, second, third, pcsKind, state.ProfileVersion);
            colorants[index] = colorant;
            index = index + 1;
        }

        IccColorantSet set = new IccColorantSet();
        set.ColorSpaceSignature = associatedSignature;
        set.Colorants = colorants;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Colorants);
        data.Colorants = set;
        string message = $"Decoded colorantTableType data for {channelCount} channels.";
        if (duplicateCount != 0)
        {
            message = $"Decoded colorantTableType data with {duplicateCount} duplicate colourant name occurrences.";
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static byte[] SerializeColorantTable(IccColorantSet set, IccVersionKind profileVersion, IccPcsKind profileConnectionSpace, bool deviceLinkData)
    {
        if (set == null || set.Colorants == null || set.Colorants.Length < 1 || set.Colorants.Length > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"A colorant table shall contain between 1 and {IccConstants.MaximumIcc1ChannelCount} colorants.");
        }

        int expectedCount = ColorSpaceChannelCount.Decode(set.ColorSpaceSignature);
        if (expectedCount == 0 || expectedCount != set.Colorants.Length)
        {
            throw new IccProfileWriteException($"The colorant table contains {set.Colorants.Length} records, which does not match associated colour-space signature 0x{set.ColorSpaceSignature:X8} and its {expectedCount} channels.");
        }

        IccPcsKind pcsKind = deviceLinkData ? IccPcsKind.Lab : profileConnectionSpace;
        if (pcsKind != IccPcsKind.Xyz && pcsKind != IccPcsKind.Lab)
        {
            throw new IccProfileWriteException("colorantTableType coordinates require either PCSXYZ or PCSLAB interpretation.");
        }

        if (profileVersion != IccVersionKind.Version2 && profileVersion != IccVersionKind.Version4)
        {
            throw new IccProfileWriteException("colorantTableType is supported only by ICC.1 version 2 and version 4 profiles.");
        }

        byte[] payload = new byte[12 + (set.Colorants.Length * 38)];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ColorantTable);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)set.Colorants.Length);
        System.Collections.Generic.Dictionary<string, int> names = new System.Collections.Generic.Dictionary<string, int>();
        int index = 0;
        while (index < set.Colorants.Length)
        {
            IccColorant colorant = set.Colorants[index];
            if (colorant == null || colorant.PcsColor == null || colorant.PcsColor.Components == null || colorant.PcsColor.Components.Length != 3)
            {
                throw new IccProfileWriteException($"Colorant record {index} and its three PCS coordinates cannot be null.");
            }

            ValidateName(colorant.Name, index);
            if (names.ContainsKey(colorant.Name))
            {
                throw new IccProfileWriteException($"Colourant name '{colorant.Name}' occurs more than once; colorantTableType names shall be unique.");
            }

            names.Add(colorant.Name, index);
            WriteName(writer, colorant.Name);
            WritePcs(writer, colorant.PcsColor.Components, pcsKind, profileVersion, index);
            index = index + 1;
        }

        return payload;
    }

    internal static string? DecodeName(System.ReadOnlyMemory<byte> bytes)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        int terminator = -1;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == 0 && terminator < 0)
            {
                terminator = index;
            }
            else if (terminator < 0 && values[index] > 127)
            {
                return null;
            }
            else if (terminator >= 0 && values[index] != 0)
            {
                return null;
            }

            index = index + 1;
        }

        if (terminator < 0)
        {
            return null;
        }

        return TagCodecSupport.DecodeSevenBitAscii(bytes, terminator);
    }

    internal static double[] DecodePcs(ushort first, ushort second, ushort third, IccPcsKind pcsKind, IccVersionKind profileVersion)
    {
        if (pcsKind == IccPcsKind.Xyz)
        {
            return new double[] { first / 32768.0, second / 32768.0, third / 32768.0 };
        }

        if (profileVersion == IccVersionKind.Version2)
        {
            return new double[] { first * 100.0 / 65280.0, (second / 256.0) - 128.0, (third / 256.0) - 128.0 };
        }

        return new double[] { first * 100.0 / 65535.0, (second * 255.0 / 65535.0) - 128.0, (third * 255.0 / 65535.0) - 128.0 };
    }

    internal static void ValidateName(string name, int colorantIndex)
    {
        if (name == null || name.Length > 31)
        {
            throw new IccProfileWriteException($"Colorant name {colorantIndex} shall contain at most 31 7-bit ASCII characters so the fixed 32-byte field remains null terminated.");
        }

        int index = 0;
        while (index < name.Length)
        {
            if (name[index] == 0 || name[index] > 127)
            {
                throw new IccProfileWriteException($"Colorant name {colorantIndex} shall contain only non-null 7-bit ASCII characters.");
            }

            index = index + 1;
        }
    }

    internal static void WriteName(BigEndianWriter writer, string name)
    {
        int index = 0;
        while (index < name.Length)
        {
            writer.WriteUInt8((byte)name[index]);
            index = index + 1;
        }

        writer.WriteZeroBytes(32 - name.Length);
    }

    internal static void WritePcs(BigEndianWriter writer, double[] components, IccPcsKind pcsKind, IccVersionKind profileVersion, int colorantIndex)
    {
        if (pcsKind == IccPcsKind.Xyz)
        {
            writer.WriteUInt16(EncodeComponent(components[0], 32768.0, 0.0, 65535.0, colorantIndex, "X"));
            writer.WriteUInt16(EncodeComponent(components[1], 32768.0, 0.0, 65535.0, colorantIndex, "Y"));
            writer.WriteUInt16(EncodeComponent(components[2], 32768.0, 0.0, 65535.0, colorantIndex, "Z"));
            return;
        }

        if (profileVersion == IccVersionKind.Version2)
        {
            writer.WriteUInt16(EncodeComponent(components[0], 652.8, 0.0, 65280.0, colorantIndex, "L*"));
            writer.WriteUInt16(EncodeComponent(components[1] + 128.0, 256.0, 0.0, 65535.0, colorantIndex, "a*"));
            writer.WriteUInt16(EncodeComponent(components[2] + 128.0, 256.0, 0.0, 65535.0, colorantIndex, "b*"));
            return;
        }

        writer.WriteUInt16(EncodeComponent(components[0], 655.35, 0.0, 65535.0, colorantIndex, "L*"));
        writer.WriteUInt16(EncodeComponent(components[1] + 128.0, 65535.0 / 255.0, 0.0, 65535.0, colorantIndex, "a*"));
        writer.WriteUInt16(EncodeComponent(components[2] + 128.0, 65535.0 / 255.0, 0.0, 65535.0, colorantIndex, "b*"));
    }

    internal static ushort EncodeComponent(double value, double scale, double minimumEncoded, double maximumEncoded, int colorantIndex, string componentName)
    {
        if (System.Double.IsNaN(value) || System.Double.IsInfinity(value))
        {
            throw new IccProfileWriteException($"Colorant {colorantIndex} PCS component {componentName} shall be finite.");
        }

        double encoded = System.Math.Round(value * scale, System.MidpointRounding.AwayFromZero);
        if (encoded < minimumEncoded || encoded > maximumEncoded)
        {
            throw new IccProfileWriteException($"Colorant {colorantIndex} PCS component {componentName} cannot be represented by colorantTableType.");
        }

        return (ushort)encoded;
    }
}
