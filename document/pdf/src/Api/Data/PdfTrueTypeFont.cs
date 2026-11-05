namespace pdf;

/// <summary>
/// Contains validated metadata and Unicode mappings loaded from one TrueType or OpenType font.
/// </summary>
public class PdfTrueTypeFont
{
    /// <summary>
    /// Stores the deterministic SHA-256 identity of the complete font data.
    /// </summary>
    public string Identity;
    /// <summary>
    /// Stores the deterministic PDF resource name assigned to the font.
    /// </summary>
    public string ResourceName;
    /// <summary>
    /// Stores the typographic family name when supplied by the font.
    /// </summary>
    public string FamilyName;
    /// <summary>
    /// Stores the typographic subfamily name when supplied by the font.
    /// </summary>
    public string SubfamilyName;
    /// <summary>
    /// Stores the full font name.
    /// </summary>
    public string FullName;
    /// <summary>
    /// Stores the PostScript font name.
    /// </summary>
    public string PostScriptName;
    /// <summary>
    /// Indicates whether the font uses the OpenType CFF signature.
    /// </summary>
    public bool IsOpenType;
    /// <summary>
    /// Stores the number of font design units per em.
    /// </summary>
    public int UnitsPerEm;
    /// <summary>
    /// Stores the minimum horizontal coordinate in font design units.
    /// </summary>
    public int XMin;
    /// <summary>
    /// Stores the minimum vertical coordinate in font design units.
    /// </summary>
    public int YMin;
    /// <summary>
    /// Stores the maximum horizontal coordinate in font design units.
    /// </summary>
    public int XMax;
    /// <summary>
    /// Stores the maximum vertical coordinate in font design units.
    /// </summary>
    public int YMax;
    /// <summary>
    /// Stores the horizontal-header ascent in font design units.
    /// </summary>
    public int Ascent;
    /// <summary>
    /// Stores the horizontal-header descent in font design units.
    /// </summary>
    public int Descent;
    /// <summary>
    /// Stores the horizontal-header line gap in font design units.
    /// </summary>
    public int LineGap;
    /// <summary>
    /// Stores the cap height in font design units, or zero when unavailable.
    /// </summary>
    public int CapHeight;
    /// <summary>
    /// Stores the x-height in font design units, or zero when unavailable.
    /// </summary>
    public int XHeight;
    /// <summary>
    /// Stores the number of glyphs declared by the font.
    /// </summary>
    public int GlyphCount;
    /// <summary>
    /// Stores one advance width in font design units for every glyph.
    /// </summary>
    public int[] AdvanceWidths;
    /// <summary>
    /// Stores the sorted Unicode scalar values represented by the selected cmap.
    /// </summary>
    public int[] UnicodeCodePoints;
    /// <summary>
    /// Stores the glyph index corresponding to each value in <see cref="UnicodeCodePoints"/>.
    /// </summary>
    public int[] GlyphIndices;
    /// <summary>
    /// Stores the platform identifier of the selected Unicode cmap.
    /// </summary>
    public int CmapPlatformId;
    /// <summary>
    /// Stores the encoding identifier of the selected Unicode cmap.
    /// </summary>
    public int CmapEncodingId;
    /// <summary>
    /// Stores the format number of the selected Unicode cmap.
    /// </summary>
    public int CmapFormat;
    /// <summary>
    /// Stores the embedding permission declared by the OS/2 table.
    /// </summary>
    public PdfFontEmbeddingPermission EmbeddingPermission;
    /// <summary>
    /// Indicates whether the font permits outline embedding.
    /// </summary>
    public bool EmbeddingAllowed;
    /// <summary>
    /// Indicates whether the font permits subsetting when outline embedding is allowed.
    /// </summary>
    public bool SubsettingAllowed;
    internal byte[] FontData;

    internal PdfTrueTypeFont(string identity, string resourceName, string familyName, string subfamilyName, string fullName, string postScriptName, bool isOpenType, int unitsPerEm, int xMin, int yMin, int xMax, int yMax, int ascent, int descent, int lineGap, int capHeight, int xHeight, int glyphCount, int[] advanceWidths, int[] unicodeCodePoints, int[] glyphIndices, int cmapPlatformId, int cmapEncodingId, int cmapFormat, PdfFontEmbeddingPermission embeddingPermission, bool embeddingAllowed, bool subsettingAllowed, byte[] fontData)
    {
        Identity = identity;
        ResourceName = resourceName;
        FamilyName = familyName;
        SubfamilyName = subfamilyName;
        FullName = fullName;
        PostScriptName = postScriptName;
        IsOpenType = isOpenType;
        UnitsPerEm = unitsPerEm;
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
        Ascent = ascent;
        Descent = descent;
        LineGap = lineGap;
        CapHeight = capHeight;
        XHeight = xHeight;
        GlyphCount = glyphCount;
        AdvanceWidths = advanceWidths;
        UnicodeCodePoints = unicodeCodePoints;
        GlyphIndices = glyphIndices;
        CmapPlatformId = cmapPlatformId;
        CmapEncodingId = cmapEncodingId;
        CmapFormat = cmapFormat;
        EmbeddingPermission = embeddingPermission;
        EmbeddingAllowed = embeddingAllowed;
        SubsettingAllowed = subsettingAllowed;
        FontData = fontData;
    }

    internal int GetGlyphIndex(int unicodeValue)
    {
        int low = 0;
        int high = UnicodeCodePoints.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (UnicodeCodePoints[middle] == unicodeValue)
            {
                return GlyphIndices[middle];
            }

            if (UnicodeCodePoints[middle] < unicodeValue)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }
}
