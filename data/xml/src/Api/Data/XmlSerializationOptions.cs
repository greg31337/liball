namespace xml;

/// <summary>
/// Carries Serialization 3.1 parameters and XML output controls.
/// </summary>
public class XmlSerializationOptions
{
    internal XmlExtensionRuntimeState? ExtensionState;
    /// <summary>
    /// The serialization method.
    /// </summary>
    public XmlSerializationMethod Method;
    /// <summary>
    /// The expanded name of a caller-provided method.
    /// </summary>
    public XmlName? ExtensionMethodName;
    /// <summary>
    /// The registered extension serialization methods.
    /// </summary>
    public XmlSerializationMethodBinding[]? ExtensionMethods;
    /// <summary>
    /// The trust and invocation policy for extension serialization methods.
    /// </summary>
    public XmlExtensionPolicy? ExtensionPolicy;
    /// <summary>
    /// The allow-duplicate-names parameter.
    /// </summary>
    public bool AllowDuplicateNames;
    /// <summary>
    /// The byte-order-mark parameter.
    /// </summary>
    public bool ByteOrderMark;
    /// <summary>
    /// The cdata-section-elements parameter.
    /// </summary>
    public XmlName[]? CDataSectionElements;
    /// <summary>
    /// The doctype-public parameter.
    /// </summary>
    public string? DoctypePublic;
    /// <summary>
    /// The doctype-system parameter.
    /// </summary>
    public string? DoctypeSystem;
    /// <summary>
    /// The encoding parameter.
    /// </summary>
    public string? Encoding;
    /// <summary>
    /// The escape-uri-attributes parameter.
    /// </summary>
    public bool EscapeUriAttributes;
    /// <summary>
    /// The html-version parameter.
    /// </summary>
    public string? HtmlVersion;
    /// <summary>
    /// The include-content-type parameter.
    /// </summary>
    public bool IncludeContentType;
    /// <summary>
    /// The indent parameter.
    /// </summary>
    public bool Indent;
    /// <summary>
    /// The item-separator parameter.
    /// </summary>
    public string? ItemSeparator;
    /// <summary>
    /// The json-node-output-method parameter.
    /// </summary>
    public XmlSerializationMethod JsonNodeOutputMethod;
    /// <summary>
    /// The media-type parameter.
    /// </summary>
    public string? MediaType;
    /// <summary>
    /// The normalization-form parameter.
    /// </summary>
    public string? NormalizationForm;
    /// <summary>
    /// The omit-xml-declaration parameter.
    /// </summary>
    public bool OmitXmlDeclaration;
    /// <summary>
    /// The standalone parameter.
    /// </summary>
    public XmlStandalone Standalone;
    /// <summary>
    /// The suppress-indentation parameter.
    /// </summary>
    public XmlName[]? SuppressIndentation;
    /// <summary>
    /// The undeclare-prefixes parameter.
    /// </summary>
    public bool UndeclarePrefixes;
    /// <summary>
    /// The effective output character map entries.
    /// </summary>
    public XmlCharacterMapEntry[]? UseCharacterMaps;
    /// <summary>
    /// The version parameter.
    /// </summary>
    public string? Version;
    /// <summary>
    /// The policy for characters that the selected output method cannot represent.
    /// </summary>
    public XmlInvalidCharacterPolicy InvalidCharacterPolicy;
    /// <summary>
    /// The replacement used by the replacement policy.
    /// </summary>
    public string? InvalidCharacterReplacement;
    /// <summary>
    /// The newline sequence used for indentation.
    /// </summary>
    public string? NewLine;
}
