namespace pptx;

/// <summary>
/// Defines operations for slide master parts.
/// </summary>
public interface IPptxSlideMasterOperations
{
    /// <summary>
    /// Returns the editable slide-master shape tree.
    /// </summary>
    /// <param name="target">The slide master to inspect or edit.</param>
    /// <returns>The editable part shape tree.</returns>
    PptxPartShapeTree GetShapeTree(PptxSlideMaster target);
}
