namespace odf;

/// <summary>
/// Identifies an inline or style-driven text-flow break.
/// </summary>
public enum OdfTextBreakKind
{
    /// <summary>
    /// The break value is unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// An explicit line break.
    /// </summary>
    Line = 1,
    /// <summary>
    /// A soft page-break hint.
    /// </summary>
    SoftPage = 2,
    /// <summary>
    /// A page break before a text block.
    /// </summary>
    PageBefore = 3,
    /// <summary>
    /// A page break after a text block.
    /// </summary>
    PageAfter = 4,
    /// <summary>
    /// A column break before a text block.
    /// </summary>
    ColumnBefore = 5,
    /// <summary>
    /// A column break after a text block.
    /// </summary>
    ColumnAfter = 6,
    /// <summary>
    /// An even-page break before a text block.
    /// </summary>
    EvenPageBefore = 7,
    /// <summary>
    /// An even-page break after a text block.
    /// </summary>
    EvenPageAfter = 8,
    /// <summary>
    /// An odd-page break before a text block.
    /// </summary>
    OddPageBefore = 9,
    /// <summary>
    /// An odd-page break after a text block.
    /// </summary>
    OddPageAfter = 10
}
