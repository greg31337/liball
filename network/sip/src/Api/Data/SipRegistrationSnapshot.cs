namespace sip;

/// <summary>Holds an immutable-style snapshot of registration state.</summary>
public class SipRegistrationSnapshot
{
    /// <summary>The registration state.</summary>
    public SipRegistrationState State;
    /// <summary>The address of record.</summary>
    public SipUri? AddressOfRecord;
    /// <summary>The active bindings.</summary>
    public SipRegistrationBinding[] Bindings = new SipRegistrationBinding[0];
    /// <summary>The registrar request target.</summary>
    public SipUri? Target;
    /// <summary>The configured outbound proxy.</summary>
    public SipUri? OutboundProxy;
    /// <summary>The Service-Route set associated with the registration.</summary>
    public SipRouteValue[] ServiceRoute = new SipRouteValue[0];
    /// <summary>The registered Outbound primary and backup flows.</summary>
    public SipFlow[] Flows = new SipFlow[0];
    /// <summary>The currently selected Outbound flow.</summary>
    public SipFlow? ActiveFlow;
    /// <summary>The stable registration Call-ID.</summary>
    public string? CallId;
    /// <summary>The most recently allocated registration CSeq.</summary>
    public int CSeq = -1;
    /// <summary>The authentication context associated with registration retries.</summary>
    public string? AuthenticationContextId;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The next monotonic refresh timestamp.</summary>
    public long RefreshAtMilliseconds = -1;
    /// <summary>The monotonic registration expiry timestamp.</summary>
    public long ExpiresAtMilliseconds = -1;
    /// <summary>The next monotonic transient-failure retry timestamp.</summary>
    public long RetryAtMilliseconds = -1;
    /// <summary>The number of authentication retries attempted.</summary>
    public int AuthenticationAttemptCount;
    /// <summary>The number of redirects followed.</summary>
    public int RedirectCount;
    /// <summary>The last response.</summary>
    public SipMessage? Response;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
}
