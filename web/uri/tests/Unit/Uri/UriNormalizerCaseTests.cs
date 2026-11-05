namespace uri.Tests;

public class UriNormalizerCaseTests
{
    [TestCase("", "")]
    [TestCase("http", "http")]
    [TestCase("HTTP", "http")]
    [TestCase("HtTp", "http")]
    [TestCase("A1+Foo-Bar.BAZ", "a1+foo-bar.baz")]
    public void SchemeNormalizationLowercasesAsciiLettersOnly(
        string scheme,
        string expected)
    {
        string result = UriNormalizerCore.NormalizeSchemeCase(scheme.AsSpan());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("", "")]
    [TestCase("example.com", "example.com")]
    [TestCase("Example.COM", "example.com")]
    [TestCase("A-B_C~!$&'()*+,;=", "a-b_c~!$&'()*+,;=")]
    [TestCase("ÄEXAMPLE", "Äexample")]
    public void RegisteredNameNormalizationLowercasesLiteralAsciiLettersOnly(
        string host,
        string expected)
    {
        string result = UriNormalizerCore.NormalizeRegisteredNameCase(host.AsSpan());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("%41EXAMPLE", "%41example")]
    [TestCase("%aFABC", "%aFabc")]
    [TestCase("A%2Fb%2fC", "a%2Fb%2fc")]
    [TestCase("%DE%ad", "%DE%ad")]
    public void RegisteredNameCaseNormalizationPreservesPercentTripletSpelling(
        string host,
        string expected)
    {
        string result = UriNormalizerCore.NormalizeRegisteredNameCase(host.AsSpan());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(UriHostKind.Absent, "EXAMPLE.COM")]
    [TestCase(UriHostKind.Ipv4, "192.000.002.001")]
    [TestCase(UriHostKind.Ipv6, "2001:0DB8::ABCD")]
    [TestCase(UriHostKind.IpvFuture, "VF.ABC:DEF")]
    public void NonRegisteredHostKindsRemainCharacterForCharacter(
        UriHostKind hostKind,
        string host)
    {
        string result = UriNormalizerCore.NormalizeHostCase(host.AsSpan(), hostKind);

        Assert.That(result, Is.EqualTo(host));
    }

    [Test]
    public void HostDispatcherNormalizesRegisteredNames()
    {
        string result = UriNormalizerCore.NormalizeHostCase(
            "Example.COM".AsSpan(),
            UriHostKind.RegisteredName);

        Assert.That(result, Is.EqualTo("example.com"));
    }

    [Test]
    public void NormalizationConsumesOnlyTheProvidedSpans()
    {
        const string scheme = "HTTP";
        const string host = "Example.COM";
        string paddedScheme = "before" + scheme + "after";
        string paddedHost = "before" + host + "after";

        string normalizedScheme = UriNormalizerCore.NormalizeSchemeCase(
            paddedScheme.AsSpan(6, scheme.Length));
        string normalizedHost = UriNormalizerCore.NormalizeRegisteredNameCase(
            paddedHost.AsSpan(6, host.Length));

        Assert.Multiple(() =>
        {
            Assert.That(normalizedScheme, Is.EqualTo("http"));
            Assert.That(normalizedHost, Is.EqualTo("example.com"));
        });
    }

    [TestCase("HTTP", "Example.COM")]
    [TestCase("urn", "%45XAMPLE.COM")]
    [TestCase("A1+B", "A-B_C")]
    public void CaseNormalizationIsIdempotent(string scheme, string host)
    {
        string firstScheme = UriNormalizerCore.NormalizeSchemeCase(scheme.AsSpan());
        string secondScheme = UriNormalizerCore.NormalizeSchemeCase(firstScheme.AsSpan());
        string firstHost = UriNormalizerCore.NormalizeRegisteredNameCase(host.AsSpan());
        string secondHost = UriNormalizerCore.NormalizeRegisteredNameCase(firstHost.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(secondScheme, Is.EqualTo(firstScheme));
            Assert.That(secondHost, Is.EqualTo(firstHost));
        });
    }
}
