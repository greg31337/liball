using raw;

namespace raw.Tests;

public class PublicGenericDecodeTests
{
    [Test]
    public void BoundedMemoryOperationsDecodeEveryStoredAndNormalizedDestination()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor unsignedDescriptor = codec.CreateRgbDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);
        byte[] unsignedSource = Enumerable.Range(0, 18).Select(value => (byte)value).ToArray();
        RawDecodeRegion colorCrop = new RawDecodeRegion(1, 1, 2, 1, 1, 2, 1, 5);
        uint[] storedUnsigned = Enumerable.Repeat(999U, 6).ToArray();
        ushort[] normalizedUnsigned = Enumerable.Repeat((ushort)999, 6).ToArray();
        float[] normalizedFloating = Enumerable.Repeat(999.0F, 6).ToArray();

        codec.DecodeGenericUnsigned(unsignedSource, unsignedDescriptor, colorCrop, storedUnsigned, new RawDecodeOptions());
        codec.DecodeGenericNormalizedUnsigned(unsignedSource, unsignedDescriptor, colorCrop, normalizedUnsigned, new RawDecodeOptions());
        codec.DecodeGenericNormalizedFloating(unsignedSource, unsignedDescriptor, colorCrop, normalizedFloating, new RawDecodeOptions());

        RawDescriptor signedDescriptor = codec.CreateGrayDescriptor(4, 1, 8, 8, RawByteOrder.LittleEndian);
        signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
        byte[] signedSource = new byte[] { 0x80, 0xFF, 0x00, 0x7F };
        int[] storedSigned = new int[4];
        codec.DecodeGenericSigned(signedSource, signedDescriptor, WholeRegion(signedDescriptor), storedSigned, new RawDecodeOptions());

        RawDescriptor floatingDescriptor = codec.CreateGrayDescriptor(3, 1, 32, 32, RawByteOrder.LittleEndian);
        floatingDescriptor.SampleKind = RawSampleKind.Single;
        byte[] floatingSource = CreateSingleBytes(new float[] { -1.0F, 0.25F, float.PositiveInfinity });
        float[] storedFloating = new float[3];
        float[] retainedFloating = new float[3];
        codec.DecodeGenericFloating(floatingSource, floatingDescriptor, WholeRegion(floatingDescriptor), storedFloating, new RawDecodeOptions());
        codec.DecodeGenericNormalizedFloating(floatingSource, floatingDescriptor, WholeRegion(floatingDescriptor), retainedFloating, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(storedUnsigned, Is.EqualTo(new uint[] { 999, 13, 14, 16, 17, 999 }));
            Assert.That(normalizedUnsigned, Is.EqualTo(new ushort[] { 999, 13 * 257, 14 * 257, 16 * 257, 17 * 257, 999 }));
            Assert.That(normalizedFloating[0], Is.EqualTo(999.0F));
            Assert.That(normalizedFloating[1], Is.EqualTo(13.0F / 255.0F).Within(0.000001F));
            Assert.That(normalizedFloating[4], Is.EqualTo(17.0F / 255.0F).Within(0.000001F));
            Assert.That(normalizedFloating[5], Is.EqualTo(999.0F));
            Assert.That(storedSigned, Is.EqualTo(new int[] { -128, -1, 0, 127 }));
            Assert.That(storedFloating[0], Is.EqualTo(-1.0F));
            Assert.That(storedFloating[1], Is.EqualTo(0.25F));
            Assert.That(float.IsPositiveInfinity(storedFloating[2]), Is.True);
            Assert.That(retainedFloating, Is.EqualTo(storedFloating));
        });
    }

    [Test]
    public void BoundedNonSeekableStreamOperationsDecodeRegionsRowsChannelsAndPlanesWithoutClosingStreams()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor planar = codec.CreateRgbDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);
        planar.Layout = RawSampleLayout.Planar;
        planar.RowStrideBytes = 4;
        planar.PlaneStrideBytes = 10;
        planar.DataOffset = 1;
        planar.DataLength = 28;
        byte[] planarSource = CreatePlanarBytes(planar);
        RawDecodeRegion planes = new RawDecodeRegion(1, 0, 2, 2, 1, 2, 1, 6);
        uint[] storedUnsigned = Enumerable.Repeat(777U, 12).ToArray();
        ushort[] normalizedUnsigned = Enumerable.Repeat((ushort)777, 12).ToArray();
        float[] normalizedFloating = Enumerable.Repeat(777.0F, 12).ToArray();
        DecodeTrackingStream unsignedStream = CreateStream(planarSource);
        DecodeTrackingStream normalizedUnsignedStream = CreateStream(planarSource);
        DecodeTrackingStream normalizedFloatingStream = CreateStream(planarSource);

        codec.DecodeGenericUnsignedStream(unsignedStream, 2, planarSource.Length, planar, planes, storedUnsigned, new RawDecodeOptions());
        codec.DecodeGenericNormalizedUnsignedStream(normalizedUnsignedStream, 2, planarSource.Length, planar, planes, normalizedUnsigned, new RawDecodeOptions());
        codec.DecodeGenericNormalizedFloatingStream(normalizedFloatingStream, 2, planarSource.Length, planar, planes, normalizedFloating, new RawDecodeOptions());

        RawDescriptor signedDescriptor = codec.CreateGrayDescriptor(4, 1, 8, 8, RawByteOrder.LittleEndian);
        signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
        byte[] signedSource = new byte[] { 0x80, 0xFF, 0x00, 0x7F };
        DecodeTrackingStream signedStream = CreateStream(signedSource);
        int[] storedSigned = new int[4];
        codec.DecodeGenericSignedStream(signedStream, 2, signedSource.Length, signedDescriptor, WholeRegion(signedDescriptor), storedSigned, new RawDecodeOptions());

        RawDescriptor floatingDescriptor = codec.CreateGrayDescriptor(3, 1, 32, 32, RawByteOrder.LittleEndian);
        floatingDescriptor.SampleKind = RawSampleKind.Single;
        byte[] floatingSource = CreateSingleBytes(new float[] { -1.0F, 0.25F, float.PositiveInfinity });
        DecodeTrackingStream floatingStream = CreateStream(floatingSource);
        float[] storedFloating = new float[3];
        codec.DecodeGenericFloatingStream(floatingStream, 2, floatingSource.Length, floatingDescriptor, WholeRegion(floatingDescriptor), storedFloating, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(storedUnsigned, Is.EqualTo(new uint[]
            {
                777, 51, 101, 52, 102, 777,
                777, 61, 111, 62, 112, 777
            }));
            Assert.That(normalizedUnsigned[1], Is.EqualTo(51 * 257));
            Assert.That(normalizedUnsigned[4], Is.EqualTo(102 * 257));
            Assert.That(normalizedUnsigned[7], Is.EqualTo(61 * 257));
            Assert.That(normalizedUnsigned[10], Is.EqualTo(112 * 257));
            Assert.That(normalizedFloating[1], Is.EqualTo(51.0F / 255.0F).Within(0.000001F));
            Assert.That(normalizedFloating[10], Is.EqualTo(112.0F / 255.0F).Within(0.000001F));
            Assert.That(storedSigned, Is.EqualTo(new int[] { -128, -1, 0, 127 }));
            Assert.That(storedFloating[0], Is.EqualTo(-1.0F));
            Assert.That(storedFloating[1], Is.EqualTo(0.25F));
            Assert.That(float.IsPositiveInfinity(storedFloating[2]), Is.True);
            Assert.That(unsignedStream.Disposed, Is.False);
            Assert.That(normalizedUnsignedStream.Disposed, Is.False);
            Assert.That(normalizedFloatingStream.Disposed, Is.False);
            Assert.That(signedStream.Disposed, Is.False);
            Assert.That(floatingStream.Disposed, Is.False);
        });
    }

    [Test]
    public void PublicDecodersRejectInvalidBuffersLimitsAndCancellationWithoutModifyingCallerStorage()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = codec.CreateGrayDescriptor(2, 2, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 1, 2, 3, 4 };
        RawDecodeRegion whole = WholeRegion(descriptor);
        uint[] unchanged = new uint[] { 99, 99, 99, 99 };
        RawDecodeOptions limited = new RawDecodeOptions();
        (limited.Limits ?? throw new InvalidOperationException()).MaxAllocationBytes = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => codec.DecodeGenericUnsigned(source, descriptor, whole, new uint[3], new RawDecodeOptions()));
            Assert.Throws<RawFormatException>(() => codec.DecodeGenericSigned(source, descriptor, whole, new int[4], new RawDecodeOptions()));
            Assert.Throws<RawLimitException>(() => codec.DecodeGenericUnsigned(source, descriptor, whole, unchanged, limited));
            Assert.That(unchanged, Is.All.EqualTo(99));
        });

        RawDecodeOptions canceled = new RawDecodeOptions();
        canceled.CancellationToken = new CancellationToken(true);
        DecodeTrackingStream canceledStream = CreateStream(source);
        Assert.Throws<OperationCanceledException>(() => codec.DecodeGenericUnsignedStream(canceledStream, 2, source.Length, descriptor, whole, unchanged, canceled));
        Assert.Multiple(() =>
        {
            Assert.That(canceledStream.BytesRead, Is.Zero);
            Assert.That(canceledStream.Disposed, Is.False);
            Assert.That(unchanged, Is.All.EqualTo(99));
        });
    }

    private static RawDecodeRegion WholeRegion(RawDescriptor descriptor)
    {
        return new RawDecodeRegion(0, 0, descriptor.Width, descriptor.Height, 0, descriptor.ChannelCount, 0, descriptor.Width * descriptor.ChannelCount);
    }

    private static byte[] CreateSingleBytes(float[] values)
    {
        byte[] result = new byte[values.Length * 4];
        int index = 0;
        while (index < values.Length)
        {
            BitConverter.TryWriteBytes(result.AsSpan(index * 4, 4), values[index]);
            index++;
        }
        return result;
    }

    private static byte[] CreatePlanarBytes(RawDescriptor descriptor)
    {
        byte[] result = Enumerable.Repeat((byte)0xA5, (int)(descriptor.DataOffset + descriptor.DataLength)).ToArray();
        int plane = 0;
        while (plane < descriptor.ChannelCount)
        {
            int row = 0;
            while (row < descriptor.Height)
            {
                int pixel = 0;
                while (pixel < descriptor.Width)
                {
                    int offset = (int)(descriptor.DataOffset + plane * descriptor.PlaneStrideBytes + row * descriptor.RowStrideBytes + pixel);
                    result[offset] = (byte)(plane * 50 + row * 10 + pixel);
                    pixel++;
                }
                row++;
            }
            plane++;
        }
        return result;
    }

    private static DecodeTrackingStream CreateStream(byte[] source)
    {
        byte[] wrapped = new byte[source.Length + 3];
        wrapped[0] = 90;
        wrapped[1] = 91;
        source.CopyTo(wrapped, 2);
        wrapped[wrapped.Length - 1] = 92;
        return new DecodeTrackingStream(wrapped, 2);
    }
}

internal class DecodeTrackingStream : Stream
{
    internal MemoryStream Inner;
    internal int MaximumRead;
    internal int BytesRead;
    internal bool Disposed;

    internal DecodeTrackingStream(byte[] source, int maximumRead)
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
        base.Dispose(disposing);
    }
}
