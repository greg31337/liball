namespace odf;

/// <summary>
/// Validates an OpenDocument document.
/// </summary>
public interface IOdfValidator
{
    /// <summary>
    /// Validates a document using the supplied options.
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The collected diagnostics.</returns>
    public OdfDiagnostic[] ValidateDocument(OdfDocument document, OdfValidationOptions options);
}
