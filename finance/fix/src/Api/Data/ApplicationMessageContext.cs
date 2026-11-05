namespace fix;

/// <summary>Provides session and replay state to an application callback.</summary>
public class ApplicationMessageContext
{
    /// <summary>The session identity.</summary>
    public SessionId Id = new SessionId();
    /// <summary>The callback message.</summary>
    public FixMessage Message = new FixMessage();
    /// <summary>The message sequence number.</summary>
    public int SequenceNumber;
    /// <summary>Whether the message is administrative.</summary>
    public bool IsAdministrative;
    /// <summary>Whether the message is being replayed.</summary>
    public bool IsReplay;
    /// <summary>Whether PossDupFlag is set.</summary>
    public bool IsPossibleDuplicate;
    /// <summary>Whether PossResend is set.</summary>
    public bool IsPossibleResend;
    /// <summary>The pending application-message queue count before this decision.</summary>
    public int PendingQueueCount;
}
