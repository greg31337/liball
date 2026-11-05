namespace asn1;

/// <summary>
/// Associates a structured runtime field name with presence and value data.
/// </summary>
public class AsnRuntimeFieldValue
{
    /// <summary>
    /// Contains the ASN.1 component name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Indicates whether the field is present in the abstract value.
    /// </summary>
    public bool Present;

    /// <summary>
    /// Indicates whether a present field was explicitly equal to its default.
    /// </summary>
    public bool PresentDefault;

    /// <summary>
    /// Contains the field value when Present is true.
    /// </summary>
    public AsnRuntimeValue? Value;
}
