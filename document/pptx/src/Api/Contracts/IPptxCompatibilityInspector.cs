namespace pptx;

/// <summary>
/// Defines read-only compatibility inspection for presentation packages and documents.
/// </summary>
public interface IPptxCompatibilityInspector
{
    /// <summary>
    /// Inspects package features without changing package content.
    /// </summary>
    /// <param name="package">The package to inspect.</param>
    /// <returns>The grouped compatibility inspection.</returns>
    PptxCompatibilityInspection Inspect(PptxPackage package);
    /// <summary>
    /// Inspects document features without changing document content.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The grouped compatibility inspection.</returns>
    PptxCompatibilityInspection InspectDocument(PptxDocument document);
}
