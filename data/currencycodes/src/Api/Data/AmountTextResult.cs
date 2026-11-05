namespace currencycodes;

/// <summary>
/// Stores the structured outcome of invariant currency amount formatting or parsing.
/// </summary>
public class AmountTextResult
{
    /// <summary>
    /// Indicates whether formatting or parsing succeeded.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// Identifies the formatting or parsing failure category.
    /// </summary>
    public AmountValidationError Error = AmountValidationError.None;

    /// <summary>
    /// Stores an invariant explanation of the operation outcome.
    /// </summary>
    public string Reason = string.Empty;

    /// <summary>
    /// Stores the matched currency when one is available.
    /// </summary>
    public CurrencyInfo? Currency = null;

    /// <summary>
    /// Stores the parsed or formatted amount.
    /// </summary>
    public decimal Amount = 0m;

    /// <summary>
    /// Stores the invariant currency amount text.
    /// </summary>
    public string InvariantText = string.Empty;
}
