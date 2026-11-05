namespace ttf;

/// <summary>Suggests one numeric adjustment for a PDF <c>TJ</c> array.</summary>
public class PdfTjAdjustment : IPdfTjAdjustment
{
    int afterGlyphIndex;
    double value;

    internal PdfTjAdjustment(int afterGlyphIndex, double value)
    {
        this.afterGlyphIndex = afterGlyphIndex;
        this.value = value;
    }

    /// <summary>Gets the visual glyph after which to place the number.</summary>
    /// <returns>The zero-based visual glyph index.</returns>
    public int GetAfterGlyphIndex() { return afterGlyphIndex; }

    /// <summary>Gets the suggested PDF <c>TJ</c> number.</summary>
    /// <remarks>
    /// The value is the nominal font advance minus the shaped advance in PDF
    /// 1000-unit text space. A positive horizontal value reduces the advance.
    /// </remarks>
    /// <returns>A finite nonzero adjustment.</returns>
    public double GetValue() { return value; }
}
