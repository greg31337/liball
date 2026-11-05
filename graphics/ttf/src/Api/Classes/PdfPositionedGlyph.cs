namespace ttf;

/// <summary>Contains one encoded glyph's PDF text-space positioning data.</summary>
/// <remarks>
/// Advances and shaping offsets use PDF's 1000-unit text space. Baseline shift
/// remains in caller/user-space points so a PDF generator can apply it through
/// a text matrix without depending on a selected font size convention.
/// </remarks>
public class PdfPositionedGlyph : IPdfPositionedGlyph
{
    int visualIndex;
    int characterCode;
    int cid;
    int sourceGlyphId;
    int utf16Start;
    int utf16Length;
    double xAdvance;
    double yAdvance;
    double xOffset;
    double yOffset;
    double baselineShift;

    internal PdfPositionedGlyph(
        int visualIndex,
        int characterCode,
        int cid,
        int sourceGlyphId,
        int utf16Start,
        int utf16Length,
        double xAdvance,
        double yAdvance,
        double xOffset,
        double yOffset,
        double baselineShift)
    {
        this.visualIndex = visualIndex;
        this.characterCode = characterCode;
        this.cid = cid;
        this.sourceGlyphId = sourceGlyphId;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.xOffset = xOffset;
        this.yOffset = yOffset;
        this.baselineShift = baselineShift;
    }

    /// <summary>Gets the glyph's zero-based visual index.</summary>
    /// <returns>The glyph order used by the encoded byte string.</returns>
    public int GetVisualIndex() { return visualIndex; }

    /// <summary>Gets the fixed-width PDF character code.</summary>
    /// <returns>The allocated one-byte or two-byte code value.</returns>
    public int GetCharacterCode() { return characterCode; }

    /// <summary>Gets the allocated document CID.</summary>
    /// <returns>The CID selected by the character code.</returns>
    public int GetCid() { return cid; }

    /// <summary>Gets the source-font glyph identifier.</summary>
    /// <returns>The original glyph identifier.</returns>
    public int GetSourceGlyphId() { return sourceGlyphId; }

    /// <summary>Gets the original cluster's UTF-16 start.</summary>
    /// <returns>The zero-based start relative to the shaped run.</returns>
    public int GetUtf16Start() { return utf16Start; }

    /// <summary>Gets the original cluster's UTF-16 length.</summary>
    /// <returns>The cluster length in UTF-16 code units.</returns>
    public int GetUtf16Length() { return utf16Length; }

    /// <summary>Gets the shaped horizontal advance.</summary>
    /// <returns>The x advance in PDF 1000-unit text space.</returns>
    public double GetXAdvance() { return xAdvance; }

    /// <summary>Gets the shaped vertical advance.</summary>
    /// <returns>The y advance in PDF 1000-unit text space.</returns>
    public double GetYAdvance() { return yAdvance; }

    /// <summary>Gets the shaped horizontal placement offset.</summary>
    /// <returns>The x offset in PDF 1000-unit text space.</returns>
    public double GetXOffset() { return xOffset; }

    /// <summary>Gets the shaped vertical placement offset.</summary>
    /// <returns>The y offset in PDF 1000-unit text space.</returns>
    public double GetYOffset() { return yOffset; }

    /// <summary>Gets the requested shift perpendicular to the baseline.</summary>
    /// <returns>The baseline shift in caller/user-space points.</returns>
    public double GetBaselineShift() { return baselineShift; }

    /// <summary>Reports whether the glyph needs explicit placement.</summary>
    /// <returns>True for a shaping offset or nonzero baseline shift.</returns>
    public bool HasExplicitPlacement()
    {
        return xOffset != 0 || yOffset != 0 || baselineShift != 0;
    }
}
