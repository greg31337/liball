using System.Text;
using email;

namespace email.Tests;

public class EmailBase64DecoderTests
{
    [TestCase("", "")]
    [TestCase("TQ==", "M")]
    [TestCase("TWE=", "Ma")]
    [TestCase("TWFu", "Man")]
    [TestCase("U29tZSBsb25nZXIgdGV4dA==", "Some longer text")]
    public void StrictDecoderAcceptsCanonicalVectors(string encoded, string decoded)
    {
        EmailBase64DecodeResult result = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes(encoded),
            EmailBase64DecodeMode.Strict,
            1000);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo(decoded));
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasFatalError(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void CanonicalDataDecodesIdenticallyAcrossEveryTwoChunkSplit()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("VGhlIHF1aWNrIGJyb3duIGZveA==");
        byte[] expected = Encoding.ASCII.GetBytes("The quick brown fox");
        int split = 0;
        while (split <= encoded.Length)
        {
            EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Strict, 1000);
            List<byte> decoded = new List<byte>();
            decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, 0, split)));
            decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, split, encoded.Length - split)));
            decoded.AddRange(decoder.Complete());

            Assert.That(decoded.ToArray(), Is.EqualTo(expected), "split " + split);
            Assert.That(decoder.GetDecodedByteCount(), Is.EqualTo(expected.Length));
            Assert.That(decoder.GetIssues().GetCount(), Is.Zero);
            split++;
        }
    }

    [Test]
    public void CanonicalRoundTripsRemainStableForManyLengthsAndSmallChunks()
    {
        int length = 0;
        while (length <= 128)
        {
            byte[] original = new byte[length];
            int index = 0;
            while (index < original.Length)
            {
                original[index] = (byte)(index * 37 + length);
                index++;
            }

            byte[] encoded = Encoding.ASCII.GetBytes(Convert.ToBase64String(original));
            int chunkSize = 1;
            while (chunkSize <= 7)
            {
                EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Strict, 1000);
                List<byte> decoded = new List<byte>();
                int offset = 0;
                while (offset < encoded.Length)
                {
                    int count = encoded.Length - offset;
                    if (count > chunkSize)
                    {
                        count = chunkSize;
                    }

                    decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, offset, count)));
                    offset += count;
                }

                decoded.AddRange(decoder.Complete());
                Assert.That(decoded.ToArray(), Is.EqualTo(original), "length " + length + ", chunk " + chunkSize);
                Assert.That(decoder.GetIssues().GetCount(), Is.Zero);
                chunkSize++;
            }

            length++;
        }
    }

    [Test]
    public void StandardMimeWhitespaceIsIgnoredInBothModes()
    {
        byte[] encoded = Encoding.ASCII.GetBytes(" T\tW\r\nFu \r\n");

        EmailBase64DecodeResult strict = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Strict, 10);
        EmailBase64DecodeResult tolerant = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Tolerant, 10);

        Assert.That(Encoding.ASCII.GetString(strict.GetDecodedBytes()), Is.EqualTo("Man"));
        Assert.That(strict.GetIssues().GetCount(), Is.Zero);
        Assert.That(tolerant.GetDecodedBytes(), Is.EqualTo(strict.GetDecodedBytes()));
        Assert.That(tolerant.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void InvalidCharactersAreErrorsInStrictModeAndWarningsInTolerantMode()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("T!W@Fu");

        EmailBase64DecodeResult strict = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Strict, 10);
        EmailBase64DecodeResult tolerant = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Tolerant, 10);

        Assert.That(Encoding.ASCII.GetString(strict.GetDecodedBytes()), Is.EqualTo("Man"));
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(strict.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64InvalidCharacter));
        Assert.That(Encoding.ASCII.GetString(tolerant.GetDecodedBytes()), Is.EqualTo("Man"));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
    }

    [Test]
    public void NonCanonicalWhitespaceIsOnlyToleratedWithADiagnostic()
    {
        byte[] encoded = new byte[] { (byte)'T', (byte)'Q', 11, (byte)'=', (byte)'=' };

        EmailBase64DecodeResult strict = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Strict, 10);
        EmailBase64DecodeResult tolerant = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Tolerant, 10);

        Assert.That(strict.GetDecodedBytes(), Is.EqualTo(new byte[] { (byte)'M' }));
        Assert.That(strict.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64InvalidCharacter));
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(tolerant.GetDecodedBytes(), Is.EqualTo(new byte[] { (byte)'M' }));
        Assert.That(tolerant.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64NonCanonicalWhitespace));
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
    }

    [TestCase("TQ", "M", 1)]
    [TestCase("TWE", "Ma", 1)]
    [TestCase("TQ=", "M", 1)]
    [TestCase("T", "", 1)]
    public void MissingPaddingProducesDeterministicPartialOutput(string encoded, string decoded, int issueCount)
    {
        EmailBase64DecodeResult result = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes(encoded),
            EmailBase64DecodeMode.Tolerant,
            10);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo(decoded));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(issueCount));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64TruncatedQuantum));
    }

    [Test]
    public void MisplacedExcessAndTrailingPaddingAreReportedPrecisely()
    {
        EmailBase64DecodeResult leading = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes("=TQ=="),
            EmailBase64DecodeMode.Tolerant,
            10);
        EmailBase64DecodeResult excess = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes("TQ==="),
            EmailBase64DecodeMode.Strict,
            10);

        Assert.That(Encoding.ASCII.GetString(leading.GetDecodedBytes()), Is.EqualTo("M"));
        Assert.That(leading.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64InvalidPadding));
        Assert.That(leading.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.Zero);
        Assert.That(Encoding.ASCII.GetString(excess.GetDecodedBytes()), Is.EqualTo("M"));
        Assert.That(excess.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(excess.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64InvalidPadding));
        Assert.That(excess.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(4));
    }

    [Test]
    public void AlphabetAfterTerminalPaddingIsIgnoredAndReported()
    {
        EmailBase64DecodeResult result = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes("TQ==QQ=="),
            EmailBase64DecodeMode.Tolerant,
            10);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("M"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Base64DataAfterPadding), Is.True);
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(4));
    }

    [Test]
    public void NonZeroPaddingBitsAreReportedButHighOrderBytesRemainUsable()
    {
        EmailBase64DecodeResult oneByte = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes("TR=="),
            EmailBase64DecodeMode.Strict,
            10);
        EmailBase64DecodeResult twoBytes = new EmailBase64().Decode(
            Encoding.ASCII.GetBytes("TWF="),
            EmailBase64DecodeMode.Tolerant,
            10);

        Assert.That(Encoding.ASCII.GetString(oneByte.GetDecodedBytes()), Is.EqualTo("M"));
        Assert.That(oneByte.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64NonZeroPaddingBits));
        Assert.That(oneByte.IsSuccess(), Is.False);
        Assert.That(Encoding.ASCII.GetString(twoBytes.GetDecodedBytes()), Is.EqualTo("Ma"));
        Assert.That(twoBytes.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64NonZeroPaddingBits));
        Assert.That(twoBytes.IsSuccess(), Is.True);
    }

    [Test]
    public void IssuePositionsTrackLinesAcrossFeedBoundaries()
    {
        EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Strict, 10);
        List<byte> decoded = new List<byte>();
        decoded.AddRange(decoder.Feed(Encoding.ASCII.GetBytes("TQ\r")));
        decoded.AddRange(decoder.Feed(Encoding.ASCII.GetBytes("\n!==")));
        decoded.AddRange(decoder.Complete());

        EmailIssue issue = decoder.GetIssues().GetAt(0);
        Assert.That(decoded.ToArray(), Is.EqualTo(new byte[] { (byte)'M' }));
        Assert.That(issue.GetCode(), Is.EqualTo(EmailIssueCode.Base64InvalidCharacter));
        Assert.That(issue.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(4));
        Assert.That(issue.GetSourcePosition()!.GetLineNumber(), Is.EqualTo(2));
        Assert.That(issue.GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void DecodedLimitStopsBeforeCrossingQuantumAndReturnsPrefix()
    {
        EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Tolerant, 3);
        byte[] output = decoder.Feed(Encoding.ASCII.GetBytes("TWFuTQ==ignored"));
        byte[] tail = decoder.Complete();

        Assert.That(Encoding.ASCII.GetString(output), Is.EqualTo("Man"));
        Assert.That(tail, Is.Empty);
        Assert.That(decoder.GetDecodedByteCount(), Is.EqualTo(3));
        Assert.That(decoder.HasFatalError(), Is.True);
        Assert.That(decoder.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(decoder.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.DecodedByteLimitExceeded));
        Assert.That(decoder.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Fatal));
        Assert.That(decoder.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(7));
    }

    [Test]
    public void IncompleteQuantumRecoveryDescribesLimitBlockedOutputAccurately()
    {
        EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Tolerant, 0);
        Assert.That(decoder.Feed(Encoding.ASCII.GetBytes("TQ")), Is.Empty);

        byte[] tail = decoder.Complete();

        Assert.That(tail, Is.Empty);
        Assert.That(decoder.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(decoder.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.Base64TruncatedQuantum));
        Assert.That(decoder.GetIssues().GetAt(0).GetRecoveryDescription(), Does.Contain("not emitted"));
        Assert.That(decoder.GetIssues().GetAt(1).GetCode(), Is.EqualTo(EmailIssueCode.DecodedByteLimitExceeded));
        Assert.That(decoder.GetIssues().GetAt(1).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Fatal));
    }

    [Test]
    public void DecoderLifecycleAndIssueSnapshotsAreExplicit()
    {
        EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Strict, 10);
        decoder.Feed(new byte[] { (byte)'!' });
        EmailIssueList snapshot = decoder.GetIssues();
        snapshot.Clear();

        decoder.Complete();

        Assert.That(decoder.GetMode(), Is.EqualTo(EmailBase64DecodeMode.Strict));
        Assert.That(decoder.GetMaximumDecodedBytes(), Is.EqualTo(10));
        Assert.That(decoder.IsComplete(), Is.True);
        Assert.That(decoder.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => decoder.Feed(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => decoder.Complete());
    }

    [Test]
    public void DecoderAndResultValidateAndDefensivelyCopyInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64Decoder((EmailBase64DecodeMode)10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64Decoder(EmailBase64DecodeMode.Strict, -1));
        Assert.Throws<ArgumentNullException>(() => new EmailBase64DecodeResult(null!, new EmailIssueList()));
        Assert.Throws<ArgumentNullException>(() => new EmailBase64DecodeResult(Array.Empty<byte>(), null!));

        byte[] value = new byte[] { 1 };
        EmailBase64DecodeResult result = new EmailBase64DecodeResult(value, new EmailIssueList());
        value[0] = 2;
        byte[] returned = result.GetDecodedBytes();
        returned[0] = 3;
        Assert.That(result.GetDecodedBytes(), Is.EqualTo(new byte[] { 1 }));
        Assert.That(result.GetDecodedByteCount(), Is.EqualTo(1));
    }
}
