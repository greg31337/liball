namespace icc;

internal static class Icc2TextTagCodec
{
    internal static void DecodeText(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The ICC.2 Unicode text tag reserved bytes shall be zero.");
            return;
        }

        System.ReadOnlyMemory<byte> encoded = payload.Slice(8);
        if (typeSignature == IccTypeSignatures.Utf8)
        {
            DecodeUtf8(state, payload, typeSignature, encoded, false);
            return;
        }

        if (typeSignature == IccTypeSignatures.Utf16)
        {
            DecodeUtf16(state, payload, typeSignature, encoded);
            return;
        }

        ZlibDecodeResult decompressed = ZlibCodec.Decode(encoded, state.MaximumDecompressedBytes);
        if (!decompressed.Success)
        {
            CompleteDecompressionFailure(state, payload, typeSignature, decompressed);
            return;
        }

        System.ReadOnlyMemory<byte> content = decompressed.Bytes;
        if (typeSignature == IccTypeSignatures.ZippedUtf8)
        {
            DecodeUtf8(state, payload, typeSignature, content, true);
            return;
        }

        DecodeXml(state, payload, typeSignature, content);
    }

    internal static byte[] SerializeIcc2Text(IccTextData value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("ICC.2 text data cannot be null.");
        }

        if (value.Text == null)
        {
            throw new IccProfileWriteException("ICC.2 decoded text cannot be null.");
        }

        uint typeSignature = value.TypeSignature;
        byte[] content;
        if (typeSignature == IccTypeSignatures.Utf16)
        {
            content = LocalizedUnicodeTagCodec.EncodeUtf16BigEndian(value.Text);
        }
        else if (typeSignature == IccTypeSignatures.Utf8 || typeSignature == IccTypeSignatures.ZippedUtf8 || IsZippedXml(typeSignature))
        {
            if (!IsValidUnicodeString(value.Text))
            {
                throw new IccProfileWriteException("ICC.2 text contains an invalid UTF-16 surrogate sequence.");
            }

            content = System.Text.Encoding.UTF8.GetBytes(value.Text);
            if (IsZippedXml(typeSignature) && (!IsValidXmlText(value.Text) || !XmlDeclarationMatchesEncoding(value.Text, 1, content)))
            {
                throw new IccProfileWriteException("Zipped XML text contains characters or an encoding declaration that cannot be encoded as canonical UTF-8 XML.");
            }
        }
        else
        {
            throw new IccProfileWriteException($"Type signature 0x{typeSignature:X8} is not an ICC.2 Unicode text type.");
        }

        if (typeSignature == IccTypeSignatures.ZippedUtf8 || IsZippedXml(typeSignature))
        {
            content = ZlibCodec.EncodeStored(content);
        }

        long payloadSize = 8L + content.Length;
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The ICC.2 text payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(typeSignature);
        writer.WriteUInt32(0);
        writer.WriteExactBytes(content);
        return payload;
    }

    internal static void DecodeUtf8(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, System.ReadOnlyMemory<byte> content, bool compressed)
    {
        int start = HasUtf8Bom(content) ? 3 : 0;
        if (!TextTagCodec.IsValidUtf8(content.Slice(start), content.Length - start))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The ICC.2 text contains an invalid UTF-8 byte sequence.");
            return;
        }

        string text = System.Text.Encoding.UTF8.GetString(content.Span.Slice(start));
        CompleteTextDecode(state, payload, typeSignature, content, text, compressed, false);
    }

    internal static void DecodeUtf16(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, System.ReadOnlyMemory<byte> content)
    {
        if ((content.Length & 1) != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The utf16Type content shall contain a whole number of big-endian UTF-16 code units.");
            return;
        }

        if (content.Length >= 2 && content.Span[0] == 255 && content.Span[1] == 254)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The utf16Type content uses a little-endian byte-order mark even though uInt16Number encoding is big-endian.");
            return;
        }

        int start = content.Length >= 2 && content.Span[0] == 254 && content.Span[1] == 255 ? 2 : 0;
        System.ReadOnlyMemory<byte> textBytes = content.Slice(start);
        if (!LocalizedUnicodeTagCodec.IsValidUtf16BigEndian(textBytes))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The utf16Type content contains an invalid UTF-16 surrogate sequence.");
            return;
        }

        string text = LocalizedUnicodeTagCodec.DecodeUtf16BigEndian(textBytes);
        CompleteTextDecode(state, payload, typeSignature, content, text, false, false);
    }

    internal static void DecodeXml(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, System.ReadOnlyMemory<byte> content)
    {
        int encodingKind = DetectXmlEncoding(content);
        string text;
        if (encodingKind == 1)
        {
            int start = HasUtf8Bom(content) ? 3 : 0;
            if (!TextTagCodec.IsValidUtf8(content.Slice(start), content.Length - start))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The zipped XML content contains an invalid UTF-8 byte sequence.");
                return;
            }

            text = System.Text.Encoding.UTF8.GetString(content.Span.Slice(start));
        }
        else if (encodingKind == 2)
        {
            int start = HasUtf16BigEndianBom(content) ? 2 : 0;
            System.ReadOnlyMemory<byte> textBytes = content.Slice(start);
            if ((textBytes.Length & 1) != 0 || !LocalizedUnicodeTagCodec.IsValidUtf16BigEndian(textBytes))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The zipped XML content contains invalid big-endian UTF-16.");
                return;
            }

            text = LocalizedUnicodeTagCodec.DecodeUtf16BigEndian(textBytes);
        }
        else if (encodingKind == 3)
        {
            int start = HasUtf16LittleEndianBom(content) ? 2 : 0;
            System.ReadOnlyMemory<byte> textBytes = content.Slice(start);
            if ((textBytes.Length & 1) != 0 || !IsValidUtf16LittleEndian(textBytes))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The zipped XML content contains invalid little-endian UTF-16.");
                return;
            }

            text = DecodeUtf16LittleEndian(textBytes);
        }
        else
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The zipped XML byte encoding is not a supported XML UTF-8 or UTF-16 encoding.");
            return;
        }

        if (!IsValidXmlText(text) || !XmlDeclarationMatchesEncoding(text, encodingKind, content))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The zipped XML content has invalid XML characters or an inconsistent encoding declaration.");
            return;
        }

        CompleteTextDecode(state, payload, typeSignature, content, text, true, true);
    }

    internal static void CompleteTextDecode(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, System.ReadOnlyMemory<byte> content, string textValue, bool compressed, bool xml)
    {
        IccTextData text = new IccTextData();
        text.TypeSignature = typeSignature;
        text.Text = textValue;
        text.Bytes = content.ToArray();
        text.IsCompressed = compressed;
        text.IsXml = xml;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Text);
        data.Text = text;
        RegisteredArrayProjectionCodec.ProjectColorEncodingName(state, data);
        string kind = xml ? "zipped XML" : compressed ? "zipped UTF-8" : typeSignature == IccTypeSignatures.Utf16 ? "UTF-16" : "UTF-8";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded ICC.2 {kind} text.");
    }

    internal static void CompleteDecompressionFailure(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, ZlibDecodeResult decompressed)
    {
        if (!decompressed.LimitExceeded)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, decompressed.Error);
            return;
        }

        IccTagData data = new IccTagData();
        if (state.PreserveOriginalBytes)
        {
            UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        }

        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.ResourceLimitExceeded);
        result.Message = decompressed.Error;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static int DetectXmlEncoding(System.ReadOnlyMemory<byte> content)
    {
        if (HasUtf8Bom(content))
        {
            return 1;
        }

        if (HasUtf16BigEndianBom(content))
        {
            return 2;
        }

        if (HasUtf16LittleEndianBom(content))
        {
            return 3;
        }

        if (content.Length >= 4 && content.Span[0] == 0 && content.Span[1] == 60 && content.Span[2] == 0 && content.Span[3] != 0)
        {
            return 2;
        }

        if (content.Length >= 4 && content.Span[0] == 60 && content.Span[1] == 0 && content.Span[2] != 0 && content.Span[3] == 0)
        {
            return 3;
        }

        return 1;
    }

    internal static bool XmlDeclarationMatchesEncoding(string text, int encodingKind, System.ReadOnlyMemory<byte> content)
    {
        if (!text.StartsWith("<?xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        int declarationEnd = text.IndexOf("?>", System.StringComparison.Ordinal);
        if (declarationEnd < 0)
        {
            return false;
        }

        string declaration = text.Substring(0, declarationEnd + 2);
        string lower = declaration.ToLowerInvariant();
        int encodingIndex = lower.IndexOf("encoding", System.StringComparison.Ordinal);
        if (encodingIndex < 0)
        {
            return true;
        }

        int equalsIndex = lower.IndexOf('=', encodingIndex);
        if (equalsIndex < 0)
        {
            return false;
        }

        int quoteIndex = equalsIndex + 1;
        while (quoteIndex < lower.Length && (lower[quoteIndex] == ' ' || lower[quoteIndex] == '\t' || lower[quoteIndex] == '\r' || lower[quoteIndex] == '\n'))
        {
            quoteIndex = quoteIndex + 1;
        }

        if (quoteIndex >= lower.Length || (lower[quoteIndex] != '\'' && lower[quoteIndex] != '"'))
        {
            return false;
        }

        char quote = lower[quoteIndex];
        int valueEnd = lower.IndexOf(quote, quoteIndex + 1);
        if (valueEnd < 0)
        {
            return false;
        }

        string encoding = lower.Substring(quoteIndex + 1, valueEnd - quoteIndex - 1);
        if (encoding == "utf-8")
        {
            return encodingKind == 1;
        }

        if (encoding == "utf-16")
        {
            return (encodingKind == 2 && HasUtf16BigEndianBom(content)) || (encodingKind == 3 && HasUtf16LittleEndianBom(content));
        }

        if (encoding == "utf-16be")
        {
            return encodingKind == 2;
        }

        if (encoding == "utf-16le")
        {
            return encodingKind == 3;
        }

        return false;
    }

    internal static bool IsValidXmlText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int firstContent = 0;
        while (firstContent < text.Length && (text[firstContent] == ' ' || text[firstContent] == '\t' || text[firstContent] == '\r' || text[firstContent] == '\n'))
        {
            firstContent = firstContent + 1;
        }

        if (firstContent >= text.Length || text[firstContent] != '<')
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            int codePoint = text[index];
            if (codePoint >= 55296 && codePoint <= 56319)
            {
                if (index + 1 >= text.Length || text[index + 1] < 56320 || text[index + 1] > 57343)
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if ((codePoint >= 56320 && codePoint <= 57343)
                || (codePoint < 32 && codePoint != 9 && codePoint != 10 && codePoint != 13)
                || codePoint == 65534 || codePoint == 65535)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidUnicodeString(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char codeUnit = text[index];
            if (codeUnit >= 55296 && codeUnit <= 56319)
            {
                if (index + 1 >= text.Length || text[index + 1] < 56320 || text[index + 1] > 57343)
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (codeUnit >= 56320 && codeUnit <= 57343)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidUtf16LittleEndian(System.ReadOnlyMemory<byte> bytes)
    {
        int offset = 0;
        while (offset < bytes.Length)
        {
            int codeUnit = bytes.Span[offset] | (bytes.Span[offset + 1] << 8);
            if (codeUnit >= 55296 && codeUnit <= 56319)
            {
                if (offset + 3 >= bytes.Length)
                {
                    return false;
                }

                int low = bytes.Span[offset + 2] | (bytes.Span[offset + 3] << 8);
                if (low < 56320 || low > 57343)
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

    internal static string DecodeUtf16LittleEndian(System.ReadOnlyMemory<byte> bytes)
    {
        char[] characters = new char[bytes.Length / 2];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)(bytes.Span[index * 2] | (bytes.Span[(index * 2) + 1] << 8));
            index = index + 1;
        }

        return new string(characters);
    }

    internal static bool HasUtf8Bom(System.ReadOnlyMemory<byte> content)
    {
        return content.Length >= 3 && content.Span[0] == 239 && content.Span[1] == 187 && content.Span[2] == 191;
    }

    internal static bool HasUtf16BigEndianBom(System.ReadOnlyMemory<byte> content)
    {
        return content.Length >= 2 && content.Span[0] == 254 && content.Span[1] == 255;
    }

    internal static bool HasUtf16LittleEndianBom(System.ReadOnlyMemory<byte> content)
    {
        return content.Length >= 2 && content.Span[0] == 255 && content.Span[1] == 254;
    }

    internal static bool IsZippedXml(uint typeSignature)
    {
        return typeSignature == IccTypeSignatures.ZippedXml || typeSignature == IccTypeSignatures.ZippedXmlUppercase;
    }
}
