using geo;

namespace geo.Tests;

public class GeometrySplitterTests
{
    [Test]
    public void PointSplittingInterpolatesSourceZAndMOrdinates()
    {
        Geometry source = Read(
            "LINESTRING ZM (0 0 0 10, 10 0 20 30)");

        Geometry result = GeometrySplitter.SplitGeometry(
            source, Read("POINT (2.5 0)"));
        Geometry[] pieces = GetPieces(result);

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Has.Length.EqualTo(2));
            AssertLine(pieces[0], [0.0, 2.5]);
            AssertLine(pieces[1], [2.5, 10.0]);
            Assert.That(pieces[0].CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(pieces[0].LineString!.Coordinates[1].Z,
                Is.EqualTo(5.0));
            Assert.That(pieces[0].LineString!.Coordinates[1].M,
                Is.EqualTo(15.0));
            Assert.That(source.LineString!.Coordinates[0].X,
                Is.EqualTo(0.0));
        });
    }

    [Test]
    public void MultiPointSplittingSortsCutsAlongTheSourceAndDeduplicatesThem()
    {
        Geometry result = GeometrySplitter.SplitGeometry(
            Read("LINESTRING (0 0, 2 0, 8 0, 10 0)"),
            Read("MULTIPOINT ((7 0), (3 0), (7 0))"));
        Geometry[] pieces = GetPieces(result);

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Has.Length.EqualTo(3));
            AssertLine(pieces[0], [0.0, 2.0, 3.0]);
            AssertLine(pieces[1], [3.0, 7.0]);
            AssertLine(pieces[2], [7.0, 8.0, 10.0]);
        });
    }

    [Test]
    public void LineSplittingHandlesCrossingsAndCollinearOverlapEnds()
    {
        Geometry crossing = GeometrySplitter.SplitGeometry(
            Read("LINESTRING (0 0, 10 0)"),
            Read("MULTILINESTRING ((2 -1, 2 1), (8 -1, 8 1))"));
        Geometry overlap = GeometrySplitter.SplitGeometry(
            Read("LINESTRING (0 0, 10 0)"),
            Read("LINESTRING (3 0, 7 0)"));

        Assert.Multiple(() =>
        {
            Assert.That(GetPieces(crossing), Has.Length.EqualTo(3));
            AssertLine(GetPieces(crossing)[0], [0.0, 2.0]);
            AssertLine(GetPieces(crossing)[1], [2.0, 8.0]);
            AssertLine(GetPieces(crossing)[2], [8.0, 10.0]);
            Assert.That(GetPieces(overlap), Has.Length.EqualTo(3));
            AssertLine(GetPieces(overlap)[0], [0.0, 3.0]);
            AssertLine(GetPieces(overlap)[1], [3.0, 7.0]);
            AssertLine(GetPieces(overlap)[2], [7.0, 10.0]);
        });
    }

    [Test]
    public void PolygonSplittingUsesShellAndHoleBoundaries()
    {
        Geometry result = GeometrySplitter.SplitGeometry(
            Read("LINESTRING (-1 5, 11 5)"),
            Read("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (4 4, 6 4, 6 6, 4 6, 4 4))"));
        Geometry[] pieces = GetPieces(result);

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Has.Length.EqualTo(5));
            AssertLine(pieces[0], [-1.0, 0.0]);
            AssertLine(pieces[1], [0.0, 4.0]);
            AssertLine(pieces[2], [4.0, 6.0]);
            AssertLine(pieces[3], [6.0, 10.0]);
            AssertLine(pieces[4], [10.0, 11.0]);
        });
    }

    [Test]
    public void MultiLineSplittingPreservesComponentOrderAndDirection()
    {
        Geometry result = GeometrySplitter.SplitGeometry(
            Read("MULTILINESTRING ((10 0, 0 0), (0 2, 10 2))"),
            Read("LINESTRING (3 -1, 3 3)"));
        Geometry[] pieces = GetPieces(result);

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Has.Length.EqualTo(4));
            AssertLine(pieces[0], [10.0, 3.0]);
            AssertLine(pieces[1], [3.0, 0.0]);
            AssertLine(pieces[2], [0.0, 3.0]);
            AssertLine(pieces[3], [3.0, 10.0]);
            Assert.That(pieces[0].LineString!.Coordinates[0].Y,
                Is.EqualTo(0.0));
            Assert.That(pieces[2].LineString!.Coordinates[0].Y,
                Is.EqualTo(2.0));
        });
    }

    [Test]
    public void NoSplitReturnsOneOwningCopyOfTheCompleteInput()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 5 0), (0 2, 5 2))");
        Geometry expected = GeometryCopy.CopyGeometry(source);

        Geometry offLine = GeometrySplitter.SplitGeometry(
            source, Read("POINT (2 0.000001)"));
        Geometry endpoint = GeometrySplitter.SplitGeometry(
            source, Read("POINT (0 0)"));
        Geometry empty = GeometrySplitter.SplitGeometry(
            source, Read("MULTIPOINT EMPTY"));
        source.MultiLineString!.LineStrings[0].Coordinates[0].X = 99.0;

        Assert.Multiple(() =>
        {
            AssertUnsplit(offLine, expected);
            AssertUnsplit(endpoint, expected);
            AssertUnsplit(empty, expected);
        });
    }

    [Test]
    public void SplittingValidatesSourceBladeAndSpatialReferenceMetadata()
    {
        Geometry line = Read("LINESTRING (0 0, 2 0)");
        Geometry firstSrid = ReadWithSrid(
            "SRID=4326;LINESTRING (0 0, 2 0)");
        Geometry secondSrid = ReadWithSrid(
            "SRID=3857;POINT (1 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySplitter.SplitGeometry(
                    Read("POINT (0 0)"), Read("POINT (0 0)")));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySplitter.SplitGeometry(
                    Read("POLYGON ((0 0, 2 0, 2 2, 0 0))"),
                    Read("LINESTRING (1 -1, 1 3)")));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySplitter.SplitGeometry(
                    line,
                    Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))")));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySplitter.SplitGeometry(firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySplitter.SplitGeometry(null!, line));
        });
    }

    private static Geometry[] GetPieces(Geometry result)
    {
        Assert.That(result.GeometryType,
            Is.EqualTo(GeometryType.GeometryCollection));
        return result.GeometryCollection!.Geometries;
    }

    private static void AssertUnsplit(
        Geometry result, Geometry expected)
    {
        Geometry[] pieces = GetPieces(result);
        Assert.That(pieces, Has.Length.EqualTo(1));
        Assert.That(GeometryEquality.AreExactlyEqual(
            pieces[0], expected), Is.True);
        Assert.That(pieces[0], Is.Not.SameAs(expected));
    }

    private static void AssertLine(
        Geometry geometry, double[] expectedX)
    {
        Assert.That(geometry.GeometryType,
            Is.EqualTo(GeometryType.LineString));
        Coordinate[] coordinates = geometry.LineString!.Coordinates;
        Assert.That(coordinates, Has.Length.EqualTo(expectedX.Length));
        int index = 0;
        while (index < coordinates.Length)
        {
            Assert.That(coordinates[index].X,
                Is.EqualTo(expectedX[index]));
            index++;
        }
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
