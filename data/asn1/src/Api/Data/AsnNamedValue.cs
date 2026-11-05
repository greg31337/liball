namespace asn1;

/// <summary>
/// Associates an ASN.1 identifier with an abstract value.
/// </summary>
public class AsnNamedValue
{
    /// <summary>
    /// Contains the ASN.1 identifier.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the associated value when resolved.
    /// </summary>
    public AsnValue? Value;
}
