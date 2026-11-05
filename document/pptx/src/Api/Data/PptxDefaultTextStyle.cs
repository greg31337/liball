namespace pptx;

/// <summary>
/// Represents the complete presentation default text style as a validated PresentationML element.
/// </summary>
public class PptxDefaultTextStyle
{
    /// <summary>
    /// Stores a complete <c>p:defaultTextStyle</c> XML element, or an empty string when no style is defined.
    /// </summary>
    public string Xml;

    /// <summary>
    /// Initializes an empty default text style.
    /// </summary>
    public PptxDefaultTextStyle()
    {
        Xml = "";
    }
}
