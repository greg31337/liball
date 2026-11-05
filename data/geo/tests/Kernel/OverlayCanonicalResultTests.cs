using geo;

namespace geo.Tests;

public class OverlayCanonicalResultTests
{
    [Test]
    public void DegreeTwoLineFragmentsMergeIntoCanonicalMaximalLines()
    {
        Geometry first = Read("LINESTRING (0 0, 2 0)");
        Geometry second = Read("LINESTRING (2 0, 4 0)");
        Geometry reversedFirst = Read("LINESTRING (2 0, 0 0)");
        Geometry reversedSecond = Read("LINESTRING (4 0, 2 0)");

        Geometry result = OverlayEngine.GetUnion(first, second);
        Geometry reversedResult = OverlayEngine.GetUnion(
            reversedSecond, reversedFirst);

        Assert.Multiple(() =>
        {
            AssertCanonicalEqual(
                result, Read("LINESTRING (0 0, 2 0, 4 0)"));
            AssertCanonicalEqual(reversedResult, result);
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.LineString!.Coordinates.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void BranchNodesRemainSeparateCanonicalLineComponents()
    {
        Geometry horizontal = Read("LINESTRING (-2 0, 2 0)");
        Geometry vertical = Read("LINESTRING (0 -2, 0 2)");

        Geometry result = OverlayEngine.GetUnion(horizontal, vertical);
        MultiLineString lines = (MultiLineString)
            GeometryDispatch.GetSelectedPayload(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lines.LineStrings.Length, Is.EqualTo(4));
            Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                Is.EqualTo(8.0));
        });
    }

    [Test]
    public void EmptyResultsUseTheOperationResultDimension()
    {
        Geometry point = Read("POINT (20 20)");
        Geometry line = Read("LINESTRING (0 0, 4 0)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");

        Geometry pointIntersection =
            OverlayEngine.GetIntersection(point, line);
        Geometry lineIntersection = OverlayEngine.GetIntersection(
            line, Read("POLYGON ((0 10, 4 10, 4 14, 0 14, 0 10))"));
        Geometry areaIntersection = OverlayEngine.GetIntersection(
            polygon,
            Read("POLYGON ((10 10, 14 10, 14 14, 10 14, 10 10))"));
        Geometry pointDifference = OverlayEngine.GetDifference(
            Read("POINT (1 1)"), Read("POINT (1 1)"));
        Geometry emptyLineUnion = OverlayEngine.GetUnion(
            Read("LINESTRING EMPTY"), Read("LINESTRING EMPTY"));
        Geometry emptyAreaSymmetricDifference =
            OverlayEngine.GetSymmetricDifference(
                Read("POLYGON EMPTY"), Read("POLYGON EMPTY"));

        Assert.Multiple(() =>
        {
            AssertEmptyType(pointIntersection, GeometryType.Point);
            AssertEmptyType(lineIntersection, GeometryType.LineString);
            AssertEmptyType(areaIntersection, GeometryType.Polygon);
            AssertEmptyType(pointDifference, GeometryType.Point);
            AssertEmptyType(emptyLineUnion, GeometryType.LineString);
            AssertEmptyType(
                emptyAreaSymmetricDifference, GeometryType.Polygon);
        });
    }

    [Test]
    public void HomogeneousAndMixedComponentsUseTheNarrowestCoreType()
    {
        Geometry firstPolygon = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry secondPolygon = Read(
            "POLYGON ((4 0, 6 0, 6 2, 4 2, 4 0))");
        Geometry polygons = OverlayEngine.GetUnion(
            firstPolygon, secondPolygon);
        Geometry points = OverlayEngine.GetUnion(
            Read("POINT (3 4)"), Read("POINT (1 2)"));
        Geometry mixed = OverlayEngine.GetUnion(
            Read("POINT (3 4)"), Read("LINESTRING (0 0, 1 0)"));

        Assert.Multiple(() =>
        {
            Assert.That(polygons.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(polygons.MultiPolygon!.Polygons.Length, Is.EqualTo(2));
            AssertCanonicalEqual(
                points, Read("MULTIPOINT ((1 2), (3 4))"));
            Assert.That(mixed.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(mixed.GeometryCollection!.Geometries.Length,
                Is.EqualTo(2));
            Assert.That(mixed.GeometryCollection.Geometries[0].GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(mixed.GeometryCollection.Geometries[1].GeometryType,
                Is.EqualTo(GeometryType.LineString));
        });
    }

    [Test]
    public void TouchingAreaUnionsChoosePolygonOrMultiPolygonByConnectivity()
    {
        Geometry first = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry edgeTouch = Read(
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0))");
        Geometry cornerTouch = Read(
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2))");

        Geometry edgeUnion = OverlayEngine.GetUnion(first, edgeTouch);
        Geometry edgeSymmetricDifference =
            OverlayEngine.GetSymmetricDifference(first, edgeTouch);
        Geometry cornerUnion = OverlayEngine.GetUnion(first, cornerTouch);

        Assert.Multiple(() =>
        {
            Assert.That(edgeUnion.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                edgeUnion,
                Read("POLYGON ((0 0, 4 0, 4 2, 0 2, 0 0))")), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                edgeUnion, edgeSymmetricDifference), Is.True);
            Assert.That(cornerUnion.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(cornerUnion.MultiPolygon!.Polygons.Length,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void OverlappingSurfaceComponentsInCollectionsAreDissolved()
    {
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), POLYGON ((2 0, 6 0, 6 4, 2 4, 2 0)))");
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");

        Geometry union = OverlayEngine.GetUnion(collection, empty);
        Geometry difference = OverlayEngine.GetDifference(collection, empty);

        Assert.Multiple(() =>
        {
            Assert.That(union.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                union,
                Read("POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0))")), Is.True);
            AssertCanonicalEqual(difference, union);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(union),
                Is.EqualTo(24.0));
        });
    }

    [Test]
    public void CoveredCollectionComponentsAreSuppressedByHigherDimensions()
    {
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POINT (2 2), LINESTRING (-1 2, 5 2), POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)))");
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");

        Geometry result = OverlayEngine.GetUnion(collection, empty);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.GeometryCollection!.Geometries.Length,
                Is.EqualTo(3));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0));
            Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                Is.EqualTo(18.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result, Read(
                    "GEOMETRYCOLLECTION (LINESTRING (-1 2, 0 2), LINESTRING (4 2, 5 2), POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)))")),
                Is.True);
        });
    }

    [Test]
    public void CanonicalCommutativeResultsAreExactlyStableByInputOrder()
    {
        Geometry first = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        Geometry second = Read(
            "POLYGON ((2 -1, 6 -1, 6 3, 2 3, 2 -1))");

        Geometry firstUnion = OverlayEngine.GetUnion(first, second);
        Geometry secondUnion = OverlayEngine.GetUnion(second, first);
        Geometry firstIntersection =
            OverlayEngine.GetIntersection(first, second);
        Geometry secondIntersection =
            OverlayEngine.GetIntersection(second, first);
        Geometry firstSymmetricDifference =
            OverlayEngine.GetSymmetricDifference(first, second);
        Geometry secondSymmetricDifference =
            OverlayEngine.GetSymmetricDifference(second, first);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                firstUnion, secondUnion), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                firstIntersection, secondIntersection), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                firstSymmetricDifference,
                secondSymmetricDifference), Is.True);
        });
    }

    [Test]
    public void EmptyOutputPreservesLayoutAndSpatialReferenceMetadata()
    {
        Geometry first = ReadWithSrid("SRID=4979;POINT Z (0 0 1)");
        Geometry second = ReadWithSrid(
            "SRID=4979;POLYGON Z ((10 10 2, 12 10 2, 12 12 2, 10 12 2, 10 10 2))");

        Geometry result = OverlayEngine.GetIntersection(first, second);

        Assert.Multiple(() =>
        {
            AssertEmptyType(result, GeometryType.Point);
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4979));
        });
    }

    private static void AssertEmptyType(
        Geometry geometry, GeometryType geometryType)
    {
        Assert.That(geometry.GeometryType, Is.EqualTo(geometryType));
        Assert.That(GeometryInspection.IsEmptyValue(geometry), Is.True);
    }

    private static void AssertCanonicalEqual(
        Geometry actual, Geometry expected)
    {
        Geometry normalizedExpected =
            GeometryNormalization.NormalizeGeometryValue(expected);
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        Assert.That(GeometryEquality.AreExactlyEqual(
            actual, normalizedExpected), Is.True,
            codec.WriteWkt(actual) + " != " +
            codec.WriteWkt(normalizedExpected));
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
