namespace asn1;

/// <summary>
/// Contains one named XML Schema document and its deterministic resolver base.
/// </summary>
public class AsnXsdInput
{
    /// <summary>
    /// Contains the caller-defined schema name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the XML Schema document text.
    /// </summary>
    public string Text = "";

    /// <summary>
    /// Indicates whether BaseUri was supplied.
    /// </summary>
    public bool HasBaseUri;

    /// <summary>
    /// Contains the resolver base URI when HasBaseUri is true.
    /// </summary>
    public string BaseUri = "";
}
