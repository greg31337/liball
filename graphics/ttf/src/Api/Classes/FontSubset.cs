namespace ttf;

/// <summary>Represents an immutable deterministic font subset request.</summary>
public class FontSubset : IFontSubset
{
    FontInstance instance;
    FontSubsetOptions options;
    int[] codePoints;
    int[] glyphIds;
    int[] recommendedGlyphIds;
    int[] featureTags;
    int[] paletteIndices;
    int[][] unicodeSequences;
    FontSubsetGlyphRequest[] shapedGlyphs;
    FontSubsetUnicodeMapping[] unicodeMappings;
    FontSubsetLayoutClosure layoutClosure;
    FontSubsetColorClosure colorClosure;
    FontSubsetCompositeClosure compositeClosure;
    FontSubsetCffClosure cffClosure;
    FontSubsetGlyphAssignment glyphAssignment;
    FontSubsetEmbeddingDecision embeddingDecision;
    FontDiagnostic[] diagnostics;

    internal FontSubset(
        FontInstance instance,
        FontSubsetOptions options,
        int[] codePoints,
        int[] glyphIds,
        int[] recommendedGlyphIds,
        int[] featureTags,
        int[] paletteIndices,
        int[][] unicodeSequences,
        FontSubsetGlyphRequest[] shapedGlyphs,
        FontSubsetUnicodeMapping[] unicodeMappings,
        FontSubsetLayoutClosure layoutClosure,
        FontSubsetColorClosure colorClosure,
        FontSubsetCompositeClosure compositeClosure,
        FontSubsetCffClosure cffClosure,
        FontSubsetGlyphAssignment glyphAssignment,
        FontSubsetEmbeddingDecision embeddingDecision,
        FontDiagnostic[] diagnostics)
    {
        this.instance = instance;
        this.options = CopyOptions(options);
        this.codePoints = CopyIntegers(codePoints);
        this.glyphIds = CopyIntegers(glyphIds);
        this.recommendedGlyphIds = CopyIntegers(recommendedGlyphIds);
        this.featureTags = CopyIntegers(featureTags);
        this.paletteIndices = CopyIntegers(paletteIndices);
        this.unicodeSequences = CopySequences(unicodeSequences);
        this.shapedGlyphs = CopyShapedGlyphs(shapedGlyphs);
        this.unicodeMappings = CopyUnicodeMappings(unicodeMappings);
        this.layoutClosure = CopyLayoutClosure(layoutClosure);
        this.colorClosure = CopyColorClosure(colorClosure);
        this.compositeClosure = CopyCompositeClosure(compositeClosure);
        this.cffClosure = new FontSubsetCffClosure(
            cffClosure.IsApplicable(),
            cffClosure.CopyFontDictionaryIndices(),
            cffClosure.CopyGlobalSubroutineIndices(),
            cffClosure.CopyLocalSubroutines(),
            cffClosure.CopyDiagnostics());
        this.glyphAssignment = new FontSubsetGlyphAssignment(
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds());
        this.embeddingDecision = embeddingDecision;
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    internal FontInstance GetInstance()
    {
        return instance;
    }

    internal FontSubsetOptions CopyOptions()
    {
        return CopyOptions(options);
    }

    internal int GetCodePointCount()
    {
        return codePoints.Length;
    }

    internal int GetCodePoint(int index)
    {
        return GetValue(codePoints, index);
    }

    internal int GetGlyphCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyph(int index)
    {
        return GetValue(glyphIds, index);
    }

    internal int GetFeatureCount()
    {
        return featureTags.Length;
    }

    internal int GetFeature(int index)
    {
        return GetValue(featureTags, index);
    }

    internal int GetPaletteCount()
    {
        return paletteIndices.Length;
    }

    internal int GetPalette(int index)
    {
        return GetValue(paletteIndices, index);
    }

    internal int GetUnicodeSequenceCount()
    {
        return unicodeSequences.Length;
    }

    internal int[] CopyUnicodeSequence(int index)
    {
        ValidateIndex(index, unicodeSequences.Length);
        return CopyIntegers(unicodeSequences[index]);
    }

    internal int GetShapedGlyphCount()
    {
        return shapedGlyphs.Length;
    }

    internal FontSubsetGlyphRequest GetShapedGlyph(int index)
    {
        ValidateIndex(index, shapedGlyphs.Length);
        FontSubsetGlyphRequest request = shapedGlyphs[index];
        return new FontSubsetGlyphRequest(
            request.GetGlyphId(),
            request.CopyUnicodeSequence());
    }

    internal int GetUnicodeMappingCount()
    {
        return unicodeMappings.Length;
    }

    internal FontSubsetUnicodeMapping GetUnicodeMapping(int index)
    {
        ValidateIndex(index, unicodeMappings.Length);
        FontSubsetUnicodeMapping mapping = unicodeMappings[index];
        return new FontSubsetUnicodeMapping(
            mapping.GetGlyphId(),
            mapping.CopyUnicodeSequence());
    }

    internal bool HasCffClosure()
    {
        return cffClosure.IsApplicable();
    }

    internal bool HasColorClosure()
    {
        return colorClosure.IsApplicable();
    }

    internal int[] CopyColrBaseGlyphIds()
    {
        return colorClosure.CopyColrBaseGlyphIds();
    }

    internal int[] CopyColrLayerIndices()
    {
        return colorClosure.CopyColrLayerIndices();
    }

    internal int[] CopyColrPaintOffsets()
    {
        return colorClosure.CopyColrPaintOffsets();
    }

    internal int[] CopySvgDocumentIndices()
    {
        return colorClosure.CopySvgDocumentIndices();
    }

    internal int[] CopySbixGlyphIds()
    {
        return colorClosure.CopySbixGlyphIds();
    }

    internal int[] CopyCbdtGlyphIds()
    {
        return colorClosure.CopyCbdtGlyphIds();
    }

    internal int[] CopyEbdtGlyphIds()
    {
        return colorClosure.CopyEbdtGlyphIds();
    }

    internal int[] CopyRetainedPaletteIndices()
    {
        return colorClosure.CopyPaletteIndices();
    }

    internal int[] CopyRetainedPaletteEntryIndices()
    {
        return colorClosure.CopyPaletteEntryIndices();
    }

    internal bool HasLayoutClosure()
    {
        return layoutClosure.IsApplicable();
    }

    internal bool RetainsGdef()
    {
        return layoutClosure.RetainsGdef();
    }

    internal int[] CopyGsubFeatureIndices()
    {
        return layoutClosure.CopyGsubFeatureIndices();
    }

    internal int[] CopyGsubLookupIndices()
    {
        return layoutClosure.CopyGsubLookupIndices();
    }

    internal int[] CopyGposFeatureIndices()
    {
        return layoutClosure.CopyGposFeatureIndices();
    }

    internal int[] CopyGposLookupIndices()
    {
        return layoutClosure.CopyGposLookupIndices();
    }

    internal int[] CopyGdefMarkGlyphSetIndices()
    {
        return layoutClosure.CopyGdefMarkGlyphSetIndices();
    }

    internal int[] CopyCffFontDictionaryIndices()
    {
        return cffClosure.CopyFontDictionaryIndices();
    }

    internal int[] CopyCffGlobalSubroutineIndices()
    {
        return cffClosure.CopyGlobalSubroutineIndices();
    }

    internal FontSubsetCffLocalSubroutineClosure[]
        CopyCffLocalSubroutines()
    {
        return cffClosure.CopyLocalSubroutines();
    }

    internal SfntInstancedGlyfData CreateTrueTypeGlyphData()
    {
        return SfntStaticTrueTypeInstancer.CreateSubsetGlyphData(
            instance,
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds(),
            options.GetHintPolicy() == FontSubsetHintPolicy.Retain);
    }

    internal SfntSubsetTrueTypeData CreateTrueTypeTables()
    {
        return SfntStaticTrueTypeInstancer.CreateSubsetTables(
            instance,
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds(),
            options.GetHintPolicy() == FontSubsetHintPolicy.Retain,
            options.GetVerticalPolicy() == FontSubsetVerticalPolicy.Retain);
    }

    internal FontSubsetCffData CreateCffData()
    {
        string outputName = CreateNameData().GetPostScriptName();
        return FontSubsetCffBuilder.Create(
            instance,
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds(),
            new FontSubsetCffClosure(
                cffClosure.IsApplicable(),
                cffClosure.CopyFontDictionaryIndices(),
                cffClosure.CopyGlobalSubroutineIndices(),
                cffClosure.CopyLocalSubroutines(),
                cffClosure.CopyDiagnostics()),
            outputName);
    }

    internal FontSubsetCffData CreatePdfCidCffData(
        int[] charsetIdentifiers)
    {
        string outputName = CreateNameData().GetPostScriptName();
        return FontSubsetCffBuilder.CreatePdfCidFont(
            instance,
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds(),
            new FontSubsetCffClosure(
                cffClosure.IsApplicable(),
                cffClosure.CopyFontDictionaryIndices(),
                cffClosure.CopyGlobalSubroutineIndices(),
                cffClosure.CopyLocalSubroutines(),
                cffClosure.CopyDiagnostics()),
            outputName,
            charsetIdentifiers);
    }

    internal byte[] CreateCmapData()
    {
        return FontSubsetCmapBuilder.Create(
            instance,
            CopyUnicodeMappings(unicodeMappings),
            glyphAssignment.CopyOldToNewGlyphIds(),
            CopyIntegers(codePoints));
    }

    internal FontSubsetNameData CreateNameData()
    {
        return FontSubsetNameBuilder.Create(
            instance,
            glyphAssignment.CopyOldToNewGlyphIds(),
            options);
    }

    internal byte[]? CreateOs2Data()
    {
        return FontSubsetOs2Builder.Create(
            instance,
            CopyUnicodeMappings(unicodeMappings),
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds(),
            CopyIntegers(codePoints));
    }

    internal byte[]? CreatePostData()
    {
        return FontSubsetPostBuilder.Create(
            instance,
            glyphAssignment.CopyNewToOldGlyphIds(),
            options.GetMetadataPolicy());
    }

    internal FontSubsetLayoutData CreateLayoutData()
    {
        return FontSubsetLayoutBuilder.Create(
            instance,
            glyphAssignment.CopyOldToNewGlyphIds(),
            CopyLayoutClosure(layoutClosure));
    }

    internal FontSubsetAuxiliaryTables CreateAuxiliaryTables()
    {
        return FontSubsetAuxiliaryTablesBuilder.Create(
            instance,
            options,
            glyphAssignment.CopyOldToNewGlyphIds(),
            glyphAssignment.CopyNewToOldGlyphIds(),
            CopyColorClosure(colorClosure));
    }

    internal FontSubsetUnknownTables CreateUnknownTables()
    {
        return FontSubsetUnknownTablesBuilder.Create(instance.GetFace(), options);
    }

    internal byte[] CreateFullFontData()
    {
        if (options.GetEmbeddingMode() != FontSubsetEmbeddingMode.FullFont)
        {
            throw new System.InvalidOperationException(
                "Full-font data requires full-font embedding mode.");
        }

        return FontSubsetFullFontWriter.Write(instance);
    }

    /// <summary>
    /// Copies the map from original glyph identifiers to output identifiers.
    /// </summary>
    /// <remarks>
    /// An entry is <c>-1</c> when the original glyph is omitted.
    /// </remarks>
    /// <returns>A new array indexed by original glyph identifier.</returns>
    public int[] GetOldToNewGlyphMap()
    {
        return glyphAssignment.CopyOldToNewGlyphIds();
    }

    /// <summary>
    /// Copies the map from output glyph identifiers to original identifiers.
    /// </summary>
    /// <remarks>
    /// An entry is <c>-1</c> for an unassigned preserve-GID gap.
    /// </remarks>
    /// <returns>A new array indexed by output glyph identifier.</returns>
    public int[] GetNewToOldGlyphMap()
    {
        return glyphAssignment.CopyNewToOldGlyphIds();
    }

    /// <summary>Gets the explicit embedding-rights policy decision.</summary>
    /// <returns>The immutable embedding decision report.</returns>
    public FontSubsetEmbeddingDecision GetEmbeddingDecision()
    {
        return embeddingDecision;
    }

    /// <summary>Gets every computed dependency closure for this subset.</summary>
    /// <returns>An immutable closure report using source-font indices.</returns>
    public FontSubsetClosureReport GetClosureReport()
    {
        return new FontSubsetClosureReport(
            recommendedGlyphIds,
            layoutClosure,
            colorClosure,
            compositeClosure,
            cffClosure);
    }

    /// <summary>Copies this request as a generated standalone SFNT font.</summary>
    /// <returns>A new array containing the subset or complete static font.</returns>
    public byte[] CopyFontBytes()
    {
        return CreateResult().CopyFontBytes();
    }

    /// <summary>Gets the deterministic PostScript-compatible output name.</summary>
    /// <returns>The subset-prefixed name, or the full-font name.</returns>
    public string GetSubsetName()
    {
        return CreateNameData().GetPostScriptName();
    }

    /// <summary>Generates the standalone font and its complete result report.</summary>
    /// <returns>
    /// An immutable result containing bytes, names, maps, table disposition,
    /// repairs, embedding decisions, and diagnostics.
    /// </returns>
    /// <exception cref="FontFormatException">
    /// A required source table or retained structure cannot be rewritten.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// Generated output exceeds a configured resource limit.
    /// </exception>
    public FontSubsetResult CreateResult()
    {
        return FontSubsetResultBuilder.Create(this);
    }

    /// <summary>Copies diagnostics produced while constructing the subset.</summary>
    /// <returns>A new array in deterministic processing order.</returns>
    public FontDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    static int GetValue(int[] values, int index)
    {
        ValidateIndex(index, values.Length);
        return values[index];
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static int[][] CopySequences(int[][] values)
    {
        int[][] copy = new int[values.Length][];
        int index = 0;

        while (index < copy.Length)
        {
            copy[index] = CopyIntegers(values[index]);
            index++;
        }

        return copy;
    }

    static FontSubsetGlyphRequest[] CopyShapedGlyphs(
        FontSubsetGlyphRequest[] values)
    {
        FontSubsetGlyphRequest[] copy =
            new FontSubsetGlyphRequest[values.Length];
        int index = 0;

        while (index < copy.Length)
        {
            FontSubsetGlyphRequest request = values[index];
            copy[index] = new FontSubsetGlyphRequest(
                request.GetGlyphId(),
                request.CopyUnicodeSequence());
            index++;
        }

        return copy;
    }

    static FontSubsetUnicodeMapping[] CopyUnicodeMappings(
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

    static FontSubsetCompositeClosure CopyCompositeClosure(
        FontSubsetCompositeClosure source)
    {
        return new FontSubsetCompositeClosure(
            source.CopyGlyphIds(),
            source.CopyDiagnostics());
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static FontSubsetOptions CopyOptions(FontSubsetOptions source)
    {
        FontSubsetOptions copy = FontSubsetOptions.Create(
            source.GetEmbeddingMode(),
            source.GetGlyphRetentionMode(),
            source.GetRecommendedGlyphPolicy(),
            source.GetHintPolicy(),
            source.GetLayoutPolicy(),
            source.GetVerticalPolicy(),
            source.GetColorPolicy(),
            source.GetMetadataPolicy(),
            source.GetUnknownTablePolicy(),
            source.GetRightsPolicy());
        copy.SetSubsetPrefixOverride(source.GetSubsetPrefixOverride());
        copy.SetOperationOptions(source.GetOperationOptions());
        return copy;
    }

    static void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
