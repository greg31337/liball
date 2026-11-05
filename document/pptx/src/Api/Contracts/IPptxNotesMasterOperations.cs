namespace pptx;

/// <summary>
/// Defines operations exposed for notes master parts.
/// </summary>
public interface IPptxNotesMasterOperations
{
    /// <summary>
    /// Returns common part and display metadata.
    /// </summary>
    /// <param name="target">The notes master to inspect.</param>
    /// <returns>The metadata snapshot.</returns>
    PptxPartMetadata GetMetadata(PptxNotesMaster target);
    /// <summary>
    /// Returns the editable notes-master shape tree.
    /// </summary>
    /// <param name="target">The notes master to inspect.</param>
    /// <returns>The shape-tree snapshot.</returns>
    PptxPartShapeTree GetShapeTree(PptxNotesMaster target);
    /// <summary>
    /// Returns the related theme.
    /// </summary>
    /// <param name="target">The notes master to inspect.</param>
    /// <returns>The related theme, or <c>null</c> when none exists.</returns>
    PptxTheme? GetTheme(PptxNotesMaster target);
}
