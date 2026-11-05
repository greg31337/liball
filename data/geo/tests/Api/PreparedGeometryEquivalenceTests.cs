using geo;

namespace geo.Tests;

public class PreparedGeometryEquivalenceTests
{
    [Test]
    public void EveryCoreGeometryFamilyHasFullOperationParity()
    {
        Geometry[] sources = NonEmptySources();
        Geometry[] queries = NonEmptyQueries();
        int sourceIndex = 0;
        while (sourceIndex < sources.Length)
        {
            IPreparedGeometry prepared =
                new PreparedGeometry(sources[sourceIndex]);
            int queryIndex = 0;
            while (queryIndex < queries.Length)
            {
                AssertFullParity(
                    sources[sourceIndex], prepared,
                    queries[queryIndex],
                    "source " + sourceIndex +
                    ", query " + queryIndex);
                queryIndex++;
            }
            sourceIndex++;
        }
    }

    [Test]
    public void EveryCoreEmptyFamilyHasFullOperationParity()
    {
        Geometry[] nonEmptySources = NonEmptySources();
        Geometry[] nonEmptyQueries = NonEmptyQueries();
        Geometry[] empty = EmptyFamilies();
        int index = 0;
        while (index < empty.Length)
        {
            IPreparedGeometry emptyPrepared =
                new PreparedGeometry(empty[index]);
            AssertFullParity(
                empty[index], emptyPrepared, nonEmptyQueries[index],
                "empty source " + index);

            IPreparedGeometry nonEmptyPrepared =
                new PreparedGeometry(nonEmptySources[index]);
            AssertFullParity(
                nonEmptySources[index], nonEmptyPrepared, empty[index],
                "empty query " + index);
            index++;
        }
    }

    [Test]
    public void EveryPreparedOperationRejectsInvalidQueries()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0))");
        Geometry invalid = new Geometry
        {
            GeometryType = GeometryType.LineString,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 1.0, Y = 1.0 }]
            }
        };
        IPreparedGeometry prepared = new PreparedGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Disjoint(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Intersects(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Touches(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Crosses(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Within(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Contains(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Overlaps(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Covers(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.CoveredBy(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.ContainsProperly(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.RelatePrepared(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.MatchPreparedRelation(
                    invalid, De9ImPatterns.Disjoint));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.DistancePrepared(invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.WithinPreparedDistance(invalid, 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.NearestPreparedPoints(invalid));
        });
    }

    [Test]
    public void PreparedSourceAndReturnedValuesRemainMutationIsolated()
    {
        Geometry source = Read(
            "MULTILINESTRING Z ((0 0 0, 10 0 10), " +
            "(20 20 20, 30 20 30))");
        Geometry original = GeometryCopy.CopyGeometry(source);
        Geometry query = Read("POINT Z (4 3 100)");
        IPreparedGeometry prepared = new PreparedGeometry(source);

        RelationMatrix relation = prepared.RelatePrepared(query);
        DistanceResult nearest = prepared.NearestPreparedPoints(query);
        Geometry snapshot = prepared.GetGeometrySnapshot();
        source.MultiLineString!.LineStrings[0].Coordinates[0].Y = 90.0;
        snapshot.MultiLineString!.LineStrings[0].Coordinates[0].Y = 80.0;
        query.Point!.Coordinate!.Y = 70.0;
        relation.Cells[0] = RelationDimension.Surface;
        nearest.FirstCoordinate!.Y = 60.0;

        Geometry unchangedQuery = Read("POINT Z (4 3 100)");
        RelationMatrix independent =
            prepared.RelatePrepared(unchangedQuery);
        DistanceResult independentNearest =
            prepared.NearestPreparedPoints(unchangedQuery);
        Geometry independentSnapshot = prepared.GetGeometrySnapshot();
        IGeometryRelations relations = new GeometryRelations();

        Assert.Multiple(() =>
        {
            Assert.That(RelationText(independent), Is.EqualTo(
                RelationText(relations.Relate(
                    original, unchangedQuery))));
            Assert.That(independentNearest.Distance, Is.EqualTo(3.0));
            Assert.That(independentNearest.FirstCoordinate!.Y, Is.Zero);
            Assert.That(independentSnapshot.MultiLineString!
                .LineStrings[0].Coordinates[0].Y, Is.Zero);
            Assert.That(independent.Cells,
                Is.Not.SameAs(relation.Cells));
            Assert.That(independentNearest.FirstCoordinate,
                Is.Not.SameAs(nearest.FirstCoordinate));
        });
    }

    private static void AssertFullParity(
        Geometry source, IPreparedGeometry prepared,
        Geometry query, string context)
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        IGeometryRelations relations = new GeometryRelations();
        IGeometryMeasures measures = new GeometryMeasures();
        RelationMatrix expectedRelation = relations.Relate(source, query);
        RelationMatrix actualRelation = prepared.RelatePrepared(query);
        DistanceResult expectedNearest =
            measures.NearestPoints(source, query);
        DistanceResult actualNearest =
            prepared.NearestPreparedPoints(query);
        string[] patterns =
        [
            De9ImPatterns.Disjoint,
            De9ImPatterns.Contains,
            De9ImPatterns.Within,
            De9ImPatterns.ContainsProperly,
            "T********",
            "*********"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Disjoint(query),
                Is.EqualTo(predicates.Disjoint(source, query)), context);
            Assert.That(prepared.Intersects(query),
                Is.EqualTo(predicates.Intersects(source, query)), context);
            Assert.That(prepared.Touches(query),
                Is.EqualTo(predicates.Touches(source, query)), context);
            Assert.That(prepared.Crosses(query),
                Is.EqualTo(predicates.Crosses(source, query)), context);
            Assert.That(prepared.Within(query),
                Is.EqualTo(predicates.Within(source, query)), context);
            Assert.That(prepared.Contains(query),
                Is.EqualTo(predicates.Contains(source, query)), context);
            Assert.That(prepared.Overlaps(query),
                Is.EqualTo(predicates.Overlaps(source, query)), context);
            Assert.That(prepared.Covers(query),
                Is.EqualTo(predicates.Covers(source, query)), context);
            Assert.That(prepared.CoveredBy(query),
                Is.EqualTo(predicates.CoveredBy(source, query)), context);
            Assert.That(prepared.ContainsProperly(query),
                Is.EqualTo(predicates.ContainsProperly(source, query)),
                context);
            Assert.That(RelationText(actualRelation),
                Is.EqualTo(RelationText(expectedRelation)), context);
            Assert.That(actualRelation.LeftDimension,
                Is.EqualTo(expectedRelation.LeftDimension), context);
            Assert.That(actualRelation.RightDimension,
                Is.EqualTo(expectedRelation.RightDimension), context);
            Assert.That(prepared.DistancePrepared(query),
                Is.EqualTo(measures.IndexedDistance(source, query)),
                context);
            Assert.That(prepared.WithinPreparedDistance(query, 0.0),
                Is.EqualTo(predicates.WithinDistance(
                    source, query, 0.0)), context);
            Assert.That(prepared.WithinPreparedDistance(query, 2.5),
                Is.EqualTo(predicates.WithinDistance(
                    source, query, 2.5)), context);
            Assert.That(prepared.WithinPreparedDistance(query, 100.0),
                Is.EqualTo(predicates.WithinDistance(
                    source, query, 100.0)), context);
        });
        int patternIndex = 0;
        while (patternIndex < patterns.Length)
        {
            Assert.That(prepared.MatchPreparedRelation(
                query, patterns[patternIndex]), Is.EqualTo(
                    relations.MatchRelationPattern(
                        expectedRelation, patterns[patternIndex])),
                context + ", pattern " + patternIndex);
            patternIndex++;
        }
        AssertDistanceResultParity(
            actualNearest, expectedNearest, context);
    }

    private static void AssertDistanceResultParity(
        DistanceResult actual, DistanceResult expected,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.HasResult,
                Is.EqualTo(expected.HasResult), context);
            Assert.That(actual.Distance,
                Is.EqualTo(expected.Distance), context);
            AssertCoordinateParity(
                actual.FirstCoordinate, expected.FirstCoordinate,
                context + ", first coordinate");
            AssertCoordinateParity(
                actual.SecondCoordinate, expected.SecondCoordinate,
                context + ", second coordinate");
            AssertLocationParity(
                actual.FirstLocation, expected.FirstLocation,
                context + ", first location");
            AssertLocationParity(
                actual.SecondLocation, expected.SecondLocation,
                context + ", second location");
        });
    }

    private static void AssertCoordinateParity(
        Coordinate? actual, Coordinate? expected, string context)
    {
        Assert.That(actual == null, Is.EqualTo(expected == null), context);
        if (actual == null || expected == null) return;
        Assert.That(actual.X, Is.EqualTo(expected.X), context);
        Assert.That(actual.Y, Is.EqualTo(expected.Y), context);
        Assert.That(ComparableOrdinate(actual.Z),
            Is.EqualTo(ComparableOrdinate(expected.Z)), context);
        Assert.That(ComparableOrdinate(actual.M),
            Is.EqualTo(ComparableOrdinate(expected.M)), context);
    }

    private static void AssertLocationParity(
        LinearLocation? actual, LinearLocation? expected,
        string context)
    {
        Assert.That(actual == null, Is.EqualTo(expected == null), context);
        if (actual == null || expected == null) return;
        Assert.That(actual.IsValid, Is.EqualTo(expected.IsValid), context);
        Assert.That(actual.ComponentIndex,
            Is.EqualTo(expected.ComponentIndex), context);
        Assert.That(actual.SegmentIndex,
            Is.EqualTo(expected.SegmentIndex), context);
        Assert.That(actual.SegmentFraction,
            Is.EqualTo(expected.SegmentFraction), context);
    }

    private static double ComparableOrdinate(double value)
    {
        return double.IsNaN(value) ? double.NaN : value;
    }

    private static string RelationText(RelationMatrix matrix)
    {
        return RelationMatrixAlgorithms.GetCanonicalText(matrix);
    }

    private static Geometry[] NonEmptySources()
    {
        return
        [
            Read("POINT (1 1)"),
            Read("LINESTRING (-1 1, 5 1)"),
            Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))"),
            Read("MULTIPOINT ((1 1), (7 7))"),
            Read("MULTILINESTRING ((-1 2, 5 2), (7 7, 9 7))"),
            Read("MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
                "((7 7, 9 7, 9 9, 7 9, 7 7)))"),
            Read("GEOMETRYCOLLECTION (POINT (1 1), " +
                "LINESTRING (6 0, 6 5))")
        ];
    }

    private static Geometry[] NonEmptyQueries()
    {
        return
        [
            Read("POINT (2 2)"),
            Read("LINESTRING (-2 2, 8 2)"),
            Read("POLYGON ((2 -1, 6 -1, 6 3, 2 3, 2 -1))"),
            Read("MULTIPOINT ((0 0), (5 5))"),
            Read("MULTILINESTRING ((2 -2, 2 6), (8 6, 8 10))"),
            Read("MULTIPOLYGON (((2 2, 6 2, 6 6, 2 6, 2 2)), " +
                "((8 8, 10 8, 10 10, 8 10, 8 8)))"),
            Read("GEOMETRYCOLLECTION (POINT (3 3), " +
                "LINESTRING (-1 3, 5 3))")
        ];
    }

    private static Geometry[] EmptyFamilies()
    {
        return
        [
            Read("POINT EMPTY"),
            Read("LINESTRING EMPTY"),
            Read("POLYGON EMPTY"),
            Read("MULTIPOINT EMPTY"),
            Read("MULTILINESTRING EMPTY"),
            Read("MULTIPOLYGON EMPTY"),
            Read("GEOMETRYCOLLECTION EMPTY")
        ];
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
