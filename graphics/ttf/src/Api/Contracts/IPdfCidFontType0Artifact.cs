namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidFontType0Artifact"/>.</summary>
public interface IPdfCidFontType0Artifact
{
    /// <summary>Copies the raw CID-keyed CFF program for a FontFile3 stream.</summary>
    /// <returns>A new array containing the complete CFF program.</returns>
    byte[] CopyFontFile3();

    /// <summary>Gets the required FontFile3 stream subtype.</summary>
    /// <returns><c>CIDFontType0C</c>.</returns>
    string GetFontFile3Subtype();

    /// <summary>Gets the PostScript-compatible name of the embedded program.</summary>
    /// <returns>The subset-prefixed or full-font base name.</returns>
    string GetBaseFontName();

    /// <summary>Gets the shared descriptor, dictionary, and program names.</summary>
    /// <returns>The immutable font resource names.</returns>
    PdfFontResourceNames GetResourceNames();

    /// <summary>Gets the PDF font kind represented by this artifact.</summary>
    /// <returns>The CIDFontType0 resource kind.</returns>
    PdfFontKind GetFontKind();

    /// <summary>Gets the CFF registry.</summary>
    /// <returns><c>Adobe</c>.</returns>
    string GetRegistry();

    /// <summary>Gets the CFF ordering.</summary>
    /// <returns><c>Identity</c>.</returns>
    string GetOrdering();

    /// <summary>Gets the CFF registry supplement.</summary>
    /// <returns>Zero.</returns>
    int GetSupplement();

    /// <summary>Gets the number of allocated document CIDs.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    int GetCidCount();

    /// <summary>Gets the CIDCount value required by the CFF program.</summary>
    /// <returns>One greater than the greatest CFF charset CID.</returns>
    int GetCffCidCount();

    /// <summary>Gets the source glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The glyph identifier in the source font instance.</returns>
    int GetSourceGlyphId(int cid);

    /// <summary>Finds the document CID assigned to one source glyph.</summary>
    /// <param name="glyphId">The source glyph identifier.</param>
    /// <returns>The CID, or -1 when the glyph has not been allocated.</returns>
    int FindCidForSourceGlyph(int glyphId);

    /// <summary>Gets the embedded glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The glyph identifier in the CFF CharStrings INDEX.</returns>
    int GetSubsetGlyphId(int cid);

    /// <summary>Finds the document CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The glyph identifier in the CFF program.</param>
    /// <returns>The CID, or -1 when no document CID selects the glyph.</returns>
    int FindCidForSubsetGlyph(int glyphId);

    /// <summary>Gets the CFF charset CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The glyph identifier in the CFF program.</param>
    /// <returns>The CID stored in the CFF charset.</returns>
    int GetCharsetCid(int glyphId);

    /// <summary>Finds the embedded glyph identified by one CFF charset CID.</summary>
    /// <param name="cid">The CFF charset CID.</param>
    /// <returns>The embedded glyph identifier, or -1 when absent.</returns>
    int FindSubsetGlyphForCharsetCid(int cid);

    /// <summary>Copies the CFF charset CIDs indexed by embedded GID.</summary>
    /// <returns>A new array containing one unique CID per embedded glyph.</returns>
    int[] CopyCharsetCids();

    /// <summary>Copies the CID-to-source-GID mapping.</summary>
    /// <returns>A new array indexed by document CID.</returns>
    int[] CopyCidToSourceGlyphIds();

    /// <summary>Copies the source-GID-to-CID mapping.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    int[] CopySourceGlyphToCids();

    /// <summary>Copies the CID-to-embedded-GID mapping.</summary>
    /// <returns>A new array indexed by document CID.</returns>
    int[] CopyCidToSubsetGlyphIds();

    /// <summary>Copies the embedded-GID-to-CID mapping.</summary>
    /// <returns>A new array indexed by embedded glyph identifier.</returns>
    int[] CopySubsetGlyphToCids();

    /// <summary>Copies the source-to-embedded glyph mapping.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    int[] CopyOldToNewGlyphIds();

    /// <summary>Copies the embedded-to-source glyph mapping.</summary>
    /// <returns>A new array indexed by embedded glyph identifier.</returns>
    int[] CopyNewToOldGlyphIds();

    /// <summary>Gets CIDSystemInfo and optional CIDSet data.</summary>
    /// <returns>The immutable mapping artifact without a CIDToGIDMap entry.</returns>
    PdfCidMappingArtifact GetCidMapping();

    /// <summary>Gets codes mapped to their original Unicode sequences.</summary>
    /// <returns>The immutable two-byte ToUnicode mapping artifact.</returns>
    PdfToUnicodeMappingArtifact GetToUnicodeMappings();

    /// <summary>Gets the PDF horizontal <c>DW</c> and <c>W</c> data.</summary>
    /// <returns>The immutable CID widths artifact.</returns>
    PdfCidWidthsArtifact GetWidths();

    /// <summary>Reports whether vertical <c>DW2</c>/<c>W2</c> data is present.</summary>
    /// <returns>True for a vertical-writing font artifact.</returns>
    bool HasVerticalWidths();

    /// <summary>Gets the PDF vertical <c>DW2</c> and <c>W2</c> data.</summary>
    /// <returns>The immutable artifact, or null for horizontal writing.</returns>
    PdfCidVerticalWidthsArtifact? GetVerticalWidths();

    /// <summary>Gets the PDF FontDescriptor flags and inference report.</summary>
    /// <returns>The immutable descriptor-flags report.</returns>
    PdfFontDescriptorFlagsReport GetDescriptorFlags();

    /// <summary>Gets normalized FontDescriptor metrics and font-file metadata.</summary>
    /// <returns>The immutable descriptor metrics.</returns>
    PdfFontDescriptorMetrics GetDescriptorMetrics();

    /// <summary>Gets the embedding-rights decision applied to the program.</summary>
    /// <returns>The immutable subset embedding decision.</returns>
    FontSubsetEmbeddingDecision GetEmbeddingDecision();
}
