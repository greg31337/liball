namespace fake;

/// <summary>
/// Describes the safety policy applied to phone-number generation for a locale and category.
/// </summary>
public class PhoneSafetyMetadata
{
    /// <summary>The safety mode requested through the captured fake options.</summary>
    public SafetyMode RequestedMode;

    /// <summary>The safety mode actually applied to the requested phone category.</summary>
    public SafetyMode AppliedMode;

    /// <summary>The phone category described by this safety decision.</summary>
    public PhoneNumberCategory Category;

    /// <summary>The region code of the resolved phone catalog.</summary>
    public string RegionCode = string.Empty;

    /// <summary>Whether generation uses the authoritative safe-fiction range in <c>SafeRange</c>.</summary>
    public bool UsesAuthoritativeSafeRange;

    /// <summary>Whether the requested mode was unavailable and generation fell back to another mode.</summary>
    public bool UsedFallback;

    /// <summary>An explicit explanation when fallback was required, or an empty string otherwise.</summary>
    public string FallbackReason = string.Empty;

    /// <summary>The authoritative safe-fiction range used for generation, or null when no range is applied.</summary>
    public PhoneSafeRange? SafeRange;

    internal PhoneSafetyMetadata()
    {
        RegionCode = string.Empty;
        FallbackReason = string.Empty;
    }
}
