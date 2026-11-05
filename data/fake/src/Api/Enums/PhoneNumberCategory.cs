namespace fake;

/// <summary>
/// Identifies the intended structural category of a synthetic phone fixture.
/// </summary>
/// <remarks>A category describes generation rules and does not establish carrier, subscriber, reachability, or ownership.</remarks>
public enum PhoneNumberCategory
{
    /// <summary>Uses the locale's general-purpose phone shape.</summary>
    General = 0,

    /// <summary>Uses a fixed-line-shaped fixture where the locale distinguishes one.</summary>
    Landline = 1,

    /// <summary>Uses a mobile-shaped fixture where the locale distinguishes one.</summary>
    Mobile = 2,

    /// <summary>Uses a toll-free-shaped fixture.</summary>
    TollFree = 3,

    /// <summary>Uses a premium-rate-shaped fixture.</summary>
    Premium = 4
}
