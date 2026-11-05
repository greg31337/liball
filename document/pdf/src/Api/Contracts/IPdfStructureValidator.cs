namespace pdf;

/// <summary>
/// Defines byte-level and document-level PDF structure validation.
/// </summary>
public interface IPdfStructureValidator
{
    /// <summary>Validates a complete PDF byte array.</summary>
    /// <param name="pdfData">The complete PDF bytes.</param>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidateBytes(byte[] pdfData);

    /// <summary>Serializes and validates a document when its model is saveable.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidateDocument(PdfDocument document);
}
