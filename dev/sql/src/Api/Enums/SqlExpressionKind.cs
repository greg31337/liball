namespace sql;

/// <summary>Specifies the supported expression kind.</summary>
public enum SqlExpressionKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents name.</summary>
    Name = 1,
    /// <summary>Represents literal.</summary>
    Literal = 2,
    /// <summary>Represents parameter.</summary>
    Parameter = 3,
    /// <summary>Represents wildcard.</summary>
    Wildcard = 4,
    /// <summary>Represents unary.</summary>
    Unary = 5,
    /// <summary>Represents binary.</summary>
    Binary = 6,
    /// <summary>Represents function call.</summary>
    FunctionCall = 7,
    /// <summary>Represents parenthesized.</summary>
    Parenthesized = 8,
    /// <summary>Represents cast.</summary>
    Cast = 9,
    /// <summary>Represents case.</summary>
    Case = 10,
    /// <summary>Represents exists.</summary>
    Exists = 11,
    /// <summary>Represents subquery.</summary>
    Subquery = 12,
    /// <summary>Represents row value.</summary>
    RowValue = 13,
    /// <summary>Represents between.</summary>
    Between = 14,
    /// <summary>Represents in.</summary>
    In = 15,
    /// <summary>Represents collate.</summary>
    Collate = 16,
    /// <summary>Represents is.</summary>
    Is = 17,
    /// <summary>Represents like.</summary>
    Like = 18,
    /// <summary>Represents window.</summary>
    Window = 19,
    /// <summary>Represents an IS DISTINCT FROM predicate.</summary>
    Distinctness = 20,
    /// <summary>Represents an EXTRACT-like expression.</summary>
    Extract = 21,
    /// <summary>Represents an array constructor.</summary>
    Array = 22,
    /// <summary>Represents an array or value subscript.</summary>
    Subscript = 23,
    /// <summary>Represents a JSON operator expression.</summary>
    JsonOperator = 24
}
