using raw;

namespace raw.Tests;

public class TiffWriterTests
{
    [Test]
    public void ClassicLittleEndianRoundTripPreservesUnknownValuesAndOpaqueBlocks()
    {
        byte[] unknownInline = new byte[] { 0xa1, 0xb2, 0xc3 };
        byte[] unknownOutOfLine = new byte[] { 0xde, 0xad, 0xbe, 0xef, 0x11, 0x22 };
        byte[] strip = new byte[] { 3, 1, 4, 1, 5 };
        byte[] opaque = new byte[] { 0x43, 0x41, 0x4d, 0x00, 0xff };
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            UndefinedEntry(65000, unknownOutOfLine),
            LongEntry(330, new uint[] { 0 }, RawByteOrder.LittleEndian, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/SubIFD0") }),
            LongEntry(273, new uint[] { 0 }, RawByteOrder.LittleEndian, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "strip-0") }),
            UndefinedEntry(64999, unknownInline),
            LongEntry(256, new uint[] { 37 }, RawByteOrder.LittleEndian, null)
        }, null);
        TiffLayoutIfdInput child = new TiffLayoutIfdInput("IFD0/SubIFD0", new TiffLayoutEntryInput[]
        {
            LongEntry(256, new uint[] { 11 }, RawByteOrder.LittleEndian, null)
        }, null);
        TiffLayoutRequest request = new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { root, child }, new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("strip-0", TiffLayoutBlockKind.Strip, strip.Length, strip),
            new TiffLayoutBlockInput("maker-note", TiffLayoutBlockKind.Opaque, opaque.Length, opaque)
        });

        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        byte[] encoded = TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
        TiffIfdGraph parsed = Parse(encoded);
        TiffIfd? parsedRoot = parsed.FindIfd("IFD0");
        TiffLayoutBlockPlan? stripPlan = (plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(block => block.Name == "strip-0");
        TiffLayoutBlockPlan opaquePlan = plan.Blocks.Single(block => block.Name == "maker-note");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(plan.PredictedSize));
            Assert.That(parsed.Header.BigTiff, Is.False);
            Assert.That(parsed.Header.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(parsed.Ifds.Select(ifd => ifd.Path), Is.EqualTo(new string[] { "IFD0", "IFD0/SubIFD0" }));
            Assert.That((parsedRoot ?? throw new System.InvalidOperationException("A required value is unavailable.")).Entries.Select(entry => entry.Tag), Is.Ordered);
            Assert.That((parsedRoot.FindEntry(64999) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(unknownInline));
            Assert.That((parsedRoot.FindEntry(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(unknownOutOfLine));
            Assert.That((parsedRoot.FindEntry(273) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt32(), Is.EqualTo(stripPlan.Offset));
            Assert.That(((parsed.FindIfd("IFD0/SubIFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(256) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt32(), Is.EqualTo(11));
            Assert.That(encoded.AsSpan((int)stripPlan.Offset, strip.Length).ToArray(), Is.EqualTo(strip));
            Assert.That(encoded.AsSpan((int)opaquePlan.Offset, opaque.Length).ToArray(), Is.EqualTo(opaque));
        });
    }

    [Test]
    public void BigTiffBigEndianWritesEightByteStructuresAndPreservesInlineUnknownBytes()
    {
        byte[] tile = new byte[] { 9, 8, 7, 6 };
        byte[] unknown = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80 };
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            UndefinedEntry(65001, unknown),
            Long8Entry(324, new ulong[] { 0 }, RawByteOrder.BigEndian, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "tile-0") }),
            LongEntry(256, new uint[] { 0x01020304 }, RawByteOrder.BigEndian, null)
        }, null);
        TiffLayoutRequest request = new TiffLayoutRequest(true, RawByteOrder.BigEndian, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("tile-0", TiffLayoutBlockKind.Tile, tile.Length, tile)
        });

        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        byte[] encoded = TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
        TiffIfdGraph parsed = Parse(encoded);
        TiffIfd? parsedRoot = parsed.FindIfd("IFD0");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.AsSpan(0, 4).ToArray(), Is.EqualTo(new byte[] { 0x4d, 0x4d, 0x00, 0x2b }));
            Assert.That(parsed.Header.BigTiff, Is.True);
            Assert.That(parsed.Header.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(((parsedRoot ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(256) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt32(), Is.EqualTo(0x01020304));
            Assert.That((parsedRoot.FindEntry(324) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt64(), Is.EqualTo((plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Offset));
            Assert.That((parsedRoot.FindEntry(65001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Inline, Is.True);
            Assert.That((parsedRoot.FindEntry(65001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(unknown));
            Assert.That(encoded.AsSpan((int)plan.Blocks[0].Offset, tile.Length).ToArray(), Is.EqualTo(tile));
        });
    }

    [Test]
    public void PreplannedNonSeekableOutputMatchesSeekableOutputAndLeavesStreamsOpen()
    {
        TiffLayoutPlan plan = CreateSimplePlan();
        MemoryStream seekable = new MemoryStream();
        WriteOnlyNonSeekableStream nonSeekable = new WriteOnlyNonSeekableStream();

        TiffWriter.Write(seekable, plan, new RawLimits(), CancellationToken.None);
        TiffWriter.Write(nonSeekable, plan, new RawLimits(), CancellationToken.None);
        seekable.WriteByte(0xee);
        nonSeekable.WriteByte(0xee);

        Assert.Multiple(() =>
        {
            Assert.That(seekable.ToArray().AsSpan(0, (int)plan.PredictedSize).ToArray(), Is.EqualTo(nonSeekable.ToArray().AsSpan(0, (int)plan.PredictedSize).ToArray()));
            Assert.That(seekable.Length, Is.EqualTo(plan.PredictedSize + 1));
            Assert.That(nonSeekable.ToArray().Length, Is.EqualTo(plan.PredictedSize + 1));
        });
    }

    [Test]
    public void SeekableOutputStartsAtCurrentPositionAndUsesTiffRelativeOffsets()
    {
        TiffLayoutPlan plan = CreateSimplePlan();
        byte[] expected = TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
        MemoryStream destination = new MemoryStream();
        byte[] prefix = new byte[] { 7, 6, 5, 4, 3 };
        destination.Write(prefix, 0, prefix.Length);

        TiffWriter.Write(destination, plan, new RawLimits(), CancellationToken.None);
        byte[] actual = destination.ToArray();
        TiffIfdGraph parsed = TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(actual), prefix.Length, plan.PredictedSize, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(actual.AsSpan(0, prefix.Length).ToArray(), Is.EqualTo(prefix));
            Assert.That(actual.AsSpan(prefix.Length).ToArray(), Is.EqualTo(expected));
            Assert.That(parsed.Header.BaseOffset, Is.EqualTo(prefix.Length));
            Assert.That(((parsed.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        });
    }

    [Test]
    public void InvalidDestinationsPlansLimitsAndCancellationAreRejectedBeforeCompletion()
    {
        TiffLayoutPlan plan = CreateSimplePlan();
        RawLimits small = new RawLimits();
        small.MaxFileSizeBytes = plan.PredictedSize - 1;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        MemoryStream readOnly = new MemoryStream(new byte[16], false);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => TiffWriter.Write(null, plan, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => TiffWriter.Write(readOnly, plan, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => TiffWriter.Write(new MemoryStream(), null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawLimitException>(() => TiffWriter.Write(new MemoryStream(), plan, small, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => TiffWriter.Write(new MemoryStream(), plan, new RawLimits(), cancellation.Token));
        });
    }

    [Test]
    public void CorruptPreplannedOffsetsAndMissingBlockBytesAreRejected()
    {
        TiffLayoutPlan badOffset = CreateSimplePlan();
        (badOffset.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Offset = (ulong)badOffset.PredictedSize + 2;
        TiffLayoutPlan missingBytes = CreateSimplePlan();
        (missingBytes.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Bytes = null;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => TiffWriter.Write(new MemoryStream(), badOffset, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => TiffWriter.Write(new MemoryStream(), missingBytes, new RawLimits(), CancellationToken.None));
        });
    }

    static TiffLayoutPlan CreateSimplePlan()
    {
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            UndefinedEntry(65000, new byte[] { 1, 2, 3, 4, 5, 6 }),
            LongEntry(273, new uint[] { 0 }, RawByteOrder.LittleEndian, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "payload") })
        }, null);
        TiffLayoutRequest request = new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("payload", TiffLayoutBlockKind.Opaque, 3, new byte[] { 8, 9, 10 })
        });
        return TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
    }

    static TiffLayoutEntryInput LongEntry(ushort tag, uint[] values, RawByteOrder byteOrder, TiffLayoutReference[]? references)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, (ulong)values.Length, bytes, references);
    }

    static TiffLayoutEntryInput Long8Entry(ushort tag, ulong[] values, RawByteOrder byteOrder, TiffLayoutReference[] references)
    {
        byte[] bytes = new byte[values.Length * 8];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long8, (ulong)values.Length, bytes, references);
    }

    static TiffLayoutEntryInput UndefinedEntry(ushort tag, byte[] bytes)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Undefined, (ulong)bytes.Length, bytes, null);
    }

    static TiffIfdGraph Parse(byte[] source)
    {
        return TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits()));
    }

    class WriteOnlyNonSeekableStream : Stream
    {
        MemoryStream Inner;

        public WriteOnlyNonSeekableStream()
        {
            Inner = new MemoryStream();
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Inner.Write(buffer, offset, count);
        }

        public byte[] ToArray()
        {
            return Inner.ToArray();
        }
    }
}
