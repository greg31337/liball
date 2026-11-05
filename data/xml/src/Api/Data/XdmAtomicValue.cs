namespace xml;

/// <summary>
/// Carries one XDM atomic value and its type annotation.
/// </summary>
public class XdmAtomicValue
{
    /// <summary>
    /// The atomic type.
    /// </summary>
    public XdmAtomicType Type;
    /// <summary>
    /// The original lexical representation.
    /// </summary>
    public string? LexicalValue;
    /// <summary>
    /// The canonical lexical representation.
    /// </summary>
    public string? CanonicalValue;
    /// <summary>
    /// The string value.
    /// </summary>
    public string? StringValue;
    /// <summary>
    /// The Boolean value when applicable.
    /// </summary>
    public bool BooleanValue;
    /// <summary>
    /// The decimal value when applicable.
    /// </summary>
    public decimal DecimalValue;
    /// <summary>
    /// The signed integer value when it fits in 64 bits.
    /// </summary>
    public long IntegerValue;
    /// <summary>
    /// The floating-point value when applicable.
    /// </summary>
    public double FloatingValue;
    /// <summary>
    /// The binary value when applicable.
    /// </summary>
    public byte[]? BinaryValue;
    /// <summary>
    /// The QName or NOTATION value when applicable.
    /// </summary>
    public XmlName? NameValue;
    /// <summary>
    /// The built-in value representation for values requiring a wider range or temporal type.
    /// </summary>
    public object? ExtendedValue;
    /// <summary>
    /// The schema type annotation.
    /// </summary>
    public XmlSchemaTypeInfo? TypeInfo;
    /// <summary>
    /// Whether the value storage is an immutable snapshot.
    /// </summary>
    public bool IsImmutable;
}
