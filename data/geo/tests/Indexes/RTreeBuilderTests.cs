using geo;

namespace geo.Tests;

public class RTreeBuilderTests
{
    [Test]
    public void QuadraticSplitSeparatesDistantClusters()
    {
        RTreeBuilder builder = new RTreeBuilder(3);
        builder.InsertItem(Item(1, 0.0, 0.0, 1.0, 1.0, null));
        builder.InsertItem(Item(2, 2.0, 0.0, 3.0, 1.0, null));
        builder.InsertItem(Item(3, 100.0, 0.0, 101.0, 1.0, null));
        builder.InsertItem(Item(4, 102.0, 0.0, 103.0, 1.0, null));

        Assert.Multiple(() =>
        {
            Assert.That(builder.Root.Leaf, Is.False);
            Assert.That(builder.Root.Children, Has.Count.EqualTo(2));
            Assert.That(LeafIds(builder.Root.Children[0]),
                Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(LeafIds(builder.Root.Children[1]),
                Is.EquivalentTo(new[] { 3, 4 }));
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void ChooseSubtreeUsesLeastExpansionForNewEntries()
    {
        RTreeBuilder builder = CreateTwoClusterTree();

        builder.InsertItem(Item(5, 4.0, 0.0, 5.0, 1.0, null));
        RTreeNode? selected = RTreeBuilder.FindLeafByItemId(
            builder.Root, 5);

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.Not.Null);
            Assert.That(LeafIds(selected!),
                Is.EquivalentTo(new[] { 1, 2, 5 }));
            Assert.That(selected!.Bounds.MaxX, Is.EqualTo(5.0));
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void InsertSnapshotsEnvelopeGeometryAndPayloadReference()
    {
        object payload = new object();
        Geometry geometry = PointGeometry(2.0, 3.0);
        SpatialIndexItem item = Item(
            9, 1.0, 2.0, 3.0, 4.0, geometry);
        item.Payload = payload;
        RTreeBuilder builder = new RTreeBuilder(4);

        builder.InsertItem(item);
        item.Id = 99;
        item.Envelope.MinX = -100.0;
        geometry.Point!.Coordinate!.X = 200.0;
        PackedItem stored = RTreeBuilder.GetItemFromLeaf(
            builder.Root, 9);

        Assert.Multiple(() =>
        {
            Assert.That(stored.Item.Id, Is.EqualTo(9));
            AssertEnvelope(stored.Item.Envelope, 1.0, 2.0, 3.0, 4.0);
            Assert.That(stored.Item.Geometry, Is.Not.SameAs(geometry));
            Assert.That(stored.Item.Geometry!.Point!.Coordinate!.X,
                Is.EqualTo(2.0));
            Assert.That(stored.Item.Payload, Is.SameAs(payload));
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void RemoveCondensesReinsertsAndCollapsesRoot()
    {
        RTreeBuilder builder = new RTreeBuilder(3);
        int index = 0;
        while (index < 30)
        {
            double x = (index % 6) * 10.0;
            double y = (index / 6) * 10.0;
            builder.InsertItem(Item(
                index, x, y, x + 1.0, y + 1.0, null));
            index++;
        }

        index = 0;
        while (index < 29)
        {
            Assert.That(builder.RemoveItem(index), Is.True);
            Assert.That(builder.CountItems(), Is.EqualTo(29 - index));
            Assert.That(ValidateTree(builder), Is.True);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(builder.Root.Leaf, Is.True);
            Assert.That(LeafIds(builder.Root), Is.EqualTo(new[] { 29 }));
            Assert.That(builder.RemoveItem(500), Is.False);
            Assert.That(builder.RemoveItem(29), Is.True);
            Assert.That(builder.CountItems(), Is.Zero);
            Assert.That(builder.Root.Leaf, Is.True);
            Assert.That(builder.Root.Items, Is.Empty);
            Assert.That(double.IsNaN(builder.Root.Bounds.MinX), Is.True);
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void UpdateMovesAnExistingSnapshotWithoutChangingCountOrOrder()
    {
        RTreeBuilder builder = new RTreeBuilder(2);
        builder.InsertItem(Item(1, 0.0, 0.0, 1.0, 1.0,
            PointGeometry(0.5, 0.5)));
        builder.InsertItem(Item(2, 2.0, 0.0, 3.0, 1.0, null));
        builder.InsertItem(Item(3, 20.0, 0.0, 21.0, 1.0, null));
        SpatialIndexItem update = Item(
            1, 30.0, 0.0, 31.0, 1.0, PointGeometry(30.5, 0.5));

        bool updated = builder.UpdateItem(update);
        update.Envelope.MinX = -200.0;
        update.Geometry!.Point!.Coordinate!.X = -300.0;
        RTreeNode? leaf = RTreeBuilder.FindLeafByItemId(builder.Root, 1);
        PackedItem stored = RTreeBuilder.GetItemFromLeaf(leaf!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(builder.UpdateItem(Item(
                99, 0.0, 0.0, 1.0, 1.0, null)), Is.False);
            Assert.That(builder.CountItems(), Is.EqualTo(3));
            Assert.That(stored.Order, Is.Zero);
            Assert.That(stored.Item.Envelope.MinX, Is.EqualTo(30.0));
            Assert.That(stored.Item.Geometry!.Point!.Coordinate!.X,
                Is.EqualTo(30.5));
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void DuplicateInvalidAndNonCoveringItemsAreRejectedAtomically()
    {
        RTreeBuilder builder = new RTreeBuilder(3);
        builder.InsertItem(Item(1, 0.0, 0.0, 1.0, 1.0, null));
        SpatialIndexItem empty = new SpatialIndexItem { Id = 2 };
        SpatialIndexItem partial = new SpatialIndexItem
        {
            Id = 3,
            Envelope = new Envelope
            {
                MinX = 0.0,
                MinY = 0.0,
                MaxX = 1.0
            }
        };
        SpatialIndexItem uncovered = Item(
            4, 0.0, 0.0, 1.0, 1.0, PointGeometry(5.0, 5.0));

        Assert.Multiple(() =>
        {
            Assert.Throws<SpatialIndexException>(() =>
                new RTreeBuilder(1));
            Assert.Throws<SpatialIndexException>(() =>
                builder.InsertItem(null!));
            Assert.Throws<SpatialIndexException>(() =>
                builder.InsertItem(Item(
                    1, 2.0, 2.0, 3.0, 3.0, null)));
            Assert.Throws<SpatialIndexException>(() =>
                builder.InsertItem(empty));
            Assert.Throws<InvalidGeometryException>(() =>
                builder.InsertItem(partial));
            Assert.Throws<SpatialIndexException>(() =>
                builder.InsertItem(uncovered));
            Assert.That(builder.CountItems(), Is.EqualTo(1));
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void FailedUpdateLeavesTheExistingItemUnchanged()
    {
        RTreeBuilder builder = new RTreeBuilder(3);
        builder.InsertItem(Item(7, 0.0, 0.0, 1.0, 1.0,
            PointGeometry(0.5, 0.5)));
        SpatialIndexItem invalid = Item(
            7, 10.0, 10.0, 11.0, 11.0,
            PointGeometry(20.0, 20.0));

        Assert.Throws<SpatialIndexException>(() =>
            builder.UpdateItem(invalid));
        PackedItem stored = RTreeBuilder.GetItemFromLeaf(
            builder.Root, 7);

        Assert.Multiple(() =>
        {
            Assert.That(builder.CountItems(), Is.EqualTo(1));
            AssertEnvelope(stored.Item.Envelope, 0.0, 0.0, 1.0, 1.0);
            Assert.That(stored.Item.Geometry!.Point!.Coordinate!.X,
                Is.EqualTo(0.5));
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void ClearResetsTheTreeAndAcceptsFreshIdentifiers()
    {
        RTreeBuilder builder = CreateTwoClusterTree();

        builder.ClearItems();
        builder.InsertItem(Item(1, -2.0, -2.0, -1.0, -1.0, null));

        Assert.Multiple(() =>
        {
            Assert.That(builder.CountItems(), Is.EqualTo(1));
            Assert.That(builder.Root.Leaf, Is.True);
            Assert.That(LeafIds(builder.Root), Is.EqualTo(new[] { 1 }));
            Assert.That(builder.Root.Items[0].Order, Is.Zero);
            AssertEnvelope(
                builder.Root.Bounds, -2.0, -2.0, -1.0, -1.0);
            Assert.That(ValidateTree(builder), Is.True);
        });
    }

    [Test]
    public void IdenticalMutationSequencesProduceIdenticalTrees()
    {
        RTreeBuilder first = CreateMutationSequenceTree();
        RTreeBuilder second = CreateMutationSequenceTree();

        Assert.Multiple(() =>
        {
            Assert.That(DescribeTree(first.Root),
                Is.EqualTo(DescribeTree(second.Root)));
            Assert.That(ValidateTree(first), Is.True);
            Assert.That(ValidateTree(second), Is.True);
        });
    }

    private static RTreeBuilder CreateTwoClusterTree()
    {
        RTreeBuilder builder = new RTreeBuilder(3);
        builder.InsertItem(Item(1, 0.0, 0.0, 1.0, 1.0, null));
        builder.InsertItem(Item(2, 2.0, 0.0, 3.0, 1.0, null));
        builder.InsertItem(Item(3, 100.0, 0.0, 101.0, 1.0, null));
        builder.InsertItem(Item(4, 102.0, 0.0, 103.0, 1.0, null));
        return builder;
    }

    private static RTreeBuilder CreateMutationSequenceTree()
    {
        RTreeBuilder builder = new RTreeBuilder(3);
        int index = 0;
        while (index < 18)
        {
            double x = (index * 17) % 23;
            double y = (index * 11) % 19;
            builder.InsertItem(Item(
                index, x, y, x + 1.0, y + 1.0, null));
            index++;
        }
        builder.RemoveItem(2);
        builder.RemoveItem(7);
        builder.RemoveItem(13);
        builder.UpdateItem(Item(
            5, 30.0, 30.0, 31.0, 31.0, null));
        return builder;
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

    private static int[] LeafIds(RTreeNode leaf)
    {
        int[] identifiers = new int[leaf.Items.Count];
        int index = 0;
        while (index < leaf.Items.Count)
        {
            identifiers[index] = leaf.Items[index].Item.Id;
            index++;
        }
        return identifiers;
    }

    private static bool ValidateTree(RTreeBuilder builder)
    {
        if (builder.Root.Parent != null) return false;
        List<int> depths = new List<int>();
        HashSet<int> identifiers = new HashSet<int>();
        if (!ValidateNode(
            builder.Root, builder.NodeCapacity,
            builder.MinimumNodeSize, true, 0, depths, identifiers))
        {
            return false;
        }
        if (identifiers.Count != builder.CountItems()) return false;
        int index = 1;
        while (index < depths.Count)
        {
            if (depths[index] != depths[0]) return false;
            index++;
        }
        return true;
    }

    private static bool ValidateNode(
        RTreeNode node, int capacity, int minimum, bool root,
        int depth, List<int> depths, HashSet<int> identifiers)
    {
        int count = node.GetEntryCount();
        if (count > capacity) return false;
        if (!root && count < minimum) return false;
        if (node.Leaf)
        {
            if (node.Children.Count != 0) return false;
            depths.Add(depth);
            Envelope expected = new Envelope();
            int itemIndex = 0;
            while (itemIndex < node.Items.Count)
            {
                PackedItem item = node.Items[itemIndex];
                if (!identifiers.Add(item.Item.Id)) return false;
                expected = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                    expected, item.Item.Envelope);
                itemIndex++;
            }
            return EnvelopesEqual(expected, node.Bounds);
        }
        if (node.Items.Count != 0 || node.Children.Count == 0) return false;
        Envelope childBounds = new Envelope();
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            RTreeNode child = node.Children[childIndex];
            if (child.Parent != node) return false;
            if (!ValidateNode(
                child, capacity, minimum, false, depth + 1,
                depths, identifiers))
            {
                return false;
            }
            childBounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                childBounds, child.Bounds);
            childIndex++;
        }
        return EnvelopesEqual(childBounds, node.Bounds);
    }

    private static bool EnvelopesEqual(Envelope first, Envelope second)
    {
        bool firstEmpty = EnvelopeAlgorithms.ValidateAndCheckEmpty(first);
        bool secondEmpty = EnvelopeAlgorithms.ValidateAndCheckEmpty(second);
        if (firstEmpty || secondEmpty) return firstEmpty && secondEmpty;
        return first.MinX == second.MinX && first.MinY == second.MinY &&
            first.MaxX == second.MaxX && first.MaxY == second.MaxY;
    }

    private static string DescribeTree(RTreeNode node)
    {
        if (node.Leaf)
        {
            return "[" + string.Join(",", LeafIds(node)) + "]";
        }
        string[] children = new string[node.Children.Count];
        int index = 0;
        while (index < node.Children.Count)
        {
            children[index] = DescribeTree(node.Children[index]);
            index++;
        }
        return "(" + string.Join("", children) + ")";
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
