using System.Buffers.Binary;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicContainerApiTests
{
    const ulong LargeSyntheticOffset = 4294971392UL;

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void ConfiguredContainer_OpensThroughBytesSpanPathAndStream(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        byte[] bytes = CreateFileBytes(fileKind, byteOrder, 2, 4);
        string path = CreateTempPath();
        File.WriteAllBytes(path, bytes);
        MemoryStream stream = new MemoryStream(bytes);
        TiffReadOptions streamOptions = new TiffReadOptions();
        streamOptions.LeaveOpen = true;

        TiffFile byteFile = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffFile spanFile = TiffApi.Factory.OpenReadMemory(bytes.AsSpan(), new TiffReadOptions());
        TiffFile pathFile = TiffApi.Factory.OpenReadPath(path, new TiffReadOptions());
        TiffFile streamFile = TiffApi.Factory.OpenReadStream(stream, streamOptions);

        AssertContainer(byteFile, fileKind, byteOrder, 2, 4);
        AssertContainer(spanFile, fileKind, byteOrder, 2, 4);
        AssertContainer(pathFile, fileKind, byteOrder, 2, 4);
        AssertContainer(streamFile, fileKind, byteOrder, 2, 4);
        Assert.That(stream.CanRead, Is.True);
        File.Delete(path);
    }

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void ConfiguredPathAndStreamWriters_RoundTripThroughPublicFactories(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        string path = CreateTempPath();
        TiffWriteOptions pathOptions = CreateWriteOptions(fileKind, byteOrder);
        TiffWriter pathWriter = TiffApi.Factory.CreatePathWriter(path, pathOptions);
        PopulateWriter(pathWriter, 1, 3);
        pathWriter.Close();

        MemoryStream stream = new MemoryStream();
        TiffWriteOptions streamOptions = CreateWriteOptions(fileKind, byteOrder);
        streamOptions.LeaveOpen = true;
        TiffWriter streamWriter = TiffApi.Factory.CreateStreamWriter(stream, streamOptions);
        PopulateWriter(streamWriter, 1, 3);
        streamWriter.Close();

        Assert.That(pathWriter.GetSuccess(), Is.True);
        Assert.That(streamWriter.GetSuccess(), Is.True);
        Assert.That(stream.CanWrite, Is.True);
        AssertContainer(TiffApi.Factory.OpenReadPath(path, new TiffReadOptions()), fileKind, byteOrder, 1, 3);
        AssertContainer(TiffApi.Factory.OpenReadBytes(stream.ToArray(), new TiffReadOptions()), fileKind, byteOrder, 1, 3);
        File.Delete(path);
    }

    [Test]
    public void StreamOwnership_IsAppliedByPublicReaderAndWriterFactories()
    {
        byte[] bytes = CreateFileBytes(TiffFileKind.Classic, TiffByteOrder.LittleEndian, 1, 1);
        MemoryStream ownedInput = new MemoryStream(bytes);
        MemoryStream retainedInput = new MemoryStream(bytes);
        TiffReadOptions retainedReadOptions = new TiffReadOptions();
        retainedReadOptions.LeaveOpen = true;
        TiffFile ownedFile = TiffApi.Factory.OpenReadStream(ownedInput, new TiffReadOptions());
        TiffFile retainedFile = TiffApi.Factory.OpenReadStream(retainedInput, retainedReadOptions);

        MemoryStream ownedOutput = new MemoryStream();
        MemoryStream retainedOutput = new MemoryStream();
        TiffWriteOptions retainedWriteOptions = new TiffWriteOptions();
        retainedWriteOptions.LeaveOpen = true;
        TiffWriter ownedWriter = TiffApi.Factory.CreateStreamWriter(ownedOutput, new TiffWriteOptions());
        TiffWriter retainedWriter = TiffApi.Factory.CreateStreamWriter(retainedOutput, retainedWriteOptions);
        PopulateWriter(ownedWriter, 1, 1);
        PopulateWriter(retainedWriter, 1, 1);

        ownedFile.Close();
        retainedFile.Close();
        ownedWriter.Close();
        retainedWriter.Close();

        Assert.Multiple(() =>
        {
            Assert.That(ownedInput.CanRead, Is.False);
            Assert.That(retainedInput.CanRead, Is.True);
            Assert.That(ownedOutput.CanWrite, Is.False);
            Assert.That(retainedOutput.CanWrite, Is.True);
            Assert.That(retainedOutput.Length, Is.GreaterThan(0));
        });
    }

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void NonSeekableStreams_AreRejectedForReadingAndSupportedForWriting(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        byte[] bytes = CreateFileBytes(fileKind, byteOrder, 1, 2);
        NonSeekableReadStream input = new NonSeekableReadStream(bytes);
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.LeaveOpen = true;
        TiffFile rejected = TiffApi.Factory.OpenReadStream(input, readOptions);

        NonSeekableWriteStream output = new NonSeekableWriteStream();
        TiffWriteOptions writeOptions = CreateWriteOptions(fileKind, byteOrder);
        writeOptions.LeaveOpen = true;
        TiffWriter writer = TiffApi.Factory.CreateStreamWriter(output, writeOptions);
        PopulateWriter(writer, 1, 2);
        writer.Close();
        TiffFile written = TiffApi.Factory.OpenReadBytes(output.ToArray(), new TiffReadOptions());

        Assert.That(rejected.GetSuccess(), Is.False);
        Assert.That(rejected.GetDiagnostics().ContainsCode(TiffErrorCode.NonSeekableStream), Is.True);
        Assert.That(input.CanRead, Is.True);
        Assert.That(output.CanWrite, Is.True);
        AssertContainer(written, fileKind, byteOrder, 1, 2);
    }

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void StreamingSource_DefersStripPayloadUntilTheImageIsRead(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        byte[] bytes = CreateFileBytes(fileKind, byteOrder, 1, 4096);
        TiffFile reference = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffByteRange payloadRange = reference.GetImage(0).GetStripLayout().Layout.Segments[0].ByteRange;
        reference.Close();
        TrackingSource source = new TrackingSource(bytes, payloadRange);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;
        options.MemoryPolicy.SourceLoadingMode = TiffSourceLoadingMode.Streaming;

        TiffFile file = TiffApi.Factory.OpenReadSource(source, options);
        TiffImage image = file.GetImage(0);
        TiffImageInfo info = image.GetInfo();
        TiffStripLayoutResult layout = image.GetStripLayout();

        Assert.That(file.GetSource(), Is.SameAs(source));
        Assert.That(info.Width, Is.EqualTo(4096));
        Assert.That(layout.Success, Is.True);
        Assert.That(source.PayloadWasRead, Is.False);

        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(strip.Success, Is.True);
        Assert.That(strip.Bytes, Has.Length.EqualTo(4096));
        Assert.That(source.PayloadWasRead, Is.True);
        file.Close();
        Assert.That(source.GetIsOpen(), Is.True);
        source.Close();
    }

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void TrailingBytes_ArePreservedOrHiddenAccordingToReadOptions(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        byte[] container = CreateFileBytes(fileKind, byteOrder, 1, 2);
        byte[] trailing = new byte[] { 91, 92, 93, 94, 95 };
        byte[] combined = new byte[container.Length + trailing.Length];
        container.CopyTo(combined, 0);
        trailing.CopyTo(combined, container.Length);
        TiffFile preserved = TiffApi.Factory.OpenReadBytes(combined, new TiffReadOptions());
        TiffReadOptions discardOptions = new TiffReadOptions();
        discardOptions.PreserveTrailingData = false;
        TiffFile discarded = TiffApi.Factory.OpenReadMemory(combined.AsSpan(), discardOptions);

        TiffByteRange range = preserved.GetTrailingDataRange();
        byte[] destination = new byte[trailing.Length];
        TiffDiagnostics diagnostics = preserved.ReadTrailingData(destination);

        Assert.Multiple(() =>
        {
            Assert.That(range.Offset, Is.EqualTo((ulong)container.Length));
            Assert.That(range.ByteCount, Is.EqualTo((ulong)trailing.Length));
            Assert.That(preserved.GetTrailingData(), Is.EqualTo(trailing));
            Assert.That(destination, Is.EqualTo(trailing));
            Assert.That(diagnostics.HasErrors(), Is.False);
            Assert.That(discarded.GetTrailingData(), Is.Empty);
            Assert.That(discarded.GetFileByteCount(), Is.EqualTo(container.Length));
        });

        preserved.Close();
        discarded.Close();
    }

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void MultiPageChains_RoundTripAndCyclesAreReported(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        byte[] bytes = CreateFileBytes(fileKind, byteOrder, 3, 2);
        TiffFile valid = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffDirectory[] directories = valid.GetDirectories();

        Assert.That(valid.GetSuccess(), Is.True);
        Assert.That(valid.GetImageCount(), Is.EqualTo(3));
        Assert.That(directories[0].NextIfdOffset, Is.EqualTo(directories[1].Offset));
        Assert.That(directories[1].NextIfdOffset, Is.EqualTo(directories[2].Offset));
        Assert.That(directories[2].NextIfdOffset, Is.EqualTo(0));
        valid.Close();

        WriteNextIfdOffset(bytes, directories[2], directories[0].Offset, fileKind, byteOrder);
        TiffFile cyclic = TiffApi.Factory.OpenReadMemory(bytes.AsSpan(), new TiffReadOptions());

        Assert.That(cyclic.GetSuccess(), Is.False);
        Assert.That(cyclic.GetImageCount(), Is.EqualTo(3));
        Assert.That(cyclic.GetDiagnostics().ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
        Assert.That(cyclic.GetDiagnostics().ToArray().Any(item => item.Code == TiffErrorCode.CyclicIfdChain && item.Offset == (long)directories[0].Offset), Is.True);
        cyclic.Close();
    }

    [TestCase(TiffByteOrder.LittleEndian)]
    [TestCase(TiffByteOrder.BigEndian)]
    public void BigTiffLargeSyntheticFirstIfdOffset_IsPreservedInPublicDiagnostics(TiffByteOrder byteOrder)
    {
        byte[] bytes = CreateBigTiffHeader(byteOrder, LargeSyntheticOffset);

        TiffFile file = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffHeader header = file.GetHeader();

        Assert.Multiple(() =>
        {
            Assert.That(file.GetSuccess(), Is.False);
            Assert.That(header.FileKind, Is.EqualTo(TiffFileKind.BigTiff));
            Assert.That(header.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(header.FirstIfdOffset, Is.EqualTo(LargeSyntheticOffset));
            Assert.That(file.GetImageCount(), Is.EqualTo(0));
            Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
            Assert.That(file.GetDiagnostics().ToArray().Any(item => item.Offset == (long)LargeSyntheticOffset), Is.True);
        });

        file.Close();
    }

    static TiffWriteOptions CreateWriteOptions(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = fileKind;
        options.ByteOrder = byteOrder;
        return options;
    }

    static byte[] CreateFileBytes(TiffFileKind fileKind, TiffByteOrder byteOrder, int pageCount, int width)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(CreateWriteOptions(fileKind, byteOrder));
        PopulateWriter(writer, pageCount, width);
        TiffDiagnostics diagnostics = writer.Flush();
        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(writer.GetSuccess(), Is.True);
        byte[] bytes = writer.GetFileBytes();
        writer.Close();
        return bytes;
    }

    static void PopulateWriter(TiffWriter writer, int pageCount, int width)
    {
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateStripTags(width, pageIndex, pageCount)));
            byte[] pixels = new byte[width];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = (byte)(pageIndex + index + 1);
            }

            TiffRawStripWriteResult result = writer.WriteRawStrip(imageIndex, 0, pixels);
            Assert.That(result.Success, Is.True);
        }
    }

    static TiffTagValue[] CreateStripTags(int width, int pageIndex, int pageCount)
    {
        return new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, (ulong)width),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedArrayTag(TiffTagId.PageNumber, TiffDataType.Short, new ulong[] { (ulong)pageIndex, (ulong)pageCount })
        };
    }

    static void AssertContainer(TiffFile file, TiffFileKind fileKind, TiffByteOrder byteOrder, int pageCount, int width)
    {
        Assert.That(file.GetSuccess(), Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(fileKind));
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(byteOrder));
        Assert.That(file.GetImageCount(), Is.EqualTo(pageCount));
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            TiffImage image = file.GetImage(pageIndex);
            TiffRawStripReadResult strip = image.ReadRawStrip(0);
            Assert.That(image.GetInfo().Width, Is.EqualTo((ulong)width));
            Assert.That(strip.Success, Is.True);
            Assert.That(strip.Bytes, Has.Length.EqualTo(width));
            Assert.That(strip.Bytes[0], Is.EqualTo((byte)(pageIndex + 1)));
        }

        file.Close();
    }

    static void WriteNextIfdOffset(byte[] bytes, TiffDirectory directory, ulong nextOffset, TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        int entryByteCount = fileKind == TiffFileKind.Classic ? 12 : 20;
        int countByteCount = fileKind == TiffFileKind.Classic ? 2 : 8;
        int nextByteCount = fileKind == TiffFileKind.Classic ? 4 : 8;
        int offset = (int)directory.Offset + countByteCount + (directory.Entries.Length * entryByteCount);
        if (nextByteCount == 4)
        {
            WriteUInt32(bytes.AsSpan(offset, 4), (uint)nextOffset, byteOrder);
        }
        else
        {
            WriteUInt64(bytes.AsSpan(offset, 8), nextOffset, byteOrder);
        }
    }

    static byte[] CreateBigTiffHeader(TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        byte[] bytes = new byte[16];
        byte marker = byteOrder == TiffByteOrder.LittleEndian ? (byte)0x49 : (byte)0x4D;
        bytes[0] = marker;
        bytes[1] = marker;
        WriteUInt16(bytes.AsSpan(2, 2), 43, byteOrder);
        WriteUInt16(bytes.AsSpan(4, 2), 8, byteOrder);
        WriteUInt16(bytes.AsSpan(6, 2), 0, byteOrder);
        WriteUInt64(bytes.AsSpan(8, 8), firstIfdOffset, byteOrder);
        return bytes;
    }

    static void WriteUInt16(Span<byte> destination, ushort value, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
        }
    }

    static void WriteUInt32(Span<byte> destination, uint value, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        }
    }

    static void WriteUInt64(Span<byte> destination, ulong value, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, value);
        }
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return path;
    }

    class TrackingSource : ITiffRandomAccessSource
    {
        byte[] Bytes;
        TiffByteRange PayloadRange;
        bool IsOpen;
        public bool PayloadWasRead;

        public TrackingSource(byte[] bytes, TiffByteRange payloadRange)
        {
            Bytes = bytes;
            PayloadRange = payloadRange;
            IsOpen = true;
            PayloadWasRead = false;
        }

        public bool GetIsOpen()
        {
            return IsOpen;
        }

        public long GetLength()
        {
            return Bytes.Length;
        }

        public int Read(long offset, Span<byte> destination)
        {
            long requestEnd = offset + destination.Length;
            long payloadStart = (long)PayloadRange.Offset;
            long payloadEnd = (long)PayloadRange.EndOffset;
            if (offset < payloadEnd && requestEnd > payloadStart)
            {
                PayloadWasRead = true;
            }

            if (!IsOpen || offset < 0 || offset >= Bytes.Length)
            {
                return 0;
            }

            int count = Math.Min(destination.Length, Bytes.Length - (int)offset);
            Bytes.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }

        public void Close()
        {
            IsOpen = false;
        }
    }

    class NonSeekableReadStream : MemoryStream
    {
        public NonSeekableReadStream(byte[] bytes) : base(bytes)
        {
        }

        public override bool CanSeek
        {
            get { return false; }
        }
    }

    class NonSeekableWriteStream : Stream
    {
        MemoryStream Inner;
        bool IsOpen;

        public NonSeekableWriteStream()
        {
            Inner = new MemoryStream();
            IsOpen = true;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return IsOpen; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public byte[] ToArray()
        {
            return Inner.ToArray();
        }

        public override void Flush()
        {
            Inner.Flush();
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

        protected override void Dispose(bool disposing)
        {
            IsOpen = false;
            base.Dispose(disposing);
        }
    }
}
