using geo;

namespace geo.Tests;

public class GeometrySnapNodeSharedPathTests
{
    [Test]
    public void GeometrySnapInsertsNearbyTargetVerticesWithSourceOrdinates()
    {
        Geometry source = Read(
            "LINESTRING ZM (0 0 0 10, 10 0 20 30)");

        Geometry result = GeometrySnapper.SnapGeometry(
            source, Read("POINT (5 0.1)"), 0.2);
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(coordinates, Has.Length.EqualTo(3));
            AssertCoordinate(coordinates[1], 5.0, 0.1);
            Assert.That(coordinates[1].Z, Is.EqualTo(10.0));
            Assert.That(coordinates[1].M, Is.EqualTo(20.0));
            AssertCoordinate(
                source.LineString!.Coordinates[1], 10.0, 0.0);
        });
    }

    [Test]
    public void GeometrySnapMovesPointsAndPreservesPolygonStructure()
    {
        Geometry point = GeometrySnapper.SnapGeometry(
            Read("POINT (0.1 0.1)"),
            Read("LINESTRING (0 0, 2 2)"), 0.2);
        Geometry polygon = GeometrySnapper.SnapGeometry(
            Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))"),
            Read("POINT (2 0.1)"), 0.2);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point.Point!.Coordinate!, 0.0, 0.0);
            Assert.That(polygon.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(polygon.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(6));
            AssertCoordinate(
                polygon.Polygon.Shell.Coordinates[1], 2.0, 0.1);
            Assert.That(polygon.Polygon.Holes, Is.Empty);
        });
    }

    [Test]
    public void GeometrySnapReturnsOwningNoOpCopyAndValidatesTolerance()
    {
        Geometry source = Read("LINESTRING (0 0, 10 0)");
        Geometry expected = GeometryCopy.CopyGeometry(source);

        Geometry result = GeometrySnapper.SnapGeometry(
            source, Read("POINT (5 1)"), 0.5);
        source.LineString!.Coordinates[0].X = 50.0;

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                result, expected), Is.True);
            Assert.That(result, Is.Not.SameAs(source));
            Assert.Throws<PrecisionException>(() =>
                GeometrySnapper.SnapGeometry(
                    expected, Read("POINT (0 0)"), -0.1));
            Assert.Throws<PrecisionException>(() =>
                GeometrySnapper.SnapGeometry(
                    expected, Read("POINT (0 0)"), double.NaN));
        });
    }

    [Test]
    public void UnaryNodingSplitsSelfAndComponentIntersections()
    {
        Geometry bowTie = GeometryNoder.NodeGeometry(
            Read("LINESTRING (0 0, 2 2, 0 2, 2 0)"));
        Geometry components = GeometryNoder.NodeGeometry(
            Read("MULTILINESTRING ((0 0, 4 0), (2 -2, 2 2))"));

        Assert.Multiple(() =>
        {
            Assert.That(GetLines(bowTie), Has.Length.EqualTo(3));
            Assert.That(GetLines(components), Has.Length.EqualTo(4));
            Assert.That(FullyNodedValidator.ValidateOutput(
                CreateEdges(components)).IsValid, Is.True);
        });
    }

    [Test]
    public void MutualNodingReturnsBothInputsWithLocalMetadataAndOrdinates()
    {
        Geometry[] result = GeometryNoder.NodeMutualGeometries(
            Read("LINESTRING Z (0 0 0, 10 10 10)"),
            Read("LINESTRING Z (0 10 100, 10 0 200)"));
        LineString[] first = GetLines(result[0]);
        LineString[] second = GetLines(result[1]);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(2));
            Assert.That(second, Has.Length.EqualTo(2));
            Assert.That(first[0].Coordinates[1].Z,
                Is.EqualTo(5.0));
            Assert.That(second[0].Coordinates[1].Z,
                Is.EqualTo(150.0));
            Assert.That(result[0].SpatialReferenceId,
                Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
        });
    }

    [Test]
    public void SharedPathsClassifyForwardAndReverseSourceDirections()
    {
        Geometry result = SharedPathFinder.GetSharedPaths(
            Read("LINESTRING (0 0, 10 0)"),
            Read("MULTILINESTRING ((2 0, 5 0), (8 0, 5 0))"));
        Geometry[] groups = result.GeometryCollection!.Geometries;
        LineString[] forward = groups[0].MultiLineString!.LineStrings;
        LineString[] reverse = groups[1].MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(groups, Has.Length.EqualTo(2));
            Assert.That(forward, Has.Length.EqualTo(1));
            Assert.That(reverse, Has.Length.EqualTo(1));
            AssertLine(forward[0], [2.0, 5.0]);
            AssertLine(reverse[0], [5.0, 8.0]);
        });
    }

    [Test]
    public void SharedPathsMergeAdjacentFragmentsAndIgnorePointTouches()
    {
        Geometry merged = SharedPathFinder.GetSharedPaths(
            Read("LINESTRING Z (0 0 0, 10 0 10)"),
            Read("MULTILINESTRING ((2 0, 5 0), (5 0, 8 0))"));
        Geometry touch = SharedPathFinder.GetSharedPaths(
            Read("LINESTRING (0 0, 2 0)"),
            Read("LINESTRING (2 0, 2 2)"));
        Geometry[] mergedGroups = merged.GeometryCollection!.Geometries;
        Geometry[] touchGroups = touch.GeometryCollection!.Geometries;
        LineString mergedLine =
            mergedGroups[0].MultiLineString!.LineStrings[0];

        Assert.Multiple(() =>
        {
            AssertLine(mergedLine, [2.0, 5.0, 8.0]);
            Assert.That(mergedLine.Coordinates[0].Z,
                Is.EqualTo(2.0));
            Assert.That(mergedLine.Coordinates[2].Z,
                Is.EqualTo(8.0));
            Assert.That(mergedGroups[1].MultiLineString!.LineStrings,
                Is.Empty);
            Assert.That(touchGroups[0].MultiLineString!.LineStrings,
                Is.Empty);
            Assert.That(touchGroups[1].MultiLineString!.LineStrings,
                Is.Empty);
        });
    }

    [Test]
    public void GeometryOperationsValidateTypesAndSpatialReferences()
    {
        Geometry line = Read("LINESTRING (0 0, 2 0)");
        Geometry firstSrid = ReadWithSrid(
            "SRID=4326;LINESTRING (0 0, 2 0)");
        Geometry secondSrid = ReadWithSrid(
            "SRID=3857;LINESTRING (1 0, 3 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySnapper.SnapGeometry(
                    line,
                    Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))"), 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryNoder.NodeGeometry(
                    Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))")));
            Assert.Throws<InvalidGeometryException>(() =>
                SharedPathFinder.GetSharedPaths(
                    Read("POINT (0 0)"), line));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometrySnapper.SnapGeometry(
                    firstSrid, secondSrid, 0.1));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryNoder.NodeMutualGeometries(
                    firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                SharedPathFinder.GetSharedPaths(
                    firstSrid, secondSrid));
        });
    }

    private static SegmentString[] CreateEdges(Geometry geometry)
    {
        return GeometrySplitter.CreateSplitterEdges(
            new RelationGeometryParts(geometry));
    }

    private static LineString[] GetLines(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return [geometry.LineString!];
        }
        Assert.That(geometry.GeometryType,
            Is.EqualTo(GeometryType.MultiLineString));
        return geometry.MultiLineString!.LineStrings;
    }

    private static void AssertLine(
        LineString line, double[] expectedX)
    {
        Assert.That(line.Coordinates,
            Has.Length.EqualTo(expectedX.Length));
        int index = 0;
        while (index < line.Coordinates.Length)
        {
            Assert.That(line.Coordinates[index].X,
                Is.EqualTo(expectedX[index]));
            index++;
        }
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y)
    {
        Assert.That(coordinate.X, Is.EqualTo(x));
        Assert.That(coordinate.Y, Is.EqualTo(y));
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
