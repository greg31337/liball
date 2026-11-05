namespace pptx;

/// <summary>
/// Defines high-level package-manifest and editable-model comparison workflows.
/// </summary>
public interface IPptxComparisonOperations
{
    /// <summary>
    /// Compares two documents without changing either document.
    /// </summary>
    /// <param name="left">The left document to compare.</param>
    /// <param name="right">The right document to compare.</param>
    /// <returns>The combined presentation comparison.</returns>
    PptxPresentationComparison CompareDocuments(PptxDocument left, PptxDocument right);
    /// <summary>
    /// Loads and compares two presentation sources without changing their supplied models.
    /// </summary>
    /// <param name="left">The left presentation source.</param>
    /// <param name="right">The right presentation source.</param>
    /// <returns>The combined presentation comparison.</returns>
    PptxPresentationComparison CompareSources(IPptxPresentationSource left, IPptxPresentationSource right);
}
