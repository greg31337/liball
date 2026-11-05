namespace iso20022;

/// <summary>Represents an ISO amount with its exact decimal and currency code.</summary>
public class IsoAmountValue
{
    /// <summary>The decimal amount.</summary>
    public IsoDecimalValue Amount = new IsoDecimalValue();
    /// <summary>The currency code.</summary>
    public string Currency = string.Empty;
}
