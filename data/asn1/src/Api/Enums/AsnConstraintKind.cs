namespace asn1;

/// <summary>
/// Identifies an ASN.1 constraint or set-operation category.
/// </summary>
public enum AsnConstraintKind
{
    /// <summary>
    /// Represents unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Represents single value.
    /// </summary>
    SingleValue = 1,

    /// <summary>
    /// Represents contained subtype.
    /// </summary>
    ContainedSubtype = 2,

    /// <summary>
    /// Represents value range.
    /// </summary>
    ValueRange = 3,

    /// <summary>
    /// Represents size.
    /// </summary>
    Size = 4,

    /// <summary>
    /// Represents permitted alphabet.
    /// </summary>
    PermittedAlphabet = 5,

    /// <summary>
    /// Represents pattern.
    /// </summary>
    Pattern = 6,

    /// <summary>
    /// Represents property settings.
    /// </summary>
    PropertySettings = 7,

    /// <summary>
    /// Represents type.
    /// </summary>
    Type = 8,

    /// <summary>
    /// Represents with component.
    /// </summary>
    WithComponent = 9,

    /// <summary>
    /// Represents with components.
    /// </summary>
    WithComponents = 10,

    /// <summary>
    /// Represents user defined.
    /// </summary>
    UserDefined = 11,

    /// <summary>
    /// Represents simple table.
    /// </summary>
    SimpleTable = 12,

    /// <summary>
    /// Represents component relation.
    /// </summary>
    ComponentRelation = 13,

    /// <summary>
    /// Represents contents.
    /// </summary>
    Contents = 14,

    /// <summary>
    /// Represents union.
    /// </summary>
    Union = 15,

    /// <summary>
    /// Represents intersection.
    /// </summary>
    Intersection = 16,

    /// <summary>
    /// Represents except.
    /// </summary>
    Except = 17,

    /// <summary>
    /// Represents all except.
    /// </summary>
    AllExcept = 18,

    /// <summary>
    /// Represents extension.
    /// </summary>
    Extension = 19,

    /// <summary>
    /// Represents exception.
    /// </summary>
    Exception = 20
}
