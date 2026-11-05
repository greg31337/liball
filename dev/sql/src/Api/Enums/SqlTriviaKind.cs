namespace sql;

/// <summary>Specifies the supported trivia kind.</summary>
public enum SqlTriviaKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents whitespace.</summary>
    Whitespace = 1,
    /// <summary>Represents line comment.</summary>
    LineComment = 2,
    /// <summary>Represents block comment.</summary>
    BlockComment = 3,
    /// <summary>Represents skipped text.</summary>
    SkippedText = 4
}
