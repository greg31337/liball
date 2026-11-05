namespace countrycodes;

/// <summary>
/// Defines stable lengths, ranges, separators, and schema values used by country codes.
/// </summary>
public static class CountryCodeConstants
{
    /// <summary>
    /// The required length of an ISO 3166-1 alpha-2 code.
    /// </summary>
    public const int Alpha2Length = 2;

    /// <summary>
    /// The required length of an ISO 3166-1 alpha-3 code.
    /// </summary>
    public const int Alpha3Length = 3;

    /// <summary>
    /// The required length of an ISO 3166-1 numeric code string.
    /// </summary>
    public const int Numeric3Length = 3;

    /// <summary>
    /// The required length of an ISO 3166-3 alpha-4 code.
    /// </summary>
    public const int Alpha4Length = 4;

    /// <summary>
    /// The length of the country prefix in an ISO 3166-2 subdivision code.
    /// </summary>
    public const int SubdivisionCountryCodeLength = 2;

    /// <summary>
    /// The minimum length of a canonical ISO 3166-2 subdivision suffix.
    /// </summary>
    public const int SubdivisionMinimumSuffixLength = 1;

    /// <summary>
    /// The maximum length of a canonical ISO 3166-2 subdivision suffix.
    /// </summary>
    public const int SubdivisionMaximumSuffixLength = 3;

    /// <summary>
    /// The canonical separator between a country code and subdivision suffix.
    /// </summary>
    public const char SubdivisionSeparator = '-';

    /// <summary>
    /// The minimum integer accepted by numeric-3 syntax.
    /// </summary>
    public const int MinimumNumericValue = 0;

    /// <summary>
    /// The maximum integer accepted by numeric-3 syntax.
    /// </summary>
    public const int MaximumNumericValue = 999;

    /// <summary>
    /// The first numeric code reserved for user assignment.
    /// </summary>
    public const int UserAssignedNumericMinimum = 900;

    /// <summary>
    /// The last numeric code reserved for user assignment.
    /// </summary>
    public const int UserAssignedNumericMaximum = 999;

    /// <summary>
    /// The public schema version of the compiled dataset.
    /// </summary>
    public const string SchemaVersion = GeneratedDataMetadata.SchemaVersion;
}
