namespace currencycodes;

/// <summary>
/// Identifies the outcome category of amount validation or conversion.
/// </summary>
public enum AmountValidationError
{
    /// <summary>
    /// The amount operation completed without an error.
    /// </summary>
    None = 0,

    /// <summary>
    /// The supplied currency is malformed, unassigned, or out of scope.
    /// </summary>
    InvalidCurrency = 1,

    /// <summary>
    /// The selected amount metadata is unavailable.
    /// </summary>
    MetadataUnavailable = 2,

    /// <summary>
    /// The amount has more fractional digits than the selected rule permits.
    /// </summary>
    InvalidScale = 3,

    /// <summary>
    /// The amount is not a multiple of the selected increment.
    /// </summary>
    InvalidIncrement = 4,

    /// <summary>
    /// The amount or conversion result is outside the supported range.
    /// </summary>
    OutOfRange = 5,

    /// <summary>
    /// The invariant amount text has an invalid format.
    /// </summary>
    InvalidFormat = 6
}
