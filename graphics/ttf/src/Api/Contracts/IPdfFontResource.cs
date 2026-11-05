namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontResource"/>.</summary>
public interface IPdfFontResource
{
    /// <summary>Gets the stable identity of this complete finalized resource.</summary>
    /// <remarks>
    /// Equal keys identify resources with the same face and static instance,
    /// policies, writing mode, character-code allocation, Unicode observations,
    /// embedding outcome, and generated font program.
    /// </remarks>
    /// <returns>A versioned uppercase SHA-256 key.</returns>
    string GetDeduplicationKey();

    /// <summary>Gets font-resource compatibility facts and requested-profile findings.</summary>
    /// <remarks>
    /// The report assesses this font resource only and never claims that a PDF
    /// document conforms to PDF/A, PDF/UA, or another document standard.
    /// </remarks>
    /// <returns>The immutable finalized resource report.</returns>
    PdfFontResourceReport GetCompatibilityReport();

    /// <summary>Gets the resolved PDF font and program strategy.</summary>
    /// <returns>The concrete finalized font kind.</returns>
    PdfFontKind GetFontKind();

    /// <summary>Gets the number of frozen allocated character codes.</summary>
    /// <returns>The code count including reserved code zero.</returns>
    int GetCharacterCodeCount();

    /// <summary>Copies the finalized embedded font program.</summary>
    /// <returns>A new standalone TrueType, CFF, or OpenType byte array.</returns>
    byte[] CopyEmbeddedFont();

    /// <summary>Gets the PDF font-file stream kind.</summary>
    /// <returns><c>FontFile2</c> for TrueType or <c>FontFile3</c> otherwise.</returns>
    PdfEmbeddedFontFileKind GetEmbeddedFontFileKind();

    /// <summary>Gets the shared finalized PDF base font name.</summary>
    /// <returns>The subset-prefixed or full-font PostScript-compatible name.</returns>
    string GetBaseFontName();

    /// <summary>Gets the Type 0 Identity CMap metadata.</summary>
    /// <returns>The immutable Identity-H or Identity-V metadata.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is a simple font.
    /// </exception>
    PdfType0EncodingMetadata GetType0EncodingMetadata();

    /// <summary>Gets the shared descriptor, dictionary, and program names.</summary>
    /// <returns>The immutable font resource names.</returns>
    PdfFontResourceNames GetResourceNames();

    /// <summary>Gets CIDSystemInfo, CIDToGIDMap, and optional CIDSet data.</summary>
    /// <returns>The immutable CID mapping artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is a simple font.
    /// </exception>
    PdfCidMappingArtifact GetCidMapping();

    /// <summary>Gets the descendant font's CIDSystemInfo values.</summary>
    /// <returns>The immutable Adobe-Identity-0 metadata.</returns>
    PdfCidSystemInfo GetCidSystemInfo();

    /// <summary>Reports whether the descendant accepts a CIDToGIDMap entry.</summary>
    /// <returns>True for a CIDFontType2 descendant.</returns>
    bool HasCidToGidMap();

    /// <summary>Reports whether the predefined Identity CIDToGIDMap is valid.</summary>
    /// <returns>True when every CID equals its embedded glyph identifier.</returns>
    bool UsesIdentityCidToGidMap();

    /// <summary>Gets the predefined CIDToGIDMap name when used.</summary>
    /// <returns>Identity, or an empty string for a stream or no entry.</returns>
    string GetCidToGidMapName();

    /// <summary>Copies the big-endian CIDToGIDMap stream data.</summary>
    /// <returns>Two bytes per CID, or an empty array for Identity or no entry.</returns>
    byte[] CopyCidToGidMap();

    /// <summary>Reports whether legacy PDF/A-1 CIDSet data was requested.</summary>
    /// <returns>True when a CIDSet stream should be emitted.</returns>
    bool HasCidSet();

    /// <summary>Copies the high-bit-first CIDSet stream data.</summary>
    /// <returns>A new array, or an empty array when CIDSet is not requested.</returns>
    byte[] CopyCidSet();

    /// <summary>Gets the PDF horizontal DW and W data.</summary>
    /// <returns>The immutable widths artifact.</returns>
    PdfCidWidthsArtifact GetWidths();

    /// <summary>Reports whether vertical DW2 and W2 data is present.</summary>
    /// <returns>True for a vertical Type 0 resource.</returns>
    bool HasVerticalWidths();

    /// <summary>Gets the PDF vertical DW2 and W2 data.</summary>
    /// <returns>The immutable artifact, or null for horizontal writing.</returns>
    PdfCidVerticalWidthsArtifact? GetVerticalWidths();

    /// <summary>Gets the PDF FontDescriptor flags and inference report.</summary>
    /// <returns>The immutable descriptor-flags report.</returns>
    PdfFontDescriptorFlagsReport GetDescriptorFlags();

    /// <summary>Gets normalized FontDescriptor metrics and font-file metadata.</summary>
    /// <returns>The immutable descriptor metrics.</returns>
    PdfFontDescriptorMetrics GetDescriptorMetrics();

    /// <summary>Gets the source glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The source glyph identifier, or -1 for a sparse CID hole.</returns>
    int GetSourceGlyphId(int cid);

    /// <summary>Finds the document CID assigned to one source glyph.</summary>
    /// <param name="glyphId">The source glyph identifier.</param>
    /// <returns>The CID, or -1 when the glyph was not allocated.</returns>
    int FindCidForSourceGlyph(int glyphId);

    /// <summary>Gets the embedded glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The embedded glyph identifier.</returns>
    int GetSubsetGlyphId(int cid);

    /// <summary>Finds the document CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The embedded glyph identifier.</param>
    /// <returns>The CID, or -1 when no document CID selects the glyph.</returns>
    int FindCidForSubsetGlyph(int glyphId);

    /// <summary>Reports whether the embedded program uses CFF charset CIDs.</summary>
    /// <returns>True for a CID-keyed CFF descendant.</returns>
    bool HasCffCharsetMappings();

    /// <summary>Gets the CFF charset CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The embedded glyph identifier.</param>
    /// <returns>The CID stored in the CFF charset.</returns>
    int GetCffCharsetCid(int glyphId);

    /// <summary>Finds the embedded glyph identified by one CFF charset CID.</summary>
    /// <param name="cid">The CFF charset CID.</param>
    /// <returns>The embedded glyph identifier, or -1 when absent.</returns>
    int FindSubsetGlyphForCffCharsetCid(int cid);

    /// <summary>Copies CFF charset CIDs indexed by embedded glyph identifier.</summary>
    /// <returns>A new array containing one CID per embedded glyph.</returns>
    int[] CopyCffCharsetCids();

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

    /// <summary>Gets the CID selected by one PDF character code.</summary>
    /// <param name="code">The one-byte or two-byte character code.</param>
    /// <returns>The identical CID, or -1 for a sparse unallocated code.</returns>
    int GetCidForCharacterCode(int code);

    /// <summary>Finds the PDF character code selecting one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The identical code, or -1 for a sparse unallocated CID.</returns>
    int FindCharacterCodeForCid(int cid);

    /// <summary>Copies the character-code-to-CID mapping.</summary>
    /// <returns>A new array indexed by character code with -1 for sparse holes.</returns>
    int[] CopyCharacterCodeToCids();

    /// <summary>Copies the CID-to-character-code mapping.</summary>
    /// <returns>A new array indexed by CID with -1 for sparse holes.</returns>
    int[] CopyCidToCharacterCodes();

    /// <summary>Gets finalized code-to-original-Unicode mappings.</summary>
    /// <returns>The immutable mapping and ActualText recommendation artifact.</returns>
    PdfToUnicodeMappingArtifact GetToUnicodeMappings();

    /// <summary>Gets the applied embedding-rights decision.</summary>
    /// <returns>The immutable subset or full-font embedding decision.</returns>
    FontSubsetEmbeddingDecision GetEmbeddingDecision();

    /// <summary>Gets the finalized CIDFontType2 artifact.</summary>
    /// <returns>The immutable Type2 artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not a Type 0 CIDFontType2 resource.
    /// </exception>
    PdfCidFontType2Artifact? GetCidFontType2Artifact();

    /// <summary>Gets the finalized CIDFontType0 artifact.</summary>
    /// <returns>The immutable Type0 artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not a Type 0 CIDFontType0 resource.
    /// </exception>
    PdfCidFontType0Artifact? GetCidFontType0Artifact();

    /// <summary>Gets the finalized OpenType FontFile3 artifact.</summary>
    /// <returns>The immutable OpenType artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not an OpenType FontFile3 resource.
    /// </exception>
    PdfOpenTypeFontArtifact? GetOpenTypeArtifact();

    /// <summary>Gets the finalized legacy simple-font encoding.</summary>
    /// <returns>The immutable one-byte encoding artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not a simple TrueType resource.
    /// </exception>
    PdfSimpleFontEncodingArtifact? GetSimpleEncoding();

    /// <summary>Gets the complete controls retained for this finalized resource.</summary>
    /// <returns>An independent mutable copy.</returns>
    PdfFontOptions GetFontOptions();

    /// <summary>Gets shared operation controls used during finalization.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();
}
