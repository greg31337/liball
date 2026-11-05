using System.Collections.Generic;

namespace jmap;

internal class MimeByteDecodeResult
{
    internal byte[] Bytes = new byte[0];
    internal bool IsMalformed;
    internal bool IsTruncated;
}

internal class MimeTransferDecoder
{
    internal MimeByteDecodeResult DecodeBody(byte[] value, string? encoding, int maximumBytes)
    {
        string normalized = NormalizeEncoding(encoding);
        if (normalized.Length == 0 || normalized == "7bit" || normalized == "8bit" || normalized == "binary")
        {
            MimeByteDecodeResult result = CopyIdentityBody(value, maximumBytes);
            if (normalized == "7bit" && ContainsEightBitByte(value))
            {
                result.IsMalformed = true;
            }

            return result;
        }

        if (normalized == "base64")
        {
            return DecodeBase64Body(value, maximumBytes);
        }

        if (normalized == "quoted-printable")
        {
            return DecodeQuotedPrintableBody(value, maximumBytes);
        }

        MimeByteDecodeResult unknown = CopyIdentityBody(value, maximumBytes);
        unknown.IsMalformed = true;
        return unknown;
    }

    internal MimeByteDecodeResult CopyIdentityBody(byte[] value, int maximumBytes)
    {
        MimeByteDecodeResult result = new MimeByteDecodeResult();
        int length = value.Length > maximumBytes ? maximumBytes : value.Length;
        byte[] copied = new byte[length];
        int index = 0;
        while (index < length)
        {
            copied[index] = value[index];
            index = index + 1;
        }

        result.Bytes = copied;
        result.IsTruncated = value.Length > maximumBytes;
        return result;
    }

    internal MimeByteDecodeResult DecodeBase64Body(byte[] value, int maximumBytes)
    {
        MimeByteDecodeResult result = new MimeByteDecodeResult();
        List<byte> decoded = new List<byte>();
        int[] quartet = new int[4];
        int count = 0;
        bool finished = false;
        int index = 0;
        while (index < value.Length)
        {
            byte item = value[index];
            if (IsTransferWhitespace(item))
            {
                index = index + 1;
                continue;
            }

            if (finished)
            {
                result.IsMalformed = true;
                index = index + 1;
                continue;
            }

            int digit = item == 61 ? -2 : Base64Value(item);
            if (digit < 0 && digit != -2)
            {
                result.IsMalformed = true;
                index = index + 1;
                continue;
            }

            quartet[count] = digit;
            count = count + 1;
            if (count == 4)
            {
                if (quartet[0] < 0 || quartet[1] < 0 || (quartet[2] == -2 && quartet[3] != -2))
                {
                    result.IsMalformed = true;
                }
                else
                {
                    AddDecodedByte(decoded, (byte)((quartet[0] << 2) | (quartet[1] >> 4)), maximumBytes, result);
                    if (quartet[2] == -2)
                    {
                        if ((quartet[1] & 15) != 0)
                        {
                            result.IsMalformed = true;
                        }

                        finished = true;
                    }
                    else
                    {
                        AddDecodedByte(decoded, (byte)(((quartet[1] & 15) << 4) | (quartet[2] >> 2)), maximumBytes, result);
                        if (quartet[3] == -2)
                        {
                            if ((quartet[2] & 3) != 0)
                            {
                                result.IsMalformed = true;
                            }

                            finished = true;
                        }
                        else
                        {
                            AddDecodedByte(decoded, (byte)(((quartet[2] & 3) << 6) | quartet[3]), maximumBytes, result);
                        }
                    }
                }

                count = 0;
            }

            index = index + 1;
        }

        if (count != 0)
        {
            result.IsMalformed = true;
            if (count >= 2 && quartet[0] >= 0 && quartet[1] >= 0)
            {
                AddDecodedByte(decoded, (byte)((quartet[0] << 2) | (quartet[1] >> 4)), maximumBytes, result);
                if (count >= 3 && quartet[2] >= 0)
                {
                    AddDecodedByte(decoded, (byte)(((quartet[1] & 15) << 4) | (quartet[2] >> 2)), maximumBytes, result);
                }
            }
        }

        result.Bytes = decoded.ToArray();
        return result;
    }

    internal MimeByteDecodeResult DecodeQuotedPrintableBody(byte[] value, int maximumBytes)
    {
        MimeByteDecodeResult result = new MimeByteDecodeResult();
        List<byte> decoded = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 32 || value[index] == 9)
            {
                int whitespaceEnd = index;
                while (whitespaceEnd < value.Length && (value[whitespaceEnd] == 32 || value[whitespaceEnd] == 9))
                {
                    whitespaceEnd = whitespaceEnd + 1;
                }

                if (whitespaceEnd == value.Length || value[whitespaceEnd] == 13 || value[whitespaceEnd] == 10)
                {
                    index = whitespaceEnd;
                    continue;
                }
            }

            if (value[index] != 61)
            {
                if (value[index] > 126 || value[index] == 0 || value[index] == 13 &&
                    (index + 1 >= value.Length || value[index + 1] != 10) || value[index] == 10 &&
                    (index == 0 || value[index - 1] != 13))
                {
                    result.IsMalformed = true;
                }

                AddDecodedByte(decoded, value[index], maximumBytes, result);
                index = index + 1;
                continue;
            }

            if (index + 2 < value.Length && value[index + 1] == 13 && value[index + 2] == 10)
            {
                index = index + 3;
                continue;
            }

            if (index + 1 < value.Length && value[index + 1] == 10)
            {
                result.IsMalformed = true;
                index = index + 2;
                continue;
            }

            if (index + 2 < value.Length)
            {
                int high = HexValue(value[index + 1]);
                int low = HexValue(value[index + 2]);
                if (high >= 0 && low >= 0)
                {
                    AddDecodedByte(decoded, (byte)((high * 16) + low), maximumBytes, result);
                    index = index + 3;
                    continue;
                }
            }

            result.IsMalformed = true;
            AddDecodedByte(decoded, value[index], maximumBytes, result);
            index = index + 1;
        }

        result.Bytes = decoded.ToArray();
        return result;
    }

    internal void AddDecodedByte(List<byte> value, byte item, int maximumBytes, MimeByteDecodeResult result)
    {
        if (value.Count < maximumBytes)
        {
            value.Add(item);
        }
        else
        {
            result.IsTruncated = true;
        }
    }

    internal string NormalizeEncoding(string? value)
    {
        if (value == null)
        {
            return "";
        }

        int start = 0;
        while (start < value.Length && (value[start] == ' ' || value[start] == '\t' || value[start] == '\r' || value[start] == '\n'))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == ' ' || value[end - 1] == '\t' || value[end - 1] == '\r' || value[end - 1] == '\n'))
        {
            end = end - 1;
        }

        return value.Substring(start, end - start).ToLowerInvariant();
    }

    internal bool ContainsEightBitByte(byte[] value)
    {
        foreach (byte item in value)
        {
            if (item > 127)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsTransferWhitespace(byte value)
    {
        return value == 32 || value == 9 || value == 13 || value == 10;
    }

    internal int Base64Value(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return value - 65;
        }

        if (value >= 97 && value <= 122)
        {
            return value - 97 + 26;
        }

        if (value >= 48 && value <= 57)
        {
            return value - 48 + 52;
        }

        if (value == 43)
        {
            return 62;
        }

        return value == 47 ? 63 : -1;
    }

    internal int HexValue(byte value)
    {
        if (value >= 48 && value <= 57)
        {
            return value - 48;
        }

        if (value >= 65 && value <= 70)
        {
            return value - 65 + 10;
        }

        if (value >= 97 && value <= 102)
        {
            return value - 97 + 10;
        }

        return -1;
    }
}
