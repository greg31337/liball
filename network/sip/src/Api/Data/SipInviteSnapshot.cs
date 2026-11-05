namespace sip;

/// <summary>Holds an immutable-style snapshot of one INVITE usage.</summary>
public class SipInviteSnapshot
{
    /// <summary>The current INVITE usage state.</summary>
    public SipInviteSessionState State;
    /// <summary>The local dialog role.</summary>
    public SipDialogRole Role;
    /// <summary>The current or terminal dialog snapshot.</summary>
    public SipDialogSnapshot? Dialog;
    /// <summary>The current offer/answer state.</summary>
    public SipOfferAnswer? OfferAnswer;
    /// <summary>The initial INVITE while it remains live.</summary>
    public SipMessage? Invite;
    /// <summary>The most recently observed or generated response.</summary>
    public SipMessage? LastResponse;
    /// <summary>The active timer names.</summary>
    public string[] TimerNames = new string[0];
    /// <summary>The corresponding monotonic timer deadlines.</summary>
    public long[] TimerDeadlines = new long[0];
    /// <summary>The earliest active monotonic deadline, or negative one.</summary>
    public long NextDeadlineMilliseconds = -1;
    /// <summary>Indicates that protocol cancellation is in progress.</summary>
    public bool CancellationPending;
    /// <summary>The cancellation identifier.</summary>
    public string? CancellationId;
    /// <summary>The authentication context retained across related requests.</summary>
    public string? AuthenticationContextId;
    /// <summary>The number of authenticated retry requests after the first attempt.</summary>
    public int AuthenticationRetryCount;
    /// <summary>The terminal transport failure description.</summary>
    public string? ErrorMessage;
    /// <summary>The creation timestamp in monotonic milliseconds.</summary>
    public long CreatedAtMilliseconds = -1;
    /// <summary>The latest state observation timestamp.</summary>
    public long UpdatedAtMilliseconds = -1;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
