namespace odf;

/// <summary>
/// Represents an XML namespace declaration.
/// </summary>
public class OdfNamespaceDeclaration
{
    /// <summary>
    /// The declared prefix, or an empty string for the default namespace.
    /// </summary>
    public string Prefix = "";
    /// <summary>
    /// The namespace name, or an empty string when undeclaring the default namespace.
    /// </summary>
    public string NamespaceName = "";
    /// <summary>
    /// The source location of the declaration, when available.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
}
