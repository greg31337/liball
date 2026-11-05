using System.Collections.Generic;

namespace unicode;

/// <summary>
/// Creates and validates PDF ToUnicode character maps from glyph mappings.
/// </summary>
public class UnicodePdfCMap : IUnicodePdfCMap
{
    internal static readonly UnicodePdfCMap Instance = new UnicodePdfCMap();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodePdfCMap()
    {
    }
    /// <summary>
    /// The maximum number of entries emitted in one PDF CMap mapping section.
    /// </summary>
    internal const int MaxCMapSectionEntries = 100;
    /// <summary>
    /// The stable diagnostic rule identifier for empty Map.
    /// </summary>
    internal const string EmptyMapRule = "Pdf.CMap.EmptyMap";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Mapping.
    /// </summary>
    internal const string InvalidMappingRule = "Pdf.CMap.InvalidMapping";
    /// <summary>
    /// The stable diagnostic rule identifier for duplicate Character Code.
    /// </summary>
    internal const string DuplicateCharacterCodeRule = "Pdf.CMap.DuplicateCharacterCode";
    /// <summary>
    /// The stable diagnostic rule identifier for missing Unicode Scalars.
    /// </summary>
    internal const string MissingUnicodeScalarsRule = "Pdf.CMap.MissingUnicodeScalars";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Unicode Scalar.
    /// </summary>
    internal const string InvalidUnicodeScalarRule = "Pdf.CMap.InvalidUnicodeScalar";
    /// <summary>
    /// The stable diagnostic rule identifier for replacement Character.
    /// </summary>
    internal const string ReplacementCharacterRule = "Pdf.CMap.ReplacementCharacter";
    /// <summary>
    /// The stable diagnostic rule identifier for an invalid CMap font name.
    /// </summary>
    internal const string InvalidFontNameRule = "Pdf.CMap.InvalidFontName";

    /// <summary>
    /// Validates, sorts, and serializes a supplied glyph-map snapshot as a complete PDF ToUnicode CMap.
    /// </summary>
    /// <param name="glyphMap">The PDF glyph-map source to create or validate.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>glyphMap</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    public string CreateToUnicodeMap(IUnicodePdfGlyphMap glyphMap)
    {
        System.ArgumentNullException.ThrowIfNull(glyphMap);

        string? fontName = glyphMap.GetFontName();
        UnicodePdfGlyphMapping[]? suppliedMappings = glyphMap.GetMappings();
        UnicodePdfCMapValidationResult validationResult = ValidateToUnicodeSnapshot(fontName, suppliedMappings);
        if (!validationResult.IsValid)
        {
            throw new System.ArgumentException(
                "Cannot create ToUnicode CMap from invalid glyph map: " + validationResult.Diagnostics[0].Message,
                "glyphMap");
        }

        System.ArgumentException.ThrowIfNullOrWhiteSpace(fontName);
        System.ArgumentNullException.ThrowIfNull(suppliedMappings);
        UnicodePdfGlyphMapping[] mappings = SortMappingsByCharacterCode(suppliedMappings);
        UnicodePdfGlyphRange[] ranges = CreateSequentialRanges(mappings);
        UnicodePdfGlyphMapping[] bfCharMappings = CreateBfCharMappings(mappings);
        int characterCodeHexWidth = GetCharacterCodeHexWidth(mappings);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        AppendCMapHeader(builder, fontName, characterCodeHexWidth);
        AppendBfRangeSection(builder, ranges, characterCodeHexWidth);
        AppendBfCharSection(builder, bfCharMappings, characterCodeHexWidth);
        AppendCMapFooter(builder);
        return builder.ToString();
    }

    internal static void AppendCMapHeader(
        System.Text.StringBuilder builder,
        string fontName,
        int characterCodeHexWidth)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        System.ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        builder.Append("/CIDInit /ProcSet findresource begin\n");
        builder.Append("12 dict begin\n");
        builder.Append("begincmap\n");
        builder.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
        builder.Append("/CMapName /");
        AppendPdfNameText(builder, fontName);
        builder.Append("-ToUnicode def\n");
        builder.Append("/CMapType 2 def\n");
        AppendCodeSpaceRange(builder, characterCodeHexWidth);
    }

    internal static void AppendCMapFooter(System.Text.StringBuilder builder)
    {
        System.ArgumentNullException.ThrowIfNull(builder);

        builder.Append("endcmap\n");
        builder.Append("CMapName currentdict /CMap defineresource pop\n");
        builder.Append("end\n");
        builder.Append("end\n");
    }

    internal static void AppendCodeSpaceRange(System.Text.StringBuilder builder, int characterCodeHexWidth)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        ValidateCharacterCodeHexWidth(characterCodeHexWidth);

        builder.Append("1 begincodespacerange\n");
        builder.Append("<");
        AppendHexValue(builder, 0, characterCodeHexWidth);
        builder.Append("> <");
        AppendMaxHexValue(builder, characterCodeHexWidth);
        builder.Append(">\n");
        builder.Append("endcodespacerange\n");
    }

    internal static void AppendBfRangeSection(
        System.Text.StringBuilder builder,
        UnicodePdfGlyphRange[] ranges,
        int characterCodeHexWidth)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        System.ArgumentNullException.ThrowIfNull(ranges);
        ValidateCharacterCodeHexWidth(characterCodeHexWidth);

        int index = 0;
        while (index < ranges.Length)
        {
            int entryCount = GetCMapBatchEntryCount(ranges.Length, index);
            builder.Append(entryCount);
            builder.Append(" beginbfrange\n");

            int end = index + entryCount;
            while (index < end)
            {
                if (ranges[index] == null)
                {
                    throw new System.ArgumentNullException("ranges");
                }

                builder.Append(FormatCharacterCode(ranges[index].StartCharacterCode, characterCodeHexWidth));
                builder.Append(" ");
                builder.Append(FormatCharacterCode(ranges[index].EndCharacterCode, characterCodeHexWidth));
                builder.Append(" ");
                builder.Append(FormatUnicodeString(new int[] { ranges[index].StartUnicodeCodePoint }));
                builder.Append("\n");
                index = index + 1;
            }

            builder.Append("endbfrange\n");
        }
    }

    internal static void AppendBfCharSection(
        System.Text.StringBuilder builder,
        UnicodePdfGlyphMapping[] mappings,
        int characterCodeHexWidth)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        System.ArgumentNullException.ThrowIfNull(mappings);
        ValidateCharacterCodeHexWidth(characterCodeHexWidth);

        int index = 0;
        while (index < mappings.Length)
        {
            int entryCount = GetCMapBatchEntryCount(mappings.Length, index);
            builder.Append(entryCount);
            builder.Append(" beginbfchar\n");

            int end = index + entryCount;
            while (index < end)
            {
                if (mappings[index] == null)
                {
                    throw new System.ArgumentNullException("mappings");
                }

                builder.Append(FormatCharacterCode(mappings[index].CharacterCode, characterCodeHexWidth));
                builder.Append(" ");
                builder.Append(FormatUnicodeString(mappings[index].UnicodeCodePoints));
                builder.Append("\n");
                index = index + 1;
            }

            builder.Append("endbfchar\n");
        }
    }

    internal static UnicodePdfGlyphMapping[] SortMappingsByCharacterCode(UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        UnicodePdfGlyphMapping[] sorted = UnicodePdfGlyphMap.CopyMappings(mappings);
        int index = 1;
        while (index < sorted.Length)
        {
            if (sorted[index] == null)
            {
                throw new System.ArgumentNullException("mappings");
            }

            UnicodePdfGlyphMapping current = sorted[index];
            int scanIndex = index - 1;
            while (scanIndex >= 0)
            {
                if (sorted[scanIndex] == null)
                {
                    throw new System.ArgumentNullException("mappings");
                }

                if (sorted[scanIndex].CharacterCode <= current.CharacterCode)
                {
                    break;
                }

                sorted[scanIndex + 1] = sorted[scanIndex];
                scanIndex = scanIndex - 1;
            }

            sorted[scanIndex + 1] = current;
            index = index + 1;
        }

        return sorted;
    }

    internal static UnicodePdfGlyphRange[] CreateSequentialRanges(UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        UnicodePdfGlyphMapping[] sorted = SortMappingsByCharacterCode(mappings);
        List<UnicodePdfGlyphRange> ranges = new List<UnicodePdfGlyphRange>();
        int index = 0;
        while (index < sorted.Length)
        {
            int rangeEnd = FindSequentialBmpRangeEnd(sorted, index);
            if (rangeEnd > index)
            {
                ranges.Add(new UnicodePdfGlyphRange(
                    sorted[index].CharacterCode,
                    sorted[rangeEnd].CharacterCode,
                    sorted[index].UnicodeCodePoints[0]));
                index = rangeEnd + 1;
            }
            else
            {
                index = index + 1;
            }
        }

        return ranges.ToArray();
    }

    internal static UnicodePdfGlyphMapping[] CreateBfCharMappings(UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        UnicodePdfGlyphMapping[] sorted = SortMappingsByCharacterCode(mappings);
        List<UnicodePdfGlyphMapping> bfCharMappings = new List<UnicodePdfGlyphMapping>();
        int index = 0;
        while (index < sorted.Length)
        {
            int rangeEnd = FindSequentialBmpRangeEnd(sorted, index);
            if (rangeEnd > index)
            {
                index = rangeEnd + 1;
            }
            else
            {
                bfCharMappings.Add(sorted[index]);
                index = index + 1;
            }
        }

        return bfCharMappings.ToArray();
    }

    internal static int FindSequentialBmpRangeEnd(UnicodePdfGlyphMapping[] mappings, int startIndex)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        if (startIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex >= mappings.Length)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }

        if (!CanUseInSequentialBmpRange(mappings[startIndex]))
        {
            return startIndex;
        }

        int index = startIndex + 1;
        int rangeEnd = startIndex;
        while (index < mappings.Length)
        {
            if (!CanExtendSequentialBmpRange(mappings[rangeEnd], mappings[index]))
            {
                break;
            }

            rangeEnd = index;
            index = index + 1;
        }

        return rangeEnd;
    }

    internal static bool CanExtendSequentialBmpRange(
        UnicodePdfGlyphMapping previous,
        UnicodePdfGlyphMapping current)
    {
        if (!CanUseInSequentialBmpRange(previous))
        {
            return false;
        }

        if (!CanUseInSequentialBmpRange(current))
        {
            return false;
        }

        if (previous.CharacterCode == int.MaxValue)
        {
            return false;
        }

        if (current.CharacterCode != previous.CharacterCode + 1)
        {
            return false;
        }

        return current.UnicodeCodePoints[0] == previous.UnicodeCodePoints[0] + 1;
    }

    internal static bool CanUseInSequentialBmpRange(UnicodePdfGlyphMapping? mapping)
    {
        if (mapping == null)
        {
            return false;
        }

        if (!mapping.MapsToSingleScalar())
        {
            return false;
        }

        int codePoint = mapping.UnicodeCodePoints[0];
        if (!UnicodeScalar.Instance.IsValid(codePoint))
        {
            return false;
        }

        return codePoint <= 0xFFFF;
    }

    internal static int GetCharacterCodeHexWidth(UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        int maxCharacterCode = 0;
        int index = 0;
        while (index < mappings.Length)
        {
            if (mappings[index] == null)
            {
                throw new System.ArgumentNullException("mappings");
            }

            if (mappings[index].CharacterCode < 0)
            {
                throw new System.ArgumentOutOfRangeException("mappings");
            }

            if (mappings[index].CharacterCode > maxCharacterCode)
            {
                maxCharacterCode = mappings[index].CharacterCode;
            }

            index = index + 1;
        }

        if (maxCharacterCode <= 0xFF)
        {
            return 2;
        }

        if (maxCharacterCode <= 0xFFFF)
        {
            return 4;
        }

        if (maxCharacterCode <= 0xFFFFFF)
        {
            return 6;
        }

        return 8;
    }

    internal static string FormatCharacterCode(int characterCode, int characterCodeHexWidth)
    {
        ValidateCharacterCodeHexWidth(characterCodeHexWidth);

        if (characterCode < 0)
        {
            throw new System.ArgumentOutOfRangeException("characterCode");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(characterCodeHexWidth + 2);
        builder.Append("<");
        AppendHexValue(builder, characterCode, characterCodeHexWidth);
        builder.Append(">");
        return builder.ToString();
    }

    internal static string FormatUnicodeString(int[] codePoints)
    {
        UnicodePdfGlyphMapping.ValidateUnicodeCodePoints(codePoints);

        System.Text.StringBuilder textBuilder = new System.Text.StringBuilder(codePoints.Length);
        int index = 0;
        while (index < codePoints.Length)
        {
            textBuilder.Append(UnicodeScalar.Instance.ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return "<" + UnicodePdfText.Instance.ToHexString(UnicodePdfEncoding.EncodeUtf16BePayload(textBuilder.ToString())) + ">";
    }

    internal static void AppendPdfNameText(System.Text.StringBuilder builder, string text)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        System.ArgumentNullException.ThrowIfNull(text);

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(UnicodeScalarReader.Instance.ReplaceInvalid(text));
        int index = 0;
        while (index < bytes.Length)
        {
            if (IsRegularPdfNameByte(bytes[index]))
            {
                builder.Append((char)bytes[index]);
            }
            else
            {
                builder.Append("#");
                builder.Append(UnicodePdfText.ToHexDigit(bytes[index] >> 4));
                builder.Append(UnicodePdfText.ToHexDigit(bytes[index] & 0x0F));
            }

            index = index + 1;
        }
    }

    internal static bool IsRegularPdfNameByte(int value)
    {
        if (value <= 0x20)
        {
            return false;
        }

        if (value >= 0x7F)
        {
            return false;
        }

        if (value == 0x23)
        {
            return false;
        }

        if (value == 0x25)
        {
            return false;
        }

        if (value == 0x28)
        {
            return false;
        }

        if (value == 0x29)
        {
            return false;
        }

        if (value == 0x2F)
        {
            return false;
        }

        if (value == 0x3C)
        {
            return false;
        }

        if (value == 0x3E)
        {
            return false;
        }

        if (value == 0x5B)
        {
            return false;
        }

        if (value == 0x5D)
        {
            return false;
        }

        if (value == 0x7B)
        {
            return false;
        }

        if (value == 0x7D)
        {
            return false;
        }

        return true;
    }

    internal static int GetCMapBatchEntryCount(int totalCount, int startIndex)
    {
        if (totalCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("totalCount");
        }

        if (startIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex >= totalCount)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }

        int remaining = totalCount - startIndex;
        if (remaining > MaxCMapSectionEntries)
        {
            return MaxCMapSectionEntries;
        }

        return remaining;
    }

    internal static void AppendHexValue(System.Text.StringBuilder builder, int value, int hexWidth)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        ValidateCharacterCodeHexWidth(hexWidth);

        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (hexWidth < 8)
        {
            int maxValue = (1 << (hexWidth * 4)) - 1;
            if (value > maxValue)
            {
                throw new System.ArgumentOutOfRangeException("value");
            }
        }

        int shift = (hexWidth - 1) * 4;
        while (shift >= 0)
        {
            builder.Append(UnicodePdfText.ToHexDigit((value >> shift) & 0x0F));
            shift = shift - 4;
        }
    }

    internal static void AppendMaxHexValue(System.Text.StringBuilder builder, int hexWidth)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        ValidateCharacterCodeHexWidth(hexWidth);

        int index = 0;
        while (index < hexWidth)
        {
            builder.Append("F");
            index = index + 1;
        }
    }

    internal static void ValidateCharacterCodeHexWidth(int hexWidth)
    {
        if (hexWidth == 2)
        {
            return;
        }

        if (hexWidth == 4)
        {
            return;
        }

        if (hexWidth == 6)
        {
            return;
        }

        if (hexWidth == 8)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("hexWidth");
    }

    /// <summary>
    /// Checks a supplied glyph-map snapshot for empty, duplicate, invalid, missing, or replacement Unicode values.
    /// </summary>
    /// <param name="glyphMap">The PDF glyph-map source to validate.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>glyphMap</c> is <c>null</c>.</exception>
    public UnicodePdfCMapValidationResult ValidateToUnicodeMap(IUnicodePdfGlyphMap glyphMap)
    {
        System.ArgumentNullException.ThrowIfNull(glyphMap);

        return ValidateToUnicodeSnapshot(glyphMap.GetFontName(), glyphMap.GetMappings());
    }

    internal static UnicodePdfCMapValidationResult ValidateToUnicodeSnapshot(
        string? fontName,
        UnicodePdfGlyphMapping[]? mappings)
    {
        List<UnicodePdfCMapDiagnostic> diagnostics = new List<UnicodePdfCMapDiagnostic>();
        if (System.String.IsNullOrWhiteSpace(fontName))
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                -1,
                -1,
                -1,
                UnicodePdfCMapStatus.InvalidFontName,
                InvalidFontNameRule,
                "ToUnicode CMap font name must not be null, empty, or white space."));
        }

        AddMappingSetDiagnostics(mappings, diagnostics);
        return CreateValidationResult(diagnostics);
    }

    internal static UnicodePdfCMapValidationResult ValidateToUnicodeMappings(UnicodePdfGlyphMapping[]? mappings)
    {
        List<UnicodePdfCMapDiagnostic> diagnostics = new List<UnicodePdfCMapDiagnostic>();
        AddMappingSetDiagnostics(mappings, diagnostics);
        return CreateValidationResult(diagnostics);
    }

    internal static void AddMappingSetDiagnostics(
        UnicodePdfGlyphMapping[]? mappings,
        List<UnicodePdfCMapDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (mappings == null)
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                -1,
                -1,
                -1,
                UnicodePdfCMapStatus.InvalidMapping,
                InvalidMappingRule,
                "Glyph map mappings must not be null."));
            return;
        }

        if (mappings.Length == 0)
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                -1,
                -1,
                -1,
                UnicodePdfCMapStatus.EmptyMap,
                EmptyMapRule,
                "ToUnicode map must contain at least one glyph mapping."));
        }

        int index = 0;
        while (index < mappings.Length)
        {
            AddMappingDiagnostics(mappings[index], index, diagnostics);
            AddDuplicateCharacterCodeDiagnostics(mappings, index, diagnostics);
            index = index + 1;
        }
    }

    internal static void AddMappingDiagnostics(
        UnicodePdfGlyphMapping? mapping,
        int mappingIndex,
        List<UnicodePdfCMapDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (mappingIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("mappingIndex");
        }

        if (mapping == null)
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                mappingIndex,
                -1,
                -1,
                UnicodePdfCMapStatus.InvalidMapping,
                InvalidMappingRule,
                "Glyph mapping entry must not be null."));
            return;
        }

        if (mapping.CharacterCode < 0)
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                mappingIndex,
                mapping.CharacterCode,
                -1,
                UnicodePdfCMapStatus.InvalidMapping,
                InvalidMappingRule,
                "Glyph mapping character code must be non-negative."));
        }

        if (mapping.UnicodeCodePoints == null)
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                mappingIndex,
                mapping.CharacterCode,
                -1,
                UnicodePdfCMapStatus.MissingUnicodeScalars,
                MissingUnicodeScalarsRule,
                "Glyph mapping must contain Unicode scalar values."));
            return;
        }

        if (mapping.UnicodeCodePoints.Length == 0)
        {
            diagnostics.Add(new UnicodePdfCMapDiagnostic(
                mappingIndex,
                mapping.CharacterCode,
                -1,
                UnicodePdfCMapStatus.MissingUnicodeScalars,
                MissingUnicodeScalarsRule,
                "Glyph mapping must contain at least one Unicode scalar value."));
            return;
        }

        int index = 0;
        while (index < mapping.UnicodeCodePoints.Length)
        {
            int codePoint = mapping.UnicodeCodePoints[index];
            if (!UnicodeScalar.Instance.IsValid(codePoint))
            {
                diagnostics.Add(new UnicodePdfCMapDiagnostic(
                    mappingIndex,
                    mapping.CharacterCode,
                    codePoint,
                    UnicodePdfCMapStatus.InvalidUnicodeScalar,
                    InvalidUnicodeScalarRule,
                    "Glyph mapping contains an invalid Unicode scalar value."));
            }
            else
            {
                if (codePoint == UnicodeScalar.ReplacementCharacter)
                {
                    diagnostics.Add(new UnicodePdfCMapDiagnostic(
                        mappingIndex,
                        mapping.CharacterCode,
                        codePoint,
                        UnicodePdfCMapStatus.ReplacementCharacter,
                        ReplacementCharacterRule,
                        "Glyph mapping should not map text extraction to the replacement character."));
                }
            }

            index = index + 1;
        }
    }

    internal static void AddDuplicateCharacterCodeDiagnostics(
        UnicodePdfGlyphMapping[] mappings,
        int mappingIndex,
        List<UnicodePdfCMapDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (mappingIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("mappingIndex");
        }

        if (mappingIndex >= mappings.Length)
        {
            throw new System.ArgumentOutOfRangeException("mappingIndex");
        }

        if (mappings[mappingIndex] == null)
        {
            return;
        }

        int index = 0;
        while (index < mappingIndex)
        {
            if (mappings[index] != null)
            {
                if (mappings[index].CharacterCode == mappings[mappingIndex].CharacterCode)
                {
                    diagnostics.Add(new UnicodePdfCMapDiagnostic(
                        mappingIndex,
                        mappings[mappingIndex].CharacterCode,
                        -1,
                        UnicodePdfCMapStatus.DuplicateCharacterCode,
                        DuplicateCharacterCodeRule,
                        "Glyph mapping character code duplicates the mapping at index " + index.ToString() + "."));
                    return;
                }
            }

            index = index + 1;
        }
    }

    internal static UnicodePdfCMapValidationResult CreateValidationResult(List<UnicodePdfCMapDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodePdfCMapStatus status = GetValidationStatus(diagnostics);
        return new UnicodePdfCMapValidationResult(
            diagnostics.Count == 0,
            status,
            diagnostics.ToArray());
    }

    internal static UnicodePdfCMapStatus GetValidationStatus(List<UnicodePdfCMapDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (diagnostics.Count == 0)
        {
            return UnicodePdfCMapStatus.Valid;
        }

        return diagnostics[0].Status;
    }
}
