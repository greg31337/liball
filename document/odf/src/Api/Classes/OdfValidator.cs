namespace odf;

/// <summary>
/// Provides configurable package, XML, semantic, version, and profile validation.
/// </summary>
public class OdfValidator : IOdfValidator
{
    /// <summary>
    /// Initializes a document validator.
    /// </summary>
    public OdfValidator()
    {
    }

    /// <summary>
    /// Validates a document using the supplied options.
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The collected diagnostics.</returns>
    public OdfDiagnostic[] ValidateDocument(
        OdfDocument document,
        OdfValidationOptions options)
    {
        return new OdfValidationPipeline(options)
            .ValidateDocument(document).Diagnostics;
    }
}
