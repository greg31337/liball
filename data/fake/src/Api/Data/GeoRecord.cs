namespace fake;

/// <summary>
/// Represents correlated geographic metadata for a synthetic locality.
/// </summary>
/// <remarks>
/// Reserved mode uses the project's explicitly fictional country fixture namespace.
/// Plausible and unrestricted modes may resemble real localities, so they do not
/// guarantee nonexistence and never assert allocation or geographic identity.
/// </remarks>
public class GeoRecord
{
    /// <summary>The safety mode captured for this record.</summary>
    public SafetyMode SafetyMode;

    /// <summary>Whether the selected mode guarantees that the record is not deliverable.</summary>
    public bool NonDeliverabilityGuaranteed;

    /// <summary>Whether the selected mode guarantees that the complete locality does not exist.</summary>
    public bool NonexistenceGuaranteed;

    /// <summary>The fictional country display name.</summary>
    public string Country = string.Empty;

    /// <summary>The alpha-2 country-code-shaped value.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The fictional subdivision display name.</summary>
    public string Subdivision = string.Empty;

    /// <summary>The subdivision-code-shaped value associated with the country.</summary>
    public string SubdivisionCode = string.Empty;

    /// <summary>The synthetic postal-code-shaped value using the country's declared format.</summary>
    public string PostalCode = string.Empty;

    /// <summary>The bundled IANA time-zone identifier associated with the country fixture.</summary>
    public string TimeZoneId = string.Empty;

    /// <summary>A coordinate inside the country's declared broad bounds.</summary>
    public GeoCoordinate Coordinate = new GeoCoordinate(0.0, 0.0);

    internal GeoRecord()
    {
    }
}
