namespace pptx;

/// <summary>
/// Represents one horizontal or vertical editing guide in the presentation slide view.
/// </summary>
public class PptxPresentationGuide
{
    /// <summary>
    /// Stores <c>horz</c> for a horizontal guide or <c>vert</c> for a vertical guide.
    /// </summary>
    public string Orientation;
    /// <summary>
    /// Stores the guide position in the units used by PresentationML view properties.
    /// </summary>
    public int Position;

    /// <summary>
    /// Initializes an empty presentation guide.
    /// </summary>
    public PptxPresentationGuide()
    {
        Orientation = "horz";
        Position = 0;
    }
}
