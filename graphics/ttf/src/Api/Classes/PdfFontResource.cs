namespace ttf;

/// <summary>Contains one finalized, immutable PDF font resource artifact.</summary>
public class PdfFontResource : IPdfFontResource
{
    string deduplicationKey;
    PdfFontResourceReport compatibilityReport;
    PdfFontKind fontKind;
    PdfType0EncodingMetadata? type0Encoding;
    PdfCidFontType2Artifact? type2;
    PdfCidFontType0Artifact? type0;
    PdfOpenTypeFontArtifact? openType;
    PdfSimpleFontEncodingArtifact? simpleEncoding;
    PdfFontOptions fontOptions;
    FontOperationOptions operationOptions;

    internal PdfFontResource(
        string deduplicationKey,
        PdfFontResourceReport compatibilityReport,
        PdfFontKind fontKind,
        PdfType0EncodingMetadata? type0Encoding,
        PdfCidFontType2Artifact? type2,
        PdfCidFontType0Artifact? type0,
        PdfOpenTypeFontArtifact? openType,
        PdfSimpleFontEncodingArtifact? simpleEncoding,
        PdfFontOptions fontOptions)
    {
        this.deduplicationKey = deduplicationKey;
        this.compatibilityReport = compatibilityReport;
        this.fontKind = fontKind;
        this.type0Encoding = type0Encoding;
        this.type2 = type2;
        this.type0 = type0;
        this.openType = openType;
        this.simpleEncoding = simpleEncoding;
        this.fontOptions = fontOptions.Copy();
        operationOptions = fontOptions.GetOperationOptions();
    }

    /// <summary>Gets the stable identity of this complete finalized resource.</summary>
    /// <remarks>
    /// Equal keys identify resources with the same face and static instance,
    /// policies, writing mode, character-code allocation, Unicode observations,
    /// embedding outcome, and generated font program.
    /// </remarks>
    /// <returns>A versioned uppercase SHA-256 key.</returns>
    public string GetDeduplicationKey()
    {
        return deduplicationKey;
    }

    /// <summary>Gets font-resource compatibility facts and requested-profile findings.</summary>
    /// <remarks>
    /// The report assesses this font resource only and never claims that a PDF
    /// document conforms to PDF/A, PDF/UA, or another document standard.
    /// </remarks>
    /// <returns>The immutable finalized resource report.</returns>
    public PdfFontResourceReport GetCompatibilityReport()
    {
        return compatibilityReport;
    }

    /// <summary>Gets the resolved PDF font and program strategy.</summary>
    /// <returns>The concrete finalized font kind.</returns>
    public PdfFontKind GetFontKind() { return fontKind; }

    /// <summary>Gets the number of frozen allocated character codes.</summary>
    /// <returns>The code count including reserved code zero.</returns>
    public int GetCharacterCodeCount()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetCidCount();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetCidCount();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetCidCount();
    }

    /// <summary>Copies the finalized embedded font program.</summary>
    /// <returns>A new standalone TrueType, CFF, or OpenType byte array.</returns>
    public byte[] CopyEmbeddedFont()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopyFontFile3();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopyFontFile3();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopyFontFile2();
    }

    /// <summary>Gets the PDF font-file stream kind.</summary>
    /// <returns><c>FontFile2</c> for TrueType or <c>FontFile3</c> otherwise.</returns>
    public PdfEmbeddedFontFileKind GetEmbeddedFontFileKind()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0 ||
            fontKind == PdfFontKind.Type0OpenType)
        {
            return PdfEmbeddedFontFileKind.FontFile3;
        }

        return PdfEmbeddedFontFileKind.FontFile2;
    }

    /// <summary>Gets the shared finalized PDF base font name.</summary>
    /// <returns>The subset-prefixed or full-font PostScript-compatible name.</returns>
    public string GetBaseFontName()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetBaseFontName();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetBaseFontName();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetBaseFontName();
    }

    /// <summary>Gets the Type 0 Identity CMap metadata.</summary>
    /// <returns>The immutable Identity-H or Identity-V metadata.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is a simple font.
    /// </exception>
    public PdfType0EncodingMetadata GetType0EncodingMetadata()
    {
        if (type0Encoding == null)
        {
            throw new System.InvalidOperationException(
                "A simple PDF font does not use Type 0 encoding metadata.");
        }

        return type0Encoding;
    }

    /// <summary>Gets the shared descriptor, dictionary, and program names.</summary>
    /// <returns>The immutable font resource names.</returns>
    public PdfFontResourceNames GetResourceNames()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetResourceNames();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetResourceNames();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetResourceNames();
    }

    /// <summary>Gets CIDSystemInfo, CIDToGIDMap, and optional CIDSet data.</summary>
    /// <returns>The immutable CID mapping artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is a simple font.
    /// </exception>
    public PdfCidMappingArtifact GetCidMapping()
    {
        if (fontKind == PdfFontKind.SimpleTrueType)
        {
            throw new System.InvalidOperationException(
                "A simple PDF font does not use CID mapping metadata.");
        }

        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetCidMapping();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetCidMapping();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetCidMapping();
    }

    /// <summary>Gets the descendant font's CIDSystemInfo values.</summary>
    /// <returns>The immutable Adobe-Identity-0 metadata.</returns>
    public PdfCidSystemInfo GetCidSystemInfo()
    {
        return GetCidMapping().GetCidSystemInfo();
    }

    /// <summary>Reports whether the descendant accepts a CIDToGIDMap entry.</summary>
    /// <returns>True for a CIDFontType2 descendant.</returns>
    public bool HasCidToGidMap()
    {
        return GetCidMapping().HasCidToGidMap();
    }

    /// <summary>Reports whether the predefined Identity CIDToGIDMap is valid.</summary>
    /// <returns>True when every CID equals its embedded glyph identifier.</returns>
    public bool UsesIdentityCidToGidMap()
    {
        return GetCidMapping().UsesIdentityCidToGidMap();
    }

    /// <summary>Gets the predefined CIDToGIDMap name when used.</summary>
    /// <returns>Identity, or an empty string for a stream or no entry.</returns>
    public string GetCidToGidMapName()
    {
        return GetCidMapping().GetCidToGidMapName();
    }

    /// <summary>Copies the big-endian CIDToGIDMap stream data.</summary>
    /// <returns>Two bytes per CID, or an empty array for Identity or no entry.</returns>
    public byte[] CopyCidToGidMap()
    {
        return GetCidMapping().CopyCidToGidMap();
    }

    /// <summary>Reports whether legacy PDF/A-1 CIDSet data was requested.</summary>
    /// <returns>True when a CIDSet stream should be emitted.</returns>
    public bool HasCidSet()
    {
        return GetCidMapping().HasCidSet();
    }

    /// <summary>Copies the high-bit-first CIDSet stream data.</summary>
    /// <returns>A new array, or an empty array when CIDSet is not requested.</returns>
    public byte[] CopyCidSet()
    {
        return GetCidMapping().CopyCidSet();
    }

    /// <summary>Gets the PDF horizontal DW and W data.</summary>
    /// <returns>The immutable widths artifact.</returns>
    public PdfCidWidthsArtifact GetWidths()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetWidths();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetWidths();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetWidths();
    }

    /// <summary>Reports whether vertical DW2 and W2 data is present.</summary>
    /// <returns>True for a vertical Type 0 resource.</returns>
    public bool HasVerticalWidths()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.HasVerticalWidths();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.HasVerticalWidths();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.HasVerticalWidths();
    }

    /// <summary>Gets the PDF vertical DW2 and W2 data.</summary>
    /// <returns>The immutable artifact, or null for horizontal writing.</returns>
    public PdfCidVerticalWidthsArtifact? GetVerticalWidths()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetVerticalWidths();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetVerticalWidths();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetVerticalWidths();
    }

    /// <summary>Gets the PDF FontDescriptor flags and inference report.</summary>
    /// <returns>The immutable descriptor-flags report.</returns>
    public PdfFontDescriptorFlagsReport GetDescriptorFlags()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetDescriptorFlags();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetDescriptorFlags();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetDescriptorFlags();
    }

    /// <summary>Gets normalized FontDescriptor metrics and font-file metadata.</summary>
    /// <returns>The immutable descriptor metrics.</returns>
    public PdfFontDescriptorMetrics GetDescriptorMetrics()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetDescriptorMetrics();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetDescriptorMetrics();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetDescriptorMetrics();
    }

    /// <summary>Gets the source glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The source glyph identifier, or -1 for a sparse CID hole.</returns>
    public int GetSourceGlyphId(int cid)
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetSourceGlyphId(cid);
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetSourceGlyphId(cid);
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetSourceGlyphId(cid);
    }

    /// <summary>Finds the document CID assigned to one source glyph.</summary>
    /// <param name="glyphId">The source glyph identifier.</param>
    /// <returns>The CID, or -1 when the glyph was not allocated.</returns>
    public int FindCidForSourceGlyph(int glyphId)
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.FindCidForSourceGlyph(glyphId);
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.FindCidForSourceGlyph(glyphId);
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.FindCidForSourceGlyph(glyphId);
    }

    /// <summary>Gets the embedded glyph selected by one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The embedded glyph identifier.</returns>
    public int GetSubsetGlyphId(int cid)
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetSubsetGlyphId(cid);
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetSubsetGlyphId(cid);
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetSubsetGlyphId(cid);
    }

    /// <summary>Finds the document CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The embedded glyph identifier.</param>
    /// <returns>The CID, or -1 when no document CID selects the glyph.</returns>
    public int FindCidForSubsetGlyph(int glyphId)
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.FindCidForSubsetGlyph(glyphId);
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.FindCidForSubsetGlyph(glyphId);
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.FindCidForSubsetGlyph(glyphId);
    }

    /// <summary>Reports whether the embedded program uses CFF charset CIDs.</summary>
    /// <returns>True for a CID-keyed CFF descendant.</returns>
    public bool HasCffCharsetMappings()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            return true;
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.UsesCffCharset();
        }

        return false;
    }

    /// <summary>Gets the CFF charset CID assigned to one embedded glyph.</summary>
    /// <param name="glyphId">The embedded glyph identifier.</param>
    /// <returns>The CID stored in the CFF charset.</returns>
    public int GetCffCharsetCid(int glyphId)
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetCharsetCid(glyphId);
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            int[] cids = openType.CopyCffCharsetCids();
            if (cids.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "The OpenType program does not use CFF charset CIDs.");
            }
            if (glyphId < 0 || glyphId >= cids.Length)
            {
                throw new System.ArgumentOutOfRangeException("glyphId");
            }
            return cids[glyphId];
        }

        throw new System.InvalidOperationException(
            "The PDF font resource does not use CFF charset CIDs.");
    }

    /// <summary>Finds the embedded glyph identified by one CFF charset CID.</summary>
    /// <param name="cid">The CFF charset CID.</param>
    /// <returns>The embedded glyph identifier, or -1 when absent.</returns>
    public int FindSubsetGlyphForCffCharsetCid(int cid)
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.FindSubsetGlyphForCharsetCid(cid);
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            int[] cids = openType.CopyCffCharsetCids();
            if (cids.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "The OpenType program does not use CFF charset CIDs.");
            }
            int glyphId = 0;
            while (glyphId < cids.Length)
            {
                if (cids[glyphId] == cid)
                {
                    return glyphId;
                }
                glyphId += 1;
            }
            return -1;
        }

        throw new System.InvalidOperationException(
            "The PDF font resource does not use CFF charset CIDs.");
    }

    /// <summary>Copies CFF charset CIDs indexed by embedded glyph identifier.</summary>
    /// <returns>A new array containing one CID per embedded glyph.</returns>
    public int[] CopyCffCharsetCids()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopyCharsetCids();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            int[] cids = openType.CopyCffCharsetCids();
            if (cids.Length != 0)
            {
                return cids;
            }
        }

        throw new System.InvalidOperationException(
            "The PDF font resource does not use CFF charset CIDs.");
    }

    /// <summary>Copies the CID-to-source-GID mapping.</summary>
    /// <returns>A new array indexed by document CID.</returns>
    public int[] CopyCidToSourceGlyphIds()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopyCidToSourceGlyphIds();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopyCidToSourceGlyphIds();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopyCidToSourceGlyphIds();
    }

    /// <summary>Copies the source-GID-to-CID mapping.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    public int[] CopySourceGlyphToCids()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopySourceGlyphToCids();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopySourceGlyphToCids();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopySourceGlyphToCids();
    }

    /// <summary>Copies the CID-to-embedded-GID mapping.</summary>
    /// <returns>A new array indexed by document CID.</returns>
    public int[] CopyCidToSubsetGlyphIds()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopyCidToSubsetGlyphIds();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopyCidToSubsetGlyphIds();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopyCidToSubsetGlyphIds();
    }

    /// <summary>Copies the embedded-GID-to-CID mapping.</summary>
    /// <returns>A new array indexed by embedded glyph identifier.</returns>
    public int[] CopySubsetGlyphToCids()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopySubsetGlyphToCids();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopySubsetGlyphToCids();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopySubsetGlyphToCids();
    }

    /// <summary>Copies the source-to-embedded glyph mapping.</summary>
    /// <returns>A new array indexed by source glyph identifier.</returns>
    public int[] CopyOldToNewGlyphIds()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopyOldToNewGlyphIds();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopyOldToNewGlyphIds();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopyOldToNewGlyphIds();
    }

    /// <summary>Copies the embedded-to-source glyph mapping.</summary>
    /// <returns>A new array indexed by embedded glyph identifier.</returns>
    public int[] CopyNewToOldGlyphIds()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.CopyNewToOldGlyphIds();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.CopyNewToOldGlyphIds();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.CopyNewToOldGlyphIds();
    }

    /// <summary>Gets the CID selected by one PDF character code.</summary>
    /// <param name="code">The one-byte or two-byte character code.</param>
    /// <returns>The identical CID, or -1 for a sparse unallocated code.</returns>
    public int GetCidForCharacterCode(int code)
    {
        int[] sourceGlyphIds = CopyCidToSourceGlyphIds();
        if (code < 0 || code >= sourceGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("code");
        }

        return sourceGlyphIds[code] < 0 ? -1 : code;
    }

    /// <summary>Finds the PDF character code selecting one document CID.</summary>
    /// <param name="cid">The document CID.</param>
    /// <returns>The identical code, or -1 for a sparse unallocated CID.</returns>
    public int FindCharacterCodeForCid(int cid)
    {
        int[] sourceGlyphIds = CopyCidToSourceGlyphIds();
        if (cid < 0 || cid >= sourceGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("cid");
        }

        return sourceGlyphIds[cid] < 0 ? -1 : cid;
    }

    /// <summary>Copies the character-code-to-CID mapping.</summary>
    /// <returns>A new array indexed by character code with -1 for sparse holes.</returns>
    public int[] CopyCharacterCodeToCids()
    {
        int[] sourceGlyphIds = CopyCidToSourceGlyphIds();
        int[] result = new int[sourceGlyphIds.Length];
        int code = 0;

        while (code < result.Length)
        {
            result[code] = sourceGlyphIds[code] < 0 ? -1 : code;
            code += 1;
        }

        return result;
    }

    /// <summary>Copies the CID-to-character-code mapping.</summary>
    /// <returns>A new array indexed by CID with -1 for sparse holes.</returns>
    public int[] CopyCidToCharacterCodes()
    {
        return CopyCharacterCodeToCids();
    }

    /// <summary>Gets finalized code-to-original-Unicode mappings.</summary>
    /// <returns>The immutable mapping and ActualText recommendation artifact.</returns>
    public PdfToUnicodeMappingArtifact GetToUnicodeMappings()
    {
        if (fontKind == PdfFontKind.SimpleTrueType)
        {
            System.ArgumentNullException.ThrowIfNull(simpleEncoding);
            return simpleEncoding.GetToUnicodeMappings();
        }

        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetToUnicodeMappings();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetToUnicodeMappings();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetToUnicodeMappings();
    }

    /// <summary>Gets the applied embedding-rights decision.</summary>
    /// <returns>The immutable subset or full-font embedding decision.</returns>
    public FontSubsetEmbeddingDecision GetEmbeddingDecision()
    {
        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            return type0.GetEmbeddingDecision();
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            return openType.GetEmbeddingDecision();
        }

        System.ArgumentNullException.ThrowIfNull(type2);
        return type2.GetEmbeddingDecision();
    }

    /// <summary>Gets the finalized CIDFontType2 artifact.</summary>
    /// <returns>The immutable Type2 artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not a Type 0 CIDFontType2 resource.
    /// </exception>
    public PdfCidFontType2Artifact? GetCidFontType2Artifact()
    {
        if (fontKind != PdfFontKind.Type0CidFontType2)
        {
            throw new System.InvalidOperationException(
                "The finalized resource is not CIDFontType2.");
        }

        return type2;
    }

    /// <summary>Gets the finalized CIDFontType0 artifact.</summary>
    /// <returns>The immutable Type0 artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not a Type 0 CIDFontType0 resource.
    /// </exception>
    public PdfCidFontType0Artifact? GetCidFontType0Artifact()
    {
        if (fontKind != PdfFontKind.Type0CidFontType0)
        {
            throw new System.InvalidOperationException(
                "The finalized resource is not CIDFontType0.");
        }

        return type0;
    }

    /// <summary>Gets the finalized OpenType FontFile3 artifact.</summary>
    /// <returns>The immutable OpenType artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not an OpenType FontFile3 resource.
    /// </exception>
    public PdfOpenTypeFontArtifact? GetOpenTypeArtifact()
    {
        if (fontKind != PdfFontKind.Type0OpenType)
        {
            throw new System.InvalidOperationException(
                "The finalized resource is not OpenType.");
        }

        return openType;
    }

    /// <summary>Gets the finalized legacy simple-font encoding.</summary>
    /// <returns>The immutable one-byte encoding artifact.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The finalized resource is not a simple TrueType resource.
    /// </exception>
    public PdfSimpleFontEncodingArtifact? GetSimpleEncoding()
    {
        if (fontKind != PdfFontKind.SimpleTrueType)
        {
            throw new System.InvalidOperationException(
                "The finalized resource is not a simple TrueType font.");
        }

        return simpleEncoding;
    }

    /// <summary>Gets the complete controls retained for this finalized resource.</summary>
    /// <returns>An independent mutable copy.</returns>
    public PdfFontOptions GetFontOptions()
    {
        return fontOptions.Copy();
    }

    /// <summary>Gets shared operation controls used during finalization.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }
}
