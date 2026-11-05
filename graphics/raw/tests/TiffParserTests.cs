using raw;

namespace raw.Tests;

public class TiffParserTests
{
    [Test]
    public void ClassicGraphTraversesChainsAndKnownAndUnknownPointersWithStablePaths()
    {
        byte[] source = BuildClassicGraph();
        BoundedInput input = CreateInput(source, new RawLimits());

        TiffIfdGraph graph = TiffParser.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds.Select(ifd => ifd.Path), Is.EqualTo(new string[]
            {
                "IFD0",
                "IFD0/SubIFD0",
                "IFD0/SubIFD1",
                "IFD0/Exif",
                "IFD0/Exif/Interop",
                "IFD0/GPS",
                "IFD1"
            }));
            Assert.That(graph.Links.Count(link => link.Alias), Is.EqualTo(1));
            Assert.That(graph.Links.Length, Is.EqualTo(7));
            Assert.That((graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Entries.Length, Is.EqualTo(4));
            Assert.That((graph.FindIfd("IFD0/Exif") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Depth, Is.EqualTo(1));
            Assert.That((graph.FindIfd("IFD0/Exif/Interop") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Depth, Is.EqualTo(2));
            Assert.That((graph.FindIfd("IFD1") ?? throw new System.InvalidOperationException("A required value is unavailable.")).NextIfdOffset, Is.EqualTo(0));
            Assert.That(input.GetPosition(), Is.EqualTo(8));
        });

        TiffIfdLink alias = graph.Links.Single(link => link.Alias);
        Assert.Multiple(() =>
        {
            Assert.That(alias.SourcePath, Is.EqualTo("IFD0"));
            Assert.That(alias.Path, Is.EqualTo("IFD0/Tag0xC001[3:0]"));
            Assert.That(alias.TargetPath, Is.EqualTo("IFD0/GPS"));
            Assert.That(alias.TargetOffset, Is.EqualTo(140));
            Assert.That(alias.Kind, Is.EqualTo(TiffIfdLinkKind.Unknown));
            Assert.That(alias.Tag, Is.EqualTo(0xC001));
        });
    }

    [Test]
    public void MainIfdAndPointerLinksRetainRelationshipDetails()
    {
        TiffIfdGraph graph = TiffParser.Parse(CreateInput(BuildClassicGraph(), new RawLimits()));
        TiffIfdLink[] links = graph.Links;

        Assert.Multiple(() =>
        {
            Assert.That(links[0].Kind, Is.EqualTo(TiffIfdLinkKind.SubIfd));
            Assert.That(links[0].Index, Is.EqualTo(0));
            Assert.That(links[1].Path, Is.EqualTo("IFD0/SubIFD1"));
            Assert.That(links[2].Kind, Is.EqualTo(TiffIfdLinkKind.Exif));
            Assert.That(links[3].Kind, Is.EqualTo(TiffIfdLinkKind.Interop));
            Assert.That(links[4].Kind, Is.EqualTo(TiffIfdLinkKind.Gps));
            Assert.That(links[6].Kind, Is.EqualTo(TiffIfdLinkKind.Next));
            Assert.That(links[6].SourcePath, Is.EqualTo("IFD0"));
            Assert.That(links[6].TargetPath, Is.EqualTo("IFD1"));
            Assert.That(links.Take(6).Select(link => link.Tag), Is.EqualTo(new ushort[] { 330, 330, 34665, 40965, 34853, 0xC001 }));
        });
    }

    [Test]
    public void BigTiffUsesEightByteCountsEntriesOffsetsAndNextLinks()
    {
        byte[] source = new byte[128];
        WriteBigHeader(source, RawByteOrder.BigEndian, 16);
        WriteBigIfdHeader(source, 16, 1, RawByteOrder.BigEndian);
        WriteBigEntry(source, 24, 330, TiffFieldType.Ifd8, 1, 80, RawByteOrder.BigEndian);
        EndianWriter.WriteUInt64(source.AsSpan(44, 8), 0, RawByteOrder.BigEndian);
        WriteEmptyBigIfd(source, 80, RawByteOrder.BigEndian);

        TiffIfdGraph graph = TiffParser.Parse(CreateInput(source, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(graph.Header.BigTiff, Is.True);
            Assert.That(graph.Header.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(graph.Ifds.Select(ifd => ifd.Path), Is.EqualTo(new string[] { "IFD0", "IFD0/SubIFD0" }));
            Assert.That(graph.Ifds[0].Entries[0].FieldType, Is.EqualTo(TiffFieldType.Ifd8));
            Assert.That(graph.Ifds[1].Offset, Is.EqualTo(80));
            Assert.That(graph.Links[0].Kind, Is.EqualTo(TiffIfdLinkKind.SubIfd));
        });
    }

    [Test]
    public void SeekableStreamTraversalDoesNotDisposeStreamAndRestoresLogicalReads()
    {
        byte[] source = BuildClassicGraph();
        MemoryStream stream = new MemoryStream(source, false);
        BoundedInput input = BoundedInput.CreateStream(stream, 0, source.Length, new RawLimits());

        TiffIfdGraph graph = TiffParser.Parse(input);
        byte nextHeaderByte = input.ReadByte();

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds.Length, Is.EqualTo(7));
            Assert.That(input.GetPosition(), Is.EqualTo(9));
            Assert.That(nextHeaderByte, Is.EqualTo(4));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void EmbeddedTiffOffsetsResolveFromHeaderBase()
    {
        byte[] tiff = BuildClassicGraph();
        byte[] source = Enumerable.Repeat((byte)0xAA, tiff.Length + 5).ToArray();
        tiff.CopyTo(source, 5);
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 5, tiff.Length, new RawLimits());

        TiffIfdGraph graph = TiffParser.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Header.BaseOffset, Is.EqualTo(5));
            Assert.That((graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset, Is.EqualTo(8));
            Assert.That((graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).AbsoluteOffset, Is.EqualTo(13));
            Assert.That((graph.FindIfd("IFD0/Exif/Interop") ?? throw new System.InvalidOperationException("A required value is unavailable.")).AbsoluteOffset, Is.EqualTo(165));
        });
    }

    [Test]
    public void NonSeekableTraversalBuffersBoundedWindowAndPreservesAbsoluteOrigin()
    {
        byte[] tiff = BuildClassicGraph();
        byte[] source = Enumerable.Repeat((byte)0xAA, tiff.Length + 3).ToArray();
        tiff.CopyTo(source, 3);
        ParserNonSeekableStream stream = new ParserNonSeekableStream(source, 3);
        BoundedInput input = BoundedInput.CreateStream(stream, 3, tiff.Length, new RawLimits());

        TiffIfdGraph graph = TiffParser.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Header.BaseOffset, Is.EqualTo(3));
            Assert.That((graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).AbsoluteOffset, Is.EqualTo(11));
            Assert.That((graph.FindIfd("IFD0/Exif/Interop") ?? throw new System.InvalidOperationException("A required value is unavailable.")).AbsoluteOffset, Is.EqualTo(163));
            Assert.That(input.GetRemaining(), Is.EqualTo(0));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void SubIfdPathsRetainPointerArrayIndexesWhenNullOffsetsAreSkipped()
    {
        byte[] source = new byte[80];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(source, 8, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 10, 330, TiffFieldType.Long, 2, 32, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(22, 4), 0, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(32, 4), 0, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(36, 4), 48, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 48, 0, RawByteOrder.LittleEndian);

        TiffIfdGraph graph = TiffParser.Parse(CreateInput(source, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds.Select(ifd => ifd.Path), Is.EqualTo(new string[] { "IFD0", "IFD0/SubIFD1" }));
            Assert.That(graph.Links[0].Index, Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroFirstIfdOffsetProducesAnEmptyGraph()
    {
        byte[] source = new byte[8];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 0);

        TiffIfdGraph graph = TiffParser.Parse(CreateInput(source, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds, Is.Empty);
            Assert.That(graph.Links, Is.Empty);
        });
    }

    [Test]
    public void MainChainBackEdgeIsRejectedAsACycle()
    {
        byte[] source = new byte[32];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        WriteEmptyClassicIfd(source, 8, 8, RawByteOrder.LittleEndian);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffParser.Parse(CreateInput(source, new RawLimits())));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(exception.Offset, Is.EqualTo(8));
        });
    }

    [Test]
    public void MainChainCanReachConfiguredIfdLimitWithoutRecursiveStackGrowth()
    {
        int ifdCount = 4096;
        byte[] source = new byte[8 + ifdCount * 6];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        for (int index = 0; index < ifdCount; index++)
        {
            int offset = 8 + index * 6;
            uint nextOffset = 0;
            if (index + 1 < ifdCount)
            {
                nextOffset = (uint)(offset + 6);
            }
            WriteEmptyClassicIfd(source, offset, nextOffset, RawByteOrder.LittleEndian);
        }

        TiffIfdGraph graph = TiffParser.Parse(CreateInput(source, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds.Length, Is.EqualTo(ifdCount));
            Assert.That(graph.Links.Length, Is.EqualTo(ifdCount - 1));
            Assert.That(graph.Ifds[ifdCount - 1].Path, Is.EqualTo("IFD4095"));
        });
    }

    [Test]
    public void NestedBackEdgeIsRejectedAsACycle()
    {
        byte[] source = new byte[96];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(source, 8, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 10, 330, TiffFieldType.Long, 1, 40, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(22, 4), 0, RawByteOrder.LittleEndian);
        WriteClassicIfdHeader(source, 40, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 42, 34665, TiffFieldType.Long, 1, 8, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(54, 4), 0, RawByteOrder.LittleEndian);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffParser.Parse(CreateInput(source, new RawLimits())));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(exception.Offset, Is.EqualTo(8));
        });
    }

    [Test]
    public void IfdCountDepthTagCountAndMetadataLimitsAreEnforced()
    {
        byte[] chain = new byte[80];
        WriteClassicHeader(chain, RawByteOrder.LittleEndian, 8);
        WriteEmptyClassicIfd(chain, 8, 40, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(chain, 40, 0, RawByteOrder.LittleEndian);
        RawLimits ifdLimits = new RawLimits();
        ifdLimits.MaxIfdCount = 1;
        RawLimitException? ifdCount = Assert.Throws<RawLimitException>(() => TiffParser.Parse(CreateInput(chain, ifdLimits)));

        byte[] nested = BuildDepthGraph();
        RawLimits depthLimits = new RawLimits();
        depthLimits.MaxIfdDepth = 1;
        RawLimitException? depth = Assert.Throws<RawLimitException>(() => TiffParser.Parse(CreateInput(nested, depthLimits)));

        byte[] tags = new byte[64];
        WriteClassicHeader(tags, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(tags, 8, 2, RawByteOrder.LittleEndian);
        WriteClassicEntry(tags, 10, 256, TiffFieldType.Short, 1, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(tags, 22, 257, TiffFieldType.Short, 1, 1, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(tags.AsSpan(34, 4), 0, RawByteOrder.LittleEndian);
        RawLimits tagLimits = new RawLimits();
        tagLimits.MaxTagCount = 1;
        RawLimitException? tagCount = Assert.Throws<RawLimitException>(() => TiffParser.Parse(CreateInput(tags, tagLimits)));

        byte[] metadata = new byte[80];
        WriteClassicHeader(metadata, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(metadata, 8, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(metadata, 10, 700, TiffFieldType.Undefined, 5, 40, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(metadata.AsSpan(22, 4), 0, RawByteOrder.LittleEndian);
        new byte[] { 1, 2, 3, 4, 5 }.CopyTo(metadata, 40);
        RawLimits metadataLimits = new RawLimits();
        metadataLimits.MaxMetadataBytes = 4;
        RawLimitException? metadataCount = Assert.Throws<RawLimitException>(() => TiffParser.Parse(CreateInput(metadata, metadataLimits)));

        Assert.Multiple(() =>
        {
            Assert.That((ifdCount ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That(ifdCount.Offset, Is.EqualTo(40));
            Assert.That((depth ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That(depth.Offset, Is.EqualTo(80));
            Assert.That((tagCount ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That(tagCount.Offset, Is.EqualTo(8));
            Assert.That((metadataCount ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void OutOfRangeAndTruncatedIfdsAreRejected()
    {
        byte[] outside = new byte[32];
        WriteClassicHeader(outside, RawByteOrder.LittleEndian, 100);
        RawFormatException? invalidOffset = Assert.Throws<RawFormatException>(() => TiffParser.Parse(CreateInput(outside, new RawLimits())));

        byte[] truncated = new byte[12];
        WriteClassicHeader(truncated, RawByteOrder.LittleEndian, 8);
        EndianWriter.WriteUInt16(truncated.AsSpan(8, 2), 1, RawByteOrder.LittleEndian);
        RawFormatException? invalidLength = Assert.Throws<RawFormatException>(() => TiffParser.Parse(CreateInput(truncated, new RawLimits())));

        Assert.Multiple(() =>
        {
            Assert.That((invalidOffset ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(invalidOffset.Offset, Is.EqualTo(100));
            Assert.That((invalidLength ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(invalidLength.Offset, Is.EqualTo(10));
        });
    }

    [Test]
    public void NonPointerLongTagsAreNotTraversed()
    {
        byte[] source = new byte[64];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(source, 8, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 10, 256, TiffFieldType.Long, 1, 40, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(22, 4), 0, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 40, 0, RawByteOrder.LittleEndian);

        TiffIfdGraph graph = TiffParser.Parse(CreateInput(source, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds.Select(ifd => ifd.Path), Is.EqualTo(new string[] { "IFD0" }));
            Assert.That(graph.Links, Is.Empty);
            Assert.That((graph.Ifds[0].FindEntry(256) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetUInt32(), Is.EqualTo(40));
        });
    }

    static byte[] BuildClassicGraph()
    {
        byte[] source = new byte[192];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(source, 8, 4, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 10, 330, TiffFieldType.Long, 2, 64, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 22, 34665, TiffFieldType.Long, 1, 120, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 34, 34853, TiffFieldType.Ifd, 1, 140, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 46, 0xC001, TiffFieldType.Ifd, 1, 140, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(58, 4), 180, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(64, 4), 80, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(68, 4), 100, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 80, 0, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 100, 0, RawByteOrder.LittleEndian);
        WriteClassicIfdHeader(source, 120, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 122, 40965, TiffFieldType.Long, 1, 160, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(134, 4), 0, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 140, 0, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 160, 0, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 180, 0, RawByteOrder.LittleEndian);
        return source;
    }

    static byte[] BuildDepthGraph()
    {
        byte[] source = new byte[112];
        WriteClassicHeader(source, RawByteOrder.LittleEndian, 8);
        WriteClassicIfdHeader(source, 8, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 10, 330, TiffFieldType.Long, 1, 40, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(22, 4), 0, RawByteOrder.LittleEndian);
        WriteClassicIfdHeader(source, 40, 1, RawByteOrder.LittleEndian);
        WriteClassicEntry(source, 42, 330, TiffFieldType.Long, 1, 80, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(54, 4), 0, RawByteOrder.LittleEndian);
        WriteEmptyClassicIfd(source, 80, 0, RawByteOrder.LittleEndian);
        return source;
    }

    static void WriteClassicHeader(byte[] destination, RawByteOrder byteOrder, uint firstIfdOffset)
    {
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            destination[0] = 0x49;
            destination[1] = 0x49;
        }
        else
        {
            destination[0] = 0x4D;
            destination[1] = 0x4D;
        }
        EndianWriter.WriteUInt16(destination.AsSpan(2, 2), 42, byteOrder);
        EndianWriter.WriteUInt32(destination.AsSpan(4, 4), firstIfdOffset, byteOrder);
    }

    static void WriteBigHeader(byte[] destination, RawByteOrder byteOrder, ulong firstIfdOffset)
    {
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            destination[0] = 0x49;
            destination[1] = 0x49;
        }
        else
        {
            destination[0] = 0x4D;
            destination[1] = 0x4D;
        }
        EndianWriter.WriteUInt16(destination.AsSpan(2, 2), 43, byteOrder);
        EndianWriter.WriteUInt16(destination.AsSpan(4, 2), 8, byteOrder);
        EndianWriter.WriteUInt16(destination.AsSpan(6, 2), 0, byteOrder);
        EndianWriter.WriteUInt64(destination.AsSpan(8, 8), firstIfdOffset, byteOrder);
    }

    static void WriteClassicIfdHeader(byte[] destination, int offset, ushort count, RawByteOrder byteOrder)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), count, byteOrder);
    }

    static void WriteBigIfdHeader(byte[] destination, int offset, ulong count, RawByteOrder byteOrder)
    {
        EndianWriter.WriteUInt64(destination.AsSpan(offset, 8), count, byteOrder);
    }

    static void WriteClassicEntry(byte[] destination, int offset, ushort tag, TiffFieldType fieldType, uint count, uint valueOrOffset, RawByteOrder byteOrder)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), tag, byteOrder);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 2, 2), (ushort)fieldType, byteOrder);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 4, 4), count, byteOrder);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 8, 4), valueOrOffset, byteOrder);
    }

    static void WriteBigEntry(byte[] destination, int offset, ushort tag, TiffFieldType fieldType, ulong count, ulong valueOrOffset, RawByteOrder byteOrder)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), tag, byteOrder);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 2, 2), (ushort)fieldType, byteOrder);
        EndianWriter.WriteUInt64(destination.AsSpan(offset + 4, 8), count, byteOrder);
        EndianWriter.WriteUInt64(destination.AsSpan(offset + 12, 8), valueOrOffset, byteOrder);
    }

    static void WriteEmptyClassicIfd(byte[] destination, int offset, uint nextOffset, RawByteOrder byteOrder)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), 0, byteOrder);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 2, 4), nextOffset, byteOrder);
    }

    static void WriteEmptyBigIfd(byte[] destination, int offset, RawByteOrder byteOrder)
    {
        EndianWriter.WriteUInt64(destination.AsSpan(offset, 8), 0, byteOrder);
        EndianWriter.WriteUInt64(destination.AsSpan(offset + 8, 8), 0, byteOrder);
    }

    static BoundedInput CreateInput(byte[] source, RawLimits limits)
    {
        return new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, limits);
    }

    class ParserNonSeekableStream : Stream
    {
        MemoryStream Inner;
        int MaximumRead;
        public bool WasDisposed;

        public ParserNonSeekableStream(byte[] source, int maximumRead)
        {
            Inner = new MemoryStream(source, false);
            MaximumRead = maximumRead;
            WasDisposed = false;
        }

        public override bool CanRead
        {
            get
            {
                return true;
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
                return false;
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
            return Inner.Read(buffer, offset, Math.Min(count, MaximumRead));
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
