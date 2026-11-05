namespace currencycodes;

/// <summary>
/// Identifies the purpose or classification of a currency designation.
/// </summary>
public enum CurrencyKind
{
    /// <summary>
    /// A monetary currency used as a unit of account or exchange.
    /// </summary>
    MonetaryCurrency = 0,

    /// <summary>
    /// A registered fund or accounting-unit designation.
    /// </summary>
    Fund = 1,

    /// <summary>
    /// A precious-metal designation.
    /// </summary>
    PreciousMetal = 2,

    /// <summary>
    /// A special-purpose or supranational unit.
    /// </summary>
    SpecialUnit = 3,

    /// <summary>
    /// A designation reserved for testing.
    /// </summary>
    Testing = 4,

    /// <summary>
    /// A designation representing no universal currency.
    /// </summary>
    NoCurrency = 5,

    /// <summary>
    /// A designation whose classification is not known.
    /// </summary>
    Unknown = 6
}
