using geo;

namespace geo.Tests;

public class PreparedGeometryTests
{
    [Test]
    public void ConstructorAndSnapshotAccessAreMutationIsolated()
    {
        Geometry source = Read(
            "POLYGON Z ((0 0 1, 8 0 2, 8 8 3, 0 8 4, 0 0 1))");
        IPreparedGeometry prepared = new PreparedGeometry(source);

        source.Polygon!.Shell.Coordinates[0].X = 100.0;
        Geometry first = prepared.GetGeometrySnapshot();
        first.Polygon!.Shell.Coordinates[0].Y = 200.0;
        Geometry second = prepared.GetGeometrySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Polygon, Is.Not.SameAs(second.Polygon));
            Assert.That(second.Polygon!.Shell.Coordinates[0].X, Is.Zero);
            Assert.That(second.Polygon.Shell.Coordinates[0].Y, Is.Zero);
            Assert.That(second.Polygon.Shell.Coordinates[0].Z,
                Is.EqualTo(1.0));
            Assert.That(prepared.Contains(Read("POINT (2 2)")), Is.True);
        });
    }

    [Test]
    public void NamedPredicatesExposePreparedFirstOperandSemantics()
    {
        IPreparedGeometry prepared = new PreparedGeometry(Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))"));
        Geometry inside = Read("POINT (2 2)");
        Geometry boundary = Read("POINT (0 5)");
        Geometry crossing = Read("LINESTRING (-2 5, 12 5)");
        Geometry overlap = Read(
            "POLYGON ((8 8, 12 8, 12 12, 8 12, 8 8))");
        Geometry containing = Read(
            "POLYGON ((-2 -2, 12 -2, 12 12, -2 12, -2 -2))");
        Geometry far = Read("POINT (20 20)");

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Intersects(inside), Is.True);
            Assert.That(prepared.Disjoint(inside), Is.False);
            Assert.That(prepared.Contains(inside), Is.True);
            Assert.That(prepared.ContainsProperly(inside), Is.True);
            Assert.That(prepared.Covers(inside), Is.True);
            Assert.That(prepared.Touches(boundary), Is.True);
            Assert.That(prepared.Contains(boundary), Is.False);
            Assert.That(prepared.Covers(boundary), Is.True);
            Assert.That(prepared.Crosses(crossing), Is.True);
            Assert.That(prepared.Overlaps(overlap), Is.True);
            Assert.That(prepared.Within(containing), Is.True);
            Assert.That(prepared.CoveredBy(containing), Is.True);
            Assert.That(prepared.Disjoint(far), Is.True);
            Assert.That(prepared.Intersects(far), Is.False);
        });
    }

    [Test]
    public void PreparedPointLineAndCollectionQueriesMatchOrdinaryServices()
    {
        Geometry[] sources =
        [
            Read("MULTIPOINT ((0 0), (3 3))"),
            Read("MULTILINESTRING ((0 0, 4 4), (0 4, 4 0))"),
            Read("GEOMETRYCOLLECTION (POINT (8 8), LINESTRING (0 0, 4 0))")
        ];
        Geometry[] queries =
        [
            Read("LINESTRING (-1 -1, 1 1)"),
            Read("LINESTRING (0 2, 4 2)"),
            Read("POINT (2 0)")
        ];
        IGeometryPredicates ordinary = new GeometryPredicates();

        int index = 0;
        while (index < sources.Length)
        {
            IPreparedGeometry prepared = new PreparedGeometry(sources[index]);
            Geometry query = queries[index];
            Assert.Multiple(() =>
            {
                Assert.That(prepared.Intersects(query),
                    Is.EqualTo(ordinary.Intersects(sources[index], query)));
                Assert.That(prepared.Disjoint(query),
                    Is.EqualTo(ordinary.Disjoint(sources[index], query)));
                Assert.That(prepared.Touches(query),
                    Is.EqualTo(ordinary.Touches(sources[index], query)));
                Assert.That(prepared.Crosses(query),
                    Is.EqualTo(ordinary.Crosses(sources[index], query)));
                Assert.That(prepared.Within(query),
                    Is.EqualTo(ordinary.Within(sources[index], query)));
                Assert.That(prepared.Contains(query),
                    Is.EqualTo(ordinary.Contains(sources[index], query)));
                Assert.That(prepared.Overlaps(query),
                    Is.EqualTo(ordinary.Overlaps(sources[index], query)));
                Assert.That(prepared.Covers(query),
                    Is.EqualTo(ordinary.Covers(sources[index], query)));
                Assert.That(prepared.CoveredBy(query),
                    Is.EqualTo(ordinary.CoveredBy(sources[index], query)));
                Assert.That(prepared.ContainsProperly(query),
                    Is.EqualTo(ordinary.ContainsProperly(
                        sources[index], query)));
            });
            index++;
        }
    }

    [Test]
    public void PreparedRelationAndPatternMatchOrdinaryRelationService()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (POINT (8 8), " +
            "LINESTRING (-1 1, 4 1))");
        Geometry query = Read(
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))");
        IPreparedGeometry prepared = new PreparedGeometry(source);
        IGeometryRelations relations = new GeometryRelations();

        RelationMatrix actual = prepared.RelatePrepared(query);
        RelationMatrix expected = relations.Relate(source, query);
        actual.Cells[0] = RelationDimension.False;
        RelationMatrix independent = prepared.RelatePrepared(query);

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
                independent), Is.EqualTo(
                    RelationMatrixAlgorithms.GetCanonicalText(expected)));
            Assert.That(prepared.MatchPreparedRelation(
                query, "T********"), Is.True);
            Assert.That(prepared.MatchPreparedRelation(
                query, De9ImPatterns.Disjoint), Is.False);
            Assert.That(actual.Cells, Is.Not.SameAs(independent.Cells));
        });
    }

    [Test]
    public void PreparedDistanceWithinDistanceAndNearestPointsAreOwning()
    {
        Geometry source = Read("LINESTRING Z (0 0 0, 10 0 10)");
        Geometry query = Read("POINT Z (4 3 100)");
        IPreparedGeometry prepared = new PreparedGeometry(source);
        IGeometryMeasures measures = new GeometryMeasures();

        DistanceResult nearest = prepared.NearestPreparedPoints(query);
        query.Point!.Coordinate!.Y = 30.0;

        Assert.Multiple(() =>
        {
            Assert.That(prepared.DistancePrepared(Read("POINT (4 3)")),
                Is.EqualTo(measures.IndexedDistance(
                    source, Read("POINT (4 3)"))));
            Assert.That(nearest.HasResult, Is.True);
            Assert.That(nearest.Distance, Is.EqualTo(3.0));
            Assert.That(nearest.FirstCoordinate!.X, Is.EqualTo(4.0));
            Assert.That(nearest.FirstCoordinate.Y, Is.Zero);
            Assert.That(nearest.FirstCoordinate.Z, Is.EqualTo(4.0));
            Assert.That(nearest.SecondCoordinate!.Y, Is.EqualTo(3.0));
            Assert.That(nearest.FirstLocation!.ComponentIndex, Is.Zero);
            Assert.That(nearest.FirstLocation.SegmentIndex, Is.Zero);
            Assert.That(nearest.FirstLocation.SegmentFraction,
                Is.EqualTo(0.4));
            Assert.That(prepared.WithinPreparedDistance(
                Read("POINT (4 3)"), 3.0), Is.True);
            Assert.That(prepared.WithinPreparedDistance(
                Read("POINT (4 3)"), 2.999), Is.False);
        });
    }

    [Test]
    public void EmptyPreparedGeometryUsesOrdinaryEmptySemantics()
    {
        IPreparedGeometry prepared = new PreparedGeometry(
            Read("GEOMETRYCOLLECTION EMPTY"));
        Geometry empty = Read("POINT EMPTY");
        Geometry point = Read("POINT (1 1)");

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Disjoint(point), Is.True);
            Assert.That(prepared.Intersects(point), Is.False);
            Assert.That(prepared.Touches(point), Is.False);
            Assert.That(prepared.Crosses(point), Is.False);
            Assert.That(prepared.Within(point), Is.False);
            Assert.That(prepared.Contains(point), Is.False);
            Assert.That(prepared.Overlaps(point), Is.False);
            Assert.That(prepared.Covers(point), Is.False);
            Assert.That(prepared.CoveredBy(point), Is.False);
            Assert.That(prepared.ContainsProperly(point), Is.False);
            Assert.That(prepared.Disjoint(empty), Is.True);
            Assert.That(double.IsPositiveInfinity(
                prepared.DistancePrepared(point)), Is.True);
            Assert.That(prepared.WithinPreparedDistance(
                point, 100.0), Is.False);
            Assert.That(prepared.NearestPreparedPoints(point).HasResult,
                Is.False);
        });
    }

    [Test]
    public void InvalidPreparedAndQueryInputsAreRejected()
    {
        Geometry invalidPolygon = Read(
            "POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))");
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        Geometry invalidLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 100.0, Y = 100.0 }]
            }
        };
        Geometry source = Read("POINT (0 0)");
        Geometry mismatched = Read("POINT (100 100)");
        source.SpatialReferenceId = 4326;
        mismatched.SpatialReferenceId = 3857;
        IPreparedGeometry prepared = new PreparedGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedGeometry(null!));
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedGeometry(invalidPolygon));
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedGeometry(triangle));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Intersects(invalidLine));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Intersects(invalidPolygon));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.DistancePrepared(triangle));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.Contains(mismatched));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.MatchPreparedRelation(source, "BAD"));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.WithinPreparedDistance(source, -1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                prepared.WithinPreparedDistance(source, double.NaN));
        });
    }

    [Test]
    public void PublicPreparedServiceSupportsConcurrentReads()
    {
        IPreparedGeometry prepared = new PreparedGeometry(Read(
            "POLYGON ((0 0, 100 0, 100 100, 0 100, 0 0), " +
            "(40 40, 60 40, 60 60, 40 60, 40 40))"));
        Geometry inside = Read("POINT (10 10)");
        Geometry hole = Read("POINT (50 50)");
        int failures = 0;

        Parallel.For(0, 128, index =>
        {
            Geometry query = index % 2 == 0 ? inside : hole;
            bool expected = index % 2 == 0;
            bool contains = prepared.Contains(query);
            bool withinZero = prepared.WithinPreparedDistance(query, 0.0);
            RelationMatrix relation = prepared.RelatePrepared(query);
            bool relationContains = RelationMatrixAlgorithms.MatchesPattern(
                relation, De9ImPatterns.Contains);
            if (contains != expected || withinZero != expected ||
                relationContains != expected)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.That(failures, Is.Zero);
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
