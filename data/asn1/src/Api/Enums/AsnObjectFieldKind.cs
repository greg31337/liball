namespace asn1;

/// <summary>
/// Identifies an X.681 object-class field category.
/// </summary>
public enum AsnObjectFieldKind
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
    /// Represents fixed type value.
    /// </summary>
    FixedTypeValue = 2,

    /// <summary>
    /// Represents variable type value.
    /// </summary>
    VariableTypeValue = 3,

    /// <summary>
    /// Represents fixed type value set.
    /// </summary>
    FixedTypeValueSet = 4,

    /// <summary>
    /// Represents variable type value set.
    /// </summary>
    VariableTypeValueSet = 5,

    /// <summary>
    /// Represents object.
    /// </summary>
    Object = 6,

    /// <summary>
    /// Represents object set.
    /// </summary>
    ObjectSet = 7
}
