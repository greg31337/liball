namespace docx;

/// <summary>
/// Specifies the repair policy applied while loading a package.
/// </summary>
public enum DocxRepairMode
{
    /// <summary>
    /// Does not repair the loaded package.
    /// </summary>
    Disabled = 0,
    /// <summary>
    /// Repairs missing catalogs, relationships, and an empty document body.
    /// </summary>
    Safe = 1,
    /// <summary>
    /// Applies safe repairs and removes orphaned internal relationships.
    /// </summary>
    Aggressive = 2
}
