using System.Text;
using email;

namespace email.Tests;

public class EmailUuencodeTests
{
    [Test]
    public void DetectionFindsTraditionalHeaderAfterPreamble()
    {
        byte[] input = Encoding.ASCII.GetBytes("intro\r\nbegin 644 cat.txt\r\n#0V%T\r\n`\r\nend\r\n");

        EmailUuencodeDetectionResult result = new EmailUuencode().Detect(input);

        Assert.That(result.IsDetected(), Is.True);
        Assert.That(result.GetFileMode(), Is.EqualTo(420));
        Assert.That(result.GetFileName(), Is.EqualTo("cat.txt"));
        Assert.That(result.GetHeaderPosition()!.GetByteOffset(), Is.EqualTo(7));
        Assert.That(result.GetHeaderPosition()!.GetLineNumber(), Is.EqualTo(2));
        Assert.That(result.GetHeaderPosition()!.GetColumnNumber(), Is.EqualTo(1));
    }

    [TestCase("begin 999 file")]
    [TestCase("begin 644 ")]
    [TestCase("begin-base64 644 file")]
    [TestCase("Begin 644 file")]
    public void DetectionRejectsNonTraditionalOrMalformedHeaders(string value)
    {
        EmailUuencodeDetectionResult result = new EmailUuencode().Detect(Encoding.ASCII.GetBytes(value));

        Assert.That(result.IsDetected(), Is.False);
        Assert.That(result.HasFileName(), Is.False);
        Assert.That(result.HasHeaderPosition(), Is.False);
    }

    [Test]
    public void CanonicalEncoderProducesKnownVectorWithCrlf()
    {
        byte[] encoded = new EmailUuencode().Encode(Encoding.ASCII.GetBytes("Cat"), 420, "cat.txt");

        Assert.That(
            Encoding.ASCII.GetString(encoded),
            Is.EqualTo("begin 644 cat.txt\r\n#0V%T\r\n`\r\nend\r\n"));
    }

    [Test]
    public void DecoderAcceptsCanonicalKnownVectorAndMetadata()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("begin 644 cat.txt\r\n#0V%T\r\n`\r\nend\r\n");

        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(encoded, 100);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(result.GetDetection().IsDetected(), Is.True);
        Assert.That(result.GetDetection().GetFileMode(), Is.EqualTo(420));
        Assert.That(result.GetDetection().GetFileName(), Is.EqualTo("cat.txt"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasFatalError(), Is.False);
    }

    [Test]
    public void TrailingTextAfterEndMarkerIsIgnored()
    {
        byte[] encoded = Encoding.ASCII.GetBytes(
            "begin 644 cat.txt\r\n#0V%T\r\n`\r\nend\r\n" + new string('x', 100) + "\r\n");
        EmailUuencodeDecoder decoder = new EmailUuencodeDecoder(10, 20);
        List<byte> decoded = new List<byte>();
        decoded.AddRange(decoder.Feed(encoded));
        decoded.AddRange(decoder.Complete());

        Assert.That(Encoding.ASCII.GetString(decoded.ToArray()), Is.EqualTo("Cat"));
        Assert.That(decoder.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void IncrementalDecodingIsIndependentOfEveryTwoChunkSplit()
    {
        byte[] original = CreateInput(100);
        byte[] encoded = new EmailUuencode().Encode(original, 384, "data.bin");
        int split = 0;
        while (split <= encoded.Length)
        {
            EmailUuencodeDecoder decoder = new EmailUuencodeDecoder(1000, EmailUuencodeConstants.DefaultMaximumLineBytes);
            List<byte> decoded = new List<byte>();
            decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, 0, split)));
            decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, split, encoded.Length - split)));
            decoded.AddRange(decoder.Complete());

            Assert.That(decoded.ToArray(), Is.EqualTo(original), "split " + split);
            Assert.That(decoder.GetSourceByteCount(), Is.EqualTo(encoded.Length));
            Assert.That(decoder.GetDecodedByteCount(), Is.EqualTo(original.Length));
            Assert.That(decoder.GetIssues().GetCount(), Is.Zero);
            split++;
        }
    }

    [Test]
    public void CanonicalGenerationRoundTripsManyLengths()
    {
        int length = 0;
        while (length <= 128)
        {
            byte[] original = CreateInput(length);
            byte[] encoded = new EmailUuencode().Encode(original, 416, "value.bin");
            EmailUuencodeDecodeResult decoded = new EmailUuencode().Decode(encoded, original.Length);

            Assert.That(decoded.GetDecodedBytes(), Is.EqualTo(original), "length " + length);
            Assert.That(decoded.GetIssues().GetCount(), Is.Zero, "length " + length);
            length++;
        }
    }

    [Test]
    public void DecoderAcceptsSpaceZeroMarkerAndMixedLineEndings()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("begin 644 cat.txt\n#0V%T\r \r\nend");

        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(encoded, 10);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EmptyLineRecoversAStrippedSpaceZeroMarker()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("begin 644 cat.txt\n#0V%T\n\nend\n");

        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(encoded, 10);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeInvalidLine));
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void TruncatedDataLineReturnsEveryRecoverableByte()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("begin 644 cat.txt\r\n#0V\r\n`\r\nend\r\n");

        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(encoded, 10);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("C"));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeTruncatedLine));
        Assert.That(result.GetIssues().GetAt(0).GetRecoveryDescription(), Does.Contain("recoverable"));
    }

    [Test]
    public void InvalidEncodingCharacterIsMaskedAndReported()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("begin 644 cat.txt\r\n#0~%T\r\n`\r\nend\r\n");

        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(encoded, 10);

        Assert.That(result.GetDecodedByteCount(), Is.EqualTo(3));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeInvalidCharacter));
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(21));
    }

    [Test]
    public void MissingTerminatorsRetainDecodedBytesWithWarnings()
    {
        EmailUuencodeDecodeResult missingBoth = new EmailUuencode().Decode(
            Encoding.ASCII.GetBytes("begin 644 cat.txt\n#0V%T"),
            10);
        EmailUuencodeDecodeResult missingEnd = new EmailUuencode().Decode(
            Encoding.ASCII.GetBytes("begin 644 cat.txt\n#0V%T\n`"),
            10);
        EmailUuencodeDecodeResult missingZero = new EmailUuencode().Decode(
            Encoding.ASCII.GetBytes("begin 644 cat.txt\n#0V%T\nend"),
            10);

        Assert.That(Encoding.ASCII.GetString(missingBoth.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(missingBoth.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeMissingTermination));
        Assert.That(Encoding.ASCII.GetString(missingEnd.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(missingEnd.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeMissingTermination));
        Assert.That(Encoding.ASCII.GetString(missingZero.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(missingZero.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeMissingTermination));
    }

    [Test]
    public void MissingHeaderIsAnErrorWithoutDecodedOutput()
    {
        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(
            Encoding.ASCII.GetBytes("not uuencoded"),
            100);

        Assert.That(result.GetDecodedBytes(), Is.Empty);
        Assert.That(result.GetDetection().IsDetected(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeHeaderNotFound));
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(result.IsSuccess(), Is.False);
    }

    [Test]
    public void DecodedLimitStopsBeforeCrossingAndReturnsPrefix()
    {
        byte[] encoded = new EmailUuencode().Encode(Encoding.ASCII.GetBytes("Cat"), 420, "cat.txt");

        EmailUuencodeDecodeResult result = new EmailUuencode().Decode(encoded, 2);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("Ca"));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.UuencodeDecodedByteLimitExceeded));
    }

    [Test]
    public void DecoderLifecycleAndSnapshotsAreExplicit()
    {
        EmailUuencodeDecoder decoder = new EmailUuencodeDecoder(10, 100);
        decoder.Feed(Encoding.ASCII.GetBytes("begin 644 x\n#0V\n"));
        EmailIssueList snapshot = decoder.GetIssues();
        snapshot.Clear();
        decoder.Complete();

        Assert.That(decoder.GetMaximumDecodedBytes(), Is.EqualTo(10));
        Assert.That(decoder.GetMaximumLineBytes(), Is.EqualTo(100));
        Assert.That(decoder.GetDetection().IsDetected(), Is.True);
        Assert.That(decoder.HasFatalError(), Is.False);
        Assert.That(decoder.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => decoder.Feed(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => decoder.Complete());
    }

    [Test]
    public void MetadataArgumentsAndResultOwnershipAreValidated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailUuencodeDecoder(-1, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailUuencodeDecoder(1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailUuencode().Encode(Array.Empty<byte>(), -1, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailUuencode().Encode(Array.Empty<byte>(), 512, "x"));
        Assert.Throws<ArgumentNullException>(() => new EmailUuencode().Encode(Array.Empty<byte>(), 420, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailUuencode().Encode(Array.Empty<byte>(), 420, ""));
        Assert.Throws<ArgumentException>(() => new EmailUuencode().Encode(Array.Empty<byte>(), 420, "bad\nname"));
        Assert.Throws<ArgumentNullException>(() => new EmailUuencodeDetectionResultOperations().CreateDetected(420, "x", null!));
        Assert.Throws<ArgumentNullException>(() => new EmailUuencodeDecodeResult(
            null!,
            new EmailUuencodeDetectionResultOperations().CreateNotDetected(),
            new EmailIssueList()));

        byte[] value = new byte[] { 1 };
        EmailIssueList issues = new EmailIssueList();
        EmailUuencodeDecodeResult result = new EmailUuencodeDecodeResult(
            value,
            new EmailUuencodeDetectionResultOperations().CreateNotDetected(),
            issues);
        value[0] = 2;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Error, EmailIssueCode.ValidationFailed, "test"));
        byte[] returned = result.GetDecodedBytes();
        returned[0] = 3;
        Assert.That(result.GetDecodedBytes(), Is.EqualTo(new byte[] { 1 }));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    static byte[] CreateInput(int length)
    {
        byte[] input = new byte[length];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)(index * 37 + length);
            index++;
        }

        return input;
    }
}
