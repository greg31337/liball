using System;

namespace raw;

internal static class ExifTextCodec
{
    internal static readonly System.Text.Encoding Jis = CreateJis();

    internal static RawTextValue DecodeAscii(byte[] bytes)
    {
        int length = TextLength(bytes, 1);
        bool terminated = length < bytes.Length;
        bool valid = true;
        int index = 0;
        while (index < length)
        {
            if (bytes[index] > 127)
            {
                valid = false;
            }
            index++;
        }
        byte[] textBytes = bytes.AsSpan(0, length).ToArray();
        return new RawTextValue(System.Text.Encoding.ASCII.GetString(textBytes), RawTextEncoding.Ascii, textBytes, null, terminated, valid && terminated);
    }

    internal static RawTextValue DecodeUtf8(byte[] bytes)
    {
        int length = TextLength(bytes, 1);
        bool terminated = length < bytes.Length;
        byte[] textBytes = bytes.AsSpan(0, length).ToArray();
        bool valid = terminated && !HasUtf8Bom(textBytes) && IsValidUtf8(textBytes);
        string text = System.Text.Encoding.UTF8.GetString(textBytes);
        return new RawTextValue(text, RawTextEncoding.Utf8, textBytes, null, terminated, valid);
    }

    internal static RawTextValue DecodePrefixed(byte[] bytes, RawByteOrder byteOrder)
    {
        if (bytes.Length < 8)
        {
            return new RawTextValue(string.Empty, RawTextEncoding.Unknown, bytes, bytes, false, false);
        }
        byte[] prefix = bytes.AsSpan(0, 8).ToArray();
        byte[] payload = bytes.AsSpan(8).ToArray();
        if (Matches(prefix, new byte[] { 65, 83, 67, 73, 73, 0, 0, 0 }))
        {
            RawTextValue value = DecodeAsciiPayload(payload);
            value.Prefix = prefix;
            return value;
        }
        if (Matches(prefix, new byte[] { 74, 73, 83, 0, 0, 0, 0, 0 }))
        {
            return DecodeJisPayload(payload, prefix);
        }
        if (Matches(prefix, new byte[] { 85, 78, 73, 67, 79, 68, 69, 0 }))
        {
            return DecodeUnicodePayload(payload, prefix, byteOrder);
        }
        string text = System.Text.Encoding.UTF8.GetString(payload);
        return new RawTextValue(text, RawTextEncoding.Unknown, payload, prefix, false, false);
    }

    internal static RawTagValue EncodeAscii(string text)
    {
        RequireText(text);
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127)
            {
                throw RawTagValue.Invalid("Exif ASCII text can contain only seven-bit characters.");
            }
            index++;
        }
        byte[] content = System.Text.Encoding.ASCII.GetBytes(text);
        byte[] bytes = new byte[content.Length + 1];
        content.AsSpan().CopyTo(bytes);
        return new RawTagValue(RawTagType.Ascii, (ulong)bytes.Length, RawByteOrder.LittleEndian, bytes);
    }

    internal static RawTagValue EncodeUtf8(string text)
    {
        RequireText(text);
        byte[] content = new System.Text.UTF8Encoding(false).GetBytes(text);
        byte[] bytes = new byte[content.Length + 1];
        content.AsSpan().CopyTo(bytes);
        return new RawTagValue(RawTagType.Utf8, (ulong)bytes.Length, RawByteOrder.LittleEndian, bytes);
    }

    internal static RawTagValue EncodePrefixed(string text, RawTextEncoding encoding, RawByteOrder byteOrder)
    {
        RequireText(text);
        RawTagValue.RequireByteOrder(byteOrder);
        byte[] prefix;
        byte[] payload;
        switch (encoding)
        {
            case RawTextEncoding.Ascii:
                prefix = new byte[] { 65, 83, 67, 73, 73, 0, 0, 0 };
                payload = EncodeAsciiPayload(text);
                break;
            case RawTextEncoding.Jis:
                prefix = new byte[] { 74, 73, 83, 0, 0, 0, 0, 0 };
                payload = Jis.GetBytes(text);
                break;
            case RawTextEncoding.Unicode:
            case RawTextEncoding.Utf8:
                prefix = new byte[] { 85, 78, 73, 67, 79, 68, 69, 0 };
                payload = new System.Text.UTF8Encoding(false).GetBytes(text);
                break;
            case RawTextEncoding.Unknown:
                prefix = new byte[8];
                payload = new System.Text.UTF8Encoding(false).GetBytes(text);
                break;
            default:
                throw RawTagValue.Invalid("The Exif text encoding is not defined.");
        }
        byte[] bytes = new byte[prefix.Length + payload.Length];
        prefix.AsSpan().CopyTo(bytes);
        payload.AsSpan().CopyTo(bytes.AsSpan(prefix.Length));
        return new RawTagValue(RawTagType.Undefined, (ulong)bytes.Length, byteOrder, bytes);
    }

    internal static RawTextValue DecodeAsciiPayload(byte[] payload)
    {
        int length = TextLength(payload, 1);
        bool terminated = length < payload.Length;
        byte[] textBytes = payload.AsSpan(0, length).ToArray();
        bool valid = true;
        int index = 0;
        while (index < textBytes.Length)
        {
            if (textBytes[index] > 127)
            {
                valid = false;
            }
            index++;
        }
        return new RawTextValue(System.Text.Encoding.ASCII.GetString(textBytes), RawTextEncoding.Ascii, textBytes, null, terminated, valid);
    }

    internal static RawTextValue DecodeJisPayload(byte[] payload, byte[] prefix)
    {
        int length = TextLength(payload, 1);
        bool terminated = length < payload.Length;
        byte[] textBytes = payload.AsSpan(0, length).ToArray();
        return new RawTextValue(Jis.GetString(textBytes), RawTextEncoding.Jis, textBytes, prefix, terminated, true);
    }

    internal static RawTextValue DecodeUnicodePayload(byte[] payload, byte[] prefix, RawByteOrder byteOrder)
    {
        if (payload.Length >= 2 && payload[0] == 255 && payload[1] == 254)
        {
            return DecodeUtf16Payload(payload, prefix, System.Text.Encoding.Unicode, 2);
        }
        if (payload.Length >= 2 && payload[0] == 254 && payload[1] == 255)
        {
            return DecodeUtf16Payload(payload, prefix, System.Text.Encoding.BigEndianUnicode, 2);
        }
        if (LooksLikeLegacyUtf16(payload, byteOrder))
        {
            System.Text.Encoding encoding = byteOrder == RawByteOrder.BigEndian ? System.Text.Encoding.BigEndianUnicode : System.Text.Encoding.Unicode;
            return DecodeUtf16Payload(payload, prefix, encoding, 0);
        }
        int length = TextLength(payload, 1);
        bool terminated = length < payload.Length;
        byte[] textBytes = payload.AsSpan(0, length).ToArray();
        return new RawTextValue(System.Text.Encoding.UTF8.GetString(textBytes), RawTextEncoding.Unicode, textBytes, prefix, terminated, !HasUtf8Bom(textBytes) && IsValidUtf8(textBytes));
    }

    internal static RawTextValue DecodeUtf16Payload(byte[] payload, byte[] prefix, System.Text.Encoding encoding, int start)
    {
        int length = payload.Length - start;
        bool terminated = false;
        if (length >= 2 && payload[payload.Length - 1] == 0 && payload[payload.Length - 2] == 0)
        {
            length -= 2;
            terminated = true;
        }
        if ((length & 1) != 0)
        {
            length--;
        }
        byte[] textBytes = payload.AsSpan(start, length).ToArray();
        return new RawTextValue(encoding.GetString(textBytes), RawTextEncoding.Unicode, textBytes, prefix, terminated, start == 2 || (payload.Length & 1) == 0);
    }

    internal static bool LooksLikeLegacyUtf16(byte[] payload, RawByteOrder byteOrder)
    {
        if (payload.Length < 2 || (payload.Length & 1) != 0)
        {
            return false;
        }
        int zeroCount = 0;
        int expectedIndex = byteOrder == RawByteOrder.BigEndian ? 0 : 1;
        int index = expectedIndex;
        while (index < payload.Length)
        {
            if (payload[index] == 0)
            {
                zeroCount++;
            }
            index += 2;
        }
        return zeroCount * 2 >= payload.Length / 2;
    }

    internal static byte[] EncodeAsciiPayload(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127)
            {
                throw RawTagValue.Invalid("Exif ASCII text can contain only seven-bit characters.");
            }
            index++;
        }
        return System.Text.Encoding.ASCII.GetBytes(text);
    }

    internal static int TextLength(byte[] bytes, int unit)
    {
        int length = bytes.Length;
        if (unit == 1 && length > 0 && bytes[length - 1] == 0)
        {
            return length - 1;
        }
        return length;
    }

    internal static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191;
    }

    internal static bool IsValidUtf8(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            int first = bytes[index];
            if (first <= 127)
            {
                index++;
                continue;
            }
            if (first >= 194 && first <= 223)
            {
                if (index + 1 >= bytes.Length || !Continuation(bytes[index + 1]))
                {
                    return false;
                }
                index += 2;
                continue;
            }
            if (first >= 224 && first <= 239)
            {
                if (index + 2 >= bytes.Length || !Continuation(bytes[index + 1]) || !Continuation(bytes[index + 2]))
                {
                    return false;
                }
                int second = bytes[index + 1];
                if ((first == 224 && second < 160) || (first == 237 && second >= 160))
                {
                    return false;
                }
                index += 3;
                continue;
            }
            if (first >= 240 && first <= 244)
            {
                if (index + 3 >= bytes.Length || !Continuation(bytes[index + 1]) || !Continuation(bytes[index + 2]) || !Continuation(bytes[index + 3]))
                {
                    return false;
                }
                int second = bytes[index + 1];
                if ((first == 240 && second < 144) || (first == 244 && second >= 144))
                {
                    return false;
                }
                index += 4;
                continue;
            }
            return false;
        }
        return true;
    }

    internal static bool Continuation(byte value)
    {
        return value >= 128 && value <= 191;
    }

    internal static bool Matches(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static void RequireText(string text)
    {
        if (text == null || text.IndexOf('\0') >= 0)
        {
            throw RawTagValue.Invalid("Exif text must be non-null and cannot contain an embedded null.");
        }
    }

    internal static System.Text.Encoding CreateJis()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        return System.Text.Encoding.GetEncoding(50220);
    }
}
