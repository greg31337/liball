using geo;

namespace geo.Tests;

public class IndexNearestTests
{
    [Test]
    public void EnvelopeNearestMatchesIndependentBruteForceForBothIndexes()
    {
        SpatialIndexItem[] items = GridItems(60);
        ISpatialIndex[] indexes =
        [
            new StrTreeIndex(4),
            new RTreeIndex(4)
        ];
        SpatialIndexQuery nearestQuery = Query(
            0.25, -0.75, 0.25, -0.75, 0,
            double.PositiveInfinity);
        SpatialIndexQuery nearestNine = Query(
            0.25, -0.75, 0.25, -0.75, 9,
            double.PositiveInfinity);
        SpatialIndexQuery within = Query(
            0.25, -0.75, 0.25, -0.75, 0, 6.5);
        int[] expectedAll = BruteForceNearestIds(
            items, nearestQuery.Envelope, double.PositiveInfinity, 0);
        int[] expectedNine = BruteForceNearestIds(
            items, nearestQuery.Envelope, double.PositiveInfinity, 9);
        int[] expectedWithin = BruteForceNearestIds(
            items, nearestQuery.Envelope, 6.5, 0);

        int index = 0;
        while (index < indexes.Length)
        {
            AddItems(indexes[index], items);
            indexes[index].BuildIndex();
            SpatialIndexItem? nearest =
                indexes[index].QueryNearest(nearestQuery);
            Assert.Multiple(() =>
            {
                Assert.That(nearest, Is.Not.Null);
                Assert.That(nearest!.Id, Is.EqualTo(expectedAll[0]));
                Assert.That(Ids(indexes[index].QueryKNearest(nearestNine)),
                    Is.EqualTo(expectedNine));
                Assert.That(Ids(
                    indexes[index].QueryWithinDistance(within)),
                    Is.EqualTo(expectedWithin));
            });
            index++;
        }
    }

    [Test]
    public void RefinedNearestUsesExactGeometryAndStableTies()
    {
        ISpatialIndex[] indexes =
        [
            new StrTreeIndex(2),
            new RTreeIndex(2)
        ];
        SpatialIndexItem[] items =
        [
            Item(1, -100.0, -1.0, 100.0, 1.0,
                PointGeometry(20.0, 0.0)),
            Item(2, 5.0, 0.0, 5.0, 0.0,
                PointGeometry(5.0, 0.0)),
            Item(3, 1.0, 0.0, 1.0, 0.0, null),
            Item(4, -5.0, 0.0, -5.0, 0.0,
                PointGeometry(-5.0, 0.0))
        ];
        SpatialIndexQuery all = Query(
            0.0, 0.0, 0.0, 0.0, 0,
            double.PositiveInfinity);
        all.Geometry = PointGeometry(0.0, 0.0);
        all.RefineWithGeometry = true;
        SpatialIndexQuery within = Query(
            0.0, 0.0, 0.0, 0.0, 0, 6.0);
        within.Geometry = PointGeometry(0.0, 0.0);
        within.RefineWithGeometry = true;

        int index = 0;
        while (index < indexes.Length)
        {
            AddItems(indexes[index], items);
            indexes[index].BuildIndex();
            Assert.Multiple(() =>
            {
                Assert.That(indexes[index].QueryNearest(all)!.Id,
                    Is.EqualTo(2));
                Assert.That(Ids(indexes[index].QueryKNearest(all)),
                    Is.EqualTo(new[] { 2, 4, 1 }));
                Assert.That(Ids(
                    indexes[index].QueryWithinDistance(within)),
                    Is.EqualTo(new[] { 2, 4 }));
            });
            index++;
        }
    }

    [Test]
    public void EmptyIndexesAndExclusiveDistanceLimitsHaveNoNearestResult()
    {
        ISpatialIndex[] indexes =
        [
            new StrTreeIndex(3),
            new RTreeIndex(3)
        ];
        SpatialIndexQuery query = Query(
            0.0, 0.0, 0.0, 0.0, 0, 0.999);

        int index = 0;
        while (index < indexes.Length)
        {
            indexes[index].BuildIndex();
            Assert.That(indexes[index].QueryNearest(query), Is.Null);
            Assert.That(indexes[index].QueryKNearest(query), Is.Empty);
            Assert.That(
                indexes[index].QueryWithinDistance(query), Is.Empty);
            index++;
        }

        ISpatialIndex packed = new StrTreeIndex(3);
        ISpatialIndex dynamic = new RTreeIndex(3);
        packed.InsertItem(Item(
            8, 1.0, 0.0, 1.0, 0.0, null));
        dynamic.InsertItem(Item(
            8, 1.0, 0.0, 1.0, 0.0, null));

        Assert.Multiple(() =>
        {
            Assert.That(packed.QueryNearest(query), Is.Null);
            Assert.That(dynamic.QueryNearest(query), Is.Null);
        });
    }

    private static int[] BruteForceNearestIds(
        SpatialIndexItem[] items, Envelope query,
        double maximumDistance, int maximumResults)
    {
        List<int> indexes = new List<int>();
        int index = 0;
        while (index < items.Length)
        {
            if (EnvelopeDistance(items[index].Envelope, query) <=
                maximumDistance)
            {
                indexes.Add(index);
            }
            index++;
        }
        indexes.Sort((first, second) =>
        {
            double firstDistance =
                EnvelopeDistance(items[first].Envelope, query);
            double secondDistance =
                EnvelopeDistance(items[second].Envelope, query);
            int comparison = firstDistance.CompareTo(secondDistance);
            return comparison != 0 ? comparison : first.CompareTo(second);
        });
        int count = indexes.Count;
        if (maximumResults > 0 && count > maximumResults)
        {
            count = maximumResults;
        }
        int[] identifiers = new int[count];
        index = 0;
        while (index < count)
        {
            identifiers[index] = items[indexes[index]].Id;
            index++;
        }
        return identifiers;
    }

    private static double EnvelopeDistance(
        Envelope first, Envelope second)
    {
        double horizontal = 0.0;
        if (first.MaxX < second.MinX)
        {
            horizontal = second.MinX - first.MaxX;
        }
        else if (second.MaxX < first.MinX)
        {
            horizontal = first.MinX - second.MaxX;
        }
        double vertical = 0.0;
        if (first.MaxY < second.MinY)
        {
            vertical = second.MinY - first.MaxY;
        }
        else if (second.MaxY < first.MinY)
        {
            vertical = first.MinY - second.MaxY;
        }
        return Math.Sqrt(
            horizontal * horizontal + vertical * vertical);
    }

    private static SpatialIndexItem[] GridItems(int count)
    {
        SpatialIndexItem[] items = new SpatialIndexItem[count];
        int index = 0;
        while (index < count)
        {
            double x = (index * 17) % 23 - 11;
            double y = (index * 31) % 29 - 14;
            items[index] = Item(
                1000 + index, x, y, x + 0.25, y + 0.5, null);
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
}
