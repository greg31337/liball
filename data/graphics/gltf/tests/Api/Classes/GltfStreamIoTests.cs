namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfStreamIoTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(7)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(19)]
    [TestCase(20)]
    [TestCase(21)]
    [TestCase(81920)]
    public void ReadStreamAcceptsGlbFragmentedAcrossContainerAndChunkBoundaries(
        int maximumReadByteLength)
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Generator = "fragmented-stream";
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Glb;
        writeOptions.Validate = false;
        byte[] glb = new GltfWriter().WriteBytes(
            document,
            writeOptions).PrimaryBytes;
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream(
            glb,
            true,
            false,
            maximumReadByteLength);

        GltfReadResult result = new GltfReader().ReadStream(
            stream,
            new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Container!.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(result.Document!.Asset.Generator, Is.EqualTo("fragmented-stream"));
            Assert.That(stream.ReadByteLength, Is.EqualTo(glb.Length));
            Assert.That(stream.SeekAttempted, Is.False);
            Assert.That(stream.Disposed, Is.False);
        });
    }

    [Test]
    public void ReadContainerStream_BuffersAChunkedNonSeekableInputAndLeavesItOpen()
    {
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream(
            [0x20, 0x0A, 0x7B, 0x7D],
            true,
            false,
            1);
        GltfReadOptions options = new GltfReadOptions();
        options.LeaveStreamOpen = true;

        GltfReadResult result = new GltfReader().ReadContainerStream(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Container, Is.Not.Null);
            Assert.That(result.Container!.Format, Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(result.Container.JsonBytes, Is.EqualTo(new byte[] { 0x20, 0x0A, 0x7B, 0x7D }));
            Assert.That(stream.Disposed, Is.False);
            Assert.That(stream.SeekAttempted, Is.False);
            Assert.That(stream.ReadByteLength, Is.EqualTo(4));
        });
    }

    [Test]
    public void ReadContainerStream_ProbesOneBytePastTheLimitAndDisposesOwnedInput()
    {
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream(
            new byte[100],
            true,
            false,
            int.MaxValue);
        GltfReadOptions options = new GltfReadOptions();
        options.Limits.MaximumInputByteLength = 2;
        options.LeaveStreamOpen = false;

        Assert.That(
            () => new GltfReader().ReadContainerStream(stream, options),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("Input stream exceeds the configured maximum 2 at byte 2."));
        Assert.Multiple(() =>
        {
            Assert.That(stream.Disposed, Is.True);
            Assert.That(stream.ReadByteLength, Is.EqualTo(3));
            Assert.That(stream.LargestReadRequest, Is.EqualTo(3));
            Assert.That(stream.SeekAttempted, Is.False);
        });
    }

    [Test]
    public void WriteContainerStream_WritesNonSeekableGlbAndDisposesOwnedOutput()
    {
        GltfContainerData container = CreateContainer();
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream([], false, true, 1);
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = GltfContainerFormat.Glb;
        options.LeaveStreamOpen = false;

        GltfWriteResult result = new GltfWriter().WriteContainerStream(container, stream, options);
        byte[] bytes = stream.GetBytes();
        GlbReader reader = new GlbReader();
        GltfContainerData parsed = reader.ReadChunks(
            bytes,
            reader.ReadHeader(bytes, bytes.Length),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(parsed.JsonBytes, Is.EqualTo(new byte[] { 0x7B, 0x7D, 0x20, 0x20 }));
            Assert.That(parsed.HasBinChunk, Is.True);
            Assert.That(parsed.BinBytes, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(stream.Disposed, Is.True);
            Assert.That(stream.SeekAttempted, Is.False);
        });
    }

    [Test]
    public void WriteContainerStream_LeavesCallerOutputOpenAndOwnsNoResultCopy()
    {
        GltfContainerData container = CreateContainer();
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream([], false, true, 1);
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = GltfContainerFormat.Json;
        options.LeaveStreamOpen = true;

        GltfWriteResult result = new GltfWriter().WriteContainerStream(container, stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(stream.GetBytes(), Is.EqualTo(new byte[] { 0x7B, 0x7D }));
            Assert.That(stream.Disposed, Is.False);
            Assert.That(result.PrimaryBytes, Is.Empty);
        });
    }

    [Test]
    public void DocumentStreamPaths_ApplyOwnershipWhenReaderAndWriterComplete()
    {
        TrackingNonSeekableStream input = new TrackingNonSeekableStream(
            System.Text.Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}"),
            true,
            false,
            1);
        TrackingNonSeekableStream output = new TrackingNonSeekableStream([], false, true, 1);
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.LeaveStreamOpen = false;
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.LeaveStreamOpen = false;
        writeOptions.Validate = false;

        GltfReadResult readResult = new GltfReader().ReadStream(input, readOptions);
        GltfWriteResult writeResult = new GltfWriter().WriteStream(new GltfDocument(), output, writeOptions);
        Assert.Multiple(() =>
        {
            Assert.That(readResult.Document, Is.Not.Null);
            Assert.That(writeResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(writeResult.PrimaryBytes, Is.Empty);
            Assert.That(
                output.GetBytes(),
                Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}")));
        });
        Assert.Multiple(() =>
        {
            Assert.That(input.Disposed, Is.True);
            Assert.That(output.Disposed, Is.True);
        });
    }

    [Test]
    public void ConvertStream_BuffersInputAndAppliesIndependentOwnershipOptions()
    {
        TrackingNonSeekableStream input = new TrackingNonSeekableStream(
            System.Text.Encoding.UTF8.GetBytes(
                "{\"asset\":{\"version\":\"2.0\"}}"),
            true,
            false,
            1);
        TrackingNonSeekableStream output = new TrackingNonSeekableStream([], false, true, 1);
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Json;
        options.ReadOptions.LeaveStreamOpen = false;
        options.WriteOptions.LeaveStreamOpen = false;
        options.WriteOptions.Validate = false;

        GltfConversionResult result = new GltfConverter().ConvertStream(
            input,
            output,
            options);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.PrimaryBytes, Is.Empty);
            Assert.That(output.GetBytes(), Is.EqualTo(
                System.Text.Encoding.UTF8.GetBytes(
                    "{\"asset\":{\"version\":\"2.0\"}}")));
            Assert.That(input.ReadByteLength, Is.EqualTo(27));
            Assert.That(input.SeekAttempted, Is.False);
            Assert.That(input.Disposed, Is.True);
            Assert.That(output.Disposed, Is.True);
        });
    }

    [Test]
    public void WriteContainerStream_DisposesOwnedOutputWhenPreflightFails()
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream([], false, true, 1);
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = GltfContainerFormat.Auto;
        options.LeaveStreamOpen = false;

        Assert.That(
            () => new GltfWriter().WriteContainerStream(container, stream, options),
            Throws.TypeOf<GltfWriteException>()
                .With.Message.EqualTo("The JSON container must not be empty."));
        Assert.That(stream.Disposed, Is.True);
    }

    private static GltfContainerData CreateContainer()
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.JsonBytes = [0x7B, 0x7D];
        container.HasBinChunk = true;
        container.BinBytes = [1, 2, 3];
        return container;
    }
}

internal sealed class TrackingNonSeekableStream : Stream
{
    private readonly MemoryStream stream;
    private readonly bool readable;
    private readonly bool writable;
    private readonly int maximumReadByteLength;

    internal bool Disposed;
    internal bool SeekAttempted;
    internal int ReadByteLength;
    internal int LargestReadRequest;

    internal TrackingNonSeekableStream(
        byte[] bytes,
        bool readable,
        bool writable,
        int maximumReadByteLength)
    {
        stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        this.readable = readable;
        this.writable = writable;
        this.maximumReadByteLength = maximumReadByteLength;
    }

    public override bool CanRead => readable && !Disposed;

    public override bool CanSeek => false;

    public override bool CanWrite => writable && !Disposed;

    public override long Length
    {
        get
        {
            SeekAttempted = true;
            throw new NotSupportedException();
        }
    }

    public override long Position
    {
        get
        {
            SeekAttempted = true;
            throw new NotSupportedException();
        }

        set
        {
            SeekAttempted = true;
            throw new NotSupportedException();
        }
    }

    internal byte[] GetBytes()
    {
        return stream.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }

    public override void Flush()
    {
        stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!CanRead)
        {
            throw new NotSupportedException();
        }

        if (count > LargestReadRequest)
        {
            LargestReadRequest = count;
        }

        int boundedCount = count;
        if (boundedCount > maximumReadByteLength)
        {
            boundedCount = maximumReadByteLength;
        }

        int readByteLength = stream.Read(buffer, offset, boundedCount);
        ReadByteLength += readByteLength;
        return readByteLength;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        SeekAttempted = true;
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        stream.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        stream.WriteByte(value);
    }
}
