using raw;

namespace raw.Tests;

public class BitPackingTests
{
    [Test]
    public void GenericUnsignedPackingMatchesKnownNibbleVectors()
    {
        uint[] samples = new uint[] { 0xA, 0xB, 0xC };
        byte[] msbBytes = Enumerable.Repeat((byte)0xAA, 3).ToArray();
        byte[] lsbBytes = Enumerable.Repeat((byte)0xAA, 3).ToArray();
        uint[] msbResult = new uint[3];
        uint[] lsbResult = new uint[3];

        int msbCount = BitPacking.PackUnsigned(samples, 4, RawBitOrder.MostSignificantBitFirst, msbBytes);
        int lsbCount = BitPacking.PackUnsigned(samples, 4, RawBitOrder.LeastSignificantBitFirst, lsbBytes);
        BitPacking.UnpackUnsigned(msbBytes.AsSpan(0, msbCount), samples.Length, 4, RawBitOrder.MostSignificantBitFirst, msbResult);
        BitPacking.UnpackUnsigned(lsbBytes.AsSpan(0, lsbCount), samples.Length, 4, RawBitOrder.LeastSignificantBitFirst, lsbResult);

        Assert.Multiple(() =>
        {
            Assert.That(msbCount, Is.EqualTo(2));
            Assert.That(lsbCount, Is.EqualTo(2));
            Assert.That(msbBytes, Is.EqualTo(new byte[] { 0xAB, 0xC0, 0xAA }));
            Assert.That(lsbBytes, Is.EqualTo(new byte[] { 0xBA, 0x0C, 0xAA }));
            Assert.That(msbResult, Is.EqualTo(samples));
            Assert.That(lsbResult, Is.EqualTo(samples));
        });
    }

    [Test]
    public void GenericSignedPackingUsesTwosComplementAtRequestedWidth()
    {
        int[] samples = new int[] { -8, -1, 0, 7 };
        byte[] msbBytes = new byte[2];
        byte[] lsbBytes = new byte[2];
        int[] msbResult = new int[4];
        int[] lsbResult = new int[4];

        BitPacking.PackSigned(samples, 4, RawBitOrder.MostSignificantBitFirst, msbBytes);
        BitPacking.PackSigned(samples, 4, RawBitOrder.LeastSignificantBitFirst, lsbBytes);
        BitPacking.UnpackSigned(msbBytes, samples.Length, 4, RawBitOrder.MostSignificantBitFirst, msbResult);
        BitPacking.UnpackSigned(lsbBytes, samples.Length, 4, RawBitOrder.LeastSignificantBitFirst, lsbResult);

        Assert.Multiple(() =>
        {
            Assert.That(msbBytes, Is.EqualTo(new byte[] { 0x8F, 0x07 }));
            Assert.That(lsbBytes, Is.EqualTo(new byte[] { 0xF8, 0x70 }));
            Assert.That(msbResult, Is.EqualTo(samples));
            Assert.That(lsbResult, Is.EqualTo(samples));
        });
    }

    [Test]
    public void GenericPackingSupportsOneAndThirtyTwoBitBoundaries()
    {
        uint[] oneBitSamples = new uint[] { 0, 1, 1, 0, 1 };
        byte[] oneBitMsb = new byte[1];
        byte[] oneBitLsb = new byte[1];
        uint[] values32 = new uint[] { 0x89ABCDEFU, 0x01234567U };
        byte[] bytes32Msb = new byte[8];
        byte[] bytes32Lsb = new byte[8];
        uint[] result32Msb = new uint[2];
        uint[] result32Lsb = new uint[2];

        BitPacking.PackUnsigned(oneBitSamples, 1, RawBitOrder.MostSignificantBitFirst, oneBitMsb);
        BitPacking.PackUnsigned(oneBitSamples, 1, RawBitOrder.LeastSignificantBitFirst, oneBitLsb);
        BitPacking.PackUnsigned(values32, 32, RawBitOrder.MostSignificantBitFirst, bytes32Msb);
        BitPacking.PackUnsigned(values32, 32, RawBitOrder.LeastSignificantBitFirst, bytes32Lsb);
        BitPacking.UnpackUnsigned(bytes32Msb, 2, 32, RawBitOrder.MostSignificantBitFirst, result32Msb);
        BitPacking.UnpackUnsigned(bytes32Lsb, 2, 32, RawBitOrder.LeastSignificantBitFirst, result32Lsb);

        Assert.Multiple(() =>
        {
            Assert.That(oneBitMsb[0], Is.EqualTo(0x68));
            Assert.That(oneBitLsb[0], Is.EqualTo(0x16));
            Assert.That(bytes32Msb, Is.EqualTo(new byte[] { 0x89, 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67 }));
            Assert.That(bytes32Lsb, Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
            Assert.That(result32Msb, Is.EqualTo(values32));
            Assert.That(result32Lsb, Is.EqualTo(values32));
        });
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void SignedThirtyTwoBitExtremesRoundTrip(RawBitOrder bitOrder)
    {
        int[] samples = new int[] { int.MinValue, -1, 0, int.MaxValue };
        byte[] bytes = new byte[16];
        int[] result = new int[4];

        BitPacking.PackSigned(samples, 32, bitOrder, bytes);
        BitPacking.UnpackSigned(bytes, samples.Length, 32, bitOrder, result);

        Assert.That(result, Is.EqualTo(samples));
    }

    [Test]
    public void GenericPackingPreflightsRangesAndDestinations()
    {
        byte[] invalidValueBytes = new byte[] { 0xAA };
        byte[] shortBytes = new byte[] { 0xAA };

        RawFormatException? invalidUnsigned = Assert.Throws<RawFormatException>(() => BitPacking.PackUnsigned(new uint[] { 16 }, 4, RawBitOrder.MostSignificantBitFirst, invalidValueBytes));
        RawFormatException? invalidSigned = Assert.Throws<RawFormatException>(() => BitPacking.PackSigned(new int[] { -9 }, 4, RawBitOrder.MostSignificantBitFirst, invalidValueBytes));
        RawFormatException? shortDestination = Assert.Throws<RawFormatException>(() => BitPacking.PackUnsigned(new uint[] { 1, 2, 3 }, 4, RawBitOrder.MostSignificantBitFirst, shortBytes));

        Assert.Multiple(() =>
        {
            Assert.That((invalidUnsigned ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((invalidSigned ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((shortDestination ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That(invalidValueBytes[0], Is.EqualTo(0xAA));
            Assert.That(shortBytes[0], Is.EqualTo(0xAA));
        });
    }

    [Test]
    public void GenericUnpackingRejectsTruncationAndNonzeroPaddingBeforeOutput()
    {
        uint[] truncatedOutput = new uint[] { 99, 99, 99 };
        uint[] msbOutput = new uint[] { 99, 99, 99 };
        uint[] lsbOutput = new uint[] { 99, 99, 99 };

        RawFormatException? truncated = Assert.Throws<RawFormatException>(() => BitPacking.UnpackUnsigned(new byte[1], 3, 4, RawBitOrder.MostSignificantBitFirst, truncatedOutput));
        RawFormatException? msbPadding = Assert.Throws<RawFormatException>(() => BitPacking.UnpackUnsigned(new byte[] { 0xAB, 0xCF }, 3, 4, RawBitOrder.MostSignificantBitFirst, msbOutput));
        RawFormatException? lsbPadding = Assert.Throws<RawFormatException>(() => BitPacking.UnpackUnsigned(new byte[] { 0xBA, 0xFC }, 3, 4, RawBitOrder.LeastSignificantBitFirst, lsbOutput));

        Assert.Multiple(() =>
        {
            Assert.That((truncated ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((msbPadding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((lsbPadding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(truncatedOutput, Is.All.EqualTo(99));
            Assert.That(msbOutput, Is.All.EqualTo(99));
            Assert.That(lsbOutput, Is.All.EqualTo(99));
        });
    }

    [Test]
    public void Raw10MatchesStandardFourPixelGroupLayout()
    {
        uint[] samples = new uint[] { 0x3FF, 0x000, 0x155, 0x2AA };
        byte[] bytes = new byte[5];
        uint[] result = new uint[4];

        int written = BitPacking.PackRaw10(samples, bytes);
        int consumed = BitPacking.UnpackRaw10(bytes, samples.Length, result);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(5));
            Assert.That(consumed, Is.EqualTo(5));
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xFF, 0x00, 0x55, 0xAA, 0x93 }));
            Assert.That(result, Is.EqualTo(samples));
        });
    }

    [Test]
    public void Raw12MatchesStandardTwoPixelGroupLayout()
    {
        uint[] samples = new uint[] { 0xABC, 0x123 };
        byte[] bytes = new byte[3];
        uint[] result = new uint[2];

        BitPacking.PackRaw12(samples, bytes);
        BitPacking.UnpackRaw12(bytes, samples.Length, result);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xAB, 0x12, 0x3C }));
            Assert.That(result, Is.EqualTo(samples));
        });
    }

    [Test]
    public void Raw14MatchesStandardFourPixelGroupLayout()
    {
        uint[] samples = new uint[] { 0x3FFF, 0x0000, 0x1555, 0x2AAA };
        byte[] bytes = new byte[7];
        uint[] result = new uint[4];

        BitPacking.PackRaw14(samples, bytes);
        BitPacking.UnpackRaw14(bytes, samples.Length, result);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xFF, 0x00, 0x55, 0xAA, 0x3F, 0x50, 0xA9 }));
            Assert.That(result, Is.EqualTo(samples));
        });
    }

    [Test]
    public void NamedLayoutsSupportPartialFinalGroups()
    {
        uint[] raw10Samples = new uint[] { 0x3FF };
        uint[] raw12Samples = new uint[] { 0xABC };
        uint[] raw14Samples = new uint[] { 0x3FFF, 0x0000, 0x1555 };
        byte[] raw10Bytes = new byte[2];
        byte[] raw12Bytes = new byte[2];
        byte[] raw14Bytes = new byte[6];
        uint[] raw10Result = new uint[1];
        uint[] raw12Result = new uint[1];
        uint[] raw14Result = new uint[3];

        BitPacking.PackRaw10(raw10Samples, raw10Bytes);
        BitPacking.PackRaw12(raw12Samples, raw12Bytes);
        BitPacking.PackRaw14(raw14Samples, raw14Bytes);
        BitPacking.UnpackRaw10(raw10Bytes, 1, raw10Result);
        BitPacking.UnpackRaw12(raw12Bytes, 1, raw12Result);
        BitPacking.UnpackRaw14(raw14Bytes, 3, raw14Result);

        Assert.Multiple(() =>
        {
            Assert.That(raw10Bytes, Is.EqualTo(new byte[] { 0xFF, 0x03 }));
            Assert.That(raw12Bytes, Is.EqualTo(new byte[] { 0xAB, 0x0C }));
            Assert.That(raw14Bytes, Is.EqualTo(new byte[] { 0xFF, 0x00, 0x55, 0x3F, 0x50, 0x01 }));
            Assert.That(raw10Result, Is.EqualTo(raw10Samples));
            Assert.That(raw12Result, Is.EqualTo(raw12Samples));
            Assert.That(raw14Result, Is.EqualTo(raw14Samples));
        });
    }

    [Test]
    public void NamedLayoutsRejectInvalidPaddingRangesAndTruncation()
    {
        uint[] destination = new uint[] { 99 };
        byte[] unchanged = new byte[] { 0xAA, 0xAA };

        RawFormatException? padding = Assert.Throws<RawFormatException>(() => BitPacking.UnpackRaw10(new byte[] { 0xFF, 0xFC }, 1, destination));
        RawFormatException? range = Assert.Throws<RawFormatException>(() => BitPacking.PackRaw10(new uint[] { 0x400 }, unchanged));
        RawFormatException? truncated = Assert.Throws<RawFormatException>(() => BitPacking.UnpackRaw12(new byte[] { 0xAB }, 1, destination));

        Assert.Multiple(() =>
        {
            Assert.That((padding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((range ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((truncated ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(destination[0], Is.EqualTo(99));
            Assert.That(unchanged, Is.EqualTo(new byte[] { 0xAA, 0xAA }));
        });
    }

    [Test]
    public void PackedByteCountsHandleEmptyOddAndCompleteGroups()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BitPacking.GetPackedByteCount(0, 17), Is.EqualTo(0));
            Assert.That(BitPacking.GetPackedByteCount(3, 17), Is.EqualTo(7));
            Assert.That(BitPacking.GetNamedPackedByteCount(0, RawPacking.Raw10), Is.EqualTo(0));
            Assert.That(BitPacking.GetNamedPackedByteCount(1, RawPacking.Raw10), Is.EqualTo(2));
            Assert.That(BitPacking.GetNamedPackedByteCount(4, RawPacking.Raw10), Is.EqualTo(5));
            Assert.That(BitPacking.GetNamedPackedByteCount(5, RawPacking.Raw10), Is.EqualTo(7));
            Assert.That(BitPacking.GetNamedPackedByteCount(3, RawPacking.Raw12), Is.EqualTo(5));
            Assert.That(BitPacking.GetNamedPackedByteCount(5, RawPacking.Raw14), Is.EqualTo(9));
        });
    }
}
