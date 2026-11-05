namespace currencycodes;

/// <summary>
/// Identifies whether a currency designation is current or withdrawn.
/// </summary>
public enum CurrencyStatus
{
    /// <summary>
    /// The currency designation is currently assigned.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The currency designation has been withdrawn.
    /// </summary>
    Historical = 1
}
