namespace odf;

/// <summary>
/// Identifies the semantic kind of an OpenDocument value.
/// </summary>
public enum OdfValueKind
{
    /// <summary>
    /// No value is present.
    /// </summary>
    Empty = 0,
    /// <summary>
    /// A string value.
    /// </summary>
    String = 1,
    /// <summary>
    /// A floating-point numeric value.
    /// </summary>
    Float = 2,
    /// <summary>
    /// A decimal numeric value.
    /// </summary>
    Decimal = 3,
    /// <summary>
    /// A percentage value.
    /// </summary>
    Percentage = 4,
    /// <summary>
    /// A currency value.
    /// </summary>
    Currency = 5,
    /// <summary>
    /// A calendar-date value.
    /// </summary>
    Date = 6,
    /// <summary>
    /// A date-and-time value.
    /// </summary>
    DateTime = 7,
    /// <summary>
    /// A time-of-day value.
    /// </summary>
    Time = 8,
    /// <summary>
    /// A duration value.
    /// </summary>
    Duration = 9,
    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean = 10,
    /// <summary>
    /// An error value.
    /// </summary>
    Error = 11,
    /// <summary>
    /// A complex-number value.
    /// </summary>
    Complex = 12,
    /// <summary>
    /// A cell or range reference value.
    /// </summary>
    Reference = 13,
    /// <summary>
    /// An array value.
    /// </summary>
    Array = 14
}
