using geo;

namespace geo.Tests;

public class AssemblyComprehensiveTests
{
    [Test]
    public void SameDirectionDuplicatesRemainDistinctForDirectedMerge()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0), (0 0, 2 0))");

        Geometry directed = LineMerger.MergeGeometry(source, true);
        Geometry undirected = LineMerger.MergeGeometry(source, false);

        Assert.Multiple(() =>
        {
            Assert.That(directed.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(directed.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(undirected.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(undirected.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            AssertClosed(undirected.LineString.Coordinates);
        });
    }

    [Test]
    public void DirectedBranchEmitsEveryEdgeExactlyOnce()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0), " +
            "(1 0, 1 1), (2 0, 3 0))");

        Geometry result = LineMerger.MergeGeometry(source, true);
        LineString[] lines = result.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(3));
            Assert.That(TotalSegmentCount(lines), Is.EqualTo(4));
            Assert.That(lines[0].Coordinates, Has.Length.EqualTo(2));
            Assert.That(lines[1].Coordinates, Has.Length.EqualTo(2));
            Assert.That(lines[2].Coordinates, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void UndirectedMergeIsDeterministicAcrossInputOrder()
    {
        Geometry first = Read(
            "MULTILINESTRING ((2 0, 3 0), (1 0, 2 0), " +
            "(1 0, 1 1), (0 0, 1 0))");
        Geometry second = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 1 1), " +
            "(1 0, 2 0), (2 0, 3 0))");

        Geometry firstResult = LineMerger.MergeGeometry(first, false);
        Geometry secondResult = LineMerger.MergeGeometry(second, false);

        Assert.That(GeometryEquality.AreExactlyEqual(
            firstResult, secondResult), Is.True);
    }

    [Test]
    public void DiagonalSplitsAClosedRingIntoTwoFaces()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 2 2), " +
            "(2 2, 0 2), (0 2, 0 0), (0 0, 2 2))");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygons.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.Polygons.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(new GeometryMeasures().Area(result.Polygons),
                Is.EqualTo(4.0));
            Assert.That(GeometryInspection.IsEmptyValue(result.Dangles),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(result.CutEdges),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(
                result.InvalidRings), Is.True);
        });
    }

    [Test]
    public void EveryEdgeInAConnectorChainIsACutEdge()
    {
        Geometry source = Read(
            "MULTILINESTRING (" +
            "(0 0, 1 0), (1 0, 1 1), (1 1, 0 1), (0 1, 0 0), " +
            "(4 0, 5 0), (5 0, 5 1), (5 1, 4 1), (4 1, 4 0), " +
            "(1 0, 2 0), (2 0, 4 0))");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygons.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(result.CutEdges.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(GeometryInspection.IsEmptyValue(result.Dangles),
                Is.True);
        });
    }

    [Test]
    public void NonzeroAreaSelfCrossingRingIsInvalid()
    {
        Geometry source = Read(
            "LINESTRING (0 0, 4 0, 1 3, 3 3, 0 0)");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(RingMath.GetExactSignedAreaSign(
                source.LineString!.Coordinates), Is.Not.EqualTo(0));
            Assert.That(GeometryInspection.IsEmptyValue(result.Polygons),
                Is.True);
            Assert.That(result.InvalidRings.MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void DuplicateClosedLineworkIsReportedRatherThanPolygonized()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0, 2 2, 0 2, 0 0), " +
            "(0 0, 2 0, 2 2, 0 2, 0 0))");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryInspection.IsEmptyValue(result.Polygons),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(result.Dangles),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(result.CutEdges),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(
                result.InvalidRings), Is.False);
        });
    }

    [Test]
    public void ThreeNestedRingsProduceFacesAndOddEvenArea()
    {
        Geometry source = Read(
            "MULTILINESTRING (" +
            "(0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(2 2, 8 2, 8 8, 2 8, 2 2), " +
            "(4 4, 6 4, 6 6, 4 6, 4 4))");

        Geometry faces = Polygonizer.PolygonizeGeometry(source);
        Geometry area = AreaBuilder.BuildGeometry(source);
        int faceHoleCount = CountPolygonHoles(faces);

        Assert.Multiple(() =>
        {
            Assert.That(faces.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(faces.MultiPolygon!.Polygons,
                Has.Length.EqualTo(3));
            Assert.That(faceHoleCount, Is.EqualTo(2));
            Assert.That(new GeometryMeasures().Area(faces),
                Is.EqualTo(100.0));
            Assert.That(area.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(area.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(new GeometryMeasures().Area(area),
                Is.EqualTo(68.0));
        });
    }

    [Test]
    public void PolygonizationIsDeterministicAcrossLineOrder()
    {
        Geometry first = ReadWithSrid(
            "SRID=3857;MULTILINESTRING Z (" +
            "(0 0 5, 2 0 5), (2 0 5, 2 2 5), " +
            "(2 2 5, 0 2 5), (0 2 5, 0 0 5), " +
            "(0 0 5, 2 2 5))");
        Geometry second = ReadWithSrid(
            "SRID=3857;MULTILINESTRING Z (" +
            "(2 2 5, 0 2 5), (0 0 5, 2 2 5), " +
            "(0 2 5, 0 0 5), (2 0 5, 2 2 5), " +
            "(0 0 5, 2 0 5))");

        Geometry firstResult = Polygonizer.PolygonizeFullGeometry(first);
        Geometry secondResult = Polygonizer.PolygonizeFullGeometry(second);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                firstResult, secondResult), Is.True);
            Assert.That(firstResult.SpatialReferenceId,
                Is.EqualTo(3857));
            Assert.That(firstResult.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
        });
    }

    [Test]
    public void InvalidAndOpenLineworkBuildsCanonicalEmptyArea()
    {
        Geometry invalid = Read(
            "LINESTRING (0 0, 4 0, 1 3, 3 3, 0 0)");
        Geometry open = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0))");

        Geometry invalidArea = AreaBuilder.BuildGeometry(invalid);
        Geometry openArea = AreaBuilder.BuildGeometry(open);

        Assert.Multiple(() =>
        {
            Assert.That(invalidArea.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(openArea.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(invalidArea),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(openArea),
                Is.True);
        });
    }

    private static int TotalSegmentCount(LineString[] lines)
    {
        int count = 0;
        int index = 0;
        while (index < lines.Length)
        {
            count += lines[index].Coordinates.Length - 1;
            index++;
        }
        return count;
    }

    private static int CountPolygonHoles(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            return geometry.Polygon!.Holes.Length;
        }
        int count = 0;
        int index = 0;
        while (index < geometry.MultiPolygon!.Polygons.Length)
        {
            count += geometry.MultiPolygon.Polygons[index].Holes.Length;
            index++;
        }
        return count;
    }

    private static void AssertClosed(Coordinate[] coordinates)
    {
        Assert.That(coordinates[0].X,
            Is.EqualTo(coordinates[coordinates.Length - 1].X));
        Assert.That(coordinates[0].Y,
            Is.EqualTo(coordinates[coordinates.Length - 1].Y));
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
