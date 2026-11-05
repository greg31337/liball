using geo;

namespace geo.Tests;

public class GeographicOperationsTests
{
    [Test]
    public void PointCalculationsExposeExplicitDistanceAndAngularUnits()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate east = CoordinateValue(90.0, 0.0);

        Coordinate destination = operations.Destination(
            origin, Math.PI / 2.0, SpatialUnit.Radians,
            Math.PI / 2.0, SpatialUnit.Radians);
        Coordinate midpoint = operations.GeographicMidpoint(
            origin, east);
        Coordinate rhumbDestination = operations.RhumbDestination(
            origin, 90.0, SpatialUnit.Degrees,
            90.0, SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            Assert.That(operations.ConvertUnits(
                1.0, SpatialUnit.Kilometers, SpatialUnit.Meters),
                Is.EqualTo(1000.0));
            Assert.That(operations.HaversineDistance(
                origin, east, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(operations.InitialBearing(
                origin, east, SpatialUnit.Radians),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-12));
            Assert.That(operations.FinalBearing(
                origin, east, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            AssertCoordinate(destination, 90.0, 0.0, 1.0e-12);
            AssertCoordinate(midpoint, 45.0, 0.0, 1.0e-12);
            Assert.That(operations.RhumbDistance(
                origin, east, SpatialUnit.Radians),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-12));
            Assert.That(operations.RhumbBearing(
                origin, east, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            AssertCoordinate(rhumbDestination, 90.0, 0.0, 1.0e-12);
        });
    }

    [Test]
    public void GeometryCalculationsDelegateThroughThePublicContract()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Geometry line = Read(
            "LINESTRING (170 0, -170 0, -170 10)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 90 0, 0 90, 0 0))");

        Geometry greatCircle = operations.GreatCircle(
            CoordinateValue(170.0, 0.0),
            CoordinateValue(-170.0, 0.0), 4);
        Coordinate? along = operations.AlongGeographicLine(
            line, 10.0, SpatialUnit.Degrees);
        Geometry slice = operations.SliceGeographicLine(
            line, 5.0, 25.0, SpatialUnit.Degrees);
        DistanceResult nearest = operations.NearestOnGeographicLine(
            line, CoordinateValue(180.0, 5.0),
            SpatialUnit.Kilometers);

        Assert.Multiple(() =>
        {
            Assert.That(greatCircle.LineString!.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(operations.GeographicLength(
                line, SpatialUnit.Degrees),
                Is.EqualTo(30.0).Within(1.0e-12));
            Assert.That(operations.SphericalArea(
                polygon, SpatialUnit.Radians),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-14));
            AssertCoordinate(along!, -180.0, 0.0, 1.0e-12);
            Assert.That(operations.GeographicLength(
                slice, SpatialUnit.Degrees),
                Is.EqualTo(20.0).Within(1.0e-12));
            Assert.That(nearest.HasResult, Is.True);
            Assert.That(nearest.Distance,
                Is.EqualTo(5.0 * Math.PI / 180.0 *
                    GeographicMath.EarthRadiusMeters / 1000.0)
                    .Within(1.0e-9));
        });
    }

    [Test]
    public void GeographicCircleUsesTheCallerSelectedRadiusUnit()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Coordinate center = CoordinateValue(179.0, 0.0);

        Geometry circle = operations.GeographicCircle(
            center, 500.0, SpatialUnit.Kilometers, 16);
        Coordinate[] coordinates = circle.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(circle.SpatialReferenceId,
                Is.EqualTo(GeometryConstants.Crs84SpatialReferenceId));
            Assert.That(coordinates, Has.Length.EqualTo(17));
            Assert.That(operations.HaversineDistance(
                center, coordinates[12], SpatialUnit.Kilometers),
                Is.EqualTo(500.0).Within(1.0e-9));
            Assert.That(coordinates[12].X, Is.LessThan(0.0));
        });
    }

    [Test]
    public void PublicOperationsRetainKernelFailurePolicies()
    {
        IGeographicOperations operations =
            new GeographicOperations();
        Coordinate origin = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                operations.InitialBearing(
                    origin, CoordinateValue(1.0, 0.0),
                    SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.GreatCircle(
                    origin, CoordinateValue(1.0, 0.0), 0));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.GeographicLength(
                    Read("POINT (0 0)"), SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.SphericalArea(
                    Read("LINESTRING (0 0, 1 1)"),
                    SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.GeographicCircle(
                    origin, -1.0, SpatialUnit.Meters, 16));
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

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
