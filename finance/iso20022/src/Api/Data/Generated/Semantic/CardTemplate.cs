namespace iso20022;

/// <summary>Provides version-neutral business values for a card transaction message.</summary>
public class CardTemplate
{
    /// <summary>The card message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The card transaction identifier.</summary>
    public string TransactionIdentifier = string.Empty;
    /// <summary>The acquiring party identifier.</summary>
    public string AcquirerIdentifier = string.Empty;
    /// <summary>The merchant identifier.</summary>
    public string MerchantIdentifier = string.Empty;
    /// <summary>A masked card identifier safe for message construction.</summary>
    public string MaskedCardIdentifier = string.Empty;
    /// <summary>The transaction amount and exact currency.</summary>
    public IsoAmountValue Amount = new IsoAmountValue();
    /// <summary>The transaction date and time.</summary>
    public IsoDateTimeValue TransactionDateTime = new IsoDateTimeValue();
}
