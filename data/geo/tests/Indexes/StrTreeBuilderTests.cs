using geo;

namespace geo.Tests;

public class StrTreeBuilderTests
{
    [Test]
    public void BuildCreatesCapacityBoundedHierarchyAndAggregateBounds()
    {
        StrTreeBuilder builder = new StrTreeBuilder(3);
        int index = 0;
        while (index < 20)
        {
            double x = index % 5;
            double y = index / 5;
            builder.AddItem(Item(index, x, y, x + 0.5, y + 0.25));
            index++;
        }

        PackedNode? root = builder.BuildTree();

        Assert.Multiple(() =>
        {
            Assert.That(root, Is.Not.Null);
            Assert.That(root!.Level, Is.GreaterThan(0));
            Assert.That(root.Children, Has.Length.InRange(1, 3));
            AssertEnvelope(root.Bounds, 0.0, 0.0, 4.5, 3.25);
            Assert.That(CountItems(root), Is.EqualTo(20));
            Assert.That(AllNodesRespectCapacity(root, 3), Is.True);
            Assert.That(AllNodeBoundsContainChildren(root), Is.True);
        });
    }

    [Test]
    public void StableStrPackingPreservesTieOrderDeterministically()
    {
        StrTreeBuilder first = CreateTieHeavyBuilder();
        StrTreeBuilder second = CreateTieHeavyBuilder();

        PackedNode? firstRoot = first.BuildTree();
        PackedNode? secondRoot = second.BuildTree();
        string firstLayout = GetLeafLayout(firstRoot!);
        string secondLayout = GetLeafLayout(secondRoot!);

        Assert.Multiple(() =>
        {
            Assert.That(firstLayout,
                Is.EqualTo("1,2|3,0|5,6|7,4"));
            Assert.That(secondLayout, Is.EqualTo(firstLayout));
            Assert.That(firstRoot!.Level, Is.EqualTo(2));
            Assert.That(firstRoot.Children, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void AddedItemsAreSnapshottedBeforePacking()
    {
        object payload = new object();
        Geometry geometry = PointGeometry(1.5, 2.5);
        Envelope envelope = EnvelopeValue(1.0, 2.0, 2.0, 3.0);
        SpatialIndexItem item = new SpatialIndexItem
        {
            Id = 17,
            Envelope = envelope,
            Geometry = geometry,
            Payload = payload
        };
        StrTreeBuilder builder = new StrTreeBuilder(4);

        builder.AddItem(item);
        item.Id = 99;
        envelope.MinX = -100.0;
        geometry.Point!.Coordinate!.X = 200.0;
        PackedNode? root = builder.BuildTree();
        SpatialIndexItem snapshot = root!.Items[0].Item;

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Not.SameAs(item));
            Assert.That(snapshot.Id, Is.EqualTo(17));
            AssertEnvelope(snapshot.Envelope, 1.0, 2.0, 2.0, 3.0);
            Assert.That(snapshot.Envelope, Is.Not.SameAs(envelope));
            Assert.That(snapshot.Geometry, Is.Not.SameAs(geometry));
            Assert.That(snapshot.Geometry!.Point!.Coordinate!.X,
                Is.EqualTo(1.5));
            Assert.That(snapshot.Payload, Is.SameAs(payload));
        });
    }

    [Test]
    public void EmptyAndCompletedBuildsHaveFrozenIdempotentLifecycle()
    {
        StrTreeBuilder empty = new StrTreeBuilder(4);
        PackedNode? firstEmpty = empty.BuildTree();
        PackedNode? secondEmpty = empty.BuildTree();
        StrTreeBuilder populated = new StrTreeBuilder(4);
        populated.AddItem(Item(1, 0.0, 0.0, 1.0, 1.0));
        PackedNode? firstRoot = populated.BuildTree();
        PackedNode? secondRoot = populated.BuildTree();

        Assert.Multiple(() =>
        {
            Assert.That(firstEmpty, Is.Null);
            Assert.That(secondEmpty, Is.Null);
            Assert.That(empty.Built, Is.True);
            Assert.That(firstRoot, Is.SameAs(secondRoot));
            Assert.That(populated.Built, Is.True);
            Assert.Throws<SpatialIndexException>(() =>
                empty.AddItem(Item(2, 0.0, 0.0, 1.0, 1.0)));
            Assert.Throws<SpatialIndexException>(() =>
                populated.AddItem(Item(2, 2.0, 2.0, 3.0, 3.0)));
        });
    }

    [Test]
    public void InvalidCapacitiesItemsEnvelopesAndDuplicatesAreRejected()
    {
        StrTreeBuilder builder = new StrTreeBuilder(4);
        builder.AddItem(Item(5, 0.0, 0.0, 1.0, 1.0));
        SpatialIndexItem empty = new SpatialIndexItem { Id = 6 };
        SpatialIndexItem partial = new SpatialIndexItem
        {
            Id = 7,
            Envelope = new Envelope
            {
                MinX = 0.0,
                MinY = 0.0,
                MaxX = 1.0
            }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<SpatialIndexException>(() =>
                new StrTreeBuilder(1));
            Assert.Throws<SpatialIndexException>(() =>
                new StrTreeBuilder(
                    GeometryConstants.DefaultMaximumComponents + 1));
            Assert.Throws<SpatialIndexException>(() =>
                builder.AddItem(null!));
            Assert.Throws<SpatialIndexException>(() =>
                builder.AddItem(empty));
            Assert.Throws<InvalidGeometryException>(() =>
                builder.AddItem(partial));
            Assert.Throws<SpatialIndexException>(() =>
                builder.AddItem(Item(5, 2.0, 2.0, 3.0, 3.0)));
        });
    }

    [Test]
    public void PackingHandlesExtremeFiniteEnvelopeCenters()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(
            1, -double.MaxValue, -double.MaxValue,
            double.MaxValue, double.MaxValue));
        builder.AddItem(Item(2, 1.0, 2.0, 1.0, 2.0));

        PackedNode? root = builder.BuildTree();

        Assert.Multiple(() =>
        {
            Assert.That(root, Is.Not.Null);
            Assert.That(root!.CenterX, Is.Zero);
            Assert.That(root.CenterY, Is.Zero);
            Assert.That(root.Items[0].CenterX, Is.Zero);
            Assert.That(root.Items[0].CenterY, Is.Zero);
            AssertEnvelope(
                root.Bounds, -double.MaxValue, -double.MaxValue,
                double.MaxValue, double.MaxValue);
        });
    }

    private static StrTreeBuilder CreateTieHeavyBuilder()
    {
        StrTreeBuilder builder = new StrTreeBuilder(2);
        builder.AddItem(Item(0, 0.0, 3.0, 0.0, 3.0));
        builder.AddItem(Item(1, 0.0, 1.0, 0.0, 1.0));
        builder.AddItem(Item(2, 0.0, 1.0, 0.0, 1.0));
        builder.AddItem(Item(3, 0.0, 2.0, 0.0, 2.0));
        builder.AddItem(Item(4, 10.0, 3.0, 10.0, 3.0));
        builder.AddItem(Item(5, 10.0, 1.0, 10.0, 1.0));
        builder.AddItem(Item(6, 10.0, 1.0, 10.0, 1.0));
        builder.AddItem(Item(7, 10.0, 2.0, 10.0, 2.0));
        return builder;
    }

    private static SpatialIndexItem Item(
        int id, double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        return new SpatialIndexItem
        {
            Id = id,
            Envelope = EnvelopeValue(
                minimumX, minimumY, maximumX, maximumY)
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

    private static int CountItems(PackedNode node)
    {
        if (node.IsLeafNode()) return node.Items.Length;
        int count = 0;
        int index = 0;
        while (index < node.Children.Length)
        {
            count += CountItems(node.Children[index]);
            index++;
        }
        return count;
    }

    private static bool AllNodesRespectCapacity(
        PackedNode node, int capacity)
    {
        if (node.IsLeafNode())
        {
            return node.Items.Length > 0 &&
                node.Items.Length <= capacity &&
                node.Children.Length == 0;
        }
        if (node.Children.Length == 0 ||
            node.Children.Length > capacity || node.Items.Length != 0)
        {
            return false;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (!AllNodesRespectCapacity(node.Children[index], capacity))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static bool AllNodeBoundsContainChildren(PackedNode node)
    {
        int index = 0;
        if (node.IsLeafNode())
        {
            while (index < node.Items.Length)
            {
                if (!EnvelopeAlgorithms.CoversEnvelopeValue(
                    node.Bounds, node.Items[index].Item.Envelope))
                {
                    return false;
                }
                index++;
            }
            return true;
        }
        while (index < node.Children.Length)
        {
            if (!EnvelopeAlgorithms.CoversEnvelopeValue(
                node.Bounds, node.Children[index].Bounds) ||
                !AllNodeBoundsContainChildren(node.Children[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static string GetLeafLayout(PackedNode node)
    {
        List<string> groups = new List<string>();
        AddLeafGroups(node, groups);
        return string.Join("|", groups);
    }

    private static void AddLeafGroups(
        PackedNode node, List<string> groups)
    {
        if (node.IsLeafNode())
        {
            string[] identifiers = new string[node.Items.Length];
            int itemIndex = 0;
            while (itemIndex < node.Items.Length)
            {
                identifiers[itemIndex] =
                    node.Items[itemIndex].Item.Id.ToString();
                itemIndex++;
            }
            groups.Add(string.Join(",", identifiers));
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AddLeafGroups(node.Children[index], groups);
            index++;
        }
    }

    private static void AssertEnvelope(
        Envelope envelope, double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        Assert.That(envelope.MinX, Is.EqualTo(minimumX));
        Assert.That(envelope.MinY, Is.EqualTo(minimumY));
        Assert.That(envelope.MaxX, Is.EqualTo(maximumX));
        Assert.That(envelope.MaxY, Is.EqualTo(maximumY));
    }
}
