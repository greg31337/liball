using System;

namespace html;

internal class HtmlDecodedInputBuilder
{
    internal string encodingName;
    internal int byteCount;
    internal char[] characters;
    internal int characterCount;
    internal int[] characterByteOffsets;
    internal int[] byteCharacterOffsets;
    internal HtmlDecodingError[] errors;
    internal int errorCount;
    internal int nextByteOffset;

    internal HtmlDecodedInputBuilder(string encodingName, int byteCount, int firstByteOffset)
    {
        if (byteCount == int.MaxValue)
        {
            throw new InsufficientMemoryException("The byte-to-character map is too large.");
        }

        this.encodingName = encodingName;
        this.byteCount = byteCount;
        this.characters = new char[byteCount];
        this.characterCount = 0;
        this.characterByteOffsets = new int[byteCount + 1];
        this.byteCharacterOffsets = new int[byteCount + 1];
        this.errors = new HtmlDecodingError[4];
        this.errorCount = 0;
        this.nextByteOffset = firstByteOffset;
        this.characterByteOffsets[0] = firstByteOffset;
    }

    internal void AppendScalar(int scalar, int byteStart, int byteEnd)
    {
        if (scalar <= 65535)
        {
            this.AppendCharacter((char)scalar, byteStart, byteEnd);
            return;
        }

        int adjusted = scalar - 65536;
        char high = (char)((adjusted >> 10) + 0xD800);
        char low = (char)((adjusted & 1023) + 0xDC00);
        this.ValidateByteRange(byteStart, byteEnd);
        this.EnsureCharacterCapacity(this.characterCount + 2);
        int start = this.characterCount;
        this.characters[start] = high;
        this.characters[start + 1] = low;
        this.characterByteOffsets[start] = byteStart;
        this.characterByteOffsets[start + 1] = byteStart;
        this.characterCount = start + 2;
        this.characterByteOffsets[this.characterCount] = byteEnd;
        this.MapBytes(byteStart, byteEnd, start, this.characterCount);
        this.nextByteOffset = byteEnd;
    }

    internal void AppendError(int byteStart, int byteEnd)
    {
        this.AppendCharacter('\uFFFD', byteStart, byteEnd);
        this.AppendErrorRecord(byteStart, byteEnd);
    }

    internal void AppendTwoCharacters(char first, char second, int byteStart, int byteEnd)
    {
        this.ValidateByteRange(byteStart, byteEnd);
        this.EnsureCharacterCapacity(this.characterCount + 2);
        int start = this.characterCount;
        this.characters[start] = first;
        this.characters[start + 1] = second;
        this.characterByteOffsets[start] = byteStart;
        this.characterByteOffsets[start + 1] = byteStart;
        this.characterCount = start + 2;
        this.characterByteOffsets[this.characterCount] = byteEnd;
        this.MapBytes(byteStart, byteEnd, start, this.characterCount);
        this.nextByteOffset = byteEnd;
    }

    internal void AppendCharacters(ReadOnlySpan<char> values, int byteStart, int byteEnd)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one decoded character is required.", "values");
        }

        this.ValidateByteRange(byteStart, byteEnd);
        this.EnsureCharacterCapacity(this.characterCount + values.Length);
        int start = this.characterCount;
        int index = 0;
        while (index < values.Length)
        {
            this.characters[start + index] = values[index];
            this.characterByteOffsets[start + index] = byteStart;
            index = index + 1;
        }

        this.characterCount = start + values.Length;
        this.characterByteOffsets[this.characterCount] = byteEnd;
        this.MapBytes(byteStart, byteEnd, start, this.characterCount);
        this.nextByteOffset = byteEnd;
    }

    internal void AddError(int byteStart, int byteEnd)
    {
        if (byteStart < 0 || byteEnd <= byteStart || byteEnd > this.byteCount)
        {
            throw new ArgumentOutOfRangeException("byteStart", byteStart, "A decoding error requires a valid nonempty byte range.");
        }

        this.AppendErrorRecord(byteStart, byteEnd);
    }

    internal int GetNextByteOffset()
    {
        return this.nextByteOffset;
    }

    internal void SkipBytes(int byteStart, int byteEnd)
    {
        this.ValidateByteRange(byteStart, byteEnd);
        int offset = byteStart;
        while (offset < byteEnd)
        {
            this.byteCharacterOffsets[offset] = this.characterCount;
            offset = offset + 1;
        }

        this.byteCharacterOffsets[byteEnd] = this.characterCount;
        this.characterByteOffsets[this.characterCount] = byteEnd;
        this.nextByteOffset = byteEnd;
    }

    internal HtmlDecodedInput Finish()
    {
        if (this.nextByteOffset != this.byteCount)
        {
            throw new InvalidOperationException("The decoder did not consume the complete byte input.");
        }

        char[] finalCharacters = new char[this.characterCount];
        Array.Copy(this.characters, finalCharacters, this.characterCount);
        int[] finalCharacterOffsets = new int[this.characterCount + 1];
        Array.Copy(this.characterByteOffsets, finalCharacterOffsets, this.characterCount + 1);
        HtmlDecodingError[] finalErrors = new HtmlDecodingError[this.errorCount];
        Array.Copy(this.errors, finalErrors, this.errorCount);
        return new HtmlDecodedInput(true, this.encodingName, new string(finalCharacters), this.byteCount,
            finalCharacterOffsets, this.byteCharacterOffsets, finalErrors);
    }

    internal void AppendCharacter(char value, int byteStart, int byteEnd)
    {
        this.ValidateByteRange(byteStart, byteEnd);
        this.EnsureCharacterCapacity(this.characterCount + 1);
        int start = this.characterCount;
        this.characters[start] = value;
        this.characterByteOffsets[start] = byteStart;
        this.characterCount = start + 1;
        this.characterByteOffsets[this.characterCount] = byteEnd;
        this.MapBytes(byteStart, byteEnd, start, this.characterCount);
        this.nextByteOffset = byteEnd;
    }

    internal void EnsureCharacterCapacity(int required)
    {
        if (required <= this.characters.Length)
        {
            return;
        }

        if (required < 0 || required == int.MaxValue)
        {
            throw new InsufficientMemoryException(
                "The character-to-byte map is too large.");
        }

        int capacity = BufferCapacity.GetNext(this.characters.Length,
            required, int.MaxValue - 1);
        char[] expandedCharacters = new char[capacity];
        int[] expandedOffsets = new int[capacity + 1];
        Array.Copy(this.characters, expandedCharacters, this.characterCount);
        Array.Copy(this.characterByteOffsets, expandedOffsets, this.characterCount + 1);
        this.characters = expandedCharacters;
        this.characterByteOffsets = expandedOffsets;
    }

    internal void AppendErrorRecord(int byteStart, int byteEnd)
    {
        if (this.errorCount == this.errors.Length)
        {
            if (this.errors.Length == int.MaxValue)
            {
                throw new InsufficientMemoryException(
                    "The decoding-error list cannot represent another error.");
            }

            int capacity = BufferCapacity.GetNext(this.errors.Length,
                this.errors.Length + 1, int.MaxValue);
            HtmlDecodingError[] expanded = new HtmlDecodingError[capacity];
            Array.Copy(this.errors, expanded, this.errorCount);
            this.errors = expanded;
        }

        this.errors[this.errorCount] = new HtmlDecodingError(byteStart, byteEnd - byteStart);
        this.errorCount = this.errorCount + 1;
    }

    internal void ValidateByteRange(int byteStart, int byteEnd)
    {
        if (byteStart != this.nextByteOffset || byteEnd <= byteStart || byteEnd > this.byteCount)
        {
            throw new InvalidOperationException("The decoder produced a discontinuous byte mapping.");
        }
    }

    internal void MapBytes(int byteStart, int byteEnd, int characterStart, int characterEnd)
    {
        int offset = byteStart;
        while (offset < byteEnd)
        {
            this.byteCharacterOffsets[offset] = characterStart;
            offset = offset + 1;
        }

        this.byteCharacterOffsets[byteEnd] = characterEnd;
    }
}

/// <summary>Decodes supported HTML byte encodings while retaining byte-to-character mappings and errors.</summary>
internal static class HtmlByteDecoder
{
    internal static readonly int[] windows1252 =
    [
        0x20AC, 0, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
        0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0, 0x017D, 0,
        0, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
        0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0, 0x017E, 0x0178,
    ];

    /// <summary>Decodes the supplied input using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <returns>The decoded text, encoding information, byte mapping, and decoding errors.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static HtmlDecodedInput Decode(ReadOnlySpan<byte> input)
    {
        return Decode(input, HtmlEncodingDetector.Detect(input));
    }

    /// <summary>Decodes the supplied input using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="encodingLabel">The encoding label to resolve.</param>
    /// <returns>The decoded text, encoding information, byte mapping, and decoding errors.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlDecodedInput Decode(ReadOnlySpan<byte> input, string encodingLabel)
    {
        if (encodingLabel == null)
        {
            throw new ArgumentNullException("encodingLabel");
        }

        HtmlEncodingDetectionResult detected = HtmlEncodingDetector.Detect(input, encodingLabel);
        if (detected.GetSource() == HtmlEncodingSource.Bom)
        {
            return Decode(input, detected);
        }

        string encodingName = HtmlEncodingDetector.GetEncodingName(encodingLabel);
        if (encodingName.Length == 0)
        {
            return Unsupported(input.Length, "");
        }

        return DecodeName(input, encodingName, 0);
    }

    /// <summary>Decodes the supplied input using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="detection">The prior encoding-detection result that selects the decoder.</param>
    /// <returns>The decoded text, encoding information, byte mapping, and decoding errors.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public static HtmlDecodedInput Decode(ReadOnlySpan<byte> input, HtmlEncodingDetectionResult detection)
    {
        if (detection == null)
        {
            throw new ArgumentNullException("detection");
        }

        int start = detection.GetByteOrderMarkLength();
        if (start < 0 || start > input.Length)
        {
            throw new ArgumentException("The detected byte order mark length exceeds the input.", "detection");
        }

        return DecodeName(input, detection.GetEncodingName(), start);
    }

    internal static HtmlDecodedInput DecodeName(ReadOnlySpan<byte> input, string encodingName, int start)
    {
        if (encodingName == "UTF-8")
        {
            return DecodeUtf8(input, start);
        }

        if (encodingName == "UTF-16LE")
        {
            return DecodeUtf16(input, start, false);
        }

        if (encodingName == "UTF-16BE")
        {
            return DecodeUtf16(input, start, true);
        }

        if (encodingName == "windows-1252")
        {
            return DecodeWindows1252(input, start);
        }

        return HtmlLegacyByteDecoder.Decode(input, encodingName, start);
    }

    internal static HtmlDecodedInput DecodeUtf8(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("UTF-8", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte first = input[position];
            if (first <= 0x7F)
            {
                builder.AppendScalar(first, position, position + 1);
                position = position + 1;
                continue;
            }

            int needed;
            int scalar;
            int lower = 0x80;
            int upper = 0xBF;
            if (first >= 0xC2 && first <= 0xDF)
            {
                needed = 1;
                scalar = first & 0x1F;
            }
            else if (first >= 0xE0 && first <= 0xEF)
            {
                needed = 2;
                scalar = first & 0x0F;
                if (first == 0xE0)
                {
                    lower = 0xA0;
                }
                else if (first == 0xED)
                {
                    upper = 0x9F;
                }
            }
            else if (first >= 0xF0 && first <= 0xF4)
            {
                needed = 3;
                scalar = first & 0x07;
                if (first == 0xF0)
                {
                    lower = 0x90;
                }
                else if (first == 0xF4)
                {
                    upper = 0x8F;
                }
            }
            else
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
                continue;
            }

            int consumed = 1;
            bool failed = false;
            while (consumed <= needed)
            {
                int continuationOffset = position + consumed;
                if (continuationOffset >= input.Length)
                {
                    builder.AppendError(position, input.Length);
                    position = input.Length;
                    failed = true;
                    break;
                }

                byte continuation = input[continuationOffset];
                if (continuation < lower || continuation > upper)
                {
                    builder.AppendError(position, continuationOffset);
                    position = continuationOffset;
                    failed = true;
                    break;
                }

                scalar = (scalar << 6) | (continuation & 0x3F);
                consumed = consumed + 1;
                lower = 0x80;
                upper = 0xBF;
            }

            if (!failed)
            {
                int end = position + consumed;
                builder.AppendScalar(scalar, position, end);
                position = end;
            }
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeUtf16(ReadOnlySpan<byte> input, int start, bool bigEndian)
    {
        string encodingName = bigEndian ? "UTF-16BE" : "UTF-16LE";
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder(encodingName, input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            if (position + 1 >= input.Length)
            {
                builder.AppendError(position, input.Length);
                position = input.Length;
                continue;
            }

            int unit = GetUtf16Unit(input, position, bigEndian);
            if (unit >= 0xD800 && unit <= 0xDBFF)
            {
                if (position + 3 >= input.Length)
                {
                    builder.AppendError(position, input.Length);
                    position = input.Length;
                    continue;
                }

                int trailing = GetUtf16Unit(input, position + 2, bigEndian);
                if (trailing >= 0xDC00 && trailing <= 0xDFFF)
                {
                    int scalar = 0x10000 + ((unit - 0xD800) << 10) + trailing - 0xDC00;
                    builder.AppendScalar(scalar, position, position + 4);
                    position = position + 4;
                    continue;
                }

                builder.AppendError(position, position + 2);
                position = position + 2;
                continue;
            }

            if (unit >= 0xDC00 && unit <= 0xDFFF)
            {
                builder.AppendError(position, position + 2);
            }
            else
            {
                builder.AppendScalar(unit, position, position + 2);
            }

            position = position + 2;
        }

        return builder.Finish();
    }

    internal static int GetUtf16Unit(ReadOnlySpan<byte> input, int position, bool bigEndian)
    {
        if (bigEndian)
        {
            return (input[position] << 8) | input[position + 1];
        }

        return input[position] | (input[position + 1] << 8);
    }

    internal static HtmlDecodedInput DecodeWindows1252(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("windows-1252", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte value = input[position];
            if (value >= 0x80 && value <= 0x9F)
            {
                int scalar = windows1252[value - 0x80];
                if (scalar == 0)
                {
                    builder.AppendError(position, position + 1);
                }
                else
                {
                    builder.AppendScalar(scalar, position, position + 1);
                }
            }
            else
            {
                builder.AppendScalar(value, position, position + 1);
            }

            position = position + 1;
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput Unsupported(int byteCount, string encodingName)
    {
        if (byteCount == int.MaxValue)
        {
            throw new InsufficientMemoryException("The byte-to-character map is too large.");
        }

        return new HtmlDecodedInput(false, encodingName, "", byteCount, new int[] { 0 },
            new int[byteCount + 1], new HtmlDecodingError[0]);
    }
}
