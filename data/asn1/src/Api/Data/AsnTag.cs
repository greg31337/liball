namespace asn1;

/// <summary>
/// Describes an ASN.1 tag classification and tagging mode.
/// </summary>
public class AsnTag
{
    /// <summary>
    /// Contains the tag class.
    /// </summary>
    public AsnTagClass Class;

    /// <summary>
    /// Contains the tagging mode.
    /// </summary>
    public AsnTaggingMode Mode;

    /// <summary>
    /// Contains the non-negative arbitrary-size tag number.
    /// </summary>
    public System.Numerics.BigInteger Number;

    internal bool IsValid()
    {
        int classValue = (int)this.Class;
        int modeValue = (int)this.Mode;
        return classValue >= (int)AsnTagClass.Universal
            && classValue <= (int)AsnTagClass.Private
            && modeValue >= (int)AsnTaggingMode.Unspecified
            && modeValue <= (int)AsnTaggingMode.Automatic
            && this.Number >= System.Numerics.BigInteger.Zero;
    }
}
