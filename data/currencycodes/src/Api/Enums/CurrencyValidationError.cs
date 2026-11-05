namespace currencycodes;

/// <summary>
/// Identifies the outcome category of currency validation.
/// </summary>
public enum CurrencyValidationError
{
    /// <summary>
    /// Validation completed without an error.
    /// </summary>
    None = 0,

    /// <summary>
    /// The supplied currency representation is malformed.
    /// </summary>
    MalformedInput = 1,

    /// <summary>
    /// The supplied value is well formed but is not assigned.
    /// </summary>
    Unassigned = 2,

    /// <summary>
    /// The currency does not have the required assignment status.
    /// </summary>
    InvalidStatus = 3,

    /// <summary>
    /// The currency is not valid on the requested date.
    /// </summary>
    InvalidDate = 4,

    /// <summary>
    /// The territory is invalid or is not associated with the currency.
    /// </summary>
    InvalidTerritory = 5,

    /// <summary>
    /// The available source dates cannot determine the result.
    /// </summary>
    Indeterminate = 6
}
