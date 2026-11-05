namespace pptx;

/// <summary>
/// Provides public operations for handout master parts.
/// </summary>
public class PptxHandoutMasterOperations : IPptxHandoutMasterOperations
{
    /// <summary>
    /// Creates a handout-master operations instance.
    /// </summary>
    public PptxHandoutMasterOperations()
    {
    }
    /// <summary>
    /// Returns common part and display metadata.
    /// </summary>
    /// <param name="target">The handout master to inspect.</param>
    /// <returns>The metadata snapshot.</returns>
    public PptxPartMetadata GetMetadata(PptxHandoutMaster target)
    {
        return target.GetMetadata();
    }
    /// <summary>
    /// Returns the editable handout-master shape tree.
    /// </summary>
    /// <param name="target">The handout master to inspect.</param>
    /// <returns>The shape-tree snapshot.</returns>
    public PptxPartShapeTree GetShapeTree(PptxHandoutMaster target)
    {
        return target.GetShapeTree();
    }
    /// <summary>
    /// Returns the related theme.
    /// </summary>
    /// <param name="target">The handout master to inspect.</param>
    /// <returns>The related theme, or <c>null</c> when none exists.</returns>
    public PptxTheme? GetTheme(PptxHandoutMaster target)
    {
        return target.GetTheme();
    }
}
