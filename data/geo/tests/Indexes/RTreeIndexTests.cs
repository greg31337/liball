using geo;

namespace geo.Tests;

public class RTreeIndexTests
{
    [Test]
    public void PublicContractRemainsMutableAfterBuild()
    {
        ISpatialIndex index = new RTreeIndex(2);
        index.InsertItem(Item(
            1, 0.0, 0.0, 1.0, 1.0, null));
        index.InsertItem(Item(
            2, 5.0, 0.0, 6.0, 1.0, null));

        index.BuildIndex();
        bool updated = index.UpdateItem(Item(
            1, 10.0, 0.0, 11.0, 1.0, null));
        bool removed = index.RemoveItem(2);
        index.InsertItem(Item(
            3, 20.0, 0.0, 21.0, 1.0, null));
        index.BuildIndex();
        SpatialIndexQuery query = Query(
            9.0, -1.0, 22.0, 2.0, 0,
            double.PositiveInfinity);

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(removed, Is.True);
            Assert.That(Ids(index.QueryEnvelope(query)),
                Is.EqualTo(new[] { 1, 3 }));
            Assert.That(Ids(index.EnumerateItems()),
                Is.EqualTo(new[] { 1, 3 }));
            Assert.That(index.CountItems(), Is.EqualTo(2));
        });
    }

    [Test]
    public void PublicContractRoutesRefinedAndDistanceQueries()
    {
        object payload = new object();
        ISpatialIndex index = new RTreeIndex(2);
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
        SpatialIndexQuery refinedQuery = Query(
            0.0, 10.0, 0.0, 10.0, 0,
            double.PositiveInfinity);
        refinedQuery.Geometry = Read("POINT (0 10)");
        refinedQuery.RefineWithGeometry = true;
        SpatialIndexQuery distanceQuery = Query(
            0.0, 10.0, 0.0, 10.0, 2, 25.0);

        SpatialIndexItem[] refined =
            index.QueryGeometry(refinedQuery);
        SpatialIndexItem? nearest = index.QueryNearest(distanceQuery);
        SpatialIndexItem[] nearestTwo =
            index.QueryKNearest(distanceQuery);
        SpatialIndexItem[] within =
            index.QueryWithinDistance(distanceQuery);
        SpatialIndexItem[] all = index.EnumerateItems();
        all[0].Envelope.MinX = -100.0;
        SpatialIndexItem[] unchanged = index.EnumerateItems();

        Assert.Multiple(() =>
        {
            Assert.That(Ids(refined), Is.EqualTo(new[] { 2 }));
            Assert.That(nearest!.Id, Is.EqualTo(1));
            Assert.That(Ids(nearestTwo), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(Ids(within), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(unchanged[0].Envelope.MinX, Is.EqualTo(0.0));
            Assert.That(unchanged[0].Payload, Is.SameAs(payload));
        });
    }

    [Test]
    public void ClearLeavesMutableIndexReadyForReuse()
    {
        ISpatialIndex index = new RTreeIndex(3);
        index.InsertItem(Item(
            4, 0.0, 0.0, 1.0, 1.0, null));
        index.ClearIndex();
        index.InsertItem(Item(
            4, 8.0, 8.0, 9.0, 9.0, null));
        SpatialIndexQuery query = Query(
            7.0, 7.0, 10.0, 10.0, 0,
            double.PositiveInfinity);

        Assert.Multiple(() =>
        {
            Assert.That(index.CountItems(), Is.EqualTo(1));
            Assert.That(Ids(index.QueryEnvelope(query)),
                Is.EqualTo(new[] { 4 }));
            Assert.Throws<SpatialIndexException>(() =>
                new RTreeIndex(1));
            Assert.Throws<SpatialIndexException>(() =>
                index.InsertItem(Item(
                    4, 10.0, 10.0, 11.0, 11.0, null)));
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
