namespace sip;

/// <summary>Identifies a back-to-back session state.</summary>
public enum SipBackToBackState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>The two legs are being established.</summary>
    Establishing = 1,
    /// <summary>Both legs are confirmed.</summary>
    Confirmed = 2,
    /// <summary>The session is terminating.</summary>
    Terminating = 3,
    /// <summary>The session terminated.</summary>
    Terminated = 4,
    /// <summary>The session failed.</summary>
    Failed = 5
}
