namespace log;

/// <summary>
/// Identifies which value field of a structured log value contains meaningful data.
/// </summary>
public enum LogValueKind
{
    /// <summary>
    /// No value is present.
    /// </summary>
    Null = 0,
    /// <summary>
    /// TextValue contains a string.
    /// </summary>
    String = 1,
    /// <summary>
    /// BoolValue contains a Boolean.
    /// </summary>
    Bool = 2,
    /// <summary>
    /// IntegerValue contains a signed 64-bit integer.
    /// </summary>
    Integer = 3,
    /// <summary>
    /// DecimalValue contains a decimal number.
    /// </summary>
    Decimal = 4,
    /// <summary>
    /// FloatingPointValue contains a double-precision number.
    /// </summary>
    FloatingPoint = 5,
    /// <summary>
    /// DateTimeValue contains a timestamp.
    /// </summary>
    DateTime = 6,
    /// <summary>
    /// DurationValue contains a time span.
    /// </summary>
    Duration = 7,
    /// <summary>
    /// ExceptionValue and TextValue describe an exception.
    /// </summary>
    Exception = 8,
    /// <summary>
    /// TextValue contains the normalized result of ToString.
    /// </summary>
    ObjectText = 9,
    /// <summary>
    /// ArrayValue contains copied structured values.
    /// </summary>
    Array = 10,
    /// <summary>
    /// DictionaryValue contains copied string-keyed structured values.
    /// </summary>
    Dictionary = 11,
    /// <summary>
    /// RawValue contains JSON-compatible text emitted without JSON quoting.
    /// </summary>
    Raw = 12
}
