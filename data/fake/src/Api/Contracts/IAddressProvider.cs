using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="AddressProvider"/>.</summary>
public interface IAddressProvider
{
    /// <summary>Generates a locale-provided building number.</summary>
    /// <returns>A generated building-number component.</returns>
    string BuildingNumber();
    /// <summary>Generates a locale-provided street-name component without a street type.</summary>
    /// <returns>A street-name component.</returns>
    string StreetName();
    /// <summary>Generates a street name composed with its full street type.</summary>
    /// <returns>A composed street name.</returns>
    string Street();
    /// <summary>Generates a full street-type name.</summary>
    /// <returns>A street-type name.</returns>
    string StreetType();
    /// <summary>Generates a street-type abbreviation.</summary>
    /// <returns>A street-type abbreviation.</returns>
    string StreetTypeAbbreviation();
    /// <summary>Generates a secondary unit composed from a unit type and identifier.</summary>
    /// <returns>A composed unit component.</returns>
    string Unit();
    /// <summary>Generates a fictional post-office-box component.</summary>
    /// <returns>A post-office-box component.</returns>
    string PoBox();
    /// <summary>Generates a fictional district or neighborhood name.</summary>
    /// <returns>A district name.</returns>
    string District();
    /// <summary>Generates a fictional city or locality name.</summary>
    /// <returns>A city name.</returns>
    string City();
    /// <summary>Generates a fictional county name.</summary>
    /// <returns>A county name.</returns>
    string County();
    /// <summary>Generates a fictional subdivision name.</summary>
    /// <returns>A subdivision name.</returns>
    string Subdivision();
    /// <summary>Generates a fictional subdivision abbreviation.</summary>
    /// <returns>A subdivision abbreviation.</returns>
    string SubdivisionAbbreviation();
    /// <summary>Generates a subdivision-code-shaped value from locale data.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A subdivision-code-shaped value.</returns>
    string SubdivisionCode();
    /// <summary>Generates a synthetic postal-code-shaped value.</summary>
    /// <remarks>The result is not claimed to identify a deliverable address.</remarks>
    /// <returns>A postal-code-shaped value.</returns>
    string PostalCode();
    /// <summary>Generates a fictional country display name.</summary>
    /// <returns>A country display name.</returns>
    string Country();
    /// <summary>Generates a fictional country abbreviation.</summary>
    /// <returns>A country abbreviation.</returns>
    string CountryAbbreviation();
    /// <summary>Generates a country-code-shaped value from locale data.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A country-code-shaped value.</returns>
    string CountryCode();
    /// <summary>Generates an alpha-3 country-code-shaped value.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A three-letter country-code-shaped value.</returns>
    string CountryAlpha3Code();
    /// <summary>Generates a three-digit country-code-shaped value.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A three-digit country-code-shaped value.</returns>
    string CountryNumericCode();
    /// <summary>Generates a fictional language display name associated with a country fixture.</summary>
    /// <returns>A language display name.</returns>
    string Language();
    /// <summary>Generates a language-code-shaped value associated with a country fixture.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A lowercase language-code-shaped value.</returns>
    string LanguageCode();
    /// <summary>Generates a fictional currency display name associated with a country fixture.</summary>
    /// <returns>A currency display name.</returns>
    string Currency();
    /// <summary>Generates a currency-code-shaped value associated with a country fixture.</summary>
    /// <remarks>The result is fictional and is not claimed to be an assigned code.</remarks>
    /// <returns>A three-letter currency-code-shaped value.</returns>
    string CurrencyCode();
    /// <summary>Generates a calling-code-shaped value associated with a country fixture.</summary>
    /// <remarks>The result is synthetic and is not claimed to route to a country or subscriber.</remarks>
    /// <returns>A calling-code-shaped value.</returns>
    string CallingCode();
    /// <summary>Generates a bundled IANA time-zone identifier associated with a country fixture.</summary>
    /// <returns>An IANA time-zone identifier.</returns>
    string TimeZoneId();
    /// <summary>Generates one correlated fictional country metadata value.</summary>
    /// <returns>A correlated country details value.</returns>
    CountryDetails CountryDetails();
    /// <summary>Generates a correlated synthetic postal address record.</summary>
    /// <remarks>
    /// The country, subdivision, postal format, time zone, and coordinate bounds come from one
    /// catalog entry. Reserved mode guarantees a project-fictional, non-deliverable complete
    /// address. Plausible and unrestricted modes make no nonexistence or non-deliverability guarantee.
    /// </remarks>
    /// <returns>A correlated address and geographic record.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or inconsistent.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds a configured limit.</exception>
    AddressRecord Record();
    /// <summary>Generates correlated synthetic locality and geographic metadata.</summary>
    /// <remarks>
    /// The coordinate falls inside the broad bounds declared for the selected country fixture.
    /// The returned record states whether its selected mode carries nonexistence and non-deliverability guarantees.
    /// </remarks>
    /// <returns>A correlated geographic record.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or inconsistent.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds a configured limit.</exception>
    GeoRecord GeoRecord();
    /// <summary>Generates a latitude across the complete valid range.</summary>
    /// <returns>A latitude in decimal degrees from negative 90 through positive 90.</returns>
    double Latitude();
    /// <summary>Generates a latitude within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive minimum latitude in decimal degrees.</param>
    /// <param name="maximum">The inclusive maximum latitude in decimal degrees.</param>
    /// <returns>A latitude within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, outside the latitude range, or reversed.</exception>
    double LatitudeFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>Generates a longitude across the complete valid range.</summary>
    /// <returns>A longitude in decimal degrees from negative 180 through positive 180.</returns>
    double Longitude();
    /// <summary>Generates a longitude within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive minimum longitude in decimal degrees.</param>
    /// <param name="maximum">The inclusive maximum longitude in decimal degrees.</param>
    /// <returns>A longitude within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, outside the longitude range, or reversed.</exception>
    double LongitudeFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>Generates a coordinate across the complete latitude and longitude ranges.</summary>
    /// <returns>A generated geographic coordinate.</returns>
    GeoCoordinate Coordinate();
    /// <summary>Generates a coordinate within inclusive latitude and longitude bounds.</summary>
    /// <param name="minimumLatitude">The inclusive minimum latitude.</param>
    /// <param name="maximumLatitude">The inclusive maximum latitude.</param>
    /// <param name="minimumLongitude">The inclusive minimum longitude.</param>
    /// <param name="maximumLongitude">The inclusive maximum longitude.</param>
    /// <returns>A generated coordinate within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, invalid, or reversed.</exception>
    GeoCoordinate CoordinateFromMinimumLatitudeAndMaximumLatitudeAndMinimumLongitudeAndMaximumLongitude(double minimumLatitude, double maximumLatitude, double minimumLongitude, double maximumLongitude);
    /// <summary>Generates a nonnegative altitude in meters up to the default terrestrial maximum.</summary>
    /// <returns>An altitude from zero through 8848 meters.</returns>
    double Altitude();
    /// <summary>Generates a nonnegative altitude within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive nonnegative minimum altitude in meters.</param>
    /// <param name="maximum">The inclusive nonnegative maximum altitude in meters.</param>
    /// <returns>An altitude within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, negative, or reversed.</exception>
    double AltitudeFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>Generates a negative depth in meters down to the default terrestrial maximum.</summary>
    /// <returns>A depth from negative 10994 meters inclusive to zero exclusive.</returns>
    double Depth();
    /// <summary>Generates a negative depth within a requested interval.</summary>
    /// <param name="minimum">The inclusive negative minimum depth in meters.</param>
    /// <param name="maximum">The exclusive maximum depth, which must be zero or less.</param>
    /// <returns>A negative depth within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite, invalid, or the interval is empty or reversed.</exception>
    double DepthFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>Generates a full cardinal or ordinal compass direction.</summary>
    /// <returns>One of the eight full compass direction names.</returns>
    string Direction();
    /// <summary>Generates a cardinal or ordinal compass direction.</summary>
    /// <param name="useAbbreviation">Whether to return the one-letter or two-letter abbreviation.</param>
    /// <returns>One of the eight compass directions in the requested form.</returns>
    string DirectionFromUseAbbreviation(bool useAbbreviation);
    /// <summary>Generates a full cardinal compass direction.</summary>
    /// <returns>North, east, south, or west.</returns>
    string CardinalDirection();
    /// <summary>Generates a cardinal compass direction.</summary>
    /// <param name="useAbbreviation">Whether to return the one-letter abbreviation.</param>
    /// <returns>A cardinal direction in the requested form.</returns>
    string CardinalDirectionFromUseAbbreviation(bool useAbbreviation);
    /// <summary>Generates a full ordinal compass direction.</summary>
    /// <returns>Northeast, southeast, southwest, or northwest.</returns>
    string OrdinalDirection();
    /// <summary>Generates an ordinal compass direction.</summary>
    /// <param name="useAbbreviation">Whether to return the two-letter abbreviation.</param>
    /// <returns>An ordinal direction in the requested form.</returns>
    string OrdinalDirectionFromUseAbbreviation(bool useAbbreviation);
    /// <summary>Generates a compass bearing in degrees.</summary>
    /// <returns>A bearing from zero inclusive to 360 exclusive.</returns>
    double Bearing();
    /// <summary>Generates a point in an inclusive geographic bounding box.</summary>
    /// <remarks>A west longitude greater than the east longitude describes a box that crosses the antimeridian.</remarks>
    /// <param name="southLatitude">The inclusive southern latitude.</param>
    /// <param name="westLongitude">The inclusive western longitude.</param>
    /// <param name="northLatitude">The inclusive northern latitude.</param>
    /// <param name="eastLongitude">The inclusive eastern longitude.</param>
    /// <returns>A coordinate within the bounding box.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or invalid, or the latitude bounds are reversed.</exception>
    GeoCoordinate BoundingBoxPoint(double southLatitude, double westLongitude, double northLatitude, double eastLongitude);
    /// <summary>Generates a point no farther than a radius from an origin on a spherical earth.</summary>
    /// <remarks>Points are sampled uniformly by surface area within the spherical cap.</remarks>
    /// <param name="latitude">The origin latitude in decimal degrees.</param>
    /// <param name="longitude">The origin longitude in decimal degrees.</param>
    /// <param name="radiusMeters">The inclusive radius in meters, no greater than half the earth circumference.</param>
    /// <returns>A coordinate within the requested radius.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument is non-finite or outside its valid range.</exception>
    GeoCoordinate NearbyPoint(double latitude, double longitude, double radiusMeters);
    /// <summary>Generates a standard-base32 geohash-shaped value with 12 characters.</summary>
    /// <returns>A geohash-shaped value encoding a generated coordinate.</returns>
    string Geohash();
    /// <summary>Generates a standard-base32 geohash-shaped value with requested precision.</summary>
    /// <param name="precision">The number of characters from 1 through 12.</param>
    /// <returns>A geohash-shaped value encoding a generated coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when precision is invalid or exceeds the configured output limit.</exception>
    string GeohashFromPrecision(int precision);
    /// <summary>Encodes a coordinate as a standard-base32 geohash-shaped value.</summary>
    /// <param name="latitude">The latitude in decimal degrees.</param>
    /// <param name="longitude">The longitude in decimal degrees.</param>
    /// <param name="precision">The number of characters from 1 through 12.</param>
    /// <returns>The geohash-shaped encoding of the supplied coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument is non-finite or invalid, or precision exceeds the configured output limit.</exception>
    string GeohashFromLatitudeAndLongitudeAndPrecision(double latitude, double longitude, int precision);
    /// <summary>Generates a complete multi-line address using a weighted locale format style.</summary>
    /// <remarks>Lines are separated by a line-feed character. Optional empty components are omitted with their complete line.</remarks>
    /// <returns>A locale-ordered multi-line fictional address.</returns>
    string FullAddress();
    /// <summary>Generates a complete multi-line address using a requested locale format style.</summary>
    /// <param name="style">A style identifier declared by the resolved address locale data, such as <c>full</c> or <c>abbreviated</c>.</param>
    /// <returns>A locale-ordered multi-line fictional address.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>style</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>style</c> is empty, whitespace, or unavailable.</exception>
    string FullAddressFromStyle(string style);
    /// <summary>Generates a complete single-line address using a weighted locale format style.</summary>
    /// <remarks>Optional empty components and their punctuation segments are omitted.</remarks>
    /// <returns>A locale-ordered single-line fictional address.</returns>
    string SingleLineAddress();
    /// <summary>Generates a complete single-line address using a requested locale format style.</summary>
    /// <param name="style">A style identifier declared by the resolved address locale data, such as <c>full</c> or <c>abbreviated</c>.</param>
    /// <returns>A locale-ordered single-line fictional address.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>style</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>style</c> is empty, whitespace, or unavailable.</exception>
    string SingleLineAddressFromStyle(string style);
    /// <summary>Generates a multi-line address using native locale text and the <c>full</c> style.</summary>
    /// <returns>A native-form fictional address.</returns>
    string NativeAddress();
    /// <summary>Generates a multi-line address using resolved localized text and a weighted locale style.</summary>
    /// <returns>A localized fictional address.</returns>
    string LocalizedAddress();
}
