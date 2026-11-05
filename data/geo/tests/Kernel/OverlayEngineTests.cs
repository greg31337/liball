using geo;

namespace geo.Tests;

public class OverlayEngineTests
{
    [Test]
    public void AreaOperationsSelectAndAssembleExpectedRegions()
    {
        Geometry first = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        Geometry second = Read(
            "POLYGON ((2 0, 6 0, 6 4, 2 4, 2 0))");

        Geometry intersection = OverlayEngine.GetIntersection(first, second);
        Geometry union = OverlayEngine.GetUnion(first, second);
        Geometry difference = OverlayEngine.GetDifference(first, second);
        Geometry symmetricDifference =
            OverlayEngine.GetSymmetricDifference(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                intersection,
                Read("POLYGON ((2 0, 4 0, 4 4, 2 4, 2 0))")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                union,
                Read("POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0))")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                difference,
                Read("POLYGON ((0 0, 2 0, 2 4, 0 4, 0 0))")), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                intersection), Is.EqualTo(8.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                union), Is.EqualTo(24.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                difference), Is.EqualTo(8.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                symmetricDifference), Is.EqualTo(16.0));
        });
    }

    [Test]
    public void DifferenceAssignsContainedRingAsHole()
    {
        Geometry outer = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        Geometry inner = Read(
            "POLYGON ((3 3, 7 3, 7 7, 3 7, 3 3))");

        Geometry result = OverlayEngine.GetDifference(outer, inner);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(84.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result, Read(
                    "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (3 3, 3 7, 7 7, 7 3, 3 3))")),
                Is.True);
        });
    }

    [Test]
    public void TouchingAreaIntersectionsCollapseToLineOrPoint()
    {
        Geometry first = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry edgeTouch = Read(
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0))");
        Geometry cornerTouch = Read(
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2))");

        Geometry lineResult = OverlayEngine.GetIntersection(first, edgeTouch);
        Geometry pointResult = OverlayEngine.GetIntersection(first, cornerTouch);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                lineResult, Read("LINESTRING (2 0, 2 2)")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                pointResult, Read("POINT (2 2)")), Is.True);
        });
    }

    [Test]
    public void LineOperationsUseNodedCrossingsAndCoincidentEdges()
    {
        Geometry rising = Read("LINESTRING (0 0, 4 4)");
        Geometry falling = Read("LINESTRING (0 4, 4 0)");
        Geometry firstOverlap = Read("LINESTRING (0 0, 4 0)");
        Geometry secondOverlap = Read("LINESTRING (2 0, 6 0)");

        Geometry crossingIntersection =
            OverlayEngine.GetIntersection(rising, falling);
        Geometry crossingUnion = OverlayEngine.GetUnion(rising, falling);
        Geometry overlapIntersection =
            OverlayEngine.GetIntersection(firstOverlap, secondOverlap);
        Geometry overlapDifference =
            OverlayEngine.GetDifference(firstOverlap, secondOverlap);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                crossingIntersection, Read("POINT (2 2)")), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetLength(crossingUnion),
                Is.EqualTo(2.0 * System.Math.Sqrt(32.0)).Within(1e-12));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                overlapIntersection, Read("LINESTRING (2 0, 4 0)")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                overlapDifference, Read("LINESTRING (0 0, 2 0)")), Is.True);
        });
    }

    [Test]
    public void PointOperationsDeduplicateAndSelectExplicitComponents()
    {
        Geometry first = Read("MULTIPOINT ((0 0), (1 1))");
        Geometry second = Read("MULTIPOINT ((1 1), (2 2))");

        Geometry intersection = OverlayEngine.GetIntersection(first, second);
        Geometry union = OverlayEngine.GetUnion(first, second);
        Geometry difference = OverlayEngine.GetDifference(first, second);
        Geometry symmetricDifference =
            OverlayEngine.GetSymmetricDifference(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                intersection, Read("POINT (1 1)")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                union, Read("MULTIPOINT ((0 0), (1 1), (2 2))")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                difference, Read("POINT (0 0)")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                symmetricDifference,
                Read("MULTIPOINT ((0 0), (2 2))")), Is.True);
        });
    }

    [Test]
    public void MixedDimensionOverlaySuppressesCoveredComponents()
    {
        Geometry line = Read("LINESTRING (-1 2, 5 2)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");

        Geometry intersection = OverlayEngine.GetIntersection(line, polygon);
        Geometry union = OverlayEngine.GetUnion(line, polygon);
        Geometry difference = OverlayEngine.GetDifference(line, polygon);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                intersection, Read("LINESTRING (0 2, 4 2)")), Is.True);
            Assert.That(union.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(union),
                Is.EqualTo(16.0));
            Assert.That(GeometryMeasureAlgorithms.GetLength(union),
                Is.EqualTo(18.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                difference,
                Read("MULTILINESTRING ((-1 2, 0 2), (4 2, 5 2))")), Is.True);
        });
    }

    [Test]
    public void ConstructedPointReconcilesMatchingZAndMOrdinates()
    {
        Geometry first = Read(
            "LINESTRING ZM (0 0 0 0, 4 4 4 8)");
        Geometry second = Read(
            "LINESTRING ZM (0 4 0 0, 4 0 4 8)");

        Geometry result = OverlayEngine.GetIntersection(first, second);
        Point point = (Point)GeometryDispatch.GetSelectedPayload(result);
        Coordinate coordinate = point.Coordinate!;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(coordinate.X, Is.EqualTo(2.0));
            Assert.That(coordinate.Y, Is.EqualTo(2.0));
            Assert.That(coordinate.Z, Is.EqualTo(2.0));
            Assert.That(coordinate.M, Is.EqualTo(4.0));
        });
    }

    [Test]
    public void OverlayRejectsMismatchedMetadataAndDeferredTypes()
    {
        Geometry xy = Read("POINT (0 0)");
        Geometry xyz = Read("POINT Z (0 0 1)");
        Geometry firstSrid = ReadWithSrid("SRID=4326;POINT (0 0)");
        Geometry secondSrid = ReadWithSrid("SRID=3857;POINT (0 0)");
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetUnion(xy, xyz));
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetIntersection(firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetDifference(triangle, xy));
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
