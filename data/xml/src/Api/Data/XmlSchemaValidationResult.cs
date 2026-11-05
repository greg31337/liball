namespace xml;

/// <summary>
/// Carries an XML Schema validation outcome and annotations.
/// </summary>
public class XmlSchemaValidationResult
{
    /// <summary>
    /// The validated document.
    /// </summary>
    public XmlDocument? Document;
    /// <summary>
    /// The schema set used for validation.
    /// </summary>
    public XmlSchemaSet? SchemaSet;
    /// <summary>
    /// The validation options.
    /// </summary>
    public XmlSchemaValidationOptions? Options;
    /// <summary>
    /// The overall validity.
    /// </summary>
    public XmlSchemaValidity Validity;
    /// <summary>
    /// The produced type annotations.
    /// </summary>
    public XmlSchemaTypeInfo[]? TypeAnnotations;
    /// <summary>
    /// The validation diagnostics.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// Whether validation completed without a fatal processing failure.
    /// </summary>
    public bool Succeeded;
}

