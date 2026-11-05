namespace ttf;

/// <summary>Contains a TrueType CID font program and its glyph mappings.</summary>
/// <remarks>
/// Character codes select document CIDs. Document CIDs are independent from
/// both source and embedded glyph identifiers, so consumers must use the
/// returned mappings when constructing a CID-to-GID map.
/// </remarks>
public class PdfCidFontType2Artifact : IPdfCidFontType2Artifact
{
    byte[] fontFile2;
    PdfFontResourceNames resourceNames;
    PdfCidAllocation allocation;
    PdfCidMappingArtifact cidMapping;
    PdfToUnicodeMappingArtifact toUnicodeMappings;
    PdfCidWidthsArtifact widths;
    PdfCidVerticalWidthsArtifact? verticalWidths;
    PdfFontDescriptorFlagsReport descriptorFlags;
    PdfFontDescriptorMetrics descriptorMetrics;
    FontSubsetEmbeddingDecision embeddingDecision;

    internal PdfCidFontType2Artifact(
        byte[] fontFile2,
        PdfFontResourceNames resourceNames,
        PdfCidAllocation allocation,
        PdfCidMappingArtifact cidMapping,
        PdfToUnicodeMappingArtifact toUnicodeMappings,
        PdfCidWidthsArtifact widths,
        PdfCidVerticalWidthsArtifact? verticalWidths,
        PdfFontDescriptorFlagsReport descriptorFlags,
        PdfFontDescriptorMetrics descriptorMetrics,
        FontSubsetEmbeddingDecision embeddingDecision)
    {
        this.fontFile2 = CopyBytes(fontFile2);
        this.resourceNames = resourceNames;
        this.allocation = allocation;
        this.cidMapping = cidMapping;
        this.toUnicodeMappings = toUnicodeMappings;
        this.widths = widths;
        this.verticalWidths = verticalWidths;
        this.descriptorFlags = descriptorFlags;
        this.descriptorMetrics = descriptorMetrics;
        this.embeddingDecision = embeddingDecision;
    }

    /// <summary>Copies the standalone TrueType program for a FontFile2 stream.</summary>
    /// <returns>A new array containing the complete stream bytes.</returns>
    public byte[] CopyFontFile2()
    {
        return CopyBytes(fontFile2);
    }

    /// <summary>Gets the PostScript-compatible name of the embedded program.</summary>
    /// <returns>The subset-prefixed or full-font base name.</returns>
    public string GetBaseFontName()
    {
        return resourceNames.GetBaseFontName();
    }

    /// <summary>Gets the shared descriptor, dictionary, and program names.</summary>
    /// <returns>The immutable font resource names.</returns>
    public PdfFontResourceNames GetResourceNames()
    {
        return resourceNames;
    }

    /// <summary>Gets the PDF font kind represented by this artifact.</summary>
    /// <returns>The CIDFontType2 resource kind.</returns>
    public PdfFontKind GetFontKind()
    {
        return PdfFontKind.Type0CidFontType2;
    }

    /// <summary>Gets the number of allocated document CIDs.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    public int GetCidCount()
    {
        return allocation.GetCidCount();
    }

    /// <summary>Gets the source glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The glyph identifier in the source font instance.</returns>
    public int GetSourceGlyphId(int cid)
    {
        return allocation.GetSourceGlyphId(cid);
    }

    /// <summary>Finds the document CID assigned to one source glyph.</summary>
    /// <param name="glyphId">The source glyph identifier.</param>
    /// <returns>The CID, or -1 when the glyph has not been allocated.</returns>
    public int FindCidForSourceGlyph(int glyphId)
    {
        return allocation.FindCidForSourceGlyph(glyphId);
    }

    /// <summary>Gets the embedded glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The glyph identifier in the FontFile2 program.</returns>
    public int GetSubsetGlyphId(int cid)
    {
        return allocation.GetSubsetGlyphId(cid);
    }

    /// <summary>Finds the document CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The glyph identifier in the FontFile2 program.</param>
    /// <returns>The CID, or -1 when no document CID selects the glyph.</returns>
    public int FindCidForSubsetGlyph(int glyphId)
    {
        return allocation.FindCidForSubsetGlyph(glyphId);
    }

    /// <summary>Copies the CID-to-source-GID mapping.</summary>
    /// <returns>A new array indexed by document CID.</returns>
    public int[] CopyCidToSourceGlyphIds()
    {
        return allocation.CopyCidToSourceGlyphIds();
    }

    /// <summary>Copies the source-GID-to-CID mapping.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    public int[] CopySourceGlyphToCids()
    {
        return allocation.CopySourceGlyphToCids();
    }

    /// <summary>Copies the CID-to-embedded-GID mapping.</summary>
    /// <returns>A new array indexed by document CID.</returns>
    public int[] CopyCidToSubsetGlyphIds()
    {
        return allocation.CopyCidToSubsetGlyphIds();
    }

    /// <summary>Copies the embedded-GID-to-CID mapping.</summary>
    /// <returns>A new array indexed by embedded glyph identifier.</returns>
    public int[] CopySubsetGlyphToCids()
    {
        return allocation.CopySubsetGlyphToCids();
    }

    /// <summary>Copies the source-to-embedded glyph mapping.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    public int[] CopyOldToNewGlyphIds()
    {
        return allocation.CopyOldToNewGlyphIds();
    }

    /// <summary>Copies the embedded-to-source glyph mapping.</summary>
    /// <returns>A new array indexed by embedded glyph identifier.</returns>
    public int[] CopyNewToOldGlyphIds()
    {
        return allocation.CopyNewToOldGlyphIds();
    }

    /// <summary>Gets CIDSystemInfo, CID-to-GID, and optional CIDSet data.</summary>
    /// <returns>The immutable mapping artifact.</returns>
    public PdfCidMappingArtifact GetCidMapping()
    {
        return cidMapping;
    }

    /// <summary>Gets codes mapped to their original Unicode sequences.</summary>
    /// <returns>The immutable two-byte ToUnicode mapping artifact.</returns>
    public PdfToUnicodeMappingArtifact GetToUnicodeMappings()
    {
        return toUnicodeMappings;
    }

    /// <summary>Gets the PDF horizontal <c>DW</c> and <c>W</c> data.</summary>
    /// <returns>The immutable CID widths artifact.</returns>
    public PdfCidWidthsArtifact GetWidths()
    {
        return widths;
    }

    /// <summary>Reports whether vertical <c>DW2</c>/<c>W2</c> data is present.</summary>
    /// <returns>True for a vertical-writing font artifact.</returns>
    public bool HasVerticalWidths()
    {
        return verticalWidths != null;
    }

    /// <summary>Gets the PDF vertical <c>DW2</c> and <c>W2</c> data.</summary>
    /// <returns>The immutable artifact, or null for horizontal writing.</returns>
    public PdfCidVerticalWidthsArtifact? GetVerticalWidths()
    {
        return verticalWidths;
    }

    /// <summary>Gets the PDF FontDescriptor flags and inference report.</summary>
    /// <returns>The immutable descriptor-flags report.</returns>
    public PdfFontDescriptorFlagsReport GetDescriptorFlags()
    {
        return descriptorFlags;
    }

    /// <summary>Gets normalized FontDescriptor metrics and font-file metadata.</summary>
    /// <returns>The immutable descriptor metrics.</returns>
    public PdfFontDescriptorMetrics GetDescriptorMetrics()
    {
        return descriptorMetrics;
    }

    /// <summary>Gets the embedding-rights decision applied to the program.</summary>
    /// <returns>The immutable subset embedding decision.</returns>
    public FontSubsetEmbeddingDecision GetEmbeddingDecision()
    {
        return embeddingDecision;
    }

    static byte[] CopyBytes(byte[] values)
    {
        byte[] copy = new byte[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
