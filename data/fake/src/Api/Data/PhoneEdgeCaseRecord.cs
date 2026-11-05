namespace fake;

/// <summary>
/// Describes one explicit phone-number modeling edge case.
/// </summary>
/// <remarks>
/// Values are fixture shapes only. No record asserts current validity, dialability,
/// reachability, allocation, subscriber identity, or geographic location.
/// </remarks>
public class PhoneEdgeCaseRecord
{
    /// <summary>The edge-case scenario represented by this record.</summary>
    public PhoneEdgeCase Case;

    /// <summary>The primary formatted phone fixture.</summary>
    public string Number = string.Empty;

    /// <summary>A related contrasting number, or an empty string when the case needs only one value.</summary>
    public string RelatedNumber = string.Empty;

    /// <summary>The calling-code prefix shared by the represented values.</summary>
    public string CallingCode = string.Empty;

    /// <summary>The possible region codes, or an empty array for a non-geographic service.</summary>
    public string[] RegionCodes = new string[0];

    /// <summary>The possible structural categories retained for this fixture.</summary>
    public PhoneNumberCategory[] PossibleCategories = new PhoneNumberCategory[0];

    /// <summary>Whether related values intentionally demonstrate different number lengths.</summary>
    public bool HasVariableLength;

    /// <summary>Whether the calling code is associated with multiple regions.</summary>
    public bool CallingCodeIsShared;

    /// <summary>Whether a leading zero after the calling code is part of the national number.</summary>
    public bool LeadingZeroIsSignificant;

    /// <summary>Whether <c>Number</c> contains an extension suffix.</summary>
    public bool HasExtension;

    /// <summary>The extension digits, or an empty string when no extension is present.</summary>
    public string Extension = string.Empty;

    /// <summary>Whether the calling code represents a non-geographic service.</summary>
    public bool IsNonGeographic;

    /// <summary>Whether the number shape alone cannot determine one category.</summary>
    public bool CategoryIsAmbiguous;

    /// <summary>Whether reachability is known; fixture records always report false.</summary>
    public bool ReachabilityKnown;

    /// <summary>Whether a subscriber identity is known; fixture records always report false.</summary>
    public bool SubscriberIdentityKnown;

    /// <summary>Whether a geographic location is known; fixture records always report false.</summary>
    public bool GeographicLocationKnown;

    /// <summary>The generalized generation and validation metadata for this requested edge case.</summary>
    public GenerationMetadata Metadata = new GenerationMetadata();

    internal PhoneEdgeCaseRecord()
    {
    }
}
