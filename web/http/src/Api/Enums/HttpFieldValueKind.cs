namespace http;

/// <summary>
/// Identifies the represented kind of a structured or legacy HTTP field value.
/// </summary>
public enum HttpFieldValueKind
{
    /// <summary>
    /// An ASCII token value.
    /// </summary>
    Token = 0,

    /// <summary>
    /// A quoted string value.
    /// </summary>
    String = 1,

    /// <summary>
    /// An opaque byte sequence value.
    /// </summary>
    ByteSequence = 2,

    /// <summary>
    /// An integer value.
    /// </summary>
    Integer = 3,

    /// <summary>
    /// A fixed-precision decimal value.
    /// </summary>
    Decimal = 4,

    /// <summary>
    /// A boolean value.
    /// </summary>
    Boolean = 5,

    /// <summary>
    /// A date value represented as seconds since the Unix epoch.
    /// </summary>
    Date = 6,

    /// <summary>
    /// A Unicode display string value.
    /// </summary>
    DisplayString = 7
}
