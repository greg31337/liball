using geo;

namespace geo.Tests;

public class VisvalingamWhyattTests
{
    [Test]
    public void EffectiveAreaToleranceRemovesOnlyEligibleVertices()
    {
        Geometry source = Read(
            "LINESTRING ZM (0 0 1 11, 1 0.1 2 12, 2 0 3 13, 3 2 4 14)");

        Geometry result = VisvalingamWhyatt.SimplifyGeometry(
            source, Parameters(0.1, true));
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(3));
            AssertCoordinate(coordinates[0], 0.0, 0.0, 1.0, 11.0);
            AssertCoordinate(coordinates[1], 2.0, 0.0, 3.0, 13.0);
            AssertCoordinate(coordinates[2], 3.0, 2.0, 4.0, 14.0);
        });
    }

    [Test]
    public void EndpointPolicyControlsWhetherLineExtentsMayContract()
    {
        Geometry source = Read(
            "LINESTRING (0 0.01, 1 0, 2 0, 3 0.01)");

        Geometry preserved = VisvalingamWhyatt.SimplifyGeometry(
            source, Parameters(10.0, true));
        Geometry contracted = VisvalingamWhyatt.SimplifyGeometry(
            source, Parameters(10.0, false));

        Assert.Multiple(() =>
        {
            AssertCoordinate(
                preserved.LineString!.Coordinates[0], 0.0, 0.01);
            AssertCoordinate(
                preserved.LineString.Coordinates[1], 3.0, 0.01);
            Assert.That(contracted.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(contracted.LineString.Coordinates[0].X,
                Is.Not.EqualTo(0.0));
        });
    }

    [Test]
    public void RingsStayClosedSimpleAndAboveMinimumCardinality()
    {
        Geometry source = RingGeometry([
            Point(0.0, 0.0), Point(2.0, 0.1),
            Point(4.0, 0.0), Point(4.0, 4.0),
            Point(2.0, 3.9), Point(0.0, 4.0),
            Point(0.0, 0.0)
        ]);

        Geometry result = VisvalingamWhyatt.SimplifyGeometry(
            source, Parameters(100.0, true));
        Coordinate[] coordinates = result.LinearRing!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(4));
            Assert.That(coordinates[0].X,
                Is.EqualTo(coordinates[coordinates.Length - 1].X));
            Assert.That(coordinates[0].Y,
                Is.EqualTo(coordinates[coordinates.Length - 1].Y));
            Assert.That(VisvalingamWhyatt.ClosedRingIsSimple(coordinates),
                Is.True);
        });
    }

    [Test]
    public void PolygonShellsAndHolesAreSimplifiedWithoutInvalidatingRings()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 5 0, 10 0, 10 5, 10 10, 5 10, 0 10, 0 5, 0 0), " +
            "(2 2, 2 3, 2 4, 3 4, 4 4, 4 3, 4 2, 3 2, 2 2))");

        Geometry result = VisvalingamWhyatt.SimplifyGeometry(
            source, Parameters(0.0, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(result.Polygon.Holes[0].Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(VisvalingamWhyatt.PolygonRingsAreValid(
                result.Polygon.Shell.Coordinates,
                result.Polygon.Holes), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(96.0));
        });
    }

    [Test]
    public void CollectionsPreserveMetadataOwnershipAndPointComponents()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;GEOMETRYCOLLECTION (POINT (8 9), " +
            "LINESTRING (0 0, 1 0.1, 2 0))");

        Geometry result = VisvalingamWhyatt.SimplifyGeometry(
            source, Parameters(1.0, true));

        source.GeometryCollection!.Geometries[1]
            .LineString!.Coordinates[0].X = 99.0;
        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.GeometryCollection!.Geometries[0].GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(result.GeometryCollection.Geometries[1]
                .LineString!.Coordinates, Has.Length.EqualTo(2));
            Assert.That(result.GeometryCollection.Geometries[1]
                .LineString!.Coordinates[0].X, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void RepeatedAndDegenerateLinesRemainStructurallyValid()
    {
        Geometry repeated = Read(
            "LINESTRING (0 0, 0 0, 1 0, 2 0, 2 0)");
        Geometry degenerate = Read("LINESTRING (3 4, 3 4)");

        Geometry repeatedResult = VisvalingamWhyatt.SimplifyGeometry(
            repeated, Parameters(0.0, true));
        Geometry degenerateResult = VisvalingamWhyatt.SimplifyGeometry(
            degenerate, Parameters(1.0, true));

        Assert.Multiple(() =>
        {
            Assert.That(repeatedResult.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(degenerateResult.LineString!.Coordinates,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void InvalidParametersAndUnsupportedGeometryAreRejected()
    {
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        SimplifyParameters negative = Parameters(-1.0, true);
        SimplifyParameters nonFinite = Parameters(double.NaN, true);
        SimplifyParameters wrong = Parameters(1.0, true);
        wrong.Algorithm = SimplificationAlgorithm.DouglasPeucker;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                VisvalingamWhyatt.SimplifyGeometry(line, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                VisvalingamWhyatt.SimplifyGeometry(line, negative));
            Assert.Throws<InvalidGeometryException>(() =>
                VisvalingamWhyatt.SimplifyGeometry(line, nonFinite));
            Assert.Throws<InvalidGeometryException>(() =>
                VisvalingamWhyatt.SimplifyGeometry(line, wrong));
            Assert.Throws<InvalidGeometryException>(() =>
                VisvalingamWhyatt.SimplifyGeometry(
                    Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))"),
                    Parameters(1.0, true)));
        });
    }

    private static SimplifyParameters Parameters(
        double tolerance, bool preserveEndpoints)
    {
        return new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.VisvalingamWhyatt,
            Tolerance = tolerance,
            PreserveEndpoints = preserveEndpoints
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

    private static Coordinate Point(double x, double y)
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
