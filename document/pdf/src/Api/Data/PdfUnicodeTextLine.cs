namespace pdf;

/// <summary>
/// Contains one visually ordered Unicode line and its measured font runs.
/// </summary>
public class PdfUnicodeTextLine
{
    /// <summary>
    /// Stores the line's visually ordered text.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the visually ordered font runs.
    /// </summary>
    public PdfUnicodeGlyphRun[] Runs;
    /// <summary>
    /// Stores the measured line width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Stores the measured line height in PDF points.
    /// </summary>
    public double Height;

    internal PdfUnicodeTextLine(string text, PdfUnicodeGlyphRun[] runs, double width, double height)
    {
        Text = text;
        Runs = runs;
        Width = width;
        Height = height;
    }
}
