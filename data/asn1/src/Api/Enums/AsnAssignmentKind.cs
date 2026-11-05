namespace asn1;

/// <summary>
/// Identifies an ASN.1 assignment category.
/// </summary>
public enum AsnAssignmentKind
{
    /// <summary>
    /// Represents unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Represents type.
    /// </summary>
    Type = 1,

    /// <summary>
    /// Represents value.
    /// </summary>
    Value = 2,

    /// <summary>
    /// Represents value set.
    /// </summary>
    ValueSet = 3,

    /// <summary>
    /// Represents object class.
    /// </summary>
    ObjectClass = 4,

    /// <summary>
    /// Represents object.
    /// </summary>
    Object = 5,

    /// <summary>
    /// Represents object set.
    /// </summary>
    ObjectSet = 6
}
