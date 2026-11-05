namespace pdf;

/// <summary>
/// Checks whether a document contains the metadata and reading-order information needed before accessibility work.
/// </summary>
public interface IPdfAccessibilityPreparationChecker
{
    /// <summary>
    /// Checks language metadata, reading-order nodes, descriptions, links, annotations, and form fields.
    /// </summary>
    /// <param name="document">The PDF document to inspect.</param>
    /// <returns>The resulting accessibility preparation result.</returns>
    PdfAccessibilityPreparationResult Check(PdfDocument document);
}
