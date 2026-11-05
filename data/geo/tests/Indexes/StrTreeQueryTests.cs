using geo;

namespace geo.Tests;

public class StrTreeQueryTests
{
    [Test]
    public void EnvelopeQueryReturnsLimitedInsertionOrderedSnapshots()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(30, 8.0, 8.0, 9.0, 9.0, null));
        builder.AddItem(Item(10, 0.0, 0.0, 2.0, 2.0,
            PointGeometry(1.0, 1.0)));
        builder.AddItem(Item(20, 1.0, 1.0, 3.0, 3.0,
            PointGeometry(2.0, 2.0)));
        SpatialIndexQuery query = Query(
            -1.0, -1.0, 10.0, 10.0, 2, double.PositiveInfinity);

        SpatialIndexItem[] first = builder.QueryEnvelope(query);
        first[0].Id = 999;
        first[0].Envelope.MinX = -100.0;
        if (first[1].Geometry != null)
        {
            first[1].Geometry!.Point!.Coordinate!.X = 200.0;
        }
        SpatialIndexItem[] second = builder.QueryEnvelope(query);

        Assert.Multiple(() =>
        {
            Assert.That(Ids(second), Is.EqualTo(new[] { 30, 10 }));
            Assert.That(second[0].Envelope.MinX, Is.EqualTo(8.0));
            Assert.That(second[1].Geometry!.Point!.Coordinate!.X,
                Is.EqualTo(1.0));
            Assert.That(second[0], Is.Not.SameAs(first[0]));
            Assert.That(second[0].Envelope,
                Is.Not.SameAs(first[0].Envelope));
            Assert.That(builder.Built, Is.True);
            Assert.Throws<SpatialIndexException>(() =>
                builder.RemoveItem(30));
        });
    }

    [Test]
    public void GeometryRefinementRemovesEnvelopeFalsePositives()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(1, 0.0, 0.0, 10.0, 10.0,
            Read("LINESTRING (0 0, 10 10)")));
        builder.AddItem(Item(2, 0.0, 10.0, 0.0, 10.0,
            Read("POINT (0 10)")));
        SpatialIndexQuery broad = Query(
            0.0, 10.0, 0.0, 10.0, 0, double.PositiveInfinity);
        broad.Geometry = Read("POINT (0 10)");
        SpatialIndexQuery exact = Query(
            0.0, 10.0, 0.0, 10.0, 0, double.PositiveInfinity);
        exact.Geometry = Read("POINT (0 10)");
        exact.RefineWithGeometry = true;

        SpatialIndexItem[] candidates = builder.QueryGeometry(broad);
        SpatialIndexItem[] matches = builder.QueryGeometry(exact);

        Assert.Multiple(() =>
        {
            Assert.That(Ids(candidates), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(Ids(matches), Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void EnumerationAndRemovalPreserveSurvivingInsertionOrder()
    {
        StrTreeBuilder builder = new StrTreeBuilder(3);
        builder.AddItem(Item(1, 0.0, 0.0, 1.0, 1.0, null));
        builder.AddItem(Item(2, 2.0, 0.0, 3.0, 1.0, null));
        builder.AddItem(Item(3, 4.0, 0.0, 5.0, 1.0, null));

        bool removed = builder.RemoveItem(2);
        bool missing = builder.RemoveItem(99);
        builder.AddItem(Item(2, 6.0, 0.0, 7.0, 1.0, null));
        SpatialIndexItem[] items = builder.EnumerateItems();

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(builder.CountItems(), Is.EqualTo(3));
            Assert.That(Ids(items), Is.EqualTo(new[] { 1, 3, 2 }));
            Assert.Throws<SpatialIndexException>(() =>
                builder.RemoveItem(1));
        });
    }

    [Test]
    public void EnvelopeNearestAndKNearestUseStableDistanceOrdering()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(10, 1.0, 0.0, 1.0, 0.0, null));
        builder.AddItem(Item(11, -1.0, 0.0, -1.0, 0.0, null));
        builder.AddItem(Item(12, 3.0, 0.0, 3.0, 0.0, null));
        SpatialIndexQuery nearestQuery = Query(
            0.0, 0.0, 0.0, 0.0, 0, double.PositiveInfinity);
        SpatialIndexQuery pairQuery = Query(
            0.0, 0.0, 0.0, 0.0, 2, double.PositiveInfinity);
        SpatialIndexQuery limited = Query(
            0.0, 0.0, 0.0, 0.0, 0, 0.5);

        SpatialIndexItem? nearest = builder.QueryNearest(nearestQuery);
        SpatialIndexItem[] pair = builder.QueryKNearest(pairQuery);

        Assert.Multiple(() =>
        {
            Assert.That(nearest, Is.Not.Null);
            Assert.That(nearest!.Id, Is.EqualTo(10));
            Assert.That(Ids(pair), Is.EqualTo(new[] { 10, 11 }));
            Assert.That(builder.QueryNearest(limited), Is.Null);
            Assert.That(builder.QueryKNearest(limited), Is.Empty);
        });
    }

    [Test]
    public void WithinDistanceReturnsAllEligibleItemsInDistanceOrder()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(10, 1.0, 0.0, 1.0, 0.0, null));
        builder.AddItem(Item(11, -1.0, 0.0, -1.0, 0.0, null));
        builder.AddItem(Item(12, 3.0, 0.0, 3.0, 0.0, null));
        SpatialIndexQuery withinOne = Query(
            0.0, 0.0, 0.0, 0.0, 0, 1.0);
        SpatialIndexQuery firstOnly = Query(
            0.0, 0.0, 0.0, 0.0, 1, 1.0);
        SpatialIndexQuery unlimited = Query(
            0.0, 0.0, 0.0, 0.0, 0, double.PositiveInfinity);

        Assert.Multiple(() =>
        {
            Assert.That(Ids(builder.QueryWithinDistance(withinOne)),
                Is.EqualTo(new[] { 10, 11 }));
            Assert.That(Ids(builder.QueryWithinDistance(firstOnly)),
                Is.EqualTo(new[] { 10 }));
            Assert.That(Ids(builder.QueryWithinDistance(unlimited)),
                Is.EqualTo(new[] { 10, 11, 12 }));
        });
    }

    [Test]
    public void RefinedNearestUsesGeometryDistanceAndSkipsMissingGeometry()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(1, 0.0, -1.0, 100.0, 1.0,
            PointGeometry(100.0, 0.0)));
        builder.AddItem(Item(2, 10.0, 0.0, 10.0, 0.0,
            PointGeometry(10.0, 0.0)));
        builder.AddItem(Item(3, 5.0, 0.0, 5.0, 0.0, null));
        SpatialIndexQuery query = Query(
            0.0, 0.0, 0.0, 0.0, 0, double.PositiveInfinity);
        query.Geometry = PointGeometry(0.0, 0.0);
        query.RefineWithGeometry = true;
        SpatialIndexQuery within = Query(
            0.0, 0.0, 0.0, 0.0, 0, 50.0);
        within.Geometry = PointGeometry(0.0, 0.0);
        within.RefineWithGeometry = true;

        SpatialIndexItem? nearest = builder.QueryNearest(query);
        SpatialIndexItem[] pair = builder.QueryKNearest(query);
        SpatialIndexItem[] nearby = builder.QueryWithinDistance(within);

        Assert.Multiple(() =>
        {
            Assert.That(nearest!.Id, Is.EqualTo(2));
            Assert.That(Ids(pair), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(Ids(nearby), Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void QueryAndRefinementValidationRejectsInvalidCriteria()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(1, 0.0, 0.0, 1.0, 1.0,
            PointGeometry(0.5, 0.5)));
        SpatialIndexQuery negativeCount = Query(
            0.0, 0.0, 1.0, 1.0, -1, double.PositiveInfinity);
        SpatialIndexQuery missingGeometry = Query(
            0.0, 0.0, 1.0, 1.0, 0, double.PositiveInfinity);
        missingGeometry.RefineWithGeometry = true;
        SpatialIndexQuery uncoveredGeometry = Query(
            0.0, 0.0, 1.0, 1.0, 0, double.PositiveInfinity);
        uncoveredGeometry.Geometry = PointGeometry(5.0, 5.0);
        uncoveredGeometry.RefineWithGeometry = true;
        SpatialIndexQuery invalidDistance = Query(
            0.0, 0.0, 1.0, 1.0, 0, double.NaN);
        SpatialIndexItem uncoveredItem = Item(
            2, 0.0, 0.0, 1.0, 1.0, PointGeometry(5.0, 5.0));

        Assert.Multiple(() =>
        {
            Assert.Throws<SpatialIndexException>(() =>
                builder.QueryEnvelope(null!));
            Assert.Throws<SpatialIndexException>(() =>
                builder.QueryEnvelope(negativeCount));
            Assert.Throws<SpatialIndexException>(() =>
                builder.QueryGeometry(missingGeometry));
            Assert.Throws<SpatialIndexException>(() =>
                builder.QueryGeometry(uncoveredGeometry));
            Assert.Throws<SpatialIndexException>(() =>
                builder.QueryKNearest(invalidDistance));
            Assert.Throws<SpatialIndexException>(() =>
                new StrTreeBuilder(2).AddItem(uncoveredItem));
        });
    }

    [Test]
    public void ConcurrentFirstQueriesBuildOnceAndReturnCompleteResults()
    {
        StrTreeBuilder builder = new StrTreeBuilder(4);
        int index = 0;
        while (index < 50)
        {
            builder.AddItem(Item(
                index, index, index, index + 0.5, index + 0.5, null));
            index++;
        }
        SpatialIndexQuery query = Query(
            -1.0, -1.0, 100.0, 100.0, 0,
            double.PositiveInfinity);
        int failures = 0;

        Parallel.For(0, 32, unused =>
        {
            SpatialIndexItem[] results = builder.QueryEnvelope(query);
            if (results.Length != 50 || results[0].Id != 0 ||
                results[49].Id != 49)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.Zero);
            Assert.That(builder.Built, Is.True);
            Assert.That(builder.CountItems(), Is.EqualTo(50));
        });
    }

    private static SpatialIndexItem Item(
        int id, double minimumX, double minimumY,
        double maximumX, double maximumY, Geometry? geometry)
    {
        return new SpatialIndexItem
        {
            Id = id,
            Envelope = new Envelope
            {
                MinX = minimumX,
                MinY = minimumY,
                MaxX = maximumX,
                MaxY = maximumY
            },
            Geometry = geometry
        };
    }

    private static SpatialIndexQuery Query(
        double minimumX, double minimumY,
        double maximumX, double maximumY,
        int maximumResults, double maximumDistance)
    {
        return new SpatialIndexQuery
        {
            Envelope = new Envelope
            {
                MinX = minimumX,
                MinY = minimumY,
                MaxX = maximumX,
                MaxY = maximumY
            },
            MaximumResults = maximumResults,
            MaximumDistance = maximumDistance
        };
    }

    private static int[] Ids(SpatialIndexItem[] items)
    {
        int[] identifiers = new int[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            identifiers[index] = items[index].Id;
            index++;
        }
        return identifiers;
    }

    private static Geometry PointGeometry(double x, double y)
    {
        return new Geometry
        {
            Point = new Point
            {
                Coordinate = new Coordinate { X = x, Y = y }
            }
        };
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
