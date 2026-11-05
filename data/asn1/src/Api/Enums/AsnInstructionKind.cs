namespace asn1;

/// <summary>
/// Identifies an ASN.1 encoding instruction category.
/// </summary>
public enum AsnInstructionKind
{
    /// <summary>
    /// Represents unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Represents any attributes.
    /// </summary>
    AnyAttributes = 1,

    /// <summary>
    /// Represents any element.
    /// </summary>
    AnyElement = 2,

    /// <summary>
    /// Represents attribute.
    /// </summary>
    Attribute = 3,

    /// <summary>
    /// Represents base64.
    /// </summary>
    Base64 = 4,

    /// <summary>
    /// Represents decimal.
    /// </summary>
    Decimal = 5,

    /// <summary>
    /// Represents default for empty.
    /// </summary>
    DefaultForEmpty = 6,

    /// <summary>
    /// Represents element.
    /// </summary>
    Element = 7,

    /// <summary>
    /// Represents embed values.
    /// </summary>
    EmbedValues = 8,

    /// <summary>
    /// Represents global defaults.
    /// </summary>
    GlobalDefaults = 9,

    /// <summary>
    /// Represents list.
    /// </summary>
    List = 10,

    /// <summary>
    /// Represents name.
    /// </summary>
    Name = 11,

    /// <summary>
    /// Represents namespace.
    /// </summary>
    Namespace = 12,

    /// <summary>
    /// Represents pi or comment.
    /// </summary>
    PiOrComment = 13,

    /// <summary>
    /// Represents text.
    /// </summary>
    Text = 14,

    /// <summary>
    /// Represents untagged.
    /// </summary>
    Untagged = 15,

    /// <summary>
    /// Represents use nil.
    /// </summary>
    UseNil = 16,

    /// <summary>
    /// Represents use number.
    /// </summary>
    UseNumber = 17,

    /// <summary>
    /// Represents use order.
    /// </summary>
    UseOrder = 18,

    /// <summary>
    /// Represents use q name.
    /// </summary>
    UseQName = 19,

    /// <summary>
    /// Represents use type.
    /// </summary>
    UseType = 20,

    /// <summary>
    /// Represents use union.
    /// </summary>
    UseUnion = 21,

    /// <summary>
    /// Represents whitespace.
    /// </summary>
    Whitespace = 22,

    /// <summary>
    /// Represents per application.
    /// </summary>
    PerApplication = 23,

    /// <summary>
    /// Represents ecn.
    /// </summary>
    Ecn = 24
}
