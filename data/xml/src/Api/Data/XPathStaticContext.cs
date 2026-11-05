namespace xml;

/// <summary>
/// Carries the static context used to compile an XPath expression.
/// </summary>
public class XPathStaticContext
{
    internal bool FunctionsArePrevalidated;
    /// <summary>
    /// The XPath language version.
    /// </summary>
    public XPathLanguageVersion LanguageVersion;
    /// <summary>
    /// The compatibility mode.
    /// </summary>
    public XPathCompatibilityMode CompatibilityMode;
    /// <summary>
    /// The statically known namespace bindings.
    /// </summary>
    public XmlNamespaceBinding[]? Namespaces;
    /// <summary>
    /// The default element and type namespace.
    /// </summary>
    public string? DefaultElementNamespace;
    /// <summary>
    /// The default function namespace.
    /// </summary>
    public string? DefaultFunctionNamespace;
    /// <summary>
    /// The static base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The default collation URI.
    /// </summary>
    public string? DefaultCollation;
    /// <summary>
    /// The available collation URIs.
    /// </summary>
    public string[]? Collations;
    /// <summary>
    /// The available collation implementations keyed by URI.
    /// </summary>
    public XPathCollationBinding[]? CollationBindings;
    /// <summary>
    /// The statically known variables.
    /// </summary>
    public XPathVariableBinding[]? Variables;
    /// <summary>
    /// The statically known functions.
    /// </summary>
    public XPathFunctionBinding[]? Functions;
    /// <summary>
    /// The resolver used to bind registered function implementations during compilation.
    /// </summary>
    public IXPathFunctionResolver? FunctionResolver;
    /// <summary>
    /// The trust and invocation policy for registered functions and collations.
    /// </summary>
    public XmlExtensionPolicy? ExtensionPolicy;
    /// <summary>
    /// The schema set used for schema-aware compilation.
    /// </summary>
    public XmlSchemaSet? SchemaSet;
    /// <summary>
    /// The available decimal format names.
    /// </summary>
    public XmlName[]? DecimalFormats;
    /// <summary>
    /// The symbol declarations for default and named decimal formats.
    /// </summary>
    public XPathDecimalFormat[]? DecimalFormatDeclarations;
    /// <summary>
    /// The enabled implementation-defined features.
    /// </summary>
    public string[]? EnabledFeatures;
    /// <summary>
    /// The resource policy.
    /// </summary>
    public XmlResolverPolicy? ResolverPolicy;
    /// <summary>
    /// Whether schema-aware syntax and typing are enabled.
    /// </summary>
    public bool SchemaAware;
    /// <summary>
    /// Whether higher-order function features are enabled.
    /// </summary>
    public bool AllowHigherOrderFunctions;
}
