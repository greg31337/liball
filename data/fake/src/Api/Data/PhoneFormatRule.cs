namespace fake;

/// <summary>
/// Describes one locale phone-format rule.
/// </summary>
/// <remarks>Within masks, <c>#</c> means a decimal digit and <c>N</c> means a digit from 2 through 9.</remarks>
public class PhoneFormatRule
{
    /// <summary>The phone category described by this rule.</summary>
    public PhoneNumberCategory Category;

    /// <summary>The category-specific area or service-code mask.</summary>
    public string AreaCodeMask = string.Empty;

    /// <summary>The category-specific subscriber-number mask.</summary>
    public string SubscriberMask = string.Empty;

    /// <summary>The national display mask.</summary>
    public string NationalMask = string.Empty;

    /// <summary>The international display mask.</summary>
    public string InternationalMask = string.Empty;

    /// <summary>The E.164-shaped mask.</summary>
    public string E164Mask = string.Empty;

    /// <summary>The fixed area or service codes admitted by this rule, or an empty array when the general area-code mask applies.</summary>
    public string[] AreaCodes = new string[0];

    /// <summary>Whether the rule permits an extension suffix.</summary>
    public bool AllowsExtension;

    /// <summary>Whether this category can be distinguished from the number shape alone.</summary>
    public bool DistinguishableByNumber;

    internal PhoneFormatRule()
    {
    }
}
