namespace sql;

/// <summary>Specifies a string quoting form recognized by a dialect.</summary>
public enum SqlStringQuoteStyle
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents ordinary single-quoted strings.</summary>
    SingleQuote = 1,
    /// <summary>Represents ordinary double-quoted strings.</summary>
    DoubleQuote = 2,
    /// <summary>Represents escaped single-quoted strings.</summary>
    EscapedSingleQuote = 3,
    /// <summary>Represents dollar-quoted strings.</summary>
    DollarQuote = 4,
    /// <summary>Represents SQL Unicode escape strings.</summary>
    UnicodeSingleQuote = 5
}

/// <summary>Specifies a parameter placeholder form recognized by a dialect.</summary>
public enum SqlParameterStyle
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents an anonymous question-mark parameter.</summary>
    Question = 1,
    /// <summary>Represents a numbered question-mark parameter.</summary>
    NumberedQuestion = 2,
    /// <summary>Represents a colon-prefixed named parameter.</summary>
    ColonName = 3,
    /// <summary>Represents an at-prefixed named parameter.</summary>
    AtName = 4,
    /// <summary>Represents a dollar-prefixed named parameter.</summary>
    DollarName = 5,
    /// <summary>Represents a dollar-prefixed numbered parameter.</summary>
    DollarNumber = 6
}

/// <summary>Specifies a literal syntax policy exposed by a dialect.</summary>
public enum SqlDialectLiteralPolicyKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents null literals.</summary>
    Null = 1,
    /// <summary>Represents decimal integer literals.</summary>
    Integer = 2,
    /// <summary>Represents decimal point literals.</summary>
    Decimal = 3,
    /// <summary>Represents exponent notation.</summary>
    Exponent = 4,
    /// <summary>Represents string literals.</summary>
    String = 5,
    /// <summary>Represents blob literals.</summary>
    Blob = 6,
    /// <summary>Represents hexadecimal integer literals.</summary>
    HexInteger = 7,
    /// <summary>Represents boolean literals.</summary>
    Boolean = 8,
    /// <summary>Represents current date literals.</summary>
    CurrentDate = 9,
    /// <summary>Represents current time literals.</summary>
    CurrentTime = 10,
    /// <summary>Represents current timestamp literals.</summary>
    CurrentTimestamp = 11
}

/// <summary>Specifies a statement clause whose availability can vary by dialect.</summary>
public enum SqlDialectClauseKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents a common table expression clause.</summary>
    CommonTableExpression = 1,
    /// <summary>Represents a window clause or invocation.</summary>
    Window = 2,
    /// <summary>Represents a returning clause.</summary>
    Returning = 3,
    /// <summary>Represents an upsert clause.</summary>
    Upsert = 4
}
