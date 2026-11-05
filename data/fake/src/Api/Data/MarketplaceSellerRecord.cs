namespace fake;

/// <summary>Represents an explicitly synthetic marketplace seller fixture.</summary>
/// <remarks>The record does not represent an active, verified, registered, or reachable seller.</remarks>
public class MarketplaceSellerRecord
{
    /// <summary>The synthetic seller-identifier shape.</summary>
    public string Identifier = string.Empty;

    /// <summary>The seller display name copied from <c>Company</c>.</summary>
    public string DisplayName = string.Empty;

    /// <summary>The project-authored retail department associated with the fixture.</summary>
    public string RetailDepartment = string.Empty;

    /// <summary>The reserved example storefront domain copied from <c>Company</c>.</summary>
    public string StorefrontDomain = string.Empty;

    /// <summary>The reserved example support mailbox copied from <c>Company</c>.</summary>
    public string SupportEmail = string.Empty;

    /// <summary>The correlated explicitly synthetic company profile.</summary>
    public CompanyProfile Company = new CompanyProfile();

    /// <summary>Whether the seller fixture is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the fixture represents an active seller account, always <c>false</c>.</summary>
    public bool RepresentsActiveSeller;

    /// <summary>Whether the fixture represents a verified seller, always <c>false</c>.</summary>
    public bool RepresentsVerifiedSeller;

    internal MarketplaceSellerRecord()
    {
    }
}
