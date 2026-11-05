namespace csv;

/// <summary>
/// Identifies the built-in value type inferred for a column.
/// </summary>
public enum CsvTypeKind
{
    /// <summary>
    /// Indicates that no type has been determined.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates that every sampled value is null.
    /// </summary>
    Null = 1,
    /// <summary>
    /// Indicates Boolean text.
    /// </summary>
    Boolean = 2,
    /// <summary>
    /// Indicates a signed 32-bit integer.
    /// </summary>
    Int32 = 3,
    /// <summary>
    /// Indicates a signed 64-bit integer.
    /// </summary>
    Int64 = 4,
    /// <summary>
    /// Indicates a base-10 decimal number.
    /// </summary>
    Decimal = 5,
    /// <summary>
    /// Indicates a double-precision number.
    /// </summary>
    Double = 6,
    /// <summary>
    /// Indicates a date and time.
    /// </summary>
    DateTime = 7,
    /// <summary>
    /// Indicates a globally unique identifier.
    /// </summary>
    Guid = 8,
    /// <summary>
    /// Indicates hexadecimal byte data.
    /// </summary>
    Binary = 9,
    /// <summary>
    /// Indicates general text or mixed sampled values.
    /// </summary>
    String = 10,
    /// <summary>
    /// Indicates a calendar date without a time.
    /// </summary>
    DateOnly = 11,
    /// <summary>
    /// Indicates a clock time without a date.
    /// </summary>
    TimeOnly = 12
}
