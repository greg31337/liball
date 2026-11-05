namespace fake;

/// <summary>
/// Contains locale-resolved phone masks, dialing rules, categories, extension rules, and safe ranges.
/// </summary>
/// <remarks>Metadata describes fixture structure only and makes no claim about a carrier, subscriber, location, allocation, or reachability.</remarks>
public class PhoneLocaleMetadata
{
    internal string RequestedLocale = string.Empty;
    internal string SourceLocale = string.Empty;
    internal bool UsedLocaleFallback;

    /// <summary>The region code described by the resolved fixture catalog.</summary>
    public string RegionCode = string.Empty;

    /// <summary>The international calling code including its leading plus sign.</summary>
    public string CallingCode = string.Empty;

    /// <summary>The prefix dialed from the region before an international destination.</summary>
    public string InternationalDialingPrefix = string.Empty;

    /// <summary>The national dialing or trunk prefix, which may be empty.</summary>
    public string NationalPrefix = string.Empty;

    /// <summary>The general area-code mask.</summary>
    public string AreaCodeMask = string.Empty;

    /// <summary>The general subscriber-number mask.</summary>
    public string SubscriberMask = string.Empty;

    /// <summary>The display prefix placed before an extension.</summary>
    public string ExtensionPrefix = string.Empty;

    /// <summary>The minimum supported extension length in decimal digits.</summary>
    public int MinimumExtensionDigits;

    /// <summary>The maximum supported extension length in decimal digits.</summary>
    public int MaximumExtensionDigits;

    /// <summary>The available category-specific format rules.</summary>
    public PhoneFormatRule[] Formats = new PhoneFormatRule[0];

    /// <summary>The authoritative safe-fiction ranges available to this locale catalog.</summary>
    public PhoneSafeRange[] SafeRanges = new PhoneSafeRange[0];

    /// <summary>Whether at least one authoritative safe-fiction range is available.</summary>
    public bool HasAuthoritativeSafeRange;

    /// <summary>Whether the locale catalog supports alphabetic telephone-keypad formatting.</summary>
    public bool SupportsPhoneWords;

    internal PhoneLocaleMetadata()
    {
    }
}
