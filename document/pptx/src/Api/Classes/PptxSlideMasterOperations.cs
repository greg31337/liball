namespace pptx;

/// <summary>
/// Provides operations for slide master parts.
/// </summary>
public class PptxSlideMasterOperations : IPptxSlideMasterOperations
{
    /// <summary>
    /// Creates a slide-master operations instance.
    /// </summary>
    public PptxSlideMasterOperations()
    {
    }
    /// <summary>
    /// Returns the editable slide-master shape tree.
    /// </summary>
    /// <param name="target">The slide master to inspect or edit.</param>
    /// <returns>The editable part shape tree.</returns>
    public PptxPartShapeTree GetShapeTree(PptxSlideMaster target)
    {
        return target.GetShapeTree();
    }
}
