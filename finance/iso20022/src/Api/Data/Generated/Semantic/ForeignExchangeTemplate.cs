namespace iso20022;

/// <summary>Provides version-neutral business values for a foreign-exchange message.</summary>
public class ForeignExchangeTemplate
{
    /// <summary>The foreign-exchange message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The foreign-exchange trade identifier.</summary>
    public string TradeIdentifier = string.Empty;
    /// <summary>The counterparty identifier or display name.</summary>
    public string Counterparty = string.Empty;
    /// <summary>The bought amount and exact currency.</summary>
    public IsoAmountValue BoughtAmount = new IsoAmountValue();
    /// <summary>The sold amount and exact currency.</summary>
    public IsoAmountValue SoldAmount = new IsoAmountValue();
    /// <summary>The agreed exchange rate with exact decimal precision.</summary>
    public IsoDecimalValue ExchangeRate = new IsoDecimalValue();
    /// <summary>The trade date.</summary>
    public IsoDateValue TradeDate = new IsoDateValue();
    /// <summary>The settlement date.</summary>
    public IsoDateValue SettlementDate = new IsoDateValue();
}
