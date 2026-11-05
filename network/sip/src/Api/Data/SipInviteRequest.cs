namespace sip;

/// <summary>Describes an initial or in-dialog INVITE usage operation.</summary>
public class SipInviteRequest
{
    /// <summary>The outgoing request or incoming response message.</summary>
    public SipMessage? Message;
    /// <summary>The initial remote target, or local target advertised by a refresh.</summary>
    public SipUri? Target;
    /// <summary>The offer/answer data for the operation.</summary>
    public SipOfferAnswer? OfferAnswer;
    /// <summary>The INVITE usage options.</summary>
    public SipInviteOptions Options = new SipInviteOptions();
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The absolute monotonic deadline, or negative one when unbounded.</summary>
    public long DeadlineMilliseconds = -1;
    /// <summary>The authentication context associated with retries and dialogs.</summary>
    public string? AuthenticationContextId;
}
