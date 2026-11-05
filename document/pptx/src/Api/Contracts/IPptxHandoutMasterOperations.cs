namespace pptx;

/// <summary>
/// Defines operations exposed for handout master parts.
/// </summary>
public interface IPptxHandoutMasterOperations
{
    /// <summary>
    /// Returns common part and display metadata.
    /// </summary>
    /// <param name="target">The handout master to inspect.</param>
    /// <returns>The metadata snapshot.</returns>
    PptxPartMetadata GetMetadata(PptxHandoutMaster target);
    /// <summary>
    /// Returns the editable handout-master shape tree.
    /// </summary>
    /// <param name="target">The handout master to inspect.</param>
    /// <returns>The shape-tree snapshot.</returns>
    PptxPartShapeTree GetShapeTree(PptxHandoutMaster target);
    /// <summary>
    /// Returns the related theme.
    /// </summary>
    /// <param name="target">The handout master to inspect.</param>
    /// <returns>The related theme, or <c>null</c> when none exists.</returns>
    PptxTheme? GetTheme(PptxHandoutMaster target);
}
