namespace asn1;

/// <summary>
/// Describes the classification and schema value of a normalized runtime value.
/// </summary>
public class AsnRuntimeValue
{
    /// <summary>
    /// Contains the runtime-value category.
    /// </summary>
    public AsnRuntimeValueKind Kind;

    /// <summary>
    /// Contains the schema-level value when one is available.
    /// </summary>
    public AsnValue? Value;

    /// <summary>
    /// Indicates whether BooleanValue contains a BOOLEAN value.
    /// </summary>
    public bool HasBooleanValue;

    /// <summary>
    /// Contains a BOOLEAN value when HasBooleanValue is true.
    /// </summary>
    public bool BooleanValue;

    /// <summary>
    /// Indicates whether IntegerValue contains an arbitrary-size integer.
    /// </summary>
    public bool HasIntegerValue;

    /// <summary>
    /// Contains an arbitrary-size integer when HasIntegerValue is true.
    /// </summary>
    public System.Numerics.BigInteger IntegerValue;

    /// <summary>
    /// Contains character or identifier text when applicable.
    /// </summary>
    public string TextValue = "";

    /// <summary>
    /// Contains a bit-string value when applicable.
    /// </summary>
    public AsnBitStringValue? BitStringValue;

    /// <summary>
    /// Contains octet-string data when applicable.
    /// </summary>
    public byte[] Octets = [];

    /// <summary>
    /// Contains an object identifier value when applicable.
    /// </summary>
    public AsnObjectIdentifierValue? ObjectIdentifierValue;

    /// <summary>
    /// Contains a REAL value when applicable.
    /// </summary>
    public AsnRealValue? RealValue;

    /// <summary>
    /// Contains a time value when applicable.
    /// </summary>
    public AsnTimeValue? TimeValue;

    /// <summary>
    /// Contains structured field values.
    /// </summary>
    public AsnRuntimeFieldValue[] Fields = [];

    /// <summary>
    /// Contains collection values in abstract-value order.
    /// </summary>
    public AsnRuntimeValue[] Items = [];

    /// <summary>
    /// Contains an open-type value when applicable.
    /// </summary>
    public AsnOpenTypeValue? OpenTypeValue;

    /// <summary>
    /// Contains preserved unknown extension additions.
    /// </summary>
    public AsnUnknownExtension[] UnknownExtensions = [];

    /// <summary>
    /// Contains preserved unknown named fields.
    /// </summary>
    public AsnUnknownField[] UnknownFields = [];
}
