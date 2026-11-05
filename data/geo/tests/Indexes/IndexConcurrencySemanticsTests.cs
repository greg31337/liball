using geo;

namespace geo.Tests;

public class IndexConcurrencySemanticsTests
{
    [Test]
    public void BuiltPackedIndexSupportsConcurrentSnapshotQueries()
    {
        ISpatialIndex index = new StrTreeIndex(4);
        int itemIndex = 0;
        while (itemIndex < 60)
        {
            index.InsertItem(Item(itemIndex, itemIndex));
            itemIndex++;
        }
        index.BuildIndex();
        SpatialIndexQuery all = Query(
            -1.0, -1.0, 100.0, 1.0, 0,
            double.PositiveInfinity);
        SpatialIndexQuery nearest = Query(
            30.25, 0.0, 30.25, 0.0, 3,
            double.PositiveInfinity);
        int failures = 0;

        Parallel.For(0, 128, operation =>
        {
            if (operation % 4 == 0)
            {
                SpatialIndexItem[] results = index.QueryEnvelope(all);
                if (!HasSequentialIdentifiers(results, 60))
                {
                    Interlocked.Increment(ref failures);
                }
                if (results.Length > 0) results[0].Id = 1000;
            }
            else if (operation % 4 == 1)
            {
                SpatialIndexItem[] results = index.QueryGeometry(all);
                if (!HasSequentialIdentifiers(results, 60))
                {
                    Interlocked.Increment(ref failures);
                }
            }
            else if (operation % 4 == 2)
            {
                SpatialIndexItem[] results = index.EnumerateItems();
                if (!HasSequentialIdentifiers(results, 60))
                {
                    Interlocked.Increment(ref failures);
                }
            }
            else
            {
                SpatialIndexItem[] results = index.QueryKNearest(nearest);
                if (results.Length != 3 || results[0].Id != 30 ||
                    results[1].Id != 31 || results[2].Id != 29)
                {
                    Interlocked.Increment(ref failures);
                }
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.Zero);
            Assert.That(index.CountItems(), Is.EqualTo(60));
            Assert.That(HasSequentialIdentifiers(
                index.EnumerateItems(), 60), Is.True);
        });
    }

    [Test]
    public void DynamicIndexSerializesConcurrentMutationsAndQueries()
    {
        ISpatialIndex index = new RTreeIndex(4);
        Parallel.For(0, 100, itemId =>
        {
            index.InsertItem(Item(itemId, itemId));
        });
        SpatialIndexQuery all = Query(
            -1.0, -1.0, 1200.0, 1.0, 0,
            double.PositiveInfinity);
        int failures = 0;

        Parallel.For(0, 200, operation =>
        {
            if (operation % 2 == 0)
            {
                int itemId = operation / 2;
                bool updated = index.UpdateItem(
                    Item(itemId, itemId + 1000.0));
                if (!updated) Interlocked.Increment(ref failures);
            }
            else
            {
                SpatialIndexItem[] snapshot = index.QueryEnvelope(all);
                if (!ValidDynamicSnapshot(snapshot, true, 100, 100))
                {
                    Interlocked.Increment(ref failures);
                }
            }
        });

        Parallel.For(0, 150, operation =>
        {
            if (operation < 50)
            {
                bool removed = index.RemoveItem(operation * 2);
                if (!removed) Interlocked.Increment(ref failures);
            }
            else
            {
                SpatialIndexItem[] snapshot = index.EnumerateItems();
                if (!ValidDynamicSnapshot(snapshot, false, 50, 100))
                {
                    Interlocked.Increment(ref failures);
                }
            }
        });

        SpatialIndexItem[] finalItems = index.EnumerateItems();
        if (finalItems.Length > 0)
        {
            finalItems[0].Envelope.MinX = -500.0;
        }
        SpatialIndexItem[] independent = index.EnumerateItems();

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.Zero);
            Assert.That(index.CountItems(), Is.EqualTo(50));
            Assert.That(ValidDynamicSnapshot(
                independent, false, 50, 50), Is.True);
            Assert.That(AllIdentifiersOdd(independent), Is.True);
            Assert.That(AllUpdatedCoordinates(independent), Is.True);
        });
    }

    private static bool HasSequentialIdentifiers(
        SpatialIndexItem[] items, int count)
    {
        if (items.Length != count) return false;
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Id != index) return false;
            index++;
        }
        return true;
    }

    private static bool ValidDynamicSnapshot(
        SpatialIndexItem[] items, bool allowOldCoordinates,
        int minimumCount, int maximumCount)
    {
        if (items.Length < minimumCount || items.Length > maximumCount)
        {
            return false;
        }
        HashSet<int> identifiers = new HashSet<int>();
        int index = 0;
        while (index < items.Length)
        {
            SpatialIndexItem item = items[index];
            if (item.Id < 0 || item.Id >= 100 ||
                !identifiers.Add(item.Id))
            {
                return false;
            }
            double minimumX = item.Envelope.MinX;
            if (minimumX != item.Id + 1000.0 &&
                (!allowOldCoordinates || minimumX != item.Id))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static bool AllIdentifiersOdd(SpatialIndexItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Id % 2 == 0) return false;
            index++;
        }
        return true;
    }

    private static bool AllUpdatedCoordinates(SpatialIndexItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Envelope.MinX != items[index].Id + 1000.0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static SpatialIndexItem Item(int id, double x)
    {
        return new SpatialIndexItem
        {
            Id = id,
            Envelope = new Envelope
            {
                MinX = x,
                MinY = 0.0,
                MaxX = x,
                MaxY = 0.0
            }
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
}
