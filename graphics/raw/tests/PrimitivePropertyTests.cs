using raw;

namespace raw.Tests;

public class PrimitivePropertyTests
{
    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void EndianPrimitivesRoundTripRandomBitPatterns(RawByteOrder byteOrder)
    {
        Random random = new Random(1701 + (int)byteOrder);
        byte[] bytes = new byte[8];
        int iteration = 0;
        while (iteration < 256)
        {
            ushort value16 = (ushort)random.Next(0, 65536);
            short signed16 = (short)value16;
            uint value32 = NextUInt32(random);
            int signed32 = (int)value32;
            ulong value64 = NextUInt64(random);
            long signed64 = (long)value64;
            Half half = BitConverter.UInt16BitsToHalf(value16);
            float single = BitConverter.UInt32BitsToSingle(value32);
            double doubleValue = BitConverter.UInt64BitsToDouble(value64);

            EndianWriter.WriteUInt16(bytes, value16, byteOrder);
            Assert.That(EndianReader.ReadUInt16(bytes, byteOrder), Is.EqualTo(value16));
            EndianWriter.WriteInt16(bytes, signed16, byteOrder);
            Assert.That(EndianReader.ReadInt16(bytes, byteOrder), Is.EqualTo(signed16));
            EndianWriter.WriteUInt32(bytes, value32, byteOrder);
            Assert.That(EndianReader.ReadUInt32(bytes, byteOrder), Is.EqualTo(value32));
            EndianWriter.WriteInt32(bytes, signed32, byteOrder);
            Assert.That(EndianReader.ReadInt32(bytes, byteOrder), Is.EqualTo(signed32));
            EndianWriter.WriteUInt64(bytes, value64, byteOrder);
            Assert.That(EndianReader.ReadUInt64(bytes, byteOrder), Is.EqualTo(value64));
            EndianWriter.WriteInt64(bytes, signed64, byteOrder);
            Assert.That(EndianReader.ReadInt64(bytes, byteOrder), Is.EqualTo(signed64));
            EndianWriter.WriteHalf(bytes, half, byteOrder);
            Assert.That(BitConverter.HalfToUInt16Bits(EndianReader.ReadHalf(bytes, byteOrder)), Is.EqualTo(value16));
            EndianWriter.WriteSingle(bytes, single, byteOrder);
            Assert.That(BitConverter.SingleToUInt32Bits(EndianReader.ReadSingle(bytes, byteOrder)), Is.EqualTo(value32));
            EndianWriter.WriteDouble(bytes, doubleValue, byteOrder);
            Assert.That(BitConverter.DoubleToUInt64Bits(EndianReader.ReadDouble(bytes, byteOrder)), Is.EqualTo(value64));
            iteration++;
        }
    }

    [Test]
    public void MsbBitstreamRandomSequencesRoundTripThroughPeekAndRead()
    {
        Random random = new Random(30101);
        int[] widths = new int[257];
        uint[] values = new uint[257];
        long bitCount = FillBitSequence(random, widths, values);
        byte[] bytes = new byte[(int)((bitCount + 7) / 8)];
        MsbBitWriter writer = new MsbBitWriter(bytes);
        int index = 0;
        while (index < values.Length)
        {
            writer.WriteBits(values[index], widths[index]);
            index++;
        }
        writer.Finish(0);

        MsbBitReader reader = CreateMsbReader(bytes);
        index = 0;
        while (index < values.Length)
        {
            long position = reader.GetBitPosition();
            Assert.That(reader.PeekBits(widths[index]), Is.EqualTo(values[index]));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(position));
            Assert.That(reader.ReadBits(widths[index]), Is.EqualTo(values[index]));
            index++;
        }
        reader.ValidatePaddingToEnd(0);

        Assert.Multiple(() =>
        {
            Assert.That(writer.GetBitPosition(), Is.EqualTo(bytes.Length * 8));
            Assert.That(reader.IsAtEnd(), Is.True);
        });
    }

    [Test]
    public void LsbBitstreamRandomSequencesRoundTripThroughPeekAndRead()
    {
        Random random = new Random(30102);
        int[] widths = new int[257];
        uint[] values = new uint[257];
        long bitCount = FillBitSequence(random, widths, values);
        byte[] bytes = new byte[(int)((bitCount + 7) / 8)];
        LsbBitWriter writer = new LsbBitWriter(bytes);
        int index = 0;
        while (index < values.Length)
        {
            writer.WriteBits(values[index], widths[index]);
            index++;
        }
        writer.Finish(0);

        LsbBitReader reader = CreateLsbReader(bytes);
        index = 0;
        while (index < values.Length)
        {
            long position = reader.GetBitPosition();
            Assert.That(reader.PeekBits(widths[index]), Is.EqualTo(values[index]));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(position));
            Assert.That(reader.ReadBits(widths[index]), Is.EqualTo(values[index]));
            index++;
        }
        reader.ValidatePaddingToEnd(0);

        Assert.Multiple(() =>
        {
            Assert.That(writer.GetBitPosition(), Is.EqualTo(bytes.Length * 8));
            Assert.That(reader.IsAtEnd(), Is.True);
        });
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void EveryUnsignedAndSignedSampleWidthRoundTrips(RawBitOrder bitOrder)
    {
        Random random = new Random(41000 + (int)bitOrder);
        int bitDepth = 1;
        while (bitDepth <= 32)
        {
            uint mask = (uint)BitUtility.Mask(bitDepth);
            uint[] unsignedSamples = new uint[19];
            int[] signedSamples = new int[19];
            unsignedSamples[0] = 0;
            unsignedSamples[1] = mask;
            unsignedSamples[2] = mask / 2;
            int index = 3;
            while (index < unsignedSamples.Length)
            {
                unsignedSamples[index] = NextUInt32(random) & mask;
                index++;
            }
            index = 0;
            while (index < signedSamples.Length)
            {
                signedSamples[index] = DecodeSigned(unsignedSamples[index], bitDepth);
                index++;
            }

            int byteCount = BitPacking.GetPackedByteCount(unsignedSamples.Length, bitDepth);
            byte[] unsignedBytes = new byte[byteCount];
            byte[] signedBytes = new byte[byteCount];
            uint[] unsignedResult = new uint[unsignedSamples.Length];
            int[] signedResult = new int[signedSamples.Length];
            BitPacking.PackUnsigned(unsignedSamples, bitDepth, bitOrder, unsignedBytes);
            BitPacking.PackSigned(signedSamples, bitDepth, bitOrder, signedBytes);
            BitPacking.UnpackUnsigned(unsignedBytes, unsignedSamples.Length, bitDepth, bitOrder, unsignedResult);
            BitPacking.UnpackSigned(signedBytes, signedSamples.Length, bitDepth, bitOrder, signedResult);

            Assert.That(unsignedResult, Is.EqualTo(unsignedSamples), $"Unsigned width {bitDepth}");
            Assert.That(signedResult, Is.EqualTo(signedSamples), $"Signed width {bitDepth}");
            bitDepth++;
        }
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void OddWidthRowsRoundTripWithoutChangingStridePadding(RawBitOrder bitOrder)
    {
        int width = 7;
        int height = 3;
        int bitDepth = 13;
        int packedRowBytes = BitPacking.GetPackedByteCount(width, bitDepth);
        int rowStride = packedRowBytes + 3;
        uint[] samples = new uint[width * height];
        uint[] result = new uint[samples.Length];
        byte[] rows = Enumerable.Repeat((byte)0xA5, rowStride * height).ToArray();
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = (uint)((index * 977) & 0x1FFF);
            index++;
        }

        int row = 0;
        while (row < height)
        {
            BitPacking.PackUnsigned(samples.AsSpan(row * width, width), bitDepth, bitOrder, rows.AsSpan(row * rowStride, rowStride));
            BitPacking.UnpackUnsigned(rows.AsSpan(row * rowStride, packedRowBytes), width, bitDepth, bitOrder, result.AsSpan(row * width, width));
            int paddingIndex = packedRowBytes;
            while (paddingIndex < rowStride)
            {
                Assert.That(rows[row * rowStride + paddingIndex], Is.EqualTo(0xA5), $"Row {row} padding byte {paddingIndex}");
                paddingIndex++;
            }
            row++;
        }

        Assert.That(result, Is.EqualTo(samples));
    }

    [Test]
    public void NamedRawPackingRoundTripsPartialGroupsAndOddRows()
    {
        RawPacking[] packings = new RawPacking[] { RawPacking.Raw10, RawPacking.Raw12, RawPacking.Raw14 };
        int packingIndex = 0;
        while (packingIndex < packings.Length)
        {
            RawPacking packing = packings[packingIndex];
            int bitDepth = BitPacking.GetNamedBitDepth(packing);
            uint mask = (uint)BitUtility.Mask(bitDepth);
            int width = 5;
            int height = 3;
            int packedRowBytes = BitPacking.GetNamedPackedByteCount(width, packing);
            int rowStride = packedRowBytes + 2;
            uint[] samples = new uint[width * height];
            uint[] result = new uint[samples.Length];
            byte[] rows = Enumerable.Repeat((byte)0x6D, rowStride * height).ToArray();
            int index = 0;
            while (index < samples.Length)
            {
                samples[index] = (uint)(index * 613) & mask;
                index++;
            }

            int row = 0;
            while (row < height)
            {
                BitPacking.PackNamed(samples.AsSpan(row * width, width), packing, rows.AsSpan(row * rowStride, rowStride));
                BitPacking.UnpackNamed(rows.AsSpan(row * rowStride, packedRowBytes), width, packing, result.AsSpan(row * width, width));
                Assert.That(rows[row * rowStride + packedRowBytes], Is.EqualTo(0x6D));
                Assert.That(rows[row * rowStride + packedRowBytes + 1], Is.EqualTo(0x6D));
                row++;
            }

            Assert.That(result, Is.EqualTo(samples), $"Packing {packing}");
            packingIndex++;
        }
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void EveryUnalignedSampleWidthRejectsCorruptedPadding(RawBitOrder bitOrder)
    {
        int bitDepth = 1;
        while (bitDepth <= 32)
        {
            int sampleCount = 3;
            int remainder = sampleCount * bitDepth % 8;
            if (remainder != 0)
            {
                uint mask = (uint)BitUtility.Mask(bitDepth);
                uint[] samples = new uint[] { 0, mask, mask / 3 };
                int byteCount = BitPacking.GetPackedByteCount(sampleCount, bitDepth);
                byte[] bytes = new byte[byteCount];
                uint[] destination = Enumerable.Repeat(0xDEADBEEFU, sampleCount).ToArray();
                BitPacking.PackUnsigned(samples, bitDepth, bitOrder, bytes);
                if (bitOrder == RawBitOrder.MostSignificantBitFirst)
                {
                    bytes[byteCount - 1] |= 0x01;
                }
                else
                {
                    bytes[byteCount - 1] |= 0x80;
                }

                RawFormatException? exception = Assert.Throws<RawFormatException>(() => BitPacking.UnpackUnsigned(bytes, sampleCount, bitDepth, bitOrder, destination));
                Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData), $"Width {bitDepth}");
                Assert.That(destination, Is.All.EqualTo(0xDEADBEEFU), $"Width {bitDepth}");
            }
            bitDepth++;
        }
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void EverySampleWidthRejectsTruncatedPackedInput(RawBitOrder bitOrder)
    {
        int bitDepth = 1;
        while (bitDepth <= 32)
        {
            int sampleCount = 9;
            uint mask = (uint)BitUtility.Mask(bitDepth);
            uint[] samples = Enumerable.Repeat(mask, sampleCount).ToArray();
            int byteCount = BitPacking.GetPackedByteCount(sampleCount, bitDepth);
            byte[] complete = new byte[byteCount];
            BitPacking.PackUnsigned(samples, bitDepth, bitOrder, complete);
            byte[] truncated = complete.Take(byteCount - 1).ToArray();
            uint[] destination = Enumerable.Repeat(123U, sampleCount).ToArray();

            RawFormatException? exception = Assert.Throws<RawFormatException>(() => BitPacking.UnpackUnsigned(truncated, sampleCount, bitDepth, bitOrder, destination));

            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput), $"Width {bitDepth}");
            Assert.That(destination, Is.All.EqualTo(123U), $"Width {bitDepth}");
            bitDepth++;
        }
    }

    [Test]
    public void SizeCalculationsAndBoundedWritersRejectOverflow()
    {
        RawFormatException? generic = Assert.Throws<RawFormatException>(() => BitPacking.GetPackedByteCount(int.MaxValue, 32));
        RawFormatException? named = Assert.Throws<RawFormatException>(() => BitPacking.GetNamedPackedByteCount(int.MaxValue, RawPacking.Raw14));
        RawFormatException? writer = Assert.Throws<RawFormatException>(() => MsbBitWriter.CreateStream(new MemoryStream(), long.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That((generic ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
            Assert.That((named ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
            Assert.That((writer ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
        });
    }

    static long FillBitSequence(Random random, int[] widths, uint[] values)
    {
        long bitCount = 0;
        int index = 0;
        while (index < widths.Length)
        {
            int width;
            if (index == 0)
            {
                width = 0;
            }
            else if (index == 1)
            {
                width = 1;
            }
            else if (index == 2)
            {
                width = 32;
            }
            else
            {
                width = random.Next(0, 33);
            }
            uint value = NextUInt32(random) & (uint)BitUtility.Mask(width);
            widths[index] = width;
            values[index] = value;
            bitCount += width;
            index++;
        }
        return bitCount;
    }

    static int DecodeSigned(uint encoded, int bitDepth)
    {
        if (bitDepth == 32)
        {
            return (int)encoded;
        }
        uint signBit = 1U << (bitDepth - 1);
        if ((encoded & signBit) == 0)
        {
            return (int)encoded;
        }
        return (int)((long)encoded - (1L << bitDepth));
    }

    static uint NextUInt32(Random random)
    {
        return ((uint)random.Next(0, 65536) << 16) | (uint)random.Next(0, 65536);
    }

    static ulong NextUInt64(Random random)
    {
        return ((ulong)NextUInt32(random) << 32) | NextUInt32(random);
    }

    static MsbBitReader CreateMsbReader(byte[] bytes)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, new RawLimits());
        return new MsbBitReader(input);
    }

    static LsbBitReader CreateLsbReader(byte[] bytes)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, new RawLimits());
        return new LsbBitReader(input);
    }
}
