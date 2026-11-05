namespace ttf;

/// <summary>Exposes every deterministic dependency closure of a font subset.</summary>
public class FontSubsetClosureReport : IFontSubsetClosureReport
{
    int[] recommendedGlyphIds;
    bool layoutApplicable;
    bool retainsGdef;
    int[] layoutGlyphIds;
    int[] gsubFeatureIndices;
    int[] gsubLookupIndices;
    int[] gposFeatureIndices;
    int[] gposLookupIndices;
    int[] gdefMarkGlyphSetIndices;
    bool colorApplicable;
    int[] colorGlyphIds;
    int[] colrBaseGlyphIds;
    int[] colrLayerIndices;
    int[] colrPaintOffsets;
    int[] svgDocumentIndices;
    int[] sbixGlyphIds;
    int[] cbdtGlyphIds;
    int[] ebdtGlyphIds;
    int[] paletteIndices;
    int[] paletteEntryIndices;
    int[] compositeGlyphIds;
    bool cffApplicable;
    int[] cffFontDictionaryIndices;
    int[] cffGlobalSubroutineIndices;
    FontSubsetCffLocalSubroutineSet[] cffLocalSubroutines;

    internal FontSubsetClosureReport(
        int[] recommendedGlyphIds,
        FontSubsetLayoutClosure layoutClosure,
        FontSubsetColorClosure colorClosure,
        FontSubsetCompositeClosure compositeClosure,
        FontSubsetCffClosure cffClosure)
    {
        this.recommendedGlyphIds = CopyIntegers(recommendedGlyphIds);
        layoutApplicable = layoutClosure.IsApplicable();
        retainsGdef = layoutClosure.RetainsGdef();
        layoutGlyphIds = layoutClosure.CopyGlyphIds();
        gsubFeatureIndices = layoutClosure.CopyGsubFeatureIndices();
        gsubLookupIndices = layoutClosure.CopyGsubLookupIndices();
        gposFeatureIndices = layoutClosure.CopyGposFeatureIndices();
        gposLookupIndices = layoutClosure.CopyGposLookupIndices();
        gdefMarkGlyphSetIndices = layoutClosure.CopyGdefMarkGlyphSetIndices();
        colorApplicable = colorClosure.IsApplicable();
        colorGlyphIds = colorClosure.CopyGlyphIds();
        colrBaseGlyphIds = colorClosure.CopyColrBaseGlyphIds();
        colrLayerIndices = colorClosure.CopyColrLayerIndices();
        colrPaintOffsets = colorClosure.CopyColrPaintOffsets();
        svgDocumentIndices = colorClosure.CopySvgDocumentIndices();
        sbixGlyphIds = colorClosure.CopySbixGlyphIds();
        cbdtGlyphIds = colorClosure.CopyCbdtGlyphIds();
        ebdtGlyphIds = colorClosure.CopyEbdtGlyphIds();
        paletteIndices = colorClosure.CopyPaletteIndices();
        paletteEntryIndices = colorClosure.CopyPaletteEntryIndices();
        compositeGlyphIds = compositeClosure.CopyGlyphIds();
        cffApplicable = cffClosure.IsApplicable();
        cffFontDictionaryIndices = cffClosure.CopyFontDictionaryIndices();
        cffGlobalSubroutineIndices = cffClosure.CopyGlobalSubroutineIndices();
        cffLocalSubroutines = ConvertLocalSubroutines(
            cffClosure.CopyLocalSubroutines());
    }

    /// <summary>Copies glyphs contributed by the recommended-glyph policy.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    public int[] GetRecommendedGlyphIds()
    {
        return CopyIntegers(recommendedGlyphIds);
    }

    /// <summary>Reports whether layout retention was requested.</summary>
    /// <returns>True when the layout closure is applicable.</returns>
    public bool IsLayoutApplicable() { return layoutApplicable; }

    /// <summary>Reports whether GDEF is part of the layout closure.</summary>
    /// <returns>True when GDEF is retained.</returns>
    public bool RetainsGdef() { return retainsGdef; }

    /// <summary>Copies the complete glyph set after layout closure.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    public int[] GetLayoutGlyphIds() { return CopyIntegers(layoutGlyphIds); }

    /// <summary>Copies retained GSUB feature indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetGsubFeatureIndices()
    {
        return CopyIntegers(gsubFeatureIndices);
    }

    /// <summary>Copies retained GSUB lookup indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetGsubLookupIndices()
    {
        return CopyIntegers(gsubLookupIndices);
    }

    /// <summary>Copies retained GPOS feature indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetGposFeatureIndices()
    {
        return CopyIntegers(gposFeatureIndices);
    }

    /// <summary>Copies retained GPOS lookup indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetGposLookupIndices()
    {
        return CopyIntegers(gposLookupIndices);
    }

    /// <summary>Copies retained GDEF mark-glyph-set indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetGdefMarkGlyphSetIndices()
    {
        return CopyIntegers(gdefMarkGlyphSetIndices);
    }

    /// <summary>Reports whether color-table retention was requested.</summary>
    /// <returns>True when the color closure is applicable.</returns>
    public bool IsColorApplicable() { return colorApplicable; }

    /// <summary>Copies the complete glyph set after color closure.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    public int[] GetColorGlyphIds() { return CopyIntegers(colorGlyphIds); }

    /// <summary>Copies retained COLR base-glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetColrBaseGlyphIds()
    {
        return CopyIntegers(colrBaseGlyphIds);
    }

    /// <summary>Copies retained COLR layer indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetColrLayerIndices() { return CopyIntegers(colrLayerIndices); }

    /// <summary>Copies retained COLR v1 paint offsets.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetColrPaintOffsets() { return CopyIntegers(colrPaintOffsets); }

    /// <summary>Copies retained SVG document indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetSvgDocumentIndices()
    {
        return CopyIntegers(svgDocumentIndices);
    }

    /// <summary>Copies retained <c>sbix</c> glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetSbixGlyphIds() { return CopyIntegers(sbixGlyphIds); }

    /// <summary>Copies retained CBDT glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetCbdtGlyphIds() { return CopyIntegers(cbdtGlyphIds); }

    /// <summary>Copies retained EBDT glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetEbdtGlyphIds() { return CopyIntegers(ebdtGlyphIds); }

    /// <summary>Copies retained CPAL palette indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetPaletteIndices() { return CopyIntegers(paletteIndices); }

    /// <summary>Copies retained CPAL palette-entry indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetPaletteEntryIndices()
    {
        return CopyIntegers(paletteEntryIndices);
    }

    /// <summary>Copies the complete glyph set after composite closure.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    public int[] GetCompositeGlyphIds()
    {
        return CopyIntegers(compositeGlyphIds);
    }

    /// <summary>Reports whether CFF subroutine closure was computed.</summary>
    /// <returns>True for an applicable CFF closure.</returns>
    public bool IsCffApplicable() { return cffApplicable; }

    /// <summary>Copies retained CFF font-dictionary indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetCffFontDictionaryIndices()
    {
        return CopyIntegers(cffFontDictionaryIndices);
    }

    /// <summary>Copies retained global CFF subroutine indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetCffGlobalSubroutineIndices()
    {
        return CopyIntegers(cffGlobalSubroutineIndices);
    }

    /// <summary>Gets the number of local CFF subroutine sets.</summary>
    /// <returns>The local-set count.</returns>
    public int GetCffLocalSubroutineSetCount()
    {
        return cffLocalSubroutines.Length;
    }

    /// <summary>Gets retained local subroutines for one font dictionary.</summary>
    /// <param name="index">The zero-based set index.</param>
    /// <returns>An immutable local-subroutine set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public FontSubsetCffLocalSubroutineSet GetCffLocalSubroutineSet(int index)
    {
        if (index < 0 || index >= cffLocalSubroutines.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        FontSubsetCffLocalSubroutineSet value = cffLocalSubroutines[index];
        return new FontSubsetCffLocalSubroutineSet(
            value.GetFontDictionaryIndex(),
            value.GetSubroutineIndices());
    }

    static FontSubsetCffLocalSubroutineSet[] ConvertLocalSubroutines(
        FontSubsetCffLocalSubroutineClosure[] values)
    {
        FontSubsetCffLocalSubroutineSet[] results =
            new FontSubsetCffLocalSubroutineSet[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            FontSubsetCffLocalSubroutineClosure value = values[index];
            results[index] = new FontSubsetCffLocalSubroutineSet(
                value.GetFontDictionaryIndex(),
                value.CopySubroutineIndices());
            index += 1;
        }

        return results;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
