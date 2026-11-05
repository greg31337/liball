using raw;

namespace raw.Tests;

public class GenericRawRoundTripTests
{
    [TestCase(RawBitOrder.MostSignificantBitFirst, RawSampleLayout.Interleaved)]
    [TestCase(RawBitOrder.MostSignificantBitFirst, RawSampleLayout.Planar)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst, RawSampleLayout.Interleaved)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst, RawSampleLayout.Planar)]
    public void EveryGenericPackedWidthRoundTripsExactly(RawBitOrder bitOrder, RawSampleLayout layout)
    {
        int bitDepth = 1;
        while (bitDepth <= 32)
        {
            RawDescriptor descriptor = CreateIntegerDescriptor(RawPacking.PackedBits, bitDepth, bitDepth, RawByteOrder.LittleEndian, bitOrder, layout, 1, 2, 0, 0);
            uint[] source = CreateUnsignedSamples(descriptor.Width * descriptor.Height * descriptor.ChannelCount, bitDepth);

            byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, new RawGenericEncodeOptions());
            uint[] decoded = new uint[source.Length];
            GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

            Assert.That(decoded, Is.EqualTo(source), $"Packed {bitDepth}-bit {bitOrder} {layout}");
            bitDepth++;
        }
    }

    [Test]
    public void EveryByteAlignedWordOrderLayoutAndSignednessRoundTripsExactly()
    {
        int[] wordBits = new int[] { 8, 16, 32 };
        RawByteOrder[] byteOrders = new RawByteOrder[] { RawByteOrder.LittleEndian, RawByteOrder.BigEndian };
        RawSampleLayout[] layouts = new RawSampleLayout[] { RawSampleLayout.Interleaved, RawSampleLayout.Planar };
        int wordIndex = 0;
        while (wordIndex < wordBits.Length)
        {
            int storedWordBits = wordBits[wordIndex];
            int bitDepth = storedWordBits == 16 ? 12 : storedWordBits == 32 ? 31 : 8;
            int orderIndex = 0;
            while (orderIndex < byteOrders.Length)
            {
                int layoutIndex = 0;
                while (layoutIndex < layouts.Length)
                {
                    RawDescriptor unsignedDescriptor = CreateIntegerDescriptor(RawPacking.ByteAligned, bitDepth, storedWordBits, byteOrders[orderIndex], RawBitOrder.MostSignificantBitFirst, layouts[layoutIndex], 2, 3, 0, 0);
                    uint[] unsignedSource = CreateUnsignedSamples(unsignedDescriptor.Width * unsignedDescriptor.Height * unsignedDescriptor.ChannelCount, bitDepth);
                    byte[] unsignedBytes = GenericRawEncoder.EncodeStored(unsignedDescriptor, unsignedSource, 0, unsignedDescriptor.Width * unsignedDescriptor.ChannelCount, new RawGenericEncodeOptions());
                    uint[] unsignedDecoded = new uint[unsignedSource.Length];
                    GenericRawCodec.DecodeStored(unsignedBytes, unsignedDescriptor, RawDecodeRegion.CreateWhole(unsignedDescriptor), unsignedDecoded, new RawDecodeOptions());

                    RawDescriptor signedDescriptor = CreateIntegerDescriptor(RawPacking.ByteAligned, bitDepth, storedWordBits, byteOrders[orderIndex], RawBitOrder.MostSignificantBitFirst, layouts[layoutIndex], 2, 3, 0, 0);
                    signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
                    int[] signedSource = CreateSignedSamples(signedDescriptor.Width * signedDescriptor.Height * signedDescriptor.ChannelCount, bitDepth);
                    byte[] signedBytes = GenericRawEncoder.EncodeStored(signedDescriptor, signedSource, 0, signedDescriptor.Width * signedDescriptor.ChannelCount, new RawGenericEncodeOptions());
                    int[] signedDecoded = new int[signedSource.Length];
                    GenericRawCodec.DecodeStored(signedBytes, signedDescriptor, RawDecodeRegion.CreateWhole(signedDescriptor), signedDecoded, new RawDecodeOptions());

                    Assert.Multiple(() =>
                    {
                        Assert.That(unsignedDecoded, Is.EqualTo(unsignedSource), $"Unsigned {bitDepth}-in-{storedWordBits} {byteOrders[orderIndex]} {layouts[layoutIndex]}");
                        Assert.That(signedDecoded, Is.EqualTo(signedSource), $"Signed {bitDepth}-in-{storedWordBits} {byteOrders[orderIndex]} {layouts[layoutIndex]}");
                    });
                    layoutIndex++;
                }
                orderIndex++;
            }
            wordIndex++;
        }
    }

    [TestCase(RawPacking.Raw10, RawSampleLayout.Interleaved)]
    [TestCase(RawPacking.Raw10, RawSampleLayout.Planar)]
    [TestCase(RawPacking.Raw12, RawSampleLayout.Interleaved)]
    [TestCase(RawPacking.Raw12, RawSampleLayout.Planar)]
    [TestCase(RawPacking.Raw14, RawSampleLayout.Interleaved)]
    [TestCase(RawPacking.Raw14, RawSampleLayout.Planar)]
    public void EveryNamedPackingAndLayoutRoundTripsPartialGroupsExactly(RawPacking packing, RawSampleLayout layout)
    {
        int bitDepth = packing == RawPacking.Raw10 ? 10 : packing == RawPacking.Raw12 ? 12 : 14;
        RawDescriptor descriptor = CreateIntegerDescriptor(packing, bitDepth, bitDepth, RawByteOrder.LittleEndian, RawBitOrder.LeastSignificantBitFirst, layout, 2, 2, 0, 0);
        uint[] source = CreateUnsignedSamples(descriptor.Width * descriptor.Height * descriptor.ChannelCount, bitDepth);

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, new RawGenericEncodeOptions());
        uint[] decoded = new uint[source.Length];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.That(decoded, Is.EqualTo(source));
    }

    [Test]
    public void NonSeekablePlanarStreamsRoundTripOffsetsPaddingPlaneGapsAndTrailingBytes()
    {
        RawDescriptor descriptor = CreateIntegerDescriptor(RawPacking.PackedBits, 11, 11, RawByteOrder.LittleEndian, RawBitOrder.LeastSignificantBitFirst, RawSampleLayout.Planar, 2, 3, 3, 4);
        uint[] source = CreateUnsignedSamples(descriptor.Width * descriptor.Height * descriptor.ChannelCount, descriptor.BitDepth);
        RawGenericEncodeOptions encodeOptions = new RawGenericEncodeOptions();
        encodeOptions.PaddingByte = 0xA6;
        encodeOptions.TrailingBytePolicy = RawTrailingBytePolicy.ZeroFill;
        ForwardOnlyOutput output = new ForwardOnlyOutput();

        GenericRawEncoder.EncodeStored(output, descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, encodeOptions);
        byte[] encoded = output.Bytes();
        ShortReadInput input = new ShortReadInput(encoded, 1);
        uint[] decoded = new uint[source.Length];
        GenericRawCodec.DecodeStored(input, 0, encoded.Length, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(source));
            Assert.That(output.WasDisposed, Is.False);
            Assert.That(input.WasDisposed, Is.False);
            Assert.That(encoded.AsSpan(0, (int)descriptor.DataOffset).ToArray(), Is.All.EqualTo(0xA6));
            Assert.That(encoded[^1], Is.Zero);
        });
    }

    [Test]
    public void PreservedNonsampleBytesRemainExactAndCroppedPlanarDecodeSelectsPixelsAndChannels()
    {
        RawDescriptor descriptor = CreateIntegerDescriptor(RawPacking.ByteAligned, 12, 16, RawByteOrder.BigEndian, RawBitOrder.MostSignificantBitFirst, RawSampleLayout.Planar, 2, 4, 5, 6);
        uint[] source = CreateUnsignedSamples(descriptor.Width * descriptor.Height * descriptor.ChannelCount, descriptor.BitDepth);
        int outputLength = checked((int)(descriptor.DataOffset + descriptor.DataLength));
        byte[] template = new byte[outputLength];
        int index = 0;
        while (index < template.Length)
        {
            template[index] = (byte)(0x40 + index % 127);
            index++;
        }
        byte[] expected = template.ToArray();
        WritePlanarUInt16Samples(expected, descriptor, source);
        RawGenericEncodeOptions encodeOptions = new RawGenericEncodeOptions();
        encodeOptions.TrailingBytePolicy = RawTrailingBytePolicy.Preserve;
        encodeOptions.PreservedBytes = template;

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, encodeOptions);
        uint[] destination = Enumerable.Repeat(uint.MaxValue, 12).ToArray();
        RawDecodeRegion crop = new RawDecodeRegion(1, 0, 2, 2, 1, 2, 2, 6);
        GenericRawCodec.DecodeStored(encoded, descriptor, crop, destination, new RawDecodeOptions());

        uint sentinel = uint.MaxValue;
        uint[] expectedCrop = new uint[]
        {
            sentinel, sentinel, source[4], source[5], source[7], source[8],
            sentinel, sentinel, source[19], source[20], source[22], source[23]
        };
        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(destination, Is.EqualTo(expectedCrop));
        });
    }

    [TestCase(RawByteOrder.LittleEndian, RawSampleLayout.Interleaved)]
    [TestCase(RawByteOrder.LittleEndian, RawSampleLayout.Planar)]
    [TestCase(RawByteOrder.BigEndian, RawSampleLayout.Interleaved)]
    [TestCase(RawByteOrder.BigEndian, RawSampleLayout.Planar)]
    public void SinglePrecisionPreservePolicyRoundTripsEveryBitPatternAcrossOrdersAndLayouts(RawByteOrder byteOrder, RawSampleLayout layout)
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(2, 1, 32, 32, byteOrder);
        descriptor.SampleKind = RawSampleKind.Single;
        descriptor.Layout = layout;
        ConfigureGeometry(descriptor, 1, 2, 0, 0);
        uint[] sourceBits = new uint[] { 0x00000000, 0x80000000, 0x00000001, 0x7F800000, 0xFF800000, 0x7FC12345 };
        float[] source = new float[sourceBits.Length];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = BitConverter.UInt32BitsToSingle(sourceBits[index]);
            index++;
        }

        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, descriptor.Width * descriptor.ChannelCount, new RawGenericEncodeOptions());
        float[] decoded = new float[source.Length];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());
        uint[] decodedBits = new uint[decoded.Length];
        index = 0;
        while (index < decoded.Length)
        {
            decodedBits[index] = BitConverter.SingleToUInt32Bits(decoded[index]);
            index++;
        }

        Assert.That(decodedBits, Is.EqualTo(sourceBits));
    }

    [Test]
    public void NonFinitePoliciesRejectBeforeWritingOrClampDeterministically()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(3, 1, 32, 32, RawByteOrder.LittleEndian);
        descriptor.SampleKind = RawSampleKind.Single;
        float[] source = new float[] { BitConverter.UInt32BitsToSingle(0x7FC12345), float.PositiveInfinity, float.NegativeInfinity };
        RawGenericEncodeOptions reject = new RawGenericEncodeOptions();
        reject.NonFinitePolicy = RawNonFinitePolicy.Reject;
        ForwardOnlyOutput rejectedOutput = new ForwardOnlyOutput();
        RawGenericEncodeOptions clamp = new RawGenericEncodeOptions();
        clamp.NonFinitePolicy = RawNonFinitePolicy.Clamp;

        Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(rejectedOutput, descriptor, source, 0, 3, reject));
        byte[] encoded = GenericRawEncoder.EncodeStored(descriptor, source, 0, 3, clamp);
        float[] decoded = new float[3];
        GenericRawCodec.DecodeStored(encoded, descriptor, RawDecodeRegion.CreateWhole(descriptor), decoded, new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rejectedOutput.Bytes(), Is.Empty);
            Assert.That(decoded[0], Is.Zero);
            Assert.That(decoded[1], Is.EqualTo(float.MaxValue));
            Assert.That(decoded[2], Is.EqualTo(-float.MaxValue));
        });
    }

    [Test]
    public void InvalidDescriptorsAreRejectedByBothEncodingAndDecoding()
    {
        RawDescriptor[] descriptors = CreateInvalidDescriptors();
        int index = 0;
        while (index < descriptors.Length)
        {
            RawDescriptor descriptor = descriptors[index];
            Assert.That(descriptor.Validate(new RawLimits()).IsValid(), Is.False, $"Invalid descriptor case {index}");
            Assert.Throws<RawWriteException>(() => GenericRawEncoder.EncodeStored(descriptor, new uint[64], 0, 8, new RawGenericEncodeOptions()), $"Encoder descriptor case {index}");
            Assert.Throws<RawFormatException>(() => GenericRawCodec.DecodeStored(new byte[64], descriptor, new RawDecodeRegion(0, 0, 1, 1, 0, 1, 0, 1), new uint[1], new RawDecodeOptions()), $"Decoder descriptor case {index}");
            index++;
        }
    }

    private static RawDescriptor CreateIntegerDescriptor(RawPacking packing, int bitDepth, int storedWordBits, RawByteOrder byteOrder, RawBitOrder bitOrder, RawSampleLayout layout, int rowPadding, int planeGap, int dataOffset, int trailingBytes)
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(5, 2, 8, 8, byteOrder);
        descriptor.BitDepth = bitDepth;
        descriptor.StoredWordBits = storedWordBits;
        descriptor.Packing = packing;
        descriptor.ByteOrder = byteOrder;
        descriptor.BitOrder = bitOrder;
        descriptor.Layout = layout;
        ConfigureGeometry(descriptor, rowPadding, planeGap, dataOffset, trailingBytes);
        return descriptor;
    }

    private static void ConfigureGeometry(RawDescriptor descriptor, int rowPadding, int planeGap, int dataOffset, int trailingBytes)
    {
        descriptor.RowStrideBytes = descriptor.GetMinimumRowBytes() + rowPadding;
        descriptor.PlaneStrideBytes = descriptor.Layout == RawSampleLayout.Planar ? descriptor.RowStrideBytes * descriptor.Height + planeGap : 0;
        descriptor.DataOffset = dataOffset;
        descriptor.DataLength = descriptor.GetRequiredDataLength() + trailingBytes;
        descriptor.AllowTrailingBytes = trailingBytes != 0;
    }

    private static uint[] CreateUnsignedSamples(int count, int bitDepth)
    {
        uint mask = bitDepth == 32 ? uint.MaxValue : (1U << bitDepth) - 1U;
        uint[] result = new uint[count];
        int index = 0;
        while (index < count)
        {
            if (index % 5 == 0)
            {
                result[index] = 0;
            }
            else if (index % 5 == 1)
            {
                result[index] = mask;
            }
            else if (index % 5 == 2)
            {
                result[index] = mask / 2;
            }
            else
            {
                result[index] = unchecked((uint)(index * 2654435761U)) & mask;
            }
            index++;
        }
        return result;
    }

    private static int[] CreateSignedSamples(int count, int bitDepth)
    {
        uint[] codes = CreateUnsignedSamples(count, bitDepth);
        int[] result = new int[count];
        uint signBit = 1U << (bitDepth - 1);
        int index = 0;
        while (index < result.Length)
        {
            uint code = codes[index];
            if (bitDepth == 32)
            {
                result[index] = unchecked((int)code);
            }
            else if ((code & signBit) == 0)
            {
                result[index] = (int)code;
            }
            else
            {
                result[index] = (int)((long)code - (1L << bitDepth));
            }
            index++;
        }
        return result;
    }

    private static void WritePlanarUInt16Samples(byte[] destination, RawDescriptor descriptor, uint[] source)
    {
        int channel = 0;
        while (channel < descriptor.ChannelCount)
        {
            int row = 0;
            while (row < descriptor.Height)
            {
                int pixel = 0;
                while (pixel < descriptor.Width)
                {
                    int sourceIndex = (row * descriptor.Width + pixel) * descriptor.ChannelCount + channel;
                    long offset = descriptor.DataOffset + channel * descriptor.PlaneStrideBytes + row * descriptor.RowStrideBytes + pixel * 2;
                    EndianWriter.WriteUInt16(destination.AsSpan((int)offset, 2), (ushort)source[sourceIndex], descriptor.ByteOrder);
                    pixel++;
                }
                row++;
            }
            channel++;
        }
    }

    private static RawDescriptor[] CreateInvalidDescriptors()
    {
        RawDescriptor noWidth = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        noWidth.Width = 0;
        RawDescriptor wrongPackedWidth = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        wrongPackedWidth.Packing = RawPacking.PackedBits;
        wrongPackedWidth.StoredWordBits = 7;
        RawDescriptor shortRow = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        shortRow.RowStrideBytes = 1;
        RawDescriptor undefinedOrder = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        undefinedOrder.ByteOrder = (RawByteOrder)99;
        RawDescriptor missingChannels = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        missingChannels.Channels = Array.Empty<RawChannelDescription>();
        RawDescriptor shortData = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        shortData.DataLength = 1;
        return new RawDescriptor[] { noWidth, wrongPackedWidth, shortRow, undefinedOrder, missingChannels, shortData };
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

    private class ShortReadInput : Stream
    {
        private readonly MemoryStream Inner;
        private readonly int MaximumRead;
        public bool WasDisposed;

        public ShortReadInput(byte[] bytes, int maximumRead)
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
