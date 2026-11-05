namespace sql;

/// <summary>Specifies the supported unary operator kind.</summary>
public enum SqlUnaryOperatorKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents positive.</summary>
    Positive = 1,
    /// <summary>Represents negative.</summary>
    Negative = 2,
    /// <summary>Represents bitwise not.</summary>
    BitwiseNot = 3,
    /// <summary>Represents not.</summary>
    Not = 4
}
