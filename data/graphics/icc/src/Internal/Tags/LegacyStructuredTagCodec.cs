namespace icc;

internal class LegacyAsciiFieldDecodeResult
{
    internal string Error = string.Empty;
    internal string Text = string.Empty;
    internal int NextOffset;
}

internal static class LegacyStructuredTagCodec
{
    internal const uint ScreeningFlagMask = 3;

    internal static void DecodeCrdInfo(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 29)
        {
            CompleteInvalid(state, payload, typeSignature, "A crdInfoType requires its header, five count fields, and a terminated product name.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteInvalid(state, payload, typeSignature, "The crdInfoType reserved bytes shall be zero.");
            return;
        }

        LegacyAsciiFieldDecodeResult product = DecodeCountedAscii(payload, 8, false, "PostScript product name");
        if (product.Error.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, product.Error);
            return;
        }

        string[] names = new string[4];
        int offset = product.NextOffset;
        int index = 0;
        while (index < names.Length)
        {
            LegacyAsciiFieldDecodeResult name = DecodeCountedAscii(payload, offset, true, $"rendering intent {index} CRD name");
            if (name.Error.Length > 0)
            {
                CompleteInvalid(state, payload, typeSignature, name.Error);
                return;
            }

            names[index] = name.Text;
            offset = name.NextOffset;
            index = index + 1;
        }

        if (offset != payload.Length)
        {
            CompleteInvalid(state, payload, typeSignature, $"The crdInfoType fields end at byte {offset}, leaving {payload.Length - offset} unexpected bytes.");
            return;
        }

        IccCrdInfo info = new IccCrdInfo();
        info.ProductName = product.Text;
        info.RenderingIntentCrdNames = names;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.CrdInfo);
        data.CrdInfo = info;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded legacy crdInfoType product and rendering-intent CRD names.");
    }

    internal static byte[] SerializeCrdInfo(IccCrdInfo info)
    {
        if (info == null || info.ProductName == null || info.RenderingIntentCrdNames == null || info.RenderingIntentCrdNames.Length != 4)
        {
            throw new IccProfileWriteException("CRD information requires a product name and exactly four rendering-intent name fields.");
        }

        ValidateAsciiString(info.ProductName, "PostScript product name");
        long size = 8L + 4L + info.ProductName.Length + 1L;
        int index = 0;
        while (index < info.RenderingIntentCrdNames.Length)
        {
            string name = info.RenderingIntentCrdNames[index];
            if (name == null)
            {
                throw new IccProfileWriteException($"Rendering intent {index} CRD name cannot be null.");
            }

            ValidateAsciiString(name, $"rendering intent {index} CRD name");
            size = size + 4L;
            if (name.Length > 0)
            {
                size = size + name.Length + 1L;
            }

            index = index + 1;
        }

        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException("The crdInfoType encoding exceeds the supported managed size.");
        }

        byte[] payload = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.CrdInfo);
        writer.WriteZeroBytes(4);
        WriteCountedAscii(writer, info.ProductName, false);
        index = 0;
        while (index < info.RenderingIntentCrdNames.Length)
        {
            WriteCountedAscii(writer, info.RenderingIntentCrdNames[index], true);
            index = index + 1;
        }

        return payload;
    }

    internal static void DecodeScreening(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 16)
        {
            CompleteInvalid(state, payload, typeSignature, "A screeningType payload requires a 16-byte header.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteInvalid(state, payload, typeSignature, "The screeningType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 8), state.AbsolutePayloadOffset + 8);
        uint flags = headerReader.ReadUInt32();
        uint encodedCount = headerReader.ReadUInt32();
        if ((flags & ~ScreeningFlagMask) != 0)
        {
            CompleteInvalid(state, payload, typeSignature, $"The screeningType flag value 0x{flags:X8} uses reserved bits.");
            return;
        }

        if (encodedCount > IccConstants.MaximumIcc1ChannelCount)
        {
            CompleteInvalid(state, payload, typeSignature, $"A screeningType cannot contain more than {IccConstants.MaximumIcc1ChannelCount} channels.");
            return;
        }

        int channelCount = (int)encodedCount;
        int expectedLength = 16 + (channelCount * 12);
        if (payload.Length != expectedLength)
        {
            CompleteInvalid(state, payload, typeSignature, $"A {channelCount}-channel screeningType requires exactly {expectedLength} bytes, but this payload contains {payload.Length}.");
            return;
        }

        IccScreeningChannel[] channels = new IccScreeningChannel[channelCount];
        BigEndianReader channelReader = new BigEndianReader(payload.Slice(16), state.AbsolutePayloadOffset + 16);
        int index = 0;
        while (index < channels.Length)
        {
            IccScreeningChannel channel = new IccScreeningChannel();
            channel.Frequency = IccNumberCodec.ReadS15Fixed16(channelReader);
            channel.AngleDegrees = IccNumberCodec.ReadS15Fixed16(channelReader);
            uint spotShape = channelReader.ReadUInt32();
            if (spotShape > (uint)IccScreeningSpotShape.Cross)
            {
                CompleteInvalid(state, payload, typeSignature, $"Screening channel {index} has undefined spot-shape value {spotShape}.");
                return;
            }

            channel.SpotShape = (IccScreeningSpotShape)spotShape;
            channels[index] = channel;
            index = index + 1;
        }

        IccScreening screening = new IccScreening();
        screening.UsesPrinterDefaultScreens = (flags & 1) != 0;
        screening.FrequencyIsLinesPerInch = (flags & 2) != 0;
        screening.Channels = channels;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Screening);
        data.Screening = screening;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded legacy screeningType data for {channelCount} channels.");
    }

    internal static byte[] SerializeScreening(IccScreening screening)
    {
        if (screening == null || screening.Channels == null)
        {
            throw new IccProfileWriteException("Screening data and its channel array cannot be null.");
        }

        if (screening.Channels.Length > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"A screeningType cannot contain more than {IccConstants.MaximumIcc1ChannelCount} channels.");
        }

        byte[] payload = new byte[16 + (screening.Channels.Length * 12)];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Screening);
        writer.WriteZeroBytes(4);
        uint flags = 0;
        if (screening.UsesPrinterDefaultScreens)
        {
            flags = flags | 1;
        }

        if (screening.FrequencyIsLinesPerInch)
        {
            flags = flags | 2;
        }

        writer.WriteUInt32(flags);
        writer.WriteUInt32((uint)screening.Channels.Length);
        int index = 0;
        while (index < screening.Channels.Length)
        {
            IccScreeningChannel channel = screening.Channels[index];
            if (channel == null || channel.SpotShape < IccScreeningSpotShape.Unknown || channel.SpotShape > IccScreeningSpotShape.Cross)
            {
                throw new IccProfileWriteException($"Screening channel {index} has invalid data or an undefined spot shape.");
            }

            IccNumberCodec.WriteS15Fixed16(writer, channel.Frequency);
            IccNumberCodec.WriteS15Fixed16(writer, channel.AngleDegrees);
            writer.WriteUInt32((uint)channel.SpotShape);
            index = index + 1;
        }

        return payload;
    }

    internal static void DecodeUcrBg(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 17)
        {
            CompleteInvalid(state, payload, typeSignature, "A ucrbgType requires two curve counts and a null-terminated description.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteInvalid(state, payload, typeSignature, "The ucrbgType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader countReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint encodedUcrCount = countReader.ReadUInt32();
        if (encodedUcrCount > int.MaxValue || ((long)encodedUcrCount * 2L) > payload.Length - 17L)
        {
            CompleteInvalid(state, payload, typeSignature, $"The ucrbgType UCR count {encodedUcrCount} cannot fit in the payload.");
            return;
        }

        int ucrCount = (int)encodedUcrCount;
        ushort[] ucrValues = ReadUInt16Values(payload, 12, ucrCount);
        int bgCountOffset = 12 + (ucrCount * 2);
        BigEndianReader bgCountReader = new BigEndianReader(payload.Slice(bgCountOffset, 4), state.AbsolutePayloadOffset + bgCountOffset);
        uint encodedBgCount = bgCountReader.ReadUInt32();
        int bgValuesOffset = bgCountOffset + 4;
        if (encodedBgCount > int.MaxValue || ((long)encodedBgCount * 2L) > payload.Length - bgValuesOffset - 1L)
        {
            CompleteInvalid(state, payload, typeSignature, $"The ucrbgType black-generation count {encodedBgCount} cannot fit in the payload.");
            return;
        }

        int bgCount = (int)encodedBgCount;
        ushort[] bgValues = ReadUInt16Values(payload, bgValuesOffset, bgCount);
        int descriptionOffset = bgValuesOffset + (bgCount * 2);
        System.ReadOnlyMemory<byte> descriptionBytes = payload.Slice(descriptionOffset);
        string descriptionError = ValidateTerminatedAscii(descriptionBytes, "ucrbgType description");
        if (descriptionError.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, descriptionError);
            return;
        }

        if ((ucrValues.Length == 1 && ucrValues[0] > 100) || (bgValues.Length == 1 && bgValues[0] > 100))
        {
            CompleteInvalid(state, payload, typeSignature, "A single-entry ucrbgType curve is a percentage and shall not exceed 100.");
            return;
        }

        IccUcrBg ucrBg = new IccUcrBg();
        ucrBg.UnderColorRemovalValues = ucrValues;
        ucrBg.BlackGenerationValues = bgValues;
        ucrBg.Description = DecodeAscii(descriptionBytes.Slice(0, descriptionBytes.Length - 1));
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.UcrBg);
        data.UcrBg = ucrBg;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded legacy under-colour-removal and black-generation curves.");
    }

    internal static byte[] SerializeUcrBg(IccUcrBg ucrBg)
    {
        if (ucrBg == null || ucrBg.UnderColorRemovalValues == null || ucrBg.BlackGenerationValues == null || ucrBg.Description == null)
        {
            throw new IccProfileWriteException("UCR/BG curves and description cannot be null.");
        }

        ValidateAsciiString(ucrBg.Description, "UCR/BG description");
        ValidatePercentageCurve(ucrBg.UnderColorRemovalValues, "UCR");
        ValidatePercentageCurve(ucrBg.BlackGenerationValues, "black-generation");
        long size = 17L + ((long)ucrBg.UnderColorRemovalValues.Length * 2L) + ((long)ucrBg.BlackGenerationValues.Length * 2L) + ucrBg.Description.Length;
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException("The ucrbgType encoding exceeds the supported managed size.");
        }

        byte[] payload = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.UcrBg);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)ucrBg.UnderColorRemovalValues.Length);
        WriteUInt16Values(writer, ucrBg.UnderColorRemovalValues);
        writer.WriteUInt32((uint)ucrBg.BlackGenerationValues.Length);
        WriteUInt16Values(writer, ucrBg.BlackGenerationValues);
        WriteAscii(writer, ucrBg.Description);
        writer.WriteUInt8(0);
        return payload;
    }

    internal static void DecodeDeviceSettingsRaw(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        string error = GetDeviceSettingsError(payload);
        if (error.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, error);
            return;
        }

        IccTagData data = new IccTagData();
        UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.Success);
        result.Message = "Recognized and structurally validated legacy deviceSettingsType data; the extensible payload was retained losslessly as raw data.";
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static byte[] SerializeDeviceSettingsRaw(IccRawData rawData)
    {
        if (rawData == null || rawData.Bytes == null)
        {
            throw new IccProfileWriteException("Raw device-settings data cannot be null.");
        }

        System.ReadOnlyMemory<byte> payload = new System.ReadOnlyMemory<byte>(rawData.Bytes);
        string error = GetDeviceSettingsError(payload);
        if (error.Length > 0)
        {
            throw new IccProfileWriteException(error);
        }

        byte[] clone = new byte[rawData.Bytes.Length];
        System.Array.Copy(rawData.Bytes, clone, clone.Length);
        return clone;
    }

    internal static bool ValidateV2TagTypeMapping(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (state.ProfileVersion != IccVersionKind.Version2 || !state.HasProfileContext || state.TagSignature == 0)
        {
            return true;
        }

        uint expectedType = GetMappedType(state.TagSignature);
        if (expectedType != 0 && expectedType != typeSignature)
        {
            CompleteInvalid(state, payload, typeSignature, $"Legacy v2 tag '{FourCcCodec.Format(state.TagSignature)}' requires tag type '{FourCcCodec.Format(expectedType)}', not '{FourCcCodec.Format(typeSignature)}'.");
            return false;
        }

        uint requiredTag = GetExclusiveTag(typeSignature);
        if (requiredTag != 0 && state.TagSignature != requiredTag)
        {
            CompleteInvalid(state, payload, typeSignature, $"Legacy v2 tag type '{FourCcCodec.Format(typeSignature)}' is permitted only for tag '{FourCcCodec.Format(requiredTag)}'.");
            return false;
        }

        return true;
    }

    internal static bool HasLegacyTagMapping(uint tagSignature)
    {
        return GetMappedType(tagSignature) != 0;
    }

    internal static uint GetMappedType(uint tagSignature)
    {
        switch (tagSignature)
        {
            case IccTagSignatures.CrdInfo:
                return IccTypeSignatures.CrdInfo;
            case IccTagSignatures.NamedColor:
                return IccTypeSignatures.NamedColor;
            case IccTagSignatures.PostScriptCrd0:
            case IccTagSignatures.PostScriptCrd1:
            case IccTagSignatures.PostScriptCrd2:
            case IccTagSignatures.PostScriptCrd3:
            case IccTagSignatures.PostScriptCsa:
            case IccTagSignatures.PostScriptRenderingIntent:
                return IccTypeSignatures.Data;
            case IccTagSignatures.ScreeningDescription:
                return IccTypeSignatures.TextDescription;
            case IccTagSignatures.Screening:
                return IccTypeSignatures.Screening;
            case IccTagSignatures.UcrBg:
                return IccTypeSignatures.UcrBg;
            case IccTagSignatures.DeviceSettings:
                return IccTypeSignatures.DeviceSettings;
            default:
                return 0;
        }
    }

    internal static uint GetExclusiveTag(uint typeSignature)
    {
        switch (typeSignature)
        {
            case IccTypeSignatures.CrdInfo:
                return IccTagSignatures.CrdInfo;
            case IccTypeSignatures.NamedColor:
                return IccTagSignatures.NamedColor;
            case IccTypeSignatures.Screening:
                return IccTagSignatures.Screening;
            case IccTypeSignatures.UcrBg:
                return IccTagSignatures.UcrBg;
            case IccTypeSignatures.DeviceSettings:
                return IccTagSignatures.DeviceSettings;
            default:
                return 0;
        }
    }

    internal static string GetDeviceSettingsError(System.ReadOnlyMemory<byte> payload)
    {
        if (payload.Length < 12)
        {
            return "A deviceSettingsType payload requires a 12-byte header.";
        }

        if (ReadUInt32(payload, 0) != IccTypeSignatures.DeviceSettings)
        {
            return "Raw device-settings data does not begin with the deviceSettingsType signature.";
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            return "The deviceSettingsType reserved bytes shall be zero.";
        }

        uint encodedPlatformCount = ReadUInt32(payload, 8);
        if (encodedPlatformCount > (uint)((payload.Length - 12) / 12))
        {
            return $"The deviceSettingsType platform count {encodedPlatformCount} cannot fit in the payload.";
        }

        int platformCount = (int)encodedPlatformCount;
        int offset = 12;
        int platformIndex = 0;
        while (platformIndex < platformCount)
        {
            if (offset > payload.Length - 12)
            {
                return $"The deviceSettingsType platform count declares platform {platformIndex}, whose header cannot fit in the payload.";
            }

            int platformStart = offset;
            uint platformSignature = ReadUInt32(payload, platformStart);
            uint encodedPlatformSize = ReadUInt32(payload, platformStart + 4);
            uint encodedCombinationCount = ReadUInt32(payload, platformStart + 8);
            if (!IsRegisteredPlatform(platformSignature))
            {
                return $"Device-settings platform {platformIndex} has unregistered signature 0x{platformSignature:X8}.";
            }

            if (encodedPlatformSize > int.MaxValue || encodedPlatformSize < 12 || encodedPlatformSize > payload.Length - platformStart)
            {
                return $"Device-settings platform {platformIndex} has an invalid structure size {encodedPlatformSize}.";
            }

            int platformEnd = platformStart + (int)encodedPlatformSize;
            offset = platformStart + 12;
            if (encodedCombinationCount > (uint)((platformEnd - offset) / 8))
            {
                return $"Device-settings platform {platformIndex} combination count {encodedCombinationCount} cannot fit in its structure.";
            }

            int combinationIndex = 0;
            while (combinationIndex < (int)encodedCombinationCount)
            {
                if (offset > platformEnd - 8)
                {
                    return $"Device-settings platform {platformIndex} combination count declares combination {combinationIndex}, whose header cannot fit in the structure.";
                }

                int combinationStart = offset;
                uint encodedCombinationSize = ReadUInt32(payload, combinationStart);
                uint encodedSettingCount = ReadUInt32(payload, combinationStart + 4);
                if (encodedCombinationSize > int.MaxValue || encodedCombinationSize < 8 || encodedCombinationSize > platformEnd - combinationStart)
                {
                    return $"Device-settings platform {platformIndex}, combination {combinationIndex} has an invalid structure size {encodedCombinationSize}.";
                }

                int combinationEnd = combinationStart + (int)encodedCombinationSize;
                offset = combinationStart + 8;
                if (encodedSettingCount > (uint)((combinationEnd - offset) / 12))
                {
                    return $"Device-settings platform {platformIndex}, combination {combinationIndex} setting count {encodedSettingCount} cannot fit in its structure.";
                }

                int settingIndex = 0;
                while (settingIndex < (int)encodedSettingCount)
                {
                    if (offset > combinationEnd - 12)
                    {
                        return $"Device-settings platform {platformIndex}, combination {combinationIndex} setting count declares setting {settingIndex}, whose header cannot fit in the structure.";
                    }

                    uint valueSize = ReadUInt32(payload, offset + 4);
                    uint valueCount = ReadUInt32(payload, offset + 8);
                    if (valueSize == 0 && valueCount != 0)
                    {
                        return $"Device-settings platform {platformIndex}, combination {combinationIndex}, setting {settingIndex} declares values with a zero byte size.";
                    }

                    int availableValueBytes = combinationEnd - offset - 12;
                    if (valueCount != 0 && (long)valueSize > availableValueBytes / (long)valueCount)
                    {
                        return $"Device-settings platform {platformIndex}, combination {combinationIndex}, setting {settingIndex} values exceed the enclosing structure.";
                    }

                    int valueBytes = (int)((long)valueSize * valueCount);
                    offset = offset + 12 + valueBytes;
                    settingIndex = settingIndex + 1;
                }

                if (offset != combinationEnd)
                {
                    return $"Device-settings platform {platformIndex}, combination {combinationIndex} contains unreferenced bytes.";
                }

                combinationIndex = combinationIndex + 1;
            }

            if (offset != platformEnd)
            {
                return $"Device-settings platform {platformIndex} contains unreferenced bytes.";
            }

            platformIndex = platformIndex + 1;
        }

        if (offset != payload.Length)
        {
            return $"The deviceSettingsType contains {payload.Length - offset} trailing or unreferenced bytes.";
        }

        return string.Empty;
    }

    internal static LegacyAsciiFieldDecodeResult DecodeCountedAscii(System.ReadOnlyMemory<byte> payload, int offset, bool allowsAbsent, string fieldName)
    {
        LegacyAsciiFieldDecodeResult result = new LegacyAsciiFieldDecodeResult();
        if (offset > payload.Length - 4)
        {
            result.Error = $"The crdInfoType is truncated before the {fieldName} count.";
            return result;
        }

        uint encodedCount = ReadUInt32(payload, offset);
        offset = offset + 4;
        if (encodedCount == 0)
        {
            if (!allowsAbsent)
            {
                result.Error = $"The crdInfoType {fieldName} shall include a terminating NULL.";
                return result;
            }

            result.NextOffset = offset;
            return result;
        }

        if (encodedCount > int.MaxValue || encodedCount > (uint)(payload.Length - offset))
        {
            result.Error = $"The crdInfoType {fieldName} count {encodedCount} cannot fit in the payload.";
            return result;
        }

        int count = (int)encodedCount;
        System.ReadOnlyMemory<byte> bytes = payload.Slice(offset, count);
        string error = ValidateTerminatedAscii(bytes, $"crdInfoType {fieldName}");
        if (error.Length > 0)
        {
            result.Error = error;
            return result;
        }

        result.Text = DecodeAscii(bytes.Slice(0, count - 1));
        result.NextOffset = offset + count;
        return result;
    }

    internal static string ValidateTerminatedAscii(System.ReadOnlyMemory<byte> bytes, string fieldName)
    {
        if (bytes.Length == 0 || bytes.Span[bytes.Length - 1] != 0)
        {
            return $"The {fieldName} shall end with a NULL byte.";
        }

        int index = 0;
        while (index < bytes.Length - 1)
        {
            if (bytes.Span[index] == 0 || bytes.Span[index] > 127)
            {
                return $"The {fieldName} shall contain only non-NULL 7-bit ASCII characters before its terminator.";
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static void ValidateAsciiString(string text, string fieldName)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == 0 || text[index] > 127)
            {
                throw new IccProfileWriteException($"The {fieldName} shall contain only non-NULL 7-bit ASCII characters.");
            }

            index = index + 1;
        }
    }

    internal static void ValidatePercentageCurve(ushort[] values, string name)
    {
        if (values.Length == 1 && values[0] > 100)
        {
            throw new IccProfileWriteException($"A single-entry {name} curve is a percentage and shall not exceed 100.");
        }
    }

    internal static ushort[] ReadUInt16Values(System.ReadOnlyMemory<byte> payload, int offset, int count)
    {
        ushort[] values = new ushort[count];
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, count * 2), offset);
        int index = 0;
        while (index < values.Length)
        {
            values[index] = reader.ReadUInt16();
            index = index + 1;
        }

        return values;
    }

    internal static void WriteUInt16Values(BigEndianWriter writer, ushort[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            writer.WriteUInt16(values[index]);
            index = index + 1;
        }
    }

    internal static void WriteCountedAscii(BigEndianWriter writer, string text, bool allowsAbsent)
    {
        if (allowsAbsent && text.Length == 0)
        {
            writer.WriteUInt32(0);
            return;
        }

        writer.WriteUInt32((uint)(text.Length + 1));
        WriteAscii(writer, text);
        writer.WriteUInt8(0);
    }

    internal static void WriteAscii(BigEndianWriter writer, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            writer.WriteUInt8((byte)text[index]);
            index = index + 1;
        }
    }

    internal static string DecodeAscii(System.ReadOnlyMemory<byte> bytes)
    {
        char[] characters = new char[bytes.Length];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)bytes.Span[index];
            index = index + 1;
        }

        return new string(characters);
    }

    internal static bool IsRegisteredPlatform(uint signature)
    {
        return signature == IccConstants.MacintoshPlatform
            || signature == IccConstants.MicrosoftPlatform
            || signature == IccConstants.SiliconGraphicsPlatform
            || signature == IccConstants.SunPlatform
            || signature == IccConstants.TaligentPlatform;
    }

    internal static uint ReadUInt32(System.ReadOnlyMemory<byte> payload, int offset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 4), offset);
        return reader.ReadUInt32();
    }

    internal static void CompleteInvalid(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, message);
    }
}
