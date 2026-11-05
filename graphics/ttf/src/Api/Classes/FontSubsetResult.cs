namespace ttf;

/// <summary>Contains a generated font subset and all of its output reports.</summary>
public class FontSubsetResult : IFontSubsetResult
{
    byte[] sfntBytes;
    string subsetName;
    int[] oldToNewGlyphIds;
    int[] newToOldGlyphIds;
    FontSubsetUnicodeMap[] unicodeMappings;
    FontSubsetTableReport tableReport;
    FontDiagnostic[] repairs;
    FontDiagnostic[] diagnostics;
    FontSubsetEmbeddingDecision embeddingDecision;
    FontSubsetClosureReport closureReport;
    FontInstance sourceInstance;
    FontSubsetOptions effectiveOptions;
    FontOutlineKind outputOutlineKind;
    bool hadVariationData;
    bool variationFrozen;
    FontSubsetPrefixReport prefixReport;

    internal FontSubsetResult(
        byte[] sfntBytes,
        string subsetName,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        FontSubsetUnicodeMap[] unicodeMappings,
        FontSubsetTableReport tableReport,
        FontDiagnostic[] repairs,
        FontDiagnostic[] diagnostics,
        FontSubsetEmbeddingDecision embeddingDecision,
        FontSubsetClosureReport closureReport,
        FontInstance sourceInstance,
        FontSubsetOptions effectiveOptions,
        FontOutlineKind outputOutlineKind,
        bool hadVariationData,
        bool variationFrozen,
        FontSubsetPrefixReport prefixReport)
    {
        this.sfntBytes = CopyBytes(sfntBytes);
        this.subsetName = subsetName;
        this.oldToNewGlyphIds = CopyIntegers(oldToNewGlyphIds);
        this.newToOldGlyphIds = CopyIntegers(newToOldGlyphIds);
        this.unicodeMappings = CopyMappings(unicodeMappings);
        this.tableReport = new FontSubsetTableReport(
            tableReport.GetRetainedTableTags(),
            tableReport.GetDroppedTableTags(),
            tableReport.GetRewrittenTableTags(),
            tableReport.GetPreservedTableTags());
        this.repairs = CopyDiagnostics(repairs);
        this.diagnostics = CopyDiagnostics(diagnostics);
        this.embeddingDecision = embeddingDecision;
        this.closureReport = closureReport;
        this.sourceInstance = sourceInstance;
        this.effectiveOptions = CopyOptions(effectiveOptions);
        this.outputOutlineKind = outputOutlineKind;
        this.hadVariationData = hadVariationData;
        this.variationFrozen = variationFrozen;
        this.prefixReport = prefixReport;
    }

    /// <summary>Copies the generated standalone font bytes.</summary>
    /// <returns>A new array containing the generated font.</returns>
    public byte[] CopyFontBytes() { return CopyBytes(sfntBytes); }

    /// <summary>Copies the generated standalone SFNT bytes.</summary>
    /// <returns>A new array containing the generated font.</returns>
    public byte[] CopySfntBytes() { return CopyFontBytes(); }

    /// <summary>Gets the PostScript-compatible name associated with the output.</summary>
    /// <returns>The deterministic subset or full-font name.</returns>
    public string GetSubsetName() { return subsetName; }

    /// <summary>Copies the source-to-output glyph identifier map.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    public int[] GetOldToNewGlyphMap()
    {
        return CopyIntegers(oldToNewGlyphIds);
    }

    /// <summary>Copies the output-to-source glyph identifier map.</summary>
    /// <returns>A new array indexed by output glyph identifier.</returns>
    public int[] GetNewToOldGlyphMap()
    {
        return CopyIntegers(newToOldGlyphIds);
    }

    /// <summary>Copies all exact Unicode-to-glyph mappings in deterministic order.</summary>
    /// <returns>A new array of immutable mapping values.</returns>
    public FontSubsetUnicodeMap[] GetUnicodeMappings()
    {
        return CopyMappings(unicodeMappings);
    }

    /// <summary>Gets the immutable emitted, rewritten, preserved, and dropped table report.</summary>
    /// <returns>A defensive copy of the table report.</returns>
    public FontSubsetTableReport GetTableReport()
    {
        return new FontSubsetTableReport(
            tableReport.GetRetainedTableTags(),
            tableReport.GetDroppedTableTags(),
            tableReport.GetRewrittenTableTags(),
            tableReport.GetPreservedTableTags());
    }

    /// <summary>Copies compatible-mode repair findings inherited by the output.</summary>
    /// <returns>A new array containing repair diagnostics.</returns>
    public FontDiagnostic[] GetRepairs() { return CopyDiagnostics(repairs); }

    /// <summary>Copies every diagnostic associated with generation.</summary>
    /// <returns>A new array in deterministic processing order.</returns>
    public FontDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    /// <summary>Gets the embedding-rights decision applied to this output.</summary>
    /// <returns>The immutable embedding decision.</returns>
    public FontSubsetEmbeddingDecision GetEmbeddingDecision()
    {
        return embeddingDecision;
    }

    /// <summary>Gets every dependency closure computed for this output.</summary>
    /// <returns>The immutable closure report using source-font indices.</returns>
    public FontSubsetClosureReport GetClosureReport()
    {
        return closureReport;
    }

    /// <summary>Gets the immutable source font instance.</summary>
    /// <returns>The exact instance supplied to the subset builder.</returns>
    public FontInstance GetSourceInstance() { return sourceInstance; }

    /// <summary>Gets the policies effective after embedding-right resolution.</summary>
    /// <returns>A defensive mutable copy of the effective options.</returns>
    public FontSubsetOptions GetEffectiveOptions()
    {
        return CopyOptions(effectiveOptions);
    }

    /// <summary>Gets the outline technology emitted in the standalone font.</summary>
    /// <returns>The output outline kind.</returns>
    public FontOutlineKind GetOutputOutlineKind() { return outputOutlineKind; }

    /// <summary>Reports whether the source used OpenType variation data.</summary>
    /// <returns>True for a variable source instance.</returns>
    public bool HadVariationData() { return hadVariationData; }

    /// <summary>Reports whether variation values were materialized into static output.</summary>
    /// <returns>True when variation data was frozen and removed.</returns>
    public bool WasVariationFrozen() { return variationFrozen; }

    /// <summary>Gets the subset-prefix outcome and all deterministic inputs.</summary>
    /// <returns>The immutable prefix report.</returns>
    public FontSubsetPrefixReport GetPrefixReport() { return prefixReport; }

    internal int GetFontByteLength() { return sfntBytes.Length; }

    static byte[] CopyBytes(byte[] values)
    {
        byte[] copy = new byte[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static FontSubsetUnicodeMap[] CopyMappings(
        FontSubsetUnicodeMap[] values)
    {
        FontSubsetUnicodeMap[] copy =
            new FontSubsetUnicodeMap[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            FontSubsetUnicodeMap value = values[index];
            copy[index] = new FontSubsetUnicodeMap(
                value.GetOriginalGlyphId(),
                value.GetSubsetGlyphId(),
                value.GetUnicodeSequence());
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
}
