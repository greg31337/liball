namespace odf;

/// <summary>
/// Represents an XML attribute and its preserved lexical value.
/// </summary>
public class OdfAttribute
{
    /// <summary>
    /// The qualified attribute name.
    /// </summary>
    public OdfQualifiedName Name = new OdfQualifiedName();
    /// <summary>
    /// The lexical attribute value.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// The source location of the attribute, when available.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
}
