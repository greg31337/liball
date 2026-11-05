namespace pdf;

/// <summary>
/// Contains the lines and aggregate size produced by Unicode text layout.
/// </summary>
public class PdfUnicodeTextResult
{
    /// <summary>
    /// Stores the laid-out lines in top-to-bottom order.
    /// </summary>
    public PdfUnicodeTextLine[] Lines;
    /// <summary>
    /// Stores the maximum measured line width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Stores the sum of measured line heights in PDF points.
    /// </summary>
    public double Height;

    internal PdfUnicodeTextResult(PdfUnicodeTextLine[] lines, double width, double height)
    {
        Lines = lines;
        Width = width;
        Height = height;
    }
}
