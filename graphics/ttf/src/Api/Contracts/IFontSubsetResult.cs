namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubsetResult"/>.</summary>
public interface IFontSubsetResult
{
    /// <summary>Copies the generated standalone font bytes.</summary>
    /// <returns>A new array containing the generated font.</returns>
    byte[] CopyFontBytes();

    /// <summary>Copies the generated standalone SFNT bytes.</summary>
    /// <returns>A new array containing the generated font.</returns>
    byte[] CopySfntBytes();

    /// <summary>Gets the PostScript-compatible name associated with the output.</summary>
    /// <returns>The deterministic subset or full-font name.</returns>
    string GetSubsetName();

    /// <summary>Copies the source-to-output glyph identifier map.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    int[] GetOldToNewGlyphMap();

    /// <summary>Copies the output-to-source glyph identifier map.</summary>
    /// <returns>A new array indexed by output glyph identifier.</returns>
    int[] GetNewToOldGlyphMap();

    /// <summary>Copies all exact Unicode-to-glyph mappings in deterministic order.</summary>
    /// <returns>A new array of immutable mapping values.</returns>
    FontSubsetUnicodeMap[] GetUnicodeMappings();

    /// <summary>Gets the immutable emitted, rewritten, preserved, and dropped table report.</summary>
    /// <returns>A defensive copy of the table report.</returns>
    FontSubsetTableReport GetTableReport();

    /// <summary>Copies compatible-mode repair findings inherited by the output.</summary>
    /// <returns>A new array containing repair diagnostics.</returns>
    FontDiagnostic[] GetRepairs();

    /// <summary>Copies every diagnostic associated with generation.</summary>
    /// <returns>A new array in deterministic processing order.</returns>
    FontDiagnostic[] GetDiagnostics();

    /// <summary>Gets the embedding-rights decision applied to this output.</summary>
    /// <returns>The immutable embedding decision.</returns>
    FontSubsetEmbeddingDecision GetEmbeddingDecision();

    /// <summary>Gets every dependency closure computed for this output.</summary>
    /// <returns>The immutable closure report using source-font indices.</returns>
    FontSubsetClosureReport GetClosureReport();

    /// <summary>Gets the immutable source font instance.</summary>
    /// <returns>The exact instance supplied to the subset builder.</returns>
    FontInstance GetSourceInstance();

    /// <summary>Gets the policies effective after embedding-right resolution.</summary>
    /// <returns>A defensive mutable copy of the effective options.</returns>
    FontSubsetOptions GetEffectiveOptions();

    /// <summary>Gets the outline technology emitted in the standalone font.</summary>
    /// <returns>The output outline kind.</returns>
    FontOutlineKind GetOutputOutlineKind();

    /// <summary>Reports whether the source used OpenType variation data.</summary>
    /// <returns>True for a variable source instance.</returns>
    bool HadVariationData();

    /// <summary>Reports whether variation values were materialized into static output.</summary>
    /// <returns>True when variation data was frozen and removed.</returns>
    bool WasVariationFrozen();

    /// <summary>Gets the subset-prefix outcome and all deterministic inputs.</summary>
    /// <returns>The immutable prefix report.</returns>
    FontSubsetPrefixReport GetPrefixReport();
}
