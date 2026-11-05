using geo;

namespace geo.Tests;

public class InteriorCircleAlgorithmsTests
{
    [Test]
    public void MaximumInscribedCircleFindsRectangleCenterAndBoundaryWitness()
    {
        Geometry rectangle = Read(
            "POLYGON ((0 0, 10 0, 10 4, 0 4, 0 0))");

        CircleResult result =
            InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                rectangle, 0.01);

        Assert.Multiple(() =>
        {
            AssertCoordinate(result.Center!, 2.0, 2.0, 0.01);
            Assert.That(result.Radius, Is.EqualTo(2.0).Within(0.01));
            Assert.That(result.Witnesses, Has.Length.EqualTo(1));
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                result.Center!, result.Witnesses[0]),
                Is.EqualTo(result.Radius).Within(1e-12));
            Assert.That(result.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryPointLocation.LocatePoint(
                result.Center!, rectangle, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Interior));
        });
    }

    [Test]
    public void InscribedCircleAvoidsHolesAndSelectsLargestPolygonComponent()
    {
        Geometry holed = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(4 4, 6 4, 6 6, 4 6, 4 4))");
        Geometry components = ReadWithSrid(
            "SRID=3857;MULTIPOLYGON (" +
            "((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "((10 0, 16 0, 16 6, 10 6, 10 0)))");

        CircleResult holeResult =
            InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                holed, 0.01);
        CircleResult componentResult =
            InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                components, 0.001);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPointLocation.LocatePoint(
                holeResult.Center!, holed, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                holeResult.Center!, holeResult.Witnesses[0]),
                Is.EqualTo(holeResult.Radius).Within(1e-12));
            Assert.That(holeResult.Radius, Is.GreaterThan(2.0));
            AssertCoordinate(componentResult.Center!, 13.0, 3.0, 0.001);
            Assert.That(componentResult.Radius,
                Is.EqualTo(3.0).Within(0.001));
            Assert.That(componentResult.Geometry!.SpatialReferenceId,
                Is.EqualTo(3857));
        });
    }

    [Test]
    public void LargestEmptyCircleBalancesPointObstaclesAndBoundary()
    {
        Geometry boundary = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        Geometry obstacles = Read(
            "MULTIPOINT ((2 5), (8 5))");

        CircleResult result = InteriorCircleAlgorithms.GetLargestEmptyCircle(
            obstacles, boundary, 0.001);

        Assert.Multiple(() =>
        {
            AssertCoordinate(result.Center!, 5.0, 3.4, 0.001);
            Assert.That(result.Radius, Is.EqualTo(3.4).Within(0.001));
            Assert.That(result.Witnesses, Has.Length.EqualTo(1));
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                result.Center!, result.Witnesses[0]),
                Is.EqualTo(result.Radius).Within(1e-12));
            Assert.That(GeometryPointLocation.LocatePoint(
                result.Center!, boundary, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Interior));
        });
    }

    [Test]
    public void EmptyAndPolygonObstaclesUseTheNearestActiveConstraint()
    {
        Geometry boundary = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        Geometry emptyObstacles = Read("GEOMETRYCOLLECTION EMPTY");
        Geometry polygonObstacle = Read(
            "POLYGON ((4 4, 6 4, 6 6, 4 6, 4 4))");

        CircleResult empty = InteriorCircleAlgorithms.GetLargestEmptyCircle(
            emptyObstacles, boundary, 0.001);
        CircleResult avoided = InteriorCircleAlgorithms.GetLargestEmptyCircle(
            polygonObstacle, boundary, 0.01);

        Assert.Multiple(() =>
        {
            AssertCoordinate(empty.Center!, 5.0, 5.0, 0.001);
            Assert.That(empty.Radius, Is.EqualTo(5.0).Within(0.001));
            Assert.That(GeometryPointLocation.LocatePoint(
                avoided.Center!, polygonObstacle, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(avoided.Radius, Is.GreaterThan(2.0));
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                avoided.Center!, avoided.Witnesses[0]),
                Is.EqualTo(avoided.Radius).Within(1e-12));
        });
    }

    [Test]
    public void PolygonTangentsReturnTwoDeterministicOwningSupportSegments()
    {
        Geometry polygon = Read(
            "POLYGON ZM ((0 0 1 11, 4 0 2 12, 4 4 3 13, " +
            "0 4 4 14, 0 0 1 11))");
        Coordinate point = new Coordinate
        {
            X = -2.0,
            Y = 2.0,
            Z = 9.0,
            M = 19.0
        };

        Geometry tangents = InteriorCircleAlgorithms.GetPolygonTangents(
            polygon, point);
        LineString[] lines = tangents.MultiLineString!.LineStrings;
        polygon.Polygon!.Shell.Coordinates[0].Z = 100.0;
        point.X = -20.0;

        Assert.Multiple(() =>
        {
            Assert.That(tangents.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lines, Has.Length.EqualTo(2));
            AssertCoordinate(lines[0].Coordinates[0], -2.0, 2.0, 0.0);
            AssertCoordinate(lines[0].Coordinates[1], 0.0, 0.0, 0.0);
            AssertCoordinate(lines[1].Coordinates[0], -2.0, 2.0, 0.0);
            AssertCoordinate(lines[1].Coordinates[1], 0.0, 4.0, 0.0);
            Assert.That(lines[0].Coordinates[0].Z, Is.EqualTo(9.0));
            Assert.That(lines[0].Coordinates[1].Z, Is.EqualTo(1.0));
            Assert.That(lines[1].Coordinates[1].M, Is.EqualTo(14.0));
        });
    }

    [Test]
    public void EmptyDomainsAndInvalidCircleOrTangentInputsAreDefined()
    {
        Geometry emptyPolygon = Read("POLYGON EMPTY");
        CircleResult empty =
            InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                emptyPolygon, 0.1);
        Geometry emptyTangents =
            InteriorCircleAlgorithms.GetPolygonTangents(
                emptyPolygon, Point(2.0, 2.0));
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.That(empty.Geometry, Is.Null);
            Assert.That(empty.Center, Is.Null);
            Assert.That(emptyTangents.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(GeometryInspection.IsEmptyValue(emptyTangents), Is.True);
            Assert.Throws<InvalidGeometryException>(() =>
                InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                    polygon, 0.0));
            Assert.Throws<InvalidGeometryException>(() =>
                InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                    Read("LINESTRING (0 0, 1 1)"), 0.1));
            Assert.Throws<InvalidGeometryException>(() =>
                InteriorCircleAlgorithms.GetLargestEmptyCircle(
                    ReadWithSrid("SRID=4326;POINT (2 2)"),
                    ReadWithSrid(
                        "SRID=3857;POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))"),
                    0.1));
            Assert.Throws<InvalidGeometryException>(() =>
                InteriorCircleAlgorithms.GetPolygonTangents(
                    polygon, Point(2.0, 2.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                InteriorCircleAlgorithms.GetPolygonTangents(
                    polygon, null!));
        });
    }

    private static CurveLinearizationParameters CurveParameters()
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 16
        };
    }

    private static Coordinate Point(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y, double tolerance)
    {
        Assert.That(coordinate.X, Is.EqualTo(x).Within(tolerance));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(tolerance));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadWithSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
