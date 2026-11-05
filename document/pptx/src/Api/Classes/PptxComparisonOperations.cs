namespace pptx;

/// <summary>
/// Provides high-level package-manifest and editable-model comparison workflows.
/// </summary>
public class PptxComparisonOperations : IPptxComparisonOperations
{
    /// <summary>
    /// Creates a comparison operations instance.
    /// </summary>
    public PptxComparisonOperations()
    {
    }

    /// <summary>
    /// Compares two documents without changing either document.
    /// </summary>
    /// <param name="left">The left document to compare.</param>
    /// <param name="right">The right document to compare.</param>
    /// <returns>The combined presentation comparison.</returns>
    public PptxPresentationComparison CompareDocuments(PptxDocument left, PptxDocument right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        return PptxComparisonWorkflowCore.ComparePackages(left.GetPackage(), right.GetPackage());
    }

    /// <summary>
    /// Loads and compares two presentation sources without changing their supplied models.
    /// </summary>
    /// <param name="left">The left presentation source.</param>
    /// <param name="right">The right presentation source.</param>
    /// <returns>The combined presentation comparison.</returns>
    public PptxPresentationComparison CompareSources(IPptxPresentationSource left, IPptxPresentationSource right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        PptxPresentation leftPresentation = left.LoadPresentation();
        PptxPresentation rightPresentation = right.LoadPresentation();
        if (leftPresentation == null || rightPresentation == null)
        {
            throw new System.ArgumentException("Presentation sources must return a presentation.");
        }

        return PptxComparisonWorkflowCore.ComparePackages(leftPresentation.Package, rightPresentation.Package);
    }
}
