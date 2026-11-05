namespace ttf;

internal class FontSubsetUnicodeMapping
{
    int glyphId;
    int[] unicodeSequence;

    internal FontSubsetUnicodeMapping(int glyphId, int[] unicodeSequence)
    {
        this.glyphId = glyphId;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int[] CopyUnicodeSequence()
    {
        return CopyIntegers(unicodeSequence);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetSeed
{
    int[] glyphIds;
    int[] recommendedGlyphIds;
    FontSubsetUnicodeMapping[] unicodeMappings;
    FontSubsetLayoutClosure layoutClosure;
    FontSubsetColorClosure colorClosure;
    FontSubsetCompositeClosure compositeClosure;
    FontSubsetCffClosure cffClosure;
    FontDiagnostic[] diagnostics;

    internal FontSubsetSeed(
        int[] glyphIds,
        int[] recommendedGlyphIds,
        FontSubsetUnicodeMapping[] unicodeMappings,
        FontSubsetLayoutClosure layoutClosure,
        FontSubsetColorClosure colorClosure,
        FontSubsetCompositeClosure compositeClosure,
        FontSubsetCffClosure cffClosure,
        FontDiagnostic[] diagnostics)
    {
        this.glyphIds = CopyIntegers(glyphIds);
        this.recommendedGlyphIds = CopyIntegers(recommendedGlyphIds);
        this.unicodeMappings = CopyMappings(unicodeMappings);
        this.layoutClosure = CopyLayoutClosure(layoutClosure);
        this.colorClosure = CopyColorClosure(colorClosure);
        this.compositeClosure = CreateCompositeClosureCopy(compositeClosure);
        this.cffClosure = new FontSubsetCffClosure(
            cffClosure.IsApplicable(),
            cffClosure.CopyFontDictionaryIndices(),
            cffClosure.CopyGlobalSubroutineIndices(),
            cffClosure.CopyLocalSubroutines(),
            cffClosure.CopyDiagnostics());
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    internal int[] CopyGlyphIds()
    {
        return CopyIntegers(glyphIds);
    }

    internal int[] CopyRecommendedGlyphIds()
    {
        return CopyIntegers(recommendedGlyphIds);
    }

    internal FontSubsetUnicodeMapping[] CopyUnicodeMappings()
    {
        return CopyMappings(unicodeMappings);
    }

    internal FontDiagnostic[] CopyDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    internal FontSubsetCffClosure CopyCffClosure()
    {
        return new FontSubsetCffClosure(
            cffClosure.IsApplicable(),
            cffClosure.CopyFontDictionaryIndices(),
            cffClosure.CopyGlobalSubroutineIndices(),
            cffClosure.CopyLocalSubroutines(),
            cffClosure.CopyDiagnostics());
    }

    internal FontSubsetLayoutClosure CopyLayoutClosure()
    {
        return CopyLayoutClosure(layoutClosure);
    }

    internal FontSubsetColorClosure CopyColorClosure()
    {
        return CopyColorClosure(colorClosure);
    }

    internal FontSubsetCompositeClosure CopyCompositeClosure()
    {
        return CreateCompositeClosureCopy(compositeClosure);
    }

    static FontSubsetCompositeClosure CreateCompositeClosureCopy(
        FontSubsetCompositeClosure source)
    {
        return new FontSubsetCompositeClosure(
            source.CopyGlyphIds(),
            source.CopyDiagnostics());
    }

    static FontSubsetColorClosure CopyColorClosure(
        FontSubsetColorClosure source)
    {
        return new FontSubsetColorClosure(
            source.IsApplicable(),
            source.CopyGlyphIds(),
            source.CopyColrBaseGlyphIds(),
            source.CopyColrLayerIndices(),
            source.CopyColrPaintOffsets(),
            source.CopySvgDocumentIndices(),
            source.CopySbixGlyphIds(),
            source.CopyCbdtGlyphIds(),
            source.CopyEbdtGlyphIds(),
            source.CopyPaletteIndices(),
            source.CopyPaletteEntryIndices());
    }

    static FontSubsetLayoutClosure CopyLayoutClosure(
        FontSubsetLayoutClosure source)
    {
        return new FontSubsetLayoutClosure(
            source.IsApplicable(),
            source.RetainsGdef(),
            source.CopyGlyphIds(),
            source.CopyGsubFeatureIndices(),
            source.CopyGsubLookupIndices(),
            source.CopyGposFeatureIndices(),
            source.CopyGposLookupIndices(),
            source.CopyGdefMarkGlyphSetIndices());
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static FontSubsetUnicodeMapping[] CopyMappings(
        FontSubsetUnicodeMapping[] values)
    {
        FontSubsetUnicodeMapping[] copy =
            new FontSubsetUnicodeMapping[values.Length];
        int index = 0;

        while (index < copy.Length)
        {
            FontSubsetUnicodeMapping mapping = values[index];
            copy[index] = new FontSubsetUnicodeMapping(
                mapping.GetGlyphId(),
                mapping.CopyUnicodeSequence());
            index++;
        }

        return copy;
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetSeedBuilder
{
    FontInstance instance;
    FontSubsetOptions options;
    SfntCmapMapping? cmap;
    int maximumEntries;
    System.Collections.Generic.List<int> glyphIds;
    System.Collections.Generic.HashSet<int> glyphIdSet;
    System.Collections.Generic.HashSet<int> recommendedGlyphIds;
    System.Collections.Generic.List<FontSubsetUnicodeMapping> unicodeMappings;

    FontSubsetSeedBuilder(
        FontInstance instance,
        FontSubsetOptions options)
    {
        this.instance = instance;
        this.options = options;
        cmap = instance.GetFace().GetCmapMapping();
        maximumEntries = instance.GetFace()
            .GetTableCache()
            .GetLimits()
            .GetMaxCoverageEntries();
        glyphIds = new System.Collections.Generic.List<int>();
        glyphIdSet = new System.Collections.Generic.HashSet<int>();
        recommendedGlyphIds = new System.Collections.Generic.HashSet<int>();
        unicodeMappings =
            new System.Collections.Generic.List<FontSubsetUnicodeMapping>();
    }

    internal static FontSubsetSeed Create(
        FontInstance instance,
        FontSubsetOptions options,
        int[] requestedCodePoints,
        int[] requestedGlyphIds,
        int[] requestedFeatureTags,
        int[] requestedPaletteIndices,
        int[][] unicodeSequences,
        FontSubsetGlyphRequest[] shapedGlyphs)
    {
        FontSubsetSeedBuilder builder =
            new FontSubsetSeedBuilder(instance, options);
        return builder.Build(
            requestedCodePoints,
            requestedGlyphIds,
            requestedFeatureTags,
            requestedPaletteIndices,
            unicodeSequences,
            shapedGlyphs);
    }

    FontSubsetSeed Build(
        int[] requestedCodePoints,
        int[] requestedGlyphIds,
        int[] requestedFeatureTags,
        int[] requestedPaletteIndices,
        int[][] unicodeSequences,
        FontSubsetGlyphRequest[] shapedGlyphs)
    {
        if (instance.GetGlyphCount() == 0)
        {
            throw FontFormatException.Create(
                "A font subset requires glyph zero for .notdef.");
        }

        AddGlyph(0);
        AddRequestedGlyphs(requestedGlyphIds);
        AddRequestedCodePoints(requestedCodePoints);
        AddVariationSequences(unicodeSequences);
        AddShapedGlyphs(shapedGlyphs);

        if (options.GetRecommendedGlyphPolicy() ==
            FontSubsetRecommendedGlyphPolicy.IncludeRecommended)
        {
            AddRecommendedGlyphs();
        }

        int[] initialGlyphIds = glyphIds.ToArray();
        System.Array.Sort(initialGlyphIds);
        FontSubsetLayoutClosure layoutClosure =
            FontSubsetLayoutClosureBuilder.Create(
                instance,
                options,
                initialGlyphIds,
                requestedFeatureTags,
                maximumEntries);
        FontSubsetColorClosure colorClosure =
            FontSubsetColorClosureBuilder.Create(
                instance,
                options,
                layoutClosure.CopyGlyphIds(),
                requestedPaletteIndices,
                maximumEntries);
        FontSubsetCompositeClosure closure =
            FontSubsetCompositeClosureBuilder.Create(
                instance,
                colorClosure.CopyGlyphIds(),
                maximumEntries);
        int[] sortedGlyphIds = closure.CopyGlyphIds();
        FontSubsetCffClosure cffClosure;

        if (instance.GetFace().GetOutlineKind() == FontOutlineKind.Cff2)
        {
            cffClosure = new FontSubsetCffClosure(
                true,
                new int[0],
                new int[0],
                new FontSubsetCffLocalSubroutineClosure[0],
                new FontDiagnostic[0]);
        }
        else
        {
            cffClosure = FontSubsetCffClosureBuilder.Create(
                instance,
                sortedGlyphIds,
                maximumEntries);
        }
        FontSubsetUnicodeMapping[] sortedMappings =
            unicodeMappings.ToArray();
        System.Array.Sort(
            sortedMappings,
            (left, right) => CompareMappings(left, right));
        return new FontSubsetSeed(
            sortedGlyphIds,
            ToSortedArray(recommendedGlyphIds),
            sortedMappings,
            layoutClosure,
            colorClosure,
            closure,
            cffClosure,
            CombineDiagnostics(
                closure.CopyDiagnostics(),
                cffClosure.CopyDiagnostics()));
    }

    static FontDiagnostic[] CombineDiagnostics(
        FontDiagnostic[] first,
        FontDiagnostic[] second)
    {
        FontDiagnostic[] combined =
            new FontDiagnostic[first.Length + second.Length];
        System.Array.Copy(first, combined, first.Length);
        System.Array.Copy(
            second,
            0,
            combined,
            first.Length,
            second.Length);
        return combined;
    }

    void AddRequestedGlyphs(int[] values)
    {
        int index = 0;

        while (index < values.Length)
        {
            AddGlyph(values[index]);
            index++;
        }
    }

    void AddRequestedCodePoints(int[] values)
    {
        int index = 0;

        while (index < values.Length)
        {
            AddNominalMapping(values[index]);
            index++;
        }
    }

    void AddVariationSequences(int[][] sequences)
    {
        if (cmap == null)
        {
            return;
        }

        int sequenceIndex = 0;

        while (sequenceIndex < sequences.Length)
        {
            int[] sequence = sequences[sequenceIndex];
            int scalarIndex = 1;

            while (scalarIndex < sequence.Length)
            {
                int selector = sequence[scalarIndex];

                if (IsVariationSelector(selector))
                {
                    int baseCodePoint = sequence[scalarIndex - 1];
                    int glyphId = cmap.MapVariation(
                        baseCodePoint,
                        selector);

                    if (glyphId != 0)
                    {
                        AddGlyph(glyphId);
                        AddMapping(
                            glyphId,
                            new int[] { baseCodePoint, selector });
                    }
                }

                scalarIndex++;
            }

            sequenceIndex++;
        }
    }

    void AddShapedGlyphs(FontSubsetGlyphRequest[] values)
    {
        int index = 0;

        while (index < values.Length)
        {
            FontSubsetGlyphRequest request = values[index];
            int glyphId = request.GetGlyphId();
            AddGlyph(glyphId);
            AddMapping(glyphId, request.CopyUnicodeSequence());
            index++;
        }
    }

    void AddRecommendedGlyphs()
    {
        int glyphId = 1;
        int limit = instance.GetGlyphCount() < 4 ?
            instance.GetGlyphCount() : 4;

        while (glyphId < limit)
        {
            AddRecommendedGlyph(glyphId);
            glyphId++;
        }

        AddRecommendedMapping(0x0000);
        AddRecommendedMapping(0x000D);
        AddRecommendedMapping(0x0020);
    }

    void AddRecommendedMapping(int codePoint)
    {
        if (cmap == null)
        {
            return;
        }

        int glyphId = cmap.MapUnicode(codePoint);

        if (glyphId != 0)
        {
            AddRecommendedGlyph(glyphId);
            AddMapping(glyphId, new int[] { codePoint });
        }
    }

    void AddRecommendedGlyph(int glyphId)
    {
        AddGlyph(glyphId);

        if (!recommendedGlyphIds.Contains(glyphId))
        {
            EnsureCapacity(
                recommendedGlyphIds.Count,
                "recommended glyph closure");
            recommendedGlyphIds.Add(glyphId);
        }
    }

    void AddNominalMapping(int codePoint)
    {
        if (cmap == null)
        {
            return;
        }

        int glyphId = cmap.MapUnicode(codePoint);

        if (glyphId != 0)
        {
            AddGlyph(glyphId);
            AddMapping(glyphId, new int[] { codePoint });
        }
    }

    void AddGlyph(int glyphId)
    {
        if (!glyphIdSet.Contains(glyphId))
        {
            EnsureCapacity(glyphIds.Count, "glyph seed");
            glyphIdSet.Add(glyphId);
            glyphIds.Add(glyphId);
        }
    }

    void AddMapping(int glyphId, int[] unicodeSequence)
    {
        int index = 0;

        while (index < unicodeMappings.Count)
        {
            FontSubsetUnicodeMapping mapping = unicodeMappings[index];

            if (mapping.GetGlyphId() == glyphId &&
                SequencesEqual(
                    mapping.CopyUnicodeSequence(),
                    unicodeSequence))
            {
                return;
            }

            index++;
        }

        EnsureCapacity(unicodeMappings.Count, "Unicode mapping");
        unicodeMappings.Add(
            new FontSubsetUnicodeMapping(glyphId, unicodeSequence));
    }

    void EnsureCapacity(int currentCount, string kind)
    {
        if (currentCount >= maximumEntries)
        {
            throw FontLimitException.Create(
                "Font subset " + kind +
                " entries exceed the configured limit.");
        }
    }

    static bool IsVariationSelector(int codePoint)
    {
        return (codePoint >= 0xFE00 && codePoint <= 0xFE0F) ||
            (codePoint >= 0xE0100 && codePoint <= 0xE01EF);
    }

    static int CompareMappings(
        FontSubsetUnicodeMapping left,
        FontSubsetUnicodeMapping right)
    {
        int glyphComparison = left.GetGlyphId().CompareTo(right.GetGlyphId());

        if (glyphComparison != 0)
        {
            return glyphComparison;
        }

        return CompareSequences(
            left.CopyUnicodeSequence(),
            right.CopyUnicodeSequence());
    }

    static int[] ToSortedArray(
        System.Collections.Generic.HashSet<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result);
        System.Array.Sort(result);
        return result;
    }

    static int CompareSequences(int[] left, int[] right)
    {
        int length = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;

        while (index < length)
        {
            int comparison = left[index].CompareTo(right[index]);

            if (comparison != 0)
            {
                return comparison;
            }

            index++;
        }

        return left.Length.CompareTo(right.Length);
    }

    static bool SequencesEqual(int[] left, int[] right)
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
}
