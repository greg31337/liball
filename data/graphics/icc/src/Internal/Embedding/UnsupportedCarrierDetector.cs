namespace icc;

internal static class UnsupportedCarrierDetector
{
    internal static string DetectName(System.ReadOnlySpan<byte> bytes)
    {
        if (IsPdf(bytes))
        {
            return "PDF";
        }
        if (IsOpenXps(bytes))
        {
            return "OpenXPS";
        }
        if (IsEps(bytes))
        {
            return "EPS";
        }
        if (IsJpegXl(bytes))
        {
            return "JPEG XL";
        }
        if (IsSvg(bytes))
        {
            return "SVG";
        }
        if (IsCss(bytes))
        {
            return "CSS";
        }
        return "";
    }

    internal static bool IsPdf(System.ReadOnlySpan<byte> bytes)
    {
        return FindAscii(bytes, "%PDF-", 1024, false) >= 0;
    }

    internal static bool IsOpenXps(System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0x50 || bytes[1] != 0x4b
            || !((bytes[2] == 0x03 && bytes[3] == 0x04)
                || (bytes[2] == 0x05 && bytes[3] == 0x06)
                || (bytes[2] == 0x07 && bytes[3] == 0x08)))
        {
            return false;
        }
        int scanLength = bytes.Length;
        if (scanLength > 65536)
        {
            scanLength = 65536;
        }
        return FindAscii(bytes, ".fdseq", scanLength, true) >= 0
            || FindAscii(bytes, ".fdoc", scanLength, true) >= 0
            || FindAscii(bytes, ".fpage", scanLength, true) >= 0
            || FindAscii(bytes, "FixedDocumentSequence", scanLength,
                true) >= 0
            || FindAscii(bytes, "application/vnd.ms-package.xps",
                scanLength, true) >= 0
            || FindAscii(bytes, "schemas.openxps.org/oxps", scanLength,
                true) >= 0;
    }

    internal static bool IsSvg(System.ReadOnlySpan<byte> bytes)
    {
        int offset = SkipTextPrefix(bytes, 0);
        bool scanning = true;
        while (scanning && offset < bytes.Length)
        {
            if (StartsWithAscii(bytes, offset, "<?", false))
            {
                int end = FindAscii(bytes.Slice(offset + 2), "?>", 2048,
                    false);
                if (end < 0)
                {
                    return false;
                }
                offset = SkipTextPrefix(bytes, offset + end + 4);
            }
            else if (StartsWithAscii(bytes, offset, "<!--", false))
            {
                int end = FindAscii(bytes.Slice(offset + 4), "-->", 2048,
                    false);
                if (end < 0)
                {
                    return false;
                }
                offset = SkipTextPrefix(bytes, offset + end + 7);
            }
            else if (StartsWithAscii(bytes, offset, "<!DOCTYPE", true))
            {
                int end = FindByte(bytes, offset + 9, 0x3e, 2048);
                if (end < 0)
                {
                    return false;
                }
                offset = SkipTextPrefix(bytes, end + 1);
            }
            else
            {
                scanning = false;
            }
        }
        if (StartsWithAscii(bytes, offset, "<svg", false)
            && HasMarkupBoundary(bytes, offset + 4))
        {
            return true;
        }
        return IsUtf16Svg(bytes);
    }

    internal static bool IsCss(System.ReadOnlySpan<byte> bytes)
    {
        int offset = SkipCssPrefix(bytes);
        if (offset >= bytes.Length || bytes[offset] == 0x3c)
        {
            return false;
        }
        if (StartsWithAscii(bytes, offset, "@charset", true)
            || StartsWithAscii(bytes, offset, "@import", true)
            || StartsWithAscii(bytes, offset, "@namespace", true)
            || StartsWithAscii(bytes, offset, "@media", true)
            || StartsWithAscii(bytes, offset, "@supports", true)
            || StartsWithAscii(bytes, offset, "@font-face", true)
            || StartsWithAscii(bytes, offset, "@page", true)
            || StartsWithAscii(bytes, offset, "@keyframes", true)
            || StartsWithAscii(bytes, offset, "@color-profile", true)
            || StartsWithAscii(bytes, offset, "@layer", true)
            || StartsWithAscii(bytes, offset, "@container", true)
            || StartsWithAscii(bytes, offset, "@scope", true))
        {
            return true;
        }

        int open = FindByte(bytes, offset, 0x7b, 2048);
        if (open <= offset)
        {
            return false;
        }
        int colon = FindByte(bytes, open + 1, 0x3a, 2048);
        int close = FindByte(bytes, open + 1, 0x7d, 2048);
        return colon > open && close > colon;
    }

    internal static bool IsEps(System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xc5 && bytes[1] == 0xd0
            && bytes[2] == 0xd3 && bytes[3] == 0xc6)
        {
            return true;
        }
        return StartsWithAscii(bytes, 0, "%!PS-Adobe-", false)
            && FindAscii(bytes, " EPSF-", 160, false) >= 0;
    }

    internal static bool IsJpegXl(System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0x0a)
        {
            return true;
        }
        return bytes.Length >= 12 && bytes[0] == 0 && bytes[1] == 0
            && bytes[2] == 0 && bytes[3] == 0x0c && bytes[4] == 0x4a
            && bytes[5] == 0x58 && bytes[6] == 0x4c && bytes[7] == 0x20
            && bytes[8] == 0x0d && bytes[9] == 0x0a
            && bytes[10] == 0x87 && bytes[11] == 0x0a;
    }

    internal static int SkipTextPrefix(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        if (offset == 0 && bytes.Length >= 3 && bytes[0] == 0xef
            && bytes[1] == 0xbb && bytes[2] == 0xbf)
        {
            offset = 3;
        }
        while (offset < bytes.Length && IsWhitespace(bytes[offset]))
        {
            offset = offset + 1;
        }
        return offset;
    }

    internal static int SkipCssPrefix(System.ReadOnlySpan<byte> bytes)
    {
        int offset = SkipTextPrefix(bytes, 0);
        bool scanning = true;
        while (scanning && StartsWithAscii(bytes, offset, "/*", false))
        {
            int end = FindAscii(bytes.Slice(offset + 2), "*/", 2048, false);
            if (end < 0)
            {
                return bytes.Length;
            }
            offset = SkipTextPrefix(bytes, offset + end + 4);
            scanning = offset < bytes.Length;
        }
        return offset;
    }

    internal static bool IsUtf16Svg(System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 8)
        {
            return false;
        }
        int limit = bytes.Length;
        if (limit > 2048)
        {
            limit = 2048;
        }
        int index = 0;
        while (index <= limit - 8)
        {
            if (bytes[index] == 0x3c && bytes[index + 1] == 0
                && bytes[index + 2] == 0x73 && bytes[index + 3] == 0
                && bytes[index + 4] == 0x76 && bytes[index + 5] == 0
                && bytes[index + 6] == 0x67 && bytes[index + 7] == 0)
            {
                return true;
            }
            if (bytes[index] == 0 && bytes[index + 1] == 0x3c
                && bytes[index + 2] == 0 && bytes[index + 3] == 0x73
                && bytes[index + 4] == 0 && bytes[index + 5] == 0x76
                && bytes[index + 6] == 0 && bytes[index + 7] == 0x67)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasMarkupBoundary(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return offset >= bytes.Length || IsWhitespace(bytes[offset])
            || bytes[offset] == 0x2f || bytes[offset] == 0x3e;
    }

    internal static bool IsWhitespace(byte value)
    {
        return value == 0x09 || value == 0x0a || value == 0x0c
            || value == 0x0d || value == 0x20;
    }

    internal static int FindByte(System.ReadOnlySpan<byte> bytes, int offset,
        byte value, int maximumLength)
    {
        int end = bytes.Length;
        long requestedEnd = (long)offset + maximumLength;
        if (requestedEnd < end)
        {
            end = (int)requestedEnd;
        }
        while (offset < end)
        {
            if (bytes[offset] == value)
            {
                return offset;
            }
            offset = offset + 1;
        }
        return -1;
    }

    internal static int FindAscii(System.ReadOnlySpan<byte> bytes,
        string value, int maximumLength, bool ignoreCase)
    {
        int limit = bytes.Length;
        if (maximumLength < limit)
        {
            limit = maximumLength;
        }
        int offset = 0;
        while (offset <= limit - value.Length)
        {
            if (StartsWithAscii(bytes, offset, value, ignoreCase))
            {
                return offset;
            }
            offset = offset + 1;
        }
        return -1;
    }

    internal static bool StartsWithAscii(System.ReadOnlySpan<byte> bytes,
        int offset, string value, bool ignoreCase)
    {
        if (offset < 0 || value.Length > bytes.Length - offset)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            byte actual = bytes[offset + index];
            byte expected = (byte)value[index];
            if (ignoreCase)
            {
                actual = ToLowerAscii(actual);
                expected = ToLowerAscii(expected);
            }
            if (actual != expected)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static byte ToLowerAscii(byte value)
    {
        if (value >= 0x41 && value <= 0x5a)
        {
            return (byte)(value + 0x20);
        }
        return value;
    }
}
