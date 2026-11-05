namespace ttf;

/// <summary>Contains an OpenType FontFile3 program and its glyph mappings.</summary>
/// <remarks>
/// TrueType outlines use a CIDFontType2 descendant. CFF outlines use a
/// CIDFontType0 descendant. CID-keyed CFF programs select glyphs through the
/// CFF charset, while name-keyed CFF programs require direct CID-to-GID values.
/// </remarks>
public class PdfOpenTypeFontArtifact : IPdfOpenTypeFontArtifact
{
    byte[] fontFile3;
    PdfFontResourceNames resourceNames;
    PdfCidAllocation allocation;
    FontOutlineKind sourceOutlineKind;
    FontOutlineKind embeddedOutlineKind;
    PdfFontKind descendantFontKind;
    bool usesCffCharset;
    bool usesDirectCids;
    int[] cffCharsetCids;
    PdfCidMappingArtifact cidMapping;
    PdfToUnicodeMappingArtifact toUnicodeMappings;
    PdfCidWidthsArtifact widths;
    PdfCidVerticalWidthsArtifact? verticalWidths;
    PdfFontDescriptorFlagsReport descriptorFlags;
    PdfFontDescriptorMetrics descriptorMetrics;
    FontSubsetEmbeddingDecision embeddingDecision;

    internal PdfOpenTypeFontArtifact(
        byte[] fontFile3,
        PdfFontResourceNames resourceNames,
        PdfCidAllocation allocation,
        FontOutlineKind sourceOutlineKind,
        FontOutlineKind embeddedOutlineKind,
        PdfFontKind descendantFontKind,
        bool usesCffCharset,
        bool usesDirectCids,
        int[] cffCharsetCids,
        PdfCidMappingArtifact cidMapping,
        PdfToUnicodeMappingArtifact toUnicodeMappings,
        PdfCidWidthsArtifact widths,
        PdfCidVerticalWidthsArtifact? verticalWidths,
        PdfFontDescriptorFlagsReport descriptorFlags,
        PdfFontDescriptorMetrics descriptorMetrics,
        FontSubsetEmbeddingDecision embeddingDecision)
    {
        this.fontFile3 = CopyBytes(fontFile3);
        this.resourceNames = resourceNames;
        this.allocation = allocation;
        this.sourceOutlineKind = sourceOutlineKind;
        this.embeddedOutlineKind = embeddedOutlineKind;
        this.descendantFontKind = descendantFontKind;
        this.usesCffCharset = usesCffCharset;
        this.usesDirectCids = usesDirectCids;
        this.cffCharsetCids = CopyIntegers(cffCharsetCids);
        this.cidMapping = cidMapping;
        this.toUnicodeMappings = toUnicodeMappings;
        this.widths = widths;
        this.verticalWidths = verticalWidths;
        this.descriptorFlags = descriptorFlags;
        this.descriptorMetrics = descriptorMetrics;
        this.embeddingDecision = embeddingDecision;
    }

    /// <summary>Copies the complete OpenType program for a FontFile3 stream.</summary>
    /// <returns>A new array containing the standalone SFNT bytes.</returns>
    public byte[] CopyFontFile3()
    {
        return CopyBytes(fontFile3);
    }

    /// <summary>Gets the required FontFile3 stream subtype.</summary>
    /// <returns><c>OpenType</c>.</returns>
    public string GetFontFile3Subtype()
    {
        return "OpenType";
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

    /// <summary>Gets the PDF font program strategy represented by the artifact.</summary>
    /// <returns>The OpenType Type 0 font kind.</returns>
    public PdfFontKind GetFontKind()
    {
        return PdfFontKind.Type0OpenType;
    }

    /// <summary>Gets the required descendant CIDFont kind.</summary>
    /// <returns>CIDFontType2 for TrueType or CIDFontType0 for CFF.</returns>
    public PdfFontKind GetDescendantFontKind()
    {
        return descendantFontKind;
    }

    /// <summary>Gets the source font's outline kind.</summary>
    /// <returns>The source TrueType, CFF 1, or CFF 2 outline kind.</returns>
    public FontOutlineKind GetSourceOutlineKind()
    {
        return sourceOutlineKind;
    }

    /// <summary>Gets the embedded static outline kind.</summary>
    /// <returns>TrueType or CFF 1.</returns>
    public FontOutlineKind GetEmbeddedOutlineKind()
    {
        return embeddedOutlineKind;
    }

    /// <summary>Reports whether CIDs select CFF GIDs through a charset.</summary>
    /// <returns>True for a CID-keyed CFF program.</returns>
    public bool UsesCffCharset()
    {
        return usesCffCharset;
    }

    /// <summary>Reports whether CIDs are used directly as embedded GIDs.</summary>
    /// <returns>True for a name-keyed CFF program.</returns>
    public bool UsesDirectCidsAsGlyphIds()
    {
        return usesDirectCids;
    }

    /// <summary>Copies CFF charset CIDs indexed by embedded GID.</summary>
    /// <returns>A new array, or an empty array for non-CID-keyed programs.</returns>
    public int[] CopyCffCharsetCids()
    {
        return CopyIntegers(cffCharsetCids);
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
    /// <returns>The glyph identifier in the embedded OpenType program.</returns>
    public int GetSubsetGlyphId(int cid)
    {
        return allocation.GetSubsetGlyphId(cid);
    }

    /// <summary>Finds the document CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The embedded glyph identifier.</param>
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
    /// <returns>The immutable mapping artifact for the descendant font kind.</returns>
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

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
