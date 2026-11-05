namespace odf;

/// <summary>
/// Represents one qualified style-property attribute and its preserved lexical value.
/// </summary>
public class OdfStyleProperty
{
    /// <summary>
    /// The qualified property name.
    /// </summary>
    public OdfQualifiedName Name = new OdfQualifiedName();
    /// <summary>
    /// The exact lexical property value.
    /// </summary>
    public string LexicalValue = "";
    /// <summary>
    /// The underlying XML attribute, when loaded or written.
    /// </summary>
    public OdfAttribute? Attribute;
    internal OdfAttribute? OriginalAttribute;
    internal string OriginalNamespaceName = "";
    internal string OriginalLocalName = "";
    internal string OriginalPrefix = "";
    internal string OriginalLexicalValue = "";
}
