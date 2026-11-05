namespace asn1;

/// <summary>
/// Identifies an ASN.1 abstract-value category.
/// </summary>
public enum AsnValueKind
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
    /// Represents character string.
    /// </summary>
    CharacterString = 8,

    /// <summary>
    /// Represents object identifier.
    /// </summary>
    ObjectIdentifier = 9,

    /// <summary>
    /// Represents relative object identifier.
    /// </summary>
    RelativeObjectIdentifier = 10,

    /// <summary>
    /// Represents object identifier iri.
    /// </summary>
    ObjectIdentifierIri = 11,

    /// <summary>
    /// Represents relative object identifier iri.
    /// </summary>
    RelativeObjectIdentifierIri = 12,

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
    /// Represents choice.
    /// </summary>
    Choice = 17,

    /// <summary>
    /// Represents open type.
    /// </summary>
    OpenType = 18,

    /// <summary>
    /// Represents external.
    /// </summary>
    External = 19,

    /// <summary>
    /// Represents embedded pdv.
    /// </summary>
    EmbeddedPdv = 20,

    /// <summary>
    /// Represents instance of.
    /// </summary>
    InstanceOf = 21,

    /// <summary>
    /// Represents time.
    /// </summary>
    Time = 22,

    /// <summary>
    /// Represents defined.
    /// </summary>
    Defined = 23,

    /// <summary>
    /// Represents named.
    /// </summary>
    Named = 24,

    /// <summary>
    /// Represents xml.
    /// </summary>
    Xml = 25
}
