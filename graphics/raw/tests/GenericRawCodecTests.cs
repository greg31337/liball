using raw;

namespace raw.Tests;

public class GenericRawCodecTests
{
    [Test]
    public void InterleavedByteAlignedRegionAndWholeImageDecodeRespectOffsetsPaddingAndChannelOrder()
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(4, 3, 8, 8, RawByteOrder.LittleEndian);
        descriptor.RowStrideBytes = 14;
        descriptor.DataOffset = 3;
        descriptor.DataLength = 42;
        byte[] source = Enumerable.Repeat((byte)0xEE, 45).ToArray();
        int row = 0;
        while (row < 3)
        {
            int pixel = 0;
            while (pixel < 4)
            {
                int channel = 0;
                while (channel < 3)
                {
                    source[3 + row * 14 + pixel * 3 + channel] = (byte)(row * 40 + pixel * 3 + channel);
                    channel++;
                }
                pixel++;
            }
            row++;
        }
        RawDecodeRegion crop = new RawDecodeRegion(1, 1, 2, 1, 1, 2, 2, 6);
        uint[] stored = Enumerable.Repeat(999U, 10).ToArray();
        ushort[] normalized = new ushort[36];

        GenericRawCodec.DecodeStored(source, descriptor, crop, stored, new RawDecodeOptions());
        GenericRawCodec.DecodeNormalized(source, descriptor, RawDecodeRegion.CreateWhole(descriptor), normalized, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.EqualTo(new uint[] { 999, 999, 44, 45, 47, 48, 999, 999, 999, 999 }));
            Assert.That(normalized[0], Is.Zero);
            Assert.That(normalized[1], Is.EqualTo(257));
            Assert.That(normalized[12], Is.EqualTo(40 * 257));
            Assert.That(normalized[35], Is.EqualTo(91 * 257));
        });
    }

    [Test]
    public void PlanarBigEndianRegionSelectsPlanesAndWritesPixelInterleavedRowsWithDestinationStride()
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(3, 2, 12, 16, RawByteOrder.BigEndian);
        descriptor.Layout = RawSampleLayout.Planar;
        descriptor.RowStrideBytes = 8;
        descriptor.PlaneStrideBytes = 20;
        descriptor.DataOffset = 2;
        descriptor.DataLength = 56;
        byte[] source = Enumerable.Repeat((byte)0xA5, 58).ToArray();
        int plane = 0;
        while (plane < 3)
        {
            int row = 0;
            while (row < 2)
            {
                int pixel = 0;
                while (pixel < 3)
                {
                    ushort value = (ushort)(plane * 1000 + row * 100 + pixel);
                    EndianWriter.WriteUInt16(source.AsSpan(2 + plane * 20 + row * 8 + pixel * 2, 2), value, RawByteOrder.BigEndian);
                    pixel++;
                }
                row++;
            }
            plane++;
        }
        RawDecodeRegion region = new RawDecodeRegion(1, 0, 2, 2, 1, 2, 1, 6);
        uint[] destination = Enumerable.Repeat(777U, 12).ToArray();

        GenericRawCodec.DecodeStored(source, descriptor, region, destination, new RawDecodeOptions());

        Assert.That(destination, Is.EqualTo(new uint[]
        {
            777, 1001, 2001, 1002, 2002, 777,
            777, 1101, 2101, 1102, 2102, 777
        }));
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void GenericPackedSignedRowsDecodeStoredAndNormalizeAcrossTheDeclaredDomain(RawBitOrder bitOrder)
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(5, 2, 12, 16, RawByteOrder.LittleEndian);
        descriptor.SampleKind = RawSampleKind.SignedInteger;
        descriptor.Packing = RawPacking.PackedBits;
        descriptor.StoredWordBits = 12;
        descriptor.BitOrder = bitOrder;
        descriptor.RowStrideBytes = 8;
        descriptor.DataLength = 16;
        int[] first = new int[] { -2048, -1024, 0, 1024, 2047 };
        int[] second = first.Reverse().ToArray();
        byte[] source = Enumerable.Repeat((byte)0xC3, 16).ToArray();
        BitPacking.PackSigned(first, 12, bitOrder, source.AsSpan(0, 8));
        BitPacking.PackSigned(second, 12, bitOrder, source.AsSpan(8, 8));
        int[] stored = new int[10];
        ushort[] normalized = new ushort[10];
        float[] floating = new float[10];
        RawDecodeRegion whole = RawDecodeRegion.CreateWhole(descriptor);

        GenericRawCodec.DecodeStored(source, descriptor, whole, stored, new RawDecodeOptions());
        GenericRawCodec.DecodeNormalized(source, descriptor, whole, normalized, new RawDecodeOptions());
        GenericRawCodec.DecodeNormalized(source, descriptor, whole, floating, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.EqualTo(first.Concat(second)));
            Assert.That(normalized[0], Is.Zero);
            Assert.That(normalized[4], Is.EqualTo(65535));
            Assert.That(normalized[5], Is.EqualTo(65535));
            Assert.That(normalized[9], Is.Zero);
            Assert.That(floating[0], Is.Zero);
            Assert.That(floating[4], Is.EqualTo(1.0F));
            Assert.That(floating[2], Is.EqualTo(2048.0F / 4095.0F).Within(0.000001F));
        });
    }

    [TestCase(RawPacking.Raw10)]
    [TestCase(RawPacking.Raw12)]
    [TestCase(RawPacking.Raw14)]
    public void NamedPackedOddRowsSupportCroppedRegionDecode(RawPacking packing)
    {
        RawDescriptor descriptor = RawDescriptor.CreatePackedBayer(5, 2, packing, RawCfaLayout.Rggb);
        long minimum = descriptor.RowStrideBytes;
        descriptor.RowStrideBytes = minimum + 2;
        descriptor.DataLength = descriptor.RowStrideBytes * descriptor.Height;
        int depth = descriptor.BitDepth;
        uint mask = depth == 32 ? uint.MaxValue : (1U << depth) - 1U;
        uint[] first = new uint[] { 1, 2, mask, mask / 2, 17 };
        uint[] second = new uint[] { 21, 22, 23, 24, 25 };
        byte[] source = Enumerable.Repeat((byte)0xD7, (int)descriptor.DataLength).ToArray();
        BitPacking.PackNamed(first, packing, source.AsSpan(0, (int)minimum));
        BitPacking.PackNamed(second, packing, source.AsSpan((int)descriptor.RowStrideBytes, (int)minimum));
        uint[] result = new uint[6];

        GenericRawCodec.DecodeStored(source, descriptor, new RawDecodeRegion(1, 0, 3, 2, 0, 1, 0, 3), result, new RawDecodeOptions());

        Assert.That(result, Is.EqualTo(new uint[] { 2, mask, mask / 2, 22, 23, 24 }));
    }

    [Test]
    public void HalfAndSingleSamplesRetainFloatingValuesAndUseDocumentedUShortConversion()
    {
        RawDescriptor halfDescriptor = RawDescriptor.CreateGray(4, 1, 16, 16, RawByteOrder.LittleEndian);
        halfDescriptor.SampleKind = RawSampleKind.Half;
        Half[] halfValues = new Half[] { (Half)(-1.0F), (Half)0.25F, (Half)1.5F, Half.PositiveInfinity };
        byte[] halfSource = new byte[8];
        int index = 0;
        while (index < halfValues.Length)
        {
            EndianWriter.WriteHalf(halfSource.AsSpan(index * 2, 2), halfValues[index], RawByteOrder.LittleEndian);
            index++;
        }
        float[] storedHalf = new float[4];
        ushort[] normalizedHalf = new ushort[4];
        GenericRawCodec.DecodeStored(halfSource, halfDescriptor, RawDecodeRegion.CreateWhole(halfDescriptor), storedHalf, new RawDecodeOptions());
        GenericRawCodec.DecodeNormalized(halfSource, halfDescriptor, RawDecodeRegion.CreateWhole(halfDescriptor), normalizedHalf, new RawDecodeOptions());

        RawDescriptor singleDescriptor = RawDescriptor.CreateGray(3, 1, 32, 32, RawByteOrder.BigEndian);
        singleDescriptor.SampleKind = RawSampleKind.Single;
        uint[] singleBits = new uint[] { 0x3F000000, 0xFF800000, 0x7FC12345 };
        byte[] singleSource = new byte[12];
        index = 0;
        while (index < singleBits.Length)
        {
            EndianWriter.WriteUInt32(singleSource.AsSpan(index * 4, 4), singleBits[index], RawByteOrder.BigEndian);
            index++;
        }
        float[] storedSingle = new float[3];
        GenericRawCodec.DecodeStored(singleSource, singleDescriptor, RawDecodeRegion.CreateWhole(singleDescriptor), storedSingle, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(storedHalf[0], Is.EqualTo(-1.0F));
            Assert.That(storedHalf[1], Is.EqualTo(0.25F));
            Assert.That(storedHalf[2], Is.EqualTo(1.5F));
            Assert.That(float.IsPositiveInfinity(storedHalf[3]), Is.True);
            Assert.That(normalizedHalf, Is.EqualTo(new ushort[] { 0, 16384, 65535, 65535 }));
            Assert.That(storedSingle.Select(BitConverter.SingleToUInt32Bits), Is.EqualTo(singleBits));
        });
    }

    [Test]
    public void NaNCannotPartiallyModifyNormalizedUShortDestination()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(3, 1, 32, 32, RawByteOrder.LittleEndian);
        descriptor.SampleKind = RawSampleKind.Single;
        byte[] source = new byte[12];
        EndianWriter.WriteSingle(source.AsSpan(0, 4), 0.25F, RawByteOrder.LittleEndian);
        EndianWriter.WriteSingle(source.AsSpan(4, 4), float.NaN, RawByteOrder.LittleEndian);
        EndianWriter.WriteSingle(source.AsSpan(8, 4), 0.75F, RawByteOrder.LittleEndian);
        ushort[] destination = new ushort[] { 123, 123, 123 };

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeNormalized(source, descriptor, RawDecodeRegion.CreateWhole(descriptor), destination, new RawDecodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(destination, Is.All.EqualTo(123));
        });
    }

    [Test]
    public void NonSeekableStreamRowDecodeHonorsBoundedOffsetAndLeavesStreamOpen()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 2, 8, 8, RawByteOrder.LittleEndian);
        descriptor.DataOffset = 1;
        descriptor.DataLength = 4;
        byte[] bytes = new byte[] { 90, 91, 0xAA, 10, 11, 20, 21, 0xBB };
        ShortNonSeekableStream stream = new ShortNonSeekableStream(bytes, 1);
        uint[] result = new uint[2];

        GenericRawCodec.DecodeStored(stream, 2, 5, descriptor, new RawDecodeRegion(0, 1, 2, 1, 0, 1, 0, 2), result, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new uint[] { 20, 21 }));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.ReadByte(), Is.EqualTo(0xBB));
        });
    }

    [Test]
    public void InvalidRegionsDestinationsKindsSamplesLengthsLimitsAndCancellationAreRejected()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 2, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 1, 2, 3, 4 };
        uint[] unchanged = new uint[] { 99, 99, 99, 99 };
        RawDecodeRegion outside = new RawDecodeRegion(1, 0, 2, 1, 0, 1, 0, 2);
        RawDecodeRegion shortDestination = new RawDecodeRegion(0, 0, 2, 2, 0, 1, 0, 2);
        RawDecodeOptions allocation = new RawDecodeOptions();
        (allocation.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxAllocationBytes = 1;
        RawDecodeOptions decoded = new RawDecodeOptions();
        (decoded.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxDecodedBytes = 3;
        decoded.Limits.MaxAllocationBytes = 3;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawDecodeOptions cancelled = new RawDecodeOptions();
        cancelled.CancellationToken = cancellation.Token;
        byte[] invalidSample = new byte[] { 0xFF, 0, 0, 0 };
        RawDescriptor narrow = RawDescriptor.CreateGray(2, 2, 4, 8, RawByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeStored(source, descriptor, outside, unchanged, new RawDecodeOptions()));
            Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeStored(source, descriptor, shortDestination, new uint[3], new RawDecodeOptions()));
            Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeStored(source, descriptor, RawDecodeRegion.CreateWhole(descriptor), new int[4], new RawDecodeOptions()));
            Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeStored(source.AsMemory(0, 3), descriptor, RawDecodeRegion.CreateWhole(descriptor), unchanged, new RawDecodeOptions()));
            Assert.Throws<RawLimitException>(() => GenericRawCodec.DecodeStored(source, descriptor, RawDecodeRegion.CreateWhole(descriptor), unchanged, allocation));
            Assert.Throws<RawLimitException>(() => GenericRawCodec.DecodeStored(source, descriptor, RawDecodeRegion.CreateWhole(descriptor), unchanged, decoded));
            Assert.Throws<OperationCanceledException>(() => GenericRawCodec.DecodeStored(source, descriptor, RawDecodeRegion.CreateWhole(descriptor), unchanged, cancelled));
            Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeStored(invalidSample, narrow, RawDecodeRegion.CreateWhole(narrow), unchanged, new RawDecodeOptions()));
            Assert.That(unchanged, Is.All.EqualTo(99));
        });
    }

    private class ShortNonSeekableStream : Stream
    {
        private readonly MemoryStream Inner;
        private readonly int MaximumRead;
        public bool WasDisposed;

        public ShortNonSeekableStream(byte[] bytes, int maximumRead)
        {
            Inner = new MemoryStream(bytes, false);
            MaximumRead = maximumRead;
            WasDisposed = false;
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
            return Inner.Read(buffer, offset, Math.Min(count, MaximumRead));
        }

        public override int Read(Span<byte> buffer)
        {
            return Inner.Read(buffer.Slice(0, Math.Min(buffer.Length, MaximumRead)));
        }

        public override int ReadByte()
        {
            return Inner.ReadByte();
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
