namespace docx;

/// <summary>
/// Identifies a default, even-page, or first-page header or footer reference.
/// </summary>
public enum DocxHeaderFooterKind
{
    /// <summary>
    /// The header or footer used by default pages.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The header or footer used by even pages when even-page stories are enabled.
    /// </summary>
    Even = 1,

    /// <summary>
    /// The header or footer used by the first page when first-page stories are enabled.
    /// </summary>
    First = 2
}
