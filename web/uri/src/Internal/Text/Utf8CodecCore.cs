using System;

namespace uri;

internal static class Utf8CodecCore
{
    internal static int GetEncodedLength(int scalarValue)
    {
        if (!UnicodeScalar.IsValid(scalarValue))
        {
            return 0;
        }

        if (scalarValue <= 127)
        {
            return 1;
        }

        if (scalarValue <= 2047)
        {
            return 2;
        }

        if (scalarValue <= 65535)
        {
            return 3;
        }

        return 4;
    }

    internal static int WriteScalar(int scalarValue, byte[] output, int offset)
    {
        int length = GetEncodedLength(scalarValue);
        if (length == 0)
        {
            return 0;
        }

        if (length == 1)
        {
            output[offset] = (byte)scalarValue;
            return 1;
        }

        if (length == 2)
        {
            output[offset] = (byte)(192 | scalarValue >> 6);
            output[offset + 1] = (byte)(128 | scalarValue & 63);
            return 2;
        }

        if (length == 3)
        {
            output[offset] = (byte)(224 | scalarValue >> 12);
            output[offset + 1] = (byte)(128 | scalarValue >> 6 & 63);
            output[offset + 2] = (byte)(128 | scalarValue & 63);
            return 3;
        }

        output[offset] = (byte)(240 | scalarValue >> 18);
        output[offset + 1] = (byte)(128 | scalarValue >> 12 & 63);
        output[offset + 2] = (byte)(128 | scalarValue >> 6 & 63);
        output[offset + 3] = (byte)(128 | scalarValue & 63);
        return 4;
    }

    internal static Utf8EncodeResult EncodeScalar(int scalarValue)
    {
        int length = GetEncodedLength(scalarValue);
        if (length == 0)
        {
            return new Utf8EncodeResult(false, new byte[0], 0, 0);
        }

        byte[] bytes = new byte[length];
        WriteScalar(scalarValue, bytes, 0);
        return new Utf8EncodeResult(true, bytes, 0, 0);
    }

    internal static Utf8EncodeResult EncodeText(ReadOnlySpan<char> text)
    {
        int byteCount = 0;
        int textOffset = 0;
        while (textOffset < text.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, textOffset);
            if (!scalar.Success)
            {
                return new Utf8EncodeResult(false, new byte[0], scalar.ErrorOffset, scalar.ErrorLength);
            }

            int scalarLength = GetEncodedLength(scalar.ScalarValue);
            if (byteCount > int.MaxValue - scalarLength)
            {
                return new Utf8EncodeResult(false, new byte[0], textOffset, scalar.CodeUnitLength);
            }

            byteCount = byteCount + scalarLength;
            textOffset = textOffset + scalar.CodeUnitLength;
        }

        byte[] bytes = new byte[byteCount];
        int byteOffset = 0;
        textOffset = 0;
        while (textOffset < text.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, textOffset);
            byteOffset = byteOffset + WriteScalar(scalar.ScalarValue, bytes, byteOffset);
            textOffset = textOffset + scalar.CodeUnitLength;
        }

        return new Utf8EncodeResult(true, bytes, 0, 0);
    }

    internal static Utf8ScalarResult DecodeScalar(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset < 0)
        {
            return new Utf8ScalarResult(false, 0, 0, 0, 0);
        }

        if (offset >= bytes.Length)
        {
            return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
        }

        byte first = bytes[offset];
        if (first <= 127)
        {
            return new Utf8ScalarResult(true, first, 1, 0, 0);
        }

        if (first >= 194 && first <= 223)
        {
            if (offset + 1 >= bytes.Length)
            {
                return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
            }

            byte second = bytes[offset + 1];
            if (!IsContinuationByte(second))
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            int scalarValue = (first & 31) << 6 | second & 63;
            return new Utf8ScalarResult(true, scalarValue, 2, 0, 0);
        }

        if (first >= 224 && first <= 239)
        {
            if (offset + 1 >= bytes.Length)
            {
                return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
            }

            byte second = bytes[offset + 1];
            if (!IsContinuationByte(second))
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            if (first == 224 && second < 160)
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            if (first == 237 && second >= 160)
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            if (offset + 2 >= bytes.Length)
            {
                return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
            }

            byte third = bytes[offset + 2];
            if (!IsContinuationByte(third))
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 2, 1);
            }

            int scalarValue = (first & 15) << 12 | (second & 63) << 6 | third & 63;
            return new Utf8ScalarResult(true, scalarValue, 3, 0, 0);
        }

        if (first >= 240 && first <= 244)
        {
            if (offset + 1 >= bytes.Length)
            {
                return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
            }

            byte second = bytes[offset + 1];
            if (!IsContinuationByte(second))
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            if (first == 240 && second < 144)
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            if (first == 244 && second > 143)
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 1, 1);
            }

            if (offset + 2 >= bytes.Length)
            {
                return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
            }

            byte third = bytes[offset + 2];
            if (!IsContinuationByte(third))
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 2, 1);
            }

            if (offset + 3 >= bytes.Length)
            {
                return new Utf8ScalarResult(false, 0, 0, bytes.Length, 0);
            }

            byte fourth = bytes[offset + 3];
            if (!IsContinuationByte(fourth))
            {
                return new Utf8ScalarResult(false, 0, 0, offset + 3, 1);
            }

            int scalarValue = (first & 7) << 18 |
                (second & 63) << 12 |
                (third & 63) << 6 |
                fourth & 63;
            if (!UnicodeScalar.IsValid(scalarValue))
            {
                return new Utf8ScalarResult(false, 0, 0, offset, 4);
            }

            return new Utf8ScalarResult(true, scalarValue, 4, 0, 0);
        }

        return new Utf8ScalarResult(false, 0, 0, offset, 1);
    }

    internal static Utf8DecodeResult DecodeBytes(ReadOnlySpan<byte> bytes)
    {
        int characterCount = 0;
        int byteOffset = 0;
        while (byteOffset < bytes.Length)
        {
            Utf8ScalarResult scalar = DecodeScalar(bytes, byteOffset);
            if (!scalar.Success)
            {
                return new Utf8DecodeResult(false, "", scalar.ErrorOffset, scalar.ErrorLength);
            }

            int scalarLength = UnicodeScalar.GetCodeUnitLength(scalar.ScalarValue);
            if (characterCount > int.MaxValue - scalarLength)
            {
                return new Utf8DecodeResult(false, "", byteOffset, scalar.ByteLength);
            }

            characterCount = characterCount + scalarLength;
            byteOffset = byteOffset + scalar.ByteLength;
        }

        char[] characters = new char[characterCount];
        int characterOffset = 0;
        byteOffset = 0;
        while (byteOffset < bytes.Length)
        {
            Utf8ScalarResult scalar = DecodeScalar(bytes, byteOffset);
            if (scalar.ScalarValue <= 65535)
            {
                characters[characterOffset] = (char)scalar.ScalarValue;
                characterOffset = characterOffset + 1;
            }
            else
            {
                int supplementaryValue = scalar.ScalarValue - 65536;
                characters[characterOffset] = (char)(55296 + supplementaryValue / 1024);
                characters[characterOffset + 1] = (char)(56320 + supplementaryValue % 1024);
                characterOffset = characterOffset + 2;
            }

            byteOffset = byteOffset + scalar.ByteLength;
        }

        return new Utf8DecodeResult(true, new string(characters), 0, 0);
    }

    internal static bool IsContinuationByte(byte value)
    {
        return value >= 128 && value <= 191;
    }
}
