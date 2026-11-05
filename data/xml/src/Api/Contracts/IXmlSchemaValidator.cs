namespace xml;

/// <summary>
/// Validates and annotates XML documents using XML Schema.
/// </summary>
public interface IXmlSchemaValidator
{
    /// <summary>
    /// Validates one XML document.
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="schemaSet">The compiled schema set.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result.</returns>
    XmlSchemaValidationResult ValidateXmlSchema(XmlDocument document, XmlSchemaSet schemaSet, XmlSchemaValidationOptions options);

    /// <summary>
    /// Validates normalized XML events using a compiled schema set.
    /// </summary>
    /// <param name="events">The normalized events to validate.</param>
    /// <param name="schemaSet">The compiled schema set.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result.</returns>
    XmlSchemaValidationResult ValidateXmlSchemaEvents(XmlEventBatch events, XmlSchemaSet schemaSet, XmlSchemaValidationOptions options);
}
