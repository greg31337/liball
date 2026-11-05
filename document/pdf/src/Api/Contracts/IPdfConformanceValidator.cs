namespace pdf;

/// <summary>
/// Defines PDF/A conformance and PDF/UA preparation validation.
/// </summary>
public interface IPdfConformanceValidator
{
    /// <summary>Validates PDF/A preparation and emitted archival structures.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidatePdfA(PdfDocument document);

    /// <summary>Validates PDF/UA preparation and emitted tagged structures.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidatePdfUa(PdfDocument document);
}
