using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides address and geography generation operations.
/// </summary>
/// <remarks>
/// Reserved safety mode selects project-defined example fixtures. Synthetic-plausible and
/// unrestricted modes may accidentally resemble real places. Component and formatted-address
/// APIs never validate existence or deliverability and must not be treated as postal records.
/// Correlated records expose their explicit guarantee metadata.
/// </remarks>
public class AddressProvider : IAddressProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal AddressProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("address");
    }

    /// <summary>Generates a locale-provided building number.</summary>
    /// <returns>A generated building-number component.</returns>
    public string BuildingNumber()
    {
        return AddressGenerator.BuildingNumber(Context, RandomValue);
    }

    /// <summary>Generates a locale-provided street-name component without a street type.</summary>
    /// <returns>A street-name component.</returns>
    public string StreetName()
    {
        return AddressGenerator.StreetName(Context, RandomValue);
    }

    /// <summary>Generates a street name composed with its full street type.</summary>
    /// <returns>A composed street name.</returns>
    public string Street()
    {
        return AddressGenerator.Street(Context, RandomValue);
    }

    /// <summary>Generates a full street-type name.</summary>
    /// <returns>A street-type name.</returns>
    public string StreetType()
    {
        return AddressGenerator.StreetType(Context, RandomValue);
    }

    /// <summary>Generates a street-type abbreviation.</summary>
    /// <returns>A street-type abbreviation.</returns>
    public string StreetTypeAbbreviation()
    {
        return AddressGenerator.StreetTypeAbbreviation(Context, RandomValue);
    }

    /// <summary>Generates a secondary unit composed from a unit type and identifier.</summary>
    /// <returns>A composed unit component.</returns>
    public string Unit()
    {
        return AddressGenerator.Unit(Context, RandomValue);
    }

    /// <summary>Generates a fictional post-office-box component.</summary>
    /// <returns>A post-office-box component.</returns>
    public string PoBox()
    {
        return AddressGenerator.PoBox(Context, RandomValue);
    }

    /// <summary>Generates a fictional district or neighborhood name.</summary>
    /// <returns>A district name.</returns>
    public string District()
    {
        return AddressGenerator.District(Context, RandomValue);
    }

    /// <summary>Generates a fictional city or locality name.</summary>
    /// <returns>A city name.</returns>
    public string City()
    {
        return AddressGenerator.City(Context, RandomValue);
    }

    /// <summary>Generates a fictional county name.</summary>
    /// <returns>A county name.</returns>
    public string County()
    {
        return AddressGenerator.County(Context, RandomValue);
    }

    /// <summary>Generates a fictional subdivision name.</summary>
    /// <returns>A subdivision name.</returns>
    public string Subdivision()
    {
        return AddressGenerator.Subdivision(Context, RandomValue);
    }

    /// <summary>Generates a fictional subdivision abbreviation.</summary>
    /// <returns>A subdivision abbreviation.</returns>
    public string SubdivisionAbbreviation()
    {
        return AddressGenerator.SubdivisionAbbreviation(Context, RandomValue);
    }

    /// <summary>Generates a subdivision-code-shaped value from locale data.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A subdivision-code-shaped value.</returns>
    public string SubdivisionCode()
    {
        return AddressGenerator.SubdivisionCode(Context, RandomValue);
    }

    /// <summary>Generates a synthetic postal-code-shaped value.</summary>
    /// <remarks>The result is not claimed to identify a deliverable address.</remarks>
    /// <returns>A postal-code-shaped value.</returns>
    public string PostalCode()
    {
        return AddressGenerator.PostalCode(Context, RandomValue);
    }

    /// <summary>Generates a fictional country display name.</summary>
    /// <returns>A country display name.</returns>
    public string Country()
    {
        return AddressGenerator.Country(Context, RandomValue);
    }

    /// <summary>Generates a fictional country abbreviation.</summary>
    /// <returns>A country abbreviation.</returns>
    public string CountryAbbreviation()
    {
        return AddressGenerator.CountryAbbreviation(Context, RandomValue);
    }

    /// <summary>Generates a country-code-shaped value from locale data.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A country-code-shaped value.</returns>
    public string CountryCode()
    {
        return AddressGenerator.CountryCode(Context, RandomValue);
    }

    /// <summary>Generates an alpha-3 country-code-shaped value.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A three-letter country-code-shaped value.</returns>
    public string CountryAlpha3Code()
    {
        return AddressGenerator.CountryAlpha3Code(Context, RandomValue);
    }

    /// <summary>Generates a three-digit country-code-shaped value.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A three-digit country-code-shaped value.</returns>
    public string CountryNumericCode()
    {
        return AddressGenerator.CountryNumericCode(Context, RandomValue);
    }

    /// <summary>Generates a fictional language display name associated with a country fixture.</summary>
    /// <returns>A language display name.</returns>
    public string Language()
    {
        return AddressGenerator.Language(Context, RandomValue);
    }

    /// <summary>Generates a language-code-shaped value associated with a country fixture.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A lowercase language-code-shaped value.</returns>
    public string LanguageCode()
    {
        return AddressGenerator.LanguageCode(Context, RandomValue);
    }

    /// <summary>Generates a fictional currency display name associated with a country fixture.</summary>
    /// <returns>A currency display name.</returns>
    public string Currency()
    {
        return AddressGenerator.Currency(Context, RandomValue);
    }

    /// <summary>Generates a currency-code-shaped value associated with a country fixture.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A three-letter currency-code-shaped value.</returns>
    public string CurrencyCode()
    {
        return AddressGenerator.CurrencyCode(Context, RandomValue);
    }

    /// <summary>Generates a calling-code-shaped value associated with a country fixture.</summary>
    /// <remarks>The result is synthetic and is not claimed to route to a country or subscriber.</remarks>
    /// <returns>A calling-code-shaped value.</returns>
    public string CallingCode()
    {
        return AddressGenerator.CallingCode(Context, RandomValue);
    }

    /// <summary>Generates a bundled IANA time-zone identifier associated with a country fixture.</summary>
    /// <returns>An IANA time-zone identifier.</returns>
    public string TimeZoneId()
    {
        return AddressGenerator.TimeZoneId(Context, RandomValue);
    }

    /// <summary>Generates one correlated fictional country metadata value.</summary>
    /// <returns>A correlated country details value.</returns>
    public CountryDetails CountryDetails()
    {
        return AddressGenerator.CountryDetails(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic postal address record.</summary>
    /// <remarks>
    /// The country, subdivision, postal format, time zone, and coordinate bounds come from one
    /// catalog entry. Reserved mode guarantees a project-fictional, non-deliverable complete
    /// address. Plausible and unrestricted modes make no nonexistence or non-deliverability guarantee.
    /// </remarks>
    /// <returns>A correlated address and geographic record.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or inconsistent.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds a configured limit.</exception>
    public AddressRecord Record()
    {
        return AddressRecordGenerator.AddressRecord(Context, RandomValue);
    }

    /// <summary>Generates correlated synthetic locality and geographic metadata.</summary>
    /// <remarks>
    /// The coordinate falls inside the broad bounds declared for the selected country fixture.
    /// The returned record states whether its selected mode carries nonexistence and non-deliverability guarantees.
    /// </remarks>
    /// <returns>A correlated geographic record.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or inconsistent.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds a configured limit.</exception>
    public GeoRecord GeoRecord()
    {
        return AddressRecordGenerator.GeoRecord(Context, RandomValue);
    }

    /// <summary>Generates a latitude across the complete valid range.</summary>
    /// <returns>A latitude in decimal degrees from negative 90 through positive 90.</returns>
    public double Latitude()
    {
        return GeographyGenerator.Latitude(RandomValue);
    }

    /// <summary>Generates a latitude within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive minimum latitude in decimal degrees.</param>
    /// <param name="maximum">The inclusive maximum latitude in decimal degrees.</param>
    /// <returns>A latitude within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, outside the latitude range, or reversed.</exception>
    public double LatitudeFromMinimumAndMaximum(double minimum, double maximum)
    {
        return GeographyGenerator.Latitude(RandomValue, minimum, maximum);
    }

    /// <summary>Generates a longitude across the complete valid range.</summary>
    /// <returns>A longitude in decimal degrees from negative 180 through positive 180.</returns>
    public double Longitude()
    {
        return GeographyGenerator.Longitude(RandomValue);
    }

    /// <summary>Generates a longitude within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive minimum longitude in decimal degrees.</param>
    /// <param name="maximum">The inclusive maximum longitude in decimal degrees.</param>
    /// <returns>A longitude within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, outside the longitude range, or reversed.</exception>
    public double LongitudeFromMinimumAndMaximum(double minimum, double maximum)
    {
        return GeographyGenerator.Longitude(RandomValue, minimum, maximum);
    }

    /// <summary>Generates a coordinate across the complete latitude and longitude ranges.</summary>
    /// <returns>A generated geographic coordinate.</returns>
    public GeoCoordinate Coordinate()
    {
        return GeographyGenerator.Coordinate(RandomValue);
    }

    /// <summary>Generates a coordinate within inclusive latitude and longitude bounds.</summary>
    /// <param name="minimumLatitude">The inclusive minimum latitude.</param>
    /// <param name="maximumLatitude">The inclusive maximum latitude.</param>
    /// <param name="minimumLongitude">The inclusive minimum longitude.</param>
    /// <param name="maximumLongitude">The inclusive maximum longitude.</param>
    /// <returns>A generated coordinate within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, invalid, or reversed.</exception>
    public GeoCoordinate CoordinateFromMinimumLatitudeAndMaximumLatitudeAndMinimumLongitudeAndMaximumLongitude(double minimumLatitude, double maximumLatitude, double minimumLongitude, double maximumLongitude)
    {
        return GeographyGenerator.Coordinate(RandomValue, minimumLatitude, maximumLatitude, minimumLongitude, maximumLongitude);
    }

    /// <summary>Generates a nonnegative altitude in meters up to the default terrestrial maximum.</summary>
    /// <returns>An altitude from zero through 8848 meters.</returns>
    public double Altitude()
    {
        return GeographyGenerator.Altitude(RandomValue);
    }

    /// <summary>Generates a nonnegative altitude within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive nonnegative minimum altitude in meters.</param>
    /// <param name="maximum">The inclusive nonnegative maximum altitude in meters.</param>
    /// <returns>An altitude within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, negative, or reversed.</exception>
    public double AltitudeFromMinimumAndMaximum(double minimum, double maximum)
    {
        return GeographyGenerator.Altitude(RandomValue, minimum, maximum);
    }

    /// <summary>Generates a negative depth in meters down to the default terrestrial maximum.</summary>
    /// <returns>A depth from negative 10994 meters inclusive to zero exclusive.</returns>
    public double Depth()
    {
        return GeographyGenerator.Depth(RandomValue);
    }

    /// <summary>Generates a negative depth within a requested interval.</summary>
    /// <param name="minimum">The inclusive negative minimum depth in meters.</param>
    /// <param name="maximum">The exclusive maximum depth, which must be zero or less.</param>
    /// <returns>A negative depth within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, invalid, or the interval is empty or reversed.</exception>
    public double DepthFromMinimumAndMaximum(double minimum, double maximum)
    {
        return GeographyGenerator.Depth(RandomValue, minimum, maximum);
    }

    /// <summary>Generates a full cardinal or ordinal compass direction.</summary>
    /// <returns>One of the eight full compass direction names.</returns>
    public string Direction()
    {
        return GeographyGenerator.Direction(RandomValue, false);
    }

    /// <summary>Generates a cardinal or ordinal compass direction.</summary>
    /// <param name="useAbbreviation">Whether to return the one-letter or two-letter abbreviation.</param>
    /// <returns>One of the eight compass directions in the requested form.</returns>
    public string DirectionFromUseAbbreviation(bool useAbbreviation)
    {
        return GeographyGenerator.Direction(RandomValue, useAbbreviation);
    }

    /// <summary>Generates a full cardinal compass direction.</summary>
    /// <returns>North, east, south, or west.</returns>
    public string CardinalDirection()
    {
        return GeographyGenerator.CardinalDirection(RandomValue, false);
    }

    /// <summary>Generates a cardinal compass direction.</summary>
    /// <param name="useAbbreviation">Whether to return the one-letter abbreviation.</param>
    /// <returns>A cardinal direction in the requested form.</returns>
    public string CardinalDirectionFromUseAbbreviation(bool useAbbreviation)
    {
        return GeographyGenerator.CardinalDirection(RandomValue, useAbbreviation);
    }

    /// <summary>Generates a full ordinal compass direction.</summary>
    /// <returns>Northeast, southeast, southwest, or northwest.</returns>
    public string OrdinalDirection()
    {
        return GeographyGenerator.OrdinalDirection(RandomValue, false);
    }

    /// <summary>Generates an ordinal compass direction.</summary>
    /// <param name="useAbbreviation">Whether to return the two-letter abbreviation.</param>
    /// <returns>An ordinal direction in the requested form.</returns>
    public string OrdinalDirectionFromUseAbbreviation(bool useAbbreviation)
    {
        return GeographyGenerator.OrdinalDirection(RandomValue, useAbbreviation);
    }

    /// <summary>Generates a compass bearing in degrees.</summary>
    /// <returns>A bearing from zero inclusive to 360 exclusive.</returns>
    public double Bearing()
    {
        return GeographyGenerator.Bearing(RandomValue);
    }

    /// <summary>Generates a point in an inclusive geographic bounding box.</summary>
    /// <remarks>A west longitude greater than the east longitude describes a box that crosses the antimeridian.</remarks>
    /// <param name="southLatitude">The inclusive southern latitude.</param>
    /// <param name="westLongitude">The inclusive western longitude.</param>
    /// <param name="northLatitude">The inclusive northern latitude.</param>
    /// <param name="eastLongitude">The inclusive eastern longitude.</param>
    /// <returns>A coordinate within the bounding box.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or invalid, or the latitude bounds are reversed.</exception>
    public GeoCoordinate BoundingBoxPoint(double southLatitude, double westLongitude, double northLatitude, double eastLongitude)
    {
        return GeographyGenerator.BoundingBoxPoint(RandomValue, southLatitude, westLongitude, northLatitude, eastLongitude);
    }

    /// <summary>Generates a point no farther than a radius from an origin on a spherical earth.</summary>
    /// <remarks>Points are sampled uniformly by surface area within the spherical cap.</remarks>
    /// <param name="latitude">The origin latitude in decimal degrees.</param>
    /// <param name="longitude">The origin longitude in decimal degrees.</param>
    /// <param name="radiusMeters">The inclusive radius in meters, no greater than half the earth circumference.</param>
    /// <returns>A coordinate within the requested radius.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument is non-finite or outside its valid range.</exception>
    public GeoCoordinate NearbyPoint(double latitude, double longitude, double radiusMeters)
    {
        return GeographyGenerator.NearbyPoint(RandomValue, latitude, longitude, radiusMeters);
    }

    /// <summary>Generates a standard-base32 geohash-shaped value with 12 characters.</summary>
    /// <returns>A geohash-shaped value encoding a generated coordinate.</returns>
    public string Geohash()
    {
        return GeographyGenerator.Geohash(RandomValue, GeographyGenerator.DefaultGeohashPrecision, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a standard-base32 geohash-shaped value with requested precision.</summary>
    /// <param name="precision">The number of characters from 1 through 12.</param>
    /// <returns>A geohash-shaped value encoding a generated coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when precision is invalid or exceeds the configured output limit.</exception>
    public string GeohashFromPrecision(int precision)
    {
        return GeographyGenerator.Geohash(RandomValue, precision, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Encodes a coordinate as a standard-base32 geohash-shaped value.</summary>
    /// <param name="latitude">The latitude in decimal degrees.</param>
    /// <param name="longitude">The longitude in decimal degrees.</param>
    /// <param name="precision">The number of characters from 1 through 12.</param>
    /// <returns>The geohash-shaped encoding of the supplied coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument is non-finite or invalid, or precision exceeds the configured output limit.</exception>
    public string GeohashFromLatitudeAndLongitudeAndPrecision(double latitude, double longitude, int precision)
    {
        return GeographyGenerator.Geohash(latitude, longitude, precision, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a complete multi-line address using a weighted locale format style.</summary>
    /// <remarks>Lines are separated by a line-feed character. Optional empty components are omitted with their complete line.</remarks>
    /// <returns>A locale-ordered multi-line fictional address.</returns>
    public string FullAddress()
    {
        return AddressFormatter.MultiLine(Context, RandomValue);
    }

    /// <summary>Generates a complete multi-line address using a requested locale format style.</summary>
    /// <param name="style">A style identifier declared by the resolved address locale data, such as <c>full</c> or <c>abbreviated</c>.</param>
    /// <returns>A locale-ordered multi-line fictional address.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>style</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>style</c> is empty, whitespace, or unavailable.</exception>
    public string FullAddressFromStyle(string style)
    {
        Guard.NotNullOrWhiteSpace(style, "style");
        return AddressFormatter.MultiLine(Context, RandomValue, style);
    }

    /// <summary>Generates a complete single-line address using a weighted locale format style.</summary>
    /// <remarks>Optional empty components and their punctuation segments are omitted.</remarks>
    /// <returns>A locale-ordered single-line fictional address.</returns>
    public string SingleLineAddress()
    {
        return AddressFormatter.SingleLine(Context, RandomValue);
    }

    /// <summary>Generates a complete single-line address using a requested locale format style.</summary>
    /// <param name="style">A style identifier declared by the resolved address locale data, such as <c>full</c> or <c>abbreviated</c>.</param>
    /// <returns>A locale-ordered single-line fictional address.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>style</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>style</c> is empty, whitespace, or unavailable.</exception>
    public string SingleLineAddressFromStyle(string style)
    {
        Guard.NotNullOrWhiteSpace(style, "style");
        return AddressFormatter.SingleLine(Context, RandomValue, style);
    }

    /// <summary>Generates a multi-line address using native locale text and the <c>full</c> style.</summary>
    /// <returns>A native-form fictional address.</returns>
    public string NativeAddress()
    {
        return AddressFormatter.MultiLine(Context, RandomValue, "full");
    }

    /// <summary>Generates a multi-line address using resolved localized text and a weighted locale style.</summary>
    /// <returns>A localized fictional address.</returns>
    public string LocalizedAddress()
    {
        return AddressFormatter.MultiLine(Context, RandomValue);
    }
}
