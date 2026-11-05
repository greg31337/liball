using System.Buffers.Binary;

namespace avif.Tests;

public class ItemDependencyGraphTests
{
    [Test]
    public void SharedDependenciesUseOneNodeAndOneCachedDecodePlan()
    {
        ParsedItem leaf = CreateItem(4, true);
        ParsedItem left = WithDependencies(CreateItem(2, true), new uint[] { 4 });
        ParsedItem right = WithDependencies(CreateItem(3, true), new uint[] { 4 });
        ParsedItem root = WithDependencies(CreateItem(1, true), new uint[] { 2, 3 });
        AvifSecurityLimits limits = CreateLimits();

        ItemDependencyGraphCreateResult result = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { root, left, right, leaf },
            limits);
        ParsedItemDependencyNode? rootNode = (result.Graph ?? throw new InvalidOperationException("Invalid internal state.")).FindNode(1);
        ParsedItemDependencyNode? leftLeaf = (result.Graph.FindNode(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetDependency(0);
        ParsedItemDependencyNode? rightLeaf = (result.Graph.FindNode(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetDependency(0);
        ParsedItemDecodePlan firstPlan = (rootNode ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodePlan();
        ParsedItemDecodePlan secondPlan = rootNode.GetDecodePlan();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Graph.GetNodeCount(), Is.EqualTo(4));
            Assert.That(rootNode.GetDepth(), Is.EqualTo(2));
            Assert.That(leftLeaf, Is.SameAs(rightLeaf));
            Assert.That(leftLeaf, Is.SameAs(result.Graph.FindNode(4)));
            Assert.That(firstPlan, Is.SameAs(secondPlan));
            Assert.That(firstPlan.GetItemCount(), Is.EqualTo(4));
            Assert.That((firstPlan.GetItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(4));
            Assert.That((firstPlan.GetItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(2));
            Assert.That((firstPlan.GetItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(3));
            Assert.That((firstPlan.GetItem(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(1));
        });
    }

    [Test]
    public void CyclesAndConfiguredDepthOrCountOverrunsAreRejected()
    {
        ParsedItem cycleOne = WithDependencies(CreateItem(1, true), new uint[] { 2 });
        ParsedItem cycleTwo = WithDependencies(CreateItem(2, true), new uint[] { 1 });
        ItemDependencyGraphCreateResult cycle = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { cycleOne, cycleTwo },
            CreateLimits());

        ParsedItem chainOne = WithDependencies(CreateItem(1, true), new uint[] { 2 });
        ParsedItem chainTwo = WithDependencies(CreateItem(2, true), new uint[] { 3 });
        ParsedItem chainThree = CreateItem(3, true);
        AvifSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaxDependencyDepth = 1;
        ItemDependencyGraphCreateResult depth = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { chainOne, chainTwo, chainThree },
            depthLimits);

        AvifSecurityLimits countLimits = CreateLimits();
        countLimits.MaxItemCount = 2;
        ItemDependencyGraphCreateResult count = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { chainOne, chainTwo, chainThree },
            countLimits);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(depth.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(count.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void SelfContainmentIncludesEveryTransitiveDependency()
    {
        ParsedItem externalLeaf = CreateItem(2, false);
        ParsedItem root = WithDependencies(CreateItem(1, true), new uint[] { 2 });
        ItemDependencyGraphCreateResult external = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { root, externalLeaf },
            CreateLimits());

        ParsedItem containedLeaf = CreateItem(2, true);
        ItemDependencyGraphCreateResult contained = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { root, containedLeaf },
            CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(external.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                (external.Graph ?? throw new InvalidOperationException("Invalid internal state.")).ValidateSelfContainment(1),
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That((external.Graph.FindNode(1) ?? throw new InvalidOperationException("Invalid internal state.")).IsSelfContained(), Is.False);
            Assert.That(
                (contained.Graph ?? throw new InvalidOperationException("Invalid internal state.")).ValidateSelfContainment(1),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That((contained.Graph.FindNode(1) ?? throw new InvalidOperationException("Invalid internal state.")).IsSelfContained(), Is.True);
            Assert.That(
                contained.Graph.ValidateSelfContainment(99),
                Is.EqualTo(AvifResultCode.NotFound));
        });
    }

    [Test]
    public void ItemConstructionReferencesSelectSourcesAndParticipateInCycleChecks()
    {
        ParsedItem unusedSource = CreateItem(2, true);
        ParsedItem externalSource = CreateItem(3, false);
        ParsedItem constructed = CreateConstructedItem(
            1,
            new uint[] { 2, 3 },
            new ulong[] { 2 });
        ItemDependencyGraphCreateResult selected = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { constructed, unusedSource, externalSource },
            CreateLimits());

        ParsedItem invalidIndex = CreateConstructedItem(
            1,
            new uint[] { 2 },
            new ulong[] { 2 });
        ItemDependencyGraphCreateResult invalid = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { invalidIndex, unusedSource },
            CreateLimits());

        ParsedItem cycleOne = CreateConstructedItem(
            1,
            new uint[] { 2 },
            new ulong[] { 1 });
        ParsedItem cycleTwo = CreateConstructedItem(
            2,
            new uint[] { 1 },
            new ulong[] { 1 });
        ItemDependencyGraphCreateResult cycle = ParsedItemDependencyGraph.Create(
            new List<ParsedItem> { cycleOne, cycleTwo },
            CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(selected.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((selected.Graph ?? throw new InvalidOperationException("Invalid internal state.")).FindNode(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDependencyCount(), Is.EqualTo(1));
            Assert.That(
                ((selected.Graph.FindNode(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDependency(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItem().GetItemId(),
                Is.EqualTo(3));
            Assert.That(
                selected.Graph.ValidateSelfContainment(1),
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(cycle.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void MetaParsingAttachesGraphsAndReportsDependencyFailuresAtIref()
    {
        byte[] validData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            WriteReferences(writer, new DependencyReference[]
            {
                new DependencyReference(1, new uint[] { 2 })
            });
        });
        byte[] cycleData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            WriteReferences(writer, new DependencyReference[]
            {
                new DependencyReference(1, new uint[] { 2 }),
                new DependencyReference(2, new uint[] { 1 })
            });
        });

        MetaParseResult valid = MetaBoxParser.Parse(validData, CreateLimits());
        MetaParseResult cycle = MetaBoxParser.Parse(cycleData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(valid.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((((valid.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDependencyGraph() ?? throw new InvalidOperationException("Invalid internal state.")).FindNode(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDepth(), Is.EqualTo(1));
            Assert.That(cycle.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(cycle.Meta, Is.Null);
            Assert.That(((cycle.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iref"));
            Assert.That((cycle.Boxes.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Item));
        });
    }

    [Test]
    public void MetaParsingAppliesTheDependencyDepthLimit()
    {
        byte[] data = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            WriteReferences(writer, new DependencyReference[]
            {
                new DependencyReference(1, new uint[] { 2 }),
                new DependencyReference(2, new uint[] { 3 })
            });
        });
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxDependencyDepth = 1;

        MetaParseResult result = MetaBoxParser.Parse(data, limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(result.Meta, Is.Null);
            Assert.That(((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iref"));
            Assert.That((result.Boxes.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    private static ParsedItem CreateItem(uint itemId, bool selfContained)
    {
        ParsedItem item = new ParsedItem(
            itemId,
            3,
            false,
            0,
            "av01",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
        List<ParsedItemExtent> extents = new List<ParsedItemExtent>
        {
            new ParsedItemExtent(0, 0, 1, false, 0, 0)
        };
        ParsedItemLocation location = new ParsedItemLocation(
            itemId,
            0,
            selfContained ? (ushort)0 : (ushort)1,
            0,
            selfContained,
            selfContained ? 1UL : 0UL,
            extents);
        if (!selfContained)
        {
            ParsedDataReference reference = new ParsedDataReference(
                1,
                ParsedDataReferenceKind.Url,
                "url ",
                false,
                string.Empty,
                "https://example.test/item.av1");
            location = location.WithDataReference(reference, false, 0, extents);
        }

        return item.WithLocation(location);
    }

    private static ParsedItem WithDependencies(ParsedItem item, uint[] targetIds)
    {
        ParsedItemReference reference = new ParsedItemReference(
            ParsedItemReferenceKind.DerivedImage,
            "dimg",
            IsoBoxType.GetValue("dimg"),
            item.GetItemId(),
            new List<uint>(targetIds));
        return item.WithReferences(
            new List<ParsedItemReference> { reference },
            new List<ParsedItemReference>());
    }

    private static ParsedItem CreateConstructedItem(
        uint itemId,
        uint[] targetIds,
        ulong[] extentIndices)
    {
        ParsedItem item = CreateItem(itemId, true);
        List<ParsedItemExtent> extents = new List<ParsedItemExtent>();
        int extentIndex = 0;
        while (extentIndex < extentIndices.Length)
        {
            extents.Add(new ParsedItemExtent(
                extentIndices[extentIndex],
                0,
                1,
                false,
                0,
                0));
            extentIndex++;
        }

        ParsedItemLocation location = new ParsedItemLocation(
            itemId,
            2,
            0,
            0,
            false,
            0,
            extents);
        ParsedItemReference reference = new ParsedItemReference(
            ParsedItemReferenceKind.Unknown,
            "iloc",
            IsoBoxType.GetValue("iloc"),
            itemId,
            new List<uint>(targetIds));
        return item
            .WithLocation(location)
            .WithReferences(
                new List<ParsedItemReference> { reference },
                new List<ParsedItemReference>());
    }

    private static byte[] BuildDocument(
        uint[] itemIds,
        Action<IsoBoxWriter> writeChildren)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer, itemIds);
        writeChildren(writer);
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteHandler(IsoBoxWriter writer)
    {
        writer.BeginFullBox("hdlr", 0, 0);
        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        writer.Write(fields);
        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer, uint[] itemIds)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, (uint)itemIds.Length);
        int index = 0;
        while (index < itemIds.Length)
        {
            writer.BeginFullBox("infe", 3, 0);
            byte[] entry = new byte[11];
            BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), itemIds[index]);
            WriteFourCc(entry.AsSpan(6, 4), "av01");
            writer.Write(entry);
            writer.EndBox();
            index++;
        }

        writer.EndBox();
    }

    private static void WriteReferences(
        IsoBoxWriter writer,
        DependencyReference[] references)
    {
        writer.BeginFullBox("iref", 0, 0);
        int referenceIndex = 0;
        while (referenceIndex < references.Length)
        {
            DependencyReference reference = references[referenceIndex];
            writer.BeginBox("dimg");
            WriteUInt16(writer, (ushort)reference.FromItemId);
            WriteUInt16(writer, (ushort)reference.TargetIds.Length);
            int targetIndex = 0;
            while (targetIndex < reference.TargetIds.Length)
            {
                WriteUInt16(writer, (ushort)reference.TargetIds[targetIndex]);
                targetIndex++;
            }

            writer.EndBox();
            referenceIndex++;
        }

        writer.EndBox();
    }

    private static void WriteUInt16(IsoBoxWriter writer, ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 131072;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 256;
        limits.MaxItemCount = 64;
        limits.MaxReferenceCount = 128;
        return limits;
    }

    private class DependencyReference
    {
        internal uint FromItemId;
        internal uint[] TargetIds;

        internal DependencyReference(uint fromItemId, uint[] targetIds)
        {
            FromItemId = fromItemId;
            TargetIds = targetIds;
        }
    }
}
