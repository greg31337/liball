namespace pptx;

/// <summary>
/// Provides operations for slide layout parts.
/// </summary>
public class PptxSlideLayoutOperations : IPptxSlideLayoutOperations
{
    /// <summary>
    /// Creates a slide-layout operations instance.
    /// </summary>
    public PptxSlideLayoutOperations()
    {
    }
    /// <summary>
    /// Returns the editable slide-layout shape tree.
    /// </summary>
    /// <param name="target">The slide layout to inspect or edit.</param>
    /// <returns>The editable part shape tree.</returns>
    public PptxPartShapeTree GetShapeTree(PptxSlideLayout target)
    {
        return target.GetShapeTree();
    }
}
