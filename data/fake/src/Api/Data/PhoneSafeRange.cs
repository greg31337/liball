namespace fake;

/// <summary>
/// Describes an authoritative safe-fiction range admitted by locale phone metadata.
/// </summary>
/// <remarks>The safety claim applies only to the stated range and source version, not to its area code or other numbers sharing its shape.</remarks>
public class PhoneSafeRange
{
    /// <summary>The category for which the range may be used.</summary>
    public PhoneNumberCategory Category;

    /// <summary>The locale area-code mask combined with the safe subscriber range.</summary>
    public string AreaCodeMask = string.Empty;

    /// <summary>The fixed exchange or central-office code.</summary>
    public string Exchange = string.Empty;

    /// <summary>The inclusive first subscriber value.</summary>
    public string FirstSubscriber = string.Empty;

    /// <summary>The inclusive last subscriber value.</summary>
    public string LastSubscriber = string.Empty;

    /// <summary>A national display mask constrained to the safe range.</summary>
    public string NationalMask = string.Empty;

    /// <summary>An international display mask constrained to the safe range.</summary>
    public string InternationalMask = string.Empty;

    /// <summary>An E.164-shaped mask constrained to the safe range.</summary>
    public string E164Mask = string.Empty;

    /// <summary>The authority that published the fictional-use range.</summary>
    public string Authority = string.Empty;

    /// <summary>The reviewed source version for the safety claim.</summary>
    public string SourceVersion = string.Empty;

    /// <summary>Whether the range has an explicit authoritative safe-fiction claim.</summary>
    public bool IsAuthoritative;

    internal PhoneSafeRange()
    {
    }
}
