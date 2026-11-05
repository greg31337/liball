namespace svg;

/// <summary>Defines CSS math expression node kinds.</summary>
public enum SvgCalcExpressionKind
{
    /// <summary>A numeric, percentage, or dimension leaf.</summary>
    Value = 0,
    /// <summary>An addition expression.</summary>
    Add = 1,
    /// <summary>A subtraction expression.</summary>
    Subtract = 2,
    /// <summary>A multiplication expression.</summary>
    Multiply = 3,
    /// <summary>A division expression.</summary>
    Divide = 4,
    /// <summary>A unary negation expression.</summary>
    Negate = 5,
    /// <summary>A <c>calc()</c> grouping expression.</summary>
    Calc = 6,
    /// <summary>A <c>min()</c> expression.</summary>
    Min = 7,
    /// <summary>A <c>max()</c> expression.</summary>
    Max = 8,
    /// <summary>A <c>clamp()</c> expression.</summary>
    Clamp = 9
}

/// <summary>Defines dimensions supported by CSS math validation.</summary>
public enum SvgCalcDimension
{
    /// <summary>Accepts any valid result dimension when used as an expectation.</summary>
    Any = 0,
    /// <summary>A unitless number.</summary>
    Number = 1,
    /// <summary>A length.</summary>
    Length = 2,
    /// <summary>A percentage.</summary>
    Percentage = 3,
    /// <summary>A combined length-percentage dimension.</summary>
    LengthPercentage = 4,
    /// <summary>An angle.</summary>
    Angle = 5,
    /// <summary>A time.</summary>
    Time = 6,
    /// <summary>A frequency.</summary>
    Frequency = 7,
    /// <summary>A resolution.</summary>
    Resolution = 8
}

/// <summary>Defines stable CSS math parsing and validation failures.</summary>
public enum SvgCalcParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>An expression was required.</summary>
    ExpectedExpression = 2,
    /// <summary>An unexpected token or missing delimiter was encountered.</summary>
    UnexpectedToken = 3,
    /// <summary>A numeric or dimension token exceeded its bound.</summary>
    TokenTooLong = 4,
    /// <summary>An unknown unit suffix was encountered.</summary>
    UnknownUnit = 5,
    /// <summary>Addition or comparison used incompatible dimensions.</summary>
    IncompatibleDimensions = 6,
    /// <summary>Multiplication or division used invalid dimensional operands.</summary>
    InvalidProduct = 7,
    /// <summary>A literal zero divisor was encountered.</summary>
    DivisionByZero = 8,
    /// <summary>A math function had an invalid name or argument count.</summary>
    InvalidFunction = 9,
    /// <summary>The configured expression nesting depth was exceeded.</summary>
    DepthExceeded = 10,
    /// <summary>The configured expression-node count was exceeded.</summary>
    NodeLimitExceeded = 11,
    /// <summary>Custom-property substitution failed.</summary>
    VariableFailure = 12
}
