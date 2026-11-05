namespace sql;

/// <summary>Specifies the supported binary operator kind.</summary>
public enum SqlBinaryOperatorKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents add.</summary>
    Add = 1,
    /// <summary>Represents subtract.</summary>
    Subtract = 2,
    /// <summary>Represents multiply.</summary>
    Multiply = 3,
    /// <summary>Represents divide.</summary>
    Divide = 4,
    /// <summary>Represents modulo.</summary>
    Modulo = 5,
    /// <summary>Represents concat.</summary>
    Concat = 6,
    /// <summary>Represents equal.</summary>
    Equal = 7,
    /// <summary>Represents not equal.</summary>
    NotEqual = 8,
    /// <summary>Represents less.</summary>
    Less = 9,
    /// <summary>Represents less or equal.</summary>
    LessOrEqual = 10,
    /// <summary>Represents greater.</summary>
    Greater = 11,
    /// <summary>Represents greater or equal.</summary>
    GreaterOrEqual = 12,
    /// <summary>Represents bitwise and.</summary>
    BitwiseAnd = 13,
    /// <summary>Represents bitwise or.</summary>
    BitwiseOr = 14,
    /// <summary>Represents bitwise xor.</summary>
    BitwiseXor = 15,
    /// <summary>Represents shift left.</summary>
    ShiftLeft = 16,
    /// <summary>Represents shift right.</summary>
    ShiftRight = 17,
    /// <summary>Represents and.</summary>
    And = 18,
    /// <summary>Represents or.</summary>
    Or = 19,
    /// <summary>Represents JSON value extraction.</summary>
    JsonExtract = 20,
    /// <summary>Represents JSON text extraction.</summary>
    JsonExtractText = 21
}
