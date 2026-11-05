using fake;

namespace fake.Tests;

[TestFixture]
public class GeographyProviderTests
{
    [Test]
    public void GeographicValuesAreDeterministicAndWithinPhysicalRanges()
    {
        AddressProvider first = Fake.Create(7501).Address();
        AddressProvider second = Fake.Create(7501).Address();

        int index = 0;
        while (index < 256)
        {
            double latitude = first.Latitude();
            double longitude = first.Longitude();
            GeoCoordinate coordinate = first.Coordinate();
            double altitude = first.Altitude();
            double depth = first.Depth();
            double bearing = first.Bearing();

            Assert.Multiple(() =>
            {
                Assert.That(latitude, Is.EqualTo(second.Latitude()));
                Assert.That(longitude, Is.EqualTo(second.Longitude()));
                GeoCoordinate otherCoordinate = second.Coordinate();
                Assert.That(coordinate.Latitude, Is.EqualTo(otherCoordinate.Latitude));
                Assert.That(coordinate.Longitude, Is.EqualTo(otherCoordinate.Longitude));
                Assert.That(altitude, Is.EqualTo(second.Altitude()));
                Assert.That(depth, Is.EqualTo(second.Depth()));
                Assert.That(bearing, Is.EqualTo(second.Bearing()));
                Assert.That(latitude, Is.InRange(-90.0, 90.0));
                Assert.That(longitude, Is.InRange(-180.0, 180.0));
                Assert.That(coordinate.Latitude, Is.InRange(-90.0, 90.0));
                Assert.That(coordinate.Longitude, Is.InRange(-180.0, 180.0));
                Assert.That(altitude, Is.InRange(0.0, 8848.0));
                Assert.That(depth, Is.GreaterThanOrEqualTo(-10994.0));
                Assert.That(depth, Is.LessThan(0.0));
                Assert.That(bearing, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(bearing, Is.LessThan(360.0));
            });
            index++;
        }
    }

    [Test]
    public void ExplicitRangesIncludeDegenerateCoordinatesAndRequestedElevations()
    {
        AddressProvider address = Fake.Create(7502).Address();

        int index = 0;
        while (index < 128)
        {
            GeoCoordinate coordinate = address.CoordinateFromMinimumLatitudeAndMaximumLatitudeAndMinimumLongitudeAndMaximumLongitude(10.0, 20.0, -40.0, -30.0);
            Assert.Multiple(() =>
            {
                Assert.That(address.LatitudeFromMinimumAndMaximum(12.5, 12.5), Is.EqualTo(12.5));
                Assert.That(address.LongitudeFromMinimumAndMaximum(-45.25, -45.25), Is.EqualTo(-45.25));
                Assert.That(coordinate.Latitude, Is.InRange(10.0, 20.0));
                Assert.That(coordinate.Longitude, Is.InRange(-40.0, -30.0));
                Assert.That(address.AltitudeFromMinimumAndMaximum(100.0, 200.0), Is.InRange(100.0, 200.0));
                Assert.That(address.DepthFromMinimumAndMaximum(-200.0, -100.0), Is.InRange(-200.0, -100.0));
            });
            index++;
        }
    }

    [Test]
    public void DirectionApisReturnOnlyTheirDeclaredCompassSets()
    {
        AddressProvider address = Fake.Create(7503).Address();
        string[] directions =
        [
            "North", "Northeast", "East", "Southeast",
            "South", "Southwest", "West", "Northwest"
        ];
        string[] abbreviations = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        string[] cardinals = ["North", "East", "South", "West"];
        string[] cardinalAbbreviations = ["N", "E", "S", "W"];
        string[] ordinals = ["Northeast", "Southeast", "Southwest", "Northwest"];
        string[] ordinalAbbreviations = ["NE", "SE", "SW", "NW"];

        int index = 0;
        while (index < 256)
        {
            Assert.Multiple(() =>
            {
                Assert.That(address.Direction(), Is.AnyOf(directions));
                Assert.That(address.DirectionFromUseAbbreviation(true), Is.AnyOf(abbreviations));
                Assert.That(address.CardinalDirection(), Is.AnyOf(cardinals));
                Assert.That(address.CardinalDirectionFromUseAbbreviation(true), Is.AnyOf(cardinalAbbreviations));
                Assert.That(address.OrdinalDirection(), Is.AnyOf(ordinals));
                Assert.That(address.OrdinalDirectionFromUseAbbreviation(true), Is.AnyOf(ordinalAbbreviations));
            });
            index++;
        }
    }

    [Test]
    public void BoundingBoxPointsSupportOrdinaryAndAntimeridianBoxes()
    {
        AddressProvider address = Fake.Create(7504).Address();

        int index = 0;
        while (index < 256)
        {
            GeoCoordinate ordinary = address.BoundingBoxPoint(30.0, -20.0, 40.0, 10.0);
            GeoCoordinate crossing = address.BoundingBoxPoint(-10.0, 170.0, 10.0, -170.0);
            Assert.Multiple(() =>
            {
                Assert.That(ordinary.Latitude, Is.InRange(30.0, 40.0));
                Assert.That(ordinary.Longitude, Is.InRange(-20.0, 10.0));
                Assert.That(crossing.Latitude, Is.InRange(-10.0, 10.0));
                Assert.That(
                    crossing.Longitude >= 170.0 || crossing.Longitude <= -170.0,
                    Is.True);
            });
            index++;
        }
    }

    [Test]
    public void NearbyPointsStayWithinTheRequestedGreatCircleRadius()
    {
        AddressProvider address = Fake.Create(7505).Address();
        GeoCoordinate unchanged = address.NearbyPoint(12.25, -179.75, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Latitude, Is.EqualTo(12.25));
            Assert.That(unchanged.Longitude, Is.EqualTo(-179.75));
        });

        int index = 0;
        while (index < 512)
        {
            GeoCoordinate point = address.NearbyPoint(65.0, 179.9, 1000000.0);
            double distance = DistanceMeters(65.0, 179.9, point.Latitude, point.Longitude);
            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.InRange(-90.0, 90.0));
                Assert.That(point.Longitude, Is.InRange(-180.0, 180.0));
                Assert.That(distance, Is.LessThanOrEqualTo(1000000.001));
            });
            index++;
        }
    }

    [Test]
    public void GeohashUsesKnownEncodingVectorsAndConfiguredPrecision()
    {
        AddressProvider address = Fake.Create(7506).Address();
        AddressProvider matching = Fake.Create(7506).Address();

        Assert.Multiple(() =>
        {
            Assert.That(address.GeohashFromLatitudeAndLongitudeAndPrecision(42.6, -5.6, 5), Is.EqualTo("ezs42"));
            Assert.That(address.GeohashFromLatitudeAndLongitudeAndPrecision(57.64911, 10.40744, 11), Is.EqualTo("u4pruydqqvj"));
            Assert.That(address.GeohashFromPrecision(1), Does.Match("^[0123456789bcdefghjkmnpqrstuvwxyz]$"));
        });

        string generated = address.Geohash();
        matching.GeohashFromPrecision(1);
        Assert.That(generated, Is.EqualTo(matching.Geohash()));
        Assert.That(generated, Does.Match("^[0123456789bcdefghjkmnpqrstuvwxyz]{12}$"));
    }

    [Test]
    public void InvalidGeographicArgumentsFailWithTheirPublicParameterNames()
    {
        AddressProvider address = Fake.Create(7507).Address();

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.LatitudeFromMinimumAndMaximum(-91.0, 0.0)).ParamName,
                Is.EqualTo("minimum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.LongitudeFromMinimumAndMaximum(0.0, 181.0)).ParamName,
                Is.EqualTo("maximum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.CoordinateFromMinimumLatitudeAndMaximumLatitudeAndMinimumLongitudeAndMaximumLongitude(20.0, 10.0, 0.0, 1.0)).ParamName,
                Is.EqualTo("maximumLatitude"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.AltitudeFromMinimumAndMaximum(-1.0, 10.0)).ParamName,
                Is.EqualTo("minimum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.DepthFromMinimumAndMaximum(-10.0, 1.0)).ParamName,
                Is.EqualTo("maximum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.BoundingBoxPoint(-10.0, 0.0, -20.0, 1.0)).ParamName,
                Is.EqualTo("northLatitude"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.NearbyPoint(0.0, 0.0, -1.0)).ParamName,
                Is.EqualTo("radiusMeters"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.GeohashFromLatitudeAndLongitudeAndPrecision(double.NaN, 0.0, 5)).ParamName,
                Is.EqualTo("latitude"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => address.GeohashFromPrecision(0)).ParamName,
                Is.EqualTo("precision"));
        });

        FakeOptions options = FakeOptions.Create(7508);
        options.MaximumOutputSize = 4;
        AddressProvider limited = Fake.Create(options).Address();
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => limited.GeohashFromPrecision(5)).ParamName,
            Is.EqualTo("precision"));
    }

    static double DistanceMeters(
        double firstLatitude,
        double firstLongitude,
        double secondLatitude,
        double secondLongitude)
    {
        double latitudeDelta = Radians(secondLatitude - firstLatitude);
        double longitudeDelta = Radians(secondLongitude - firstLongitude);
        double firstRadians = Radians(firstLatitude);
        double secondRadians = Radians(secondLatitude);
        double sineLatitude = Math.Sin(latitudeDelta / 2.0);
        double sineLongitude = Math.Sin(longitudeDelta / 2.0);
        double value =
            (sineLatitude * sineLatitude) +
            (Math.Cos(firstRadians) * Math.Cos(secondRadians) * sineLongitude * sineLongitude);
        double angle = 2.0 * Math.Atan2(Math.Sqrt(value), Math.Sqrt(1.0 - value));
        return 6371008.8 * angle;
    }

    static double Radians(double value)
    {
        return value * Math.PI / 180.0;
    }
}
