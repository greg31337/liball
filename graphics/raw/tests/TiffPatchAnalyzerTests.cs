using raw;

namespace raw.Tests;

public class TiffPatchAnalyzerTests
{
    [Test]
    public void ClassicInlineAndOutOfLineEditsProduceFixedAbsoluteRegionsThatRoundTrip()
    {
        PatchFixture fixture = CreateMetadataFixture(false, RawByteOrder.LittleEndian, 5);
        byte[] orientation = UInt16Bytes(6, RawByteOrder.LittleEndian);
        byte[] unknown = new byte[] { 9, 8, 7, 6, 5, 4 };
        TiffPatchEdit[] edits = new TiffPatchEdit[]
        {
            new TiffPatchEdit("IFD0", 65000, TiffFieldType.Undefined, 6, unknown),
            new TiffPatchEdit("IFD0", 274, TiffFieldType.Short, 1, orientation)
        };

        TiffPatchPlan plan = TiffPatchAnalyzer.Plan(fixture.Graph, edits, new RawLimits(), CancellationToken.None);
        byte[] patched = Apply(fixture.Bytes, plan);
        TiffIfdGraph reparsed = Parse(patched, fixture.ContainerStart, fixture.ContainerLength);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContainerStart, Is.EqualTo(5));
            Assert.That(plan.OriginalLength, Is.EqualTo(fixture.ContainerLength));
            Assert.That(plan.Regions.Length, Is.EqualTo(2));
            Assert.That(plan.Regions.Select(region => region.Offset), Is.Ordered);
            Assert.That(plan.Regions[0].StorageKind, Is.EqualTo(TiffPatchStorageKind.Inline));
            Assert.That(plan.Regions[1].StorageKind, Is.EqualTo(TiffPatchStorageKind.OutOfLine));
            Assert.That(patched.Length, Is.EqualTo(fixture.Bytes.Length));
            Assert.That(((reparsed.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(274) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt16(), Is.EqualTo(6));
            Assert.That(((reparsed.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(unknown));
        });
    }

    [Test]
    public void BigTiffBigEndianUsesEightByteSlotLocationsAndCopiesPlannedBytes()
    {
        PatchFixture fixture = CreateMetadataFixture(true, RawByteOrder.BigEndian, 0);
        byte[] orientation = UInt16Bytes(8, RawByteOrder.BigEndian);
        byte[] unknown = new byte[] { 10, 11, 12, 13, 14, 15, 16, 17, 18 };
        TiffPatchEdit inlineEdit = new TiffPatchEdit("IFD0", 274, TiffFieldType.Short, 1, orientation);
        TiffPatchEdit outsideEdit = new TiffPatchEdit("IFD0", 65000, TiffFieldType.Undefined, 9, unknown);

        TiffPatchPlan plan = TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[] { outsideEdit, inlineEdit }, new RawLimits(), CancellationToken.None);
        long expectedInlineOffset = ((fixture.Graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(274) ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryOffset + 12;
        orientation[0] = 0xff;
        unknown[0] = 0xff;
        byte[] patched = Apply(fixture.Bytes, plan);
        TiffIfdGraph reparsed = Parse(patched, 0, patched.Length);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Regions.Single(region => region.Tag == 274).Offset, Is.EqualTo(expectedInlineOffset));
            Assert.That(plan.Regions.Single(region => region.Tag == 274).Bytes, Is.EqualTo(UInt16Bytes(8, RawByteOrder.BigEndian)));
            Assert.That(plan.Regions.Single(region => region.Tag == 65000).Bytes[0], Is.EqualTo(10));
            Assert.That(reparsed.Header.BigTiff, Is.True);
            Assert.That(reparsed.Header.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(((reparsed.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(274) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt16(), Is.EqualTo(8));
            Assert.That(((reparsed.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes[0], Is.EqualTo(10));
        });
    }

    [Test]
    public void UnchangedReferenceEditIsANoOpButChangedIfdReferenceIsRejected()
    {
        PatchFixture fixture = CreateReferenceFixture();
        TiffEntry? pointer = (fixture.Graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(330);
        byte[] same = (pointer ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes.ToArray();
        byte[] changed = UInt32Bytes(pointer.GetUInt32() + 2, RawByteOrder.LittleEndian);

        TiffPatchPlan noOp = TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[]
        {
            new TiffPatchEdit("IFD0", 330, TiffFieldType.Long, 1, same)
        }, new RawLimits(), CancellationToken.None);

        RawWriteException? rejected = Assert.Throws<RawWriteException>(() => TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[]
        {
            new TiffPatchEdit("IFD0", 330, TiffFieldType.Long, 1, changed)
        }, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(noOp.Regions, Is.Empty);
            Assert.That((rejected ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.WritePlanningFailed));
            Assert.That(rejected.Message, Does.Contain("referenced storage"));
        });
    }

    [Test]
    public void AddRemoveTypeCountAndStorageGrowthEditsAreRejected()
    {
        PatchFixture fixture = CreateMetadataFixture(false, RawByteOrder.LittleEndian, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 60000, TiffFieldType.Undefined, 1, new byte[] { 1 })));
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 65000, TiffFieldType.Undefined, 0, Array.Empty<byte>())));
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 274, TiffFieldType.Byte, 2, new byte[] { 1, 2 })));
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 65000, TiffFieldType.Undefined, 7, new byte[7])));
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("missing", 274, TiffFieldType.Short, 1, UInt16Bytes(2, RawByteOrder.LittleEndian))));
        });
    }

    [Test]
    public void PayloadOffsetsLengthsAndImageStorageDescriptorsCannotBeChanged()
    {
        PatchFixture fixture = CreateStorageFixture();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 513, TiffFieldType.Long, 1, UInt32Bytes(2, RawByteOrder.LittleEndian))));
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 514, TiffFieldType.Long, 1, UInt32Bytes(5, RawByteOrder.LittleEndian))));
            Assert.Throws<RawWriteException>(() => Analyze(fixture, new TiffPatchEdit("IFD0", 258, TiffFieldType.Short, 1, UInt16Bytes(12, RawByteOrder.LittleEndian))));
        });
    }

    [Test]
    public void InvalidGraphsDuplicateEditsLimitsAndCancellationAreRejected()
    {
        PatchFixture fixture = CreateMetadataFixture(false, RawByteOrder.LittleEndian, 0);
        TiffPatchEdit edit = new TiffPatchEdit("IFD0", 274, TiffFieldType.Short, 1, UInt16Bytes(2, RawByteOrder.LittleEndian));
        RawLimits limits = new RawLimits();
        limits.MaxFileSizeBytes = fixture.ContainerLength - 1;
        PatchFixture invalid = CreateMetadataFixture(false, RawByteOrder.LittleEndian, 0);
        ((invalid.Graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(274) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Count = 2;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => TiffPatchAnalyzer.Plan(null, new TiffPatchEdit[] { edit }, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => TiffPatchAnalyzer.Plan(fixture.Graph, null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => TiffPatchAnalyzer.Plan(invalid.Graph, new TiffPatchEdit[] { edit }, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[] { edit, edit }, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawLimitException>(() => TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[] { edit }, limits, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[] { edit }, new RawLimits(), cancellation.Token));
        });
    }

    static TiffPatchPlan Analyze(PatchFixture fixture, TiffPatchEdit edit)
    {
        return TiffPatchAnalyzer.Plan(fixture.Graph, new TiffPatchEdit[] { edit }, new RawLimits(), CancellationToken.None);
    }

    static PatchFixture CreateMetadataFixture(bool bigTiff, RawByteOrder byteOrder, int prefixLength)
    {
        int unknownLength = bigTiff ? 9 : 6;
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            ShortEntry(274, 1, byteOrder),
            new TiffLayoutEntryInput(65000, TiffFieldType.Undefined, (ulong)unknownLength, Enumerable.Range(1, unknownLength).Select(value => (byte)value).ToArray(), null)
        }, null);
        TiffLayoutRequest request = new TiffLayoutRequest(bigTiff, byteOrder, "IFD0", new TiffLayoutIfdInput[] { root }, Array.Empty<TiffLayoutBlockInput>());
        return WriteFixture(request, prefixLength);
    }

    static PatchFixture CreateReferenceFixture()
    {
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            new TiffLayoutEntryInput(330, TiffFieldType.Long, 1, new byte[4], new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/SubIFD0") })
        }, null);
        TiffLayoutIfdInput child = new TiffLayoutIfdInput("IFD0/SubIFD0", Array.Empty<TiffLayoutEntryInput>(), null);
        return WriteFixture(new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { root, child }, Array.Empty<TiffLayoutBlockInput>()), 0);
    }

    static PatchFixture CreateStorageFixture()
    {
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            new TiffLayoutEntryInput(513, TiffFieldType.Long, 1, new byte[4], new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "preview") }),
            new TiffLayoutEntryInput(514, TiffFieldType.Long, 1, UInt32Bytes(4, RawByteOrder.LittleEndian), null),
            ShortEntry(258, 8, RawByteOrder.LittleEndian)
        }, null);
        TiffLayoutBlockInput block = new TiffLayoutBlockInput("preview", TiffLayoutBlockKind.Opaque, 4, new byte[] { 1, 2, 3, 4 });
        return WriteFixture(new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[] { block }), 0);
    }

    static PatchFixture WriteFixture(TiffLayoutRequest request, int prefixLength)
    {
        TiffLayoutPlan layout = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        byte[] tiff = TiffWriter.WriteToArray(layout, new RawLimits(), CancellationToken.None);
        byte[] source = new byte[prefixLength + tiff.Length];
        for (int index = 0; index < prefixLength; index++)
        {
            source[index] = 0xa5;
        }
        tiff.CopyTo(source, prefixLength);
        TiffIfdGraph graph = Parse(source, prefixLength, tiff.Length);
        return new PatchFixture(source, graph, prefixLength, tiff.Length);
    }

    static TiffIfdGraph Parse(byte[] source, int start, long length)
    {
        return TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(source), start, length, new RawLimits()));
    }

    static byte[] Apply(byte[] source, TiffPatchPlan plan)
    {
        byte[] result = source.ToArray();
        foreach (TiffPatchRegion region in plan.Regions)
        {
            region.Bytes.CopyTo(result, (int)region.Offset);
        }
        return result;
    }

    static TiffLayoutEntryInput ShortEntry(ushort tag, ushort value, RawByteOrder byteOrder)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, 1, UInt16Bytes(value, byteOrder), null);
    }

    static byte[] UInt16Bytes(ushort value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, byteOrder);
        return bytes;
    }

    static byte[] UInt32Bytes(uint value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, byteOrder);
        return bytes;
    }

    class PatchFixture
    {
        public byte[] Bytes;
        public TiffIfdGraph Graph;
        public int ContainerStart;
        public long ContainerLength;

        public PatchFixture(byte[] bytes, TiffIfdGraph graph, int containerStart, long containerLength)
        {
            Bytes = bytes;
            Graph = graph;
            ContainerStart = containerStart;
            ContainerLength = containerLength;
        }
    }
}
