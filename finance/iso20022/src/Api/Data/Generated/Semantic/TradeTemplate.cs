namespace iso20022;

/// <summary>Provides version-neutral business values for a trade-services message.</summary>
public class TradeTemplate
{
    /// <summary>The trade-services message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The trade transaction identifier.</summary>
    public string TradeIdentifier = string.Empty;
    /// <summary>The trade baseline identifier.</summary>
    public string BaselineIdentifier = string.Empty;
    /// <summary>The buyer identifier or display name.</summary>
    public string Buyer = string.Empty;
    /// <summary>The seller identifier or display name.</summary>
    public string Seller = string.Empty;
    /// <summary>The trade amount and exact currency.</summary>
    public IsoAmountValue Amount = new IsoAmountValue();
    /// <summary>The agreed due date.</summary>
    public IsoDateValue DueDate = new IsoDateValue();
    /// <summary>The goods or services description.</summary>
    public string GoodsDescription = string.Empty;
}
