namespace fake;

/// <summary>Represents an explicitly synthetic subscription-plan fixture.</summary>
/// <remarks>The record does not represent an account, entitlement, billing instruction, or purchasable offer.</remarks>
public class SubscriptionPlan
{
    /// <summary>The project-authored plan name.</summary>
    public string Name = string.Empty;

    /// <summary>The generated subscription status.</summary>
    public SubscriptionStatus Status;

    /// <summary>The generated billing interval.</summary>
    public SubscriptionInterval Interval;

    /// <summary>The calculated fixture price for the plan and interval.</summary>
    public decimal Price;

    /// <summary>The currency code associated with <c>Price</c>.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>Whether the plan is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the plan represents a real subscription, always <c>false</c>.</summary>
    public bool RepresentsRealSubscription;

    internal SubscriptionPlan()
    {
    }
}
