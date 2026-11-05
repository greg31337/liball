namespace fake;

/// <summary>
/// Represents a correlated synthetic postal address and its geographic metadata.
/// </summary>
/// <remarks>
/// Reserved mode uses project-defined example fixtures and guarantees that the complete
/// address is neither an existing nor deliverable postal address. Plausible and unrestricted
/// modes generate realistic-looking combinations that can accidentally resemble real data;
/// those modes make no nonexistence or non-deliverability guarantee. No mode validates an
/// address against postal or property records, and no output is a claim of deliverability.
/// </remarks>
public class AddressRecord
{
    /// <summary>The safety mode captured for this record.</summary>
    public SafetyMode SafetyMode;

    /// <summary>Whether the selected mode guarantees that the complete address is not deliverable.</summary>
    public bool NonDeliverabilityGuaranteed;

    /// <summary>Whether the selected mode guarantees that the complete address does not exist.</summary>
    public bool NonexistenceGuaranteed;

    /// <summary>The generated building number.</summary>
    public string BuildingNumber = string.Empty;

    /// <summary>The generated street name without its type.</summary>
    public string StreetName = string.Empty;

    /// <summary>The generated full street-type name.</summary>
    public string StreetType = string.Empty;

    /// <summary>The street name followed by its type.</summary>
    public string Street = string.Empty;

    /// <summary>The building number followed by the street.</summary>
    public string StreetAddress = string.Empty;

    /// <summary>The generated secondary-unit component.</summary>
    public string Unit = string.Empty;

    /// <summary>The fictional district or neighborhood.</summary>
    public string District = string.Empty;

    /// <summary>The fictional city or locality.</summary>
    public string City = string.Empty;

    /// <summary>The fictional county.</summary>
    public string County = string.Empty;

    /// <summary>The fictional subdivision associated with the selected country.</summary>
    public string Subdivision = string.Empty;

    /// <summary>The subdivision abbreviation.</summary>
    public string SubdivisionAbbreviation = string.Empty;

    /// <summary>The subdivision-code-shaped value associated with the selected country.</summary>
    public string SubdivisionCode = string.Empty;

    /// <summary>The postal-code-shaped value using the selected country's declared format.</summary>
    public string PostalCode = string.Empty;

    /// <summary>The fictional country display name.</summary>
    public string Country = string.Empty;

    /// <summary>The fictional country abbreviation.</summary>
    public string CountryAbbreviation = string.Empty;

    /// <summary>The alpha-2 country-code-shaped value.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The bundled IANA time-zone identifier associated with the selected country.</summary>
    public string TimeZoneId = string.Empty;

    /// <summary>The geographic record sharing this address's country, subdivision, postal code, and time zone.</summary>
    public GeoRecord Geography = new GeoRecord();

    internal AddressRecord()
    {
    }
}
