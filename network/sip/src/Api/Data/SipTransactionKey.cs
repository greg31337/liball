namespace sip;

/// <summary>Holds the fields used to match a SIP transaction.</summary>
public class SipTransactionKey
{
    /// <summary>The top Via branch.</summary>
    public string? Branch;
    /// <summary>The top Via sent-by value.</summary>
    public string? SentBy;
    /// <summary>The recognized request method.</summary>
    public SipMethod Method;
    /// <summary>The original request method token.</summary>
    public string? MethodToken;
    /// <summary>The Call-ID used by legacy matching.</summary>
    public string? CallId;
    /// <summary>The CSeq number used by legacy matching.</summary>
    public int CSeq = -1;
    /// <summary>The transaction role.</summary>
    public SipTransactionRole Role;

    internal SipUri? LegacyRequestUri;
    internal SipNameAddress? LegacyFrom;
    internal SipNameAddress? LegacyTo;
    internal SipViaValue? LegacyTopVia;
}
