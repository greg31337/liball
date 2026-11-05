namespace currencycodes;

/// <summary>
/// Stores the structured outcome of currency validation.
/// </summary>
public class CurrencyValidationResult
{
    /// <summary>
    /// Indicates whether the currency is valid for the requested operation.
    /// </summary>
    public bool IsValid = false;

    /// <summary>
    /// Indicates whether available source dates are insufficient to decide validity.
    /// </summary>
    public bool IsIndeterminate = false;

    /// <summary>
    /// Identifies the currency-validation outcome category.
    /// </summary>
    public CurrencyValidationError Error = CurrencyValidationError.None;

    /// <summary>
    /// Stores the normalized input when normalization succeeds.
    /// </summary>
    public string NormalizedText = string.Empty;

    /// <summary>
    /// Stores an invariant explanation of the validation outcome.
    /// </summary>
    public string Reason = string.Empty;

    /// <summary>
    /// Stores the matched currency when one is available.
    /// </summary>
    public CurrencyInfo? Currency = null;
}
