namespace pptx;

/// <summary>
/// Provides read-only compatibility inspection for presentation packages and documents.
/// </summary>
public class PptxCompatibilityInspector : IPptxCompatibilityInspector
{
    /// <summary>
    /// Creates a compatibility inspector instance.
    /// </summary>
    public PptxCompatibilityInspector()
    {
    }

    /// <summary>
    /// Inspects package features without changing package content.
    /// </summary>
    /// <param name="package">The package to inspect.</param>
    /// <returns>The grouped compatibility inspection.</returns>
    public PptxCompatibilityInspection Inspect(PptxPackage package)
    {
        return PptxCompatibilityInspectorCore.Inspect(package);
    }

    /// <summary>
    /// Inspects document features without changing document content.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The grouped compatibility inspection.</returns>
    public PptxCompatibilityInspection InspectDocument(PptxDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        return PptxCompatibilityInspectorCore.Inspect(document.GetPackage());
    }
}
