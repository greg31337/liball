using System.Text;
using email;

namespace email.Tests;

public class EmailQuotedPrintableDecoderTests
{
    [TestCase("", "")]
    [TestCase("Hello=20World=21", "Hello World!")]
    [TestCase("one=\r\ntwo", "onetwo")]
    [TestCase("one\r\ntwo", "one\r\ntwo")]
    [TestCase("plain ASCII", "plain ASCII")]
    public void StrictDecoderAcceptsCanonicalBodyVectors(string encoded, string decoded)
    {
        EmailQuotedPrintableDecodeResult result = new EmailQuotedPrintable().Decode(
            Encoding.ASCII.GetBytes(encoded),
            EmailQuotedPrintableDecodeMode.Strict,
            1000);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo(decoded));
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasFatalError(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void HexEscapesRecoverArbitraryByteValues()
    {
        EmailQuotedPrintableDecodeResult result = new EmailQuotedPrintable().Decode(
            Encoding.ASCII.GetBytes("=00=7F=80=FF"),
            EmailQuotedPrintableDecodeMode.Strict,
            10);

        Assert.That(result.GetDecodedBytes(), Is.EqualTo(new byte[] { 0, 127, 128, 255 }));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DecodingIsIndependentOfEveryTwoChunkSplit()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("a=3Db=\r\nc= \t\r\nd=20e=09f \t");
        byte[] expected = Encoding.ASCII.GetBytes("a=bcd e\tf");
        int split = 0;
        while (split <= encoded.Length)
        {
            EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(
                EmailQuotedPrintableDecodeMode.Strict,
                1000);
            List<byte> decoded = new List<byte>();
            decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, 0, split)));
            decoded.AddRange(decoder.Feed(new ReadOnlySpan<byte>(encoded, split, encoded.Length - split)));
            decoded.AddRange(decoder.Complete());

            Assert.That(decoded.ToArray(), Is.EqualTo(expected), "split " + split);
            Assert.That(decoder.GetEncodedByteCount(), Is.EqualTo(encoded.Length));
            Assert.That(decoder.GetDecodedByteCount(), Is.EqualTo(expected.Length));
            Assert.That(decoder.GetIssues().GetCount(), Is.Zero);
            split++;
        }
    }

    [Test]
    public void TransportPaddingAndTrailingLineWhitespaceAreRemoved()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("one= \t\r\ntwo \t\r\nthree\t ");

        EmailQuotedPrintableDecodeResult result = new EmailQuotedPrintable().Decode(
            encoded,
            EmailQuotedPrintableDecodeMode.Strict,
            100);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("onetwo\r\nthree"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [TestCase("a=QZb", "a=QZb")]
    [TestCase("a=A?b", "a=A?b")]
    [TestCase("a==41b", "a=Ab")]
    public void MalformedEscapesArePreservedWithModeSpecificSeverity(string encoded, string expected)
    {
        EmailQuotedPrintableDecodeResult strict = new EmailQuotedPrintable().Decode(
            Encoding.ASCII.GetBytes(encoded),
            EmailQuotedPrintableDecodeMode.Strict,
            100);
        EmailQuotedPrintableDecodeResult tolerant = new EmailQuotedPrintable().Decode(
            Encoding.ASCII.GetBytes(encoded),
            EmailQuotedPrintableDecodeMode.Tolerant,
            100);

        Assert.That(Encoding.ASCII.GetString(strict.GetDecodedBytes()), Is.EqualTo(expected));
        Assert.That(strict.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(strict.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableInvalidEscape));
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(tolerant.GetDecodedBytes(), Is.EqualTo(strict.GetDecodedBytes()));
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(tolerant.IsSuccess(), Is.True);
    }

    [TestCase("x=", "x=")]
    [TestCase("x=A", "x=A")]
    [TestCase("x=\r", "x=\r")]
    [TestCase("x= \t", "x= \t")]
    public void TruncatedEscapesArePreservedAtCompletion(string encoded, string expected)
    {
        EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(
            EmailQuotedPrintableDecodeMode.Tolerant,
            100);
        List<byte> decoded = new List<byte>();
        decoded.AddRange(decoder.Feed(Encoding.ASCII.GetBytes(encoded)));
        decoded.AddRange(decoder.Complete());

        Assert.That(Encoding.ASCII.GetString(decoded.ToArray()), Is.EqualTo(expected));
        Assert.That(decoder.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(decoder.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableTruncatedEscape));
        Assert.That(decoder.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
    }

    [Test]
    public void LowercaseHexIsDecodedAndDiagnosedInBothModes()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("=3d=af");

        EmailQuotedPrintableDecodeResult strict = new EmailQuotedPrintable().Decode(
            encoded,
            EmailQuotedPrintableDecodeMode.Strict,
            10);
        EmailQuotedPrintableDecodeResult tolerant = new EmailQuotedPrintable().Decode(
            encoded,
            EmailQuotedPrintableDecodeMode.Tolerant,
            10);

        Assert.That(strict.GetDecodedBytes(), Is.EqualTo(new byte[] { 61, 175 }));
        Assert.That(strict.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(strict.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableLowercaseHex));
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(tolerant.GetDecodedBytes(), Is.EqualTo(strict.GetDecodedBytes()));
        Assert.That(tolerant.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
    }

    [Test]
    public void TolerantModeRecoversLfSoftBreakWhileStrictModePreservesIt()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("one=\ntwo");

        EmailQuotedPrintableDecodeResult strict = new EmailQuotedPrintable().Decode(
            encoded,
            EmailQuotedPrintableDecodeMode.Strict,
            100);
        EmailQuotedPrintableDecodeResult tolerant = new EmailQuotedPrintable().Decode(
            encoded,
            EmailQuotedPrintableDecodeMode.Tolerant,
            100);

        Assert.That(Encoding.ASCII.GetString(strict.GetDecodedBytes()), Is.EqualTo("one=\ntwo"));
        Assert.That(strict.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableNonCanonicalSoftBreak));
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(Encoding.ASCII.GetString(tolerant.GetDecodedBytes()), Is.EqualTo("onetwo"));
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
    }

    [Test]
    public void MalformedCarriageReturnSoftBreakIsPreserved()
    {
        EmailQuotedPrintableDecodeResult result = new EmailQuotedPrintable().Decode(
            Encoding.ASCII.GetBytes("one=\rtwo"),
            EmailQuotedPrintableDecodeMode.Tolerant,
            100);

        Assert.That(Encoding.ASCII.GetString(result.GetDecodedBytes()), Is.EqualTo("one=\rtwo"));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableNonCanonicalSoftBreak));
    }

    [Test]
    public void IssuePositionsTrackLinesAcrossFeedBoundaries()
    {
        EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(
            EmailQuotedPrintableDecodeMode.Strict,
            100);
        decoder.Feed(Encoding.ASCII.GetBytes("ok\r"));
        decoder.Feed(Encoding.ASCII.GetBytes("\n=Q"));
        decoder.Complete();

        EmailIssue issue = decoder.GetIssues().GetAt(0);
        Assert.That(issue.GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableInvalidEscape));
        Assert.That(issue.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(4));
        Assert.That(issue.GetSourcePosition()!.GetLineNumber(), Is.EqualTo(2));
        Assert.That(issue.GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void DecodedLimitStopsBeforeCrossingAndAccountsForRemainingInput()
    {
        byte[] encoded = Encoding.ASCII.GetBytes("ab=63def");
        EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(
            EmailQuotedPrintableDecodeMode.Tolerant,
            3);

        byte[] body = decoder.Feed(encoded);
        byte[] tail = decoder.Complete();

        Assert.That(Encoding.ASCII.GetString(body), Is.EqualTo("abc"));
        Assert.That(tail, Is.Empty);
        Assert.That(decoder.GetEncodedByteCount(), Is.EqualTo(encoded.Length));
        Assert.That(decoder.GetDecodedByteCount(), Is.EqualTo(3));
        Assert.That(decoder.HasFatalError(), Is.True);
        Assert.That(decoder.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(decoder.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.DecodedByteLimitExceeded));
        Assert.That(decoder.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Fatal));
    }

    [Test]
    public void DecoderLifecycleAndIssueSnapshotsAreExplicit()
    {
        EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(
            EmailQuotedPrintableDecodeMode.Strict,
            10);
        decoder.Feed(Encoding.ASCII.GetBytes("=Q"));
        EmailIssueList snapshot = decoder.GetIssues();
        snapshot.Clear();
        decoder.Complete();

        Assert.That(decoder.GetMode(), Is.EqualTo(EmailQuotedPrintableDecodeMode.Strict));
        Assert.That(decoder.GetMaximumDecodedBytes(), Is.EqualTo(10));
        Assert.That(decoder.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(decoder.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => decoder.Feed(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => decoder.Complete());
    }

    [Test]
    public void DecoderAndResultValidateAndDefensivelyCopyInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQuotedPrintableDecoder((EmailQuotedPrintableDecodeMode)10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQuotedPrintableDecoder(EmailQuotedPrintableDecodeMode.Strict, -1));
        Assert.Throws<ArgumentNullException>(() => new EmailQuotedPrintableDecodeResult(null!, new EmailIssueList()));
        Assert.Throws<ArgumentNullException>(() => new EmailQuotedPrintableDecodeResult(Array.Empty<byte>(), null!));

        byte[] value = new byte[] { 1 };
        EmailQuotedPrintableDecodeResult result = new EmailQuotedPrintableDecodeResult(value, new EmailIssueList());
        value[0] = 2;
        byte[] returned = result.GetDecodedBytes();
        returned[0] = 3;
        Assert.That(result.GetDecodedBytes(), Is.EqualTo(new byte[] { 1 }));
        Assert.That(result.GetDecodedByteCount(), Is.EqualTo(1));
    }

    [Test]
    public void MaximumDecodedSizeIsIdentityAndValidatesInput()
    {
        Assert.That(new EmailQuotedPrintable().GetMaximumDecodedSize(0), Is.Zero);
        Assert.That(new EmailQuotedPrintable().GetMaximumDecodedSize(123), Is.EqualTo(123));
        Assert.That(new EmailQuotedPrintable().GetMaximumDecodedSize(long.MaxValue), Is.EqualTo(long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQuotedPrintable().GetMaximumDecodedSize(-1));
    }
}
