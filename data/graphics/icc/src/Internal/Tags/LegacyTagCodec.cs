namespace icc;

internal static class LegacyTagCodec
{
    internal static void DecodeTextDescription(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 90)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A textDescriptionType payload requires at least 90 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The textDescriptionType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader countReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint declaredAsciiCount = countReader.ReadUInt32();
        int maximumAsciiCount = payload.Length - 90;
        int asciiCount;
        bool toleratedInconsistency = false;
        if (declaredAsciiCount > (uint)maximumAsciiCount)
        {
            asciiCount = maximumAsciiCount;
            toleratedInconsistency = true;
        }
        else
        {
            asciiCount = (int)declaredAsciiCount;
        }

        int resolvedAsciiCount = ResolveAsciiCount(payload, asciiCount, maximumAsciiCount);
        if (resolvedAsciiCount != asciiCount)
        {
            asciiCount = resolvedAsciiCount;
            toleratedInconsistency = true;
        }

        int unicodeHeaderOffset = 12 + asciiCount;
        int scriptOffset = payload.Length - 70;
        if (unicodeHeaderOffset + 8 > scriptOffset)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The textDescriptionType length fields do not leave room for the Unicode and ScriptCode records.");
            return;
        }

        System.ReadOnlyMemory<byte> asciiBytes = payload.Slice(12, asciiCount);
        if (ContainsByteAboveAscii(asciiBytes) || FindByteTextLength(asciiBytes, asciiCount) == asciiCount)
        {
            toleratedInconsistency = true;
        }

        BigEndianReader unicodeHeaderReader = new BigEndianReader(payload.Slice(unicodeHeaderOffset, 8), state.AbsolutePayloadOffset + unicodeHeaderOffset);
        uint unicodeLanguageCode = unicodeHeaderReader.ReadUInt32();
        uint declaredUnicodeCount = unicodeHeaderReader.ReadUInt32();
        int unicodeDataOffset = unicodeHeaderOffset + 8;
        int unicodeByteCount = scriptOffset - unicodeDataOffset;
        if ((unicodeByteCount & 1) != 0)
        {
            unicodeByteCount = unicodeByteCount - 1;
            toleratedInconsistency = true;
        }

        int availableUnicodeCount = unicodeByteCount / 2;
        int unicodeCount;
        if (declaredUnicodeCount > (uint)availableUnicodeCount)
        {
            unicodeCount = availableUnicodeCount;
            toleratedInconsistency = true;
        }
        else
        {
            unicodeCount = (int)declaredUnicodeCount;
        }

        if (unicodeCount < availableUnicodeCount && unicodeCount > 0 && ReadUInt16At(payload, unicodeDataOffset + ((unicodeCount - 1) * 2)) != 0)
        {
            unicodeCount = FindUnicodeFieldCount(payload, unicodeDataOffset, availableUnicodeCount);
            toleratedInconsistency = true;
        }

        if (unicodeCount == 0 && availableUnicodeCount > 0)
        {
            unicodeCount = FindUnicodeFieldCount(payload, unicodeDataOffset, availableUnicodeCount);
            toleratedInconsistency = true;
        }

        if (declaredUnicodeCount != (uint)availableUnicodeCount)
        {
            toleratedInconsistency = true;
        }

        int unicodeTextLength = FindUnicodeTextLength(payload, unicodeDataOffset, unicodeCount);
        if (unicodeCount > 0 && unicodeTextLength == unicodeCount)
        {
            toleratedInconsistency = true;
        }

        BigEndianReader scriptReader = new BigEndianReader(payload.Slice(scriptOffset, 3), state.AbsolutePayloadOffset + scriptOffset);
        ushort scriptCode = scriptReader.ReadUInt16();
        int declaredScriptCount = scriptReader.ReadUInt8();
        int scriptCount = declaredScriptCount;
        if (scriptCount > 67)
        {
            scriptCount = 67;
            toleratedInconsistency = true;
        }

        System.ReadOnlyMemory<byte> scriptBytes = payload.Slice(scriptOffset + 3, 67);
        int scriptTextLength = FindByteTextLength(scriptBytes, scriptCount);
        if (scriptCount > 0 && scriptTextLength == scriptCount)
        {
            toleratedInconsistency = true;
        }

        IccTextData text = new IccTextData();
        text.TypeSignature = typeSignature;
        text.AsciiText = DecodeLegacyByteText(asciiBytes, FindByteTextLength(asciiBytes, asciiCount), true);
        text.UnicodeText = DecodeUnicodeText(payload, unicodeDataOffset, unicodeTextLength);
        text.UnicodeLanguageCode = unicodeLanguageCode;
        text.ScriptCodeText = DecodeLegacyByteText(scriptBytes, scriptTextLength, false);
        text.ScriptCode = scriptCode;
        if (text.UnicodeText.Length > 0)
        {
            text.Text = text.UnicodeText;
        }
        else if (text.AsciiText.Length > 0)
        {
            text.Text = text.AsciiText;
        }
        else
        {
            text.Text = text.ScriptCodeText;
        }

        text.Bytes = payload.ToArray();
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Text);
        data.Text = text;
        string message = "Decoded an ICC.1 version 2 textDescriptionType with ASCII, Unicode, and ScriptCode fields.";
        if (toleratedInconsistency)
        {
            message = "Decoded an ICC.1 version 2 textDescriptionType while tolerating inconsistent legacy length or terminator fields.";
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static byte[] SerializeTextDescription(IccTextData text)
    {
        if (text == null)
        {
            throw new IccProfileWriteException("Text-description data cannot be null.");
        }

        if (CanReuseTextDescriptionBytes(text))
        {
            byte[] clone = new byte[text.Bytes.Length];
            System.Array.Copy(text.Bytes, clone, text.Bytes.Length);
            return clone;
        }

        if (text.Text == null || text.AsciiText == null || text.UnicodeText == null || text.ScriptCodeText == null || text.Bytes == null)
        {
            throw new IccProfileWriteException("Text-description strings and encoded bytes cannot be null.");
        }

        string asciiText = SelectAsciiText(text);
        ValidateAsciiText(asciiText, "ASCII");
        ValidateScriptText(text.ScriptCodeText);
        if (text.ScriptCode < 0 || text.ScriptCode > 65535)
        {
            throw new IccProfileWriteException("A text-description ScriptCode identifier must fit in an unsigned 16-bit integer.");
        }

        int asciiCount = asciiText.Length + 1;
        int unicodeCount = 0;
        if (text.UnicodeText.Length > 0)
        {
            unicodeCount = text.UnicodeText.Length + 1;
        }

        int scriptCount = 0;
        if (text.ScriptCodeText.Length > 0)
        {
            scriptCount = text.ScriptCodeText.Length + 1;
        }

        long size = 90L + asciiCount + ((long)unicodeCount * 2L);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException("The text-description encoding exceeds the supported managed size.");
        }

        byte[] payload = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.TextDescription);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)asciiCount);
        WriteAsciiText(writer, asciiText);
        writer.WriteUInt8(0);
        writer.WriteUInt32(text.UnicodeLanguageCode);
        writer.WriteUInt32((uint)unicodeCount);
        WriteUnicodeText(writer, text.UnicodeText);
        if (unicodeCount > 0)
        {
            writer.WriteUInt16(0);
        }

        writer.WriteUInt16((ushort)text.ScriptCode);
        writer.WriteUInt8((byte)scriptCount);
        WriteScriptText(writer, text.ScriptCodeText);
        writer.WriteZeroBytes(67 - text.ScriptCodeText.Length);
        return payload;
    }

    internal static int ResolveAsciiCount(System.ReadOnlyMemory<byte> payload, int asciiCount, int maximumAsciiCount)
    {
        if (asciiCount == 0)
        {
            return 0;
        }

        int terminatedCount = FindFirstAsciiNullCount(payload, maximumAsciiCount);
        if (terminatedCount == 0 || terminatedCount == asciiCount)
        {
            return asciiCount;
        }

        if (!HasPlausibleUnicodeCount(payload, asciiCount))
        {
            return terminatedCount;
        }

        if (!HasEvenUnicodeArea(payload.Length, asciiCount) && HasEvenUnicodeArea(payload.Length, terminatedCount))
        {
            return terminatedCount;
        }

        return asciiCount;
    }

    internal static int FindFirstAsciiNullCount(System.ReadOnlyMemory<byte> payload, int maximumAsciiCount)
    {
        int index = 0;
        while (index < maximumAsciiCount)
        {
            if (payload.Span[12 + index] == 0)
            {
                return index + 1;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static bool HasEvenUnicodeArea(int payloadLength, int asciiCount)
    {
        int unicodeByteCount = payloadLength - 90 - asciiCount;
        return unicodeByteCount >= 0 && (unicodeByteCount & 1) == 0;
    }

    internal static bool HasPlausibleUnicodeCount(System.ReadOnlyMemory<byte> payload, int asciiCount)
    {
        int unicodeHeaderOffset = 12 + asciiCount;
        int unicodeByteCount = payload.Length - 70 - unicodeHeaderOffset - 8;
        if (unicodeByteCount < 0 || (unicodeByteCount & 1) != 0)
        {
            return false;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(unicodeHeaderOffset + 4, 4), unicodeHeaderOffset + 4);
        uint declaredCount = reader.ReadUInt32();
        return declaredCount <= (uint)(unicodeByteCount / 2);
    }

    internal static int FindUnicodeFieldCount(System.ReadOnlyMemory<byte> payload, int offset, int availableCount)
    {
        int index = 0;
        while (index < availableCount)
        {
            if (ReadUInt16At(payload, offset + (index * 2)) == 0)
            {
                return index + 1;
            }

            index = index + 1;
        }

        return availableCount;
    }

    internal static int FindUnicodeTextLength(System.ReadOnlyMemory<byte> payload, int offset, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (ReadUInt16At(payload, offset + (index * 2)) == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return count;
    }

    internal static ushort ReadUInt16At(System.ReadOnlyMemory<byte> payload, int offset)
    {
        System.ReadOnlySpan<byte> bytes = payload.Span;
        return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    internal static int FindByteTextLength(System.ReadOnlyMemory<byte> bytes, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (bytes.Span[index] == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return count;
    }

    internal static bool ContainsByteAboveAscii(System.ReadOnlyMemory<byte> bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes.Span[index] > 127)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string DecodeLegacyByteText(System.ReadOnlyMemory<byte> bytes, int count, bool replaceNonAscii)
    {
        char[] characters = new char[count];
        int index = 0;
        while (index < count)
        {
            byte value = bytes.Span[index];
            if (replaceNonAscii && value > 127)
            {
                characters[index] = '\ufffd';
            }
            else
            {
                characters[index] = (char)value;
            }

            index = index + 1;
        }

        return new string(characters);
    }

    internal static string DecodeUnicodeText(System.ReadOnlyMemory<byte> payload, int offset, int count)
    {
        char[] characters = new char[count];
        int index = 0;
        while (index < count)
        {
            characters[index] = (char)ReadUInt16At(payload, offset + (index * 2));
            index = index + 1;
        }

        return new string(characters);
    }

    internal static bool CanReuseTextDescriptionBytes(IccTextData text)
    {
        if (text.Bytes == null || text.Bytes.Length < 90)
        {
            return false;
        }

        System.ReadOnlyMemory<byte> bytes = new System.ReadOnlyMemory<byte>(text.Bytes);
        BigEndianReader reader = new BigEndianReader(bytes.Slice(0, 4), 0);
        if (reader.ReadUInt32() != IccTypeSignatures.TextDescription)
        {
            return false;
        }

        TagDecodeSource source = new TagDecodeSource(bytes);
        TagDecodeState state = new TagDecodeState(source, 0, bytes.Length, 0,
            IccVersionKind.Version2, false);
        DecodeTextDescription(state, bytes, IccTypeSignatures.TextDescription);
        IccTextData? decoded = state.Data.Text;
        if (state.Result.Status != IccOperationStatus.Success || decoded == null)
        {
            return false;
        }

        return decoded.Text == text.Text
            && decoded.AsciiText == text.AsciiText
            && decoded.UnicodeText == text.UnicodeText
            && decoded.UnicodeLanguageCode == text.UnicodeLanguageCode
            && decoded.ScriptCodeText == text.ScriptCodeText
            && decoded.ScriptCode == text.ScriptCode;
    }

    internal static string SelectAsciiText(IccTextData text)
    {
        if (text.AsciiText.Length > 0)
        {
            return text.AsciiText;
        }

        if (IsAsciiString(text.Text))
        {
            return text.Text;
        }

        return string.Empty;
    }

    internal static bool IsAsciiString(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127 || text[index] == 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateAsciiText(string text, string fieldName)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127 || text[index] == 0)
            {
                throw new IccProfileWriteException($"The text-description {fieldName} field must contain only non-NULL 7-bit ASCII characters.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateScriptText(string text)
    {
        if (text.Length > 66)
        {
            throw new IccProfileWriteException("The text-description ScriptCode field cannot exceed 66 bytes plus its terminating NULL.");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 255 || text[index] == 0)
            {
                throw new IccProfileWriteException("The text-description ScriptCode field must contain only non-NULL single-byte characters.");
            }

            index = index + 1;
        }
    }

    internal static void WriteAsciiText(BigEndianWriter writer, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            writer.WriteUInt8((byte)text[index]);
            index = index + 1;
        }
    }

    internal static void WriteUnicodeText(BigEndianWriter writer, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            writer.WriteUInt16(text[index]);
            index = index + 1;
        }
    }

    internal static void WriteScriptText(BigEndianWriter writer, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            writer.WriteUInt8((byte)text[index]);
            index = index + 1;
        }
    }
}
