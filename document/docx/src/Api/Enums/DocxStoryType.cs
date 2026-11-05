namespace docx;

/// <summary>
/// Identifies a WordprocessingML text story discovered through package relationships.
/// </summary>
public enum DocxStoryType
{
    /// <summary>
    /// The main document body story.
    /// </summary>
    MainDocument = 0,
    /// <summary>
    /// A header story.
    /// </summary>
    Header = 1,
    /// <summary>
    /// A footer story.
    /// </summary>
    Footer = 2,
    /// <summary>
    /// The footnotes story.
    /// </summary>
    Footnotes = 3,
    /// <summary>
    /// The endnotes story.
    /// </summary>
    Endnotes = 4,
    /// <summary>
    /// The comments story.
    /// </summary>
    Comments = 5
}
