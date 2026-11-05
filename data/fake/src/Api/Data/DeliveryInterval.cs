using System;

namespace fake;

/// <summary>Represents a method-correlated synthetic estimated-delivery interval.</summary>
/// <remarks>The interval is not a carrier commitment or delivery guarantee.</remarks>
public class DeliveryInterval
{
    /// <summary>The shipping method used to derive the interval.</summary>
    public string Method = string.Empty;

    /// <summary>The captured reference time from which transit is estimated.</summary>
    public DateTimeOffset ReferenceTime;

    /// <summary>The beginning of the estimated delivery interval.</summary>
    public DateTimeOffset StartsAt;

    /// <summary>The end of the estimated delivery interval.</summary>
    public DateTimeOffset EndsAt;

    /// <summary>The minimum transit days represented by <c>StartsAt</c>.</summary>
    public int MinimumTransitDays;

    /// <summary>The maximum transit days represented by <c>EndsAt</c>.</summary>
    public int MaximumTransitDays;

    /// <summary>Whether the interval is a guaranteed delivery commitment, always <c>false</c>.</summary>
    public bool IsGuaranteed;

    internal DeliveryInterval()
    {
    }
}
