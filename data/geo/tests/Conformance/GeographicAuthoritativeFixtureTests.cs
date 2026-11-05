using geo;

namespace geo.Tests;

// Expected values are fixed results from independent 50-decimal spherical
// vector and loxodrome calculations using the IUGG mean radius 6,371,008.8 m.
public class GeographicAuthoritativeFixtureTests
{
    private const double LondonLongitude = -0.1278;
    private const double LondonLatitude = 51.5074;
    private const double NewYorkLongitude = -74.0060;
    private const double NewYorkLatitude = 40.7128;

    [Test]
    public void UnitsDistanceAndBearingsMatchFixedReferenceValues()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Coordinate london = CoordinateValue(
            LondonLongitude, LondonLatitude);
        Coordinate newYork = CoordinateValue(
            NewYorkLongitude, NewYorkLatitude);

        Assert.Multiple(() =>
        {
            Assert.That(operations.ConvertUnits(
                1.0, SpatialUnit.Degrees, SpatialUnit.Meters),
                Is.EqualTo(111195.0802335329128468).Within(1.0e-9));
            Assert.That(operations.ConvertUnits(
                100.0, SpatialUnit.Miles, SpatialUnit.Kilometers),
                Is.EqualTo(160.9344).Within(1.0e-12));
            Assert.That(operations.ConvertUnits(
                100.0, SpatialUnit.Kilometers,
                SpatialUnit.NauticalMiles),
                Is.EqualTo(53.995680345572354).Within(1.0e-13));
            Assert.That(operations.HaversineDistance(
                london, newYork, SpatialUnit.Radians),
                Is.EqualTo(0.87430892791366468969)
                    .Within(1.0e-14));
            Assert.That(operations.HaversineDistance(
                london, newYork, SpatialUnit.Meters),
                Is.EqualTo(5570229.8736565233783)
                    .Within(1.0e-6));
            Assert.That(operations.InitialBearing(
                london, newYork, SpatialUnit.Degrees),
                Is.EqualTo(-71.670298406395126028)
                    .Within(1.0e-12));
            Assert.That(operations.FinalBearing(
                london, newYork, SpatialUnit.Degrees),
                Is.EqualTo(-128.78738317580281786)
                    .Within(1.0e-12));
        });
    }

    [Test]
    public void GreatCircleAndRhumbFixturesFollowDifferentPaths()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Coordinate london = CoordinateValue(
            LondonLongitude, LondonLatitude);
        Coordinate newYork = CoordinateValue(
            NewYorkLongitude, NewYorkLatitude);

        Geometry greatCircle = operations.GreatCircle(
            london, newYork, 4);
        Coordinate[] greatCoordinates =
            greatCircle.LineString!.Coordinates;
        double rhumbDistance = operations.RhumbDistance(
            london, newYork, SpatialUnit.Meters);
        double rhumbBearing = operations.RhumbBearing(
            london, newYork, SpatialUnit.Degrees);
        Coordinate rhumbQuarter = operations.RhumbDestination(
            london, rhumbDistance * 0.25, SpatialUnit.Meters,
            rhumbBearing, SpatialUnit.Degrees);
        Coordinate rhumbMidpoint = operations.RhumbDestination(
            london, rhumbDistance * 0.5, SpatialUnit.Meters,
            rhumbBearing, SpatialUnit.Degrees);
        Coordinate rhumbThreeQuarter = operations.RhumbDestination(
            london, rhumbDistance * 0.75, SpatialUnit.Meters,
            rhumbBearing, SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            AssertCoordinate(greatCoordinates[1],
                -20.502595504939325629,
                53.756369289986862413, 1.0e-11);
            AssertCoordinate(greatCoordinates[2],
                -41.290307356248558733,
                52.368439587462430431, 1.0e-11);
            AssertCoordinate(greatCoordinates[3],
                -59.458919746878308758,
                47.706765981786663500, 1.0e-11);
            Assert.That(rhumbDistance,
                Is.EqualTo(5794128.7539156958908)
                    .Within(1.0e-6));
            Assert.That(rhumbBearing,
                Is.EqualTo(-101.95591913189548114)
                    .Within(1.0e-12));
            AssertCoordinate(rhumbQuarter,
                -20.027102495300953683,
                48.80875, 1.0e-11);
            AssertCoordinate(rhumbMidpoint,
                -38.882455453911850495,
                46.1101, 1.0e-11);
            AssertCoordinate(rhumbThreeQuarter,
                -56.835836980716684174,
                43.41145, 1.0e-11);
        });
    }

    [Test]
    public void DestinationPoleAndDatelineFixturesRemainNormalized()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Coordinate london = CoordinateValue(
            LondonLongitude, LondonLatitude);

        Coordinate destination = operations.Destination(
            london, 1000.0, SpatialUnit.Kilometers,
            45.0, SpatialUnit.Degrees);
        Coordinate poleCrossing = operations.Destination(
            CoordinateValue(45.0, 85.0),
            1000.0, SpatialUnit.Kilometers,
            0.0, SpatialUnit.Degrees);
        Geometry dateline = operations.GreatCircle(
            CoordinateValue(170.0, 10.0),
            CoordinateValue(-170.0, 20.0), 4);
        Coordinate[] coordinates = dateline.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            AssertCoordinate(destination,
                11.690491591325289424,
                57.337493629706649398, 1.0e-11);
            AssertCoordinate(poleCrossing,
                -135.0, 86.006796362754620449, 1.0e-11);
            AssertCoordinate(coordinates[1],
                174.83167423900576159,
                12.654735660097912794, 1.0e-11);
            AssertCoordinate(coordinates[2],
                179.76316648277034727,
                15.220616735373661371, 1.0e-11);
            AssertCoordinate(coordinates[3],
                -175.18639874271481919,
                17.676418629327388338, 1.0e-11);
            Assert.That(operations.HaversineDistance(
                CoordinateValue(10.0, 90.0),
                CoordinateValue(-120.0, 90.0),
                SpatialUnit.Meters), Is.Zero);
        });
    }

    [Test]
    public void SphericalAreaMatchesExactAndPolarReferences()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Geometry octant = Read(
            "POLYGON ((0 0, 90 0, 0 90, 0 0))");
        Geometry polarTriangle = Read(
            "POLYGON ((0 80, 120 80, -120 80, 0 80))");
        Geometry datelineQuadrilateral = Read(
            "POLYGON ((170 0, -170 0, -170 10, 170 10, 170 0))");

        Assert.Multiple(() =>
        {
            Assert.That(operations.SphericalArea(
                octant, SpatialUnit.Radians),
                Is.EqualTo(1.5707963267948966192)
                    .Within(1.0e-14));
            Assert.That(operations.SphericalArea(
                octant, SpatialUnit.Kilometers),
                Is.EqualTo(63758235.121608981855)
                    .Within(1.0e-6));
            Assert.That(operations.SphericalArea(
                polarTriangle, SpatialUnit.Radians),
                Is.EqualTo(0.03992494515762060146)
                    .Within(1.0e-14));
            Assert.That(operations.SphericalArea(
                datelineQuadrilateral, SpatialUnit.Radians),
                Is.EqualTo(0.061218591421486085558)
                    .Within(1.0e-14));
        });
    }

    [Test]
    public void AlongSliceAndNearestMatchSphericalVectorFixtures()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Geometry route = Read(
            "LINESTRING (-0.1278 51.5074, -74.006 40.7128)");
        const double routeMeters = 5570229.8736565233783;

        Coordinate? quarter = operations.AlongGeographicLine(
            route, routeMeters * 0.25, SpatialUnit.Meters);
        Geometry slice = operations.SliceGeographicLine(
            route, routeMeters * 0.25, routeMeters * 0.75,
            SpatialUnit.Meters);
        Coordinate[] sliceCoordinates =
            slice.LineString!.Coordinates;
        DistanceResult nearest = operations.NearestOnGeographicLine(
            Read("LINESTRING (-20 10, 40 35)"),
            CoordinateValue(5.0, 40.0), SpatialUnit.Radians);

        Assert.Multiple(() =>
        {
            AssertCoordinate(quarter!,
                -20.502595504939325629,
                53.756369289986862413, 1.0e-11);
            Assert.That(sliceCoordinates, Has.Length.EqualTo(2));
            AssertCoordinate(sliceCoordinates[0],
                -20.502595504939325629,
                53.756369289986862413, 1.0e-11);
            AssertCoordinate(sliceCoordinates[1],
                -59.458919746878308758,
                47.706765981786663500, 1.0e-11);
            Assert.That(nearest.Distance,
                Is.EqualTo(0.23791547346400026225)
                    .Within(1.0e-13));
            AssertCoordinate(nearest.FirstCoordinate!,
                12.200911516918433826,
                27.735980423567316768, 1.0e-11);
            Assert.That(nearest.FirstLocation!.SegmentFraction,
                Is.EqualTo(0.58650239661172989522)
                    .Within(1.0e-13));
        });
    }

    [Test]
    public void GeographicCircleVerticesMatchDestinationFixtures()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Geometry circle = operations.GeographicCircle(
            CoordinateValue(12.5, 48.3),
            750.0, SpatialUnit.Kilometers, 8);
        Coordinate[] coordinates =
            circle.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(9));
            AssertCoordinate(coordinates[0],
                12.5, 55.044902727934031821, 1.0e-11);
            AssertCoordinate(coordinates[1],
                4.601519852438235903,
                52.817766912372453812, 1.0e-11);
            AssertCoordinate(coordinates[2],
                2.419033319717718731,
                47.856841978208373692, 1.0e-11);
            AssertCoordinate(coordinates[3],
                5.944018954464275320,
                43.331936390329606979, 1.0e-11);
            AssertCoordinate(coordinates[4],
                12.5, 41.555097272065962495, 1.0e-11);
            AssertCoordinate(coordinates[8],
                coordinates[0].X, coordinates[0].Y, 0.0);
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
            Assert.That(coordinate.X,
                Is.EqualTo(x).Within(tolerance));
            Assert.That(coordinate.Y,
                Is.EqualTo(y).Within(tolerance));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
