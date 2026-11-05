namespace fake;

/// <summary>Represents a correlated, explicitly synthetic company fixture.</summary>
/// <remarks>
/// The profile does not establish registration, ownership, trademark clearance,
/// contact reachability, address deliverability, or tax status. Jurisdiction,
/// phone-region, and fictional address-country fields are intentionally distinct.
/// </remarks>
public class CompanyProfile
{
    /// <summary>The normalized locale used for company presentation.</summary>
    public string Locale = string.Empty;

    /// <summary>The company-identifier jurisdiction selected from the locale.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The generated fictional company name.</summary>
    public string Name = string.Empty;

    /// <summary>The reserved example-domain subdomain derived from <c>Name</c>.</summary>
    public string Domain = string.Empty;

    /// <summary>The <c>contact</c> mailbox at <c>Domain</c>.</summary>
    public string Email = string.Empty;

    /// <summary>The generated generic industry label.</summary>
    public string Industry = string.Empty;

    /// <summary>The generated project-authored catch phrase.</summary>
    public string CatchPhrase = string.Empty;

    /// <summary>The generated project-authored company slogan.</summary>
    public string Slogan = string.Empty;

    /// <summary>The correlated phone fixture and its region and safety metadata.</summary>
    public PhoneRecord Phone = new PhoneRecord();

    /// <summary>The phone catalog's region code, copied from <c>Phone</c>.</summary>
    public string PhoneRegionCode = string.Empty;

    /// <summary>The correlated postal-address fixture and geographic metadata.</summary>
    public AddressRecord Address = new AddressRecord();

    /// <summary>The fictional address country code, copied from <c>Address</c>.</summary>
    public string AddressFixtureCountryCode = string.Empty;

    /// <summary>The correlated business and tax identifier fixture.</summary>
    public CompanyIdentifierRecord Identifiers = new CompanyIdentifierRecord();

    /// <summary>Whether <c>Domain</c> is beneath an RFC-reserved example domain, always <c>true</c>.</summary>
    public bool DomainIsReservedExample;

    /// <summary>Whether <c>Email</c> uses the reserved example domain, always <c>true</c>.</summary>
    public bool EmailIsReservedExample;

    /// <summary>Whether the profile is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the profile claims a registered entity exists, always <c>false</c>.</summary>
    public bool RepresentsRegisteredEntity;

    /// <summary>Whether the profile claims its contact fields are reachable, always <c>false</c>.</summary>
    public bool RepresentsReachableContact;

    /// <summary>Whether the profile claims its address is deliverable, always <c>false</c>.</summary>
    public bool RepresentsDeliverableAddress;

    internal CompanyProfile()
    {
    }
}
