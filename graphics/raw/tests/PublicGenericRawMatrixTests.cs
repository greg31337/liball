using raw;

namespace raw.Tests;

public class PublicGenericRawMatrixTests
{
    [Test]
    public void EveryDescriptorPresetAndBayerPhaseRoundTripsStoredSamples()
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawDescriptor[] descriptors = new RawDescriptor[]
        {
            codec.CreateGrayDescriptor(5, 2, 12, 16, RawByteOrder.BigEndian),
            codec.CreateRgbDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian),
            codec.CreateBgrDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian),
            codec.CreateRgbaDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian),
            codec.CreateBgraDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian),
            codec.CreateBayerDescriptor(5, 2, 12, 16, RawByteOrder.LittleEndian, RawCfaLayout.Rggb),
            codec.CreateBayerDescriptor(5, 2, 12, 16, RawByteOrder.LittleEndian, RawCfaLayout.Bggr),
            codec.CreateBayerDescriptor(5, 2, 12, 16, RawByteOrder.LittleEndian, RawCfaLayout.Grbg),
            codec.CreateBayerDescriptor(5, 2, 12, 16, RawByteOrder.LittleEndian, RawCfaLayout.Gbrg),
            codec.CreateArbitraryCfaDescriptor(5, 2, 10, 16, RawByteOrder.BigEndian, 3, 2, new RawCfaColor[]
            {
                RawCfaColor.Cyan, RawCfaColor.Magenta, RawCfaColor.Yellow,
                RawCfaColor.Green2, RawCfaColor.White, RawCfaColor.Red
            }),
            codec.CreateRaw10Descriptor(5, 2, RawCfaLayout.Rggb),
            codec.CreateRaw12Descriptor(5, 2, RawCfaLayout.Bggr),
            codec.CreateRaw14Descriptor(5, 2, RawCfaLayout.Grbg)
        };

        int descriptorIndex = 0;
        while (descriptorIndex < descriptors.Length)
        {
            RawDescriptor descriptor = descriptors[descriptorIndex];
            uint[] source = UnsignedSamples(descriptor);
            byte[] encoded = writer.EncodeGenericUnsigned(descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, new RawGenericEncodeOptions());
            uint[] decoded = new uint[source.Length];
            codec.DecodeGenericUnsigned(encoded, descriptor, WholeRegion(descriptor), decoded, new RawDecodeOptions());

            Assert.Multiple(() =>
            {
                Assert.That(codec.ValidateDescriptor(descriptor, new RawLimits()).IsValid(), Is.True, $"Descriptor preset {descriptorIndex}");
                Assert.That(encoded.LongLength, Is.EqualTo(descriptor.DataOffset + descriptor.DataLength), $"Descriptor preset {descriptorIndex}");
                Assert.That(decoded, Is.EqualTo(source), $"Descriptor preset {descriptorIndex}");
            });
            descriptorIndex++;
        }
    }

    [Test]
    public void EverySampleKindByteOrderAndLayoutRoundTripsThroughPublicInterfaces()
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawByteOrder[] byteOrders = new RawByteOrder[] { RawByteOrder.LittleEndian, RawByteOrder.BigEndian };
        RawSampleLayout[] layouts = new RawSampleLayout[] { RawSampleLayout.Interleaved, RawSampleLayout.Planar };

        int orderIndex = 0;
        while (orderIndex < byteOrders.Length)
        {
            int layoutIndex = 0;
            while (layoutIndex < layouts.Length)
            {
                RawDescriptor unsignedDescriptor = codec.CreateRgbDescriptor(3, 2, 12, 16, byteOrders[orderIndex]);
                SetLayout(unsignedDescriptor, layouts[layoutIndex]);
                uint[] unsignedSource = UnsignedSamples(unsignedDescriptor);
                uint[] unsignedDecoded = new uint[unsignedSource.Length];
                byte[] unsignedBytes = writer.EncodeGenericUnsigned(unsignedDescriptor, unsignedSource, 0, 9, new RawGenericEncodeOptions());
                codec.DecodeGenericUnsigned(unsignedBytes, unsignedDescriptor, WholeRegion(unsignedDescriptor), unsignedDecoded, new RawDecodeOptions());

                RawDescriptor signedDescriptor = codec.CreateRgbDescriptor(3, 2, 12, 16, byteOrders[orderIndex]);
                signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
                SetLayout(signedDescriptor, layouts[layoutIndex]);
                int[] signedSource = SignedSamples(signedDescriptor);
                int[] signedDecoded = new int[signedSource.Length];
                byte[] signedBytes = writer.EncodeGenericSigned(signedDescriptor, signedSource, 0, 9, new RawGenericEncodeOptions());
                codec.DecodeGenericSigned(signedBytes, signedDescriptor, WholeRegion(signedDescriptor), signedDecoded, new RawDecodeOptions());

                RawDescriptor halfDescriptor = codec.CreateRgbDescriptor(3, 2, 16, 16, byteOrders[orderIndex]);
                halfDescriptor.SampleKind = RawSampleKind.Half;
                SetLayout(halfDescriptor, layouts[layoutIndex]);
                float[] halfSource = FloatingSamples(halfDescriptor);
                float[] halfDecoded = new float[halfSource.Length];
                byte[] halfBytes = writer.EncodeGenericFloating(halfDescriptor, halfSource, 0, 9, new RawGenericEncodeOptions());
                codec.DecodeGenericFloating(halfBytes, halfDescriptor, WholeRegion(halfDescriptor), halfDecoded, new RawDecodeOptions());

                RawDescriptor singleDescriptor = codec.CreateRgbDescriptor(3, 2, 32, 32, byteOrders[orderIndex]);
                singleDescriptor.SampleKind = RawSampleKind.Single;
                SetLayout(singleDescriptor, layouts[layoutIndex]);
                float[] singleSource = FloatingSamples(singleDescriptor);
                float[] singleDecoded = new float[singleSource.Length];
                byte[] singleBytes = writer.EncodeGenericFloating(singleDescriptor, singleSource, 0, 9, new RawGenericEncodeOptions());
                codec.DecodeGenericFloating(singleBytes, singleDescriptor, WholeRegion(singleDescriptor), singleDecoded, new RawDecodeOptions());

                Assert.Multiple(() =>
                {
                    Assert.That(unsignedDecoded, Is.EqualTo(unsignedSource), $"Unsigned {byteOrders[orderIndex]} {layouts[layoutIndex]}");
                    Assert.That(signedDecoded, Is.EqualTo(signedSource), $"Signed {byteOrders[orderIndex]} {layouts[layoutIndex]}");
                    Assert.That(halfDecoded, Is.EqualTo(halfSource), $"Half {byteOrders[orderIndex]} {layouts[layoutIndex]}");
                    Assert.That(singleDecoded, Is.EqualTo(singleSource), $"Single {byteOrders[orderIndex]} {layouts[layoutIndex]}");
                });
                layoutIndex++;
            }
            orderIndex++;
        }
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst, RawSampleLayout.Interleaved)]
    [TestCase(RawBitOrder.MostSignificantBitFirst, RawSampleLayout.Planar)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst, RawSampleLayout.Interleaved)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst, RawSampleLayout.Planar)]
    public void GenericPackedBitsRoundTripEveryBitOrderAndLayout(RawBitOrder bitOrder, RawSampleLayout layout)
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawDescriptor descriptor = codec.CreateRgbDescriptor(5, 2, 8, 8, RawByteOrder.LittleEndian);
        descriptor.BitDepth = 11;
        descriptor.StoredWordBits = 11;
        descriptor.Packing = RawPacking.PackedBits;
        descriptor.BitOrder = bitOrder;
        SetLayout(descriptor, layout);
        uint[] source = UnsignedSamples(descriptor);
        byte[] encoded = writer.EncodeGenericUnsigned(descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, new RawGenericEncodeOptions());
        uint[] decoded = new uint[source.Length];

        codec.DecodeGenericUnsigned(encoded, descriptor, WholeRegion(descriptor), decoded, new RawDecodeOptions());

        Assert.That(decoded, Is.EqualTo(source));
    }

    [Test]
    public void MemoryStreamAndPathDestinationsRoundTripThroughHighLevelDecode()
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawDescriptor descriptor = codec.CreateRgbDescriptor(3, 2, 12, 16, RawByteOrder.BigEndian);
        SetLayout(descriptor, RawSampleLayout.Planar);
        uint[] source = UnsignedSamples(descriptor);
        RawGenericEncodeOptions encodeOptions = new RawGenericEncodeOptions();
        RawDecodeOptions decodeOptions = new RawDecodeOptions();
        decodeOptions.PreserveStoredSamples = true;
        byte[] memoryBytes = writer.EncodeGenericUnsigned(descriptor, source, 0, 9, encodeOptions);
        RawSensorImage memoryImage = codec.DecodeBytes(memoryBytes, descriptor, decodeOptions);
        MemoryStream stream = new MemoryStream();
        writer.EncodeGenericUnsignedToStream(stream, descriptor, source, 0, 9, encodeOptions);
        stream.Position = 0;
        RawSensorImage streamImage = codec.DecodeStream(stream, 0, stream.Length, descriptor, decodeOptions);
        string directory = Path.Combine(Path.GetTempPath(), "raw-public-generic-matrix-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string path = Path.Combine(directory, "round-trip.raw");
            RawPathWriteResult result = writer.EncodeGenericUnsignedToPath(path, descriptor, source, 0, 9, encodeOptions);
            RawSensorImage pathImage = codec.DecodePath(path, descriptor, decodeOptions);

            Assert.Multiple(() =>
            {
                Assert.That((memoryImage.StoredImage ?? throw new InvalidOperationException()).UnsignedSamples, Is.EqualTo(source));
                Assert.That((streamImage.StoredImage ?? throw new InvalidOperationException()).UnsignedSamples, Is.EqualTo(source));
                Assert.That((pathImage.StoredImage ?? throw new InvalidOperationException()).UnsignedSamples, Is.EqualTo(source));
                Assert.That(result.Validated, Is.True);
                Assert.That(result.Committed, Is.True);
                Assert.That(stream.CanRead, Is.True);
            });
        }
        finally
        {
            stream.Dispose();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicOperationsRejectInvalidGeometryAndPreserveFloatingNonFiniteValues()
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawDescriptor invalid = codec.CreateRgbDescriptor(3, 2, 12, 16, RawByteOrder.LittleEndian);
        invalid.RowStrideBytes--;
        uint[] integerSource = new uint[18];

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidateDescriptor(invalid, new RawLimits()).IsValid(), Is.False);
            Assert.Throws<RawFormatException>(() => codec.DecodeGenericUnsigned(new byte[invalid.DataLength], invalid, WholeRegion(invalid), integerSource, new RawDecodeOptions()));
            Assert.Throws<RawWriteException>(() => writer.EncodeGenericUnsigned(invalid, integerSource, 0, 9, new RawGenericEncodeOptions()));
        });

        RawDescriptor floating = codec.CreateGrayDescriptor(3, 1, 32, 32, RawByteOrder.BigEndian);
        floating.SampleKind = RawSampleKind.Single;
        float[] source = new float[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        byte[] encoded = writer.EncodeGenericFloating(floating, source, 0, 3, new RawGenericEncodeOptions());
        float[] decoded = new float[3];
        codec.DecodeGenericFloating(encoded, floating, WholeRegion(floating), decoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(float.IsNaN(decoded[0]), Is.True);
            Assert.That(float.IsPositiveInfinity(decoded[1]), Is.True);
            Assert.That(float.IsNegativeInfinity(decoded[2]), Is.True);
        });
    }

    private static void SetLayout(RawDescriptor descriptor, RawSampleLayout layout)
    {
        descriptor.Layout = layout;
        long samplesPerRow = layout == RawSampleLayout.Interleaved ? (long)descriptor.Width * descriptor.ChannelCount : descriptor.Width;
        descriptor.RowStrideBytes = descriptor.Packing == RawPacking.ByteAligned
            ? samplesPerRow * (descriptor.StoredWordBits / 8)
            : (samplesPerRow * descriptor.BitDepth + 7) / 8;
        descriptor.PlaneStrideBytes = layout == RawSampleLayout.Planar ? descriptor.RowStrideBytes * descriptor.Height : 0;
        descriptor.DataLength = layout == RawSampleLayout.Planar
            ? descriptor.PlaneStrideBytes * descriptor.ChannelCount
            : descriptor.RowStrideBytes * descriptor.Height;
    }

    private static uint[] UnsignedSamples(RawDescriptor descriptor)
    {
        int count = descriptor.Width * descriptor.Height * descriptor.ChannelCount;
        uint maximum = descriptor.BitDepth == 32 ? uint.MaxValue : (1U << descriptor.BitDepth) - 1U;
        uint[] result = new uint[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = ((uint)index * 137U + 19U) & maximum;
            index++;
        }
        return result;
    }

    private static int[] SignedSamples(RawDescriptor descriptor)
    {
        int count = descriptor.Width * descriptor.Height * descriptor.ChannelCount;
        int minimum = -(1 << (descriptor.BitDepth - 1));
        int range = 1 << descriptor.BitDepth;
        int[] result = new int[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = minimum + (index * 137 + 19) % range;
            index++;
        }
        return result;
    }

    private static float[] FloatingSamples(RawDescriptor descriptor)
    {
        float[] values = new float[] { -2.0F, -1.0F, -0.5F, 0.0F, 0.5F, 1.0F, 2.0F };
        float[] result = new float[descriptor.Width * descriptor.Height * descriptor.ChannelCount];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = values[index % values.Length];
            index++;
        }
        return result;
    }

    private static RawDecodeRegion WholeRegion(RawDescriptor descriptor)
    {
        return new RawDecodeRegion(0, 0, descriptor.Width, descriptor.Height, 0, descriptor.ChannelCount, 0, descriptor.Width * descriptor.ChannelCount);
    }
}
