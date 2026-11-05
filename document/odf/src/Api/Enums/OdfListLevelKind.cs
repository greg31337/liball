namespace odf;

/// <summary>
/// Identifies the label mechanism for a list-style level.
/// </summary>
public enum OdfListLevelKind
{
    /// <summary>
    /// A numbered list level.
    /// </summary>
    Number = 0,
    /// <summary>
    /// A character-bullet list level.
    /// </summary>
    Bullet = 1,
    /// <summary>
    /// An image-bullet list level.
    /// </summary>
    Image = 2,
    /// <summary>
    /// An outline-numbering level.
    /// </summary>
    Outline = 3
}
