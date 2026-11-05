namespace cache;

/// <summary>
/// Describes cache policy settings to change after construction.
/// </summary>
/// <remarks>
/// Each <c>Update...</c> flag controls whether its matching value participates in the operation. Non-positive capacity values disable their limits; negative expiration values disable their defaults, while zero remains an enabled immediate expiration boundary or duration.
/// </remarks>
public class CachePolicyUpdate
{
    /// <summary>Indicates whether <see cref="MaximumEntries"/> changes the entry-count limit.</summary>
    public bool UpdateMaximumEntries;
    /// <summary>Stores the requested maximum entry count.</summary>
    public long MaximumEntries;
    /// <summary>Indicates whether <see cref="MaximumWeight"/> changes the total-weight limit.</summary>
    public bool UpdateMaximumWeight;
    /// <summary>Stores the requested maximum total weight.</summary>
    public long MaximumWeight;
    /// <summary>Indicates whether <see cref="DefaultAbsoluteExpirationUtcTicks"/> changes the default fixed expiration boundary.</summary>
    public bool UpdateDefaultAbsoluteExpiration;
    /// <summary>Stores the requested default fixed UTC expiration boundary.</summary>
    public long DefaultAbsoluteExpirationUtcTicks;
    /// <summary>Indicates whether <see cref="DefaultTimeToLiveTicks"/> changes the default write-relative expiration.</summary>
    public bool UpdateDefaultTimeToLive;
    /// <summary>Stores the requested default time-to-live duration in .NET ticks.</summary>
    public long DefaultTimeToLiveTicks;
    /// <summary>Indicates whether <see cref="DefaultSlidingExpirationTicks"/> changes the default idle expiration.</summary>
    public bool UpdateDefaultSlidingExpiration;
    /// <summary>Stores the requested default sliding-expiration duration in .NET ticks.</summary>
    public long DefaultSlidingExpirationTicks;

    /// <summary>Creates an empty update that leaves every policy setting unchanged.</summary>
    public CachePolicyUpdate()
    {
        UpdateMaximumEntries = false;
        MaximumEntries = 0L;
        UpdateMaximumWeight = false;
        MaximumWeight = 0L;
        UpdateDefaultAbsoluteExpiration = false;
        DefaultAbsoluteExpirationUtcTicks = 0L;
        UpdateDefaultTimeToLive = false;
        DefaultTimeToLiveTicks = 0L;
        UpdateDefaultSlidingExpiration = false;
        DefaultSlidingExpirationTicks = 0L;
    }
}
