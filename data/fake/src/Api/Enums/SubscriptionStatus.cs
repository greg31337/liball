namespace fake;

/// <summary>Selects the status of a synthetic subscription fixture.</summary>
public enum SubscriptionStatus
{
    /// <summary>The fixture is in a trial-shaped state.</summary>
    Trial = 0,

    /// <summary>The fixture is in an active-shaped state.</summary>
    Active = 1,

    /// <summary>The fixture is in a paused-shaped state.</summary>
    Paused = 2,

    /// <summary>The fixture is in a cancelled-shaped state.</summary>
    Cancelled = 3,

    /// <summary>The fixture is in an expired-shaped state.</summary>
    Expired = 4
}
