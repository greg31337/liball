namespace asn1;

/// <summary>
/// Identifies an ASN.1 type category.
/// </summary>
public enum AsnTypeKind
{
    /// <summary>
    /// Represents unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Represents boolean.
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// Represents integer.
    /// </summary>
    Integer = 2,

    /// <summary>
    /// Represents enumerated.
    /// </summary>
    Enumerated = 3,

    /// <summary>
    /// Represents real.
    /// </summary>
    Real = 4,

    /// <summary>
    /// Represents bit string.
    /// </summary>
    BitString = 5,

    /// <summary>
    /// Represents octet string.
    /// </summary>
    OctetString = 6,

    /// <summary>
    /// Represents null.
    /// </summary>
    Null = 7,

    /// <summary>
    /// Represents object identifier.
    /// </summary>
    ObjectIdentifier = 8,

    /// <summary>
    /// Represents relative object identifier.
    /// </summary>
    RelativeObjectIdentifier = 9,

    /// <summary>
    /// Represents object identifier iri.
    /// </summary>
    ObjectIdentifierIri = 10,

    /// <summary>
    /// Represents relative object identifier iri.
    /// </summary>
    RelativeObjectIdentifierIri = 11,

    /// <summary>
    /// Represents choice.
    /// </summary>
    Choice = 12,

    /// <summary>
    /// Represents sequence.
    /// </summary>
    Sequence = 13,

    /// <summary>
    /// Represents set.
    /// </summary>
    Set = 14,

    /// <summary>
    /// Represents sequence of.
    /// </summary>
    SequenceOf = 15,

    /// <summary>
    /// Represents set of.
    /// </summary>
    SetOf = 16,

    /// <summary>
    /// Represents tagged.
    /// </summary>
    Tagged = 17,

    /// <summary>
    /// Represents prefixed.
    /// </summary>
    Prefixed = 18,

    /// <summary>
    /// Represents selection.
    /// </summary>
    Selection = 19,

    /// <summary>
    /// Represents instance of.
    /// </summary>
    InstanceOf = 20,

    /// <summary>
    /// Represents external.
    /// </summary>
    External = 21,

    /// <summary>
    /// Represents embedded pdv.
    /// </summary>
    EmbeddedPdv = 22,

    /// <summary>
    /// Represents character string.
    /// </summary>
    CharacterString = 23,

    /// <summary>
    /// Represents object descriptor.
    /// </summary>
    ObjectDescriptor = 24,

    /// <summary>
    /// Represents utf-8 string.
    /// </summary>
    Utf8String = 25,

    /// <summary>
    /// Represents numeric string.
    /// </summary>
    NumericString = 26,

    /// <summary>
    /// Represents printable string.
    /// </summary>
    PrintableString = 27,

    /// <summary>
    /// Represents teletex string.
    /// </summary>
    TeletexString = 28,

    /// <summary>
    /// Represents videotex string.
    /// </summary>
    VideotexString = 29,

    /// <summary>
    /// Represents ia5 string.
    /// </summary>
    Ia5String = 30,

    /// <summary>
    /// Represents graphic string.
    /// </summary>
    GraphicString = 31,

    /// <summary>
    /// Represents visible string.
    /// </summary>
    VisibleString = 32,

    /// <summary>
    /// Represents general string.
    /// </summary>
    GeneralString = 33,

    /// <summary>
    /// Represents universal string.
    /// </summary>
    UniversalString = 34,

    /// <summary>
    /// Represents bmp string.
    /// </summary>
    BmpString = 35,

    /// <summary>
    /// Represents utc time.
    /// </summary>
    UtcTime = 36,

    /// <summary>
    /// Represents generalized time.
    /// </summary>
    GeneralizedTime = 37,

    /// <summary>
    /// Represents time.
    /// </summary>
    Time = 38,

    /// <summary>
    /// Represents date.
    /// </summary>
    Date = 39,

    /// <summary>
    /// Represents time of day.
    /// </summary>
    TimeOfDay = 40,

    /// <summary>
    /// Represents date time.
    /// </summary>
    DateTime = 41,

    /// <summary>
    /// Represents duration.
    /// </summary>
    Duration = 42,

    /// <summary>
    /// Represents defined.
    /// </summary>
    Defined = 43,

    /// <summary>
    /// Represents open type.
    /// </summary>
    OpenType = 44
}
