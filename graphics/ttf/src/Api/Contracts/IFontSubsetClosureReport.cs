namespace ttf;

/// <summary>Defines deterministic dependency-closure inspection for a subset.</summary>
public interface IFontSubsetClosureReport
{
    /// <summary>Copies glyphs contributed by the recommended-glyph policy.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    int[] GetRecommendedGlyphIds();
    /// <summary>Reports whether layout retention was requested.</summary>
    /// <returns>True when the layout closure is applicable.</returns>
    bool IsLayoutApplicable();
    /// <summary>Reports whether GDEF is part of the layout closure.</summary>
    /// <returns>True when GDEF is retained.</returns>
    bool RetainsGdef();
    /// <summary>Copies the complete glyph set after layout closure.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    int[] GetLayoutGlyphIds();
    /// <summary>Copies retained GSUB feature indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetGsubFeatureIndices();
    /// <summary>Copies retained GSUB lookup indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetGsubLookupIndices();
    /// <summary>Copies retained GPOS feature indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetGposFeatureIndices();
    /// <summary>Copies retained GPOS lookup indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetGposLookupIndices();
    /// <summary>Copies retained GDEF mark-glyph-set indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetGdefMarkGlyphSetIndices();
    /// <summary>Reports whether color-table retention was requested.</summary>
    /// <returns>True when the color closure is applicable.</returns>
    bool IsColorApplicable();
    /// <summary>Copies the complete glyph set after color closure.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    int[] GetColorGlyphIds();
    /// <summary>Copies retained COLR base-glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetColrBaseGlyphIds();
    /// <summary>Copies retained COLR layer indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetColrLayerIndices();
    /// <summary>Copies retained COLR v1 paint offsets.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetColrPaintOffsets();
    /// <summary>Copies retained SVG document indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetSvgDocumentIndices();
    /// <summary>Copies retained <c>sbix</c> glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetSbixGlyphIds();
    /// <summary>Copies retained CBDT glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetCbdtGlyphIds();
    /// <summary>Copies retained EBDT glyph identifiers.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetEbdtGlyphIds();
    /// <summary>Copies retained CPAL palette indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetPaletteIndices();
    /// <summary>Copies retained CPAL palette-entry indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetPaletteEntryIndices();
    /// <summary>Copies the complete glyph set after composite closure.</summary>
    /// <returns>A new sorted array of source glyph identifiers.</returns>
    int[] GetCompositeGlyphIds();
    /// <summary>Reports whether CFF subroutine closure was computed.</summary>
    /// <returns>True for an applicable CFF closure.</returns>
    bool IsCffApplicable();
    /// <summary>Copies retained CFF font-dictionary indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetCffFontDictionaryIndices();
    /// <summary>Copies retained global CFF subroutine indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetCffGlobalSubroutineIndices();
    /// <summary>Gets the number of local CFF subroutine sets.</summary>
    /// <returns>The local-set count.</returns>
    int GetCffLocalSubroutineSetCount();
    /// <summary>Gets retained local subroutines for one font dictionary.</summary>
    /// <param name="index">The zero-based set index.</param>
    /// <returns>An immutable local-subroutine set.</returns>
    FontSubsetCffLocalSubroutineSet GetCffLocalSubroutineSet(int index);
}
