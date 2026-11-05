using System.Text;
using email;

namespace email.Tests;

public class EmailBoundaryScannerTests
{
    [Test]
    public void RecognizesOpeningClosingAndTransportPaddedDelimiterLines()
    {
        const string source = "preamble\r\n--abc \t\r\none\r\n--abc\r\ntwo\r\n--abc--  \r\nepilogue";
        EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundary("abc");

        List<EmailBoundaryDelimiter> emitted = scanner.Feed(Bytes(source));
        EmailBoundaryScanResult result = scanner.Complete();

        Assert.That(emitted, Has.Count.EqualTo(3));
        Assert.That(result.GetDelimiterCount(), Is.EqualTo(3));
        Assert.That(result.HasClosingBoundary(), Is.True);
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(Bytes(source).Length));

        EmailBoundaryDelimiter first = result.GetDelimiterAt(0);
        EmailBoundaryDelimiter second = result.GetDelimiterAt(1);
        EmailBoundaryDelimiter closing = result.GetDelimiterAt(2);
        Assert.That(first.GetSourceRange().GetStartOffset(), Is.EqualTo(source.IndexOf("--abc \t", StringComparison.Ordinal)));
        Assert.That(first.GetSourceRange().GetLength(), Is.EqualTo(9));
        Assert.That(first.IsClosing(), Is.False);
        Assert.That(first.GetTransportPaddingByteCount(), Is.EqualTo(2));
        Assert.That(first.GetPrecedingLineEndingByteCount(), Is.EqualTo(2));
        Assert.That(first.GetLineEnding(), Is.EqualTo(EmailLineEnding.CrLf));
        Assert.That(second.IsClosing(), Is.False);
        Assert.That(closing.IsClosing(), Is.True);
        Assert.That(closing.GetTransportPaddingByteCount(), Is.EqualTo(2));
        Assert.That(closing.WasRecovered(), Is.False);
    }

    [Test]
    public void PrefixCollisionsAndNonLineStartMatchesAreIgnored()
    {
        const string source = "--abcX\r\n--abc---\r\ntext--abc\r\n--abcd\r\n--abc\r\npart\r\n--abc--\r\n";
        EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundary("abc");

        scanner.Feed(Bytes(source));
        EmailBoundaryScanResult result = scanner.Complete();

        Assert.That(result.GetDelimiterCount(), Is.EqualTo(2));
        Assert.That(result.GetDelimiterAt(0).IsClosing(), Is.False);
        Assert.That(result.GetDelimiterAt(1).IsClosing(), Is.True);
    }

    [Test]
    public void EveryTwoChunkSplitProducesTheSameDelimiterRanges()
    {
        byte[] input = Bytes("pre\r\n--split\t\r\nbody\r\n--split--\r\npost");
        EmailBoundaryScanResult expected = Scan(input);

        int split = 0;
        while (split <= input.Length)
        {
            EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundary("split");
            scanner.Feed(new ReadOnlySpan<byte>(input, 0, split));
            scanner.Feed(new ReadOnlySpan<byte>(input, split, input.Length - split));
            AssertEquivalent(scanner.Complete(), expected, "split " + split);
            split++;
        }
    }

    [Test]
    public void SingleByteFeedsPreservePartialCrLfAndClosingCandidates()
    {
        byte[] input = Bytes("--edge\r\npart\r\n--edge-- \r\n");
        EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundary("edge");
        List<EmailBoundaryDelimiter> emitted = new List<EmailBoundaryDelimiter>();

        int index = 0;
        while (index < input.Length)
        {
            emitted.AddRange(scanner.Feed(new ReadOnlySpan<byte>(input, index, 1)));
            index++;
        }

        EmailBoundaryScanResult result = scanner.Complete();
        Assert.That(emitted, Has.Count.EqualTo(2));
        Assert.That(result.GetDelimiterCount(), Is.EqualTo(2));
        Assert.That(result.GetDelimiterAt(1).IsClosing(), Is.True);
        Assert.That(result.GetDelimiterAt(1).GetTransportPaddingByteCount(), Is.EqualTo(1));
    }

    [Test]
    public void ForensicModeRecoversQuotedDelimiterLinesAndMismatchedParameterQuotes()
    {
        EmailParseOptions forensic = new EmailParseOptionsOperations().CreateForensic();
        EmailBoundaryScanner quotedLine = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("abc", forensic);
        EmailBoundaryScanner mismatchedParameter = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("\"abc", forensic);

        quotedLine.Feed(Bytes("--\"abc\"\r\npart\r\n--\"abc\"--\r\n"));
        mismatchedParameter.Feed(Bytes("--abc--\r\n"));
        EmailBoundaryScanResult quotedResult = quotedLine.Complete();
        EmailBoundaryScanResult parameterResult = mismatchedParameter.Complete();

        Assert.That(quotedResult.GetDelimiterCount(), Is.EqualTo(2));
        Assert.That(quotedResult.GetDelimiterAt(0).WasRecovered(), Is.True);
        Assert.That(quotedResult.GetIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
        Assert.That(parameterResult.GetDelimiterCount(), Is.EqualTo(1));
        Assert.That(parameterResult.GetDelimiterAt(0).WasRecovered(), Is.True);
        Assert.That(parameterResult.GetIssues().ContainsCode(EmailIssueCode.RecoveryApplied), Is.True);

        EmailBoundaryScanner tolerant = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("abc", new EmailParseOptionsOperations().CreateConformingTolerant());
        tolerant.Feed(Bytes("--\"abc\"--\r\n"));
        Assert.That(tolerant.Complete().GetDelimiterCount(), Is.Zero);
    }

    [Test]
    public void BareLineEndingsRequireRecoveryPolicyAndAreMarkedRecovered()
    {
        byte[] input = Bytes("preamble\n--abc--\n");
        EmailBoundaryScanner strict = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("abc", new EmailParseOptionsOperations().CreateStrict());
        EmailBoundaryScanner forensic = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("abc", new EmailParseOptionsOperations().CreateForensic());

        strict.Feed(input);
        forensic.Feed(input);
        EmailBoundaryScanResult strictResult = strict.Complete();
        EmailBoundaryScanResult forensicResult = forensic.Complete();

        Assert.That(strictResult.HasClosingBoundary(), Is.False);
        Assert.That(strictResult.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
        Assert.That(forensicResult.HasClosingBoundary(), Is.True);
        Assert.That(forensicResult.GetDelimiterAt(0).GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(forensicResult.GetDelimiterAt(0).WasRecovered(), Is.True);
    }

    [Test]
    public void MissingClosureReturnsDeterministicPartialDelimitersAndIssue()
    {
        EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundary("abc");
        scanner.Feed(Bytes("preamble\r\n--abc\r\nunfinished"));

        EmailBoundaryScanResult result = scanner.Complete();
        List<EmailBoundaryDelimiter> returned = result.GetDelimiters();
        returned.Clear();
        EmailIssueList issues = result.GetIssues();
        issues.Clear();

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetDelimiterCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
    }

    [Test]
    public void BoundaryAndLineLimitsAndLifecycleAreValidated()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxLineBytes(4);
        options.SetLimits(limits);
        EmailBoundaryScanner limited = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("abc", options);
        EmailBoundaryScanner completed = new EmailBoundaryScannerOperations().CreateWithBoundary("abc");
        completed.Feed(Bytes("--abc--"));
        completed.Complete();

        Assert.Throws<EmailLimitException>(() => limited.Feed(Bytes("12345")));
        Assert.Throws<ArgumentNullException>(() => new EmailBoundaryScannerOperations().CreateWithBoundary(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("abc", null!));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryScannerOperations().CreateWithBoundary(""));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryScannerOperations().CreateWithBoundary(new string('a', 71)));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryScannerOperations().CreateWithBoundary("bad\rvalue"));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryScannerOperations().CreateWithBoundary("bad@value"));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryScannerOperations().CreateWithBoundary("bad "));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions("\"mismatch", new EmailParseOptionsOperations().CreateConformingTolerant()));
        Assert.Throws<InvalidOperationException>(() => completed.Feed(new byte[0]));
        Assert.Throws<InvalidOperationException>(() => completed.Complete());
    }

    static EmailBoundaryScanResult Scan(byte[] input)
    {
        EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundary("split");
        scanner.Feed(input);
        return scanner.Complete();
    }

    static void AssertEquivalent(EmailBoundaryScanResult actual, EmailBoundaryScanResult expected, string label)
    {
        Assert.That(actual.GetSourceByteCount(), Is.EqualTo(expected.GetSourceByteCount()), label);
        Assert.That(actual.HasClosingBoundary(), Is.EqualTo(expected.HasClosingBoundary()), label);
        Assert.That(actual.GetDelimiterCount(), Is.EqualTo(expected.GetDelimiterCount()), label);
        int index = 0;
        while (index < expected.GetDelimiterCount())
        {
            EmailBoundaryDelimiter left = actual.GetDelimiterAt(index);
            EmailBoundaryDelimiter right = expected.GetDelimiterAt(index);
            Assert.That(left.GetSourceRange().EqualsRange(right.GetSourceRange()), Is.True, label + " delimiter " + index);
            Assert.That(left.IsClosing(), Is.EqualTo(right.IsClosing()), label + " delimiter " + index);
            Assert.That(left.GetTransportPaddingByteCount(), Is.EqualTo(right.GetTransportPaddingByteCount()), label + " delimiter " + index);
            index++;
        }
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
