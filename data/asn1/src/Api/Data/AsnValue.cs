namespace asn1;

/// <summary>
/// Describes the classification and specialized forms of an ASN.1 value.
/// </summary>
public class AsnValue
{
    /// <summary>
    /// Contains the abstract-value category.
    /// </summary>
    public AsnValueKind Kind;

    /// <summary>
    /// Contains normalized REAL information when applicable.
    /// </summary>
    public AsnRealValue? RealValue;

    /// <summary>
    /// Contains normalized time information when applicable.
    /// </summary>
    public AsnTimeValue? TimeValue;

    /// <summary>
    /// Contains normalized bit-string information when applicable.
    /// </summary>
    public AsnBitStringValue? BitStringValue;

    /// <summary>
    /// Contains normalized object-identifier information when applicable.
    /// </summary>
    public AsnObjectIdentifierValue? ObjectIdentifierValue;

    /// <summary>
    /// Contains resolved or preserved open-type information when applicable.
    /// </summary>
    public AsnOpenTypeValue? OpenTypeValue;

    /// <summary>
    /// Indicates whether IntegerValue contains an arbitrary-size integer.
    /// </summary>
    public bool HasIntegerValue;

    /// <summary>
    /// Contains an arbitrary-size integer when HasIntegerValue is true.
    /// </summary>
    public System.Numerics.BigInteger IntegerValue;

    /// <summary>
    /// Contains textual value content when applicable.
    /// </summary>
    public string TextValue = "";

    /// <summary>
    /// Contains normalized octets when applicable.
    /// </summary>
    public byte[] Octets = [];

    /// <summary>
    /// Contains positional child values in schema order.
    /// </summary>
    public AsnValue[] Items = [];

    /// <summary>
    /// Contains named component values.
    /// </summary>
    public AsnNamedValue[] NamedValues = [];
}
