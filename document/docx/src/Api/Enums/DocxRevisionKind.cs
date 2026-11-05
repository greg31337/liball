namespace docx;

/// <summary>
/// Identifies the semantic kind of a tracked revision.
/// </summary>
public enum DocxRevisionKind
{
    /// <summary>
    /// Inserted content.
    /// </summary>
    Inserted = 0,
    /// <summary>
    /// Deleted content.
    /// </summary>
    Deleted = 1,
    /// <summary>
    /// Content at the source of a move.
    /// </summary>
    MoveFrom = 2,
    /// <summary>
    /// Content at the destination of a move.
    /// </summary>
    MoveTo = 3,
    /// <summary>
    /// A run, paragraph, table, row, cell, or section formatting change.
    /// </summary>
    FormattingChange = 4
}
