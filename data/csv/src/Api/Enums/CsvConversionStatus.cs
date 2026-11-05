namespace csv;

/// <summary>
/// Describes the outcome and original input state of an exception-less conversion.
/// </summary>
public enum CsvConversionStatus
{
    /// <summary>
    /// The input was converted without using a fallback.
    /// </summary>
    Success = 0,
    /// <summary>
    /// The input was a null value or configured null token.
    /// </summary>
    Null = 1,
    /// <summary>
    /// The requested field was not present in the row.
    /// </summary>
    Missing = 2,
    /// <summary>
    /// The input was empty text.
    /// </summary>
    Empty = 3,
    /// <summary>
    /// A configured default was converted successfully.
    /// </summary>
    Defaulted = 4,
    /// <summary>
    /// The input did not have the required representation.
    /// </summary>
    Invalid = 5,
    /// <summary>
    /// The input represented a value outside the target type range.
    /// </summary>
    OutOfRange = 6
}
