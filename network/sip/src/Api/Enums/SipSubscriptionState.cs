namespace sip;

/// <summary>Identifies a subscription state.</summary>
public enum SipSubscriptionState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>The subscription is pending.</summary>
    Pending = 1,
    /// <summary>The subscription is active.</summary>
    Active = 2,
    /// <summary>The subscription is terminated.</summary>
    Terminated = 3
}
