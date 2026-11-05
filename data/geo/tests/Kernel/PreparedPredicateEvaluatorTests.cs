using geo;

namespace geo.Tests;

public class PreparedPredicateEvaluatorTests
{
    [Test]
    public void PolygonPreparedPredicatesMatchOrdinaryPredicates()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(4 4, 6 4, 6 6, 4 6, 4 4))");
        PreparedCollectionData prepared = new PreparedCollectionData(source);
        Geometry[] queries =
        [
            Read("POINT (2 2)"),
            Read("POINT (0 5)"),
            Read("POINT (5 5)"),
            Read("LINESTRING (-2 2, 12 2)"),
            Read("POLYGON ((8 8, 12 8, 12 12, 8 12, 8 8))"),
            Read("POLYGON ((-2 -2, 12 -2, 12 12, -2 12, -2 -2))"),
            Read("POLYGON ((20 20, 22 20, 22 22, 20 22, 20 20))")
        ];

        int index = 0;
        while (index < queries.Length)
        {
            AssertNamedPredicateParity(source, prepared, queries[index]);
            index++;
        }
    }

    [Test]
    public void PointLineCollectionAndEmptyPreparedPredicatesHaveParity()
    {
        Geometry[] sources =
        [
            Read("MULTIPOINT ((0 0), (3 3))"),
            Read("MULTILINESTRING ((0 0, 4 4), (0 4, 4 0))"),
            Read("GEOMETRYCOLLECTION (POINT (8 8), LINESTRING (0 0, 4 0))"),
            Read("GEOMETRYCOLLECTION EMPTY")
        ];
        Geometry[] queries =
        [
            Read("LINESTRING (-1 -1, 1 1)"),
            Read("LINESTRING (0 2, 4 2)"),
            Read("POINT (2 0)"),
            Read("POINT EMPTY")
        ];

        int index = 0;
        while (index < sources.Length)
        {
            PreparedCollectionData prepared =
                new PreparedCollectionData(sources[index]);
            AssertNamedPredicateParity(
                sources[index], prepared, queries[index]);
            index++;
        }
    }

    [Test]
    public void PreparedRelateAndPatternMatchReturnOrdinaryDe9ImResults()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (POINT (8 8), " +
            "LINESTRING (-1 1, 4 1))");
        Geometry query = Read(
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))");
        PreparedCollectionData prepared = new PreparedCollectionData(source);

        RelationMatrix actual = PreparedPredicateEvaluator.RelatePrepared(
            prepared, query);
        RelationMatrix expected = RelationGraphBuilder.BuildRelation(
            source, query);

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(actual),
                Is.EqualTo(
                    RelationMatrixAlgorithms.GetCanonicalText(expected)));
            Assert.That(actual.LeftDimension,
                Is.EqualTo(expected.LeftDimension));
            Assert.That(actual.RightDimension,
                Is.EqualTo(expected.RightDimension));
            Assert.That(PreparedPredicateEvaluator.MatchPreparedRelation(
                prepared, query, "T********"), Is.True);
            Assert.That(PreparedPredicateEvaluator.MatchPreparedRelation(
                prepared, query, De9ImPatterns.Disjoint), Is.False);
        });
    }

    [Test]
    public void PreparedDistanceAndNearestPointsReuseThePreparedIndex()
    {
        Geometry source = Read(
            "MULTILINESTRING Z ((0 0 0, 10 0 10), " +
            "(20 20 20, 30 20 30))");
        Geometry frozen = GeometryCopy.CopyGeometry(source);
        Geometry query = Read("POINT Z (4 3 100)");
        PreparedCollectionData prepared = new PreparedCollectionData(source);
        SegmentSpatialIndex cachedIndex = prepared.SegmentIndex;
        source.MultiLineString!.LineStrings[0].Coordinates[0].Y = 200.0;
        source.MultiLineString.LineStrings[0].Coordinates[1].Y = 200.0;

        DistanceResult expected =
            DistanceAlgorithm.GetIndexedMinimumDistance(frozen, query);
        DistanceResult actual =
            PreparedPredicateEvaluator.NearestPreparedPoints(
                prepared, query);

        Assert.Multiple(() =>
        {
            Assert.That(PreparedPredicateEvaluator.DistancePrepared(
                prepared, query), Is.EqualTo(expected.Distance));
            Assert.That(actual.HasResult, Is.EqualTo(expected.HasResult));
            Assert.That(actual.Distance, Is.EqualTo(expected.Distance));
            AssertCoordinatesEqual(
                actual.FirstCoordinate, expected.FirstCoordinate);
            AssertCoordinatesEqual(
                actual.SecondCoordinate, expected.SecondCoordinate);
            AssertLocationsEqual(
                actual.FirstLocation, expected.FirstLocation);
            AssertLocationsEqual(
                actual.SecondLocation, expected.SecondLocation);
            Assert.That(PreparedPredicateEvaluator.WithinPreparedDistance(
                prepared, query, 3.0), Is.True);
            Assert.That(PreparedPredicateEvaluator.WithinPreparedDistance(
                prepared, query, 2.999), Is.False);
            Assert.That(prepared.SegmentIndex, Is.SameAs(cachedIndex));
        });
    }

    [Test]
    public void PreparedDistanceMatchesContainmentHoleAndEmptySemantics()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(4 4, 6 4, 6 6, 4 6, 4 4))");
        PreparedCollectionData prepared = new PreparedCollectionData(polygon);
        Geometry inside = Read("POINT (2 2)");
        Geometry inHole = Read("POINT (5 5)");
        Geometry empty = Read("POINT EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(PreparedPredicateEvaluator.DistancePrepared(
                prepared, inside), Is.Zero);
            Assert.That(PreparedPredicateEvaluator.WithinPreparedDistance(
                prepared, inside, 0.0), Is.True);
            Assert.That(PreparedPredicateEvaluator.DistancePrepared(
                prepared, inHole), Is.EqualTo(1.0));
            Assert.That(PreparedPredicateEvaluator.WithinPreparedDistance(
                prepared, inHole, 0.999), Is.False);
            Assert.That(double.IsPositiveInfinity(
                PreparedPredicateEvaluator.DistancePrepared(
                    prepared, empty)), Is.True);
            Assert.That(PreparedPredicateEvaluator.WithinPreparedDistance(
                prepared, empty, 100.0), Is.False);
            Assert.That(PreparedPredicateEvaluator.NearestPreparedPoints(
                prepared, empty).HasResult, Is.False);
        });
    }

    [Test]
    public void PreparedResultsOwnTheirMutableStorage()
    {
        Geometry source = Read("LINESTRING (0 0, 10 0)");
        Geometry query = Read("POINT (5 3)");
        PreparedCollectionData prepared = new PreparedCollectionData(source);

        RelationMatrix firstRelation =
            PreparedPredicateEvaluator.RelatePrepared(prepared, query);
        RelationMatrix secondRelation =
            PreparedPredicateEvaluator.RelatePrepared(prepared, query);
        DistanceResult nearest =
            PreparedPredicateEvaluator.NearestPreparedPoints(
                prepared, query);
        query.Point!.Coordinate!.Y = 30.0;
        firstRelation.Cells[0] = RelationDimension.Surface;

        Assert.Multiple(() =>
        {
            Assert.That(firstRelation.Cells,
                Is.Not.SameAs(secondRelation.Cells));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
                secondRelation), Is.EqualTo("FF1FF00F2"));
            Assert.That(nearest.Distance, Is.EqualTo(3.0));
            Assert.That(nearest.FirstCoordinate!.Y, Is.Zero);
            Assert.That(nearest.SecondCoordinate!.Y, Is.EqualTo(3.0));
        });
    }

    [Test]
    public void PreparedEvaluationValidatesQueriesPatternsLimitsAndSrids()
    {
        Geometry source = Read("POINT (0 0)");
        PreparedCollectionData prepared = new PreparedCollectionData(source);
        Geometry invalidLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 100.0, Y = 100.0 }]
            }
        };
        Geometry extended = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        Geometry mismatched = Read("POINT (100 100)");
        prepared.GeometrySnapshot.SpatialReferenceId = 4326;
        mismatched.SpatialReferenceId = 3857;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.DisjointPrepared(
                    prepared, invalidLine));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.IntersectsPrepared(
                    prepared, extended));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.ContainsPrepared(
                    prepared, mismatched));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.RelatePrepared(
                    null!, source));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.MatchPreparedRelation(
                    prepared, source, "BAD"));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.WithinPreparedDistance(
                    prepared, source, -1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.WithinPreparedDistance(
                    prepared, source, double.NaN));
            Assert.Throws<InvalidGeometryException>(() =>
                PreparedPredicateEvaluator.DistancePrepared(
                    prepared, extended));
        });
    }

    [Test]
    public void PreparedEvaluatorSupportsConcurrentReadOnlyCalls()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 100 0, 100 100, 0 100, 0 0), " +
            "(40 40, 60 40, 60 60, 40 60, 40 40))");
        Geometry inside = Read("POINT (10 10)");
        Geometry hole = Read("POINT (50 50)");
        PreparedCollectionData prepared = new PreparedCollectionData(source);
        int failures = 0;

        Parallel.For(0, 128, index =>
        {
            Geometry query = index % 2 == 0 ? inside : hole;
            bool expected = index % 2 == 0;
            bool contains = PreparedPredicateEvaluator.ContainsPrepared(
                prepared, query);
            bool withinZero =
                PreparedPredicateEvaluator.WithinPreparedDistance(
                    prepared, query, 0.0);
            if (contains != expected || withinZero != expected)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.That(failures, Is.Zero);
    }

    private static void AssertNamedPredicateParity(
        Geometry source, PreparedCollectionData prepared, Geometry query)
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Assert.Multiple(() =>
        {
            Assert.That(PreparedPredicateEvaluator.DisjointPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Disjoint(source, query)));
            Assert.That(PreparedPredicateEvaluator.IntersectsPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Intersects(source, query)));
            Assert.That(PreparedPredicateEvaluator.TouchesPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Touches(source, query)));
            Assert.That(PreparedPredicateEvaluator.CrossesPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Crosses(source, query)));
            Assert.That(PreparedPredicateEvaluator.WithinPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Within(source, query)));
            Assert.That(PreparedPredicateEvaluator.ContainsPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Contains(source, query)));
            Assert.That(PreparedPredicateEvaluator.OverlapsPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Overlaps(source, query)));
            Assert.That(PreparedPredicateEvaluator.CoversPrepared(
                prepared, query), Is.EqualTo(
                    predicates.Covers(source, query)));
            Assert.That(PreparedPredicateEvaluator.CoveredByPrepared(
                prepared, query), Is.EqualTo(
                    predicates.CoveredBy(source, query)));
            Assert.That(PreparedPredicateEvaluator.ContainsProperlyPrepared(
                prepared, query), Is.EqualTo(
                    predicates.ContainsProperly(source, query)));
        });
    }

    private static void AssertCoordinatesEqual(
        Coordinate? actual, Coordinate? expected)
    {
        Assert.That(actual == null, Is.EqualTo(expected == null));
        if (actual == null || expected == null) return;
        Assert.That(actual.X, Is.EqualTo(expected.X));
        Assert.That(actual.Y, Is.EqualTo(expected.Y));
        Assert.That(
            double.IsNaN(actual.Z) ? double.NaN : actual.Z,
            Is.EqualTo(double.IsNaN(expected.Z) ? double.NaN : expected.Z));
        Assert.That(
            double.IsNaN(actual.M) ? double.NaN : actual.M,
            Is.EqualTo(double.IsNaN(expected.M) ? double.NaN : expected.M));
    }

    private static void AssertLocationsEqual(
        LinearLocation? actual, LinearLocation? expected)
    {
        Assert.That(actual == null, Is.EqualTo(expected == null));
        if (actual == null || expected == null) return;
        Assert.That(actual.ComponentIndex,
            Is.EqualTo(expected.ComponentIndex));
        Assert.That(actual.SegmentIndex,
            Is.EqualTo(expected.SegmentIndex));
        Assert.That(actual.SegmentFraction,
            Is.EqualTo(expected.SegmentFraction));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
