namespace sql;

/// <summary>Specifies the supported token kind.</summary>
public enum SqlTokenKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents end of file.</summary>
    EndOfFile = 1,
    /// <summary>Represents bad token.</summary>
    BadToken = 2,

    /// <summary>Represents whitespace.</summary>
    Whitespace = 10,
    /// <summary>Represents line comment.</summary>
    LineComment = 11,
    /// <summary>Represents block comment.</summary>
    BlockComment = 12,

    /// <summary>Represents identifier.</summary>
    Identifier = 20,
    /// <summary>Represents keyword.</summary>
    Keyword = 21,
    /// <summary>Represents numeric literal.</summary>
    NumericLiteral = 22,
    /// <summary>Represents string literal.</summary>
    StringLiteral = 23,
    /// <summary>Represents blob literal.</summary>
    BlobLiteral = 24,
    /// <summary>Represents parameter.</summary>
    Parameter = 25,

    /// <summary>Represents open paren.</summary>
    OpenParen = 40,
    /// <summary>Represents close paren.</summary>
    CloseParen = 41,
    /// <summary>Represents open bracket.</summary>
    OpenBracket = 42,
    /// <summary>Represents close bracket.</summary>
    CloseBracket = 43,
    /// <summary>Represents comma.</summary>
    Comma = 44,
    /// <summary>Represents dot.</summary>
    Dot = 45,
    /// <summary>Represents semicolon.</summary>
    Semicolon = 46,
    /// <summary>Represents colon.</summary>
    Colon = 47,
    /// <summary>Represents double colon.</summary>
    DoubleColon = 48,
    /// <summary>Represents question.</summary>
    Question = 49,
    /// <summary>Represents at.</summary>
    At = 50,
    /// <summary>Represents dollar.</summary>
    Dollar = 51,

    /// <summary>Represents plus.</summary>
    Plus = 70,
    /// <summary>Represents minus.</summary>
    Minus = 71,
    /// <summary>Represents star.</summary>
    Star = 72,
    /// <summary>Represents slash.</summary>
    Slash = 73,
    /// <summary>Represents percent.</summary>
    Percent = 74,
    /// <summary>Represents pipe.</summary>
    Pipe = 75,
    /// <summary>Represents double pipe.</summary>
    DoublePipe = 76,
    /// <summary>Represents ampersand.</summary>
    Ampersand = 77,
    /// <summary>Represents tilde.</summary>
    Tilde = 78,
    /// <summary>Represents caret.</summary>
    Caret = 79,

    /// <summary>Represents equal.</summary>
    Equal = 90,
    /// <summary>Represents double equal.</summary>
    DoubleEqual = 91,
    /// <summary>Represents bang equal.</summary>
    BangEqual = 92,
    /// <summary>Represents less.</summary>
    Less = 93,
    /// <summary>Represents less equal.</summary>
    LessEqual = 94,
    /// <summary>Represents greater.</summary>
    Greater = 95,
    /// <summary>Represents greater equal.</summary>
    GreaterEqual = 96,
    /// <summary>Represents less greater.</summary>
    LessGreater = 97,
    /// <summary>Represents shift left.</summary>
    ShiftLeft = 98,
    /// <summary>Represents shift right.</summary>
    ShiftRight = 99,
    /// <summary>Represents arrow.</summary>
    Arrow = 100,
    /// <summary>Represents double arrow.</summary>
    DoubleArrow = 101,
    /// <summary>Represents a caller-defined operator.</summary>
    CustomOperator = 102
}
