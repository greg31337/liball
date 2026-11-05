using geo;

namespace geo.Tests;

public class TopologyPreservingSimplifierTests
{
    [Test]
    public void RejectsShortcutsThatCreateCrossingsBetweenLineComponents()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 2, 4 0), (2 -1, 2 1))");

        Geometry result = TopologyPreservingSimplifier.SimplifyGeometry(
            source, Parameters(3.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.MultiLineString!.LineStrings[0].Coordinates,
                Has.Length.EqualTo(3));
            Assert.That(result.MultiLineString.LineStrings[1].Coordinates,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                result.MultiLineString.LineStrings[0].Coordinates[1],
                2.0, 2.0);
        });
    }

    [Test]
    public void RejectsShortcutsThatCreateCollinearOverlaps()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 1, 4 0), (1 0, 3 0))");

        Geometry result = TopologyPreservingSimplifier.SimplifyGeometry(
            source, Parameters(2.0));

        Assert.That(result.MultiLineString!.LineStrings[0].Coordinates,
            Has.Length.EqualTo(3));
    }

    [Test]
    public void AllowsShortcutsThatRetainSharedEndpointNodes()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0.1, 2 0), (2 0, 2 2))");

        Geometry result = TopologyPreservingSimplifier.SimplifyGeometry(
            source, Parameters(0.2));

        Assert.Multiple(() =>
        {
            Assert.That(result.MultiLineString!.LineStrings[0].Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(result.MultiLineString.LineStrings[1].Coordinates,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                result.MultiLineString.LineStrings[0].Coordinates[1],
                2.0, 0.0);
            AssertCoordinate(
                result.MultiLineString.LineStrings[1].Coordinates[0],
                2.0, 0.0);
        });
    }

    [Test]
    public void RingsRemainClosedAndCannotSimplifyBelowThreeEdges()
    {
        Geometry source = RingGeometry([
            CoordinateValue(0.0, 2.0),
            CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(3.0, 1.0),
            CoordinateValue(4.0, 2.0),
            CoordinateValue(3.0, 3.0),
            CoordinateValue(2.0, 4.0),
            CoordinateValue(1.0, 3.0),
            CoordinateValue(0.0, 2.0)
        ]);

        Geometry result = TopologyPreservingSimplifier.SimplifyGeometry(
            source, Parameters(100.0));
        Coordinate[] coordinates = result.LinearRing!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LinearRing));
            Assert.That(coordinates, Has.Length.EqualTo(5));
            Assert.That(coordinates[0].X,
                Is.EqualTo(coordinates[coordinates.Length - 1].X));
            Assert.That(coordinates[0].Y,
                Is.EqualTo(coordinates[coordinates.Length - 1].Y));
        });
    }

    [Test]
    public void PolygonsSimplifyShellAndHoleWithoutCollapsingEitherRing()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 5 0, 10 0, 10 5, 10 10, 5 10, 0 10, 0 5, 0 0), " +
            "(2 2, 2 3, 2 4, 3 4, 4 4, 4 3, 4 2, 3 2, 2 2))");

        Geometry result = TopologyPreservingSimplifier.SimplifyGeometry(
            source, Parameters(0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(result.Polygon.Holes, Has.Length.EqualTo(1));
            Assert.That(result.Polygon.Holes[0].Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(96.0));
        });
    }

    [Test]
    public void CollectionsPreserveMetadataLayoutOrdinatesAndOwnership()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;GEOMETRYCOLLECTION ZM (POINT ZM (7 8 9 10), " +
            "LINESTRING ZM (0 0 1 11, 1 0.1 2 12, 2 0 3 13))");

        Geometry result = TopologyPreservingSimplifier.SimplifyGeometry(
            source, Parameters(0.2));
        Geometry line = result.GeometryCollection!.Geometries[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(line.LineString!.Coordinates, Has.Length.EqualTo(2));
            AssertCoordinate(
                line.LineString.Coordinates[0], 0.0, 0.0, 1.0, 11.0);
            AssertCoordinate(
                line.LineString.Coordinates[1], 2.0, 0.0, 3.0, 13.0);
        });

        source.GeometryCollection!.Geometries[1]
            .LineString!.Coordinates[0].X = 99.0;
        Assert.That(line.LineString!.Coordinates[0].X, Is.EqualTo(0.0));
    }

    [Test]
    public void EmptyAndPointOnlyInputsReturnOwningCopies()
    {
        Geometry empty = Read("POLYGON EMPTY");
        Geometry point = Read("POINT (1 2)");

        Geometry emptyResult =
            TopologyPreservingSimplifier.SimplifyGeometry(
                empty, Parameters(1.0));
        Geometry pointResult =
            TopologyPreservingSimplifier.SimplifyGeometry(
                point, Parameters(1.0));

        point.Point!.Coordinate!.X = 9.0;
        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(emptyResult), Is.True);
            Assert.That(pointResult.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(pointResult.Point!.Coordinate!.X, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void RepeatedVerticesAreCleanedWithoutCreatingInvalidLines()
    {
        Geometry repeated = Read(
            "LINESTRING (0 0, 0 0, 1 0, 2 0, 2 0)");
        Geometry collapsed = Read("LINESTRING (3 4, 3 4)");

        Geometry repeatedResult =
            TopologyPreservingSimplifier.SimplifyGeometry(
                repeated, Parameters(0.0));
        Geometry collapsedResult =
            TopologyPreservingSimplifier.SimplifyGeometry(
                collapsed, Parameters(1.0));

        Assert.Multiple(() =>
        {
            Assert.That(repeatedResult.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(collapsedResult.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                collapsedResult.LineString.Coordinates[0], 3.0, 4.0);
            AssertCoordinate(
                collapsedResult.LineString.Coordinates[1], 3.0, 4.0);
        });
    }

    [Test]
    public void InvalidParametersAndUnsupportedGeometriesAreRejected()
    {
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        SimplifyParameters negative = Parameters(-1.0);
        SimplifyParameters infinite = Parameters(double.PositiveInfinity);
        SimplifyParameters wrongAlgorithm = Parameters(1.0);
        wrongAlgorithm.Algorithm = SimplificationAlgorithm.DouglasPeucker;
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                TopologyPreservingSimplifier.SimplifyGeometry(line, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                TopologyPreservingSimplifier.SimplifyGeometry(line, negative));
            Assert.Throws<InvalidGeometryException>(() =>
                TopologyPreservingSimplifier.SimplifyGeometry(line, infinite));
            Assert.Throws<InvalidGeometryException>(() =>
                TopologyPreservingSimplifier.SimplifyGeometry(
                    line, wrongAlgorithm));
            Assert.Throws<InvalidGeometryException>(() =>
                TopologyPreservingSimplifier.SimplifyGeometry(
                    triangle, Parameters(1.0)));
        });
    }

    private static SimplifyParameters Parameters(double tolerance)
    {
        return new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.TopologyPreserving,
            Tolerance = tolerance,
            PreserveEndpoints = true
        };
    }

    private static Geometry RingGeometry(Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing { Coordinates = coordinates }
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z = double.NaN, double m = double.NaN)
    {
        Assert.That(coordinate.X, Is.EqualTo(x));
        Assert.That(coordinate.Y, Is.EqualTo(y));
        if (!double.IsNaN(z)) Assert.That(coordinate.Z, Is.EqualTo(z));
        if (!double.IsNaN(m)) Assert.That(coordinate.M, Is.EqualTo(m));
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
