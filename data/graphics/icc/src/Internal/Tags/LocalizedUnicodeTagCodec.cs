namespace icc;

internal static class LocalizedUnicodeTagCodec
{
    internal static void DecodeMultiLocalizedUnicode(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 16)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A multiLocalizedUnicodeType payload requires at least 16 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The multiLocalizedUnicodeType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 8), state.AbsolutePayloadOffset + 8);
        uint declaredRecordCount = headerReader.ReadUInt32();
        uint recordSize = headerReader.ReadUInt32();
        if (recordSize != 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A multiLocalizedUnicodeType record size shall be 12 bytes, but this payload declares {recordSize} bytes.");
            return;
        }

        int maximumRecordCount = (payload.Length - 16) / 12;
        if (declaredRecordCount > (uint)maximumRecordCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The multiLocalizedUnicodeType declares {declaredRecordCount} records, but its record table can contain at most {maximumRecordCount}.");
            return;
        }

        int recordCount = (int)declaredRecordCount;
        int stringStorageOffset = 16 + (recordCount * 12);
        IccLocalizedString[] localizedStrings = new IccLocalizedString[recordCount];
        System.Collections.Generic.Dictionary<ulong, byte[]> sharedBytes = new System.Collections.Generic.Dictionary<ulong, byte[]>();
        System.Collections.Generic.Dictionary<uint, bool> seenLocales = new System.Collections.Generic.Dictionary<uint, bool>();
        int duplicateLocaleCount = 0;
        int recordIndex = 0;
        while (recordIndex < recordCount)
        {
            int recordOffset = 16 + (recordIndex * 12);
            BigEndianReader recordReader = new BigEndianReader(payload.Slice(recordOffset, 12), state.AbsolutePayloadOffset + recordOffset);
            ushort languageCode = recordReader.ReadUInt16();
            ushort countryCode = recordReader.ReadUInt16();
            uint declaredStringLength = recordReader.ReadUInt32();
            uint declaredStringOffset = recordReader.ReadUInt32();
            if ((declaredStringLength & 1) != 0)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Localized string record {recordIndex} has an odd UTF-16BE byte length of {declaredStringLength}.");
                return;
            }

            if (declaredStringLength > int.MaxValue || declaredStringOffset > int.MaxValue)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Localized string record {recordIndex} exceeds the supported managed offset or length range.");
                return;
            }

            int stringLength = (int)declaredStringLength;
            int stringOffset = (int)declaredStringOffset;
            if (stringOffset < stringStorageOffset || (stringOffset & 1) != 0 || stringOffset > payload.Length || stringLength > payload.Length - stringOffset)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Localized string record {recordIndex} references UTF-16BE bytes outside the aligned string-storage area.");
                return;
            }

            System.ReadOnlyMemory<byte> encodedString = payload.Slice(stringOffset, stringLength);
            if (!IsValidUtf16BigEndian(encodedString))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Localized string record {recordIndex} contains an invalid UTF-16BE surrogate sequence.");
                return;
            }

            ulong storageKey = ((ulong)declaredStringOffset << 32) | declaredStringLength;
            byte[] rawBytes;
            if (sharedBytes.ContainsKey(storageKey))
            {
                rawBytes = sharedBytes[storageKey];
            }
            else
            {
                rawBytes = encodedString.ToArray();
                sharedBytes.Add(storageKey, rawBytes);
            }

            IccLocalizedString localized = new IccLocalizedString();
            localized.Language = DecodeLocaleCode(languageCode);
            localized.Country = DecodeLocaleCode(countryCode);
            localized.Text = DecodeUtf16BigEndian(encodedString);
            localized.RawBytes = rawBytes;
            localizedStrings[recordIndex] = localized;

            uint localeKey = ((uint)languageCode << 16) | countryCode;
            if (seenLocales.ContainsKey(localeKey))
            {
                duplicateLocaleCount = duplicateLocaleCount + 1;
            }
            else
            {
                seenLocales.Add(localeKey, true);
            }

            recordIndex = recordIndex + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.LocalizedText);
        data.LocalizedStrings = localizedStrings;
        string message = $"Decoded {recordCount} localized Unicode records in stable record order.";
        if (duplicateLocaleCount > 0)
        {
            string recordWord = duplicateLocaleCount == 1 ? "record" : "records";
            message = $"Decoded {recordCount} localized Unicode records in stable record order and found {duplicateLocaleCount} duplicate locale {recordWord}.";
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static IccLocalizedString? FindLocalizedString(IccLocalizedString[] localizedStrings, string language, string country)
    {
        if (localizedStrings == null)
        {
            throw new IccProfileValidationException("The localized-string collection cannot be null.");
        }

        if (language == null || country == null)
        {
            throw new IccProfileValidationException("The requested language and country cannot be null.");
        }

        IccLocalizedString? languageFallback = null;
        int index = 0;
        while (index < localizedStrings.Length)
        {
            IccLocalizedString localized = localizedStrings[index];
            if (localized == null || localized.Language == null || localized.Country == null)
            {
                throw new IccProfileValidationException($"Localized string record {index} and its locale fields cannot be null.");
            }

            if (System.String.Equals(localized.Language, language, System.StringComparison.OrdinalIgnoreCase))
            {
                if (System.String.Equals(localized.Country, country, System.StringComparison.OrdinalIgnoreCase))
                {
                    return localized;
                }

                if (languageFallback == null)
                {
                    languageFallback = localized;
                }
            }

            index = index + 1;
        }

        if (languageFallback != null)
        {
            return languageFallback;
        }

        if (localizedStrings.Length > 0)
        {
            return localizedStrings[0];
        }

        return null;
    }

    internal static byte[] SerializeMultiLocalizedUnicode(IccLocalizedString[] localizedStrings)
    {
        return SerializeMultiLocalizedUnicodeForMode(localizedStrings, IccWriteMode.Preservation);
    }

    internal static byte[] SerializeMultiLocalizedUnicodeForMode(IccLocalizedString[] localizedStrings, IccWriteMode mode)
    {
        if (localizedStrings == null)
        {
            throw new IccProfileWriteException("The localized-string collection cannot be null.");
        }

        IccLocalizedString[] orderedStrings = CreateSerializationOrder(localizedStrings, mode);
        long tableSize = 16L + ((long)orderedStrings.Length * 12L);
        if (tableSize > int.MaxValue)
        {
            throw new IccProfileWriteException("The localized-string record table exceeds the supported managed size.");
        }

        byte[][] encodedStrings = new byte[orderedStrings.Length][];
        ushort[] languageCodes = new ushort[orderedStrings.Length];
        ushort[] countryCodes = new ushort[orderedStrings.Length];
        int[] sharedRecordIndices = new int[orderedStrings.Length];
        int[] stringOffsets = new int[orderedStrings.Length];
        System.Collections.Generic.Dictionary<string, int> sharedTextIndices = new System.Collections.Generic.Dictionary<string, int>();
        long payloadSize = tableSize;
        int index = 0;
        while (index < orderedStrings.Length)
        {
            IccLocalizedString localized = orderedStrings[index];
            if (localized == null || localized.Language == null || localized.Country == null || localized.Text == null || localized.RawBytes == null)
            {
                throw new IccProfileWriteException($"Localized string record {index} and its fields cannot be null.");
            }

            languageCodes[index] = EncodeLanguageCode(localized.Language);
            countryCodes[index] = EncodeCountryCode(localized.Country);
            string normalizedText = DeterministicTagSerialization.NormalizeUnicode(localized.Text, mode, "Localized text");
            encodedStrings[index] = EncodeUtf16BigEndian(normalizedText);
            int sharedIndex = index;
            if (sharedTextIndices.ContainsKey(normalizedText))
            {
                sharedIndex = sharedTextIndices[normalizedText];
            }
            else
            {
                sharedTextIndices.Add(normalizedText, index);
            }

            sharedRecordIndices[index] = sharedIndex;
            if (sharedIndex == index)
            {
                payloadSize = payloadSize + encodedStrings[index].Length;
                if (payloadSize > int.MaxValue)
                {
                    throw new IccProfileWriteException("The localized-string payload exceeds the supported managed size.");
                }
            }

            index = index + 1;
        }

        int nextStringOffset = (int)tableSize;
        index = 0;
        while (index < orderedStrings.Length)
        {
            int sharedIndex = sharedRecordIndices[index];
            if (sharedIndex == index)
            {
                stringOffsets[index] = nextStringOffset;
                nextStringOffset = nextStringOffset + encodedStrings[index].Length;
            }
            else
            {
                stringOffsets[index] = stringOffsets[sharedIndex];
            }

            index = index + 1;
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.MultiLocalizedUnicode);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)orderedStrings.Length);
        writer.WriteUInt32(12);
        index = 0;
        while (index < orderedStrings.Length)
        {
            writer.WriteUInt16(languageCodes[index]);
            writer.WriteUInt16(countryCodes[index]);
            writer.WriteUInt32((uint)encodedStrings[index].Length);
            writer.WriteUInt32((uint)stringOffsets[index]);
            index = index + 1;
        }

        index = 0;
        while (index < orderedStrings.Length)
        {
            if (sharedRecordIndices[index] == index)
            {
                writer.WriteExactBytes(encodedStrings[index]);
            }

            index = index + 1;
        }

        return payload;
    }

    internal static IccLocalizedString[] CreateSerializationOrder(IccLocalizedString[] localizedStrings, IccWriteMode mode)
    {
        IccLocalizedString[] ordered = new IccLocalizedString[localizedStrings.Length];
        int index = 0;
        while (index < localizedStrings.Length)
        {
            IccLocalizedString value = localizedStrings[index];
            ordered[index] = value;
            if (mode == IccWriteMode.Canonical && value != null)
            {
                int insertion = index;
                while (insertion > 0 && ordered[insertion - 1] != null && CompareLocalizedStrings(ordered[insertion - 1], value) > 0)
                {
                    ordered[insertion] = ordered[insertion - 1];
                    insertion = insertion - 1;
                }

                ordered[insertion] = value;
            }

            index = index + 1;
        }

        return ordered;
    }

    internal static int CompareLocalizedStrings(IccLocalizedString first, IccLocalizedString second)
    {
        int language = System.String.CompareOrdinal(first.Language, second.Language);
        if (language != 0)
        {
            return language;
        }

        int country = System.String.CompareOrdinal(first.Country, second.Country);
        if (country != 0)
        {
            return country;
        }

        string firstText = DeterministicTagSerialization.NormalizeUnicode(first.Text, IccWriteMode.Canonical, "Localized text");
        string secondText = DeterministicTagSerialization.NormalizeUnicode(second.Text, IccWriteMode.Canonical, "Localized text");
        return System.String.CompareOrdinal(firstText, secondText);
    }

    internal static bool IsValidUtf16BigEndian(System.ReadOnlyMemory<byte> bytes)
    {
        if ((bytes.Length & 1) != 0)
        {
            return false;
        }

        int offset = 0;
        while (offset < bytes.Length)
        {
            ushort codeUnit = ReadUInt16At(bytes, offset);
            if (codeUnit >= 55296 && codeUnit <= 56319)
            {
                if (offset + 2 >= bytes.Length)
                {
                    return false;
                }

                ushort lowSurrogate = ReadUInt16At(bytes, offset + 2);
                if (lowSurrogate < 56320 || lowSurrogate > 57343)
                {
                    return false;
                }

                offset = offset + 4;
                continue;
            }

            if (codeUnit >= 56320 && codeUnit <= 57343)
            {
                return false;
            }

            offset = offset + 2;
        }

        return true;
    }

    internal static string DecodeUtf16BigEndian(System.ReadOnlyMemory<byte> bytes)
    {
        char[] characters = new char[bytes.Length / 2];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)ReadUInt16At(bytes, index * 2);
            index = index + 1;
        }

        return new string(characters);
    }

    internal static byte[] EncodeUtf16BigEndian(string text)
    {
        byte[] bytes = new byte[text.Length * 2];
        int index = 0;
        while (index < text.Length)
        {
            char codeUnit = text[index];
            if (codeUnit >= 55296 && codeUnit <= 56319)
            {
                if (index + 1 >= text.Length || text[index + 1] < 56320 || text[index + 1] > 57343)
                {
                    throw new IccProfileWriteException("A localized string contains an invalid UTF-16 surrogate sequence.");
                }
            }
            else if (codeUnit >= 56320 && codeUnit <= 57343)
            {
                if (index == 0 || text[index - 1] < 55296 || text[index - 1] > 56319)
                {
                    throw new IccProfileWriteException("A localized string contains an invalid UTF-16 surrogate sequence.");
                }
            }

            bytes[index * 2] = (byte)((codeUnit >> 8) & 255);
            bytes[(index * 2) + 1] = (byte)(codeUnit & 255);
            index = index + 1;
        }

        return bytes;
    }

    internal static ushort ReadUInt16At(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        return (ushort)((values[offset] << 8) | values[offset + 1]);
    }

    internal static string DecodeLocaleCode(ushort code)
    {
        char[] characters = new char[2];
        characters[0] = (char)(code >> 8);
        characters[1] = (char)(code & 255);
        return new string(characters);
    }

    internal static ushort EncodeLanguageCode(string code)
    {
        if (code.Length != 2 || code[0] < 'a' || code[0] > 'z' || code[1] < 'a' || code[1] > 'z')
        {
            throw new IccProfileWriteException("A localized language code must contain exactly two lowercase ASCII letters.");
        }

        return (ushort)((code[0] << 8) | code[1]);
    }

    internal static ushort EncodeCountryCode(string code)
    {
        if (code.Length != 2 || code[0] < 'A' || code[0] > 'Z' || code[1] < 'A' || code[1] > 'Z')
        {
            throw new IccProfileWriteException("A localized country code must contain exactly two uppercase ASCII letters.");
        }

        return (ushort)((code[0] << 8) | code[1]);
    }

}
