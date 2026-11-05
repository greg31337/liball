using geo;

namespace geo.Tests;

public class RTreeQueryTests
{
    [Test]
    public void EnvelopeQueryReturnsLimitedInsertionOrderedSnapshots()
    {
        object payload = new object();
        RTreeBuilder builder = new RTreeBuilder(2);
        SpatialIndexItem firstItem = Item(
            30, 8.0, 8.0, 9.0, 9.0, PointGeometry(8.5, 8.5));
        firstItem.Payload = payload;
        builder.InsertItem(firstItem);
        builder.InsertItem(Item(
            10, 0.0, 0.0, 2.0, 2.0, PointGeometry(1.0, 1.0)));
        builder.InsertItem(Item(
            20, 1.0, 1.0, 3.0, 3.0, PointGeometry(2.0, 2.0)));
        SpatialIndexQuery query = Query(
            -1.0, -1.0, 10.0, 10.0, 2,
            double.PositiveInfinity);

        SpatialIndexItem[] first = builder.QueryEnvelope(query);
        first[0].Id = 999;
        first[0].Envelope.MinX = -100.0;
        first[0].Geometry!.Point!.Coordinate!.X = 200.0;
        SpatialIndexItem[] second = builder.QueryEnvelope(query);

        Assert.Multiple(() =>
        {
            Assert.That(Ids(second), Is.EqualTo(new[] { 30, 10 }));
            Assert.That(second[0].Envelope.MinX, Is.EqualTo(8.0));
            Assert.That(second[0].Geometry!.Point!.Coordinate!.X,
                Is.EqualTo(8.5));
            Assert.That(second[0].Payload, Is.SameAs(payload));
            Assert.That(second[0], Is.Not.SameAs(first[0]));
            Assert.That(second[0].Envelope,
                Is.Not.SameAs(first[0].Envelope));
            Assert.That(second[0].Geometry,
                Is.Not.SameAs(first[0].Geometry));
        });
    }

    [Test]
    public void GeometryRefinementRemovesEnvelopeFalsePositives()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            1, 0.0, 0.0, 10.0, 10.0,
            Read("LINESTRING (0 0, 10 10)")));
        builder.InsertItem(Item(
            2, 0.0, 10.0, 0.0, 10.0,
            Read("POINT (0 10)")));
        SpatialIndexQuery broad = Query(
            0.0, 10.0, 0.0, 10.0, 0,
            double.PositiveInfinity);
        broad.Geometry = Read("POINT (0 10)");
        SpatialIndexQuery exact = Query(
            0.0, 10.0, 0.0, 10.0, 0,
            double.PositiveInfinity);
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
    public void EnumerationReflectsMutationsAndPreservesItemOrder()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            1, 0.0, 0.0, 1.0, 1.0, null));
        builder.InsertItem(Item(
            2, 2.0, 0.0, 3.0, 1.0, null));
        builder.InsertItem(Item(
            3, 4.0, 0.0, 5.0, 1.0, null));

        builder.UpdateItem(Item(
            1, 10.0, 0.0, 11.0, 1.0, null));
        builder.RemoveItem(2);
        builder.InsertItem(Item(
            2, 6.0, 0.0, 7.0, 1.0, null));
        SpatialIndexItem[] items = builder.EnumerateItems();
        items[0].Envelope.MinX = -50.0;
        SpatialIndexItem[] unchanged = builder.EnumerateItems();

        Assert.Multiple(() =>
        {
            Assert.That(Ids(items), Is.EqualTo(new[] { 1, 3, 2 }));
            Assert.That(Ids(unchanged), Is.EqualTo(new[] { 1, 3, 2 }));
            Assert.That(unchanged[0].Envelope.MinX, Is.EqualTo(10.0));
            Assert.That(builder.CountItems(), Is.EqualTo(3));
        });
    }

    [Test]
    public void EnvelopeNearestQueriesUseStableDistanceOrdering()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            10, 1.0, 0.0, 1.0, 0.0, null));
        builder.InsertItem(Item(
            11, -1.0, 0.0, -1.0, 0.0, null));
        builder.InsertItem(Item(
            12, 3.0, 0.0, 3.0, 0.0, null));
        SpatialIndexQuery nearestQuery = Query(
            0.0, 0.0, 0.0, 0.0, 0,
            double.PositiveInfinity);
        SpatialIndexQuery pairQuery = Query(
            0.0, 0.0, 0.0, 0.0, 2,
            double.PositiveInfinity);
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
    public void WithinDistanceReturnsEligibleItemsInDistanceOrder()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            10, 1.0, 0.0, 1.0, 0.0, null));
        builder.InsertItem(Item(
            11, -1.0, 0.0, -1.0, 0.0, null));
        builder.InsertItem(Item(
            12, 3.0, 0.0, 3.0, 0.0, null));
        SpatialIndexQuery withinOne = Query(
            0.0, 0.0, 0.0, 0.0, 0, 1.0);
        SpatialIndexQuery firstOnly = Query(
            0.0, 0.0, 0.0, 0.0, 1, 1.0);
        SpatialIndexQuery unlimited = Query(
            0.0, 0.0, 0.0, 0.0, 0,
            double.PositiveInfinity);

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
    public void RefinedNearestUsesGeometryAndSkipsMissingGeometry()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            1, 0.0, -1.0, 100.0, 1.0,
            PointGeometry(100.0, 0.0)));
        builder.InsertItem(Item(
            2, 10.0, 0.0, 10.0, 0.0,
            PointGeometry(10.0, 0.0)));
        builder.InsertItem(Item(
            3, 5.0, 0.0, 5.0, 0.0, null));
        SpatialIndexQuery query = Query(
            0.0, 0.0, 0.0, 0.0, 0,
            double.PositiveInfinity);
        query.Geometry = PointGeometry(0.0, 0.0);
        query.RefineWithGeometry = true;
        SpatialIndexQuery within = Query(
            0.0, 0.0, 0.0, 0.0, 0, 50.0);
        within.Geometry = PointGeometry(0.0, 0.0);
        within.RefineWithGeometry = true;

        SpatialIndexItem? nearest = builder.QueryNearest(query);
        SpatialIndexItem[] pair = builder.QueryKNearest(query);
        SpatialIndexItem[] nearby =
            builder.QueryWithinDistance(within);

        Assert.Multiple(() =>
        {
            Assert.That(nearest!.Id, Is.EqualTo(2));
            Assert.That(Ids(pair), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(Ids(nearby), Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void QueriesTrackUpdatesRemovalsAndClear()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            1, 0.0, 0.0, 1.0, 1.0, null));
        builder.InsertItem(Item(
            2, 10.0, 0.0, 11.0, 1.0, null));
        SpatialIndexQuery origin = Query(
            -1.0, -1.0, 2.0, 2.0, 0,
            double.PositiveInfinity);

        Assert.That(Ids(builder.QueryEnvelope(origin)),
            Is.EqualTo(new[] { 1 }));
        builder.UpdateItem(Item(
            1, 20.0, 0.0, 21.0, 1.0, null));
        Assert.That(builder.QueryEnvelope(origin), Is.Empty);
        builder.RemoveItem(2);
        Assert.That(Ids(builder.EnumerateItems()),
            Is.EqualTo(new[] { 1 }));
        builder.ClearItems();

        Assert.Multiple(() =>
        {
            Assert.That(builder.QueryEnvelope(origin), Is.Empty);
            Assert.That(builder.QueryNearest(origin), Is.Null);
            Assert.That(builder.QueryKNearest(origin), Is.Empty);
            Assert.That(builder.QueryWithinDistance(origin), Is.Empty);
            Assert.That(builder.EnumerateItems(), Is.Empty);
        });
    }

    [Test]
    public void QueryValidationRejectsInvalidCriteria()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(
            1, 0.0, 0.0, 1.0, 1.0,
            PointGeometry(0.5, 0.5)));
        SpatialIndexQuery negativeCount = Query(
            0.0, 0.0, 1.0, 1.0, -1,
            double.PositiveInfinity);
        SpatialIndexQuery missingGeometry = Query(
            0.0, 0.0, 1.0, 1.0, 0,
            double.PositiveInfinity);
        missingGeometry.RefineWithGeometry = true;
        SpatialIndexQuery uncoveredGeometry = Query(
            0.0, 0.0, 1.0, 1.0, 0,
            double.PositiveInfinity);
        uncoveredGeometry.Geometry = PointGeometry(5.0, 5.0);
        uncoveredGeometry.RefineWithGeometry = true;
        SpatialIndexQuery invalidDistance = Query(
            0.0, 0.0, 1.0, 1.0, 0, double.NaN);

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
