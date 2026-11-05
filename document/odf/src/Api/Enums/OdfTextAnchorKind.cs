namespace odf;

/// <summary>
/// Identifies how a drawing frame is anchored in text content.
/// </summary>
public enum OdfTextAnchorKind
{
    /// <summary>
    /// The anchor value is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The frame is anchored to a paragraph.
    /// </summary>
    Paragraph = 1,
    /// <summary>
    /// The frame is anchored to a character.
    /// </summary>
    Character = 2,
    /// <summary>
    /// The frame participates inline as a character.
    /// </summary>
    AsCharacter = 3,
    /// <summary>
    /// The frame is anchored to a page.
    /// </summary>
    Page = 4,
    /// <summary>
    /// The frame is anchored to another frame.
    /// </summary>
    Frame = 5,
    /// <summary>
    /// The frame is anchored to a table cell.
    /// </summary>
    Cell = 6
}
