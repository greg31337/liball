namespace pptx;

/// <summary>
/// Describes the effective slide background and the presentation part that supplies it.
/// </summary>
public class PptxInheritedBackground
{
    /// <summary>
    /// Stores the effective background fill.
    /// </summary>
    public PptxFill Fill;
    /// <summary>
    /// Stores the part name that supplies the fill, or an empty value when no background is declared.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// Stores <c>slide</c>, <c>layout</c>, <c>master</c>, or an empty value when no background is declared.
    /// </summary>
    public string SourceLevel;
    /// <summary>
    /// Indicates whether the background comes from a layout or master.
    /// </summary>
    public bool Inherited;

    /// <summary>
    /// Initializes an empty effective background description.
    /// </summary>
    public PptxInheritedBackground()
    {
        Fill = new PptxFill();
        SourcePartName = "";
        SourceLevel = "";
        Inherited = false;
    }
}
