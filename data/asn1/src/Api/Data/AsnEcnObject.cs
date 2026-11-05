namespace asn1;

/// <summary>
/// Contains a classified X.692 encoding object.
/// </summary>
public class AsnEcnObject
{
    /// <summary>
    /// Contains the encoding-object reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the governing encoding-class name.
    /// </summary>
    public string ClassName = "";

    /// <summary>
    /// Contains resolved encoding instructions and property settings.
    /// </summary>
    public AsnEncodingInstruction[] Instructions = [];
}
