namespace pptx;

/// <summary>
/// Combines effective package-manifest and editable-model comparisons for two presentations.
/// </summary>
public class PptxPresentationComparison
{
    /// <summary>
    /// Stores the effective serialized package comparison.
    /// </summary>
    public PptxPackageManifestComparison Manifest;
    /// <summary>
    /// Stores the editable presentation-model comparison.
    /// </summary>
    public PptxEditableModelComparison EditableModel;
    /// <summary>
    /// Indicates whether both the package manifest and editable model are equal.
    /// </summary>
    public bool Equal;

    /// <summary>
    /// Initializes an empty presentation comparison.
    /// </summary>
    public PptxPresentationComparison()
    {
        Manifest = new PptxPackageManifestComparison();
        EditableModel = new PptxEditableModelComparison();
        Equal = true;
    }
}
