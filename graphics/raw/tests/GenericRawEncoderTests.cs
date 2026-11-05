using raw;

namespace raw.Tests;

public class GenericRawEncoderTests
{
    [Test]
    public void InterleavedByteAlignedEncodingUsesSourceStrideDescriptorOffsetAndRowPadding()
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(2, 2, 8, 8, RawByteOrder.LittleEndian);
        descriptor.RowStrideBytes = 8;
        descriptor.DataOffset = 2;
        descriptor.DataLength = 16;
        uint[] source = new uint[]
        {
            999, 1, 2, 3, 4, 5, 6, 888, 888,
            11, 12, 13, 14, 15, 16, 777, 777
        };
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();
        options.PaddingByte = 0xA5;

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 1, 8, options);
        uint[] decoded = new uint[12];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                0xA5, 0xA5,
                1, 2, 3, 4, 5, 6, 0xA5, 0xA5,
                11, 12, 13, 14, 15, 16, 0xA5, 0xA5
            }));
            Assert.That(decoded, Is.EqualTo(new uint[] { 1, 2, 3, 4, 5, 6, 11, 12, 13, 14, 15, 16 }));
        });
    }

    [Test]
    public void PlanarSignedBigEndianEncodingIsTheExactInverseOfPixelInterleavedDecode()
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(2, 2, 12, 16, RawByteOrder.BigEndian);
        descriptor.SampleKind = RawSampleKind.SignedInteger;
        descriptor.Layout = RawSampleLayout.Planar;
        descriptor.RowStrideBytes = 5;
        descriptor.PlaneStrideBytes = 12;
        descriptor.DataLength = 34;
        int[] source = new int[]
        {
            -2048, -100, 0, 100, 1024, 2047,
            -7, -6, -5, 5, 6, 7
        };

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, 6, new RawGenericEncodeOptions());
        int[] decoded = new int[source.Length];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(source));
            Assert.That(encoded[4], Is.Zero);
            Assert.That(encoded[9], Is.Zero);
            Assert.That(encoded[10], Is.Zero);
            Assert.That(encoded[11], Is.Zero);
        });
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void GenericPackedSignedEncodingRoundTripsBothBitOrdersAndOddRows(RawBitOrder bitOrder)
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(5, 2, 11, 16, RawByteOrder.LittleEndian);
        descriptor.SampleKind = RawSampleKind.SignedInteger;
        descriptor.Packing = RawPacking.PackedBits;
        descriptor.StoredWordBits = 11;
        descriptor.BitOrder = bitOrder;
        descriptor.RowStrideBytes = 8;
        descriptor.DataLength = 16;
        int[] source = new int[] { -1024, -511, -1, 0, 1023, 17, 18, 19, 20, 21 };

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, 5, new RawGenericEncodeOptions());
        int[] decoded = new int[source.Length];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.That(decoded, Is.EqualTo(source));
    }

    [TestCase(RawPacking.Raw10)]
    [TestCase(RawPacking.Raw12)]
    [TestCase(RawPacking.Raw14)]
    public void NamedPackedEncodingRoundTripsPartialGroupsAndRowPadding(RawPacking packing)
    {
        RawDescriptor descriptor = RawDescriptor.CreatePackedBayer(5, 2, packing, RawCfaLayout.Bggr);
        long minimum = descriptor.RowStrideBytes;
        descriptor.RowStrideBytes = minimum + 1;
        descriptor.DataLength = descriptor.RowStrideBytes * descriptor.Height;
        uint maximum = (1U << descriptor.BitDepth) - 1U;
        uint[] source = new uint[] { 0, 1, maximum, maximum / 2, 17, 21, 22, 23, 24, 25 };
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();
        options.PaddingByte = 0x6D;

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, 5, options);
        uint[] decoded = new uint[source.Length];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(source));
            Assert.That(encoded[(int)minimum], Is.EqualTo(0x6D));
            Assert.That(encoded[(int)descriptor.RowStrideBytes + (int)minimum], Is.EqualTo(0x6D));
        });
    }

    [Test]
    public void NormalizedRoundingRangeAndSignedPoliciesAreExplicit()
    {
        RawDescriptor unsigned = RawDescriptor.CreateGray(3, 1, 4, 8, RawByteOrder.LittleEndian);
        float[] fractions = new float[] { 0.25F, 0.5F, 0.75F };
        uint[] nearest = DecodeUnsigned(unsigned, GenericRawEncoder.EncodeNormalized(unsigned, fractions, 0, 3, new RawGenericEncodeOptions()));
        RawGenericEncodeOptions truncateOptions = new RawGenericEncodeOptions();
        truncateOptions.RoundingMode = RawSampleRoundingMode.TowardZero;
        uint[] truncated = DecodeUnsigned(unsigned, GenericRawEncoder.EncodeNormalized(unsigned, fractions, 0, 3, truncateOptions));
        RawGenericEncodeOptions clampOptions = new RawGenericEncodeOptions();
        clampOptions.RangePolicy = RawSampleRangePolicy.Clamp;
        uint[] clamped = DecodeUnsigned(unsigned, GenericRawEncoder.EncodeNormalized(unsigned, new float[] { -1.0F, 0.5F, 2.0F }, 0, 3, clampOptions));

        RawDescriptor signed = RawDescriptor.CreateGray(3, 1, 4, 8, RawByteOrder.LittleEndian);
        signed.SampleKind = RawSampleKind.SignedInteger;
        ushort[] normalized = new ushort[] { 0, 32768, 65535 };
        int[] fullRange = DecodeSigned(signed, GenericRawEncoder.EncodeNormalized(signed, normalized, 0, 3, new RawGenericEncodeOptions()));
        RawGenericEncodeOptions nonnegativeOptions = new RawGenericEncodeOptions();
        nonnegativeOptions.SignedPolicy = RawSignedEncodingPolicy.NonNegative;
        int[] nonnegative = DecodeSigned(signed, GenericRawEncoder.EncodeNormalized(signed, normalized, 0, 3, nonnegativeOptions));

        Assert.Multiple(() =>
        {
            Assert.That(nearest, Is.EqualTo(new uint[] { 4, 8, 11 }));
            Assert.That(truncated, Is.EqualTo(new uint[] { 3, 7, 11 }));
            Assert.That(clamped, Is.EqualTo(new uint[] { 0, 8, 15 }));
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeNormalized(unsigned, new float[] { -0.1F, 0.5F, 1.1F }, 0, 3, new RawGenericEncodeOptions()));
            Assert.That(fullRange, Is.EqualTo(new int[] { -8, 0, 7 }));
            Assert.That(nonnegative, Is.EqualTo(new int[] { 0, 4, 7 }));
        });
    }

    [Test]
    public void HalfAndSinglePoliciesPreserveRejectOrClampNonFiniteAndOutOfRangeValues()
    {
        RawDescriptor single = RawDescriptor.CreateGray(3, 1, 32, 32, RawByteOrder.BigEndian);
        single.SampleKind = RawSampleKind.Single;
        float[] special = new float[] { BitConverter.UInt32BitsToSingle(0x7FC12345), float.PositiveInfinity, float.NegativeInfinity };
        byte[] preserved = GenericRawEncoder.EncodeStored(single, special, 0, 3, new RawGenericEncodeOptions());
        float[] decoded = new float[3];
        GenericRawCodec.DecodeStored(preserved, single, RawDecodeRegion.CreateWhole(single), decoded, new RawDecodeOptions());
        RawGenericEncodeOptions reject = new RawGenericEncodeOptions();
        reject.NonFinitePolicy = RawNonFinitePolicy.Reject;
        RawGenericEncodeOptions clamp = new RawGenericEncodeOptions();
        clamp.NonFinitePolicy = RawNonFinitePolicy.Clamp;
        byte[] clamped = GenericRawEncoder.EncodeStored(single, special, 0, 3, clamp);
        float[] clampedDecoded = new float[3];
        GenericRawCodec.DecodeStored(clamped, single, RawDecodeRegion.CreateWhole(single), clampedDecoded, new RawDecodeOptions());

        RawDescriptor half = RawDescriptor.CreateGray(1, 1, 16, 16, RawByteOrder.LittleEndian);
        half.SampleKind = RawSampleKind.Half;
        RawGenericEncodeOptions halfClamp = new RawGenericEncodeOptions();
        halfClamp.RangePolicy = RawSampleRangePolicy.Clamp;
        byte[] halfBytes = GenericRawEncoder.EncodeStored(half, new float[] { 70000.0F }, 0, 1, halfClamp);
        float[] halfDecoded = new float[1];
        GenericRawCodec.DecodeStored(halfBytes, half, RawDecodeRegion.CreateWhole(half), halfDecoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Select(BitConverter.SingleToUInt32Bits), Is.EqualTo(special.Select(BitConverter.SingleToUInt32Bits)));
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(single, special, 0, 3, reject));
            Assert.That(clampedDecoded[0], Is.Zero);
            Assert.That(clampedDecoded[1], Is.EqualTo(float.MaxValue));
            Assert.That(clampedDecoded[2], Is.EqualTo(-float.MaxValue));
            Assert.That(halfDecoded[0], Is.EqualTo(65504.0F));
        });
    }

    [Test]
    public void TrailingBytePoliciesRejectZeroFillOrPreserveEveryNonsampleByte()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        descriptor.DataOffset = 2;
        descriptor.DataLength = 5;
        descriptor.AllowTrailingBytes = true;
        uint[] source = new uint[] { 10, 20 };
        RawGenericEncodeOptions reject = new RawGenericEncodeOptions();
        RawGenericEncodeOptions zero = new RawGenericEncodeOptions();
        zero.TrailingBytePolicy = RawTrailingBytePolicy.ZeroFill;
        zero.PaddingByte = 0xA5;
        RawGenericEncodeOptions preserve = new RawGenericEncodeOptions();
        preserve.TrailingBytePolicy = RawTrailingBytePolicy.Preserve;
        preserve.PreservedBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7 };

        byte[] zeroed = GenericRawEncoder.EncodeStored(descriptor, source, 0, 2, zero);
        byte[] retained = GenericRawEncoder.EncodeStored(descriptor, source, 0, 2, preserve);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(descriptor, source, 0, 2, reject));
            Assert.That(zeroed, Is.EqualTo(new byte[] { 0xA5, 0xA5, 10, 20, 0, 0, 0 }));
            Assert.That(retained, Is.EqualTo(new byte[] { 1, 2, 10, 20, 5, 6, 7 }));
        });
    }

    [Test]
    public void StreamingEncodingNeedsOnlyRowAllocationAndLeavesNonSeekableOutputOpen()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(1, 10, 8, 8, RawByteOrder.LittleEndian);
        uint[] source = Enumerable.Range(0, 10).Select(value => (uint)value).ToArray();
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();
        options.Limits.MaxAllocationBytes = 4;
        ForwardOnlyOutput output = new ForwardOnlyOutput();

        GenericRawEncoder.EncodeStored(output, descriptor, source, 0, 1, options);

        Assert.Multiple(() =>
        {
            Assert.That(output.Bytes(), Is.EqualTo(Enumerable.Range(0, 10).Select(value => (byte)value)));
            Assert.That(output.WasDisposed, Is.False);
            Assert.Throws<RawLimitException>(() => GenericRawEncoder.EncodeStored(descriptor, source, 0, 1, options));
        });
    }

    [Test]
    public void InvalidKindsSourcesOptionsLimitsAndCancellationFailBeforeWriting()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawGenericEncodeOptions invalidOptions = new RawGenericEncodeOptions();
        invalidOptions.RoundingMode = (RawSampleRoundingMode)99;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawGenericEncodeOptions cancelled = new RawGenericEncodeOptions();
        cancelled.CancellationToken = cancellation.Token;
        RawGenericEncodeOptions limited = new RawGenericEncodeOptions();
        limited.Limits.MaxDecodedBytes = 4;
        limited.Limits.MaxAllocationBytes = 4;
        ForwardOnlyOutput output = new ForwardOnlyOutput();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(descriptor, new uint[] { 1 }, 0, 2, new RawGenericEncodeOptions()));
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(descriptor, new int[] { 1, 2 }, 0, 2, new RawGenericEncodeOptions()));
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(descriptor, new uint[] { 1, 256 }, 0, 2, new RawGenericEncodeOptions()));
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(descriptor, new uint[] { 1, 2 }, 0, 2, invalidOptions));
            Assert.Throws<OperationCanceledException>(() => GenericRawEncoder.EncodeStored(descriptor, new uint[] { 1, 2 }, 0, 2, cancelled));
            Assert.Throws<RawLimitException>(() => GenericRawEncoder.EncodeStored(RawDescriptor.CreateGray(2, 2, 8, 8, RawByteOrder.LittleEndian), new uint[] { 1, 2, 3, 4 }, 0, 2, limited));
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(output, descriptor, new uint[] { 1, 256 }, 0, 2, new RawGenericEncodeOptions()));
            Assert.That(output.Bytes(), Is.Empty);
            Assert.That(new RawGenericEncodeOptions().Validate().IsValid(), Is.True);
            Assert.That(invalidOptions.Validate().IsValid(), Is.False);
        });
    }

    private static uint[] DecodeUnsigned(RawDescriptor descriptor, byte[] encoded)
    {
        uint[] result = new uint[descriptor.Width * descriptor.Height * descriptor.ChannelCount];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), result, new RawDecodeOptions());
        return result;
    }

    private static int[] DecodeSigned(RawDescriptor descriptor, byte[] encoded)
    {
        int[] result = new int[descriptor.Width * descriptor.Height * descriptor.ChannelCount];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), result, new RawDecodeOptions());
        return result;
    }

    private class ForwardOnlyOutput : Stream
    {
        private readonly MemoryStream Inner;
        public bool WasDisposed;

        public ForwardOnlyOutput()
        {
            Inner = new MemoryStream();
            WasDisposed = false;
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

        public byte[] Bytes()
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
            WasDisposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
