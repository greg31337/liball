namespace cookies.Tests;

public class CookieHostCanonicalizerTests
{
    [TestCase("https://EXAMPLE.TEST/path", "example.test")]
    [TestCase("https://LOCALHOST:8443/path", "localhost")]
    [TestCase("https://Sub.Example.TEST./path", "sub.example.test")]
    public void CanonicalizerLowercasesExtractedAsciiDnsHosts(string uriText, string expected)
    {
        CookieCanonicalHostResult result = Canonicalize(uriText);

        AssertDnsSuccess(result, expected);
    }

    [TestCase("https://bücher.example/", "xn--bcher-kva.example")]
    [TestCase("https://例え.テスト/", "xn--r8jz45g.xn--zckzah")]
    [TestCase("https://παράδειγμα.δοκιμή/", "xn--hxajbheg2az3al.xn--jxalpdlp")]
    public void CanonicalizerConvertsUnicodeLabelsToAscii(string uriText, string expected)
    {
        CookieCanonicalHostResult result = Canonicalize(uriText);

        AssertDnsSuccess(result, expected);
    }

    [TestCase("XN--BCHER-KVA.Example", "xn--bcher-kva.example")]
    [TestCase("xn--r8jz45g.xn--zckzah", "xn--r8jz45g.xn--zckzah")]
    public void CanonicalizerAcceptsValidALabelsAndNormalizesAsciiCase(string host, string expected)
    {
        CookieCanonicalHostResult result = CookieHostCanonicalizer.Canonicalize(DnsHost(host));

        AssertDnsSuccess(result, expected);
    }

    [TestCase("xn--a")]
    [TestCase("xn--0.example")]
    [TestCase("ab--reserved.example")]
    public void CanonicalizerRejectsFakeOrInvalidALabels(string host)
    {
        CookieCanonicalHostResult result = CookieHostCanonicalizer.Canonicalize(DnsHost(host));

        AssertFailure(result, CookieErrorCode.InvalidIdna);
    }

    [TestCase("bad_name.example")]
    [TestCase("-leading.example")]
    [TestCase("trailing-.example")]
    [TestCase("xn--invalid-punycode-")]
    [TestCase("empty..label")]
    [TestCase("terminal.example.")]
    public void CanonicalizerRejectsMalformedDnsStructure(string host)
    {
        CookieCanonicalHostResult result = CookieHostCanonicalizer.Canonicalize(DnsHost(host));

        AssertFailure(result, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void CanonicalizerEnforcesTheSixtyThreeOctetLabelLimit()
    {
        string boundary = new string('a', 63);
        string over = new string('a', 64);

        CookieCanonicalHostResult boundaryResult = CookieHostCanonicalizer.Canonicalize(
            DnsHost(boundary + ".test"));
        CookieCanonicalHostResult overResult = CookieHostCanonicalizer.Canonicalize(
            DnsHost(over + ".test"));

        AssertDnsSuccess(boundaryResult, boundary + ".test");
        AssertFailure(overResult, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void CanonicalizerAppliesLabelLimitsAfterUnicodeConversion()
    {
        string boundary = new string('é', 57);
        string over = new string('é', 58);

        CookieCanonicalHostResult boundaryResult = CookieHostCanonicalizer.Canonicalize(
            DnsHost(boundary + ".test"));
        CookieCanonicalHostResult overResult = CookieHostCanonicalizer.Canonicalize(
            DnsHost(over + ".test"));

        Assert.That(boundaryResult.Success, Is.True);
        Assert.That(boundaryResult.Host.Split('.')[0], Has.Length.EqualTo(63));
        AssertFailure(overResult, CookieErrorCode.InvalidIdna);
    }

    [Test]
    public void CanonicalizerEnforcesTheTwoHundredFiftyThreeOctetDomainLimit()
    {
        string label63 = new string('a', 63);
        string label61 = new string('b', 61);
        string boundary = label63 + "." + label63 + "." + label63 + "." + label61;
        string over = boundary + "a";

        CookieCanonicalHostResult boundaryResult = CookieHostCanonicalizer.Canonicalize(DnsHost(boundary));
        CookieCanonicalHostResult overResult = CookieHostCanonicalizer.Canonicalize(DnsHost(over));

        Assert.That(boundary.Length, Is.EqualTo(253));
        Assert.That(over.Length, Is.EqualTo(254));
        AssertDnsSuccess(boundaryResult, boundary);
        AssertFailure(overResult, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void InvalidUnicodeIsReturnedAsAnIdnaFailureInsteadOfEscaping()
    {
        string invalid = "bad" + (char)0xd800 + ".example";

        CookieCanonicalHostResult result = CookieHostCanonicalizer.Canonicalize(DnsHost(invalid));

        AssertFailure(result, CookieErrorCode.InvalidIdna);
    }

    [TestCase("https://192.0.2.1:8443/path", "192.0.2.1", true, false)]
    [TestCase("https://[2001:DB8::ABCD]:8443/path", "2001:db8::abcd", false, true)]
    public void CanonicalizerPreservesIpClassificationWithoutIdna(
        string uriText,
        string expected,
        bool isIpv4,
        bool isIpv6)
    {
        CookieUriHostResult extracted = CookieUriHostExtractor.Extract(new Uri(uriText));

        CookieCanonicalHostResult result = CookieHostCanonicalizer.Canonicalize(extracted);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Host, Is.EqualTo(expected));
        Assert.That(result.IsDnsName, Is.False);
        Assert.That(result.IsIpv4, Is.EqualTo(isIpv4));
        Assert.That(result.IsIpv6, Is.EqualTo(isIpv6));
    }

    [Test]
    public void ExtractorFailuresArePropagatedDeterministically()
    {
        CookieUriHostResult unsupported = CookieUriHostExtractor.Extract(new Uri("ftp://example.test/"));
        CookieUriHostResult invalid = CookieUriHostExtractor.Extract(null!);

        CookieCanonicalHostResult unsupportedResult = CookieHostCanonicalizer.Canonicalize(unsupported);
        CookieCanonicalHostResult invalidResult = CookieHostCanonicalizer.Canonicalize(invalid);
        CookieCanonicalHostResult nullResult = CookieHostCanonicalizer.Canonicalize(null!);

        AssertFailure(unsupportedResult, CookieErrorCode.UnsupportedScheme);
        AssertFailure(invalidResult, CookieErrorCode.InvalidHost);
        AssertFailure(nullResult, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void MissingOrContradictoryHostClassificationIsRejected()
    {
        CookieUriHostResult missing = new CookieUriHostResult
        {
            Success = true,
            Host = "example.test"
        };
        CookieUriHostResult contradictory = new CookieUriHostResult
        {
            Success = true,
            Host = "192.0.2.1",
            IsDnsName = true,
            IsIpv4 = true
        };

        AssertFailure(
            CookieHostCanonicalizer.Canonicalize(missing),
            CookieErrorCode.InvalidHost);
        AssertFailure(
            CookieHostCanonicalizer.Canonicalize(contradictory),
            CookieErrorCode.InvalidHost);
    }

    static CookieCanonicalHostResult Canonicalize(string uriText)
    {
        return CookieHostCanonicalizer.Canonicalize(
            CookieUriHostExtractor.Extract(new Uri(uriText)));
    }

    static CookieUriHostResult DnsHost(string host)
    {
        return new CookieUriHostResult
        {
            Success = true,
            ErrorCode = CookieErrorCode.None,
            Host = host,
            IsDnsName = true
        };
    }

    static void AssertDnsSuccess(CookieCanonicalHostResult result, string expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Host, Is.EqualTo(expected));
        Assert.That(result.IsDnsName, Is.True);
        Assert.That(result.IsIpv4, Is.False);
        Assert.That(result.IsIpv6, Is.False);
    }

    static void AssertFailure(CookieCanonicalHostResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Host, Is.Empty);
        Assert.That(result.IsDnsName, Is.False);
        Assert.That(result.IsIpv4, Is.False);
        Assert.That(result.IsIpv6, Is.False);
    }
}
