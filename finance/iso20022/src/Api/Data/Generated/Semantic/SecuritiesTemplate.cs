namespace iso20022;

/// <summary>Provides version-neutral business values for a securities message.</summary>
public class SecuritiesTemplate
{
    /// <summary>The securities message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The securities transaction identifier.</summary>
    public string TransactionIdentifier = string.Empty;
    /// <summary>The security identifier, such as an ISIN.</summary>
    public string SecurityIdentifier = string.Empty;
    /// <summary>The securities account identifier.</summary>
    public string AccountIdentifier = string.Empty;
    /// <summary>The exact securities quantity.</summary>
    public IsoDecimalValue Quantity = new IsoDecimalValue();
    /// <summary>The settlement amount and exact currency.</summary>
    public IsoAmountValue SettlementAmount = new IsoAmountValue();
    /// <summary>The requested or effective settlement date.</summary>
    public IsoDateValue SettlementDate = new IsoDateValue();
    /// <summary>The delivering party.</summary>
    public string DeliveringParty = string.Empty;
    /// <summary>The receiving party.</summary>
    public string ReceivingParty = string.Empty;
}
