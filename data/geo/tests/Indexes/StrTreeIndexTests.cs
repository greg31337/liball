using geo;

namespace geo.Tests;

public class StrTreeIndexTests
{
    [Test]
    public void PublicContractRoutesQueriesAndReturnsSnapshots()
    {
        object payload = new object();
        ISpatialIndex index = new StrTreeIndex(2);
        SpatialIndexItem crossing = Item(
            1, 0.0, 0.0, 10.0, 10.0,
            Read("LINESTRING (0 0, 10 10)"));
        crossing.Payload = payload;
        index.InsertItem(crossing);
        index.InsertItem(Item(
            2, 0.0, 10.0, 0.0, 10.0,
            Read("POINT (0 10)")));
        index.InsertItem(Item(
            3, 20.0, 10.0, 20.0, 10.0, null));
        SpatialIndexQuery pointQuery = Query(
            0.0, 10.0, 0.0, 10.0, 0,
            double.PositiveInfinity);
        SpatialIndexQuery refinedQuery = Query(
            0.0, 10.0, 0.0, 10.0, 0,
            double.PositiveInfinity);
        refinedQuery.Geometry = Read("POINT (0 10)");
        refinedQuery.RefineWithGeometry = true;
        SpatialIndexQuery distanceQuery = Query(
            0.0, 10.0, 0.0, 10.0, 2, 25.0);

        SpatialIndexItem[] envelopes = index.QueryEnvelope(pointQuery);
        SpatialIndexItem[] refined = index.QueryGeometry(refinedQuery);
        SpatialIndexItem? nearest = index.QueryNearest(distanceQuery);
        SpatialIndexItem[] nearestTwo =
            index.QueryKNearest(distanceQuery);
        SpatialIndexItem[] within =
            index.QueryWithinDistance(distanceQuery);
        SpatialIndexItem[] all = index.EnumerateItems();
        all[0].Id = 99;
        all[0].Envelope.MinX = -100.0;
        SpatialIndexItem[] unchanged = index.EnumerateItems();

        Assert.Multiple(() =>
        {
            Assert.That(Ids(envelopes), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(Ids(refined), Is.EqualTo(new[] { 2 }));
            Assert.That(nearest!.Id, Is.EqualTo(1));
            Assert.That(Ids(nearestTwo), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(Ids(within), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(Ids(unchanged), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(unchanged[0].Envelope.MinX, Is.EqualTo(0.0));
            Assert.That(unchanged[0].Payload, Is.SameAs(payload));
            Assert.That(index.CountItems(), Is.EqualTo(3));
        });
    }

    [Test]
    public void PrebuildMutationsWorkAndBuildFreezesLifecycle()
    {
        ISpatialIndex index = new StrTreeIndex(3);
        index.InsertItem(Item(
            1, 0.0, 0.0, 1.0, 1.0, null));
        index.InsertItem(Item(
            2, 2.0, 0.0, 3.0, 1.0, null));

        bool updated = index.UpdateItem(Item(
            1, 10.0, 0.0, 11.0, 1.0, null));
        bool missingUpdate = index.UpdateItem(Item(
            9, 0.0, 0.0, 1.0, 1.0, null));
        bool removed = index.RemoveItem(2);
        bool missingRemove = index.RemoveItem(8);
        index.ClearIndex();
        index.InsertItem(Item(
            1, 4.0, 4.0, 5.0, 5.0, null));
        index.BuildIndex();
        index.BuildIndex();

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(missingUpdate, Is.False);
            Assert.That(removed, Is.True);
            Assert.That(missingRemove, Is.False);
            Assert.That(index.CountItems(), Is.EqualTo(1));
            Assert.That(Ids(index.EnumerateItems()),
                Is.EqualTo(new[] { 1 }));
            Assert.Throws<SpatialIndexException>(() =>
                index.InsertItem(Item(
                    2, 0.0, 0.0, 1.0, 1.0, null)));
            Assert.Throws<SpatialIndexException>(() =>
                index.RemoveItem(1));
            Assert.Throws<SpatialIndexException>(() =>
                index.UpdateItem(Item(
                    1, 0.0, 0.0, 1.0, 1.0, null)));
            Assert.Throws<SpatialIndexException>(() =>
                index.ClearIndex());
        });
    }

    [Test]
    public void ConstructorAndDuplicateValidationAreExposed()
    {
        ISpatialIndex index = new StrTreeIndex(2);
        index.InsertItem(Item(
            7, 0.0, 0.0, 1.0, 1.0, null));

        Assert.Multiple(() =>
        {
            Assert.Throws<SpatialIndexException>(() =>
                new StrTreeIndex(1));
            Assert.Throws<SpatialIndexException>(() =>
                index.InsertItem(Item(
                    7, 2.0, 2.0, 3.0, 3.0, null)));
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

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
