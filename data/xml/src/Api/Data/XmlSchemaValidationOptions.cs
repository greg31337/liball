namespace xml;

/// <summary>
/// Carries XML Schema validation and annotation choices.
/// </summary>
public class XmlSchemaValidationOptions
{
    /// <summary>
    /// The top-level and wildcard processing default.
    /// </summary>
    public XmlSchemaProcessContents ProcessContents;
    /// <summary>
    /// Whether validation stops after the first error.
    /// </summary>
    public bool FailFast;
    /// <summary>
    /// Whether PSVI type information is attached to the tree.
    /// </summary>
    public bool AnnotateTree;
    /// <summary>
    /// Whether schema location hints may be resolved.
    /// </summary>
    public bool UseLocationHints;
    /// <summary>
    /// Whether schema default and fixed values are applied.
    /// </summary>
    public bool ApplyDefaultValues;
    /// <summary>
    /// The schema resource policy.
    /// </summary>
    public XmlResolverPolicy? ResolverPolicy;
    /// <summary>
    /// The schema processing limits.
    /// </summary>
    public XmlSecurityLimits? SecurityLimits;
    /// <summary>
    /// The schema set to use for validation.
    /// </summary>
    public XmlSchemaSet? SchemaSet;
}
