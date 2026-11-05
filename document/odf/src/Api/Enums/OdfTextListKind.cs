namespace odf;

/// <summary>
/// Identifies the effective label mechanism of a document list.
/// </summary>
public enum OdfTextListKind
{
    /// <summary>
    /// The list style or level is unavailable.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// An ordered numeric or alphabetic list.
    /// </summary>
    Numbered = 1,
    /// <summary>
    /// An unordered character-bullet list.
    /// </summary>
    Bullet = 2,
    /// <summary>
    /// An unordered image-bullet list.
    /// </summary>
    Image = 3,
    /// <summary>
    /// An outline-numbering list.
    /// </summary>
    Outline = 4
}
