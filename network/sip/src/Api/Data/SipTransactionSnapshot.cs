namespace sip;

/// <summary>Holds an immutable-style snapshot of transaction state.</summary>
public class SipTransactionSnapshot
{
    /// <summary>The transaction identifier.</summary>
    public string? Id;
    /// <summary>The transaction matching key.</summary>
    public SipTransactionKey? Key;
    /// <summary>The transaction family.</summary>
    public SipTransactionKind Kind;
    /// <summary>The transaction role.</summary>
    public SipTransactionRole Role;
    /// <summary>The current transaction state.</summary>
    public SipTransactionState State;
    /// <summary>The initiating request.</summary>
    public SipMessage? Request;
    /// <summary>The most recent response.</summary>
    public SipMessage? Response;
    /// <summary>The active timer names.</summary>
    public string[] TimerNames = new string[0];
    /// <summary>The active timer deadlines in monotonic milliseconds.</summary>
    public long[] TimerDeadlines = new long[0];
    /// <summary>The creation timestamp in monotonic milliseconds.</summary>
    public long CreatedAtMilliseconds = -1;
    /// <summary>The last update timestamp in monotonic milliseconds.</summary>
    public long UpdatedAtMilliseconds = -1;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
