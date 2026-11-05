namespace odf;

/// <summary>
/// Identifies a named text point or range family.
/// </summary>
public enum OdfTextRangeKind
{
    /// <summary>
    /// The range kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A bookmark.
    /// </summary>
    Bookmark = 1,
    /// <summary>
    /// A reference mark.
    /// </summary>
    ReferenceMark = 2,
    /// <summary>
    /// A table-of-contents index mark.
    /// </summary>
    TableOfContentMark = 3,
    /// <summary>
    /// A user-defined index mark.
    /// </summary>
    UserIndexMark = 4,
    /// <summary>
    /// An alphabetical-index mark.
    /// </summary>
    AlphabeticalIndexMark = 5,
    /// <summary>
    /// A bibliography mark.
    /// </summary>
    BibliographyMark = 6
}
