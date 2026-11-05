using System;
using rtp;

namespace rtp.Tests.Wire;

public class BinaryPrimitiveTests
{
    [Test]
    public void ReaderReadsEveryWidthInNetworkByteOrderAtAnOffset()
    {
        byte[] source = new byte[]
        {
            255, 1, 35, 69, 103, 137, 171, 205, 239, 16
        };

        Assert.Multiple(() =>
        {
            Assert.That(NetworkBinaryReader.ReadByte(source, 1), Is.EqualTo(1));
            Assert.That(NetworkBinaryReader.ReadUInt16(source, 1), Is.EqualTo(0x0123));
            Assert.That(NetworkBinaryReader.ReadUInt24(source, 1), Is.EqualTo(0x012345U));
            Assert.That(NetworkBinaryReader.ReadUInt32(source, 1), Is.EqualTo(0x01234567U));
            Assert.That(NetworkBinaryReader.ReadUInt64(source, 1), Is.EqualTo(0x0123456789ABCDEFUL));
        });
    }

    [Test]
    public void WriterWritesEveryWidthInNetworkByteOrderAtAnOffset()
    {
        byte[] destination = new byte[26];

        NetworkBinaryWriter.WriteByte(destination, 1, 0x7F);
        NetworkBinaryWriter.WriteUInt16(destination, 3, 0x0123);
        NetworkBinaryWriter.WriteUInt24(destination, 6, 0x456789U);
        NetworkBinaryWriter.WriteUInt32(destination, 10, 0x89ABCDEFU);
        NetworkBinaryWriter.WriteUInt64(destination, 15, 0x0123456789ABCDEFUL);

        Assert.That(destination, Is.EqualTo(new byte[]
        {
            0, 127, 0, 1, 35, 0, 69, 103, 137, 0,
            137, 171, 205, 239, 0, 1, 35, 69, 103, 137,
            171, 205, 239, 0, 0, 0
        }));
    }

    [Test]
    public void UnsignedWidthsRoundTripZeroAndMaximumValues()
    {
        byte[] zeroDestination = new byte[15];
        byte[] maximumDestination = new byte[15];

        NetworkBinaryWriter.WriteByte(zeroDestination, 0, 0);
        NetworkBinaryWriter.WriteUInt16(zeroDestination, 1, 0);
        NetworkBinaryWriter.WriteUInt32(zeroDestination, 3, 0U);
        NetworkBinaryWriter.WriteUInt64(zeroDestination, 7, 0UL);
        NetworkBinaryWriter.WriteByte(maximumDestination, 0, byte.MaxValue);
        NetworkBinaryWriter.WriteUInt16(
            maximumDestination,
            1,
            ushort.MaxValue);
        NetworkBinaryWriter.WriteUInt32(
            maximumDestination,
            3,
            uint.MaxValue);
        NetworkBinaryWriter.WriteUInt64(
            maximumDestination,
            7,
            ulong.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(zeroDestination, Is.All.Zero);
            Assert.That(NetworkBinaryReader.ReadByte(zeroDestination, 0),
                Is.Zero);
            Assert.That(NetworkBinaryReader.ReadUInt16(zeroDestination, 1),
                Is.Zero);
            Assert.That(NetworkBinaryReader.ReadUInt32(zeroDestination, 3),
                Is.Zero);
            Assert.That(NetworkBinaryReader.ReadUInt64(zeroDestination, 7),
                Is.Zero);
            Assert.That(maximumDestination,
                Is.All.EqualTo(byte.MaxValue));
            Assert.That(NetworkBinaryReader.ReadByte(maximumDestination, 0),
                Is.EqualTo(byte.MaxValue));
            Assert.That(NetworkBinaryReader.ReadUInt16(maximumDestination, 1),
                Is.EqualTo(ushort.MaxValue));
            Assert.That(NetworkBinaryReader.ReadUInt32(maximumDestination, 3),
                Is.EqualTo(uint.MaxValue));
            Assert.That(NetworkBinaryReader.ReadUInt64(maximumDestination, 7),
                Is.EqualTo(ulong.MaxValue));
        });
    }

    [Test]
    public void ReaderSlicesAndWriterCopiesOnlyTheRequestedBytes()
    {
        byte[] source = new byte[] { 1, 2, 3, 4, 5 };
        byte[] destination = new byte[] { 9, 9, 9, 9, 9, 9 };

        ReadOnlySpan<byte> slice = NetworkBinaryReader.ReadSlice(source, 1, 3);
        NetworkBinaryWriter.WriteBytes(destination, 2, slice);
        byte[] slicedBytes = slice.ToArray();
        int emptySliceLength = NetworkBinaryReader.ReadSlice(source, source.Length, 0).Length;

        Assert.Multiple(() =>
        {
            Assert.That(slicedBytes, Is.EqualTo(new byte[] { 2, 3, 4 }));
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 9, 2, 3, 4, 9 }));
            Assert.That(emptySliceLength, Is.Zero);
        });
    }

    [TestCase(-8388608, new byte[] { 128, 0, 0 })]
    [TestCase(-8388607, new byte[] { 128, 0, 1 })]
    [TestCase(-2, new byte[] { 255, 255, 254 })]
    [TestCase(-1, new byte[] { 255, 255, 255 })]
    [TestCase(0, new byte[] { 0, 0, 0 })]
    [TestCase(1, new byte[] { 0, 0, 1 })]
    [TestCase(8388606, new byte[] { 127, 255, 254 })]
    [TestCase(8388607, new byte[] { 127, 255, 255 })]
    public void SignedTwentyFourBitValuesRoundTrip(int value, byte[] encoded)
    {
        byte[] destination = new byte[3];

        NetworkBinaryWriter.WriteInt24(destination, 0, value);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(encoded));
            Assert.That(NetworkBinaryReader.ReadInt24(destination, 0), Is.EqualTo(value));
        });
    }

    [TestCase(0U, new byte[] { 0, 0, 0 })]
    [TestCase(1U, new byte[] { 0, 0, 1 })]
    [TestCase(8388607U, new byte[] { 127, 255, 255 })]
    [TestCase(8388608U, new byte[] { 128, 0, 0 })]
    [TestCase(16777214U, new byte[] { 255, 255, 254 })]
    [TestCase(16777215U, new byte[] { 255, 255, 255 })]
    public void UnsignedTwentyFourBitValuesRoundTrip(
        uint value,
        byte[] encoded)
    {
        byte[] destination = new byte[3];

        NetworkBinaryWriter.WriteUInt24(destination, 0, value);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(encoded));
            Assert.That(NetworkBinaryReader.ReadUInt24(destination, 0),
                Is.EqualTo(value));
        });
    }

    [Test]
    public void TwentyFourBitWritersRejectOutOfRangeValuesWithoutWriting()
    {
        byte[] unsignedDestination = new byte[] { 1, 2, 3 };
        byte[] signedLowDestination = new byte[] { 4, 5, 6 };
        byte[] signedHighDestination = new byte[] { 7, 8, 9 };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkBinaryWriter.WriteUInt24(unsignedDestination, 0, 16777216U));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkBinaryWriter.WriteInt24(signedLowDestination, 0, -8388609));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkBinaryWriter.WriteInt24(signedHighDestination, 0, 8388608));
        });

        Assert.Multiple(() =>
        {
            Assert.That(unsignedDestination, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(signedLowDestination, Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(signedHighDestination, Is.EqualTo(new byte[] { 7, 8, 9 }));
        });
    }

    [Test]
    public void ReaderRejectsNegativeOffsetsAndTruncatedInput()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkBinaryReader.ReadByte(new byte[1], -1));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadByte(Array.Empty<byte>(), 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadUInt16(new byte[2], 1));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadUInt24(new byte[2], 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadInt24(new byte[2], 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadUInt32(new byte[3], 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadUInt64(new byte[7], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkBinaryReader.ReadSlice(new byte[1], 0, -1));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadSlice(new byte[3], 2, 2));
        });
    }

    [Test]
    public void ReaderAcceptsEveryWidthAtTheExactEndBoundary()
    {
        byte[] source = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 255
        };

        Assert.Multiple(() =>
        {
            Assert.That(NetworkBinaryReader.ReadByte(source, 7),
                Is.EqualTo(255));
            Assert.That(NetworkBinaryReader.ReadUInt16(source, 6),
                Is.EqualTo(0x07FF));
            Assert.That(NetworkBinaryReader.ReadUInt24(source, 5),
                Is.EqualTo(0x0607FFU));
            Assert.That(NetworkBinaryReader.ReadInt24(source, 5),
                Is.EqualTo(0x0607FF));
            Assert.That(NetworkBinaryReader.ReadUInt32(source, 4),
                Is.EqualTo(0x050607FFU));
            Assert.That(NetworkBinaryReader.ReadUInt64(source, 0),
                Is.EqualTo(0x01020304050607FFUL));
            Assert.That(NetworkBinaryReader.ReadSlice(source, 5, 3)
                .ToArray(), Is.EqualTo(new byte[] { 6, 7, 255 }));
            Assert.That(NetworkBinaryReader.ReadSlice(source, 8, 0)
                .Length, Is.Zero);
        });
    }

    [Test]
    public void ReaderRejectsEveryTruncationPointForEveryWidth()
    {
        AssertEveryTruncatedReadThrows(
            1,
            source => NetworkBinaryReader.ReadByte(source, 0));
        AssertEveryTruncatedReadThrows(
            2,
            source => NetworkBinaryReader.ReadUInt16(source, 0));
        AssertEveryTruncatedReadThrows(
            3,
            source => NetworkBinaryReader.ReadUInt24(source, 0));
        AssertEveryTruncatedReadThrows(
            3,
            source => NetworkBinaryReader.ReadInt24(source, 0));
        AssertEveryTruncatedReadThrows(
            4,
            source => NetworkBinaryReader.ReadUInt32(source, 0));
        AssertEveryTruncatedReadThrows(
            8,
            source => NetworkBinaryReader.ReadUInt64(source, 0));
        AssertEveryTruncatedReadThrows(
            8,
            source => NetworkBinaryReader.ReadSlice(source, 0, 8));
    }

    [Test]
    public void ReaderRejectsOffsetsBeyondTheSpanWithoutIntegerOverflow()
    {
        byte[] source = new byte[] { 1, 2, 3 };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadByte(source, source.Length));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadUInt64(source, int.MaxValue));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadSlice(source, int.MaxValue, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryReader.ReadSlice(source, 1, int.MaxValue));
        });
    }

    [Test]
    public void WriterRejectsNegativeOffsetsAndInsufficientCapacityWithoutWriting()
    {
        byte[] destination = new byte[] { 1, 2, 3, 4, 5, 6, 7 };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkBinaryWriter.WriteByte(destination, -1, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteByte(Array.Empty<byte>(), 0, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteUInt16(destination, 6, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteUInt24(destination, 5, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteInt24(destination, 5, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteUInt32(destination, 4, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteUInt64(destination, 0, 0));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteBytes(destination, 6, new byte[] { 8, 9 }));
        });

        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7 }));
    }

    [Test]
    public void WriterAcceptsEveryWidthAtTheExactCapacityBoundary()
    {
        byte[] destination = new byte[18];

        NetworkBinaryWriter.WriteByte(destination, 17, 0x11);
        NetworkBinaryWriter.WriteUInt16(destination, 15, 0x2233);
        NetworkBinaryWriter.WriteUInt24(destination, 12, 0x445566U);
        NetworkBinaryWriter.WriteInt24(destination, 9, -1);
        NetworkBinaryWriter.WriteUInt32(destination, 5, 0x778899AAU);
        NetworkBinaryWriter.WriteUInt64(
            destination,
            0,
            0x0102030405060708UL);

        Assert.That(destination, Is.EqualTo(new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            170, 255, 255, 255, 68, 85, 102, 34, 51, 17
        }));
    }

    [Test]
    public void WriterRejectsEveryCapacityShortfallWithoutPartialWrites()
    {
        AssertEveryTruncatedWriteThrows(
            1,
            destination => NetworkBinaryWriter.WriteByte(
                destination,
                0,
                1));
        AssertEveryTruncatedWriteThrows(
            2,
            destination => NetworkBinaryWriter.WriteUInt16(
                destination,
                0,
                0x1234));
        AssertEveryTruncatedWriteThrows(
            3,
            destination => NetworkBinaryWriter.WriteUInt24(
                destination,
                0,
                0x123456U));
        AssertEveryTruncatedWriteThrows(
            3,
            destination => NetworkBinaryWriter.WriteInt24(
                destination,
                0,
                -2));
        AssertEveryTruncatedWriteThrows(
            4,
            destination => NetworkBinaryWriter.WriteUInt32(
                destination,
                0,
                0x12345678U));
        AssertEveryTruncatedWriteThrows(
            8,
            destination => NetworkBinaryWriter.WriteUInt64(
                destination,
                0,
                0x0123456789ABCDEFUL));
        AssertEveryTruncatedWriteThrows(
            8,
            destination => NetworkBinaryWriter.WriteBytes(
                destination,
                0,
                new byte[8]));
    }

    [Test]
    public void WriterHandlesEmptyAndOverlappingCopiesAtBoundaries()
    {
        byte[] destination = new byte[] { 1, 2, 3, 4, 5, 6 };

        NetworkBinaryWriter.WriteBytes(
            destination,
            destination.Length,
            Array.Empty<byte>());
        NetworkBinaryWriter.WriteBytes(
            destination,
            2,
            destination.AsSpan(0, 4));

        Assert.That(destination,
            Is.EqualTo(new byte[] { 1, 2, 1, 2, 3, 4 }));
    }

    [Test]
    public void WriterRejectsExtremeOffsetsWithoutPartialWrites()
    {
        byte[] destination = new byte[] { 1, 2, 3 };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteByte(
                    destination,
                    destination.Length,
                    4));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteUInt64(
                    destination,
                    int.MaxValue,
                    1UL));
            Assert.Throws<ArgumentException>(() =>
                NetworkBinaryWriter.WriteBytes(
                    destination,
                    int.MaxValue,
                    Array.Empty<byte>()));
        });
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 3, 4)]
    [TestCase(2, 2, 4)]
    [TestCase(3, 1, 4)]
    [TestCase(4, 0, 4)]
    [TestCase(5, 3, 8)]
    [TestCase(2147483644, 0, 2147483644)]
    public void AlignmentReturnsPaddingAndAlignedSize(int size, int padding, int aligned)
    {
        Assert.Multiple(() =>
        {
            Assert.That(SizeArithmetic.GetFourBytePadding(size), Is.EqualTo(padding));
            Assert.That(SizeArithmetic.AlignToFour(size), Is.EqualTo(aligned));
        });
    }

    [Test]
    public void AlignmentCoversEveryResidueAcrossRepresentativeSizes()
    {
        int size = 0;
        while (size <= 1024)
        {
            int padding = SizeArithmetic.GetFourBytePadding(size);
            int aligned = SizeArithmetic.AlignToFour(size);
            Assert.Multiple(() =>
            {
                Assert.That(padding, Is.InRange(0, 3));
                Assert.That(aligned, Is.EqualTo(size + padding));
                Assert.That(aligned & 3, Is.Zero);
                Assert.That(aligned, Is.GreaterThanOrEqualTo(size));
            });
            size = size + 1;
        }
    }

    [Test]
    public void SizeArithmeticAcceptsBoundaryResults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SizeArithmetic.Add(0, 0), Is.Zero);
            Assert.That(SizeArithmetic.Add(int.MaxValue, 0),
                Is.EqualTo(int.MaxValue));
            Assert.That(SizeArithmetic.Add(0, int.MaxValue),
                Is.EqualTo(int.MaxValue));
            Assert.That(SizeArithmetic.Add(int.MaxValue - 1, 1), Is.EqualTo(int.MaxValue));
            Assert.That(SizeArithmetic.Multiply(0, int.MaxValue), Is.Zero);
            Assert.That(SizeArithmetic.Multiply(int.MaxValue, 0), Is.Zero);
            Assert.That(SizeArithmetic.Multiply(1, int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.That(SizeArithmetic.Multiply(int.MaxValue, 1),
                Is.EqualTo(int.MaxValue));
            Assert.That(SizeArithmetic.Multiply(46340, 46340), Is.EqualTo(2147395600));
            Assert.That(SizeArithmetic.Multiply(715827882, 3),
                Is.EqualTo(2147483646));
        });
    }

    [Test]
    public void SizeArithmeticRejectsNegativeInputsAndOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeArithmetic.Add(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeArithmetic.Add(0, -1));
            Assert.Throws<OverflowException>(() => SizeArithmetic.Add(int.MaxValue, 1));
            Assert.Throws<OverflowException>(() => SizeArithmetic.Add(1, int.MaxValue));
            Assert.Throws<OverflowException>(() =>
                SizeArithmetic.Add(int.MaxValue - 10, 11));
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeArithmetic.Multiply(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeArithmetic.Multiply(0, -1));
            Assert.Throws<OverflowException>(() => SizeArithmetic.Multiply(46341, 46341));
            Assert.Throws<OverflowException>(() =>
                SizeArithmetic.Multiply(715827883, 3));
            Assert.Throws<OverflowException>(() =>
                SizeArithmetic.Multiply(3, 715827883));
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeArithmetic.GetFourBytePadding(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeArithmetic.AlignToFour(-1));
            Assert.Throws<OverflowException>(() => SizeArithmetic.AlignToFour(int.MaxValue));
            Assert.Throws<OverflowException>(() =>
                SizeArithmetic.AlignToFour(int.MaxValue - 1));
            Assert.Throws<OverflowException>(() =>
                SizeArithmetic.AlignToFour(int.MaxValue - 2));
        });
    }

    static void AssertEveryTruncatedReadThrows(
        int requiredLength,
        Action<byte[]> read)
    {
        int availableLength = 0;
        while (availableLength < requiredLength)
        {
            byte[] source = new byte[availableLength];
            Assert.Throws<ArgumentException>(() => read(source));
            availableLength = availableLength + 1;
        }
    }

    static void AssertEveryTruncatedWriteThrows(
        int requiredLength,
        Action<byte[]> write)
    {
        int availableLength = 0;
        while (availableLength < requiredLength)
        {
            byte[] destination = new byte[availableLength];
            int byteIndex = 0;
            while (byteIndex < destination.Length)
            {
                destination[byteIndex] = 165;
                byteIndex = byteIndex + 1;
            }

            byte[] original = (byte[])destination.Clone();
            Assert.Throws<ArgumentException>(() => write(destination));
            Assert.That(destination, Is.EqualTo(original));
            availableLength = availableLength + 1;
        }
    }
}
