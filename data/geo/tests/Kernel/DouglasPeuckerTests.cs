using geo;

namespace geo.Tests;

public class DouglasPeuckerTests
{
    [Test]
    public void OpenLinesRemoveNearbyVerticesAndPreserveEndpointOrdinates()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;LINESTRING ZM (0 0 1 11, 1 0.1 2 12, 2 0 3 13, 3 3 4 14, 4 3.1 5 15, 5 3 6 16)");

        Geometry result = DouglasPeucker.SimplifyGeometry(
            source, Parameters(0.2, false));
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(coordinates, Has.Length.EqualTo(4));
            AssertCoordinate(coordinates[0], 0.0, 0.0, 1.0, 11.0);
            AssertCoordinate(coordinates[1], 2.0, 0.0, 3.0, 13.0);
            AssertCoordinate(coordinates[2], 3.0, 3.0, 4.0, 14.0);
            AssertCoordinate(coordinates[3], 5.0, 3.0, 6.0, 16.0);
        });

        source.LineString!.Coordinates[0].X = 99.0;
        Assert.That(coordinates[0].X, Is.EqualTo(0.0));
    }

    [Test]
    public void ZeroToleranceRemovesRepeatedAndCollinearInteriorVertices()
    {
        Geometry source = Read(
            "LINESTRING (0 0, 1 0, 1 0, 2 0, 2 1)");

        Geometry result = DouglasPeucker.SimplifyGeometry(
            source, Parameters(0.0, false));
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(3));
            AssertCoordinate(coordinates[0], 0.0, 0.0);
            AssertCoordinate(coordinates[1], 2.0, 0.0);
            AssertCoordinate(coordinates[2], 2.0, 1.0);
        });
    }

    [Test]
    public void RingsStayClosedAndRetainTheMinimumValidCardinality()
    {
        Geometry source = RingGeometry([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 2.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(2.0, 4.0),
            CoordinateValue(0.0, 4.0),
            CoordinateValue(0.0, 2.0),
            CoordinateValue(0.0, 0.0)
        ]);

        Geometry result = DouglasPeucker.SimplifyGeometry(
            source, Parameters(0.0, false));
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
            Assert.That(Math.Abs(RingMath.GetSignedArea(coordinates)),
                Is.EqualTo(16.0));
        });
    }

    [Test]
    public void CollapsedRingsAreEmptyUnlessCollapsedComponentsAreRequested()
    {
        Geometry source = RingGeometry(RectangleRing(
            0.0, 0.0, 1.0, 1.0));

        Geometry discarded = DouglasPeucker.SimplifyGeometry(
            source, Parameters(10.0, false));
        Geometry retained = DouglasPeucker.SimplifyGeometry(
            source, Parameters(10.0, true));

        Assert.Multiple(() =>
        {
            Assert.That(discarded.GeometryType,
                Is.EqualTo(GeometryType.LinearRing));
            Assert.That(GeometryInspection.IsEmptyValue(discarded), Is.True);
            Assert.That(retained.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(retained.LineString!.Coordinates,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void PolygonsDropCollapsedHolesWhileKeepingValidShells()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 50 0, 100 0, 100 50, 100 100, 50 100, 0 100, 0 50, 0 0), " +
            "(10 10, 11 10, 11 11, 10 11, 10 10))");

        Geometry result = DouglasPeucker.SimplifyGeometry(
            source, Parameters(2.0, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(result.Polygon.Holes, Is.Empty);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(10000.0));
        });
    }

    [Test]
    public void CollapsedPolygonShellHonorsTheComponentRetentionPolicy()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 1 0, 1 1, 0 1, 0 0))");

        Geometry discarded = DouglasPeucker.SimplifyGeometry(
            source, Parameters(10.0, false));
        Geometry retained = DouglasPeucker.SimplifyGeometry(
            source, Parameters(10.0, true));

        Assert.Multiple(() =>
        {
            Assert.That(discarded.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(discarded), Is.True);
            Assert.That(retained.GeometryType,
                Is.EqualTo(GeometryType.LineString));
        });
    }

    [Test]
    public void MultiPolygonsDropCollapsedMembersOrReturnMixedResults()
    {
        Geometry source = Read(
            "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), " +
            "((20 20, 21 20, 21 21, 20 21, 20 20)))");

        Geometry discarded = DouglasPeucker.SimplifyGeometry(
            source, Parameters(2.0, false));
        Geometry retained = DouglasPeucker.SimplifyGeometry(
            source, Parameters(2.0, true));

        Assert.Multiple(() =>
        {
            Assert.That(discarded.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(discarded.MultiPolygon!.Polygons,
                Has.Length.EqualTo(1));
            Assert.That(retained.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(retained.GeometryCollection!.Geometries,
                Has.Length.EqualTo(2));
            Assert.That(retained.GeometryCollection.Geometries[0].GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(retained.GeometryCollection.Geometries[1].GeometryType,
                Is.EqualTo(GeometryType.LineString));
        });
    }

    [Test]
    public void CollectionsRecurseAndPreserveTheirShapeAndMetadata()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION (POINT (7 8), " +
            "LINESTRING (0 0, 1 0.1, 2 0))");

        Geometry result = DouglasPeucker.SimplifyGeometry(
            source, Parameters(0.2, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(result.GeometryCollection!.Geometries,
                Has.Length.EqualTo(2));
            Assert.That(result.GeometryCollection.Geometries[0].GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(result.GeometryCollection.Geometries[1].LineString!
                .Coordinates, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void MultiLinesSimplifyEachComponentAndEmptyValuesKeepTheirType()
    {
        Geometry multiLine = Read(
            "MULTILINESTRING ((0 0, 1 0.1, 2 0), " +
            "(10 0, 11 2, 12 0))");
        Geometry emptyRing = RingGeometry([]);
        Geometry emptyPolygon = Read("POLYGON EMPTY");

        Geometry lineResult = DouglasPeucker.SimplifyGeometry(
            multiLine, Parameters(0.2, false));
        Geometry ringResult = DouglasPeucker.SimplifyGeometry(
            emptyRing, Parameters(1.0, true));
        Geometry polygonResult = DouglasPeucker.SimplifyGeometry(
            emptyPolygon, Parameters(1.0, true));

        Assert.Multiple(() =>
        {
            Assert.That(lineResult.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lineResult.MultiLineString!.LineStrings[0].Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(lineResult.MultiLineString.LineStrings[1].Coordinates,
                Has.Length.EqualTo(3));
            Assert.That(ringResult.GeometryType,
                Is.EqualTo(GeometryType.LinearRing));
            Assert.That(GeometryInspection.IsEmptyValue(ringResult), Is.True);
            Assert.That(polygonResult.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(polygonResult), Is.True);
        });
    }

    [Test]
    public void InvalidParametersAndUnsupportedGeometryAreRejected()
    {
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        SimplifyParameters negative = Parameters(-1.0, false);
        SimplifyParameters nonFinite = Parameters(double.NaN, false);
        SimplifyParameters wrongAlgorithm = Parameters(1.0, false);
        wrongAlgorithm.Algorithm =
            SimplificationAlgorithm.TopologyPreserving;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                DouglasPeucker.SimplifyGeometry(line, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                DouglasPeucker.SimplifyGeometry(line, negative));
            Assert.Throws<InvalidGeometryException>(() =>
                DouglasPeucker.SimplifyGeometry(line, nonFinite));
            Assert.Throws<InvalidGeometryException>(() =>
                DouglasPeucker.SimplifyGeometry(line, wrongAlgorithm));
            Assert.Throws<InvalidGeometryException>(() =>
                DouglasPeucker.SimplifyGeometry(
                    triangle, Parameters(1.0, false)));
        });
    }

    private static SimplifyParameters Parameters(
        double tolerance, bool keepCollapsedComponents)
    {
        return new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.DouglasPeucker,
            Tolerance = tolerance,
            PreserveEndpoints = true,
            KeepCollapsedComponents = keepCollapsedComponents
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

    private static Coordinate[] RectangleRing(
        double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        return [
            CoordinateValue(minimumX, minimumY),
            CoordinateValue(maximumX, minimumY),
            CoordinateValue(maximumX, maximumY),
            CoordinateValue(minimumX, maximumY),
            CoordinateValue(minimumX, minimumY)
        ];
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
