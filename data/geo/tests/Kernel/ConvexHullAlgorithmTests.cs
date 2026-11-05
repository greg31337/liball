using geo;

namespace geo.Tests;

public class ConvexHullAlgorithmTests
{
    [Test]
    public void EmptyAndSingleUniqueCoordinateProduceNarrowestResults()
    {
        Geometry emptySource = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION EMPTY");
        Geometry duplicateSource = Read(
            "MULTIPOINT ZM ((3 4 9 8), (3 4 2 7), (3 4 5 6))");

        Geometry empty = ConvexHullAlgorithm.GetConvexHull(emptySource);
        Geometry point = ConvexHullAlgorithm.GetConvexHull(duplicateSource);

        duplicateSource.MultiPoint!.Points[1].Coordinate!.X = 30.0;

        Assert.Multiple(() =>
        {
            Assert.That(empty.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(GeometryInspection.IsEmptyValue(empty), Is.True);
            Assert.That(empty.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(point.GeometryType, Is.EqualTo(GeometryType.Point));
            AssertCoordinate(point.Point!.Coordinate!, 3.0, 4.0);
            Assert.That(point.Point.Coordinate!.Z, Is.EqualTo(2.0));
            Assert.That(point.Point.Coordinate.M, Is.EqualTo(7.0));
        });
    }

    [Test]
    public void CollinearDuplicatesProduceOrderedEndpointLine()
    {
        Geometry source = Read(
            "MULTIPOINT ((4 8), (0 0), (2 4), (4 8), (1 2), (3 6))");

        Geometry hull = ConvexHullAlgorithm.GetConvexHull(source);

        Assert.Multiple(() =>
        {
            Assert.That(hull.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(hull.LineString!.Coordinates, Has.Length.EqualTo(2));
            AssertCoordinate(hull.LineString.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(hull.LineString.Coordinates[1], 4.0, 8.0);
        });
    }

    [Test]
    public void MonotoneChainRemovesInteriorAndEdgeCollinearCoordinates()
    {
        Geometry source = Read(
            "MULTIPOINT ((2 2), (4 0), (0 4), (2 0), (4 4), " +
            "(0 0), (4 2), (0 2), (2 4), (1 1), (0 0))");

        Geometry hull = ConvexHullAlgorithm.GetConvexHull(source);
        Coordinate[] shell = hull.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(hull.GeometryType, Is.EqualTo(GeometryType.Polygon));
            AssertLine(shell,
                [(0.0, 0.0), (4.0, 0.0), (4.0, 4.0),
                    (0.0, 4.0), (0.0, 0.0)]);
            Assert.That(RingMath.GetExactSignedAreaSign(shell),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void HullAggregatesNestedGeometryAndIncludesPolygonHoleCoordinates()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((1 1, 5 1, 5 5, 1 5, 1 1), " +
            "(2 2, 4 2, 4 4, 2 4, 2 2)), " +
            "GEOMETRYCOLLECTION (POINT (-2 3), " +
            "LINESTRING (3 -1, 8 3)))");

        Geometry hull = ConvexHullAlgorithm.GetConvexHull(source);
        Coordinate[] shell = hull.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            AssertLine(shell,
                [(-2.0, 3.0), (3.0, -1.0),
                    (8.0, 3.0), (5.0, 5.0), (1.0, 5.0),
                    (-2.0, 3.0)]);
            Assert.That(ContainsCoordinate(shell, 2.0, 2.0), Is.False);
        });
    }

    [Test]
    public void ExactOrientationKeepsNearCollinearAndExtremeVertices()
    {
        Geometry nearCollinear = Read(
            "MULTIPOINT ((0 0), (1 0.000000000000000000000000000001), " +
            "(2 0), (1 0))");
        Geometry extreme = Read(
            "MULTIPOINT ((-1E150 -1E150), (1E150 -1E150), " +
            "(1E150 1E150), (-1E150 1E150), (0 0))");

        Geometry nearHull = ConvexHullAlgorithm.GetConvexHull(nearCollinear);
        Geometry extremeHull = ConvexHullAlgorithm.GetConvexHull(extreme);

        Assert.Multiple(() =>
        {
            Assert.That(nearHull.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(nearHull.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(4));
            Assert.That(ContainsCoordinate(
                nearHull.Polygon.Shell.Coordinates, 1.0, 1.0e-30), Is.True);
            Assert.That(extremeHull.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(extremeHull.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(RingMath.GetExactSignedAreaSign(
                extremeHull.Polygon.Shell.Coordinates), Is.EqualTo(1));
        });
    }

    [Test]
    public void PermutationsProduceTheSameDeterministicOwningHull()
    {
        Geometry first = Read(
            "MULTIPOINT ZM ((0 0 1 11), (5 0 2 12), (5 5 3 13), " +
            "(0 5 4 14), (2 2 9 19), (0 0 7 17))");
        Geometry second = Read(
            "MULTIPOINT ZM ((2 2 9 19), (0 5 4 14), (0 0 7 17), " +
            "(5 5 3 13), (5 0 2 12), (0 0 1 11))");

        Geometry firstHull = ConvexHullAlgorithm.GetConvexHull(first);
        Geometry secondHull = ConvexHullAlgorithm.GetConvexHull(second);
        first.MultiPoint!.Points[0].Coordinate!.Z = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(Write(firstHull), Is.EqualTo(Write(secondHull)));
            Assert.That(firstHull.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(firstHull.Polygon!.Shell.Coordinates[0].Z,
                Is.EqualTo(1.0));
            Assert.That(firstHull.Polygon.Shell.Coordinates[0].M,
                Is.EqualTo(11.0));
            Assert.That(firstHull.Polygon.Shell.Coordinates[4].Z,
                Is.EqualTo(1.0));
        });
    }

    private static bool ContainsCoordinate(
        Coordinate[] coordinates, double x, double y)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            if (coordinates[index].X == x && coordinates[index].Y == y)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    private static void AssertLine(
        Coordinate[] actual, (double X, double Y)[] expected)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            AssertCoordinate(actual[index],
                expected[index].X, expected[index].Y);
            index++;
        }
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y)
    {
        Assert.That(coordinate.X, Is.EqualTo(x));
        Assert.That(coordinate.Y, Is.EqualTo(y));
    }

    private static string Write(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.WriteWkt(geometry);
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
