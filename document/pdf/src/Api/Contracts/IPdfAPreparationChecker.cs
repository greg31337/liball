namespace pdf;

/// <summary>
/// Checks whether a document is prepared for later conversion to a PDF/A archival profile.
/// </summary>
public interface IPdfAPreparationChecker
{
    /// <summary>
    /// Checks document structure, metadata, security, embedded content, fonts, forms, and annotations for known PDF/A preparation concerns.
    /// </summary>
    /// <param name="document">The PDF document to inspect.</param>
    /// <returns>A result containing every error and warning found by the preparation checks.</returns>
    PdfAPreparationResult Check(PdfDocument document);
}
