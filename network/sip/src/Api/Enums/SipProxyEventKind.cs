namespace sip;

/// <summary>Identifies a proxy event.</summary>
public enum SipProxyEventKind
{
    /// <summary>No event is available.</summary>
    None = 0,
    /// <summary>A request was accepted.</summary>
    RequestAccepted = 1,
    /// <summary>A branch was created.</summary>
    BranchCreated = 2,
    /// <summary>A provisional response was forwarded.</summary>
    ProvisionalForwarded = 3,
    /// <summary>A final response was selected.</summary>
    FinalSelected = 4,
    /// <summary>A branch was cancelled.</summary>
    BranchCancelled = 5,
    /// <summary>Proxy processing completed.</summary>
    Completed = 6,
    /// <summary>Proxy processing failed.</summary>
    Failed = 7,
    /// <summary>A stateful branch timed out.</summary>
    BranchTimedOut = 8,
    /// <summary>A response arrived after its stateful operation completed.</summary>
    LateResponseIgnored = 9,
    /// <summary>A branch CANCEL request was forwarded.</summary>
    CancelForwarded = 10,
    /// <summary>An ACK request was forwarded.</summary>
    AckForwarded = 11,
    /// <summary>An additional successful branch response was forwarded.</summary>
    AdditionalSuccessForwarded = 12,
    /// <summary>A hop-by-hop failure ACK was absorbed.</summary>
    AckAbsorbed = 13
}
