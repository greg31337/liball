namespace currencycodes;

/// <summary>
/// Stores the structured outcome of amount validation.
/// </summary>
public class AmountValidationResult
{
    /// <summary>
    /// Indicates whether the amount satisfies the selected currency rule.
    /// </summary>
    public bool IsValid = false;

    /// <summary>
    /// Identifies the amount-validation outcome category.
    /// </summary>
    public AmountValidationError Error = AmountValidationError.None;

    /// <summary>
    /// Stores an invariant explanation of the validation outcome.
    /// </summary>
    public string Reason = string.Empty;

    /// <summary>
    /// Stores the matched currency when one is available.
    /// </summary>
    public CurrencyInfo? Currency = null;

    /// <summary>
    /// Stores the amount that was validated.
    /// </summary>
    public decimal Amount = 0m;

    /// <summary>
    /// Indicates whether the selected digits and increment are available.
    /// </summary>
    public bool HasRequiredRule = false;

    /// <summary>
    /// Stores the fraction digit count required by the selected rule.
    /// </summary>
    public int RequiredDigits = 0;

    /// <summary>
    /// Stores the integer rounding increment required at the selected scale.
    /// </summary>
    public int RequiredIncrement = 0;

    /// <summary>
    /// Indicates whether a rounded candidate amount is available.
    /// </summary>
    public bool HasRoundedCandidate = false;

    /// <summary>
    /// Stores the candidate amount rounded to the selected rule.
    /// </summary>
    public decimal RoundedCandidate = 0m;
}
