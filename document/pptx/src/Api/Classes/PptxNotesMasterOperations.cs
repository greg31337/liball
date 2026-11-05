namespace pptx;

/// <summary>
/// Provides public operations for notes master parts.
/// </summary>
public class PptxNotesMasterOperations : IPptxNotesMasterOperations
{
    /// <summary>
    /// Creates a notes-master operations instance.
    /// </summary>
    public PptxNotesMasterOperations()
    {
    }
    /// <summary>
    /// Returns common part and display metadata.
    /// </summary>
    /// <param name="target">The notes master to inspect.</param>
    /// <returns>The metadata snapshot.</returns>
    public PptxPartMetadata GetMetadata(PptxNotesMaster target)
    {
        return target.GetMetadata();
    }
    /// <summary>
    /// Returns the editable notes-master shape tree.
    /// </summary>
    /// <param name="target">The notes master to inspect.</param>
    /// <returns>The shape-tree snapshot.</returns>
    public PptxPartShapeTree GetShapeTree(PptxNotesMaster target)
    {
        return target.GetShapeTree();
    }
    /// <summary>
    /// Returns the related theme.
    /// </summary>
    /// <param name="target">The notes master to inspect.</param>
    /// <returns>The related theme, or <c>null</c> when none exists.</returns>
    public PptxTheme? GetTheme(PptxNotesMaster target)
    {
        return target.GetTheme();
    }
}
