using geo;

namespace geo.Tests;

public class IndexMutationTests
{
    [Test]
    public void EnvelopeQueriesMatchIndependentBruteForceInStableOrder()
    {
        SpatialIndexItem[] items = GridItems(80);
        Envelope bounds = EnvelopeValue(-3.0, -2.0, 4.0, 5.0);
        int[] expected = BruteForceIntersectionIds(items, bounds);
        ISpatialIndex[] indexes =
        [
            new StrTreeIndex(5),
            new RTreeIndex(5)
        ];
        SpatialIndexQuery query = new SpatialIndexQuery
        {
            Envelope = bounds
        };

        int index = 0;
        while (index < indexes.Length)
        {
            AddItems(indexes[index], items);
            indexes[index].BuildIndex();
            SpatialIndexItem[] actual =
                indexes[index].QueryEnvelope(query);
            actual[0].Envelope.MinX = -500.0;
            SpatialIndexItem[] independent =
                indexes[index].QueryEnvelope(query);
            Assert.Multiple(() =>
            {
                Assert.That(Ids(actual), Is.EqualTo(expected));
                Assert.That(Ids(independent), Is.EqualTo(expected));
                Assert.That(independent[0].Envelope.MinX,
                    Is.Not.EqualTo(-500.0));
            });
            index++;
        }
    }

    [Test]
    public void ExplicitRefinementRemovesEnvelopeFalsePositives()
    {
        ISpatialIndex[] indexes =
        [
            new StrTreeIndex(2),
            new RTreeIndex(2)
        ];
        SpatialIndexItem[] items =
        [
            Item(5, 0.0, 0.0, 10.0, 10.0,
                Read("LINESTRING (0 0, 10 10)")),
            Item(6, 0.0, 10.0, 0.0, 10.0,
                Read("POINT (0 10)")),
            Item(7, 0.0, 10.0, 0.0, 10.0, null)
        ];
        SpatialIndexQuery envelopeOnly = new SpatialIndexQuery
        {
            Envelope = EnvelopeValue(0.0, 10.0, 0.0, 10.0),
            Geometry = Read("POINT (0 10)")
        };
        SpatialIndexQuery refined = new SpatialIndexQuery
        {
            Envelope = EnvelopeValue(0.0, 10.0, 0.0, 10.0),
            Geometry = Read("POINT (0 10)"),
            RefineWithGeometry = true
        };

        int index = 0;
        while (index < indexes.Length)
        {
            AddItems(indexes[index], items);
            Assert.Multiple(() =>
            {
                Assert.That(Ids(indexes[index].QueryGeometry(
                    envelopeOnly)), Is.EqualTo(new[] { 5, 6, 7 }));
                Assert.That(Ids(indexes[index].QueryGeometry(refined)),
                    Is.EqualTo(new[] { 6 }));
            });
            index++;
        }
    }

    [Test]
    public void PackedIndexMutationLifecycleIsAtomicAndPredictable()
    {
        ISpatialIndex index = new StrTreeIndex(3);
        index.InsertItem(Item(
            1, 0.0, 0.0, 1.0, 1.0, null));
        index.InsertItem(Item(
            2, 2.0, 0.0, 3.0, 1.0, null));
        index.InsertItem(Item(
            3, 4.0, 0.0, 5.0, 1.0, null));

        Assert.Throws<SpatialIndexException>(() =>
            index.InsertItem(Item(
                2, 20.0, 0.0, 21.0, 1.0, null)));
        Assert.That(index.UpdateItem(Item(
            2, 12.0, 0.0, 13.0, 1.0, null)), Is.True);
        Assert.That(index.RemoveItem(1), Is.True);
        index.InsertItem(Item(
            1, 8.0, 0.0, 9.0, 1.0, null));
        Assert.That(Ids(index.EnumerateItems()),
            Is.EqualTo(new[] { 2, 3, 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(index.CountItems(), Is.EqualTo(3));
            Assert.Throws<SpatialIndexException>(() =>
                index.InsertItem(Item(
                    4, 0.0, 0.0, 1.0, 1.0, null)));
            Assert.Throws<SpatialIndexException>(() =>
                index.RemoveItem(2));
            Assert.Throws<SpatialIndexException>(() =>
                index.UpdateItem(Item(
                    2, 0.0, 0.0, 1.0, 1.0, null)));
            Assert.Throws<SpatialIndexException>(() =>
                index.ClearIndex());
        });
    }

    [Test]
    public void DynamicIndexSupportsDeterministicMutationAndIdReuse()
    {
        ISpatialIndex first = CreateMutatedDynamicIndex();
        ISpatialIndex second = CreateMutatedDynamicIndex();
        SpatialIndexQuery all = new SpatialIndexQuery
        {
            Envelope = EnvelopeValue(-1.0, -1.0, 100.0, 100.0)
        };

        Assert.Multiple(() =>
        {
            Assert.That(Ids(first.EnumerateItems()),
                Is.EqualTo(new[] { 0, 1, 3, 4, 5, 6, 7, 8, 9, 2 }));
            Assert.That(Ids(first.QueryEnvelope(all)),
                Is.EqualTo(Ids(second.QueryEnvelope(all))));
            Assert.That(Ids(first.EnumerateItems()),
                Is.EqualTo(Ids(second.EnumerateItems())));
            Assert.That(first.CountItems(), Is.EqualTo(10));
        });

        first.ClearIndex();
        first.InsertItem(Item(
            0, 50.0, 50.0, 51.0, 51.0, null));
        Assert.Multiple(() =>
        {
            Assert.That(first.CountItems(), Is.EqualTo(1));
            Assert.That(Ids(first.EnumerateItems()),
                Is.EqualTo(new[] { 0 }));
        });
    }

    [Test]
    public void EmptyIndexContractIsConsistentAcrossImplementations()
    {
        ISpatialIndex[] indexes =
        [
            new StrTreeIndex(3),
            new RTreeIndex(3)
        ];
        SpatialIndexQuery query = new SpatialIndexQuery
        {
            Envelope = EnvelopeValue(-1.0, -1.0, 1.0, 1.0),
            MaximumDistance = double.PositiveInfinity
        };

        int index = 0;
        while (index < indexes.Length)
        {
            indexes[index].BuildIndex();
            Assert.Multiple(() =>
            {
                Assert.That(indexes[index].CountItems(), Is.Zero);
                Assert.That(indexes[index].EnumerateItems(), Is.Empty);
                Assert.That(indexes[index].QueryEnvelope(query), Is.Empty);
                Assert.That(indexes[index].QueryGeometry(query), Is.Empty);
                Assert.That(indexes[index].QueryNearest(query), Is.Null);
                Assert.That(indexes[index].QueryKNearest(query), Is.Empty);
                Assert.That(indexes[index].QueryWithinDistance(query),
                    Is.Empty);
            });
            index++;
        }
    }

    private static ISpatialIndex CreateMutatedDynamicIndex()
    {
        ISpatialIndex index = new RTreeIndex(3);
        int itemIndex = 0;
        while (itemIndex < 10)
        {
            double x = (itemIndex * 13) % 17;
            double y = (itemIndex * 7) % 11;
            index.InsertItem(Item(
                itemIndex, x, y, x + 1.0, y + 1.0, null));
            itemIndex++;
        }
        Assert.That(index.UpdateItem(Item(
            5, 30.0, 30.0, 31.0, 31.0, null)), Is.True);
        Assert.That(index.RemoveItem(2), Is.True);
        Assert.Throws<SpatialIndexException>(() =>
            index.InsertItem(Item(
                3, 40.0, 40.0, 41.0, 41.0, null)));
        index.InsertItem(Item(
            2, 40.0, 40.0, 41.0, 41.0, null));
        index.BuildIndex();
        return index;
    }

    private static int[] BruteForceIntersectionIds(
        SpatialIndexItem[] items, Envelope query)
    {
        List<int> matches = new List<int>();
        int index = 0;
        while (index < items.Length)
        {
            Envelope item = items[index].Envelope;
            if (item.MinX <= query.MaxX && item.MaxX >= query.MinX &&
                item.MinY <= query.MaxY && item.MaxY >= query.MinY)
            {
                matches.Add(items[index].Id);
            }
            index++;
        }
        return matches.ToArray();
    }

    private static SpatialIndexItem[] GridItems(int count)
    {
        SpatialIndexItem[] items = new SpatialIndexItem[count];
        int index = 0;
        while (index < count)
        {
            double x = (index * 19) % 31 - 15;
            double y = (index * 23) % 29 - 14;
            double width = (index % 4) * 0.5;
            double height = (index % 5) * 0.25;
            items[index] = Item(
                100 + index, x, y, x + width, y + height, null);
            index++;
        }
        return items;
    }

    private static void AddItems(
        ISpatialIndex index, SpatialIndexItem[] items)
    {
        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            index.InsertItem(items[itemIndex]);
            itemIndex++;
        }
    }

    private static SpatialIndexItem Item(
        int id, double minimumX, double minimumY,
        double maximumX, double maximumY, Geometry? geometry)
    {
        return new SpatialIndexItem
        {
            Id = id,
            Envelope = EnvelopeValue(
                minimumX, minimumY, maximumX, maximumY),
            Geometry = geometry
        };
    }

    private static Envelope EnvelopeValue(
        double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        return new Envelope
        {
            MinX = minimumX,
            MinY = minimumY,
            MaxX = maximumX,
            MaxY = maximumY
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
