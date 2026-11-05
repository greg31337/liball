using System.Text;

namespace gltf.Tests.Internal.Containers;

[TestFixture]
public class GlbWriterTests
{
    [Test]
    public void WriteBytes_WritesUnsignedHeadersAndJsonBinPadding()
    {
        GlbWriter writer = new GlbWriter();
        GltfContainerData source = CreateContainer(Encoding.UTF8.GetBytes("{\"é\":10}"));
        source.HasBinChunk = true;
        source.BinBytes = [1, 2, 3];
        GltfGlbChunk unknown = new GltfGlbChunk();
        unknown.Type = 0xFEDCBA98U;
        unknown.Data = [4, 5, 6, 7];
        source.UnknownChunks.Add(unknown);

        byte[] bytes = writer.WriteBytes(source, true);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32LittleEndian(bytes, 0), Is.EqualTo(GltfConstants.GlbMagic));
            Assert.That(ReadUInt32LittleEndian(bytes, 4), Is.EqualTo((uint)GltfConstants.GlbVersion));
            Assert.That(ReadUInt32LittleEndian(bytes, 8), Is.EqualTo(56U));
            Assert.That(ReadUInt32LittleEndian(bytes, 12), Is.EqualTo(12U));
            Assert.That(ReadUInt32LittleEndian(bytes, 16), Is.EqualTo(GltfConstants.GlbJsonChunkType));
            Assert.That(bytes[29..32], Is.EqualTo(new byte[] { 0x20, 0x20, 0x20 }));
            Assert.That(ReadUInt32LittleEndian(bytes, 32), Is.EqualTo(4U));
            Assert.That(ReadUInt32LittleEndian(bytes, 36), Is.EqualTo(GltfConstants.GlbBinChunkType));
            Assert.That(bytes[40..44], Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(ReadUInt32LittleEndian(bytes, 44), Is.EqualTo(4U));
            Assert.That(ReadUInt32LittleEndian(bytes, 48), Is.EqualTo(0xFEDCBA98U));
            Assert.That(bytes[52..56], Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
        });
    }

    [Test]
    public void WriteBytes_PreservesAbsentAndPresentEmptyBinChunks()
    {
        GlbWriter writer = new GlbWriter();
        GltfContainerData withoutBin = CreateContainer([0x7B, 0x7D]);
        GltfContainerData withEmptyBin = CreateContainer([0x7B, 0x7D]);
        withEmptyBin.HasBinChunk = true;

        byte[] absentBytes = writer.WriteBytes(withoutBin, true);
        byte[] presentBytes = writer.WriteBytes(withEmptyBin, true);
        GlbReader reader = new GlbReader();
        GltfContainerData absent = reader.ReadChunks(
            absentBytes,
            reader.ReadHeader(absentBytes, absentBytes.Length),
            true);
        GltfContainerData present = reader.ReadChunks(
            presentBytes,
            reader.ReadHeader(presentBytes, presentBytes.Length),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(absentBytes, Has.Length.EqualTo(24));
            Assert.That(absent.HasBinChunk, Is.False);
            Assert.That(presentBytes, Has.Length.EqualTo(32));
            Assert.That(ReadUInt32LittleEndian(presentBytes, 24), Is.Zero);
            Assert.That(ReadUInt32LittleEndian(presentBytes, 28), Is.EqualTo(GltfConstants.GlbBinChunkType));
            Assert.That(present.HasBinChunk, Is.True);
            Assert.That(present.BinBytes, Is.Empty);
        });
    }

    [Test]
    public void WriteBytes_PreservesOrOmitsUnknownChunksByPolicy()
    {
        GlbWriter writer = new GlbWriter();
        GltfContainerData source = CreateContainer([0x7B, 0x7D]);
        GltfGlbChunk unknown = new GltfGlbChunk();
        unknown.Type = 0x12345678U;
        unknown.Data = [8, 9, 10, 11];
        source.UnknownChunks.Add(unknown);

        byte[] preservedBytes = writer.WriteBytes(source, true);
        byte[] omittedBytes = writer.WriteBytes(source, false);
        GlbReader reader = new GlbReader();
        GltfContainerData preserved = reader.ReadChunks(
            preservedBytes,
            reader.ReadHeader(preservedBytes, preservedBytes.Length),
            true);
        GltfContainerData omitted = reader.ReadChunks(
            omittedBytes,
            reader.ReadHeader(omittedBytes, omittedBytes.Length),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.UnknownChunks, Has.Count.EqualTo(1));
            Assert.That(preserved.UnknownChunks[0].Type, Is.EqualTo(0x12345678U));
            Assert.That(preserved.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 8, 9, 10, 11 }));
            Assert.That(omitted.UnknownChunks, Is.Empty);
            Assert.That(omittedBytes, Has.Length.EqualTo(24));
        });
    }

    [Test]
    public void WriteStream_WritesSequentiallyToSeekableAndNonSeekableStreams()
    {
        GlbWriter writer = new GlbWriter();
        GltfContainerData source = CreateContainer([0x7B, 0x7D]);
        using MemoryStream seekable = new MemoryStream();
        using NonSeekableWriteStream nonSeekable = new NonSeekableWriteStream();

        int seekableByteLength = writer.WriteStream(source, seekable, true);
        int nonSeekableByteLength = writer.WriteStream(source, nonSeekable, true);

        Assert.Multiple(() =>
        {
            Assert.That(seekableByteLength, Is.EqualTo(24));
            Assert.That(nonSeekableByteLength, Is.EqualTo(24));
            Assert.That(nonSeekable.SeekAttempted, Is.False);
            Assert.That(nonSeekable.GetBytes(), Is.EqualTo(seekable.ToArray()));
        });
    }

    [Test]
    public void WriteStream_RejectsInvalidContentBeforeModifyingTheTarget()
    {
        GlbWriter writer = new GlbWriter();
        GltfContainerData emptyJson = CreateContainer([]);
        using MemoryStream target = new MemoryStream();
        target.WriteByte(99);

        Assert.That(
            () => writer.WriteStream(emptyJson, target, true),
            Throws.TypeOf<GltfWriteException>()
                .With.Message.EqualTo("The GLB JSON chunk must not be empty."));
        Assert.That(target.ToArray(), Is.EqualTo(new byte[] { 99 }));
    }

    [Test]
    public void WriteBytes_RejectsReservedOrUnalignedUnknownChunks()
    {
        GlbWriter writer = new GlbWriter();
        GltfContainerData reservedSource = CreateContainer([0x7B, 0x7D]);
        GltfGlbChunk reserved = new GltfGlbChunk();
        reserved.Type = GltfConstants.GlbJsonChunkType;
        reserved.Data = [0, 0, 0, 0];
        reservedSource.UnknownChunks.Add(reserved);
        GltfContainerData unalignedSource = CreateContainer([0x7B, 0x7D]);
        GltfGlbChunk unaligned = new GltfGlbChunk();
        unaligned.Type = 1U;
        unaligned.Data = [1, 2, 3];
        unalignedSource.UnknownChunks.Add(unaligned);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => writer.WriteBytes(reservedSource, true),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo("GLB unknown chunk 0 uses reserved chunk type 0x4E4F534A."));
            Assert.That(
                () => writer.WriteBytes(unalignedSource, true),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo("GLB unknown chunk 0 byte length 3 is not aligned to 4 bytes."));
        });
    }

    [Test]
    public void LengthHelpers_RejectNegativeUnsignedAndOverflowingTotalLengths()
    {
        GlbWriter writer = new GlbWriter();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => writer.ConvertByteLengthToUInt32(-1, "Test chunk"),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo(
                        "Test chunk byte length -1 cannot be represented as an unsigned 32-bit length."));
            Assert.That(
                () => writer.AddOutputByteLength(int.MaxValue, 1, "Test chunk"),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo(
                        "Test chunk makes the GLB output exceed the supported signed byte length."));
        });
    }

    private static GltfContainerData CreateContainer(byte[] jsonBytes)
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.JsonBytes = jsonBytes;
        return container;
    }

    private static uint ReadUInt32LittleEndian(byte[] bytes, int offset)
    {
        return (uint)bytes[offset]
            | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }
}

internal sealed class NonSeekableWriteStream : Stream
{
    private readonly MemoryStream stream = new MemoryStream();

    internal bool SeekAttempted;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => stream.Length;

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

    public override void Flush()
    {
        stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
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
        stream.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        stream.WriteByte(value);
    }
}
