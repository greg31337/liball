namespace asn1;

/// <summary>
/// Identifies a lexical token or retained trivia item.
/// </summary>
public enum AsnTokenKind
{
    /// <summary>
    /// Represents end of input.
    /// </summary>
    EndOfInput = 0,

    /// <summary>
    /// Represents invalid.
    /// </summary>
    Invalid = 1,

    /// <summary>
    /// Represents whitespace.
    /// </summary>
    Whitespace = 2,

    /// <summary>
    /// Represents comment.
    /// </summary>
    Comment = 3,

    /// <summary>
    /// Represents reserved word.
    /// </summary>
    ReservedWord = 4,

    /// <summary>
    /// Represents type reference.
    /// </summary>
    TypeReference = 5,

    /// <summary>
    /// Represents value reference.
    /// </summary>
    ValueReference = 6,

    /// <summary>
    /// Represents module reference.
    /// </summary>
    ModuleReference = 7,

    /// <summary>
    /// Represents object class reference.
    /// </summary>
    ObjectClassReference = 8,

    /// <summary>
    /// Represents object reference.
    /// </summary>
    ObjectReference = 9,

    /// <summary>
    /// Represents object set reference.
    /// </summary>
    ObjectSetReference = 10,

    /// <summary>
    /// Represents type field reference.
    /// </summary>
    TypeFieldReference = 11,

    /// <summary>
    /// Represents value field reference.
    /// </summary>
    ValueFieldReference = 12,

    /// <summary>
    /// Represents value set field reference.
    /// </summary>
    ValueSetFieldReference = 13,

    /// <summary>
    /// Represents object field reference.
    /// </summary>
    ObjectFieldReference = 14,

    /// <summary>
    /// Represents object set field reference.
    /// </summary>
    ObjectSetFieldReference = 15,

    /// <summary>
    /// Represents encoding reference.
    /// </summary>
    EncodingReference = 16,

    /// <summary>
    /// Represents identifier.
    /// </summary>
    Identifier = 17,

    /// <summary>
    /// Represents integer literal.
    /// </summary>
    IntegerLiteral = 18,

    /// <summary>
    /// Represents real literal.
    /// </summary>
    RealLiteral = 19,

    /// <summary>
    /// Represents binary string literal.
    /// </summary>
    BinaryStringLiteral = 20,

    /// <summary>
    /// Represents hex string literal.
    /// </summary>
    HexStringLiteral = 21,

    /// <summary>
    /// Represents character string literal.
    /// </summary>
    CharacterStringLiteral = 22,

    /// <summary>
    /// Represents tuple literal.
    /// </summary>
    TupleLiteral = 23,

    /// <summary>
    /// Represents quadruple literal.
    /// </summary>
    QuadrupleLiteral = 24,

    /// <summary>
    /// Represents xml value.
    /// </summary>
    XmlValue = 25,

    /// <summary>
    /// Represents left brace.
    /// </summary>
    LeftBrace = 26,

    /// <summary>
    /// Represents right brace.
    /// </summary>
    RightBrace = 27,

    /// <summary>
    /// Represents left parenthesis.
    /// </summary>
    LeftParenthesis = 28,

    /// <summary>
    /// Represents right parenthesis.
    /// </summary>
    RightParenthesis = 29,

    /// <summary>
    /// Represents left bracket.
    /// </summary>
    LeftBracket = 30,

    /// <summary>
    /// Represents right bracket.
    /// </summary>
    RightBracket = 31,

    /// <summary>
    /// Represents left version bracket.
    /// </summary>
    LeftVersionBracket = 32,

    /// <summary>
    /// Represents right version bracket.
    /// </summary>
    RightVersionBracket = 33,

    /// <summary>
    /// Represents less than.
    /// </summary>
    LessThan = 34,

    /// <summary>
    /// Represents greater than.
    /// </summary>
    GreaterThan = 35,

    /// <summary>
    /// Represents comma.
    /// </summary>
    Comma = 36,

    /// <summary>
    /// Represents semicolon.
    /// </summary>
    Semicolon = 37,

    /// <summary>
    /// Represents colon.
    /// </summary>
    Colon = 38,

    /// <summary>
    /// Represents dot.
    /// </summary>
    Dot = 39,

    /// <summary>
    /// Represents exclamation mark.
    /// </summary>
    ExclamationMark = 40,

    /// <summary>
    /// Represents at sign.
    /// </summary>
    AtSign = 41,

    /// <summary>
    /// Represents vertical bar.
    /// </summary>
    VerticalBar = 42,

    /// <summary>
    /// Represents caret.
    /// </summary>
    Caret = 43,

    /// <summary>
    /// Represents ampersand.
    /// </summary>
    Ampersand = 44,

    /// <summary>
    /// Represents plus.
    /// </summary>
    Plus = 45,

    /// <summary>
    /// Represents minus.
    /// </summary>
    Minus = 46,

    /// <summary>
    /// Represents asterisk.
    /// </summary>
    Asterisk = 47,

    /// <summary>
    /// Represents slash.
    /// </summary>
    Slash = 48,

    /// <summary>
    /// Represents assignment.
    /// </summary>
    Assignment = 49,

    /// <summary>
    /// Represents ellipsis.
    /// </summary>
    Ellipsis = 50,

    /// <summary>
    /// Represents range.
    /// </summary>
    Range = 51,

    /// <summary>
    /// Represents equals.
    /// </summary>
    Equals = 52,

    /// <summary>
    /// Represents an ECN encoding class reference, including a built-in class name.
    /// </summary>
    EncodingClassReference = 53,

    /// <summary>
    /// Represents uninterpreted text between ECN NON-ECN delimiters.
    /// </summary>
    NonEcnText = 54
}
