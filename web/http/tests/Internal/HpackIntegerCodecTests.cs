namespace http.Tests;

public class HpackIntegerCodecTests
{
    [Test]
    public void CodecMatchesRfcIntegerExamples()
    {
        HpackIntegerResult ten = HpackIntegerCodec.Encode(10, 5, 0);
        HpackIntegerResult thirteenThirtySeven = HpackIntegerCodec.Encode(1337, 5, 0);
        HpackIntegerResult fortyTwo = HpackIntegerCodec.Encode(42, 8, 0);

        HpackIntegerResult decodedTen = HpackIntegerCodec.Decode(ten.Bytes, 5);
        HpackIntegerResult decodedThirteenThirtySeven =
            HpackIntegerCodec.Decode(thirteenThirtySeven.Bytes, 5);
        HpackIntegerResult decodedFortyTwo = HpackIntegerCodec.Decode(fortyTwo.Bytes, 8);

        Assert.Multiple(() =>
        {
            Assert.That(ten.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(ten.Bytes, Is.EqualTo(new byte[] { 10 }));
            Assert.That(thirteenThirtySeven.Bytes,
                Is.EqualTo(new byte[] { 31, 154, 10 }));
            Assert.That(fortyTwo.Bytes, Is.EqualTo(new byte[] { 42 }));
            Assert.That(decodedTen.Value, Is.EqualTo(10));
            Assert.That(decodedThirteenThirtySeven.Value, Is.EqualTo(1337));
            Assert.That(decodedFortyTwo.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void CodecRoundTripsBoundariesForEveryPrefixWidth()
    {
        long[] commonValues = [0, 1, 2, 30, 31, 32, 126, 127, 128, 255,
            256, 1337, long.MaxValue];

        for (int prefixWidth = 1; prefixWidth <= 8; prefixWidth++)
        {
            int prefixMaximum = (1 << prefixWidth) - 1;
            byte prefixBits = (byte)(0xff & ~prefixMaximum);
            long[] boundaryValues = [prefixMaximum - 1, prefixMaximum,
                prefixMaximum + 1];

            foreach (long value in commonValues)
            {
                AssertRoundTrip(value, prefixWidth, prefixBits);
            }

            foreach (long value in boundaryValues)
            {
                AssertRoundTrip(value, prefixWidth, prefixBits);
            }
        }
    }

    [Test]
    public void DecoderConsumesOnlyTheIntegerAndIgnoresRepresentationBits()
    {
        HpackIntegerResult direct = HpackIntegerCodec.Decode(
            new byte[] { 0xea, 0xcc }, 5);
        HpackIntegerResult continued = HpackIntegerCodec.Decode(
            new byte[] { 0x3f, 0x9a, 0x0a, 0xcc }, 5);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(direct.Value, Is.EqualTo(10));
            Assert.That(direct.Consumed, Is.EqualTo(1));
            Assert.That(continued.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(continued.Value, Is.EqualTo(1337));
            Assert.That(continued.Consumed, Is.EqualTo(3));
        });
    }

    [Test]
    public void DecoderAcceptsNonCanonicalContinuationForms()
    {
        HpackIntegerResult redundantZero = HpackIntegerCodec.Decode(
            new byte[] { 0x1f, 0x80, 0x00 }, 5);
        HpackIntegerResult redundantOne = HpackIntegerCodec.Decode(
            new byte[] { 0x1f, 0x81, 0x00 }, 5);

        HpackIntegerResult canonicalZero = HpackIntegerCodec.Encode(
            redundantZero.Value, 5, 0);
        HpackIntegerResult canonicalOne = HpackIntegerCodec.Encode(
            redundantOne.Value, 5, 0);

        Assert.Multiple(() =>
        {
            Assert.That(redundantZero.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(redundantZero.Value, Is.EqualTo(31));
            Assert.That(redundantZero.Consumed, Is.EqualTo(3));
            Assert.That(redundantOne.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(redundantOne.Value, Is.EqualTo(32));
            Assert.That(redundantOne.Consumed, Is.EqualTo(3));
            Assert.That(canonicalZero.Bytes, Is.EqualTo(new byte[] { 0x1f, 0x00 }));
            Assert.That(canonicalOne.Bytes, Is.EqualTo(new byte[] { 0x1f, 0x01 }));
        });
    }

    [Test]
    public void DecoderReportsExactTruncationRequirements()
    {
        HpackIntegerResult empty = HpackIntegerCodec.Decode(
            ReadOnlySpan<byte>.Empty, 5);
        HpackIntegerResult missingFirstContinuation = HpackIntegerCodec.Decode(
            new byte[] { 0x1f }, 5);
        HpackIntegerResult missingLaterContinuation = HpackIntegerCodec.Decode(
            new byte[] { 0x1f, 0x80 }, 5);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(empty.Consumed, Is.Zero);
            Assert.That(empty.RequiredSize, Is.EqualTo(1));
            Assert.That(missingFirstContinuation.Status,
                Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(missingFirstContinuation.Consumed, Is.EqualTo(1));
            Assert.That(missingFirstContinuation.RequiredSize, Is.EqualTo(2));
            Assert.That(missingLaterContinuation.Status,
                Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(missingLaterContinuation.Consumed, Is.EqualTo(2));
            Assert.That(missingLaterContinuation.RequiredSize, Is.EqualTo(3));
            Assert.That(missingLaterContinuation.ErrorCode, Is.Null);
        });
    }

    [Test]
    public void DecoderRejectsValuesBeyondTheSignedRangeAtTheExactOctet()
    {
        byte[] overflow = [0xff, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x01];

        HpackIntegerResult maximum = HpackIntegerCodec.Encode(long.MaxValue, 8, 0);
        HpackIntegerResult decodedMaximum = HpackIntegerCodec.Decode(maximum.Bytes, 8);
        HpackIntegerResult rejected = HpackIntegerCodec.Decode(overflow, 8);

        Assert.Multiple(() =>
        {
            Assert.That(decodedMaximum.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decodedMaximum.Value, Is.EqualTo(long.MaxValue));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.IsOverflow, Is.True);
            Assert.That(rejected.IsMalformed, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("hpack-integer-overflow"));
            Assert.That(rejected.HasErrorPosition, Is.True);
            Assert.That(rejected.ErrorPosition, Is.EqualTo(10));
            Assert.That(rejected.Consumed, Is.EqualTo(11));
        });
    }

    [Test]
    public void DecoderRejectsAnExcessivelyLongZeroContinuationForm()
    {
        byte[] input = new byte[11];
        input[0] = 0xff;
        int index = 1;
        while (index < 10)
        {
            input[index] = 0x80;
            index = index + 1;
        }

        HpackIntegerResult result = HpackIntegerCodec.Decode(input, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsMalformed, Is.True);
            Assert.That(result.IsOverflow, Is.False);
            Assert.That(result.Consumed, Is.EqualTo(11));
            Assert.That(result.ErrorPosition, Is.EqualTo(10));
            Assert.That(result.ErrorCode,
                Is.EqualTo("hpack-integer-length-exceeded"));
        });
    }

    [Test]
    public void BoundedWriterIsExactAndAllOrNothing()
    {
        byte[] exact = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc];
        byte[] insufficient = [0xcc, 0xcc, 0xcc, 0xcc];

        HpackIntegerResult written = HpackIntegerCodec.Write(
            1337, 5, 0x20, exact, 1);
        HpackIntegerResult needOutput = HpackIntegerCodec.Write(
            1337, 5, 0x20, insufficient, 2);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(3));
            Assert.That(written.RequiredSize, Is.EqualTo(4));
            Assert.That(exact,
                Is.EqualTo(new byte[] { 0xcc, 0x3f, 0x9a, 0x0a, 0xcc }));
            Assert.That(needOutput.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(5));
            Assert.That(insufficient,
                Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc, 0xcc }));
        });
    }

    [Test]
    public void EncoderRejectsInvalidArgumentsWithoutWriting()
    {
        byte[] output = [0xcc, 0xcc, 0xcc];

        HpackIntegerResult narrow = HpackIntegerCodec.Encode(0, 0, 0);
        HpackIntegerResult wide = HpackIntegerCodec.Encode(0, 9, 0);
        HpackIntegerResult negative = HpackIntegerCodec.Encode(-1, 5, 0);
        HpackIntegerResult overlapping = HpackIntegerCodec.Write(
            10, 5, 0x01, output, 0);
        HpackIntegerResult negativeOffset = HpackIntegerCodec.Write(
            10, 5, 0, output, -1);

        Assert.Multiple(() =>
        {
            Assert.That(narrow.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(narrow.ErrorCode,
                Is.EqualTo("invalid-hpack-integer-prefix-width"));
            Assert.That(wide.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(negative.ErrorCode,
                Is.EqualTo("invalid-hpack-integer-value"));
            Assert.That(overlapping.ErrorCode,
                Is.EqualTo("invalid-hpack-integer-prefix-bits"));
            Assert.That(negativeOffset.ErrorCode,
                Is.EqualTo("invalid-hpack-integer-offset"));
            Assert.That(output, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc }));
        });
    }

    static void AssertRoundTrip(long value, int prefixWidth, byte prefixBits)
    {
        HpackIntegerResult encoded = HpackIntegerCodec.Encode(
            value, prefixWidth, prefixBits);
        HpackIntegerResult decoded = HpackIntegerCodec.Decode(
            encoded.Bytes, prefixWidth);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done),
                $"Encoding {value} with prefix width {prefixWidth} failed.");
            Assert.That(encoded.Written, Is.EqualTo(encoded.Bytes.Length));
            Assert.That(encoded.RequiredSize, Is.EqualTo(encoded.Bytes.Length));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done),
                $"Decoding {value} with prefix width {prefixWidth} failed.");
            Assert.That(decoded.Value, Is.EqualTo(value));
            Assert.That(decoded.Consumed, Is.EqualTo(encoded.Bytes.Length));
            Assert.That((encoded.Bytes[0] & ~((1 << prefixWidth) - 1)),
                Is.EqualTo(prefixBits));
        });
    }
}
