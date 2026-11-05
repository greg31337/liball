namespace asn1;

/// <summary>
/// Contains an X.681 object-class definition.
/// </summary>
public class AsnObjectClass
{
    /// <summary>
    /// Contains the object-class reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains object-class fields in declaration order.
    /// </summary>
    public AsnObjectClassField[] Fields = [];

    /// <summary>
    /// Contains WITH SYNTAX literal and field tokens when present.
    /// </summary>
    public string[] SyntaxTokens = [];
}
