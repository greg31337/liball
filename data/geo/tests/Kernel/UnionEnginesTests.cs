using geo;

namespace geo.Tests;

public class UnionEnginesTests
{
    [Test]
    public void UnaryUnionDissolvesMixedComponentsAndRemovesDuplicates()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), POLYGON ((2 0, 6 0, 6 4, 2 4, 2 0)), LINESTRING (-1 2, 7 2), LINESTRING (7 2, -1 2), POINT (8 8), POINT (8 8), POINT (2 2))");

        Geometry result = UnaryUnionEngine.GetUnaryUnion(source);
        Geometry dissolved = UnaryUnionEngine.GetDissolvedGeometry(source);
        source.GeometryCollection!.Geometries[0].Polygon!
            .Shell.Coordinates[0].X = 100.0;

        Geometry expected = Read(
            "GEOMETRYCOLLECTION (LINESTRING (-1 2, 0 2), LINESTRING (6 2, 7 2), POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0)), POINT (8 8))");

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.GeometryCollection!.Geometries.Length,
                Is.EqualTo(4));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result, expected), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                result, dissolved), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(24.0));
            Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                Is.EqualTo(22.0));
            Assert.That(result.GeometryCollection.Geometries[3]
                .Polygon!.Shell.Coordinates[0].X, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void UnaryUnionMergesDuplicateAndAdjacentLinework()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 0 0), (2 0, 4 0))");

        Geometry result = UnaryUnionEngine.GetUnaryUnion(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result, Read("LINESTRING (0 0, 4 0)")), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                Is.EqualTo(4.0));
        });
    }

    [Test]
    public void CascadedPolygonUnionUsesBalancedDeterministicResults()
    {
        Geometry forward = Read(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 3 0, 3 2, 0 2, 0 0)), POLYGON ((2 0, 5 0, 5 2, 2 2, 2 0)), POLYGON ((4 0, 7 0, 7 2, 4 2, 4 0)))");
        Geometry reverse = Read(
            "GEOMETRYCOLLECTION (POLYGON ((4 0, 7 0, 7 2, 4 2, 4 0)), POLYGON ((2 0, 5 0, 5 2, 2 2, 2 0)), POLYGON ((0 0, 3 0, 3 2, 0 2, 0 0)))");

        Geometry forwardResult =
            CascadedUnionEngine.GetCascadedUnion(forward);
        Geometry reverseResult =
            CascadedUnionEngine.GetCascadedUnion(reverse);

        Assert.Multiple(() =>
        {
            Assert.That(forwardResult.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                forwardResult), Is.EqualTo(14.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                forwardResult,
                Read("POLYGON ((0 0, 7 0, 7 2, 0 2, 0 0))")), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                forwardResult, reverseResult), Is.True);
        });
    }

    [Test]
    public void CascadedUnionCanonicalizesSingletonAndHandlesEmptySurface()
    {
        Geometry singleton = Read(
            "POLYGON ((4 4, 4 0, 0 0, 0 4, 4 4))");
        Geometry empty = Read("MULTIPOLYGON EMPTY");

        Geometry singletonResult =
            CascadedUnionEngine.GetCascadedUnion(singleton);
        Geometry emptyResult =
            CascadedUnionEngine.GetCascadedUnion(empty);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                singletonResult,
                GeometryNormalization.NormalizeGeometryValue(singleton)),
                Is.True);
            Assert.That(emptyResult.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(emptyResult),
                Is.True);
            Assert.Throws<InvalidGeometryException>(() =>
                CascadedUnionEngine.GetCascadedUnion(
                    Read("GEOMETRYCOLLECTION (POLYGON EMPTY, POINT EMPTY)")));
            Assert.Throws<InvalidGeometryException>(() =>
                CascadedUnionEngine.GetCascadedUnion(
                    Read("LINESTRING (0 0, 1 1)")));
        });
    }

    [Test]
    public void DisjointSubsetUnionCombinesEnvelopeConnectedGroups()
    {
        Geometry first = Read(
            "MULTIPOLYGON (((0 0, 3 0, 3 2, 0 2, 0 0)), ((10 0, 12 0, 12 2, 10 2, 10 0)))");
        Geometry second = Read(
            "MULTIPOLYGON (((2 0, 5 0, 5 2, 2 2, 2 0)), ((11 0, 13 0, 13 2, 11 2, 11 0)))");

        Geometry result =
            DisjointSubsetUnionEngine.GetDisjointSubsetUnion(first, second);
        Geometry reversed =
            DisjointSubsetUnionEngine.GetDisjointSubsetUnion(second, first);
        Geometry ordinary = OverlayEngine.GetUnion(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons.Length, Is.EqualTo(2));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result, ordinary), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                result, reversed), Is.True);
        });
    }

    [Test]
    public void DisjointSubsetUnionSupportsMixedDimensionsAndEmptyInputs()
    {
        Geometry first = Read(
            "GEOMETRYCOLLECTION (POINT (10 10), LINESTRING (0 0, 2 0))");
        Geometry second = Read(
            "GEOMETRYCOLLECTION (POINT (10 10), POLYGON ((20 20, 22 20, 22 22, 20 22, 20 20)))");
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");

        Geometry result =
            DisjointSubsetUnionEngine.GetDisjointSubsetUnion(first, second);
        Geometry emptyResult =
            DisjointSubsetUnionEngine.GetDisjointSubsetUnion(empty, empty);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.GeometryCollection!.Geometries.Length,
                Is.EqualTo(3));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result,
                Read("GEOMETRYCOLLECTION (POINT (10 10), LINESTRING (0 0, 2 0), POLYGON ((20 20, 22 20, 22 22, 20 22, 20 20)))")),
                Is.True);
            Assert.That(emptyResult.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(GeometryInspection.IsEmptyValue(emptyResult),
                Is.True);
        });
    }

    [Test]
    public void UnionEnginesValidateCoreTypesAndSharedMetadata()
    {
        Geometry firstSrid = ReadWithSrid(
            "SRID=4326;POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry secondSrid = ReadWithSrid(
            "SRID=3857;POLYGON ((4 0, 6 0, 6 2, 4 2, 4 0))");
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                UnaryUnionEngine.GetUnaryUnion(triangle));
            Assert.Throws<InvalidGeometryException>(() =>
                DisjointSubsetUnionEngine.GetDisjointSubsetUnion(
                    firstSrid, secondSrid));
        });
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
