using asn1;

namespace asn1.Tests;

public class PerWholeNumberCodecTests
{
    [Test]
    public void NonNegativeBinaryUsesExactMsbFirstFieldWidths()
    {
        PerWholeNumberCodec codec = CreateCodec();
        PerWholeNumberResult encoded = codec.EncodeNonNegativeBinary(21, 5);
        PerWholeNumberResult decoded = codec.DecodeNonNegativeBinary(
            encoded.Data,
            encoded.BitCount);
        PerWholeNumberResult tooNarrow =
            codec.EncodeNonNegativeBinary(8, 3);
        PerWholeNumberResult negative =
            codec.EncodeNonNegativeBinary(-1, 8);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0xa8 }));
            Assert.That(encoded.BitCount, Is.EqualTo(5));
            Assert.That(decoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(21)));
            Assert.That(tooNarrow.ErrorCode, Is.EqualTo("ASNP0011"));
            Assert.That(negative.ErrorCode, Is.EqualTo("ASNP0010"));
        });
    }

    [Test]
    public void TwosComplementUsesTheMinimumPortableOctetForm()
    {
        PerWholeNumberCodec codec = CreateCodec();
        System.Numerics.BigInteger[] values =
        [
            new System.Numerics.BigInteger(-129),
            new System.Numerics.BigInteger(-128),
            new System.Numerics.BigInteger(-1),
            System.Numerics.BigInteger.Zero,
            new System.Numerics.BigInteger(127),
            new System.Numerics.BigInteger(128)
        ];
        byte[][] expected =
        [
            [0xff, 0x7f],
            [0x80],
            [0xff],
            [0x00],
            [0x7f],
            [0x00, 0x80]
        ];

        int index = 0;
        while (index < values.Length)
        {
            PerWholeNumberResult encoded =
                codec.EncodeTwosComplementInteger(values[index]);
            PerWholeNumberResult decoded =
                codec.DecodeTwosComplementInteger(encoded.Data);
            Assert.That(encoded.Data, Is.EqualTo(expected[index]));
            Assert.That(decoded.Value, Is.EqualTo(values[index]));
            index = index + 1;
        }

        Assert.That(codec.DecodeTwosComplementInteger([0x00, 0x7f]).ErrorCode,
            Is.EqualTo("ASNP0016"));
        Assert.That(codec.DecodeTwosComplementInteger([0xff, 0x80]).ErrorCode,
            Is.EqualTo("ASNP0016"));
    }

    [Test]
    public void UnalignedConstrainedNumbersUseTheMinimumRangeWidth()
    {
        PerWholeNumberCodec codec = CreateCodec();
        PerWholeNumberResult singleton =
            codec.EncodeConstrainedWholeNumber(7, 7, 7, false);
        PerWholeNumberResult offset =
            codec.EncodeConstrainedWholeNumber(-8, -10, -8, false);
        PerWholeNumberResult decoded = codec.DecodeConstrainedWholeNumber(
            offset.Data,
            offset.BitCount,
            -10,
            -8,
            false);
        PerWholeNumberResult unused = codec.DecodeConstrainedWholeNumber(
            [0xc0],
            2,
            -10,
            -8,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(singleton.BitCount, Is.Zero);
            Assert.That(singleton.Data, Is.Empty);
            Assert.That(offset.BitCount, Is.EqualTo(2));
            Assert.That(offset.Data, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(decoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(-8)));
            Assert.That(unused.ErrorCode, Is.EqualTo("ASNP0021"));
        });
    }

    [Test]
    public void AlignedConstrainedNumbersHonorEveryRangeThreshold()
    {
        PerWholeNumberCodec codec = CreateCodec();
        PerWholeNumberResult range255 =
            codec.EncodeConstrainedWholeNumber(254, 0, 254, true);
        PerWholeNumberResult range256 =
            codec.EncodeConstrainedWholeNumber(255, 0, 255, true);
        PerWholeNumberResult range257 =
            codec.EncodeConstrainedWholeNumber(256, 0, 256, true);
        PerWholeNumberResult range64K =
            codec.EncodeConstrainedWholeNumber(65535, 0, 65535, true);
        PerWholeNumberResult indefinite =
            codec.EncodeConstrainedWholeNumber(65536, 0, 65536, true);

        Assert.Multiple(() =>
        {
            Assert.That(range255.BitCount, Is.EqualTo(8));
            Assert.That(range255.OctetAligned, Is.False);
            Assert.That(range256.Data, Is.EqualTo(new byte[] { 0xff }));
            Assert.That(range256.OctetAligned, Is.True);
            Assert.That(range257.Data,
                Is.EqualTo(new byte[] { 0x01, 0x00 }));
            Assert.That(range257.OctetAligned, Is.True);
            Assert.That(range64K.Data,
                Is.EqualTo(new byte[] { 0xff, 0xff }));
            Assert.That(indefinite.Data,
                Is.EqualTo(new byte[] { 0x01, 0x00, 0x00 }));
            Assert.That(indefinite.RequiresLengthDeterminant, Is.True);
            Assert.That(indefinite.OctetAligned, Is.True);
        });
    }

    [Test]
    public void SemiConstrainedAndUnconstrainedFormsRoundTrip()
    {
        PerWholeNumberCodec codec = CreateCodec();
        PerWholeNumberResult semi = codec.EncodeSemiConstrainedWholeNumber(
            1000,
            -24,
            false);
        PerWholeNumberResult semiDecoded =
            codec.DecodeSemiConstrainedWholeNumber(semi.Data, -24, false);
        PerWholeNumberResult negative =
            codec.EncodeUnconstrainedWholeNumber(-129, true);
        PerWholeNumberResult negativeDecoded =
            codec.DecodeUnconstrainedWholeNumber(negative.Data, true);

        Assert.Multiple(() =>
        {
            Assert.That(semi.Data,
                Is.EqualTo(new byte[] { 0x04, 0x00 }));
            Assert.That(semi.RequiresLengthDeterminant, Is.True);
            Assert.That(semi.OctetAligned, Is.False);
            Assert.That(semiDecoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(1000)));
            Assert.That(negative.Data,
                Is.EqualTo(new byte[] { 0xff, 0x7f }));
            Assert.That(negativeDecoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(-129)));
            Assert.That(negativeDecoded.OctetAligned, Is.True);
        });
    }

    [Test]
    public void NormallySmallNumbersEncodeSmallAndLargeForms()
    {
        PerWholeNumberCodec codec = CreateCodec();
        PerBitWriter smallWriter = CreateWriter();
        PerWholeNumberResult small = codec.WriteNormallySmallWholeNumber(
            smallWriter,
            63,
            false);
        PerBitWriteResult smallOutput = smallWriter.GetOutput();

        PerBitWriter unalignedWriter = CreateWriter();
        PerWholeNumberResult unaligned =
            codec.WriteNormallySmallWholeNumber(
                unalignedWriter,
                64,
                false);
        PerBitWriteResult unalignedOutput = unalignedWriter.GetOutput();

        PerBitWriter alignedWriter = CreateWriter();
        PerWholeNumberResult aligned = codec.WriteNormallySmallWholeNumber(
            alignedWriter,
            64,
            true);
        PerBitWriteResult alignedOutput = alignedWriter.GetOutput();

        PerBitWriter offsetWriter = CreateWriter();
        offsetWriter.WriteBits(5, 3);
        PerWholeNumberResult offsetAligned =
            codec.WriteNormallySmallWholeNumber(offsetWriter, 64, true);
        PerBitWriteResult offsetOutput = offsetWriter.GetOutput();

        Assert.Multiple(() =>
        {
            Assert.That(small.BitCount, Is.EqualTo(7));
            Assert.That(smallOutput.Data, Is.EqualTo(new byte[] { 0x7e }));
            Assert.That(unaligned.BitCount, Is.EqualTo(17));
            Assert.That(unalignedOutput.Data,
                Is.EqualTo(new byte[] { 0x80, 0xa0, 0x00 }));
            Assert.That(aligned.BitCount, Is.EqualTo(24));
            Assert.That(alignedOutput.Data,
                Is.EqualTo(new byte[] { 0x80, 0x01, 0x40 }));
            Assert.That(offsetAligned.BitCount, Is.EqualTo(21));
            Assert.That(offsetOutput.Data,
                Is.EqualTo(new byte[] { 0xb0, 0x01, 0x40 }));
        });
    }

    [Test]
    public void NormallySmallReaderResumesWithoutConsumingPartialInput()
    {
        PerWholeNumberCodec codec = CreateCodec();
        PerBitReader reader = CreateReader();
        reader.AppendInputBits([0x80], 0, 8, false);
        PerWholeNumberResult waiting =
            codec.ReadNormallySmallWholeNumber(reader, true);
        reader.AppendInputBits([0x01, 0x40], 0, 16, true);
        PerWholeNumberResult decoded =
            codec.ReadNormallySmallWholeNumber(reader, true);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(64)));
            Assert.That(decoded.BitCount, Is.EqualTo(24));
            Assert.That(reader.GetBitOffset(), Is.EqualTo(24));
        });
    }

    [Test]
    public void BigIntegerRangesAndResourceFailuresRemainBounded()
    {
        System.Numerics.BigInteger large =
            System.Numerics.BigInteger.One << 200;
        PerWholeNumberCodec codec = CreateCodec();
        PerWholeNumberResult aligned = codec.EncodeConstrainedWholeNumber(
            large,
            System.Numerics.BigInteger.Zero,
            large,
            true);
        PerWholeNumberResult alignedDecoded =
            codec.DecodeConstrainedWholeNumber(
                aligned.Data,
                aligned.BitCount,
                System.Numerics.BigInteger.Zero,
                large,
                true);
        PerWholeNumberResult unaligned = codec.EncodeConstrainedWholeNumber(
            large,
            System.Numerics.BigInteger.Zero,
            large,
            false);

        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumIntegerBits = 8,
            MaximumOpenTypeBytes = 8,
            MaximumTotalAllocationBytes = 32
        };
        PerWholeNumberCodec limited = new PerWholeNumberCodec(limits);
        PerWholeNumberResult rejected =
            limited.EncodeUnconstrainedWholeNumber(256, false);
        PerWholeNumberResult redundant =
            limited.DecodeMinimumNonNegative([0x00, 0x01]);

        Assert.Multiple(() =>
        {
            Assert.That(aligned.Data.Length, Is.EqualTo(26));
            Assert.That(aligned.Data[0], Is.EqualTo(1));
            Assert.That(aligned.RequiresLengthDeterminant, Is.True);
            Assert.That(alignedDecoded.Value, Is.EqualTo(large));
            Assert.That(unaligned.BitCount, Is.EqualTo(201));
            Assert.That(unaligned.Data[0], Is.EqualTo(0x80));
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNR0010"));
            Assert.That(rejected.LimitExceeded, Is.True);
            Assert.That(redundant.ErrorCode, Is.EqualTo("ASNP0014"));
        });
    }

    [Test]
    public void NormallySmallLargeValuesCrossFragmentBoundariesExactly()
    {
        System.Numerics.BigInteger value =
            System.Numerics.BigInteger.One << 131072;
        PerWholeNumberCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerWholeNumberResult encoded = codec.WriteNormallySmallWholeNumber(
            writer,
            value,
            true);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = CreateReader();
        reader.AppendInputBits(
            output.Data,
            0,
            output.WrittenBitCount,
            true);
        PerWholeNumberResult decoded =
            codec.ReadNormallySmallWholeNumber(reader, true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.OctetCount, Is.EqualTo(16385));
            Assert.That(output.Data[0], Is.EqualTo(0x80));
            Assert.That(output.Data[1], Is.EqualTo(0xc1));
            Assert.That(output.Data[16386], Is.EqualTo(0x01));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value, Is.EqualTo(value));
            Assert.That(decoded.OctetCount, Is.EqualTo(16385));
            Assert.That(reader.GetAvailableBitCount(), Is.Zero);
        });
    }

    private static PerWholeNumberCodec CreateCodec()
    {
        return new PerWholeNumberCodec(new AsnResourceLimits());
    }

    private static PerBitWriter CreateWriter()
    {
        return new PerBitWriter(new AsnResourceLimits());
    }

    private static PerBitReader CreateReader()
    {
        return new PerBitReader(new AsnResourceLimits());
    }
}
