namespace sip;

/// <summary>Identifies a registration client state.</summary>
public enum SipRegistrationState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>An initial registration is pending.</summary>
    Registering = 1,
    /// <summary>The registration is active.</summary>
    Registered = 2,
    /// <summary>A refresh is pending.</summary>
    Refreshing = 3,
    /// <summary>Unregistration is pending.</summary>
    Unregistering = 4,
    /// <summary>The registration failed.</summary>
    Failed = 5,
    /// <summary>The registration usage terminated.</summary>
    Terminated = 6
}
