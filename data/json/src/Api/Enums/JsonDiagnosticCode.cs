namespace json;

/// <summary>Identifies a stable JSON operation failure category.</summary>
public enum JsonDiagnosticCode
{
    /// <summary>Indicates that no failure occurred.</summary>
    None = 0,

    /// <summary>An input byte cannot begin a JSON token.</summary>
    UnexpectedCharacter = 1,

    /// <summary>Input contains a code point that is not JSON whitespace.</summary>
    InvalidWhitespace = 2,

    /// <summary>A JSON literal is misspelled or incomplete.</summary>
    InvalidLiteral = 3,

    /// <summary>A number does not follow the JSON number grammar.</summary>
    InvalidNumber = 4,

    /// <summary>A string contains an unescaped character that JSON forbids.</summary>
    InvalidStringCharacter = 5,

    /// <summary>A string contains an unsupported or incomplete escape.</summary>
    InvalidEscape = 6,

    /// <summary>A Unicode escape does not contain four hexadecimal digits.</summary>
    InvalidUnicodeEscape = 7,

    /// <summary>Escaped UTF-16 surrogate code units do not form an allowed sequence.</summary>
    InvalidSurrogatePair = 8,

    /// <summary>Input contains an invalid UTF-8 byte sequence.</summary>
    InvalidUtf8 = 9,

    /// <summary>Input ended before the current JSON construct was complete.</summary>
    UnexpectedEndOfInput = 10,

    /// <summary>A valid token appeared where the structural grammar forbids it.</summary>
    UnexpectedToken = 11,

    /// <summary>An object member name was required.</summary>
    MissingPropertyName = 12,

    /// <summary>A colon was required after an object member name.</summary>
    MissingColon = 13,

    /// <summary>A comma or container end token was required.</summary>
    MissingCommaOrEnd = 14,

    /// <summary>A container has a trailing comma.</summary>
    TrailingComma = 15,

    /// <summary>Non-whitespace content follows a complete single JSON value.</summary>
    TrailingContent = 16,

    /// <summary>An object contains a duplicate decoded member name.</summary>
    DuplicatePropertyName = 17,

    /// <summary>An initial UTF-8 byte order mark is disallowed by policy.</summary>
    BomNotAllowed = 18,

    /// <summary>A byte order mark occurs outside the permitted initial position.</summary>
    UnexpectedBom = 19,

    /// <summary>An RFC 7464 record does not begin with a record separator.</summary>
    MissingRecordSeparator = 20,

    /// <summary>An RFC 7464 record contains no JSON value.</summary>
    EmptySequenceRecord = 21,

    /// <summary>An RFC 7464 record ends before its JSON value is complete.</summary>
    IncompleteSequenceRecord = 22,

    /// <summary>The configured document byte limit was exceeded.</summary>
    DocumentByteLimitExceeded = 23,

    /// <summary>The configured buffered input byte limit was exceeded.</summary>
    InputBufferLimitExceeded = 24,

    /// <summary>The configured raw token byte limit was exceeded.</summary>
    TokenByteLimitExceeded = 25,

    /// <summary>The configured decoded string byte limit was exceeded.</summary>
    DecodedStringByteLimitExceeded = 26,

    /// <summary>The configured decoded string UTF-16 code-unit limit was exceeded.</summary>
    DecodedStringCodeUnitLimitExceeded = 27,

    /// <summary>The configured number character limit was exceeded.</summary>
    NumberCharacterLimitExceeded = 28,

    /// <summary>The configured nesting depth limit was exceeded.</summary>
    NestingDepthLimitExceeded = 29,

    /// <summary>The configured array item limit was exceeded.</summary>
    ArrayItemLimitExceeded = 30,

    /// <summary>The configured object member limit was exceeded.</summary>
    ObjectMemberLimitExceeded = 31,

    /// <summary>The configured DOM node limit was exceeded.</summary>
    DomNodeLimitExceeded = 32,

    /// <summary>The configured output byte limit was exceeded.</summary>
    OutputByteLimitExceeded = 33,

    /// <summary>A JSON Pointer representation has invalid syntax.</summary>
    InvalidPointerSyntax = 34,

    /// <summary>A JSON Pointer token has an invalid tilde escape.</summary>
    InvalidPointerEscape = 35,

    /// <summary>A JSON Pointer URI fragment has invalid percent encoding.</summary>
    InvalidPointerPercentEncoding = 36,

    /// <summary>A JSON Pointer array index has an invalid spelling.</summary>
    InvalidPointerIndex = 37,

    /// <summary>A JSON Pointer array index is outside the supported integer range.</summary>
    PointerIndexOutOfRange = 38,

    /// <summary>A JSON Pointer token does not identify a value.</summary>
    PointerTargetNotFound = 39,

    /// <summary>A JSON Pointer token cannot be applied to the current value kind.</summary>
    PointerTypeMismatch = 40,

    /// <summary>A publicly mutable JSON model violates its tag or payload invariants.</summary>
    InvalidModel = 41,

    /// <summary>A publicly mutable JSON model contains a reference cycle.</summary>
    ModelCycle = 42,

    /// <summary>A writer method is invalid in the current structural state.</summary>
    InvalidWriterState = 43,

    /// <summary>Raw input does not contain exactly one valid JSON value.</summary>
    InvalidRawValue = 44,

    /// <summary>A floating-point value is not finite.</summary>
    NonFiniteNumber = 45,

    /// <summary>A JSON number cannot be represented by the requested numeric type.</summary>
    NumericConversionFailed = 46,

    /// <summary>Options contain a negative, exclusive, or incompatible setting.</summary>
    InvalidOptions = 47,

    /// <summary>A caller-owned input or output operation failed.</summary>
    IoFailure = 48
}
