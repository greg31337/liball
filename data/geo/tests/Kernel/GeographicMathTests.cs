using geo;

namespace geo.Tests;

public class GeographicMathTests
{
    [Test]
    public void UnitConversionsUseOneConsistentEarthRadius()
    {
        double halfCircumference =
            Math.PI * GeographicMath.EarthRadiusMeters;

        Assert.Multiple(() =>
        {
            Assert.That(GeographicMath.ConvertUnits(
                Math.PI, SpatialUnit.Radians, SpatialUnit.Degrees),
                Is.EqualTo(180.0).Within(1.0e-12));
            Assert.That(GeographicMath.ConvertUnits(
                180.0, SpatialUnit.Degrees, SpatialUnit.Meters),
                Is.EqualTo(halfCircumference).Within(1.0e-8));
            Assert.That(GeographicMath.ConvertUnits(
                1000.0, SpatialUnit.Meters, SpatialUnit.Kilometers),
                Is.EqualTo(1.0).Within(1.0e-14));
            Assert.That(GeographicMath.ConvertUnits(
                1.0, SpatialUnit.Miles, SpatialUnit.Feet),
                Is.EqualTo(5280.0).Within(1.0e-10));
            Assert.That(GeographicMath.ConvertUnits(
                1.0, SpatialUnit.NauticalMiles, SpatialUnit.Meters),
                Is.EqualTo(1852.0).Within(1.0e-12));
        });
    }

    [Test]
    public void EveryUnitRoundTripsWithoutChangingMagnitude()
    {
        SpatialUnit[] units =
        [
            SpatialUnit.Radians,
            SpatialUnit.Degrees,
            SpatialUnit.Meters,
            SpatialUnit.Kilometers,
            SpatialUnit.Miles,
            SpatialUnit.NauticalMiles,
            SpatialUnit.Feet
        ];
        const double value = 12.3456789;

        int sourceIndex = 0;
        while (sourceIndex < units.Length)
        {
            int targetIndex = 0;
            while (targetIndex < units.Length)
            {
                double converted = GeographicMath.ConvertUnits(
                    value, units[sourceIndex], units[targetIndex]);
                double roundTrip = GeographicMath.ConvertUnits(
                    converted, units[targetIndex], units[sourceIndex]);
                Assert.That(roundTrip,
                    Is.EqualTo(value).Within(1.0e-11),
                    $"{units[sourceIndex]} through {units[targetIndex]}");
                targetIndex++;
            }
            sourceIndex++;
        }
    }

    [Test]
    public void LongitudeAndLatitudeNormalizationWrapAtGlobalLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeographicMath.NormalizeLongitude(-540.0),
                Is.EqualTo(-180.0));
            Assert.That(GeographicMath.NormalizeLongitude(180.0),
                Is.EqualTo(-180.0));
            Assert.That(GeographicMath.NormalizeLongitude(181.0),
                Is.EqualTo(-179.0));
            Assert.That(GeographicMath.NormalizeLongitude(-181.0),
                Is.EqualTo(179.0));
            Assert.That(GeographicMath.NormalizeLongitude(720.0),
                Is.EqualTo(0.0));
            Assert.That(GeographicMath.NormalizeLatitude(100.0),
                Is.EqualTo(80.0));
            Assert.That(GeographicMath.NormalizeLatitude(-100.0),
                Is.EqualTo(-80.0));
            Assert.That(GeographicMath.NormalizeLatitude(270.0),
                Is.EqualTo(-90.0));
            Assert.That(GeographicMath.NormalizeLatitude(450.0),
                Is.EqualTo(90.0));
            Assert.That(double.IsFinite(
                GeographicMath.NormalizeLongitude(1.0e300)), Is.True);
        });
    }

    [Test]
    public void CoordinateNormalizationReflectsAcrossPolesAndPreservesData()
    {
        Coordinate north = new Coordinate
        {
            X = 10.0,
            Y = 100.0,
            Z = 3.0,
            M = 4.0
        };
        Coordinate south = CoordinateValue(-170.0, -100.0);

        Coordinate normalizedNorth =
            GeographicMath.NormalizeCoordinate(north);
        Coordinate normalizedSouth =
            GeographicMath.NormalizeCoordinate(south);

        Assert.Multiple(() =>
        {
            AssertCoordinate(normalizedNorth, -170.0, 80.0, 1.0e-12);
            Assert.That(normalizedNorth.Z, Is.EqualTo(3.0));
            Assert.That(normalizedNorth.M, Is.EqualTo(4.0));
            AssertCoordinate(normalizedSouth, 10.0, -80.0, 1.0e-12);
            AssertCoordinate(north, 10.0, 100.0, 0.0);
        });
    }

    [Test]
    public void HaversineDistanceUsesShortestAntimeridianArc()
    {
        Coordinate equator = CoordinateValue(0.0, 0.0);
        Coordinate quarter = CoordinateValue(90.0, 0.0);
        Coordinate westDateline = CoordinateValue(179.0, 0.0);
        Coordinate eastDateline = CoordinateValue(-179.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(GeographicMath.GetHaversineDistance(
                equator, equator, SpatialUnit.Meters), Is.EqualTo(0.0));
            Assert.That(GeographicMath.GetHaversineDistance(
                equator, quarter, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetHaversineDistance(
                westDateline, eastDateline, SpatialUnit.Degrees),
                Is.EqualTo(2.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetHaversineDistance(
                CoordinateValue(0.0, 80.0),
                CoordinateValue(120.0, 90.0), SpatialUnit.Degrees),
                Is.EqualTo(10.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetHaversineDistance(
                CoordinateValue(0.0, 90.0),
                CoordinateValue(120.0, 90.0), SpatialUnit.Meters),
                Is.EqualTo(0.0));
            Assert.That(double.IsFinite(
                GeographicMath.GetHaversineDistance(
                    CoordinateValue(1.0e300, 0.0),
                    CoordinateValue(-1.0e300, 0.0),
                    SpatialUnit.Meters)), Is.True);
        });
    }

    [Test]
    public void InitialAndFinalBearingsHandleCardinalAndDatelineRoutes()
    {
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate east = CoordinateValue(90.0, 0.0);
        Coordinate north = CoordinateValue(0.0, 45.0);
        Coordinate westDateline = CoordinateValue(179.0, 0.0);
        Coordinate eastDateline = CoordinateValue(-179.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(GeographicMath.GetInitialBearing(
                origin, east, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetInitialBearing(
                origin, east, SpatialUnit.Radians),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetInitialBearing(
                origin, north, SpatialUnit.Degrees),
                Is.EqualTo(0.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetInitialBearing(
                westDateline, eastDateline, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetFinalBearing(
                westDateline, eastDateline, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(GeographicMath.GetFinalBearing(
                origin, origin, SpatialUnit.Degrees), Is.EqualTo(0.0));
            Assert.That(GeographicMath.GetInitialBearing(
                CoordinateValue(0.0, 90.0),
                CoordinateValue(120.0, 90.0), SpatialUnit.Degrees),
                Is.EqualTo(0.0));
        });
    }

    [Test]
    public void DestinationSupportsAngularDistancesAndPoleCrossings()
    {
        Coordinate quarter = GeographicMath.GetDestination(
            CoordinateValue(0.0, 0.0), 90.0, SpatialUnit.Degrees,
            90.0, SpatialUnit.Degrees);
        Coordinate dateline = GeographicMath.GetDestination(
            CoordinateValue(179.0, 0.0), 2.0, SpatialUnit.Degrees,
            Math.PI / 2.0, SpatialUnit.Radians);
        Coordinate poleCrossing = GeographicMath.GetDestination(
            CoordinateValue(0.0, 80.0), 20.0, SpatialUnit.Degrees,
            0.0, SpatialUnit.Degrees);
        Coordinate fromPole = GeographicMath.GetDestination(
            CoordinateValue(45.0, 90.0), 10.0, SpatialUnit.Degrees,
            180.0, SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            AssertCoordinate(quarter, 90.0, 0.0, 1.0e-12);
            AssertCoordinate(dateline, -179.0, 0.0, 1.0e-12);
            AssertCoordinate(poleCrossing, -180.0, 80.0, 1.0e-12);
            Assert.That(double.IsFinite(fromPole.X), Is.True);
            Assert.That(double.IsFinite(fromPole.Y), Is.True);
            Assert.That(fromPole.X,
                Is.GreaterThanOrEqualTo(-180.0).And.LessThan(180.0));
            Assert.That(fromPole.Y,
                Is.GreaterThanOrEqualTo(-90.0).And.LessThanOrEqualTo(90.0));
        });
    }

    [Test]
    public void MidpointUsesTheShortDatelineArcAndRemainsFiniteAtPole()
    {
        Coordinate equatorial = GeographicMath.GetMidpoint(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(90.0, 0.0));
        Coordinate dateline = GeographicMath.GetMidpoint(
            CoordinateValue(170.0, 0.0),
            CoordinateValue(-170.0, 0.0));
        Coordinate pole = GeographicMath.GetMidpoint(
            CoordinateValue(0.0, 80.0),
            CoordinateValue(-180.0, 80.0));

        Assert.Multiple(() =>
        {
            AssertCoordinate(equatorial, 45.0, 0.0, 1.0e-12);
            AssertCoordinate(dateline, -180.0, 0.0, 1.0e-12);
            Assert.That(double.IsFinite(pole.X), Is.True);
            Assert.That(pole.Y, Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(pole.X,
                Is.GreaterThanOrEqualTo(-180.0).And.LessThan(180.0));
        });
    }

    [Test]
    public void InvalidUnitsCoordinatesAndUndefinedMidpointsAreRejected()
    {
        Coordinate origin = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicMath.ConvertUnits(
                    double.NaN, SpatialUnit.Meters,
                    SpatialUnit.Kilometers));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicMath.ConvertUnits(
                    1.0, (SpatialUnit)99, SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicMath.GetHaversineDistance(
                    origin, CoordinateValue(0.0, 91.0),
                    SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicMath.GetInitialBearing(
                    origin, CoordinateValue(1.0, 1.0),
                    SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicMath.GetDestination(
                    origin, 1.0, SpatialUnit.Kilometers,
                    1.0, SpatialUnit.Miles));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicMath.GetMidpoint(
                    origin, CoordinateValue(180.0, 0.0)));
        });
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y, double tolerance)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(tolerance));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(tolerance));
        });
    }
}
