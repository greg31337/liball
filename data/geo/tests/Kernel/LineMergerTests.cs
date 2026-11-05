using geo;

namespace geo.Tests;

public class LineMergerTests
{
    [Test]
    public void UndirectedMergeReversesEdgesAndPreservesInteriorOrdinates()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;MULTILINESTRING ZM (" +
            "(2 0 20 200, 1 0 10 100), " +
            "(2 0 20 200, 2.5 0 25 250, 3 0 30 300), " +
            "(1 0 10 100, 0 0 0 0))");

        Geometry result = LineMerger.MergeGeometry(source, false);
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(coordinates, Has.Length.EqualTo(5));
            AssertCoordinate(coordinates[0], 0.0, 0.0, 0.0, 0.0);
            AssertCoordinate(coordinates[1], 1.0, 0.0, 10.0, 100.0);
            AssertCoordinate(coordinates[2], 2.0, 0.0, 20.0, 200.0);
            AssertCoordinate(coordinates[3], 2.5, 0.0, 25.0, 250.0);
            AssertCoordinate(coordinates[4], 3.0, 0.0, 30.0, 300.0);
        });

        source.MultiLineString!.LineStrings[2].Coordinates[1].X = 99.0;
        Assert.That(coordinates[0].X, Is.EqualTo(0.0));
    }

    [Test]
    public void DirectedMergeJoinsOnlyConsistentlyOrientedEdges()
    {
        Geometry aligned = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0), (2 0, 3 0))");
        Geometry opposed = Read(
            "MULTILINESTRING ((0 0, 1 0), (2 0, 1 0))");

        Geometry alignedResult = LineMerger.MergeGeometry(aligned, true);
        Geometry opposedResult = LineMerger.MergeGeometry(opposed, true);

        Assert.Multiple(() =>
        {
            Assert.That(alignedResult.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(alignedResult.LineString!.Coordinates,
                Has.Length.EqualTo(4));
            AssertCoordinate(
                alignedResult.LineString.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(
                alignedResult.LineString.Coordinates[3], 3.0, 0.0);
            Assert.That(opposedResult.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(opposedResult.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                opposedResult.MultiLineString.LineStrings[1].Coordinates[0],
                2.0, 0.0);
            AssertCoordinate(
                opposedResult.MultiLineString.LineStrings[1].Coordinates[1],
                1.0, 0.0);
        });
    }

    [Test]
    public void UndirectedMergeJoinsOpposedEdges()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0), (2 0, 1 0))");

        Geometry result = LineMerger.MergeGeometry(source, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            AssertCoordinate(result.LineString.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(result.LineString.Coordinates[2], 2.0, 0.0);
        });
    }

    [Test]
    public void BranchNodesTerminateMergedPaths()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0), " +
            "(2 0, 3 0), (2 0, 2 1))");

        Geometry result = LineMerger.MergeGeometry(source, false);
        LineString[] lines = result.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(3));
            Assert.That(lines[0].Coordinates, Has.Length.EqualTo(3));
            AssertCoordinate(lines[0].Coordinates[0], 0.0, 0.0);
            AssertCoordinate(lines[0].Coordinates[2], 2.0, 0.0);
        });
    }

    [Test]
    public void AllDegreeTwoLoopsAreEmittedAsClosedLines()
    {
        Geometry undirectedSource = Read(
            "MULTILINESTRING ((1 1, 0 1), (0 0, 1 0), " +
            "(1 0, 1 1), (0 1, 0 0))");
        Geometry directedSource = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 1, 0 1), " +
            "(1 0, 1 1), (0 1, 0 0))");

        Geometry undirected =
            LineMerger.MergeGeometry(undirectedSource, false);
        Geometry directed =
            LineMerger.MergeGeometry(directedSource, true);

        Assert.Multiple(() =>
        {
            Assert.That(undirected.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(undirected.LineString!.Coordinates,
                Has.Length.EqualTo(5));
            AssertClosed(undirected.LineString.Coordinates);
            Assert.That(directed.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(directed.LineString!.Coordinates,
                Has.Length.EqualTo(5));
            AssertClosed(directed.LineString.Coordinates);
            AssertCoordinate(directed.LineString.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(directed.LineString.Coordinates[1], 1.0, 0.0);
        });
    }

    [Test]
    public void ReverseDuplicateEdgesFormAnUndirectedLoop()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 0 0))");

        Geometry result = LineMerger.MergeGeometry(source, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            AssertClosed(result.LineString.Coordinates);
        });
    }

    [Test]
    public void InteriorCrossingsAreNotImplicitlyNoded()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 1, 2 1), (1 0, 1 2))");

        Geometry result = LineMerger.MergeGeometry(source, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(result.MultiLineString.LineStrings[0].Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(result.MultiLineString.LineStrings[1].Coordinates,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void NestedCollectionsMergeLineworkAndIgnoreNonLineComponents()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION (POINT (9 9), " +
            "LINESTRING (0 0, 1 0), " +
            "MULTILINESTRING ((2 0, 3 0), (1 0, 2 0)))");

        Geometry result = LineMerger.MergeGeometry(source, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(result.LineString!.Coordinates,
                Has.Length.EqualTo(4));
            AssertCoordinate(result.LineString.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(result.LineString.Coordinates[3], 3.0, 0.0);
        });
    }

    [Test]
    public void IsolatedRepeatedAndEmptyInputsHaveDefinedResults()
    {
        Geometry isolated = Read("LINESTRING (0 0, 1 0, 2 0)");
        Geometry repeated = Read("LINESTRING (3 4, 3 4)");
        Geometry empty = Read(
            "GEOMETRYCOLLECTION (POINT (1 2), LINESTRING EMPTY)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 1 0, 1 1, 0 1, 0 0))");

        Geometry isolatedResult = LineMerger.MergeGeometry(isolated, false);
        Geometry repeatedResult = LineMerger.MergeGeometry(repeated, false);
        Geometry emptyResult = LineMerger.MergeGeometry(empty, false);
        Geometry polygonResult = LineMerger.MergeGeometry(polygon, false);

        Assert.Multiple(() =>
        {
            Assert.That(isolatedResult.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            Assert.That(repeatedResult.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(GeometryInspection.IsEmptyValue(emptyResult), Is.True);
            Assert.That(emptyResult.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(GeometryInspection.IsEmptyValue(polygonResult), Is.True);
        });
    }

    [Test]
    public void ExtendedGeometryInputIsRejected()
    {
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Throws<InvalidGeometryException>(() =>
            LineMerger.MergeGeometry(triangle, false));
    }

    private static void AssertClosed(Coordinate[] coordinates)
    {
        Assert.That(coordinates[0].X,
            Is.EqualTo(coordinates[coordinates.Length - 1].X));
        Assert.That(coordinates[0].Y,
            Is.EqualTo(coordinates[coordinates.Length - 1].Y));
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
