using System;

namespace template;

/// <summary>
/// Defines the supported token kind values used by the template API.
/// </summary>
public enum TemplateTokenKind
{
    /// <summary>
    /// Identifies an unknown token.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Identifies a text token.
    /// </summary>
    Text = 1,
    /// <summary>
    /// Identifies an output start delimiter token.
    /// </summary>
    OutputStartDelimiter = 2,
    /// <summary>
    /// Identifies an output end delimiter token.
    /// </summary>
    OutputEndDelimiter = 3,
    /// <summary>
    /// Identifies a statement start delimiter token.
    /// </summary>
    StatementStartDelimiter = 4,
    /// <summary>
    /// Identifies a statement end delimiter token.
    /// </summary>
    StatementEndDelimiter = 5,
    /// <summary>
    /// Identifies a comment start delimiter token.
    /// </summary>
    CommentStartDelimiter = 6,
    /// <summary>
    /// Identifies a comment end delimiter token.
    /// </summary>
    CommentEndDelimiter = 7,
    /// <summary>
    /// Identifies a raw block start delimiter token.
    /// </summary>
    RawBlockStartDelimiter = 8,
    /// <summary>
    /// Identifies a raw block end delimiter token.
    /// </summary>
    RawBlockEndDelimiter = 9,
    /// <summary>
    /// Identifies a left trim marker token.
    /// </summary>
    LeftTrimMarker = 10,
    /// <summary>
    /// Identifies a right trim marker token.
    /// </summary>
    RightTrimMarker = 11,
    /// <summary>
    /// Identifies an identifier token.
    /// </summary>
    Identifier = 12,
    /// <summary>
    /// Identifies a string literal token.
    /// </summary>
    StringLiteral = 13,
    /// <summary>
    /// Identifies a number literal token.
    /// </summary>
    NumberLiteral = 14,
    /// <summary>
    /// Identifies a boolean literal token.
    /// </summary>
    BooleanLiteral = 15,
    /// <summary>
    /// Identifies a null literal token.
    /// </summary>
    NullLiteral = 16,
    /// <summary>
    /// Identifies an empty literal token.
    /// </summary>
    EmptyLiteral = 17,
    /// <summary>
    /// Identifies an operator token.
    /// </summary>
    Operator = 18,
    /// <summary>
    /// Identifies a dot token.
    /// </summary>
    Dot = 19,
    /// <summary>
    /// Identifies a comma token.
    /// </summary>
    Comma = 20,
    /// <summary>
    /// Identifies a colon token.
    /// </summary>
    Colon = 21,
    /// <summary>
    /// Identifies a semicolon token.
    /// </summary>
    Semicolon = 22,
    /// <summary>
    /// Identifies a pipe token.
    /// </summary>
    Pipe = 23,
    /// <summary>
    /// Identifies a question token.
    /// </summary>
    Question = 24,
    /// <summary>
    /// Identifies an open paren token.
    /// </summary>
    OpenParen = 25,
    /// <summary>
    /// Identifies a close paren token.
    /// </summary>
    CloseParen = 26,
    /// <summary>
    /// Identifies an open bracket token.
    /// </summary>
    OpenBracket = 27,
    /// <summary>
    /// Identifies a close bracket token.
    /// </summary>
    CloseBracket = 28,
    /// <summary>
    /// Identifies an open brace token.
    /// </summary>
    OpenBrace = 29,
    /// <summary>
    /// Identifies a close brace token.
    /// </summary>
    CloseBrace = 30,
    /// <summary>
    /// Identifies an equals token.
    /// </summary>
    Equals = 31,
    /// <summary>
    /// Identifies a comment text token.
    /// </summary>
    CommentText = 32,
    /// <summary>
    /// Identifies a raw text token.
    /// </summary>
    RawText = 33,
    /// <summary>
    /// Identifies an end of file token.
    /// </summary>
    EndOfFile = 34,
    /// <summary>
    /// Identifies a Mustache delimiter-change directive.
    /// </summary>
    DelimiterChange = 35
}

/// <summary>
/// Describes whether a whitespace trim marker removes all adjacent whitespace or only whitespace on the adjacent line.
/// </summary>
public enum TemplateTrimKind
{
    /// <summary>
    /// Indicates that no trim marker is present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Removes adjacent whitespace greedily, including newline sequences.
    /// </summary>
    Greedy = 1,
    /// <summary>
    /// Removes adjacent horizontal whitespace without consuming a newline.
    /// </summary>
    NonGreedy = 2
}
