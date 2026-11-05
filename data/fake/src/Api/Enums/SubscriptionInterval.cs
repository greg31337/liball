namespace fake;

/// <summary>Selects the billing interval of a synthetic subscription plan.</summary>
public enum SubscriptionInterval
{
    /// <summary>A weekly interval.</summary>
    Weekly = 0,

    /// <summary>A monthly interval.</summary>
    Monthly = 1,

    /// <summary>A quarterly interval.</summary>
    Quarterly = 2,

    /// <summary>A yearly interval.</summary>
    Yearly = 3
}
