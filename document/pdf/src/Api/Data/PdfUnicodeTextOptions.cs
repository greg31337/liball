namespace pdf;

/// <summary>
/// Contains sizing, direction, and script settings for Unicode text layout.
/// </summary>
public class PdfUnicodeTextOptions
{
    /// <summary>
    /// Stores the positive maximum line width in PDF points.
    /// </summary>
    public double MaxWidth;
    /// <summary>
    /// Stores the positive normal font size in PDF points.
    /// </summary>
    public double FontSize;
    /// <summary>
    /// Stores the minimum line height in PDF points, or zero to derive it from font metrics.
    /// </summary>
    public double LineHeight;
    /// <summary>
    /// Stores the paragraph's base text direction.
    /// </summary>
    public PdfTextDirection Direction;
    /// <summary>
    /// Stores the font-size multiplier used by superscript, subscript, and footnote markers.
    /// </summary>
    public double ScriptScale;
    /// <summary>
    /// Stores the superscript baseline rise as a multiplier of the normal font size.
    /// </summary>
    public double SuperscriptRise;
    /// <summary>
    /// Stores the subscript baseline drop as a multiplier of the normal font size.
    /// </summary>
    public double SubscriptDrop;

    /// <summary>
    /// Initializes Unicode text layout options with practical defaults.
    /// </summary>
    public PdfUnicodeTextOptions()
    {
        MaxWidth = 500;
        FontSize = 12;
        LineHeight = 0;
        Direction = PdfTextDirection.Auto;
        ScriptScale = 0.65;
        SuperscriptRise = 0.35;
        SubscriptDrop = 0.2;
    }
}
