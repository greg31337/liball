using raw;

namespace raw.Tests;

public class TiffValidatorTests
{
    [Test]
    public void ValidStrippedImageHasNoStructuralFindings()
    {
        TiffIfdGraph graph = CreateValidStripGraph();

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(report.Strict, Is.True);
            Assert.That(report.Messages, Is.Empty);
            Assert.That(report.IsValid(), Is.True);
        });
    }

    [Test]
    public void ParsedImageValidatesWithoutReadingOrDecodingPayload()
    {
        byte[] source = BuildParsedStrip();
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits());
        TiffIfdGraph graph = TiffParser.Parse(input);

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Images.Length, Is.EqualTo(1));
            Assert.That(graph.ContainerStart, Is.EqualTo(0));
            Assert.That(graph.ContainerLength, Is.EqualTo(120));
            Assert.That(report.Messages, Is.Empty);
            Assert.That(input.GetPosition(), Is.EqualTo(8));
        });
    }

    [Test]
    public void InvalidTypesCountsDuplicatesAndEntryStorageAreAllReported()
    {
        TiffIfdGraph graph = CreateValidStripGraph();
        TiffIfd ifd = graph.Ifds[0];
        ifd.Entries[0].FieldType = TiffFieldType.SignedLong;
        ifd.Entries[0].Count = 2;
        ifd.Entries[0].EntryOffset = 11;
        ifd.Entries[1].Tag = TiffImageData.ImageWidthTag;
        graph.Images = TiffImageData.Describe(graph, new RawLimits());

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidOffset && message.Message.Contains("IFD position"))), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidLength && message.Message.Contains("retained TIFF value length")), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidContainer && message.Message.Contains("inline boundary")), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidMetadata && message.Message.Contains("SHORT or LONG")), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidMetadata && message.Message.Contains("duplicate tag")), Is.True);
            Assert.That(report.IsValid(), Is.False);
        });
    }

    [Test]
    public void MissingRequiredImageTagsAndMixedStorageAreReported()
    {
        TiffEntry width = CreateEntry(256, TiffFieldType.Long, new ulong[] { 10 }, 10, 0, false);
        TiffEntry strips = CreateEntry(273, TiffFieldType.Long, new ulong[] { 100 }, 22, 0, false);
        TiffEntry tiles = CreateEntry(324, TiffFieldType.Long, new ulong[] { 120 }, 34, 0, false);
        TiffIfd ifd = new TiffIfd("IFD0", 8, 8, 0, new TiffEntry[] { width, strips, tiles }, 0);
        TiffIfdGraph graph = CreateGraph(ifd, 160, false);

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "IFD0/ImageLength")), Is.True);
            Assert.That(report.Messages.Any(message => message.Context == "IFD0/PhotometricInterpretation"), Is.True);
            Assert.That(report.Messages.Any(message => message.Context == "IFD0/StripByteCounts"), Is.True);
            Assert.That(report.Messages.Any(message => message.Context == "IFD0/TileWidth"), Is.True);
            Assert.That(report.Messages.Any(message => message.Message.Contains("both strip and tile")), Is.True);
        });
    }

    [Test]
    public void StripAndTileArrayCoverageUsesGeometryAndPlanarConfiguration()
    {
        TiffIfdGraph stripGraph = CreateValidStripGraph();
        TiffIfd stripIfd = stripGraph.Ifds[0];
        ReplaceEntry(stripIfd, CreateEntry(279, TiffFieldType.Long, new ulong[] { 50 }, EntryOffset(stripIfd, 279), 0, false));
        stripGraph.Images = TiffImageData.Describe(stripGraph, new RawLimits());
        RawValidationReport stripReport = TiffValidator.Validate(stripGraph, true);

        TiffIfd tileIfd = CreateTileIfd(8, 8);
        TiffIfdGraph tileGraph = CreateGraph(tileIfd, 1200, false);
        tileGraph.Images = TiffImageData.Describe(tileGraph, new RawLimits());
        RawValidationReport tileReport = TiffValidator.Validate(tileGraph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((stripReport.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidLength && message.Message.Contains("different lengths"))), Is.True);
            Assert.That(stripReport.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidDescriptor && message.Message.Contains("cover")), Is.True);
            Assert.That(((tileReport.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidDescriptor && message.Message.Contains("tile arrays"))), Is.True);
        });
    }

    [Test]
    public void OutOfBoundsMisalignedAndOverlappingPayloadRangesAreReported()
    {
        TiffIfdGraph graph = CreateValidStripGraph();
        TiffIfd ifd = graph.Ifds[0];
        ReplaceEntry(ifd, CreateEntry(273, TiffFieldType.Long, new ulong[] { 301, 320, 390 }, EntryOffset(ifd, 273), 200, false));
        ReplaceEntry(ifd, CreateEntry(279, TiffFieldType.Long, new ulong[] { 50, 50, 20 }, EntryOffset(ifd, 279), 212, false));
        graph.Images = TiffImageData.Describe(graph, new RawLimits());

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidOffset && message.Context == "IFD0/Strip0" && message.Message.Contains("alignment"))), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidContainer && message.Context == "IFD0/Strip1" && message.Message.Contains("overlaps")), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidOffset && message.Context == "IFD0/Strip2" && message.Message.Contains("beyond")), Is.True);
        });
    }

    [Test]
    public void OutOfLineValuesParticipateInBoundsAlignmentAndOverlapChecks()
    {
        TiffIfdGraph graph = CreateValidStripGraph();
        TiffIfd ifd = graph.Ifds[0];
        TiffEntry? offsets = ifd.FindEntry(273);
        TiffEntry? counts = ifd.FindEntry(279);
        (offsets ?? throw new System.InvalidOperationException("A required value is unavailable.")).ValueOffset = 99;
        (counts ?? throw new System.InvalidOperationException("A required value is unavailable.")).ValueOffset = 102;

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "IFD0/Tag0x0111" && message.Message.Contains("alignment"))), Is.True);
            Assert.That(report.Messages.Any(message => message.Context == "IFD0/Tag0x0117" && message.Message.Contains("overlaps")), Is.True);
        });
    }

    [Test]
    public void StrictAndTolerantReportsClassifyTrailingDataDeterministically()
    {
        TiffIfdGraph graph = CreateValidStripGraph();
        graph.ContainerLength = 420;

        RawValidationReport strict = TiffValidator.Validate(graph, true);
        RawValidationReport tolerant = TiffValidator.Validate(graph, false);
        RawValidationMessage? strictTrailing = (strict.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(message => message.Context == "TIFF/TrailingData");
        RawValidationMessage? tolerantTrailing = (tolerant.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(message => message.Context == "TIFF/TrailingData");

        Assert.Multiple(() =>
        {
            Assert.That(strictTrailing.Offset, Is.EqualTo(400));
            Assert.That(strictTrailing.Severity, Is.EqualTo(RawSeverity.Error));
            Assert.That(tolerantTrailing.Offset, Is.EqualTo(400));
            Assert.That(tolerantTrailing.Severity, Is.EqualTo(RawSeverity.Warning));
            Assert.That(strict.IsValid(), Is.False);
            Assert.That(tolerant.IsValid(), Is.True);
            Assert.That(tolerant.HasWarnings(), Is.True);
        });
    }

    [Test]
    public void BigTiffRequiresEightByteIfdAndValueAlignment()
    {
        TiffHeader header = new TiffHeader(RawByteOrder.LittleEndian, true, 43, 8, 18, 0, 16);
        TiffIfd ifd = new TiffIfd("IFD0", 18, 18, 0, new TiffEntry[0], 0);
        TiffIfdGraph graph = new TiffIfdGraph(header, new TiffIfd[] { ifd }, new TiffIfdLink[0]);
        graph.ContainerStart = 0;
        graph.ContainerLength = 34;

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidOffset && message.Context == "IFD0" && message.Message.Contains("alignment"))), Is.True);
    }

    [Test]
    public void OffsetAndContainerArithmeticOverflowBecomeFindings()
    {
        TiffIfdGraph graph = CreateValidStripGraph();
        graph.Images[0].Blocks[0].Offset = ulong.MaxValue;
        graph.ContainerStart = long.MaxValue;
        graph.ContainerLength = 1;

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.ArithmeticOverflow && message.Context == "TIFF")), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.ArithmeticOverflow && message.Context == "IFD0/Strip0"), Is.True);
        });
    }

    [Test]
    public void InvalidIfdLinksAndClassicLong8OffsetsAreReported()
    {
        TiffIfdGraph graph = CreateValidStripGraph();
        TiffIfd ifd = graph.Ifds[0];
        ReplaceEntry(ifd, CreateEntry(273, TiffFieldType.Long8, new ulong[] { 300, 350 }, EntryOffset(ifd, 273), 200, false));
        graph.Links = new TiffIfdLink[] { new TiffIfdLink("IFD0", "IFD1", "IFD1", 999, TiffIfdLinkKind.Next, 0, 1, false) };
        graph.Images = TiffImageData.Describe(graph, new RawLimits());

        RawValidationReport report = TiffValidator.Validate(graph, true);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidMetadata && message.Context == "IFD0/Tag0x0111")), Is.True);
            Assert.That(report.Messages.Any(message => message.Code == RawDiagnosticCode.InvalidOffset && message.Context == "IFD1"), Is.True);
        });
    }

    [Test]
    public void NullGraphProducesInvalidArgumentReport()
    {
        RawValidationReport report = TiffValidator.Validate(null, true);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single().Code), Is.EqualTo(RawDiagnosticCode.InvalidArgument));
        });
    }

    static TiffIfdGraph CreateValidStripGraph()
    {
        TiffEntry[] entries = new TiffEntry[]
        {
            CreateEntry(256, TiffFieldType.Long, new ulong[] { 10 }, 10, 0, false),
            CreateEntry(257, TiffFieldType.Long, new ulong[] { 10 }, 22, 0, false),
            CreateEntry(258, TiffFieldType.Short, new ulong[] { 8 }, 34, 0, false),
            CreateEntry(262, TiffFieldType.Short, new ulong[] { 1 }, 46, 0, false),
            CreateEntry(278, TiffFieldType.Long, new ulong[] { 5 }, 58, 0, false),
            CreateEntry(273, TiffFieldType.Long, new ulong[] { 300, 350 }, 70, 200, false),
            CreateEntry(279, TiffFieldType.Long, new ulong[] { 50, 50 }, 82, 208, false)
        };
        TiffIfd ifd = new TiffIfd("IFD0", 8, 8, 0, entries, 0);
        TiffIfdGraph graph = CreateGraph(ifd, 400, false);
        graph.Images = TiffImageData.Describe(graph, new RawLimits());
        return graph;
    }

    static TiffIfd CreateTileIfd(int offset, int absoluteOffset)
    {
        ulong[] offsets = Enumerable.Range(0, 8).Select(index => (ulong)(400 + index * 50)).ToArray();
        ulong[] counts = Enumerable.Repeat(40UL, 8).ToArray();
        TiffEntry[] entries = new TiffEntry[]
        {
            CreateEntry(256, TiffFieldType.Long, new ulong[] { 17 }, absoluteOffset + 2, 0, false),
            CreateEntry(257, TiffFieldType.Long, new ulong[] { 9 }, absoluteOffset + 14, 0, false),
            CreateEntry(262, TiffFieldType.Short, new ulong[] { 2 }, absoluteOffset + 26, 0, false),
            CreateEntry(322, TiffFieldType.Long, new ulong[] { 8 }, absoluteOffset + 38, 0, false),
            CreateEntry(323, TiffFieldType.Long, new ulong[] { 4 }, absoluteOffset + 50, 0, false),
            CreateEntry(324, TiffFieldType.Long, offsets, absoluteOffset + 62, 200, false),
            CreateEntry(325, TiffFieldType.Long, counts, absoluteOffset + 74, 232, false)
        };
        return new TiffIfd("IFD0", (ulong)offset, absoluteOffset, 0, entries, 0);
    }

    static TiffIfdGraph CreateGraph(TiffIfd ifd, long containerLength, bool bigTiff)
    {
        TiffHeader header = new TiffHeader(RawByteOrder.LittleEndian, bigTiff, bigTiff ? (ushort)43 : (ushort)42, bigTiff ? 8 : 4, ifd.Offset, 0, bigTiff ? 16 : 8);
        TiffIfdGraph graph = new TiffIfdGraph(header, new TiffIfd[] { ifd }, new TiffIfdLink[0]);
        graph.ContainerStart = 0;
        graph.ContainerLength = containerLength;
        return graph;
    }

    static TiffEntry CreateEntry(ushort tag, TiffFieldType fieldType, ulong[] values, long entryOffset, ulong valueOffset, bool bigTiff)
    {
        int elementSize = TiffFieldTypes.GetElementSize(fieldType);
        byte[] raw = new byte[values.Length * elementSize];
        for (int index = 0; index < values.Length; index++)
        {
            int offset = index * elementSize;
            if (fieldType == TiffFieldType.Short)
            {
                EndianWriter.WriteUInt16(raw.AsSpan(offset, 2), (ushort)values[index], RawByteOrder.LittleEndian);
            }
            else if (fieldType == TiffFieldType.Long || fieldType == TiffFieldType.Ifd)
            {
                EndianWriter.WriteUInt32(raw.AsSpan(offset, 4), (uint)values[index], RawByteOrder.LittleEndian);
            }
            else
            {
                EndianWriter.WriteUInt64(raw.AsSpan(offset, 8), values[index], RawByteOrder.LittleEndian);
            }
        }
        int slotSize = bigTiff ? 8 : 4;
        byte[] slot = new byte[slotSize];
        bool inline = raw.Length <= slotSize;
        if (inline)
        {
            raw.CopyTo(slot, 0);
        }
        else if (bigTiff)
        {
            EndianWriter.WriteUInt64(slot, valueOffset, RawByteOrder.LittleEndian);
        }
        else
        {
            EndianWriter.WriteUInt32(slot, (uint)valueOffset, RawByteOrder.LittleEndian);
        }
        return new TiffEntry(tag, fieldType, (ulong)values.Length, raw, slot, inline, inline ? 0 : valueOffset, RawByteOrder.LittleEndian, entryOffset);
    }

    static void ReplaceEntry(TiffIfd ifd, TiffEntry replacement)
    {
        for (int index = 0; index < ifd.Entries.Length; index++)
        {
            if (ifd.Entries[index].Tag == replacement.Tag)
            {
                ifd.Entries[index] = replacement;
                return;
            }
        }
        Assert.Fail("Tag was not found for replacement.");
    }

    static long EntryOffset(TiffIfd ifd, ushort tag)
    {
        return (ifd.FindEntry(tag) ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryOffset;
    }

    static byte[] BuildParsedStrip()
    {
        byte[] source = new byte[120];
        source[0] = 0x49;
        source[1] = 0x49;
        EndianWriter.WriteUInt16(source.AsSpan(2, 2), 42, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(4, 4), 8, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(source.AsSpan(8, 2), 7, RawByteOrder.LittleEndian);
        WriteInlineShort(source, 10, 256, 10);
        WriteInlineShort(source, 22, 257, 10);
        WriteInlineShort(source, 34, 258, 8);
        WriteInlineShort(source, 46, 262, 1);
        WriteInlineLong(source, 58, 278, 10);
        WriteInlineLong(source, 70, 273, 100);
        WriteInlineLong(source, 82, 279, 20);
        EndianWriter.WriteUInt32(source.AsSpan(94, 4), 0, RawByteOrder.LittleEndian);
        for (int index = 100; index < source.Length; index++)
        {
            source[index] = (byte)index;
        }
        return source;
    }

    static void WriteInlineShort(byte[] destination, int offset, ushort tag, ushort value)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 2, 2), (ushort)TiffFieldType.Short, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 4, 4), 1, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 8, 2), value, RawByteOrder.LittleEndian);
    }

    static void WriteInlineLong(byte[] destination, int offset, ushort tag, uint value)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 2, 2), (ushort)TiffFieldType.Long, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 4, 4), 1, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 8, 4), value, RawByteOrder.LittleEndian);
    }
}
