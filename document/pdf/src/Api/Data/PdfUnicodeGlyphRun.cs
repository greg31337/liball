namespace pdf;

/// <summary>
/// Contains one visually ordered run drawn with a single font, size, direction, and baseline shift.
/// </summary>
public class PdfUnicodeGlyphRun
{
    /// <summary>
    /// Stores the loaded font selected by the fallback chain.
    /// </summary>
    public PdfTrueTypeFont Font;
    /// <summary>
    /// Stores the visually ordered Unicode text in this run.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the run font size in PDF points.
    /// </summary>
    public double FontSize;
    /// <summary>
    /// Stores the run's baseline shift in PDF points.
    /// </summary>
    public double BaselineShift;
    /// <summary>
    /// Stores the measured run width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Indicates whether the run originated at an odd bidirectional embedding level.
    /// </summary>
    public bool RightToLeft;
    /// <summary>
    /// Indicates whether the run represents a footnote marker.
    /// </summary>
    public bool FootnoteMarker;

    internal PdfUnicodeGlyphRun(PdfTrueTypeFont font, string text, double fontSize, double baselineShift, double width, bool rightToLeft, bool footnoteMarker)
    {
        Font = font;
        Text = text;
        FontSize = fontSize;
        BaselineShift = baselineShift;
        Width = width;
        RightToLeft = rightToLeft;
        FootnoteMarker = footnoteMarker;
    }
}
