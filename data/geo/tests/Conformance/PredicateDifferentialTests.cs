using geo;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class PredicateDifferentialTests
{
    [Test]
    public void EqualityModesRemainSemanticallyDistinct()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = ReadWithoutSrid(
            "MULTILINESTRING Z ((0 0 1, 1 1 2), (3 0 3, 4 1 4))");
        Geometry reorderedAndReversed = ReadWithoutSrid(
            "MULTILINESTRING Z ((4 1 40, 3 0 30), (1 1 20, 0 0 10))");
        Geometry densified = ReadWithoutSrid(
            "MULTILINESTRING Z ((0 0 10, 0.5 0.5 15, 1 1 20), (3 0 30, 4 1 40))");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreExactlyEqual(
                first, reorderedAndReversed), Is.False);
            Assert.That(predicates.AreNormalizedEqual(
                first, reorderedAndReversed), Is.False);
            Assert.That(predicates.AreTopologicallyEqual(
                first, reorderedAndReversed), Is.True);
            Assert.That(predicates.AreNormalizedEqual(
                first, densified), Is.False);
            Assert.That(predicates.AreTopologicallyEqual(
                first, densified), Is.True);
        });
    }

    [Test]
    public void ToleranceEqualityIncludesEveryOrdinateAndItsBoundary()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = ReadWithoutSrid("POINT ZM (1 2 3 4)");
        Geometry atTolerance = ReadWithoutSrid(
            "POINT ZM (1.1 1.9 3.1 3.9)");
        Geometry changedMeasure = ReadWithoutSrid(
            "POINT ZM (1 2 3 4.100000001)");
        Geometry reordered = ReadWithoutSrid(
            "MULTIPOINT ((1.01 1), (0.01 0))");
        Geometry originalOrder = ReadWithoutSrid(
            "MULTIPOINT ((0 0), (1 1))");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreEqualWithinTolerance(
                first, atTolerance, 0.1), Is.True);
            Assert.That(predicates.AreEqualWithinTolerance(
                first, changedMeasure, 0.1), Is.False);
            Assert.That(predicates.AreEqualWithinTolerance(
                originalOrder, reordered, 1.0), Is.False);
            Assert.That(predicates.AreEqualWithinTolerance(
                first, ReadWithoutSrid("POINT Z (1 2 3)"), 100.0), Is.False);
        });
    }

    [Test]
    public void InvalidInputsAndToleranceValuesAreRejected()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry point = ReadWithoutSrid("POINT (0 0)");
        Geometry invalidCoordinate = new Geometry
        {
            Point = new Point
            {
                Coordinate = new Coordinate { X = double.NaN, Y = 0.0 }
            }
        };
        Geometry invalidLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 0.0, Y = 0.0 }]
            }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreExactlyEqual(null!, point));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreExactlyEqual(invalidCoordinate, point));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Intersects(invalidLine, point));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreEqualWithinTolerance(point, point, -0.1));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreEqualWithinTolerance(
                    point, point, double.NaN));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreEqualWithinTolerance(
                    point, point, double.PositiveInfinity));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.WithinDistance(point, point, -0.1));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.WithinDistance(
                    point, point, double.PositiveInfinity));
        });
    }

    [Test]
    public void EveryBinaryPredicateRejectsConflictingKnownSrids()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = ReadWithSrid("SRID=4326;POINT (0 0)");
        Geometry second = ReadWithSrid("SRID=3857;POINT (100 100)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreExactlyEqual(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreEqualWithinTolerance(first, second, 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreNormalizedEqual(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.AreTopologicallyEqual(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Disjoint(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Intersects(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Touches(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Crosses(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Within(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Contains(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Overlaps(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.Covers(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.CoveredBy(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.ContainsProperly(first, second));
            Assert.Throws<InvalidGeometryException>(() =>
                predicates.WithinDistance(first, second, 200.0));
        });
    }

    [Test]
    public void KnownAndUnknownSridPairingRemainsPermitted()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry known = ReadWithSrid("SRID=4326;POINT (1 1)");
        Geometry unknown = ReadWithoutSrid("POINT (1 1)");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreExactlyEqual(known, unknown), Is.False);
            Assert.That(predicates.AreNormalizedEqual(known, unknown), Is.False);
            Assert.That(predicates.AreTopologicallyEqual(known, unknown), Is.True);
            Assert.That(predicates.Intersects(known, unknown), Is.True);
            Assert.That(predicates.WithinDistance(known, unknown, 0.0), Is.True);
        });
    }

    [Test]
    public void RoundedNodedFragmentsRetainTheirOwningCurveLocation()
    {
        IGeometryRelations relations = new GeometryRelations();
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry line = ReadWithoutSrid(
            "LINESTRING (-3 -9, 2 -11)");
        Geometry polygons = ReadWithoutSrid(
            "MULTIPOLYGON (((1 -11, 3 -11, 3 -6, 1 -6, 1 -11)), ((5 -11, 7 -11, 7 -6, 5 -6, 5 -11)))");

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
                relations.Relate(line, polygons)),
                Is.EqualTo("101F00212"));
            Assert.That(predicates.Crosses(line, polygons), Is.True);
            Assert.That(predicates.Touches(line, polygons), Is.False);
        });
    }

    [Test]
    public void SeededRandomCorePredicatesMatchNetTopologySuite()
    {
        const int seed = 18720417;
        const int caseCount = 160;
        Random random = new Random(seed);
        IGeometryRelations relations = new GeometryRelations();
        IGeometryPredicates predicates = new GeometryPredicates();
        NtsWktReader ntsReader = new NtsWktReader();

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            string firstText = CreateRandomGeometryText(random);
            string secondText = CreateRandomGeometryText(random);
            double maximumDistance = random.Next(0, 7);
            Geometry first = ReadWithoutSrid(firstText);
            Geometry second = ReadWithoutSrid(secondText);
            NtsGeometry ntsFirst = ntsReader.Read(firstText);
            NtsGeometry ntsSecond = ntsReader.Read(secondText);
            string description =
                $"seed {seed}, case {caseIndex}: {firstText} versus {secondText}";

            Assert.Multiple(() =>
            {
                Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
                    relations.Relate(first, second)),
                    Is.EqualTo(ntsFirst.Relate(ntsSecond).ToString()),
                    description + " relation");
                Assert.That(predicates.AreTopologicallyEqual(first, second),
                    Is.EqualTo(ntsFirst.EqualsTopologically(ntsSecond)),
                    description + " equality");
                Assert.That(predicates.Disjoint(first, second),
                    Is.EqualTo(ntsFirst.Disjoint(ntsSecond)),
                    description + " disjoint");
                Assert.That(predicates.Intersects(first, second),
                    Is.EqualTo(ntsFirst.Intersects(ntsSecond)),
                    description + " intersects");
                Assert.That(predicates.Touches(first, second),
                    Is.EqualTo(ntsFirst.Touches(ntsSecond)),
                    description + " touches");
                Assert.That(predicates.Crosses(first, second),
                    Is.EqualTo(ntsFirst.Crosses(ntsSecond)),
                    description + " crosses");
                Assert.That(predicates.Within(first, second),
                    Is.EqualTo(ntsFirst.Within(ntsSecond)),
                    description + " within");
                Assert.That(predicates.Contains(first, second),
                    Is.EqualTo(ntsFirst.Contains(ntsSecond)),
                    description + " contains");
                Assert.That(predicates.Overlaps(first, second),
                    Is.EqualTo(ntsFirst.Overlaps(ntsSecond)),
                    description + " overlaps");
                Assert.That(predicates.Covers(first, second),
                    Is.EqualTo(ntsFirst.Covers(ntsSecond)),
                    description + " covers");
                Assert.That(predicates.CoveredBy(first, second),
                    Is.EqualTo(ntsFirst.CoveredBy(ntsSecond)),
                    description + " covered by");
                Assert.That(predicates.ContainsProperly(first, second),
                    Is.EqualTo(ntsFirst.Relate(
                        ntsSecond, De9ImPatterns.ContainsProperly)),
                    description + " contains properly");
                Assert.That(predicates.WithinDistance(
                    first, second, maximumDistance),
                    Is.EqualTo(ntsFirst.IsWithinDistance(
                        ntsSecond, maximumDistance)),
                    description + " within distance");
            });
            caseIndex++;
        }
    }

    private static string CreateRandomGeometryText(Random random)
    {
        int geometryType = random.Next(0, 6);
        int x = random.Next(-12, 13);
        int y = random.Next(-12, 13);
        int horizontal = random.Next(1, 6);
        int vertical = random.Next(1, 6);
        if (geometryType == 0)
        {
            return $"POINT ({x} {y})";
        }
        if (geometryType == 1)
        {
            return $"LINESTRING ({x} {y}, {x + horizontal} {y + random.Next(-4, 5)})";
        }
        if (geometryType == 2)
        {
            return RectangleText(x, y, horizontal, vertical);
        }
        if (geometryType == 3)
        {
            return $"MULTIPOINT (({x} {y}), ({x + horizontal} {y + vertical}))";
        }
        if (geometryType == 4)
        {
            return $"MULTILINESTRING (({x} {y}, {x + horizontal} {y}), ({x} {y + vertical}, {x + horizontal} {y + vertical}))";
        }
        string first = RectangleCoordinates(
            x, y, horizontal, vertical);
        string second = RectangleCoordinates(
            x + horizontal + 2, y, horizontal, vertical);
        return $"MULTIPOLYGON (({first}), ({second}))";
    }

    private static string RectangleText(
        int x, int y, int width, int height)
    {
        return $"POLYGON ({RectangleCoordinates(x, y, width, height)})";
    }

    private static string RectangleCoordinates(
        int x, int y, int width, int height)
    {
        return $"({x} {y}, {x + width} {y}, {x + width} {y + height}, {x} {y + height}, {x} {y})";
    }

    private static Geometry ReadWithSrid(string text)
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
