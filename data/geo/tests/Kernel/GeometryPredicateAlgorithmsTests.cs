using geo;

namespace geo.Tests;

public class GeometryPredicateAlgorithmsTests
{
    [Test]
    public void DisjointAndIntersectsAreComplementsIncludingEmptyValues()
    {
        Geometry point = ReadWithoutSrid("POINT (0 0)");
        Geometry samePoint = ReadWithoutSrid("POINT (0 0)");
        Geometry otherPoint = ReadWithoutSrid("POINT (1 1)");
        Geometry empty = ReadWithoutSrid("POLYGON EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Disjoint(
                point, otherPoint), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Intersects(
                point, otherPoint), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Disjoint(
                point, samePoint), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Intersects(
                point, samePoint), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Disjoint(
                empty, point), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Intersects(
                empty, point), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Disjoint(
                empty, ReadWithoutSrid("POINT EMPTY")), Is.True);
        });
    }

    [Test]
    public void TouchesRequiresBoundaryContactWithoutInteriorContact()
    {
        Geometry firstLine = ReadWithoutSrid(
            "LINESTRING (0 0, 1 0)");
        Geometry touchingLine = ReadWithoutSrid(
            "LINESTRING (1 0, 2 0)");
        Geometry crossingLine = ReadWithoutSrid(
            "LINESTRING (0.5 -1, 0.5 1)");
        Geometry firstPolygon = ReadWithoutSrid(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry touchingPolygon = ReadWithoutSrid(
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Touches(
                firstLine, touchingLine), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Touches(
                touchingLine, firstLine), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Touches(
                firstLine, crossingLine), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Touches(
                firstPolygon, touchingPolygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Touches(
                ReadWithoutSrid("POINT (0 0)"),
                ReadWithoutSrid("POINT (0 0)")), Is.False);
        });
    }

    [Test]
    public void CrossesUsesDimensionSpecificDe9ImSemantics()
    {
        Geometry horizontal = ReadWithoutSrid(
            "LINESTRING (0 1, 2 1)");
        Geometry vertical = ReadWithoutSrid(
            "LINESTRING (1 0, 1 2)");
        Geometry polygon = ReadWithoutSrid(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry throughPolygon = ReadWithoutSrid(
            "LINESTRING (-1 1, 3 1)");
        Geometry splitPoints = ReadWithoutSrid(
            "MULTIPOINT ((1 1), (3 3))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Crosses(
                horizontal, vertical), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Crosses(
                throughPolygon, polygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Crosses(
                polygon, throughPolygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Crosses(
                splitPoints, polygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Crosses(
                ReadWithoutSrid("LINESTRING (0 0, 2 0)"),
                ReadWithoutSrid("LINESTRING (1 0, 3 0)")), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Crosses(
                ReadWithoutSrid("POINT (1 1)"), polygon), Is.False);
        });
    }

    [Test]
    public void WithinContainsCoversAndCoveredByDistinguishBoundaries()
    {
        Geometry polygon = ReadWithoutSrid(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        Geometry interiorPoint = ReadWithoutSrid("POINT (2 2)");
        Geometry boundaryPoint = ReadWithoutSrid("POINT (0 2)");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Within(
                interiorPoint, polygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Contains(
                polygon, interiorPoint), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Covers(
                polygon, interiorPoint), Is.True);
            Assert.That(GeometryPredicateAlgorithms.CoveredBy(
                interiorPoint, polygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Within(
                boundaryPoint, polygon), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Contains(
                polygon, boundaryPoint), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Covers(
                polygon, boundaryPoint), Is.True);
            Assert.That(GeometryPredicateAlgorithms.CoveredBy(
                boundaryPoint, polygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Covers(
                polygon, ReadWithoutSrid("POINT EMPTY")), Is.False);
        });
    }

    [Test]
    public void OverlapsRequiresEqualDimensionAndPartialSharedCoverage()
    {
        Geometry firstLine = ReadWithoutSrid(
            "LINESTRING (0 0, 2 0)");
        Geometry secondLine = ReadWithoutSrid(
            "LINESTRING (1 0, 3 0)");
        Geometry firstPolygon = ReadWithoutSrid(
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))");
        Geometry secondPolygon = ReadWithoutSrid(
            "POLYGON ((2 1, 4 1, 4 4, 2 4, 2 1))");
        Geometry firstPoints = ReadWithoutSrid(
            "MULTIPOINT ((0 0), (1 1))");
        Geometry secondPoints = ReadWithoutSrid(
            "MULTIPOINT ((1 1), (2 2))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Overlaps(
                firstLine, secondLine), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Overlaps(
                firstPolygon, secondPolygon), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Overlaps(
                firstPoints, secondPoints), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Overlaps(
                firstLine, firstLine), Is.False);
            Assert.That(GeometryPredicateAlgorithms.Overlaps(
                firstLine, firstPolygon), Is.False);
        });
    }

    [Test]
    public void ContainsProperlyRejectsEqualityAndBoundaryContact()
    {
        Geometry outer = ReadWithoutSrid(
            "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))");
        Geometry inner = ReadWithoutSrid(
            "POLYGON ((1 1, 2 1, 2 2, 1 2, 1 1))");
        Geometry boundaryTouching = ReadWithoutSrid(
            "POLYGON ((0 1, 2 1, 2 2, 0 2, 0 1))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.ContainsProperly(
                outer, inner), Is.True);
            Assert.That(GeometryPredicateAlgorithms.ContainsProperly(
                outer, boundaryTouching), Is.False);
            Assert.That(GeometryPredicateAlgorithms.ContainsProperly(
                outer, outer), Is.False);
            Assert.That(GeometryPredicateAlgorithms.ContainsProperly(
                inner, outer), Is.False);
        });
    }

    [Test]
    public void WithinDistanceUsesPlanarPointSegmentAndSurfaceFacets()
    {
        Geometry firstPoint = ReadWithoutSrid("POINT Z (0 0 100)");
        Geometry secondPoint = ReadWithoutSrid("POINT Z (3 4 -100)");
        Geometry line = ReadWithoutSrid("LINESTRING (-1 0, 1 0)");
        Geometry pointAboveLine = ReadWithoutSrid("POINT (0 2)");
        Geometry firstPolygon = ReadWithoutSrid(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry secondPolygon = ReadWithoutSrid(
            "POLYGON ((5 0, 7 0, 7 2, 5 2, 5 0))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                firstPoint, secondPoint, 5.0), Is.True);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                firstPoint, secondPoint, 4.999), Is.False);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                pointAboveLine, line, 2.0), Is.True);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                pointAboveLine, line, 1.999), Is.False);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                firstPolygon, secondPolygon, 3.0), Is.True);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                firstPolygon, secondPolygon, 2.999), Is.False);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                firstPolygon, ReadWithoutSrid("POINT (1 1)"), 0.0), Is.True);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                firstPolygon, ReadWithoutSrid("POINT EMPTY"), 100.0), Is.False);
        });
    }

    [Test]
    public void WithinDistanceSupportsNestedCoreCollections()
    {
        Geometry collection = ReadWithoutSrid(
            "GEOMETRYCOLLECTION (POINT (20 20), LINESTRING (0 0, 4 0))");
        Geometry point = ReadWithoutSrid("POINT (2 3)");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                collection, point, 3.0), Is.True);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                point, collection, 3.0), Is.True);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                collection, point, 2.999), Is.False);
        });
    }

    [Test]
    public void PredicateInputsValidateDistancesSpatialReferencesAndTypes()
    {
        Geometry point = ReadWithoutSrid("POINT (0 0)");
        Geometry firstSrid = Read("SRID=4326;POINT (0 0)");
        Geometry secondSrid = Read("SRID=3857;POINT (0 0)");
        Geometry triangle = ReadWithoutSrid(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryPredicateAlgorithms.Intersects(firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryPredicateAlgorithms.WithinDistance(
                    point, point, -1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryPredicateAlgorithms.WithinDistance(
                    point, point, double.NaN));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryPredicateAlgorithms.WithinDistance(
                    point, point, double.PositiveInfinity));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryPredicateAlgorithms.Contains(triangle, point));
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
}
