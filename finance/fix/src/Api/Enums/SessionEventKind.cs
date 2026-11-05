namespace fix;

/// <summary>Identifies an event emitted by a FIX session.</summary>
public enum SessionEventKind
{
    /// <summary>No event occurred.</summary>
    None = 0,
    /// <summary>The session started.</summary>
    Started = 1,
    /// <summary>The session stopped.</summary>
    Stopped = 2,
    /// <summary>The transport connected.</summary>
    Connected = 3,
    /// <summary>The transport disconnected.</summary>
    Disconnected = 4,
    /// <summary>A logon event occurred.</summary>
    Logon = 5,
    /// <summary>A logout event occurred.</summary>
    Logout = 6,
    /// <summary>A message was sent.</summary>
    MessageSent = 7,
    /// <summary>A message was received.</summary>
    MessageReceived = 8,
    /// <summary>A heartbeat was generated or received.</summary>
    Heartbeat = 9,
    /// <summary>A test request was generated or received.</summary>
    TestRequest = 10,
    /// <summary>A sequence gap was detected.</summary>
    SequenceGap = 11,
    /// <summary>Sequence recovery started.</summary>
    RecoveryStarted = 12,
    /// <summary>Sequence recovery completed.</summary>
    RecoveryCompleted = 13,
    /// <summary>Backpressure prevented immediate progress.</summary>
    Backpressure = 14,
    /// <summary>An operation failed.</summary>
    Error = 15
}
