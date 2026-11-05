namespace pptx;

/// <summary>
/// Defines operations for slide layout parts.
/// </summary>
public interface IPptxSlideLayoutOperations
{
    /// <summary>
    /// Returns the editable slide-layout shape tree.
    /// </summary>
    /// <param name="target">The slide layout to inspect or edit.</param>
    /// <returns>The editable part shape tree.</returns>
    PptxPartShapeTree GetShapeTree(PptxSlideLayout target);
}
