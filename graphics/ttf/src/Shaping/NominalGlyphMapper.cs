namespace ttf;

internal class ShapingMappingResult
{
    FontDiagnostic[] diagnostics;
    int[] missingCodePoints;
    int[] missingClusterStarts;
    int[] missingClusterLengths;
    int consumedVariationSelectorCount;
    int defaultIgnorableCount;

    internal ShapingMappingResult(
        FontDiagnostic[] diagnostics,
        int[] missingCodePoints,
        int[] missingClusterStarts,
        int[] missingClusterLengths,
        int consumedVariationSelectorCount,
        int defaultIgnorableCount)
    {
        this.diagnostics = diagnostics;
        this.missingCodePoints = missingCodePoints;
        this.missingClusterStarts = missingClusterStarts;
        this.missingClusterLengths = missingClusterLengths;
        this.consumedVariationSelectorCount =
            consumedVariationSelectorCount;
        this.defaultIgnorableCount = defaultIgnorableCount;
    }

    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal FontDiagnostic GetDiagnostic(int index)
    {
        ValidateMissingIndex(index);
        return diagnostics[index];
    }

    internal int GetMissingGlyphCount()
    {
        return missingCodePoints.Length;
    }

    internal int GetMissingCodePoint(int index)
    {
        ValidateMissingIndex(index);
        return missingCodePoints[index];
    }

    internal int GetMissingClusterStart(int index)
    {
        ValidateMissingIndex(index);
        return missingClusterStarts[index];
    }

    internal int GetMissingClusterLength(int index)
    {
        ValidateMissingIndex(index);
        return missingClusterLengths[index];
    }

    internal int GetConsumedVariationSelectorCount()
    {
        return consumedVariationSelectorCount;
    }

    internal int GetDefaultIgnorableCount()
    {
        return defaultIgnorableCount;
    }

    void ValidateMissingIndex(int index)
    {
        if (index < 0 || index >= missingCodePoints.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal static class NominalGlyphMapper
{
    const int CmapTag = 0x636D6170;
    const string DiagnosticLimitMessage =
        "Glyph mapping exceeds the configured diagnostic limit.";

    internal static ShapingMappingResult Map(
        ShapingBuffer buffer,
        SfntCmapMapping? mapping,
        UnicodeData unicodeData,
        int maximumDiagnosticCount)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (mapping == null)
        {
            throw new System.ArgumentNullException("mapping");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (maximumDiagnosticCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumDiagnosticCount");
        }

        ValidateDiagnosticCount(
            buffer,
            mapping,
            unicodeData,
            maximumDiagnosticCount);

        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        System.Collections.Generic.List<int> missingCodePoints =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> missingClusterStarts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> missingClusterLengths =
            new System.Collections.Generic.List<int>();
        bool[] consumedVariationSelectors =
            new bool[buffer.GetCount()];
        int consumedVariationSelectorCount = 0;
        int defaultIgnorableCount = 0;
        int index = 0;

        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            bool variationSelector = HasBinaryProperty(
                unicodeData,
                UnicodeDataTags.VariationSelector,
                codePoint);
            bool defaultIgnorable = HasBinaryProperty(
                unicodeData,
                UnicodeDataTags.DefaultIgnorable,
                codePoint);
            bool layoutControl = IsLayoutControl(codePoint);
            buffer.SetDefaultIgnorable(
                index,
                defaultIgnorable || layoutControl);

            if (defaultIgnorable)
            {
                defaultIgnorableCount++;
            }

            if (variationSelector)
            {
                buffer.SetGlyphId(index, 0);
                index++;
                continue;
            }

            int glyphId = layoutControl ? 0 :
                mapping.MapUnicode(codePoint);
            bool consumedNext = false;
            if (!layoutControl && index + 1 < buffer.GetCount() &&
                HasBinaryProperty(
                    unicodeData,
                    UnicodeDataTags.VariationSelector,
                    buffer.GetScalarValue(index + 1)))
            {
                int selector = buffer.GetScalarValue(index + 1);
                int variationGlyphId = mapping.MapVariation(
                    codePoint,
                    selector);
                if (variationGlyphId != 0)
                {
                    glyphId = variationGlyphId;
                }

                if (HasBinaryProperty(
                    unicodeData,
                    UnicodeDataTags.DefaultIgnorable,
                    selector))
                {
                    defaultIgnorableCount++;
                }

                buffer.MergeClusters(index, index + 2);
                consumedVariationSelectors[index + 1] = true;
                consumedVariationSelectorCount++;
                consumedNext = true;
            }

            buffer.SetGlyphId(index, glyphId);
            if (glyphId == 0 && !defaultIgnorable && !layoutControl)
            {
                AddMissingGlyph(
                    diagnostics,
                    missingCodePoints,
                    missingClusterStarts,
                    missingClusterLengths,
                    buffer,
                    index,
                    codePoint,
                    maximumDiagnosticCount);
            }

            index += consumedNext ? 2 : 1;
        }

        if (consumedVariationSelectorCount != 0)
        {
            buffer.RemoveItems(consumedVariationSelectors);
        }

        return new ShapingMappingResult(
            diagnostics.ToArray(),
            missingCodePoints.ToArray(),
            missingClusterStarts.ToArray(),
            missingClusterLengths.ToArray(),
            consumedVariationSelectorCount,
            defaultIgnorableCount);
    }

    static void AddMissingGlyph(
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        System.Collections.Generic.List<int> missingCodePoints,
        System.Collections.Generic.List<int> missingClusterStarts,
        System.Collections.Generic.List<int> missingClusterLengths,
        ShapingBuffer buffer,
        int index,
        int codePoint,
        int maximumDiagnosticCount)
    {
        if (diagnostics.Count == maximumDiagnosticCount)
        {
            throw FontLimitException.Create(DiagnosticLimitMessage);
        }

        int clusterStart = buffer.GetClusterStart(index);
        int clusterLength = buffer.GetClusterLength(index);
        string code = codePoint.ToString(
            "X4",
            System.Globalization.CultureInfo.InvariantCulture);
        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.MissingGlyph,
            "No glyph mapping is available for U+" + code + ".",
            -1,
            CmapTag,
            -1,
            "Shaping.Cmap[" + clusterStart + "]"));
        missingCodePoints.Add(codePoint);
        missingClusterStarts.Add(clusterStart);
        missingClusterLengths.Add(clusterLength);
    }

    static void ValidateDiagnosticCount(
        ShapingBuffer buffer,
        SfntCmapMapping mapping,
        UnicodeData unicodeData,
        int maximumDiagnosticCount)
    {
        int diagnosticCount = 0;
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            if (HasBinaryProperty(
                unicodeData,
                UnicodeDataTags.VariationSelector,
                codePoint))
            {
                index++;
                continue;
            }

            bool defaultIgnorable = HasBinaryProperty(
                unicodeData,
                UnicodeDataTags.DefaultIgnorable,
                codePoint);
            bool layoutControl = IsLayoutControl(codePoint);
            int glyphId = layoutControl ? 0 : mapping.MapUnicode(codePoint);
            bool hasSelector = !layoutControl &&
                index + 1 < buffer.GetCount() &&
                HasBinaryProperty(
                    unicodeData,
                    UnicodeDataTags.VariationSelector,
                    buffer.GetScalarValue(index + 1));
            if (hasSelector)
            {
                int variationGlyphId = mapping.MapVariation(
                    codePoint,
                    buffer.GetScalarValue(index + 1));
                if (variationGlyphId != 0)
                {
                    glyphId = variationGlyphId;
                }
            }

            if (glyphId == 0 && !defaultIgnorable && !layoutControl)
            {
                diagnosticCount++;
                if (diagnosticCount > maximumDiagnosticCount)
                {
                    throw FontLimitException.Create(DiagnosticLimitMessage);
                }
            }

            index += hasSelector ? 2 : 1;
        }
    }

    static bool HasBinaryProperty(
        UnicodeData unicodeData,
        int propertyTag,
        int codePoint)
    {
        return unicodeData.GetPropertyName(propertyTag, codePoint) == "Y";
    }

    static bool IsLayoutControl(int codePoint)
    {
        return codePoint == 0x0009 ||
            codePoint == 0x000A ||
            codePoint == 0x000B ||
            codePoint == 0x000C ||
            codePoint == 0x000D ||
            codePoint == 0x0085 ||
            codePoint == 0x00AD ||
            codePoint == 0x2028 ||
            codePoint == 0x2029 ||
            codePoint == 0x2060 ||
            codePoint == 0xFEFF;
    }
}
