namespace xml;

/// <summary>
/// Validates XML documents or normalized events against a retained DTD.
/// </summary>
public interface IDtdValidator
{
    /// <summary>
    /// Validates a document against its retained DTD.
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The validation and diagnostic options.</param>
    /// <returns>The validity diagnostics.</returns>
    XmlDiagnostic[] ValidateXmlDocumentDtd(XmlDocument document, XmlParserOptions options);

    /// <summary>
    /// Validates normalized events against a DTD.
    /// </summary>
    /// <param name="events">The normalized events.</param>
    /// <param name="documentType">The retained DTD.</param>
    /// <param name="options">The validation and diagnostic options.</param>
    /// <returns>The validity diagnostics.</returns>
    XmlDiagnostic[] ValidateXmlEventDtd(XmlEventBatch events, XmlDocumentType documentType, XmlParserOptions options);
}

