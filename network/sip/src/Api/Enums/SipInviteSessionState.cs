namespace sip;

/// <summary>Identifies an INVITE usage state.</summary>
public enum SipInviteSessionState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>An outgoing INVITE is being sent.</summary>
    Calling = 1,
    /// <summary>The INVITE usage is proceeding.</summary>
    Proceeding = 2,
    /// <summary>An early dialog exists.</summary>
    Early = 3,
    /// <summary>The INVITE usage is confirmed.</summary>
    Confirmed = 4,
    /// <summary>The INVITE usage is terminating.</summary>
    Terminating = 5,
    /// <summary>The INVITE usage is terminated.</summary>
    Terminated = 6
}
