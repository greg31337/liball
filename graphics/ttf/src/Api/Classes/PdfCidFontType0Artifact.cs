namespace ttf;

/// <summary>Contains a CID-keyed CFF program and its PDF glyph mappings.</summary>
/// <remarks>
/// The CFF charset is normalized to Adobe-Identity-0. For every allocated
/// document CID, the embedded glyph selected by that CID has the same charset
/// CID. Embedded glyphs without document codes receive otherwise unused CIDs.
/// </remarks>
public class PdfCidFontType0Artifact : IPdfCidFontType0Artifact
{
    byte[] fontFile3;
    PdfFontResourceNames resourceNames;
    PdfCidAllocation allocation;
    int[] charsetCids;
    PdfCidMappingArtifact cidMapping;
    PdfToUnicodeMappingArtifact toUnicodeMappings;
    PdfCidWidthsArtifact widths;
    PdfCidVerticalWidthsArtifact? verticalWidths;
    PdfFontDescriptorFlagsReport descriptorFlags;
    PdfFontDescriptorMetrics descriptorMetrics;
    FontSubsetEmbeddingDecision embeddingDecision;

    internal PdfCidFontType0Artifact(
        byte[] fontFile3,
        PdfFontResourceNames resourceNames,
        PdfCidAllocation allocation,
        int[] charsetCids,
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
        this.charsetCids = CopyIntegers(charsetCids);
        this.cidMapping = cidMapping;
        this.toUnicodeMappings = toUnicodeMappings;
        this.widths = widths;
        this.verticalWidths = verticalWidths;
        this.descriptorFlags = descriptorFlags;
        this.descriptorMetrics = descriptorMetrics;
        this.embeddingDecision = embeddingDecision;
    }

    /// <summary>Copies the raw CID-keyed CFF program for a FontFile3 stream.</summary>
    /// <returns>A new array containing the complete CFF program.</returns>
    public byte[] CopyFontFile3()
    {
        return CopyBytes(fontFile3);
    }

    /// <summary>Gets the required FontFile3 stream subtype.</summary>
    /// <returns><c>CIDFontType0C</c>.</returns>
    public string GetFontFile3Subtype()
    {
        return "CIDFontType0C";
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
    /// <returns>The CIDFontType0 resource kind.</returns>
    public PdfFontKind GetFontKind()
    {
        return PdfFontKind.Type0CidFontType0;
    }

    /// <summary>Gets the CFF registry.</summary>
    /// <returns><c>Adobe</c>.</returns>
    public string GetRegistry()
    {
        return "Adobe";
    }

    /// <summary>Gets the CFF ordering.</summary>
    /// <returns><c>Identity</c>.</returns>
    public string GetOrdering()
    {
        return "Identity";
    }

    /// <summary>Gets the CFF registry supplement.</summary>
    /// <returns>Zero.</returns>
    public int GetSupplement()
    {
        return 0;
    }

    /// <summary>Gets the number of allocated document CIDs.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    public int GetCidCount()
    {
        return allocation.GetCidCount();
    }

    /// <summary>Gets the CIDCount value required by the CFF program.</summary>
    /// <returns>One greater than the greatest CFF charset CID.</returns>
    public int GetCffCidCount()
    {
        int maximum = 0;
        int index = 0;

        while (index < charsetCids.Length)
        {
            maximum = System.Math.Max(maximum, charsetCids[index]);
            index += 1;
        }

        return maximum == ushort.MaxValue ? 65536 : maximum + 1;
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
    /// <returns>The glyph identifier in the CFF CharStrings INDEX.</returns>
    public int GetSubsetGlyphId(int cid)
    {
        return allocation.GetSubsetGlyphId(cid);
    }

    /// <summary>Finds the document CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The glyph identifier in the CFF program.</param>
    /// <returns>The CID, or -1 when no document CID selects the glyph.</returns>
    public int FindCidForSubsetGlyph(int glyphId)
    {
        return allocation.FindCidForSubsetGlyph(glyphId);
    }

    /// <summary>Gets the CFF charset CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The glyph identifier in the CFF program.</param>
    /// <returns>The CID stored in the CFF charset.</returns>
    public int GetCharsetCid(int glyphId)
    {
        if (glyphId < 0 || glyphId >= charsetCids.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        return charsetCids[glyphId];
    }

    /// <summary>Finds the embedded glyph identified by one CFF charset CID.</summary>
    /// <param name="cid">The CFF charset CID.</param>
    /// <returns>The embedded glyph identifier, or -1 when absent.</returns>
    public int FindSubsetGlyphForCharsetCid(int cid)
    {
        if (cid < 0 || cid > ushort.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("cid");
        }

        int glyphId = 0;

        while (glyphId < charsetCids.Length)
        {
            if (charsetCids[glyphId] == cid)
            {
                return glyphId;
            }

            glyphId += 1;
        }

        return -1;
    }

    /// <summary>Copies the CFF charset CIDs indexed by embedded GID.</summary>
    /// <returns>A new array containing one unique CID per embedded glyph.</returns>
    public int[] CopyCharsetCids()
    {
        return CopyIntegers(charsetCids);
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

    /// <summary>Gets CIDSystemInfo and optional CIDSet data.</summary>
    /// <returns>The immutable mapping artifact without a CIDToGIDMap entry.</returns>
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
