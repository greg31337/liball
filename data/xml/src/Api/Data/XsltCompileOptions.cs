namespace xml;

/// <summary>
/// Carries XSLT stylesheet and package compilation choices.
/// </summary>
public class XsltCompileOptions
{
    /// <summary>
    /// The requested conformance level.
    /// </summary>
    public XsltConformanceLevel ConformanceLevel;
    /// <summary>
    /// The static recovery policy.
    /// </summary>
    public XsltRecoveryPolicy RecoveryPolicy;
    /// <summary>
    /// The stylesheet base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The requested package name.
    /// </summary>
    public XmlName? PackageName;
    /// <summary>
    /// The package dependency references.
    /// </summary>
    public XsltPackageReference[]? Packages;
    /// <summary>
    /// The supplied static parameters.
    /// </summary>
    public XsltParameter[]? StaticParameters;
    /// <summary>
    /// The registered extension bindings.
    /// </summary>
    public XsltExtensionBinding[]? Extensions;
    /// <summary>
    /// The registered XPath collation bindings available to stylesheet expressions.
    /// </summary>
    public XPathCollationBinding[]? Collations;
    /// <summary>
    /// The registered XPath function bindings available to stylesheet expressions.
    /// </summary>
    public XPathFunctionBinding[]? Functions;
    /// <summary>
    /// The registered extension serialization methods.
    /// </summary>
    public XmlSerializationMethodBinding[]? SerializationMethods;
    /// <summary>
    /// The trust and invocation policy for registered stylesheet extensions.
    /// </summary>
    public XmlExtensionPolicy? ExtensionPolicy;
    /// <summary>
    /// The schema set for schema-aware compilation.
    /// </summary>
    public XmlSchemaSet? SchemaSet;
    /// <summary>
    /// The module and package resource policy.
    /// </summary>
    public XmlResolverPolicy? ResolverPolicy;
    /// <summary>
    /// The compilation security limits.
    /// </summary>
    public XmlSecurityLimits? SecurityLimits;
    /// <summary>
    /// Whether guaranteed streamability is analyzed.
    /// </summary>
    public bool AnalyzeStreamability;
    /// <summary>
    /// Whether packages retain separately reusable compiled handles.
    /// </summary>
    public bool CompilePackagesSeparately;
}
