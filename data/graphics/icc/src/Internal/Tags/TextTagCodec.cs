namespace icc;

internal static class TextTagCodec
{
    internal static void DecodeText(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The text tag reserved bytes shall be zero.");
            return;
        }

        System.ReadOnlyMemory<byte> textBytes = payload.Slice(8);
        int textLength = FindTextTerminator(textBytes);
        if (textLength < 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The 7-bit ASCII text tag shall end with a NULL byte.");
            return;
        }

        if (!HasZeroTextPadding(textBytes, textLength + 1))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The 7-bit ASCII text tag contains a nonzero byte after its NULL terminator.");
            return;
        }

        if (!TagCodecSupport.IsSevenBitAscii(textBytes.Slice(0, textLength)))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The text tag contains a byte outside the 7-bit ASCII range.");
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Text);
        data.Text = CreateTextData(typeSignature, textBytes, textLength);
        string generationName = TagCodecDispatcher.DescribeProfileGeneration(state.ProfileVersion);
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {generationName} 7-bit ASCII text.");
    }

    internal static void DecodeData(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A data tag requires a 12-byte header, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The data tag reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint dataFlag = reader.ReadUInt32();
        System.ReadOnlyMemory<byte> dataBytes = payload.Slice(12);
        if (dataFlag == 0)
        {
            if (state.ProfileVersion == IccVersionKind.Version5)
            {
                DecodeUtf8Data(state, payload, typeSignature, dataBytes);
                return;
            }

            DecodeAsciiData(state, payload, typeSignature, dataBytes);
            return;
        }

        if (dataFlag == 1)
        {
            IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Binary);
            data.BinaryData = dataBytes.ToArray();
            TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {dataBytes.Length} bytes of binary data.");
            return;
        }

        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The data tag flag value {dataFlag} is reserved and unsupported.");
    }

    internal static void DecodeAsciiData(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, System.ReadOnlyMemory<byte> dataBytes)
    {
        if (dataBytes.Length == 0 || dataBytes.Span[dataBytes.Length - 1] != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "ASCII data shall end with a NULL byte.");
            return;
        }

        if (!TagCodecSupport.IsSevenBitAscii(dataBytes))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "ASCII data contains a byte outside the 7-bit ASCII range.");
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Text);
        data.Text = CreateTextData(typeSignature, dataBytes, dataBytes.Length - 1);
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded 7-bit ASCII data.");
    }

    internal static void DecodeUtf8Data(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, System.ReadOnlyMemory<byte> dataBytes)
    {
        if (dataBytes.Length == 0 || dataBytes.Span[dataBytes.Length - 1] != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "ICC.2 UTF-8 data shall end with a NULL byte.");
            return;
        }

        if (!IsValidUtf8(dataBytes, dataBytes.Length - 1))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "ICC.2 data contains an invalid UTF-8 byte sequence.");
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Text);
        IccTextData text = new IccTextData();
        text.TypeSignature = typeSignature;
        text.Text = System.Text.Encoding.UTF8.GetString(dataBytes.Span.Slice(0, dataBytes.Length - 1));
        text.Bytes = dataBytes.ToArray();
        data.Text = text;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded ICC.2 UTF-8 data.");
    }

    internal static bool IsValidUtf8(System.ReadOnlyMemory<byte> bytes, int count)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        int index = 0;
        while (index < count)
        {
            int first = values[index];
            if (first <= 127)
            {
                index = index + 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (index + 1 >= count || !IsUtf8Continuation(values[index + 1]))
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (index + 2 >= count || !IsUtf8Continuation(values[index + 1]) || !IsUtf8Continuation(values[index + 2]))
                {
                    return false;
                }

                int second = values[index + 1];
                if ((first == 224 && second < 160) || (first == 237 && second > 159))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (index + 3 >= count || !IsUtf8Continuation(values[index + 1]) || !IsUtf8Continuation(values[index + 2]) || !IsUtf8Continuation(values[index + 3]))
                {
                    return false;
                }

                int second = values[index + 1];
                if ((first == 240 && second < 144) || (first == 244 && second > 143))
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            return false;
        }

        return true;
    }

    internal static bool IsUtf8Continuation(byte value)
    {
        return value >= 128 && value <= 191;
    }

    internal static int FindTextTerminator(System.ReadOnlyMemory<byte> bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes.Span[index] == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool HasZeroTextPadding(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        int index = offset;
        while (index < bytes.Length)
        {
            if (bytes.Span[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static IccTextData CreateTextData(uint typeSignature, System.ReadOnlyMemory<byte> bytes, int textLength)
    {
        IccTextData text = new IccTextData();
        text.TypeSignature = typeSignature;
        text.Text = TagCodecSupport.DecodeSevenBitAscii(bytes, textLength);
        text.Bytes = bytes.ToArray();
        return text;
    }
}
