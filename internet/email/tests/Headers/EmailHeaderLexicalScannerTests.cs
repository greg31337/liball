using System.Text;
using email;

namespace email.Tests;

public class EmailHeaderLexicalScannerTests
{
    [Test]
    public void ScansAtomsDotAtomsCfwsAndSpecials()
    {
        EmailHeaderLexicalResult result = Scan("alpha beta.example@example.com");

        Assert.That(result.GetTokenCount(), Is.EqualTo(5));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Atom));
        Assert.That(Text(result.GetToken(0).GetValueBytes()), Is.EqualTo("alpha"));
        Assert.That(result.GetToken(1).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Cfws));
        Assert.That(result.GetToken(1).GetValueBytes(), Is.EqualTo(new byte[] { 32 }));
        Assert.That(result.GetToken(2).GetKind(), Is.EqualTo(EmailHeaderTokenKind.DotAtom));
        Assert.That(Text(result.GetToken(2).GetValueBytes()), Is.EqualTo("beta.example"));
        Assert.That(result.GetToken(3).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Special));
        Assert.That(Text(result.GetToken(3).GetRawBytes()), Is.EqualTo("@"));
        Assert.That(result.GetToken(4).GetKind(), Is.EqualTo(EmailHeaderTokenKind.DotAtom));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(Encoding.ASCII.GetByteCount("alpha beta.example@example.com")));
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void AcceptsEveryAtextCharacterAsOneAtom()
    {
        EmailHeaderLexicalResult result = Scan("AZaz09!#$%&'*+-/=?^_`{|}~");

        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Atom));
        Assert.That(result.GetToken(0).IsMalformed(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EmitsEachNonDelimitedRfcSpecialAsItsOwnToken()
    {
        EmailHeaderLexicalResult result = Scan(")<>,:;@\\.]");

        Assert.That(result.GetTokenCount(), Is.EqualTo(10));
        int index = 0;
        while (index < result.GetTokenCount())
        {
            Assert.That(result.GetToken(index).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Special));
            Assert.That(result.GetToken(index).GetByteCount(), Is.EqualTo(1));
            index++;
        }

        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void CombinesNestedCommentsAndFoldingWhiteSpaceIntoOneCfwsToken()
    {
        string input = "(outer (inner\\) text))\r\n\t(next)name";

        EmailHeaderLexicalResult result = Scan(input);

        Assert.That(result.GetTokenCount(), Is.EqualTo(2));
        EmailHeaderToken cfws = result.GetToken(0);
        Assert.That(cfws.GetKind(), Is.EqualTo(EmailHeaderTokenKind.Cfws));
        Assert.That(Text(cfws.GetRawBytes()), Is.EqualTo("(outer (inner\\) text))\r\n\t(next)"));
        Assert.That(cfws.GetValueBytes(), Is.EqualTo(new byte[] { 32 }));
        Assert.That(cfws.GetMaximumCommentDepth(), Is.EqualTo(2));
        Assert.That(cfws.GetQuotedPairCount(), Is.EqualTo(1));
        Assert.That(cfws.IsObsolete(), Is.False);
        Assert.That(result.GetToken(1).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Atom));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DecodesQuotedPairsAndUnfoldsQuotedStrings()
    {
        EmailHeaderLexicalResult result = Scan("\"a\\\"b\\\\c\r\n d\"");

        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        EmailHeaderToken token = result.GetToken(0);
        Assert.That(token.GetKind(), Is.EqualTo(EmailHeaderTokenKind.QuotedString));
        Assert.That(Text(token.GetValueBytes()), Is.EqualTo("a\"b\\c d"));
        Assert.That(token.GetQuotedPairCount(), Is.EqualTo(2));
        Assert.That(token.IsMalformed(), Is.False);
        Assert.That(token.IsObsolete(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DecodesDomainLiteralsAndReportsTheirObsoleteQuotedPairs()
    {
        string input = "[IPv6:2001\\:db8\r\n \t::1]";

        EmailHeaderLexicalResult tolerant = Scan(input);
        EmailHeaderLexicalResult strict = new EmailHeaderLexicalScanner().ScanWithInputAndMode(Encoding.ASCII.GetBytes(input), EmailHeaderLexicalMode.Strict);

        Assert.That(tolerant.GetTokenCount(), Is.EqualTo(1));
        EmailHeaderToken token = tolerant.GetToken(0);
        Assert.That(token.GetKind(), Is.EqualTo(EmailHeaderTokenKind.DomainLiteral));
        Assert.That(Text(token.GetValueBytes()), Is.EqualTo("IPv6:2001:db8 \t::1"));
        Assert.That(token.GetQuotedPairCount(), Is.EqualTo(1));
        Assert.That(token.IsObsolete(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalObsolete), Is.True);
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void RecognizesObsoleteControlTextWithModeDependentSeverity()
    {
        byte[] input = new byte[] { 40, 1, 41 };

        EmailHeaderLexicalResult tolerant = new EmailHeaderLexicalScanner().ScanWithInputAndMode(input, EmailHeaderLexicalMode.Tolerant);
        EmailHeaderLexicalResult strict = new EmailHeaderLexicalScanner().ScanWithInputAndMode(input, EmailHeaderLexicalMode.Strict);

        Assert.That(tolerant.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Cfws));
        Assert.That(tolerant.GetToken(0).IsObsolete(), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
    }

    [Test]
    public void DecodesObsoleteQuotedPairsWhileRetainingTheirDiagnosis()
    {
        byte[] input = new byte[] { 34, 97, 92, 1, 98, 34 };

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndMode(input, EmailHeaderLexicalMode.Tolerant);

        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        Assert.That(result.GetToken(0).GetValueBytes(), Is.EqualTo(new byte[] { 97, 1, 98 }));
        Assert.That(result.GetToken(0).GetQuotedPairCount(), Is.EqualTo(1));
        Assert.That(result.GetToken(0).IsObsolete(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalObsolete), Is.True);
    }

    [Test]
    public void DistinguishesStandaloneObsoleteControlsFromInvalidBytes()
    {
        byte[] input = new byte[] { 1, 127, 0, 13, 10, 200 };

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndMode(input, EmailHeaderLexicalMode.Tolerant);

        Assert.That(result.GetTokenCount(), Is.EqualTo(6));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Obsolete));
        Assert.That(result.GetToken(1).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Obsolete));
        Assert.That(result.GetToken(2).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(result.GetToken(3).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(result.GetToken(4).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(result.GetToken(5).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalObsolete), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void ReportsMalformedDotAtomsWithoutDiscardingTheirSource()
    {
        EmailHeaderLexicalResult result = Scan("a..b a. .a");

        Assert.That(result.GetTokenCount(), Is.EqualTo(5));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(Text(result.GetToken(0).GetRawBytes()), Is.EqualTo("a..b"));
        Assert.That(result.GetToken(2).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(Text(result.GetToken(2).GetRawBytes()), Is.EqualTo("a."));
        Assert.That(result.GetToken(4).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Invalid));
        Assert.That(Text(result.GetToken(4).GetRawBytes()), Is.EqualTo(".a"));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(3));
        Assert.That(result.GetToken(4).IsMalformed(), Is.True);
    }

    [Test]
    public void ReportsObsoleteMultipleFoldsButAcceptsCanonicalFws()
    {
        EmailHeaderLexicalResult canonical = Scan(" \r\n\t");
        EmailHeaderLexicalResult obsolete = Scan(" \r\n \r\n\t");

        Assert.That(canonical.GetTokenCount(), Is.EqualTo(1));
        Assert.That(canonical.GetIssues().GetCount(), Is.Zero);
        Assert.That(obsolete.GetTokenCount(), Is.EqualTo(1));
        Assert.That(obsolete.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Cfws));
        Assert.That(obsolete.GetToken(0).IsObsolete(), Is.True);
        Assert.That(obsolete.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalObsolete), Is.True);
    }

    [TestCase("(unfinished", EmailHeaderTokenKind.Cfws)]
    [TestCase("\"unfinished", EmailHeaderTokenKind.QuotedString)]
    [TestCase("[unfinished", EmailHeaderTokenKind.DomainLiteral)]
    public void RecoversUnterminatedDelimitedTokens(string input, EmailHeaderTokenKind expectedKind)
    {
        EmailHeaderLexicalResult result = Scan(input);

        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(expectedKind));
        Assert.That(result.GetToken(0).IsMalformed(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalMalformed), Is.True);
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void TracksInitialAndFoldedSourcePositions()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        EmailSourcePosition initial = new EmailSourcePosition(100, 10, 5);

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptionsAndInitialPosition(Encoding.ASCII.GetBytes("atom\r\n folded"), options, initial);

        Assert.That(result.GetToken(0).GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(result.GetToken(0).GetSourcePosition().GetLineNumber(), Is.EqualTo(10));
        Assert.That(result.GetToken(0).GetSourcePosition().GetColumnNumber(), Is.EqualTo(5));
        Assert.That(result.GetToken(1).GetSourcePosition().GetByteOffset(), Is.EqualTo(104));
        Assert.That(result.GetToken(2).GetSourcePosition().GetByteOffset(), Is.EqualTo(107));
        Assert.That(result.GetToken(2).GetSourcePosition().GetLineNumber(), Is.EqualTo(11));
        Assert.That(result.GetToken(2).GetSourcePosition().GetColumnNumber(), Is.EqualTo(2));
    }

    [Test]
    public void StopsBeforeAByteThatWouldExceedTheTokenCountLimit()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMaximumTokens(2);

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptions(Encoding.ASCII.GetBytes("a b"), options);

        Assert.That(result.GetTokenCount(), Is.EqualTo(2));
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(2));
        Assert.That(Text(result.GetRemainingBytes()), Is.EqualTo("b"));
        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalTokenLimitExceeded), Is.True);
    }

    [Test]
    public void RetainsOnlyThePrefixWithinTheTokenByteLimit()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMaximumTokenBytes(4);

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptions(Encoding.ASCII.GetBytes("abcdef"), options);

        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetToken(0).GetRawBytes()), Is.EqualTo("abcd"));
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(4));
        Assert.That(Text(result.GetRemainingBytes()), Is.EqualTo("ef"));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalTokenByteLimitExceeded), Is.True);
    }

    [Test]
    public void StopsBeforeACommentExceedsItsNestingLimit()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMaximumCommentDepth(2);

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptions(Encoding.ASCII.GetBytes("(a(b(c)d)e)"), options);

        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetToken(0).GetRawBytes()), Is.EqualTo("(a(b"));
        Assert.That(result.GetToken(0).GetMaximumCommentDepth(), Is.EqualTo(2));
        Assert.That(Text(result.GetRemainingBytes()), Is.EqualTo("(c)d)e)"));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalCommentDepthExceeded), Is.True);
    }

    [Test]
    public void ResultsAndTokensDefensivelySnapshotByteCollections()
    {
        byte[] input = Encoding.ASCII.GetBytes("abc");
        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndMode(input, EmailHeaderLexicalMode.Tolerant);
        input[0] = 120;
        byte[] raw = result.GetToken(0).GetRawBytes();
        raw[0] = 121;
        List<EmailHeaderToken> tokens = result.GetTokens();
        tokens.Clear();
        EmailIssueList issues = result.GetIssues();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HeaderMalformed, "external"));

        Assert.That(Text(result.GetToken(0).GetRawBytes()), Is.EqualTo("abc"));
        Assert.That(result.GetTokenCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EmptyInputProducesACompleteEmptyResult()
    {
        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndMode(Array.Empty<byte>(), EmailHeaderLexicalMode.Strict);

        Assert.That(result.GetTokenCount(), Is.Zero);
        Assert.That(result.GetConsumedByteCount(), Is.Zero);
        Assert.That(result.GetRemainingBytes(), Is.Empty);
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void InternationalizedModeExtendsRfcTextTokensWithValidUtf8()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMode(EmailHeaderLexicalMode.Strict);
        options.SetAllowInternationalized(true);

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptions(Encoding.UTF8.GetBytes("Jöhn (Dœ) \"müller\" [例え]"), options);

        Assert.That(result.GetTokenCount(), Is.EqualTo(5));
        Assert.That(result.GetToken(0).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Atom));
        Assert.That(result.GetToken(0).IsInternationalized(), Is.True);
        Assert.That(result.GetToken(1).GetKind(), Is.EqualTo(EmailHeaderTokenKind.Cfws));
        Assert.That(result.GetToken(1).IsInternationalized(), Is.True);
        Assert.That(result.GetToken(2).GetKind(), Is.EqualTo(EmailHeaderTokenKind.QuotedString));
        Assert.That(result.GetToken(2).IsInternationalized(), Is.True);
        Assert.That(result.GetToken(4).GetKind(), Is.EqualTo(EmailHeaderTokenKind.DomainLiteral));
        Assert.That(result.GetToken(4).IsInternationalized(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void InternationalizedModeDiagnosesTheFirstMalformedUtf8Byte()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMode(EmailHeaderLexicalMode.Strict);
        options.SetAllowInternationalized(true);

        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptions(new byte[] { 97, 195, 64, 98 }, options);

        Assert.That(result.GetToken(0).IsMalformed(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalMalformed), Is.True);
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(1));
        Assert.That(result.IsSuccess(), Is.False);
    }

    [Test]
    public void OptionsValidateLimitsAndCloneIndependently()
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMode(EmailHeaderLexicalMode.Strict);
        options.SetMaximumTokens(3);
        options.SetMaximumTokenBytes(7);
        options.SetMaximumCommentDepth(2);
        options.SetAllowInternationalized(true);
        EmailHeaderLexicalOptions clone = options.Clone();
        clone.SetMaximumTokens(4);

        Assert.That(options.GetMode(), Is.EqualTo(EmailHeaderLexicalMode.Strict));
        Assert.That(options.GetMaximumTokens(), Is.EqualTo(3));
        Assert.That(clone.GetMaximumTokens(), Is.EqualTo(4));
        Assert.That(clone.GetAllowInternationalized(), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumTokens(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumTokenBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumCommentDepth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailHeaderLexicalMode)99));
    }

    static EmailHeaderLexicalResult Scan(string input)
    {
        return new EmailHeaderLexicalScanner().ScanWithInputAndMode(Encoding.ASCII.GetBytes(input), EmailHeaderLexicalMode.Tolerant);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
