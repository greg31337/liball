namespace xml;

/// <summary>
/// Carries XML parser conformance, validation, preservation, and security choices.
/// </summary>
public class XmlParserOptions
{
    /// <summary>
    /// The XML version to require or assume.
    /// </summary>
    public XmlVersion Version;
    /// <summary>
    /// The namespace processing mode.
    /// </summary>
    public XmlNamespaceHandling NamespaceHandling;
    /// <summary>
    /// The DTD processing mode.
    /// </summary>
    public XmlDtdProcessing DtdProcessing;
    /// <summary>
    /// The entity handling mode.
    /// </summary>
    public XmlEntityHandling EntityHandling;
    /// <summary>
    /// The validation mode.
    /// </summary>
    public XmlValidationMode ValidationMode;
    /// <summary>
    /// Whether to parse a balanced fragment instead of a document.
    /// </summary>
    public bool ParseFragment;
    /// <summary>
    /// Whether to preserve CDATA boundaries.
    /// </summary>
    public bool PreserveCData;
    /// <summary>
    /// Whether to preserve entity reference nodes and boundaries.
    /// </summary>
    public bool PreserveEntityReferences;
    /// <summary>
    /// Whether to preserve comments.
    /// </summary>
    public bool PreserveComments;
    /// <summary>
    /// Whether to preserve processing instructions.
    /// </summary>
    public bool PreserveProcessingInstructions;
    /// <summary>
    /// Whether to preserve lexical prefix choices.
    /// </summary>
    public bool PreservePrefixes;
    /// <summary>
    /// Whether to preserve ignorable whitespace.
    /// </summary>
    public bool PreserveIgnorableWhitespace;
    /// <summary>
    /// Whether to retain source ranges.
    /// </summary>
    public bool PreserveSourceLocations;
    /// <summary>
    /// Whether namespace declaration attributes are retained.
    /// </summary>
    public bool RetainNamespaceDeclarations;
    /// <summary>
    /// The namespace bindings inherited by top-level elements in a parsed fragment.
    /// </summary>
    public XmlNamespaceBinding[]? FragmentNamespaceBindings;
    /// <summary>
    /// Whether validation diagnostics accumulate after validity errors.
    /// </summary>
    public bool AccumulateValidationDiagnostics;
    /// <summary>
    /// The processing security limits.
    /// </summary>
    public XmlSecurityLimits? SecurityLimits;
    /// <summary>
    /// The external resource policy.
    /// </summary>
    public XmlResolverPolicy? ResolverPolicy;
}
