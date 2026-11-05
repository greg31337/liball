namespace ttf;

/// <summary>Contains PDF character codes and glyph mappings for one shaped run.</summary>
public class PdfEncodedRun : IPdfEncodedRun
{
    byte[] codes;
    int codeByteCount;
    int[] cids;
    int[] sourceGlyphIds;
    PdfWritingMode writingMode;
    PdfPositionedGlyph[] glyphs;
    PdfTjAdjustment[] textAdjustments;
    double xAdvance;
    double yAdvance;
    double baselineShift;
    int sourceUtf16Length;

    internal PdfEncodedRun(
        byte[] codes,
        int codeByteCount,
        int[] cids,
        int[] sourceGlyphIds,
        PdfWritingMode writingMode,
        PdfPositionedGlyph[] glyphs,
        PdfTjAdjustment[] textAdjustments,
        double xAdvance,
        double yAdvance,
        double baselineShift,
        int sourceUtf16Length)
    {
        this.codes = CopyBytes(codes);
        this.codeByteCount = codeByteCount;
        this.cids = CopyIntegers(cids);
        this.sourceGlyphIds = CopyIntegers(sourceGlyphIds);
        this.writingMode = writingMode;
        this.glyphs = CopyGlyphs(glyphs);
        this.textAdjustments = CopyAdjustments(textAdjustments);
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.baselineShift = baselineShift;
        this.sourceUtf16Length = sourceUtf16Length;
    }

    /// <summary>Copies the encoded PDF character bytes in visual glyph order.</summary>
    /// <returns>One or two bytes per glyph according to the code width.</returns>
    public byte[] GetCodes()
    {
        return CopyBytes(codes);
    }

    /// <summary>Gets the fixed number of bytes used by each character code.</summary>
    /// <returns>One for a simple font or two for a Type 0 font.</returns>
    public int GetCodeByteCount()
    {
        return codeByteCount;
    }

    /// <summary>Gets the number of encoded glyphs.</summary>
    /// <returns>The glyph count.</returns>
    public int GetGlyphCount()
    {
        return cids.Length;
    }

    /// <summary>Gets one document CID.</summary>
    /// <param name="index">The zero-based visual glyph index.</param>
    /// <returns>The document CID represented by the character code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the run.
    /// </exception>
    public int GetCid(int index)
    {
        ValidateIndex(index);
        return cids[index];
    }

    /// <summary>Gets one source glyph identifier.</summary>
    /// <param name="index">The zero-based visual glyph index.</param>
    /// <returns>The glyph identifier in the source font instance.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the run.
    /// </exception>
    public int GetSourceGlyphId(int index)
    {
        ValidateIndex(index);
        return sourceGlyphIds[index];
    }

    /// <summary>Copies the document CIDs in visual glyph order.</summary>
    /// <returns>A new array containing one CID per glyph.</returns>
    public int[] GetCids()
    {
        return CopyIntegers(cids);
    }

    /// <summary>Copies the source glyph identifiers in visual order.</summary>
    /// <returns>A new array containing one source GID per glyph.</returns>
    public int[] GetSourceGlyphIds()
    {
        return CopyIntegers(sourceGlyphIds);
    }

    /// <summary>Gets the writing mode used by this encoded run.</summary>
    /// <returns>The horizontal or vertical writing mode.</returns>
    public PdfWritingMode GetWritingMode()
    {
        return writingMode;
    }

    /// <summary>Gets one positioned glyph in encoded visual order.</summary>
    /// <param name="index">The zero-based visual glyph index.</param>
    /// <returns>The immutable code, source range, and position data.</returns>
    public PdfPositionedGlyph GetGlyph(int index)
    {
        ValidateIndex(index);
        return glyphs[index];
    }

    /// <summary>Copies the positioned glyphs in encoded visual order.</summary>
    /// <returns>A new array containing immutable positioned glyphs.</returns>
    public PdfPositionedGlyph[] GetGlyphs()
    {
        return CopyGlyphs(glyphs);
    }

    /// <summary>Copies the positioned glyphs for text-matrix translation.</summary>
    /// <returns>A new array containing immutable positioned glyphs.</returns>
    public PdfPositionedGlyph[] GetPositions()
    {
        return CopyGlyphs(glyphs);
    }

    /// <summary>Gets the number of nonzero suggested <c>TJ</c> values.</summary>
    /// <returns>The text-adjustment count.</returns>
    public int GetTextAdjustmentCount()
    {
        return textAdjustments.Length;
    }

    /// <summary>Gets one suggested <c>TJ</c> adjustment.</summary>
    /// <param name="index">The zero-based adjustment index.</param>
    /// <returns>The immutable glyph index and numeric value.</returns>
    public PdfTjAdjustment GetTextAdjustment(int index)
    {
        if (index < 0 || index >= textAdjustments.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return textAdjustments[index];
    }

    /// <summary>Copies all suggested <c>TJ</c> adjustments.</summary>
    /// <returns>A new array in visual-glyph order.</returns>
    public PdfTjAdjustment[] GetTextAdjustments()
    {
        return CopyAdjustments(textAdjustments);
    }

    /// <summary>Gets the shaped run's total horizontal advance.</summary>
    /// <returns>The x advance in PDF 1000-unit text space.</returns>
    public double GetXAdvance() { return xAdvance; }

    /// <summary>Gets the shaped run's total vertical advance.</summary>
    /// <returns>The y advance in PDF 1000-unit text space.</returns>
    public double GetYAdvance() { return yAdvance; }

    /// <summary>Gets the run-wide shift perpendicular to the baseline.</summary>
    /// <returns>The baseline shift in caller/user-space points.</returns>
    public double GetBaselineShift() { return baselineShift; }

    /// <summary>Gets the source range start relative to the shaped run.</summary>
    /// <returns>Zero.</returns>
    public int GetSourceUtf16Start() { return 0; }

    /// <summary>Gets the shaped source range length.</summary>
    /// <returns>The source length in UTF-16 code units.</returns>
    public int GetSourceUtf16Length() { return sourceUtf16Length; }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= cids.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
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

    static PdfPositionedGlyph[] CopyGlyphs(PdfPositionedGlyph[] values)
    {
        PdfPositionedGlyph[] copy =
            new PdfPositionedGlyph[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static PdfTjAdjustment[] CopyAdjustments(PdfTjAdjustment[] values)
    {
        PdfTjAdjustment[] copy = new PdfTjAdjustment[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
