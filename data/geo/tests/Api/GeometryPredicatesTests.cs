using geo;

namespace geo.Tests;

public class GeometryPredicatesTests
{
    [Test]
    public void PublicEqualityMethodsExposeDistinctEqualitySemantics()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry line = ReadWithoutSrid(
            "LINESTRING Z (0 0 1, 1 1 2, 2 0 3)");
        Geometry reversed = ReadWithoutSrid(
            "LINESTRING Z (2 0 30, 1 1 20, 0 0 10)");
        Geometry near = ReadWithoutSrid(
            "LINESTRING Z (0.01 0 1, 1 1 2, 2 0 3)");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreExactlyEqual(line, line), Is.True);
            Assert.That(predicates.AreExactlyEqual(line, reversed), Is.False);
            Assert.That(predicates.AreEqualWithinTolerance(
                line, near, 0.01), Is.True);
            Assert.That(predicates.AreNormalizedEqual(
                ReadWithoutSrid("LINESTRING (0 0, 1 1, 2 0)"),
                ReadWithoutSrid("LINESTRING (2 0, 1 1, 0 0)")), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(
                line, reversed), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(
                line, ReadWithoutSrid(
                    "LINESTRING Z (0 0 1, 3 0 3)")), Is.False);
        });
    }

    [Test]
    public void PublicNamedPredicatesPreserveDe9ImBoundarySemantics()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry polygon = ReadWithoutSrid(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        Geometry inside = ReadWithoutSrid("POINT (2 2)");
        Geometry boundary = ReadWithoutSrid("POINT (0 2)");
        Geometry crossing = ReadWithoutSrid(
            "LINESTRING (-1 2, 5 2)");
        Geometry overlap = ReadWithoutSrid(
            "POLYGON ((2 2, 6 2, 6 6, 2 6, 2 2))");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.Within(inside, polygon), Is.True);
            Assert.That(predicates.Contains(polygon, inside), Is.True);
            Assert.That(predicates.ContainsProperly(polygon, inside), Is.True);
            Assert.That(predicates.Within(boundary, polygon), Is.False);
            Assert.That(predicates.Contains(polygon, boundary), Is.False);
            Assert.That(predicates.Covers(polygon, boundary), Is.True);
            Assert.That(predicates.CoveredBy(boundary, polygon), Is.True);
            Assert.That(predicates.Crosses(crossing, polygon), Is.True);
            Assert.That(predicates.Overlaps(polygon, overlap), Is.True);
            Assert.That(predicates.Intersects(polygon, overlap), Is.True);
            Assert.That(predicates.Disjoint(polygon, overlap), Is.False);
        });
    }

    [Test]
    public void EmptyGeometrySemanticsAreCompleteAcrossThePublicApi()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = ReadWithoutSrid("POINT EMPTY");
        Geometry second = ReadWithoutSrid("POLYGON EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreExactlyEqual(first, second), Is.False);
            Assert.That(predicates.AreNormalizedEqual(first, second), Is.False);
            Assert.That(predicates.AreTopologicallyEqual(first, second), Is.True);
            Assert.That(predicates.Disjoint(first, second), Is.True);
            Assert.That(predicates.Intersects(first, second), Is.False);
            Assert.That(predicates.Touches(first, second), Is.False);
            Assert.That(predicates.Crosses(first, second), Is.False);
            Assert.That(predicates.Within(first, second), Is.False);
            Assert.That(predicates.Contains(first, second), Is.False);
            Assert.That(predicates.Overlaps(first, second), Is.False);
            Assert.That(predicates.Covers(first, second), Is.False);
            Assert.That(predicates.CoveredBy(first, second), Is.False);
            Assert.That(predicates.ContainsProperly(first, second), Is.False);
            Assert.That(predicates.WithinDistance(
                first, second, 100.0), Is.False);
        });
    }

    [Test]
    public void NestedCollectionsUseTheirCombinedPointSetAndBoundary()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry collection = ReadWithoutSrid(
            "GEOMETRYCOLLECTION (POINT (10 10), GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING (0 0, 4 0)))");
        Geometry interiorPoint = ReadWithoutSrid("POINT (2 0)");
        Geometry endpoint = ReadWithoutSrid("POINT (0 0)");
        Geometry nearby = ReadWithoutSrid("POINT (2 3)");
        Geometry far = ReadWithoutSrid("POINT (30 30)");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.Contains(
                collection, interiorPoint), Is.True);
            Assert.That(predicates.Within(
                interiorPoint, collection), Is.True);
            Assert.That(predicates.Covers(
                collection, endpoint), Is.True);
            Assert.That(predicates.Touches(
                collection, endpoint), Is.True);
            Assert.That(predicates.Intersects(
                collection, far), Is.False);
            Assert.That(predicates.Disjoint(
                collection, far), Is.True);
            Assert.That(predicates.WithinDistance(
                collection, nearby, 3.0), Is.True);
            Assert.That(predicates.WithinDistance(
                collection, nearby, 2.999), Is.False);
        });
    }

    [Test]
    public void EnvelopeShortCircuitsReturnOnlyLogicallyDecisiveResults()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = ReadWithoutSrid(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry second = ReadWithoutSrid(
            "POLYGON ((10 10, 12 10, 12 12, 10 12, 10 10))");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.Disjoint(first, second), Is.True);
            Assert.That(predicates.Intersects(first, second), Is.False);
            Assert.That(predicates.Touches(first, second), Is.False);
            Assert.That(predicates.Crosses(first, second), Is.False);
            Assert.That(predicates.Within(first, second), Is.False);
            Assert.That(predicates.Contains(first, second), Is.False);
            Assert.That(predicates.Overlaps(first, second), Is.False);
            Assert.That(predicates.Covers(first, second), Is.False);
            Assert.That(predicates.CoveredBy(first, second), Is.False);
            Assert.That(predicates.ContainsProperly(first, second), Is.False);
            Assert.That(predicates.WithinDistance(
                first, second, 11.0), Is.False);
        });
    }

    [Test]
    public void ValidationPrecedesEnvelopeShortCircuits()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry point = ReadWithoutSrid("POINT (100 100)");
        Geometry invalidLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 0.0, Y = 0.0 }]
            }
        };
        Geometry firstSrid = Read("SRID=4326;POINT (0 0)");
        Geometry secondSrid = Read("SRID=3857;POINT (100 100)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Disjoint(invalidLine, point));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Intersects(invalidLine, point));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Disjoint(firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.WithinDistance(point, point, -1.0));
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
