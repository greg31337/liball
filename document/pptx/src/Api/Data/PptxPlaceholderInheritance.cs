namespace pptx;

/// <summary>
/// Describes the matching placeholder chain from a slide through its layout and master.
/// </summary>
public class PptxPlaceholderInheritance
{
    /// <summary>
    /// Stores the matching slide placeholder when one is declared; otherwise, <c>null</c>.
    /// </summary>
    public PptxPlaceholder? SlidePlaceholder;
    /// <summary>
    /// Stores the matching layout placeholder when one is declared; otherwise, <c>null</c>.
    /// </summary>
    public PptxPlaceholder? LayoutPlaceholder;
    /// <summary>
    /// Stores the matching master placeholder when one is declared; otherwise, <c>null</c>.
    /// </summary>
    public PptxPlaceholder? MasterPlaceholder;

    /// <summary>
    /// Initializes an empty placeholder inheritance description.
    /// </summary>
    public PptxPlaceholderInheritance()
    {
        SlidePlaceholder = null;
        LayoutPlaceholder = null;
        MasterPlaceholder = null;
    }
}
