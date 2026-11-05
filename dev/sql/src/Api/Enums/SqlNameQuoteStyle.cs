namespace sql;

/// <summary>Specifies the supported name quote style.</summary>
public enum SqlNameQuoteStyle
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents double quote.</summary>
    DoubleQuote = 1,
    /// <summary>Represents bracket.</summary>
    Bracket = 2,
    /// <summary>Represents backtick.</summary>
    Backtick = 3,
    /// <summary>Represents single quote.</summary>
    SingleQuote = 4
}
