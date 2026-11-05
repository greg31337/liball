namespace fake;

/// <summary>Contains localized, explicitly synthetic business and tax identifier shapes.</summary>
/// <remarks>
/// Structural validity does not mean that either identifier is issued, registered,
/// usable for filing, or associated with any entity.
/// </remarks>
public class CompanyIdentifierRecord
{
    /// <summary>The normalized locale used to select presentation data.</summary>
    public string Locale = string.Empty;

    /// <summary>The two-letter country code controlling identifier presentation.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The generated business-registration identifier shape.</summary>
    public string BusinessIdentifier = string.Empty;

    /// <summary>A human-readable description of the business identifier format.</summary>
    public string BusinessIdentifierFormat = string.Empty;

    /// <summary>The generated tax-registration identifier shape.</summary>
    public string TaxIdentifier = string.Empty;

    /// <summary>A human-readable description of the tax identifier format.</summary>
    public string TaxIdentifierFormat = string.Empty;

    /// <summary>Whether the business identifier passes this library's structural validation.</summary>
    public bool IsBusinessIdentifierValid;

    /// <summary>Whether the tax identifier passes this library's structural validation.</summary>
    public bool IsTaxIdentifierValid;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record claims a registered entity exists, always <c>false</c>.</summary>
    public bool RepresentsRegisteredEntity;

    /// <summary>Whether the record claims tax registration, always <c>false</c>.</summary>
    public bool RepresentsTaxRegistration;

    internal CompanyIdentifierRecord()
    {
    }
}
