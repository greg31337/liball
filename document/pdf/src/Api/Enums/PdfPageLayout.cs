namespace pdf;

/// <summary>
/// Specifies the initial arrangement of pages in a PDF viewer.
/// </summary>
public enum PdfPageLayout
{
    /// <summary>
    /// Displays one page at a time.
    /// </summary>
    SinglePage = 0,
    /// <summary>
    /// Displays pages in one continuous column.
    /// </summary>
    OneColumn = 1,
    /// <summary>
    /// Displays two columns with odd-numbered pages on the left.
    /// </summary>
    TwoColumnLeft = 2,
    /// <summary>
    /// Displays two columns with odd-numbered pages on the right.
    /// </summary>
    TwoColumnRight = 3,
    /// <summary>
    /// Displays two pages at a time with odd-numbered pages on the left; requires PDF 1.5 or later.
    /// </summary>
    TwoPageLeft = 4,
    /// <summary>
    /// Displays two pages at a time with odd-numbered pages on the right; requires PDF 1.5 or later.
    /// </summary>
    TwoPageRight = 5
}
