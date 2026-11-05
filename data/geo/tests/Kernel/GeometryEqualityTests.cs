using geo;

namespace geo.Tests;

public class GeometryEqualityTests
{
    [Test]
    public void ExactEqualityIncludesStructureMetadataOrderAndExtraOrdinates()
    {
        Geometry first = Read(
            "SRID=4326;GEOMETRYCOLLECTION Z (POINT Z (1 2 3), LINESTRING Z (0 0 4, 2 0 5))");
        Geometry same = Read(
            "SRID=4326;GEOMETRYCOLLECTION Z (POINT Z (1 2 3), LINESTRING Z (0 0 4, 2 0 5))");
        Geometry reordered = Read(
            "SRID=4326;GEOMETRYCOLLECTION Z (LINESTRING Z (0 0 4, 2 0 5), POINT Z (1 2 3))");
        Geometry changedZ = Read(
            "SRID=4326;GEOMETRYCOLLECTION Z (POINT Z (1 2 4), LINESTRING Z (0 0 4, 2 0 5))");
        Geometry unknownSrid = ReadWithoutSrid(
            "GEOMETRYCOLLECTION Z (POINT Z (1 2 3), LINESTRING Z (0 0 4, 2 0 5))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(first, same), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(first, reordered), Is.False);
            Assert.That(GeometryEquality.AreExactlyEqual(first, changedZ), Is.False);
            Assert.That(GeometryEquality.AreExactlyEqual(first, unknownSrid), Is.False);
            Assert.That(GeometryEquality.AreExactlyEqual(
                ReadWithoutSrid("POINT EMPTY"),
                ReadWithoutSrid("POLYGON EMPTY")), Is.False);
        });
    }

    [Test]
    public void ToleranceEqualityAppliesToEveryPresentOrdinateAtTheBoundary()
    {
        Geometry first = ReadWithoutSrid("POINT ZM (1 2 3 4)");
        Geometry atTolerance = ReadWithoutSrid(
            "POINT ZM (1.1 1.9 3.1 3.9)");
        Geometry outsideTolerance = ReadWithoutSrid(
            "POINT ZM (1.1000000001 2 3 4)");
        Geometry differentLayout = ReadWithoutSrid("POINT Z (1 2 3)");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreEqualWithinTolerance(
                first, atTolerance, 0.1), Is.True);
            Assert.That(GeometryEquality.AreEqualWithinTolerance(
                first, outsideTolerance, 0.1), Is.False);
            Assert.That(GeometryEquality.AreEqualWithinTolerance(
                first, differentLayout, 1.0), Is.False);
            Assert.That(GeometryEquality.AreEqualWithinTolerance(
                ReadWithoutSrid("POINT EMPTY"),
                ReadWithoutSrid("POINT EMPTY"), 0.0), Is.True);
        });
    }

    [Test]
    public void NormalizedEqualityIgnoresDirectionRingStartsHoleAndComponentOrder()
    {
        Geometry first = ReadWithoutSrid(
            "MULTIPOLYGON (((0 0, 6 0, 6 6, 0 6, 0 0), (1 1, 2 1, 2 2, 1 2, 1 1), (4 4, 5 4, 5 5, 4 5, 4 4)), ((10 0, 12 0, 12 2, 10 2, 10 0)))");
        Geometry permuted = ReadWithoutSrid(
            "MULTIPOLYGON (((12 2, 12 0, 10 0, 10 2, 12 2)), ((6 6, 6 0, 0 0, 0 6, 6 6), (5 5, 5 4, 4 4, 4 5, 5 5), (2 2, 2 1, 1 1, 1 2, 2 2)))");
        string originalText = Write(first);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(first, permuted), Is.False);
            Assert.That(GeometryEquality.AreNormalizedEqual(first, permuted), Is.True);
            Assert.That(Write(first), Is.EqualTo(originalText));
            Assert.That(GeometryEquality.AreNormalizedEqual(
                ReadWithoutSrid("LINESTRING (0 0, 1 1, 2 0)"),
                ReadWithoutSrid("LINESTRING (2 0, 1 1, 0 0)")), Is.True);
        });
    }

    [Test]
    public void NormalizedEqualityUsesCanonicalNarrowestCollectionTyping()
    {
        Geometry collection = ReadWithoutSrid(
            "GEOMETRYCOLLECTION (POINT (2 0), POINT (0 0))");
        Geometry multiPoint = ReadWithoutSrid(
            "MULTIPOINT ((0 0), (2 0))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                collection, multiPoint), Is.False);
            Assert.That(GeometryEquality.AreNormalizedEqual(
                collection, multiPoint), Is.True);
        });
    }

    [Test]
    public void TopologicalEqualityUsesOnlyThePlanarPointSet()
    {
        Geometry simpleLine = ReadWithoutSrid(
            "LINESTRING Z (0 0 1, 2 0 2)");
        Geometry densifiedReverse = ReadWithoutSrid(
            "LINESTRING Z (2 0 20, 1 0 10, 0 0 0)");
        Geometry point = ReadWithoutSrid("POINT (1 1)");
        Geometry repeatedPoint = ReadWithoutSrid(
            "MULTIPOINT ((1 1), (1 1))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                simpleLine, densifiedReverse), Is.False);
            Assert.That(GeometryEquality.AreNormalizedEqual(
                simpleLine, densifiedReverse), Is.False);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                simpleLine, densifiedReverse), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                point, repeatedPoint), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                point, ReadWithoutSrid("POINT (2 2)")), Is.False);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                point, ReadWithoutSrid("LINESTRING (0 1, 2 1)")), Is.False);
        });
    }

    [Test]
    public void TopologicalEqualityTreatsAllCoreEmptyPointSetsAsEqual()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                ReadWithoutSrid("POINT EMPTY"),
                ReadWithoutSrid("POLYGON EMPTY")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                ReadWithoutSrid("GEOMETRYCOLLECTION EMPTY"),
                ReadWithoutSrid("MULTILINESTRING EMPTY")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                ReadWithoutSrid("POINT EMPTY"),
                ReadWithoutSrid("POINT (0 0)")), Is.False);
        });
    }

    [Test]
    public void StructuralEqualitySupportsSurfaceAndCurveExtensionPayloads()
    {
        Geometry triangle = ReadWithoutSrid(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        Geometry triangleCopy = ReadWithoutSrid(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        Geometry curve = ReadWithoutSrid(
            "CIRCULARSTRING (0 0, 1 1, 2 0)");
        Geometry curveNear = ReadWithoutSrid(
            "CIRCULARSTRING (0 0, 1.01 1, 2 0)");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                triangle, triangleCopy), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                curve, curveNear), Is.False);
            Assert.That(GeometryEquality.AreEqualWithinTolerance(
                curve, curveNear, 0.01), Is.True);
        });
    }

    [Test]
    public void InvalidToleranceInputsAndKnownSridConflictsAreRejected()
    {
        Geometry first = Read("SRID=4326;POINT (0 0)");
        Geometry second = Read("SRID=3857;POINT (0 0)");
        Geometry point = ReadWithoutSrid("POINT (0 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreExactlyEqual(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreNormalizedEqual(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreTopologicallyEqual(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreEqualWithinTolerance(point, point, -0.1));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreEqualWithinTolerance(
                    point, point, double.PositiveInfinity));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreEqualWithinTolerance(
                    point, point, double.NaN));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryEquality.AreExactlyEqual(null!, point));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadWithoutSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static string Write(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.WriteWkt(geometry);
    }
}
