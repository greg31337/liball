namespace pdf;

/// <summary>
/// Specifies whether a tagged table header applies to rows, columns, or both.
/// </summary>
public enum PdfTableHeaderScope
{
    /// <summary>The header identifies a row.</summary>
    Row = 1,
    /// <summary>The header identifies a column.</summary>
    Column = 2,
    /// <summary>The header identifies both rows and columns.</summary>
    Both = 3
}
