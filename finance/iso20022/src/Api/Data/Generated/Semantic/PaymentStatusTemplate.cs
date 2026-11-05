namespace iso20022;

/// <summary>Provides version-neutral business values for a payment status report.</summary>
public class PaymentStatusTemplate
{
    /// <summary>The status report message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The original message identifier being reported.</summary>
    public string OriginalMessageIdentifier = string.Empty;
    /// <summary>The original end-to-end transaction identifier.</summary>
    public string OriginalEndToEndIdentifier = string.Empty;
    /// <summary>The ISO payment status code.</summary>
    public string Status = string.Empty;
    /// <summary>The human-readable status reason.</summary>
    public string StatusReason = string.Empty;
}
