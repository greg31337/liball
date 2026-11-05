using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class WirePrimitiveTests
{
    [Test]
    public void ByteSearchFindsBytesAndSequencesWithoutReadingPastBounds()
    {
        byte[] source = [0, 1, 255, 1, 2, 3];

        Assert.Multiple(() =>
        {
            Assert.That(ByteSearch.IndexOfByte(source, 1), Is.EqualTo(1));
            Assert.That(ByteSearch.LastIndexOfByte(source, 1), Is.EqualTo(3));
            Assert.That(ByteSearch.IndexOfByte(source, 4), Is.EqualTo(-1));
            Assert.That(ByteSearch.IndexOfByte(ReadOnlySpan<byte>.Empty, 1), Is.EqualTo(-1));
            Assert.That(ByteSearch.IndexOfSequence(source, new byte[] { 255, 1, 2 }), Is.EqualTo(2));
            Assert.That(ByteSearch.IndexOfSequence(source, new byte[] { 2, 4 }), Is.EqualTo(-1));
            Assert.That(ByteSearch.IndexOfSequence(source, ReadOnlySpan<byte>.Empty), Is.Zero);
            Assert.That(ByteSearch.IndexOfSequence(new byte[] { 1 }, new byte[] { 1, 2 }), Is.EqualTo(-1));
        });
    }

    [Test]
    public void UnsignedAsciiParsingHandlesZeroBoundsInvalidDigitsAndOverflow()
    {
        AsciiIntegerResult zero = AsciiDigits.ParseUnsignedInt("0"u8, int.MaxValue);
        AsciiIntegerResult leadingZeros = AsciiDigits.ParseUnsignedInt("00042"u8, int.MaxValue);
        AsciiIntegerResult maximum = AsciiDigits.ParseUnsignedInt("2147483647"u8, int.MaxValue);
        AsciiIntegerResult empty = AsciiDigits.ParseUnsignedInt(ReadOnlySpan<byte>.Empty, int.MaxValue);
        AsciiIntegerResult invalid = AsciiDigits.ParseUnsignedInt("12A"u8, int.MaxValue);
        AsciiIntegerResult overflow = AsciiDigits.ParseUnsignedInt("2147483648"u8, int.MaxValue);
        AsciiIntegerResult smallMaximum = AsciiDigits.ParseUnsignedInt("6"u8, 5);

        Assert.Multiple(() =>
        {
            Assert.That(zero.Success, Is.True);
            Assert.That(zero.Value, Is.Zero);
            Assert.That(leadingZeros.Value, Is.EqualTo(42));
            Assert.That(leadingZeros.Count, Is.EqualTo(5));
            Assert.That(maximum.Value, Is.EqualTo(int.MaxValue));
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Required, Is.EqualTo(1));
            Assert.That(empty.ErrorCode, Is.EqualTo("incomplete"));
            Assert.That(invalid.ErrorIndex, Is.EqualTo(2));
            Assert.That(invalid.ErrorCode, Is.EqualTo("invalid-digit"));
            Assert.That(overflow.ErrorCode, Is.EqualTo("overflow"));
            Assert.That(smallMaximum.ErrorCode, Is.EqualTo("overflow"));
        });
    }

    [Test]
    public void UnsignedAsciiFormattingReportsRequiredSizeBeforeWriting()
    {
        byte[] zeroDestination = new byte[1];
        AsciiIntegerResult zero = AsciiDigits.WriteUnsignedInt(0, zeroDestination);
        byte[] maximumDestination = new byte[10];
        AsciiIntegerResult maximum = AsciiDigits.WriteUnsignedInt(int.MaxValue, maximumDestination);
        byte[] shortDestination = [88, 88, 88];
        AsciiIntegerResult shortResult = AsciiDigits.WriteUnsignedInt(12345, shortDestination);
        AsciiIntegerResult negative = AsciiDigits.WriteUnsignedInt(-1, maximumDestination);

        Assert.Multiple(() =>
        {
            Assert.That(zero.Success, Is.True);
            Assert.That(zeroDestination, Is.EqualTo(new byte[] { 48 }));
            Assert.That(maximum.Success, Is.True);
            Assert.That(Encoding.ASCII.GetString(maximumDestination), Is.EqualTo("2147483647"));
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Required, Is.EqualTo(5));
            Assert.That(shortResult.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 88, 88, 88 }));
            Assert.That(negative.ErrorCode, Is.EqualTo("negative-value"));
        });
    }

    [Test]
    public void LimitChecksHandleInclusiveBoundsAndCombinedOverflow()
    {
        LimitCheckResult atLimit = WireLimit.CheckValue(16, 16);
        LimitCheckResult exceeded = WireLimit.CheckValue(17, 16);
        LimitCheckResult invalidActual = WireLimit.CheckValue(-1, 16);
        LimitCheckResult invalidLimit = WireLimit.CheckValue(0, -1);
        LimitCheckResult combined = WireLimit.CheckCombinedValue(10, 6, 16);
        LimitCheckResult combinedExceeded = WireLimit.CheckCombinedValue(10, 7, 16);
        LimitCheckResult integerOverflow = WireLimit.CheckCombinedValue(int.MaxValue, 1, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(atLimit.Allowed, Is.True);
            Assert.That(atLimit.Actual, Is.EqualTo(16));
            Assert.That(exceeded.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(invalidActual.ErrorCode, Is.EqualTo("invalid-actual"));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(combined.Allowed, Is.True);
            Assert.That(combined.Actual, Is.EqualTo(16));
            Assert.That(combinedExceeded.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(integerOverflow.ErrorCode, Is.EqualTo("limit-exceeded"));
        });
    }

    [Test]
    public void ChecksumCalculationFormattingAndParsingCoverEveryWireValue()
    {
        Assert.That(ChecksumCalculator.CalculateChecksum(ReadOnlySpan<byte>.Empty), Is.Zero);
        Assert.That(ChecksumCalculator.CalculateChecksum(new byte[] { 65, 66, 67 }), Is.EqualTo(198));
        Assert.That(ChecksumCalculator.CalculateChecksum(new byte[] { 255, 2 }), Is.EqualTo(1));
        Assert.That(ChecksumCalculator.CalculateChecksumRange(new byte[] { 99, 65, 66, 67, 99 }, 1, 3), Is.EqualTo(198));
        Assert.That(ChecksumCalculator.CalculateChecksumRange(new byte[] { 1, 2 }, 1, 2), Is.EqualTo(-1));

        Assert.Multiple(() =>
        {
            for (int value = 0; value < FixDefaults.ChecksumModulo; value++)
            {
                byte[] destination = new byte[FixDefaults.ChecksumDigits];
                Assert.That(ChecksumCalculator.WriteChecksum(value, destination), Is.True, value.ToString());
                AsciiIntegerResult parsed = ChecksumCalculator.ParseChecksum(destination);
                Assert.That(parsed.Success, Is.True, value.ToString());
                Assert.That(parsed.Value, Is.EqualTo(value), value.ToString());
            }
        });

        Assert.That(ChecksumCalculator.WriteChecksum(-1, new byte[3]), Is.False);
        Assert.That(ChecksumCalculator.WriteChecksum(256, new byte[3]), Is.False);
        Assert.That(ChecksumCalculator.WriteChecksum(1, new byte[2]), Is.False);
        Assert.That(ChecksumCalculator.ParseChecksum("12"u8).ErrorCode, Is.EqualTo("incomplete"));
        Assert.That(ChecksumCalculator.ParseChecksum("0000"u8).ErrorCode, Is.EqualTo("invalid-length"));
        Assert.That(ChecksumCalculator.ParseChecksum("25A"u8).ErrorCode, Is.EqualTo("invalid-digit"));
        Assert.That(ChecksumCalculator.ParseChecksum("256"u8).ErrorCode, Is.EqualTo("overflow"));
    }

    [Test]
    public void BodyLengthCalculationMeasuresFromTagNineDelimiterToChecksumField()
    {
        byte[] message = Wire("8=FIX.4.4|9=12|35=D|11=ABC|10=000|");
        BodyLengthResult result = BodyLengthCalculator.CalculateBodyLength(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Matches, Is.True);
            Assert.That(result.DeclaredLength, Is.EqualTo(12));
            Assert.That(result.CalculatedLength, Is.EqualTo(12));
            Assert.That(result.BodyStart, Is.EqualTo(15));
            Assert.That(result.ChecksumStart, Is.EqualTo(27));
        });
    }

    [Test]
    public void BodyLengthCalculationReportsMismatchAndMalformedEnvelope()
    {
        BodyLengthResult mismatch = BodyLengthCalculator.CalculateBodyLength(Wire("8=FIX.4.4|9=11|35=D|11=ABC|10=000|"));
        BodyLengthResult missing = BodyLengthCalculator.CalculateBodyLength(Wire("8=FIX.4.4|35=D|10=000|"));
        BodyLengthResult invalidDigits = BodyLengthCalculator.CalculateBodyLength(Wire("8=FIX.4.4|9=1A|35=D|10=000|"));
        BodyLengthResult invalidChecksum = BodyLengthCalculator.CalculateBodyLength(Wire("8=FIX.4.4|9=5|35=D|11=000|"));
        byte[] destination = new byte[2];
        AsciiIntegerResult written = BodyLengthCalculator.WriteBodyLength(12, destination);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.Matches, Is.False);
            Assert.That(mismatch.DeclaredLength, Is.EqualTo(11));
            Assert.That(mismatch.CalculatedLength, Is.EqualTo(12));
            Assert.That(missing.ErrorCode, Is.EqualTo("missing-body-length"));
            Assert.That(invalidDigits.ErrorCode, Is.EqualTo("invalid-body-length"));
            Assert.That(invalidChecksum.ErrorCode, Is.EqualTo("invalid-checksum-field"));
            Assert.That(written.Success, Is.True);
            Assert.That(destination, Is.EqualTo(new byte[] { 49, 50 }));
        });
    }

    private static byte[] Wire(string value)
    {
        return Encoding.ASCII.GetBytes(value.Replace('|', (char)FixDefaults.Soh));
    }
}
