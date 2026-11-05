namespace uri.Tests;

public class AuthorityParserTests
{
    [TestCase("", false, "", false, "", false, "")]
    [TestCase("host", false, "", false, "host", false, "")]
    [TestCase(":80", false, "", false, "", true, "80")]
    [TestCase(":", false, "", false, "", true, "")]
    [TestCase("@host", true, "", false, "host", false, "")]
    [TestCase("user@", true, "user", false, "", false, "")]
    [TestCase("user@:", true, "user", false, "", true, "")]
    [TestCase("user:pass@host:443", true, "user:pass", false, "host", true, "443")]
    [TestCase("a@b@host:80", true, "a@b", false, "host", true, "80")]
    [TestCase("user%40name@host%3Aname:80", true, "user%40name", false, "host%3Aname", true, "80")]
    [TestCase("[::1]", false, "", true, "::1", false, "")]
    [TestCase("[::1]:443", false, "", true, "::1", true, "443")]
    [TestCase("[]:", false, "", true, "", true, "")]
    [TestCase("[v1.a:b]:9", false, "", true, "v1.a:b", true, "9")]
    public void AuthorityBoundariesPreserveEveryAbsentAndEmptyState(
        string text,
        bool hasUserInfo,
        string userInfo,
        bool hostBracketed,
        string host,
        bool hasPort,
        string port)
    {
        AuthorityParseResult result = AuthorityParser.ParseBoundaries(text.AsSpan(), 12);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.HasUserInfo, Is.EqualTo(hasUserInfo), text);
            Assert.That(result.HostBracketed, Is.EqualTo(hostBracketed), text);
            Assert.That(result.HasPort, Is.EqualTo(hasPort), text);
            Assert.That(result.Diagnostic, Is.Null, text);
        });
        AssertParts(text, result, userInfo, host, port);
    }

    [Test]
    public void FinalAtSignDefinesTheUserInfoBoundary()
    {
        string text = "first@second@host:80";

        AuthorityParseResult result = AuthorityParser.ParseBoundaries(text.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasUserInfo, Is.True);
            Assert.That(result.UserInfoOffset, Is.Zero);
            Assert.That(result.UserInfoLength, Is.EqualTo(12));
            Assert.That(result.HostOffset, Is.EqualTo(13));
            Assert.That(result.HostLength, Is.EqualTo(4));
            Assert.That(result.PortOffset, Is.EqualTo(18));
            Assert.That(result.PortLength, Is.EqualTo(2));
        });
        AssertParts(text, result, "first@second", "host", "80");
    }

    [Test]
    public void EncodedAtAndColonCharactersNeverCreateBoundaries()
    {
        string text = "name%40part@host%3Avalue:007";

        AuthorityParseResult result = AuthorityParser.ParseBoundaries(text.AsSpan(), 0);

        AssertParts(text, result, "name%40part", "host%3Avalue", "007");
    }

    [Test]
    public void BracketedHostColonsNeverCreatePortBoundaries()
    {
        string text = "user@[2001:db8::1]:443";

        AuthorityParseResult result = AuthorityParser.ParseBoundaries(text.AsSpan(), 50);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HostBracketed, Is.True);
            Assert.That(result.HostOffset, Is.EqualTo(6));
            Assert.That(result.HostLength, Is.EqualTo(11));
            Assert.That(result.HasPort, Is.True);
            Assert.That(result.PortOffset, Is.EqualTo(19));
            Assert.That(result.PortLength, Is.EqualTo(3));
        });
        AssertParts(text, result, "user", "2001:db8::1", "443");
    }

    [TestCase("[::1", 24, 0)]
    [TestCase("[a[b]", 22, 1)]
    [TestCase("host]", 24, 1)]
    [TestCase("ho[st", 22, 1)]
    [TestCase("[::1]x", 25, 1)]
    [TestCase("[]extra", 22, 1)]
    [TestCase("user@[::1", 29, 0)]
    public void InvalidBracketPlacementReturnsExactHostDiagnostic(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        AuthorityParseResult result = AuthorityParser.ParseBoundaries(text.AsSpan(), 20);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidHost,
            UriComponent.Host,
            expectedOffset,
            expectedLength,
            DiagnosticFactory.InvalidHostMessage);
    }

    [TestCase("host:x", 25)]
    [TestCase("host:12x", 27)]
    [TestCase("[::1]:x", 26)]
    [TestCase("host:80:90", 27)]
    [TestCase(":-1", 21)]
    [TestCase("host:\uff11\uff12", 25)]
    public void PortAllowsOnlyAsciiDigitsButPreservesEmptyPort(
        string text,
        int expectedOffset)
    {
        AuthorityParseResult result = AuthorityParser.ParseBoundaries(text.AsSpan(), 20);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidPort,
            UriComponent.Port,
            expectedOffset,
            1,
            DiagnosticFactory.InvalidPortMessage);
    }

    [Test]
    public void DiagnosticsUseRelativeOffsetsForNegativeBaseAndSaturateOverflow()
    {
        AuthorityParseResult relative = AuthorityParser.ParseBoundaries("host:x".AsSpan(), -1);
        AuthorityParseResult saturated = AuthorityParser.ParseBoundaries(
            "host:x".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(5));
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void FailureReturnsNoPartialBoundaryOutcome()
    {
        AuthorityParseResult result = AuthorityParser.ParseBoundaries(
            "user@host:bad".AsSpan(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasUserInfo, Is.False);
            Assert.That(result.UserInfoLength, Is.Zero);
            Assert.That(result.HostBracketed, Is.False);
            Assert.That(result.HostLength, Is.Zero);
            Assert.That(result.HasPort, Is.False);
            Assert.That(result.PortLength, Is.Zero);
        });
    }

    private static void AssertParts(
        string text,
        AuthorityParseResult result,
        string userInfo,
        string host,
        string port)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                text.Substring(result.UserInfoOffset, result.UserInfoLength),
                Is.EqualTo(userInfo));
            Assert.That(
                text.Substring(result.HostOffset, result.HostLength),
                Is.EqualTo(host));
            Assert.That(
                text.Substring(result.PortOffset, result.PortLength),
                Is.EqualTo(port));
        });
    }

    private static void AssertFailure(
        AuthorityParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
