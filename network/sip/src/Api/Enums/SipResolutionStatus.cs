namespace sip;

/// <summary>Identifies the outcome of SIP target resolution.</summary>
public enum SipResolutionStatus
{
    /// <summary>No result is available.</summary>
    None = 0,
    /// <summary>One or more candidates were resolved.</summary>
    Success = 1,
    /// <summary>No usable records were found.</summary>
    NoRecords = 2,
    /// <summary>Resolution timed out.</summary>
    Timeout = 3,
    /// <summary>Resolution failed.</summary>
    Failed = 4,
    /// <summary>Resolution was cancelled.</summary>
    Cancelled = 5
}
