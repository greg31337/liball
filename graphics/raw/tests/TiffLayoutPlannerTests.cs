using raw;

namespace raw.Tests;

public class TiffLayoutPlannerTests
{
    [Test]
    public void ClassicLayoutSortsTagsResolvesIfdAndStripReferencesAndPlacesOpaqueBlocks()
    {
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            LongEntry(330, new uint[] { 0 }, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/SubIFD0") }),
            LongEntry(279, new uint[] { 3, 5 }, null),
            AsciiEntry(270, "hello\0"),
            LongEntry(256, new uint[] { 17 }, null),
            LongEntry(273, new uint[] { 0, 0 }, new TiffLayoutReference[]
            {
                new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "strip-0"),
                new TiffLayoutReference(1, TiffLayoutReferenceKind.Block, "strip-1")
            })
        }, "IFD0/SubIFD0");
        TiffLayoutIfdInput child = new TiffLayoutIfdInput("IFD0/SubIFD0", new TiffLayoutEntryInput[] { LongEntry(256, new uint[] { 4 }, null) }, null);
        TiffLayoutBlockInput[] blocks = new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("strip-0", TiffLayoutBlockKind.Strip, 3, new byte[] { 1, 2, 3 }),
            new TiffLayoutBlockInput("strip-1", TiffLayoutBlockKind.Strip, 5, new byte[] { 4, 5, 6, 7, 8 }),
            new TiffLayoutBlockInput("maker-note", TiffLayoutBlockKind.Opaque, 4, new byte[] { 9, 10, 11, 12 })
        };
        TiffLayoutRequest request = new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { root, child }, blocks);

        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        TiffLayoutIfdPlan rootPlan = (plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable."))[0];

        Assert.Multiple(() =>
        {
            Assert.That(plan.HeaderLength, Is.EqualTo(8));
            Assert.That(plan.Alignment, Is.EqualTo(2));
            Assert.That(plan.FirstIfdOffset, Is.EqualTo(8));
            Assert.That(plan.PredictedSize, Is.EqualTo(128));
            Assert.That(((rootPlan.Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(entry => entry.Tag) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new ushort[] { 256, 270, 273, 279, 330 }));
            Assert.That(rootPlan.Offset, Is.EqualTo(8));
            Assert.That(rootPlan.Length, Is.EqualTo(66));
            Assert.That(rootPlan.NextIfdOffset, Is.EqualTo(74));
            Assert.That(plan.Ifds[1].Offset, Is.EqualTo(74));
            Assert.That(rootPlan.Entries.Single(entry => entry.Tag == 270).ValueOffset, Is.EqualTo(92));
            Assert.That(rootPlan.Entries.Single(entry => entry.Tag == 273).ValueOffset, Is.EqualTo(98));
            Assert.That(rootPlan.Entries.Single(entry => entry.Tag == 279).ValueOffset, Is.EqualTo(106));
            Assert.That(ReadUInt32(rootPlan.Entries.Single(entry => entry.Tag == 330).ValueBytes, 0, RawByteOrder.LittleEndian), Is.EqualTo(74));
            Assert.That(ReadUInt32(rootPlan.Entries.Single(entry => entry.Tag == 273).ValueBytes, 0, RawByteOrder.LittleEndian), Is.EqualTo(114));
            Assert.That(ReadUInt32(rootPlan.Entries.Single(entry => entry.Tag == 273).ValueBytes, 4, RawByteOrder.LittleEndian), Is.EqualTo(118));
            Assert.That(((plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(block => block.Offset) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new ulong[] { 114, 118, 124 }));
            Assert.That(plan.Blocks.Select(block => block.Kind), Is.EqualTo(new TiffLayoutBlockKind[] { TiffLayoutBlockKind.Strip, TiffLayoutBlockKind.Strip, TiffLayoutBlockKind.Opaque }));
            Assert.That(plan.Blocks.All(block => block.Offset % 2 == 0), Is.True);
        });
    }

    [Test]
    public void BigTiffUsesEightByteCountsSlotsOffsetsAndAlignment()
    {
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            Ifd8Entry(330, new ulong[] { 0 }, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/SubIFD0") }),
            UndefinedEntry(270, Enumerable.Range(0, 9).Select(value => (byte)value).ToArray()),
            LongEntry(256, new uint[] { 5 }, null)
        }, null);
        TiffLayoutIfdInput child = new TiffLayoutIfdInput("IFD0/SubIFD0", new TiffLayoutEntryInput[] { LongEntry(256, new uint[] { 2 }, null) }, null);
        TiffLayoutBlockInput tile = new TiffLayoutBlockInput("tile-0", TiffLayoutBlockKind.Tile, 5, new byte[] { 1, 2, 3, 4, 5 });
        TiffLayoutRequest request = new TiffLayoutRequest(true, RawByteOrder.BigEndian, "IFD0", new TiffLayoutIfdInput[] { root, child }, new TiffLayoutBlockInput[] { tile });

        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        TiffLayoutEntryPlan? pointer = ((plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(entry => entry.Tag == 330);

        Assert.Multiple(() =>
        {
            Assert.That(plan.HeaderLength, Is.EqualTo(16));
            Assert.That(plan.Alignment, Is.EqualTo(8));
            Assert.That(plan.FirstIfdOffset, Is.EqualTo(16));
            Assert.That(plan.Ifds.Select(directory => directory.Offset), Is.EqualTo(new ulong[] { 16, 96 }));
            Assert.That(plan.Ifds[0].Length, Is.EqualTo(76));
            Assert.That(((plan.Ifds[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(entry => entry.Tag == 270).ValueOffset), Is.EqualTo(136));
            Assert.That(pointer.Inline, Is.True);
            Assert.That(EndianReader.ReadUInt64(pointer.ValueBytes, RawByteOrder.BigEndian), Is.EqualTo(96));
            Assert.That(((plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single().Offset), Is.EqualTo(152));
            Assert.That(plan.PredictedSize, Is.EqualTo(157));
            Assert.That(plan.Ifds.All(directory => directory.Offset % 8 == 0), Is.True);
        });
    }

    [Test]
    public void RepeatedPlanningIsByteAndOffsetDeterministicWithoutMutatingInputs()
    {
        byte[] original = new byte[8];
        TiffLayoutEntryInput offsets = LongEntry(273, new uint[] { 0, 0 }, new TiffLayoutReference[]
        {
            new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "first"),
            new TiffLayoutReference(1, TiffLayoutReferenceKind.Block, "second")
        });
        original.CopyTo(offsets.ValueBytes, 0);
        TiffLayoutRequest request = new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0",
            new TiffLayoutIfdInput[] { new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { offsets, LongEntry(256, new uint[] { 1 }, null) }, null) },
            new TiffLayoutBlockInput[]
            {
                new TiffLayoutBlockInput("first", TiffLayoutBlockKind.Strip, 1, new byte[] { 1 }),
                new TiffLayoutBlockInput("second", TiffLayoutBlockKind.Opaque, 2, new byte[] { 2, 3 })
            });

        TiffLayoutPlan first = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        TiffLayoutPlan second = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second.PredictedSize, Is.EqualTo(first.PredictedSize));
            Assert.That(((second.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(directory => directory.Offset) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(((first.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(directory => directory.Offset) ?? throw new System.InvalidOperationException("A required value is unavailable."))));
            Assert.That(((second.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(block => block.Offset) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(((first.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(block => block.Offset) ?? throw new System.InvalidOperationException("A required value is unavailable."))));
            Assert.That(((second.Ifds[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(entry => entry.Tag == 273).ValueBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(((first.Ifds[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(entry => entry.Tag == 273).ValueBytes ?? throw new System.InvalidOperationException("A required value is unavailable."))));
            Assert.That(offsets.ValueBytes, Is.EqualTo(original));
        });
    }

    [Test]
    public void InlineBoundaryDependsOnClassicOrBigTiffValueSlotWidth()
    {
        TiffLayoutEntryInput eightBytes = UndefinedEntry(700, new byte[8]);
        TiffLayoutIfdInput directory = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { eightBytes }, null);

        TiffLayoutPlan classic = TiffLayoutPlanner.Plan(new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { directory }, new TiffLayoutBlockInput[0]), new RawLimits(), CancellationToken.None);
        TiffLayoutPlan big = TiffLayoutPlanner.Plan(new TiffLayoutRequest(true, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { directory }, new TiffLayoutBlockInput[0]), new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(((classic.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Inline, Is.False);
            Assert.That((classic.Ifds[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].ValueOffset, Is.EqualTo(26));
            Assert.That(classic.PredictedSize, Is.EqualTo(34));
            Assert.That(((big.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Inline, Is.True);
            Assert.That((big.Ifds[0].Entries ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].ValueOffset, Is.Zero);
            Assert.That(big.PredictedSize, Is.EqualTo(52));
        });
    }

    [Test]
    public void ClassicLayoutRejectsOffsetsOrFieldFormsThatNeedBigTiff()
    {
        TiffLayoutIfdInput directory = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { LongEntry(256, new uint[] { 1 }, null) }, null);
        TiffLayoutBlockInput huge = new TiffLayoutBlockInput("huge", TiffLayoutBlockKind.Opaque, (long)uint.MaxValue, null);
        TiffLayoutRequest oversized = new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { directory }, new TiffLayoutBlockInput[] { huge });
        RawLimits largeLimits = new RawLimits();
        largeLimits.MaxFileSizeBytes = 8L * 1024 * 1024 * 1024;
        TiffLayoutIfdInput long8Directory = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { Ifd8Entry(330, new ulong[] { 0 }, null) }, null);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => TiffLayoutPlanner.Plan(oversized, largeLimits, CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { long8Directory }, new TiffLayoutBlockInput[0]), new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void InvalidGraphsValuesReferencesBlocksLimitsAndCancellationAreRejected()
    {
        TiffLayoutEntryInput badLength = new TiffLayoutEntryInput(256, TiffFieldType.Long, 2, new byte[4], null);
        TiffLayoutEntryInput unknownReference = LongEntry(273, new uint[] { 0 }, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "missing") });
        TiffLayoutIfdInput duplicates = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { LongEntry(256, new uint[] { 1 }, null), LongEntry(256, new uint[] { 2 }, null) }, null);
        TiffLayoutIfdInput badValue = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { badLength }, null);
        TiffLayoutIfdInput badReference = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { unknownReference }, null);
        TiffLayoutIfdInput largeMetadata = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { UndefinedEntry(700, new byte[64]) }, null);
        RawLimits limits = new RawLimits();
        limits.MaxMetadataBytes = 32;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(Request(duplicates), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(Request(badValue), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(Request(badReference), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "missing", new TiffLayoutIfdInput[] { badReference }, new TiffLayoutBlockInput[0]), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffLayoutPlanner.Plan(new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[0], null) }, new TiffLayoutBlockInput[] { new TiffLayoutBlockInput("bad", TiffLayoutBlockKind.Opaque, 2, new byte[1]) }), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawLimitException>(() => TiffLayoutPlanner.Plan(Request(largeMetadata), limits, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => TiffLayoutPlanner.Plan(Request(new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[0], null)), new RawLimits(), cancellation.Token));
        });
    }

    private static TiffLayoutRequest Request(TiffLayoutIfdInput directory)
    {
        return new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { directory }, new TiffLayoutBlockInput[0]);
    }

    private static TiffLayoutEntryInput LongEntry(ushort tag, uint[] values, TiffLayoutReference[]? references)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], RawByteOrder.LittleEndian);
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, (ulong)values.Length, bytes, references);
    }

    private static TiffLayoutEntryInput Ifd8Entry(ushort tag, ulong[] values, TiffLayoutReference[]? references)
    {
        byte[] bytes = new byte[values.Length * 8];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(index * 8, 8), values[index], RawByteOrder.BigEndian);
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Ifd8, (ulong)values.Length, bytes, references);
    }

    private static TiffLayoutEntryInput AsciiEntry(ushort tag, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Ascii, (ulong)bytes.Length, bytes, null);
    }

    private static TiffLayoutEntryInput UndefinedEntry(ushort tag, byte[] value)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Undefined, (ulong)value.Length, value, null);
    }

    private static uint ReadUInt32(byte[]? bytes, int offset, RawByteOrder order)
    {
        return EndianReader.ReadUInt32(bytes.AsSpan(offset, 4), order);
    }
}
