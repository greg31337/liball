namespace fake;

/// <summary>
/// Describes the locale and structural context needed to parse a generated phone fixture.
/// </summary>
/// <remarks>
/// Category is preserved generation metadata and is not necessarily inferable from the
/// number. This metadata makes no carrier, subscriber, ownership, allocation, or
/// reachability claim.
/// </remarks>
public class PhoneRecordParsingMetadata
{
    /// <summary>The region code of the resolved phone catalog.</summary>
    public string RegionCode = string.Empty;

    /// <summary>The international calling code including its leading plus sign.</summary>
    public string CallingCode = string.Empty;

    /// <summary>The national dialing or trunk prefix, which may be empty.</summary>
    public string NationalPrefix = string.Empty;

    /// <summary>The category retained from the generation request.</summary>
    public PhoneNumberCategory Category;

    /// <summary>The textual presentation used by the record.</summary>
    public PhoneNumberFormat Format;

    /// <summary>The exact mask used to produce the record's formatted number.</summary>
    public string FormatMask = string.Empty;

    /// <summary>The area or service-code component mask.</summary>
    public string AreaCodeMask = string.Empty;

    /// <summary>The subscriber-number component mask.</summary>
    public string SubscriberMask = string.Empty;

    /// <summary>The locale prefix used before an extension.</summary>
    public string ExtensionPrefix = string.Empty;

    /// <summary>Whether this category is structurally distinguishable from the number alone.</summary>
    public bool CategoryDistinguishableByNumber;

    /// <summary>Whether the category permits an extension suffix.</summary>
    public bool AllowsExtension;

    internal PhoneRecordParsingMetadata()
    {
    }
}
