using System;

namespace fake;

/// <summary>Represents a project-fictional carrier service and its transit bounds.</summary>
public class ShippingService
{
    /// <summary>The project-fictional carrier name.</summary>
    public string Carrier = string.Empty;

    /// <summary>The generic shipping method.</summary>
    public string Method = string.Empty;

    /// <summary>The minimum estimated transit time in whole days.</summary>
    public int MinimumTransitDays;

    /// <summary>The maximum estimated transit time in whole days.</summary>
    public int MaximumTransitDays;

    /// <summary>Whether the service is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    internal ShippingService()
    {
    }
}
