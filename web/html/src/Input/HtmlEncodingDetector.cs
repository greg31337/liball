using System;

namespace html;

internal class HtmlPrescanResult
{
    internal bool found;
    internal string encodingName;
    internal HtmlEncodingSource source;
    internal int byteOffset;

    internal HtmlPrescanResult(bool found, string encodingName, HtmlEncodingSource source, int byteOffset)
    {
        this.found = found;
        this.encodingName = encodingName;
        this.source = source;
        this.byteOffset = byteOffset;
    }

    internal bool GetFound()
    {
        return this.found;
    }

    internal string GetEncodingName()
    {
        return this.encodingName;
    }

    internal HtmlEncodingSource GetSource()
    {
        return this.source;
    }

    internal int GetByteOffset()
    {
        return this.byteOffset;
    }
}

internal class HtmlPrescanAttribute
{
    internal bool found;
    internal string name;
    internal string value;
    internal int nextPosition;

    internal HtmlPrescanAttribute(bool found, string name, string value, int nextPosition)
    {
        this.found = found;
        this.name = name;
        this.value = value;
        this.nextPosition = nextPosition;
    }

    internal bool GetFound()
    {
        return this.found;
    }

    internal string GetName()
    {
        return this.name;
    }

    internal string GetValue()
    {
        return this.value;
    }

    internal int GetNextPosition()
    {
        return this.nextPosition;
    }
}

/// <summary>Detects HTML encoding from bounded input without consuming external resources.</summary>
internal static class HtmlEncodingDetector
{
    /// <summary>Normalizes a WHATWG encoding label to the canonical name used by the decoder.</summary>
    /// <param name="label">The label to trim and match without ASCII case sensitivity.</param>
    /// <returns>The canonical encoding name, or an empty string when the label is unknown.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    public static string GetEncodingName(string label)
    {
        return WebEncodingLabels.GetEncodingName(label);
    }

    /// <summary>Normalizes a WHATWG encoding label to the canonical name used by the decoder.</summary>
    /// <param name="label">The label to trim and match without ASCII case sensitivity.</param>
    /// <returns>The canonical encoding name, or an empty string when the label is unknown.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static string GetEncodingName(ReadOnlySpan<char> label)
    {
        return WebEncodingLabels.GetEncodingName(label);
    }

    /// <summary>Detects the encoding from a bounded prefix of the supplied bytes.</summary>
    /// <param name="input">The input data to process.</param>
    /// <returns>The detected encoding, confidence, source, byte-order mark length, and declaration location.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static HtmlEncodingDetectionResult Detect(ReadOnlySpan<byte> input)
    {
        return Detect(input, "", "windows-1252");
    }

    /// <summary>Detects the encoding from a bounded prefix of the supplied bytes.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="transportLabel">The encoding label supplied by the transport layer, or an empty string when absent.</param>
    /// <returns>The detected encoding, confidence, source, byte-order mark length, and declaration location.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static HtmlEncodingDetectionResult Detect(ReadOnlySpan<byte> input, string transportLabel)
    {
        return Detect(input, transportLabel, "windows-1252");
    }

    /// <summary>Detects the encoding from a bounded prefix of the supplied bytes.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="transportLabel">The encoding label supplied by the transport layer, or an empty string when absent.</param>
    /// <param name="defaultLabel">The fallback encoding label used when no stronger signal exists.</param>
    /// <returns>The detected encoding, confidence, source, byte-order mark length, and declaration location.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlEncodingDetectionResult Detect(ReadOnlySpan<byte> input, string transportLabel, string defaultLabel)
    {
        if (transportLabel == null)
        {
            throw new ArgumentNullException("transportLabel");
        }

        if (defaultLabel == null)
        {
            throw new ArgumentNullException("defaultLabel");
        }

        HtmlEncodingDetectionResult? bom = DetectBom(input);
        if (bom != null)
        {
            return bom;
        }

        string transportName = WebEncodingLabels.GetEncodingName(transportLabel);
        if (transportName.Length > 0)
        {
            return new HtmlEncodingDetectionResult(transportName, HtmlEncodingConfidence.Certain,
                HtmlEncodingSource.Transport, 0, -1);
        }

        HtmlPrescanResult prescan = Prescan(input);
        if (prescan.GetFound())
        {
            return new HtmlEncodingDetectionResult(prescan.GetEncodingName(), HtmlEncodingConfidence.Tentative,
                prescan.GetSource(), 0, prescan.GetByteOffset());
        }

        string defaultName = WebEncodingLabels.GetEncodingName(defaultLabel);
        if (defaultName.Length == 0)
        {
            defaultName = "windows-1252";
        }

        return new HtmlEncodingDetectionResult(defaultName, HtmlEncodingConfidence.Tentative,
            HtmlEncodingSource.Default, 0, -1);
    }

    internal static HtmlEncodingDetectionResult? DetectBom(ReadOnlySpan<byte> input)
    {
        if (input.Length >= 3 && input[0] == 0xEF && input[1] == 0xBB && input[2] == 0xBF)
        {
            return new HtmlEncodingDetectionResult("UTF-8", HtmlEncodingConfidence.Certain,
                HtmlEncodingSource.Bom, 3, -1);
        }

        if (input.Length >= 2 && input[0] == 0xFE && input[1] == 0xFF)
        {
            return new HtmlEncodingDetectionResult("UTF-16BE", HtmlEncodingConfidence.Certain,
                HtmlEncodingSource.Bom, 2, -1);
        }

        if (input.Length >= 2 && input[0] == 0xFF && input[1] == 0xFE)
        {
            return new HtmlEncodingDetectionResult("UTF-16LE", HtmlEncodingConfidence.Certain,
                HtmlEncodingSource.Bom, 2, -1);
        }

        return null;
    }

    internal static HtmlPrescanResult Prescan(ReadOnlySpan<byte> input)
    {
        int length = input.Length < 1024 ? input.Length : 1024;
        ReadOnlySpan<byte> bytes = input.Slice(0, length);
        if (StartsWith(bytes, 0, new byte[] { 0x3C, 0x00, 0x3F, 0x00, 0x78, 0x00 }))
        {
            return Found("UTF-16LE", HtmlEncodingSource.XmlDeclaration, 0);
        }

        if (StartsWith(bytes, 0, new byte[] { 0x00, 0x3C, 0x00, 0x3F, 0x00, 0x78 }))
        {
            return Found("UTF-16BE", HtmlEncodingSource.XmlDeclaration, 0);
        }

        int position = 0;
        while (position < bytes.Length)
        {
            if (StartsWithAsciiIgnoreCase(bytes, position, "<meta")
                && position + 5 < bytes.Length && IsTagDelimiter(bytes[position + 5]))
            {
                HtmlPrescanResult meta = ScanMeta(bytes, position, position + 5);
                if (meta.GetFound())
                {
                    return meta;
                }

                position = SkipTag(bytes, position + 5);
            }
            else if (StartsWith(bytes, position, new byte[] { 0x3C, 0x21, 0x2D, 0x2D }))
            {
                position = SkipComment(bytes, position + 4);
            }
            else if (bytes[position] == 0x3C)
            {
                position = SkipTag(bytes, position + 1);
            }
            else
            {
                position = position + 1;
            }
        }

        return ScanXmlDeclaration(bytes);
    }

    internal static HtmlPrescanResult ScanMeta(ReadOnlySpan<byte> bytes, int tagOffset, int position)
    {
        bool hasCharset = false;
        string charset = "";
        bool needsPragma = false;
        bool hasPragma = false;
        bool seenCharset = false;
        bool seenContent = false;
        bool seenHttpEquiv = false;

        while (position < bytes.Length)
        {
            HtmlPrescanAttribute attribute = ReadAttribute(bytes, position);
            position = attribute.GetNextPosition();
            if (!attribute.GetFound())
            {
                break;
            }

            string name = attribute.GetName();
            if (name == "charset" && !seenCharset)
            {
                seenCharset = true;
                string candidate = WebEncodingLabels.GetEncodingName(attribute.GetValue());
                if (candidate.Length > 0)
                {
                    charset = candidate;
                    hasCharset = true;
                    needsPragma = false;
                }
            }
            else if (name == "content" && !seenContent)
            {
                seenContent = true;
                if (!hasCharset)
                {
                    string candidate = ExtractEncodingFromContent(attribute.GetValue());
                    if (candidate.Length > 0)
                    {
                        charset = candidate;
                        hasCharset = true;
                        needsPragma = true;
                    }
                }
            }
            else if (name == "http-equiv" && !seenHttpEquiv)
            {
                seenHttpEquiv = true;
                hasPragma = Ascii.EqualsIgnoreCase(attribute.GetValue().AsSpan(), "content-type".AsSpan());
            }
        }

        if (!hasCharset || needsPragma && !hasPragma)
        {
            return Missing();
        }

        return Found(RemoveMetaUnsafeEncoding(charset), HtmlEncodingSource.Meta, tagOffset);
    }

    internal static HtmlPrescanAttribute ReadAttribute(ReadOnlySpan<byte> bytes, int position)
    {
        while (position < bytes.Length && (IsHtmlWhitespace(bytes[position]) || bytes[position] == 0x2F))
        {
            position = position + 1;
        }

        if (position >= bytes.Length || bytes[position] == 0x3E)
        {
            if (position < bytes.Length)
            {
                position = position + 1;
            }

            return new HtmlPrescanAttribute(false, "", "", position);
        }

        int nameStart = position;
        while (position < bytes.Length && !IsHtmlWhitespace(bytes[position])
            && bytes[position] != 0x2F && bytes[position] != 0x3E && bytes[position] != 0x3D)
        {
            position = position + 1;
        }

        string name = GetAsciiString(bytes, nameStart, position, true);
        while (position < bytes.Length && IsHtmlWhitespace(bytes[position]))
        {
            position = position + 1;
        }

        if (position >= bytes.Length || bytes[position] != 0x3D)
        {
            return new HtmlPrescanAttribute(true, name, "", position);
        }

        position = position + 1;
        while (position < bytes.Length && IsHtmlWhitespace(bytes[position]))
        {
            position = position + 1;
        }

        int valueStart = position;
        int valueEnd = position;
        if (position < bytes.Length && (bytes[position] == 0x22 || bytes[position] == 0x27))
        {
            byte quote = bytes[position];
            position = position + 1;
            valueStart = position;
            while (position < bytes.Length && bytes[position] != quote)
            {
                position = position + 1;
            }

            valueEnd = position;
            if (position < bytes.Length)
            {
                position = position + 1;
            }
        }
        else
        {
            while (position < bytes.Length && !IsHtmlWhitespace(bytes[position]) && bytes[position] != 0x3E)
            {
                position = position + 1;
            }

            valueEnd = position;
        }

        return new HtmlPrescanAttribute(true, name, GetAsciiString(bytes, valueStart, valueEnd, false), position);
    }

    internal static string ExtractEncodingFromContent(string content)
    {
        ReadOnlySpan<char> value = content.AsSpan();
        int position = 0;
        while (position + 7 <= value.Length)
        {
            if (!Ascii.StartsWithIgnoreCase(value.Slice(position), "charset".AsSpan()))
            {
                position = position + 1;
                continue;
            }

            position = position + 7;
            while (position < value.Length && Ascii.IsHtmlWhitespace(value[position]))
            {
                position = position + 1;
            }

            if (position >= value.Length || value[position] != '=')
            {
                continue;
            }

            position = position + 1;
            while (position < value.Length && Ascii.IsHtmlWhitespace(value[position]))
            {
                position = position + 1;
            }

            int start = position;
            int end = position;
            if (position < value.Length && (value[position] == '\'' || value[position] == '"'))
            {
                char quote = value[position];
                position = position + 1;
                start = position;
                while (position < value.Length && value[position] != quote)
                {
                    position = position + 1;
                }

                end = position;
            }
            else
            {
                while (position < value.Length && !Ascii.IsHtmlWhitespace(value[position]) && value[position] != ';')
                {
                    position = position + 1;
                }

                end = position;
            }

            return WebEncodingLabels.GetEncodingName(value.Slice(start, end - start));
        }

        return "";
    }

    internal static HtmlPrescanResult ScanXmlDeclaration(ReadOnlySpan<byte> bytes)
    {
        if (!StartsWithAsciiIgnoreCase(bytes, 0, "<?xml"))
        {
            return Missing();
        }

        int position = 5;
        while (position < bytes.Length)
        {
            HtmlPrescanAttribute attribute = ReadAttribute(bytes, position);
            position = attribute.GetNextPosition();
            if (!attribute.GetFound())
            {
                break;
            }

            if (attribute.GetName() == "encoding")
            {
                string candidate = WebEncodingLabels.GetEncodingName(attribute.GetValue());
                if (candidate.Length > 0)
                {
                    if (candidate == "UTF-16BE" || candidate == "UTF-16LE")
                    {
                        candidate = "UTF-8";
                    }

                    return Found(candidate, HtmlEncodingSource.XmlDeclaration, 0);
                }
            }
        }

        return Missing();
    }

    internal static string RemoveMetaUnsafeEncoding(string encodingName)
    {
        if (encodingName == "UTF-16BE" || encodingName == "UTF-16LE")
        {
            return "UTF-8";
        }

        if (encodingName == "x-user-defined")
        {
            return "windows-1252";
        }

        return encodingName;
    }

    internal static int SkipComment(ReadOnlySpan<byte> bytes, int position)
    {
        while (position + 2 < bytes.Length)
        {
            if (bytes[position] == 0x2D && bytes[position + 1] == 0x2D && bytes[position + 2] == 0x3E)
            {
                return position + 3;
            }

            position = position + 1;
        }

        return bytes.Length;
    }

    internal static int SkipTag(ReadOnlySpan<byte> bytes, int position)
    {
        byte quote = 0;
        while (position < bytes.Length)
        {
            byte value = bytes[position];
            if (quote == 0 && (value == 0x22 || value == 0x27))
            {
                quote = value;
            }
            else if (quote != 0 && value == quote)
            {
                quote = 0;
            }
            else if (quote == 0 && value == 0x3E)
            {
                return position + 1;
            }

            position = position + 1;
        }

        return bytes.Length;
    }

    internal static bool StartsWith(ReadOnlySpan<byte> bytes, int position, byte[] prefix)
    {
        if (position < 0 || position + prefix.Length > bytes.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (bytes[position + index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool StartsWithAsciiIgnoreCase(ReadOnlySpan<byte> bytes, int position, string prefix)
    {
        if (position < 0 || position + prefix.Length > bytes.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            byte value = bytes[position + index];
            if (value >= 0x41 && value <= 0x5A)
            {
                value = (byte)(value + 0x20);
            }

            if (value != (byte)prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsTagDelimiter(byte value)
    {
        return IsHtmlWhitespace(value) || value == 0x2F || value == 0x3E;
    }

    internal static bool IsHtmlWhitespace(byte value)
    {
        return value == 0x09 || value == 0x0A || value == 0x0C || value == 0x0D || value == 0x20;
    }

    internal static string GetAsciiString(ReadOnlySpan<byte> bytes, int start, int end, bool lower)
    {
        char[] characters = new char[end - start];
        int index = 0;
        while (start + index < end)
        {
            byte value = bytes[start + index];
            if (lower && value >= 0x41 && value <= 0x5A)
            {
                value = (byte)(value + 0x20);
            }

            characters[index] = (char)value;
            index = index + 1;
        }

        return new string(characters);
    }

    internal static HtmlPrescanResult Found(string name, HtmlEncodingSource source, int byteOffset)
    {
        return new HtmlPrescanResult(true, name, source, byteOffset);
    }

    internal static HtmlPrescanResult Missing()
    {
        return new HtmlPrescanResult(false, "", HtmlEncodingSource.Default, -1);
    }
}
