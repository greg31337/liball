namespace pptx;

/// <summary>
/// Describes one paragraph tab stop.
/// </summary>
public class PptxTextTab
{
    /// <summary>
    /// Stores the tab position in English Metric Units.
    /// </summary>
    public long Position;
    /// <summary>
    /// Stores the tab alignment such as <c>l</c>, <c>ctr</c>, <c>r</c>, or <c>dec</c>.
    /// </summary>
    public string Alignment;

    /// <summary>
    /// Initializes a left-aligned tab at the origin.
    /// </summary>
    public PptxTextTab()
    {
        Position = 0L;
        Alignment = "l";
    }
}
