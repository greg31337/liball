using System.Collections.Generic;
using System.Text;

namespace image;

internal class MetadataStringReader
{
    internal static string DecodeAscii(byte[] bytes)
    {
        return DecodeAsciiRange(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static string DecodeAsciiRange(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);
        return Encoding.ASCII.GetString(range, 0, range.Length);
    }

    internal static string DecodeUtf8(byte[] bytes)
    {
        return DecodeUtf8Range(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static string DecodeUtf8Range(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);
        return Encoding.UTF8.GetString(range, 0, range.Length);
    }

    internal static string DecodeUtf8WithLatin1Fallback(byte[] bytes, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        return DecodeUtf8RangeWithLatin1Fallback(bytes, 0, ByteLength(bytes), diagnostics, blockType);
    }

    internal static string DecodeUtf8RangeWithLatin1Fallback(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);

        if (IsValidUtf8(range))
        {
            return Encoding.UTF8.GetString(range, 0, range.Length);
        }

        MetadataDiagnostic.AddWarningAt(diagnostics, "string.utf8.invalid", "UTF-8 string was invalid; decoded as Latin-1.", blockType, offset, count);
        return Encoding.Latin1.GetString(range, 0, range.Length);
    }

    internal static string DecodeLatin1(byte[] bytes)
    {
        return DecodeLatin1Range(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static string DecodeLatin1Range(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);
        return Encoding.Latin1.GetString(range, 0, range.Length);
    }

    internal static string DecodeUtf16(byte[] bytes)
    {
        if (bytes == null)
        {
            return "";
        }

        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return DecodeUtf16LittleEndianRange(bytes, 2, bytes.Length - 2, null, "");
        }

        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return DecodeUtf16BigEndianRange(bytes, 2, bytes.Length - 2, null, "");
        }

        return DecodeUtf16LittleEndianRange(bytes, 0, bytes.Length, null, "");
    }

    internal static string DecodeUtf16LittleEndian(byte[] bytes)
    {
        return DecodeUtf16LittleEndianRange(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static string DecodeUtf16LittleEndianRange(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, EvenByteCount(count, diagnostics, blockType, offset), diagnostics, blockType);
        return Encoding.Unicode.GetString(range, 0, range.Length);
    }

    internal static string DecodeUtf16BigEndian(byte[] bytes)
    {
        return DecodeUtf16BigEndianRange(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static string DecodeUtf16BigEndianRange(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, EvenByteCount(count, diagnostics, blockType, offset), diagnostics, blockType);
        return Encoding.BigEndianUnicode.GetString(range, 0, range.Length);
    }

    internal static string DecodeNullTerminatedAscii(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        int end = FindNullTerminator(bytes, offset, count, 1, diagnostics, blockType);
        return DecodeAsciiRange(bytes, offset, end - offset, diagnostics, blockType);
    }

    internal static string DecodeNullTerminatedUtf8(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        int end = FindNullTerminator(bytes, offset, count, 1, diagnostics, blockType);
        return DecodeUtf8RangeWithLatin1Fallback(bytes, offset, end - offset, diagnostics, blockType);
    }

    internal static string DecodeNullTerminatedUtf16LittleEndian(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        int end = FindNullTerminator(bytes, offset, count, 2, diagnostics, blockType);
        return DecodeUtf16LittleEndianRange(bytes, offset, end - offset, diagnostics, blockType);
    }

    internal static string DecodeNullTerminatedUtf16BigEndian(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        int end = FindNullTerminator(bytes, offset, count, 2, diagnostics, blockType);
        return DecodeUtf16BigEndianRange(bytes, offset, end - offset, diagnostics, blockType);
    }

    internal static string DecodePascalAscii(byte[] bytes, int offset, int totalLength, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        int count = PascalPayloadLength(bytes, offset, totalLength, diagnostics, blockType);
        return DecodeAsciiRange(bytes, offset + 1, count, diagnostics, blockType);
    }

    internal static string DecodePascalUtf8(byte[] bytes, int offset, int totalLength, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        int count = PascalPayloadLength(bytes, offset, totalLength, diagnostics, blockType);
        return DecodeUtf8RangeWithLatin1Fallback(bytes, offset + 1, count, diagnostics, blockType);
    }

    internal static int PascalPayloadLength(byte[] bytes, int offset, int totalLength, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.bytes.null", "String byte array was null.", blockType, offset, totalLength);
            return 0;
        }

        if (offset < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.offset.negative", "String offset was negative.", blockType, offset, totalLength);
            return 0;
        }

        if (totalLength <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.pascal.length.invalid", "Pascal string total length was invalid.", blockType, offset, totalLength);
            return 0;
        }

        if (offset >= bytes.Length)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "string.offset.outside", "String offset exceeded byte array length.", blockType, offset, totalLength);
            return 0;
        }

        int available = totalLength - 1;
        int bytesAvailable = bytes.Length - offset - 1;

        if (available > bytesAvailable)
        {
            available = bytesAvailable;
            MetadataDiagnostic.AddTruncated(diagnostics, "string.pascal.field.truncated", "Pascal string field exceeded byte array length.", blockType, offset, totalLength);
        }

        int declared = bytes[offset];

        if (declared > available)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "string.pascal.payload.truncated", "Pascal string declared length exceeded available payload bytes.", blockType, offset, declared);
            return available;
        }

        return declared;
    }

    internal static int FindNullTerminator(byte[] bytes, int offset, int count, int unitWidth, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.bytes.null", "String byte array was null.", blockType, offset, count);
            return offset;
        }

        if (offset < 0 || count < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.range.negative", "String range offset or count was negative.", blockType, offset, count);
            return offset;
        }

        if (unitWidth != 1 && unitWidth != 2)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.terminator.width.invalid", "Null terminator unit width was invalid.", blockType, offset, count);
            return offset;
        }

        if (offset > bytes.Length)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "string.offset.outside", "String offset exceeded byte array length.", blockType, offset, count);
            return bytes.Length;
        }

        int end = offset + count;
        if (end < offset || end > bytes.Length)
        {
            end = bytes.Length;
            MetadataDiagnostic.AddTruncated(diagnostics, "string.range.truncated", "String range exceeded byte array length.", blockType, offset, count);
        }

        int index = offset;
        while (index < end)
        {
            if (unitWidth == 1)
            {
                if (bytes[index] == 0)
                {
                    return index;
                }

                index = index + 1;
            }
            else
            {
                if (index + 1 >= end)
                {
                    return end;
                }

                if (bytes[index] == 0 && bytes[index + 1] == 0)
                {
                    return index;
                }

                index = index + 2;
            }
        }

        return end;
    }

    internal static bool IsValidUtf8(byte[] bytes)
    {
        if (bytes == null)
        {
            return false;
        }

        int index = 0;

        while (index < bytes.Length)
        {
            byte first = bytes[index];

            if (first <= 127)
            {
                index = index + 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (!HasContinuation(bytes, index, 1))
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (first == 224)
            {
                if (!HasByteInRange(bytes, index + 1, 160, 191) || !HasContinuation(bytes, index + 1, 1))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if ((first >= 225 && first <= 236) || (first >= 238 && first <= 239))
            {
                if (!HasContinuation(bytes, index, 2))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first == 237)
            {
                if (!HasByteInRange(bytes, index + 1, 128, 159) || !HasContinuation(bytes, index + 1, 1))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first == 240)
            {
                if (!HasByteInRange(bytes, index + 1, 144, 191) || !HasContinuation(bytes, index + 1, 2))
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            if (first >= 241 && first <= 243)
            {
                if (!HasContinuation(bytes, index, 3))
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            if (first == 244)
            {
                if (!HasByteInRange(bytes, index + 1, 128, 143) || !HasContinuation(bytes, index + 1, 2))
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

    internal static bool HasContinuation(byte[] bytes, int start, int count)
    {
        int index = start + 1;
        int end = start + 1 + count;

        if (end > bytes.Length)
        {
            return false;
        }

        while (index < end)
        {
            if (bytes[index] < 128 || bytes[index] > 191)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasByteInRange(byte[] bytes, int index, int minimum, int maximum)
    {
        if (index < 0 || index >= bytes.Length)
        {
            return false;
        }

        return bytes[index] >= minimum && bytes[index] <= maximum;
    }

    internal static byte[] CopyRange(byte[] bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.bytes.null", "String byte array was null.", blockType, offset, count);
            return new byte[0];
        }

        if (offset < 0 || count < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.range.negative", "String range offset or count was negative.", blockType, offset, count);
            return new byte[0];
        }

        if (offset > bytes.Length)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "string.offset.outside", "String offset exceeded byte array length.", blockType, offset, count);
            return new byte[0];
        }

        int actualCount = count;
        int available = bytes.Length - offset;

        if (actualCount > available)
        {
            actualCount = available;
            MetadataDiagnostic.AddTruncated(diagnostics, "string.range.truncated", "String range exceeded byte array length.", blockType, offset, count);
        }

        byte[] result = new byte[actualCount];
        int index = 0;

        while (index < actualCount)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static int EvenByteCount(int count, List<MetadataDiagnostic>? diagnostics, string blockType, int offset)
    {
        if (count < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "string.utf16.count.negative", "UTF-16 byte count was negative.", blockType, offset, count);
            return 0;
        }

        if ((count & 1) == 0)
        {
            return count;
        }

        MetadataDiagnostic.AddTruncated(diagnostics, "string.utf16.odd.length", "UTF-16 byte count was odd; trailing byte was ignored.", blockType, offset, count);
        return count - 1;
    }

    internal static int ByteLength(byte[] bytes)
    {
        if (bytes == null)
        {
            return 0;
        }

        return bytes.Length;
    }
}
