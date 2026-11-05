namespace currencycodes;

/// <summary>
/// Stores the structured outcome of a major-unit or minor-unit conversion.
/// </summary>
public class MinorUnitConversionResult
{
    /// <summary>
    /// Indicates whether conversion succeeded.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// Identifies the conversion failure category.
    /// </summary>
    public AmountValidationError Error = AmountValidationError.None;

    /// <summary>
    /// Stores an invariant explanation of the conversion outcome.
    /// </summary>
    public string Reason = string.Empty;

    /// <summary>
    /// Stores the matched currency when one is available.
    /// </summary>
    public CurrencyInfo? Currency = null;

    /// <summary>
    /// Stores the major-unit amount.
    /// </summary>
    public decimal MajorAmount = 0m;

    /// <summary>
    /// Stores the signed minor-unit value.
    /// </summary>
    public long MinorUnitValue = 0;
}
