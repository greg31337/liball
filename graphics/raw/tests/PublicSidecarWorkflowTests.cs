using System.Text;
using System.Text.Json;
using raw;

namespace raw.Tests;

public class PublicSidecarWorkflowTests
{
    [Test]
    public void BoundedMemoryRoundTripIsDeterministicAndPreservesUnknownProperties()
    {
        IRawCodec codec = new RawCodecService();
        Dictionary<string, JsonElement> firstExtensions = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        firstExtensions.Add("zeta", Element("[1,true,null]"));
        firstExtensions.Add("camera", Element("{\"make\":\"Example\",\"id\":42}"));
        Dictionary<string, JsonElement> secondExtensions = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        secondExtensions.Add("camera", Element("{\"make\":\"Example\",\"id\":42}"));
        secondExtensions.Add("zeta", Element("[1,true,null]"));
        RawSidecar first = new RawSidecar(1, codec.CreateRaw12Descriptor(5, 3, RawCfaLayout.Grbg), firstExtensions);
        RawSidecar second = new RawSidecar(1, codec.CreateRaw12Descriptor(5, 3, RawCfaLayout.Grbg), secondExtensions);

        byte[] firstJson = codec.WriteSidecarBytes(first, new RawLimits(), CancellationToken.None);
        byte[] secondJson = codec.WriteSidecarBytes(second, new RawLimits(), CancellationToken.None);
        byte[] wrapped = new byte[firstJson.Length + 4];
        wrapped[0] = 0xFF;
        wrapped[1] = 0xFE;
        firstJson.CopyTo(wrapped, 2);
        wrapped[wrapped.Length - 2] = 0xFD;
        wrapped[wrapped.Length - 1] = 0xFC;
        RawSidecar parsed = codec.ReadSidecarMemory(new ReadOnlyMemory<byte>(wrapped, 2, firstJson.Length), new RawLimits(), CancellationToken.None);
        byte[] rewritten = codec.WriteSidecarBytes(parsed, new RawLimits(), CancellationToken.None);
        string text = Encoding.UTF8.GetString(firstJson);

        Assert.Multiple(() =>
        {
            Assert.That(firstJson, Is.EqualTo(secondJson));
            Assert.That(rewritten, Is.EqualTo(firstJson));
            Assert.That(firstJson[0], Is.EqualTo((byte)'{'));
            Assert.That(text.IndexOf("\"camera\"", StringComparison.Ordinal), Is.LessThan(text.IndexOf("\"zeta\"", StringComparison.Ordinal)));
            Assert.That(parsed.SchemaVersion, Is.EqualTo(1));
            Assert.That(parsed.Descriptor.Packing, Is.EqualTo(RawPacking.Raw12));
            Assert.That(parsed.UnknownProperties.Keys, Is.EquivalentTo(new string[] { "camera", "zeta" }));
            Assert.That(parsed.UnknownProperties["camera"].GetProperty("make").GetString(), Is.EqualTo("Example"));
            Assert.That(parsed.UnknownProperties["camera"].GetProperty("id").GetInt32(), Is.EqualTo(42));
            Assert.That(parsed.UnknownProperties["zeta"][1].GetBoolean(), Is.True);
            Assert.That(parsed.UnknownProperties["zeta"][2].ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    [Test]
    public void CallerOwnedNonSeekableStreamsUseExactBoundsAndRemainOpen()
    {
        IRawCodec codec = new RawCodecService();
        RawSidecar sidecar = GraySidecar(codec);
        byte[] expected = codec.WriteSidecarBytes(sidecar, new RawLimits(), CancellationToken.None);
        byte[] wrapped = new byte[expected.Length + 5];
        wrapped[0] = 90;
        wrapped[1] = 91;
        expected.CopyTo(wrapped, 2);
        wrapped[wrapped.Length - 3] = 92;
        wrapped[wrapped.Length - 2] = 93;
        wrapped[wrapped.Length - 1] = 94;
        SidecarReadTrackingStream input = new SidecarReadTrackingStream(wrapped, 3);
        SidecarWriteTrackingStream output = new SidecarWriteTrackingStream();

        RawSidecar parsed = codec.ReadSidecarStream(input, 2, expected.Length, new RawLimits(), CancellationToken.None);
        codec.WriteSidecarStream(output, parsed, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Descriptor.Width, Is.EqualTo(2));
            Assert.That(parsed.Descriptor.Height, Is.EqualTo(3));
            Assert.That(input.BytesRead, Is.EqualTo(expected.Length + 2));
            Assert.That(input.Disposed, Is.False);
            Assert.That(output.Bytes(), Is.EqualTo(expected));
            Assert.That(output.Disposed, Is.False);
        });
    }

    [Test]
    public void PublicReadsAndWritesEnforceStrictSchemaRules()
    {
        IRawCodec codec = new RawCodecService();
        byte[] valid = codec.WriteSidecarBytes(GraySidecar(codec), new RawLimits(), CancellationToken.None);
        string text = Encoding.UTF8.GetString(valid);
        byte[] future = Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal));
        byte[] unknownDescriptor = Encoding.UTF8.GetBytes(text.Replace("\"width\": 2,", "\"width\": 2,\n    \"widht\": 2,", StringComparison.Ordinal));
        byte[] duplicateRoot = Encoding.UTF8.GetBytes(text.Substring(0, text.LastIndexOf('}')) + ",\n  \"schemaVersion\": 1\n}");
        RawSidecar invalidVersion = GraySidecar(codec);
        invalidVersion.SchemaVersion = 2;
        RawSidecar reservedExtension = GraySidecar(codec);
        reservedExtension.UnknownProperties.Add("descriptor", Element("false"));

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => codec.ReadSidecarMemory(future, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => codec.ReadSidecarMemory(unknownDescriptor, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => codec.ReadSidecarMemory(duplicateRoot, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => codec.WriteSidecarBytes(invalidVersion, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => codec.WriteSidecarBytes(reservedExtension, new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void CancellationStopsMemoryAndStreamsBeforeTransportChanges()
    {
        IRawCodec codec = new RawCodecService();
        RawSidecar sidecar = GraySidecar(codec);
        byte[] json = codec.WriteSidecarBytes(sidecar, new RawLimits(), CancellationToken.None);
        SidecarReadTrackingStream input = new SidecarReadTrackingStream(json, 2);
        SidecarWriteTrackingStream output = new SidecarWriteTrackingStream();
        CancellationToken cancelled = new CancellationToken(true);

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => codec.ReadSidecarMemory(json, new RawLimits(), cancelled));
            Assert.Throws<OperationCanceledException>(() => codec.ReadSidecarStream(input, 0, json.Length, new RawLimits(), cancelled));
            Assert.Throws<OperationCanceledException>(() => codec.WriteSidecarBytes(sidecar, new RawLimits(), cancelled));
            Assert.Throws<OperationCanceledException>(() => codec.WriteSidecarStream(output, sidecar, new RawLimits(), cancelled));
            Assert.That(input.BytesRead, Is.Zero);
            Assert.That(input.Disposed, Is.False);
            Assert.That(output.Bytes(), Is.Empty);
            Assert.That(output.Disposed, Is.False);
        });
    }

    [Test]
    public void MetadataFileAndAllocationLimitsApplyToEveryNewTransport()
    {
        IRawCodec codec = new RawCodecService();
        RawSidecar sidecar = GraySidecar(codec);
        byte[] json = codec.WriteSidecarBytes(sidecar, new RawLimits(), CancellationToken.None);
        RawLimits metadata = new RawLimits();
        metadata.MaxMetadataBytes = json.Length - 1;
        RawLimits allocation = new RawLimits();
        allocation.MaxAllocationBytes = json.Length - 1;
        RawLimits file = new RawLimits();
        file.MaxFileSizeBytes = json.Length - 1;
        SidecarReadTrackingStream metadataInput = new SidecarReadTrackingStream(json, 2);
        SidecarWriteTrackingStream metadataOutput = new SidecarWriteTrackingStream();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => codec.ReadSidecarMemory(json, metadata, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.ReadSidecarMemory(json, allocation, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.ReadSidecarMemory(json, file, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.ReadSidecarStream(metadataInput, 0, json.Length, metadata, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.WriteSidecarBytes(sidecar, metadata, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.WriteSidecarBytes(sidecar, allocation, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.WriteSidecarBytes(sidecar, file, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.WriteSidecarStream(metadataOutput, sidecar, metadata, CancellationToken.None));
            Assert.That(metadataInput.BytesRead, Is.Zero);
            Assert.That(metadataInput.Disposed, Is.False);
            Assert.That(metadataOutput.Bytes(), Is.Empty);
            Assert.That(metadataOutput.Disposed, Is.False);
            Assert.That(codec.ReadSidecarMemory(json, new RawLimits(), CancellationToken.None).Descriptor.Width, Is.EqualTo(2));
        });
    }

    private static RawSidecar GraySidecar(IRawCodec codec)
    {
        return new RawSidecar(1, codec.CreateGrayDescriptor(2, 3, 12, 16, RawByteOrder.LittleEndian), null);
    }

    private static JsonElement Element(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

internal class SidecarReadTrackingStream : Stream
{
    internal MemoryStream Inner;
    internal int MaximumRead;
    internal int BytesRead;
    internal bool Disposed;

    internal SidecarReadTrackingStream(byte[] source, int maximumRead)
    {
        Inner = new MemoryStream(source, false);
        MaximumRead = maximumRead;
        BytesRead = 0;
        Disposed = false;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = Inner.Read(buffer, offset, Math.Min(count, MaximumRead));
        BytesRead += read;
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        int read = Inner.Read(buffer.Slice(0, Math.Min(buffer.Length, MaximumRead)));
        BytesRead += read;
        return read;
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
        Disposed = true;
        Inner.Dispose();
        base.Dispose(disposing);
    }
}

internal class SidecarWriteTrackingStream : Stream
{
    internal MemoryStream Inner;
    internal bool Disposed;

    internal SidecarWriteTrackingStream()
    {
        Inner = new MemoryStream();
        Disposed = false;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    internal byte[] Bytes()
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
        Disposed = true;
        Inner.Dispose();
        base.Dispose(disposing);
    }
}
