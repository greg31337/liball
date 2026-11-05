namespace ttf;

/// <summary>Writes deterministic fixed-width PDF ToUnicode CMaps.</summary>
public class PdfToUnicodeCMapWriter : IPdfToUnicodeCMapWriter
{
    const int MaximumBlockEntries = 100;

    /// <summary>Creates a deterministic ToUnicode CMap writer.</summary>
    public PdfToUnicodeCMapWriter()
    {
    }

    /// <summary>Builds a complete ToUnicode CMap from explicit mappings.</summary>
    /// <param name="codeByteCount">One for simple codes or two for Type 0 codes.</param>
    /// <param name="mappings">Unique code-to-Unicode mappings in any order.</param>
    /// <returns>The immutable CMap stream artifact.</returns>
    public PdfToUnicodeCMap BuildCMap(
        int codeByteCount,
        PdfToUnicodeMapping[] mappings)
    {
        return Build(codeByteCount, mappings);
    }

    /// <summary>Builds a complete ToUnicode CMap from explicit mappings.</summary>
    /// <param name="codeByteCount">One for simple codes or two for Type 0 codes.</param>
    /// <param name="mappings">Unique code-to-Unicode mappings in any order.</param>
    /// <returns>The immutable CMap stream artifact.</returns>
    /// <exception cref="System.ArgumentNullException"><c>mappings</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The code width is unsupported or a code exceeds its codespace.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// A mapping is null or two mappings use the same character code.
    /// </exception>
    internal static PdfToUnicodeCMap Build(
        int codeByteCount,
        PdfToUnicodeMapping[] mappings)
    {
        if (mappings == null)
        {
            throw new System.ArgumentNullException("mappings");
        }

        if (codeByteCount != 1 && codeByteCount != 2)
        {
            throw new System.ArgumentOutOfRangeException("codeByteCount");
        }

        PdfToUnicodeMapping[] ordered = CopyAndValidateMappings(
            mappings,
            codeByteCount);
        System.Collections.Generic.List<PdfToUnicodeMapping> characters =
            new System.Collections.Generic.List<PdfToUnicodeMapping>();
        System.Collections.Generic.List<PdfToUnicodeRange> ranges =
            new System.Collections.Generic.List<PdfToUnicodeRange>();
        ClassifyMappings(ordered, characters, ranges);
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        WriteHeader(builder, codeByteCount);
        int bfCharBlockCount = WriteCharacterBlocks(
            builder,
            characters,
            codeByteCount);
        int bfRangeBlockCount = WriteRangeBlocks(
            builder,
            ranges,
            codeByteCount);
        WriteTrailer(builder);
        return new PdfToUnicodeCMap(
            builder.ToString(),
            codeByteCount,
            ordered.Length,
            bfCharBlockCount,
            bfRangeBlockCount);
    }

    static PdfToUnicodeMapping[] CopyAndValidateMappings(
        PdfToUnicodeMapping[] mappings,
        int codeByteCount)
    {
        PdfToUnicodeMapping[] copy =
            new PdfToUnicodeMapping[mappings.Length];
        int maximumCode = codeByteCount == 1 ?
            byte.MaxValue : ushort.MaxValue;
        int index = 0;

        while (index < mappings.Length)
        {
            PdfToUnicodeMapping mapping = mappings[index];

            if (mapping == null)
            {
                throw new System.ArgumentException(
                    "A ToUnicode mapping array contains null.",
                    "mappings");
            }

            if (mapping.GetCharacterCode() > maximumCode)
            {
                throw new System.ArgumentOutOfRangeException(
                    "mappings");
            }

            copy[index] = mapping;
            index += 1;
        }

        System.Array.Sort(
            copy,
            (left, right) => left.GetCharacterCode().CompareTo(
                right.GetCharacterCode()));
        index = 1;

        while (index < copy.Length)
        {
            if (copy[index - 1].GetCharacterCode() ==
                copy[index].GetCharacterCode())
            {
                throw new System.ArgumentException(
                    "A ToUnicode CMap cannot contain duplicate character codes.",
                    "mappings");
            }

            index += 1;
        }

        return copy;
    }

    static void ClassifyMappings(
        PdfToUnicodeMapping[] ordered,
        System.Collections.Generic.List<PdfToUnicodeMapping> characters,
        System.Collections.Generic.List<PdfToUnicodeRange> ranges)
    {
        int start = 0;

        while (start < ordered.Length)
        {
            int end = start;

            while (end + 1 < ordered.Length &&
                end - start + 1 < MaximumBlockEntries &&
                ordered[end + 1].GetCharacterCode() ==
                    ordered[end].GetCharacterCode() + 1)
            {
                end += 1;
            }

            int count = end - start + 1;

            if (count == 1)
            {
                characters.Add(ordered[start]);
            }
            else
            {
                PdfToUnicodeMapping[] rangeMappings =
                    new PdfToUnicodeMapping[count];
                System.Array.Copy(
                    ordered,
                    start,
                    rangeMappings,
                    0,
                    count);
                ranges.Add(new PdfToUnicodeRange(rangeMappings));
            }

            start = end + 1;
        }
    }

    static void WriteHeader(
        System.Text.StringBuilder builder,
        int codeByteCount)
    {
        builder.Append("/CIDInit /ProcSet findresource begin\n");
        builder.Append("12 dict begin\n");
        builder.Append("begincmap\n");
        builder.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
        builder.Append("/CMapName /Adobe-Identity-UCS def\n");
        builder.Append("/CMapType 2 def\n");
        builder.Append("1 begincodespacerange\n");
        builder.Append('<');
        AppendCode(builder, 0, codeByteCount);
        builder.Append("> <");
        AppendCode(
            builder,
            codeByteCount == 1 ? byte.MaxValue : ushort.MaxValue,
            codeByteCount);
        builder.Append(">\n");
        builder.Append("endcodespacerange\n");
    }

    static int WriteCharacterBlocks(
        System.Text.StringBuilder builder,
        System.Collections.Generic.List<PdfToUnicodeMapping> mappings,
        int codeByteCount)
    {
        int blockCount = 0;
        int start = 0;

        while (start < mappings.Count)
        {
            int count = System.Math.Min(
                MaximumBlockEntries,
                mappings.Count - start);
            AppendInteger(builder, count);
            builder.Append(" beginbfchar\n");
            int index = 0;

            while (index < count)
            {
                WriteCharacter(
                    builder,
                    mappings[start + index],
                    codeByteCount);
                index += 1;
            }

            builder.Append("endbfchar\n");
            blockCount += 1;
            start += count;
        }

        return blockCount;
    }

    static int WriteRangeBlocks(
        System.Text.StringBuilder builder,
        System.Collections.Generic.List<PdfToUnicodeRange> ranges,
        int codeByteCount)
    {
        int blockCount = 0;
        int start = 0;

        while (start < ranges.Count)
        {
            int count = System.Math.Min(
                MaximumBlockEntries,
                ranges.Count - start);
            AppendInteger(builder, count);
            builder.Append(" beginbfrange\n");
            int index = 0;

            while (index < count)
            {
                WriteRange(
                    builder,
                    ranges[start + index],
                    codeByteCount);
                index += 1;
            }

            builder.Append("endbfrange\n");
            blockCount += 1;
            start += count;
        }

        return blockCount;
    }

    static void WriteCharacter(
        System.Text.StringBuilder builder,
        PdfToUnicodeMapping mapping,
        int codeByteCount)
    {
        builder.Append('<');
        AppendCode(builder, mapping.GetCharacterCode(), codeByteCount);
        builder.Append("> <");
        AppendUnicodeSequence(builder, mapping);
        builder.Append(">\n");
    }

    static void WriteRange(
        System.Text.StringBuilder builder,
        PdfToUnicodeRange range,
        int codeByteCount)
    {
        PdfToUnicodeMapping first = range.GetMapping(0);
        PdfToUnicodeMapping last = range.GetMapping(range.GetCount() - 1);
        builder.Append('<');
        AppendCode(builder, first.GetCharacterCode(), codeByteCount);
        builder.Append("> <");
        AppendCode(builder, last.GetCharacterCode(), codeByteCount);

        if (IsSequentialBmpRange(range))
        {
            builder.Append("> <");
            AppendUnicodeSequence(builder, first);
            builder.Append(">\n");
            return;
        }

        builder.Append("> [");
        int index = 0;

        while (index < range.GetCount())
        {
            if (index != 0) { builder.Append(' '); }
            builder.Append('<');
            AppendUnicodeSequence(builder, range.GetMapping(index));
            builder.Append('>');
            index += 1;
        }

        builder.Append("]\n");
    }

    static bool IsSequentialBmpRange(PdfToUnicodeRange range)
    {
        PdfToUnicodeMapping first = range.GetMapping(0);

        if (first.GetUnicodeScalarCount() != 1 ||
            first.GetUnicodeScalar(0) > 0xFFFF)
        {
            return false;
        }

        int firstScalar = first.GetUnicodeScalar(0);
        int index = 1;

        while (index < range.GetCount())
        {
            PdfToUnicodeMapping mapping = range.GetMapping(index);

            if (mapping.GetUnicodeScalarCount() != 1 ||
                mapping.GetUnicodeScalar(0) > 0xFFFF ||
                mapping.GetUnicodeScalar(0) != firstScalar + index)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static void AppendCode(
        System.Text.StringBuilder builder,
        int code,
        int codeByteCount)
    {
        if (codeByteCount == 2)
        {
            AppendHexByte(builder, code >> 8);
        }

        AppendHexByte(builder, code);
    }

    static void AppendUnicodeSequence(
        System.Text.StringBuilder builder,
        PdfToUnicodeMapping mapping)
    {
        int index = 0;

        while (index < mapping.GetUnicodeScalarCount())
        {
            int scalar = mapping.GetUnicodeScalar(index);

            if (scalar <= 0xFFFF)
            {
                AppendHexUInt16(builder, scalar);
            }
            else
            {
                int value = scalar - 0x10000;
                AppendHexUInt16(builder, 0xD800 + (value >> 10));
                AppendHexUInt16(builder, 0xDC00 + (value & 0x3FF));
            }

            index += 1;
        }
    }

    static void AppendHexUInt16(
        System.Text.StringBuilder builder,
        int value)
    {
        AppendHexByte(builder, value >> 8);
        AppendHexByte(builder, value);
    }

    static void AppendHexByte(
        System.Text.StringBuilder builder,
        int value)
    {
        int byteValue = value & 0xFF;
        builder.Append(HexDigit(byteValue >> 4));
        builder.Append(HexDigit(byteValue & 15));
    }

    static char HexDigit(int value)
    {
        return value < 10 ?
            (char)('0' + value) :
            (char)('A' + value - 10);
    }

    static void AppendInteger(
        System.Text.StringBuilder builder,
        int value)
    {
        builder.Append(value.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
    }

    static void WriteTrailer(System.Text.StringBuilder builder)
    {
        builder.Append("endcmap\n");
        builder.Append("CMapName currentdict /CMap defineresource pop\n");
        builder.Append("end\n");
        builder.Append("end\n");
    }
}
