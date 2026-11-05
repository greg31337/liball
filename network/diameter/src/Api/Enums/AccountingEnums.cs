namespace diameter;

/// <summary>
/// Identifies an RFC 6733 accounting record type.
/// </summary>
public enum DiameterAccountingRecordType
{
    /// <summary>Records a one-time event.</summary>
    Event = 1,

    /// <summary>Starts an accounting session.</summary>
    Start = 2,

    /// <summary>Reports cumulative data for an accounting session.</summary>
    Interim = 3,

    /// <summary>Stops an accounting session.</summary>
    Stop = 4
}

/// <summary>
/// Identifies RFC 6733 accounting delivery failure behavior.
/// </summary>
public enum DiameterAccountingRealtimeRequired
{
    /// <summary>Requires delivery before service is granted.</summary>
    DeliverAndGrant = 1,

    /// <summary>Allows service when the record can be stored.</summary>
    GrantAndStore = 2,

    /// <summary>Allows service even when the record is lost.</summary>
    GrantAndLose = 3
}

/// <summary>
/// Identifies the lifecycle state of an accounting operation.
/// </summary>
public enum DiameterAccountingState
{
    /// <summary>No accounting operation is active.</summary>
    Idle = 0,

    /// <summary>An event record is pending.</summary>
    PendingEvent = 1,

    /// <summary>An accounting session is open.</summary>
    Open = 2,

    /// <summary>An interim record is pending.</summary>
    PendingInterim = 3,

    /// <summary>A stop record is pending.</summary>
    PendingStop = 4,

    /// <summary>The accounting operation is closed.</summary>
    Closed = 5,

    /// <summary>The accounting operation failed.</summary>
    Failed = 6
}
