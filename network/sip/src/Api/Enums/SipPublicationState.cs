namespace sip;

/// <summary>Identifies a publication client state.</summary>
public enum SipPublicationState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>An initial publication is pending.</summary>
    Publishing = 1,
    /// <summary>The publication is active.</summary>
    Published = 2,
    /// <summary>A publication refresh is pending.</summary>
    Refreshing = 3,
    /// <summary>Publication removal is pending.</summary>
    Removing = 4,
    /// <summary>The publication failed.</summary>
    Failed = 5,
    /// <summary>The publication usage terminated.</summary>
    Terminated = 6
}
