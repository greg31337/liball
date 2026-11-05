namespace sip;

/// <summary>Identifies a REFER transfer lifecycle state.</summary>
public enum SipReferState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>The REFER request is awaiting a final response.</summary>
    Sending = 1,
    /// <summary>The REFER was accepted and awaits notification progress.</summary>
    Accepted = 2,
    /// <summary>The referred operation is pending.</summary>
    Pending = 3,
    /// <summary>The referred operation is active.</summary>
    Active = 4,
    /// <summary>The referred operation completed successfully.</summary>
    Succeeded = 5,
    /// <summary>The REFER or referred operation failed.</summary>
    Failed = 6,
    /// <summary>The implicit subscription terminated without a final operation status.</summary>
    Terminated = 7
}
