namespace odf;

/// <summary>
/// Represents an XML expanded name together with its preferred lexical prefix.
/// </summary>
public class OdfQualifiedName
{
    /// <summary>
    /// The namespace name, or an empty string for no namespace.
    /// </summary>
    public string NamespaceName = "";
    /// <summary>
    /// The namespace-local name.
    /// </summary>
    public string LocalName = "";
    /// <summary>
    /// The preferred namespace prefix, or an empty string for no prefix.
    /// </summary>
    public string Prefix = "";
}
