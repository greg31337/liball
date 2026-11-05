namespace odf;

/// <summary>
/// Describes one scalar or list-valued application-defined form property.
/// </summary>
public class OdfFormProperty
{
    /// <summary>The property name.</summary>
    public string Name = "";
    /// <summary>The ODF office value-type token.</summary>
    public string ValueType = "";
    /// <summary>Whether this is a list property.</summary>
    public bool IsList;
    /// <summary>The scalar value or ordered list values.</summary>
    public OdfFormPropertyValue[] Values = [];
    /// <summary>The underlying property element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalValueType = "";
}
