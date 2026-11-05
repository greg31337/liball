using System.Net;
using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetStandardsEdgeTests
{
    [TestCase("AZaz09-._~", "AZaz09-._~")]
    [TestCase("é/東京?😀", "%C3%A9%2F%E6%9D%B1%E4%BA%AC%3F%F0%9F%98%80")]
    [TestCase("a b+c=d&e", "a%20b%2Bc%3Dd%26e")]
    public void Rfc3986ComponentVectorsRoundTripUtf8Values(string source, string expected)
    {
        string encoded = Fake.Create(8901).Internet().EncodeUriComponent(source);

        Assert.That(encoded, Is.EqualTo(expected));
        Assert.That(Uri.UnescapeDataString(encoded), Is.EqualTo(source));
        Assert.That(encoded, Does.Match("^(?:[A-Za-z0-9._~-]|%[0-9A-F]{2})*$"));
    }

    [Test]
    public void DefaultInternetTargetsRemainInsideReservedDocumentationNamespaces()
    {
        InternetProvider internet = Fake.Create(8902).Internet();

        int index = 0;
        while (index < 256)
        {
            string domain = internet.DomainName();
            string host = internet.HostName();
            string email = internet.Email();
            Uri url = internet.Uri();
            IPAddress ipv4 = internet.Ipv4Address();
            IPAddress ipv6 = internet.Ipv6Address();

            Assert.Multiple(() =>
            {
                Assert.That(IsExampleTree(domain), Is.True, domain);
                Assert.That(IsExampleTree(host), Is.True, host);
                Assert.That(IsExampleTree(email.Substring(email.LastIndexOf('@') + 1)), Is.True, email);
                Assert.That(IsExampleTree(url.Host), Is.True, url.ToString());
                Assert.That(IsIpv4Documentation(ipv4), Is.True, ipv4.ToString());
                Assert.That(IsIpv6Documentation(ipv6), Is.True, ipv6.ToString());
            });
            index++;
        }
    }

    [Test]
    public void InternetProtocolMaximumLengthsAcceptTheBoundaryAndRejectTheNextValue()
    {
        InternetProvider internet = Fake.Create(8903).Internet();
        string maximumDomain = string.Join(
            ".",
            new string('a', 63),
            new string('b', 63),
            new string('c', 63),
            new string('d', 61));
        string overlongDomain = maximumDomain + "e";
        string maximumIpv6 = "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff";
        FakeOptions exactEncodingOptions = FakeOptions.Create(8903);
        exactEncodingOptions.MaximumOutputSize = 12;
        FakeOptions shortEncodingOptions = FakeOptions.Create(8903);
        shortEncodingOptions.MaximumOutputSize = 11;

        Assert.Multiple(() =>
        {
            Assert.That(internet.DomainWordFromLength(63), Has.Length.EqualTo(63));
            Assert.That(() => internet.DomainWordFromLength(64),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("length"));
            Assert.That(maximumDomain, Has.Length.EqualTo(253));
            Assert.That(internet.ToAsciiDomain(maximumDomain), Is.EqualTo(maximumDomain));
            Assert.That(internet.ToAsciiDomain(maximumDomain + "."), Has.Length.EqualTo(254));
            Assert.That(() => internet.ToAsciiDomain(overlongDomain),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("domainName"));
            Assert.That(internet.EmailLocalPartFromKind(EmailLocalPartCase.MaximumLength), Has.Length.EqualTo(64));
            Assert.That(internet.CanonicalIpv6(maximumIpv6), Is.EqualTo(maximumIpv6).And.Length.EqualTo(39));
            Assert.That(Fake.Create(exactEncodingOptions).Internet().EncodeUriComponent("😀"), Has.Length.EqualTo(12));
            Assert.That(() => Fake.Create(shortEncodingOptions).Internet().EncodeUriComponent("😀"),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("value"));
        });
    }

    [TestCase("é.example", "xn--9ca.example", "é.example")]
    [TestCase("é.example", "xn--9ca.example", "é.example")]
    [TestCase("例え.テスト", "xn--r8jz45g.xn--zckzah", "例え.テスト")]
    [TestCase("BÜCHER。EXAMPLE.", "xn--bcher-kva.example.", "bücher.example.")]
    public void IdnVectorsCanonicalizeAndRoundTrip(
        string unicode,
        string expectedAscii,
        string expectedUnicode)
    {
        InternetProvider internet = Fake.Create(8904).Internet();
        string ascii = internet.ToAsciiDomain(unicode);
        string roundTrip = internet.ToUnicodeDomain(ascii);

        Assert.That(ascii, Is.EqualTo(expectedAscii));
        Assert.That(roundTrip, Is.EqualTo(expectedUnicode));
        Assert.That(internet.ToAsciiDomain(roundTrip), Is.EqualTo(expectedAscii));
        Assert.That(roundTrip, Is.EqualTo(roundTrip.Normalize(NormalizationForm.FormC)));
    }

    [TestCase("2001:0db8:0000:0000:0000:0000:0002:0001", "2001:db8::2:1")]
    [TestCase("2001:db8:0:1:0:1:0:1", "2001:db8:0:1:0:1:0:1")]
    [TestCase("2001:db8:0:0:1:0:0:0", "2001:db8:0:0:1::")]
    [TestCase("2001:0:0:1:0:0:0:1", "2001:0:0:1::1")]
    [TestCase("0:0:0:0:0:ffff:c000:0280", "::ffff:192.0.2.128")]
    [TestCase("0:0:0:0:0:0:0:0", "::")]
    public void Rfc5952VectorsUseCanonicalIpv6Compression(string source, string expected)
    {
        InternetProvider internet = Fake.Create(8905).Internet();
        string canonical = internet.CanonicalIpv6(source);

        Assert.That(canonical, Is.EqualTo(expected));
        Assert.That(internet.CanonicalIpv6(canonical), Is.EqualTo(canonical));
        Assert.That(IPAddress.Parse(canonical), Is.EqualTo(IPAddress.Parse(source)));
    }

    [TestCase("2001:db8:::1")]
    [TestCase("2001:db8::1::2")]
    [TestCase("2001:db8:0:0:0:0:0:0:1")]
    [TestCase("2001:db8::gggg")]
    [TestCase(":2001:db8::1")]
    [TestCase("2001:db8::1:")]
    [TestCase(" 2001:db8::1")]
    [TestCase("2001:db8::1 ")]
    public void MalformedIpv6TextIsRejected(string value)
    {
        Assert.That(() => Fake.Create(8906).Internet().CanonicalIpv6(value), Throws.Exception);
    }

    [TestCase(".")]
    [TestCase("example.com/path")]
    [TestCase("example%2Ecom")]
    [TestCase("exa@mple.com")]
    [TestCase("example.com:443")]
    [TestCase("example\u0000.com")]
    public void MalformedDomainTextIsRejected(string value)
    {
        InternetProvider internet = Fake.Create(8907).Internet();

        Assert.That(() => internet.ToAsciiDomain(value), Throws.Exception);
        Assert.That(() => internet.ToUnicodeDomain(value), Throws.Exception);
    }

    [Test]
    public void MalformedUnicodeCannotEnterUriOrIdnEncoding()
    {
        InternetProvider internet = Fake.Create(8908).Internet();

        Assert.That(() => internet.EncodeUriComponent("high\ud800"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("value"));
        Assert.That(() => internet.EncodeUriComponent("low\udc00"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("value"));
        Assert.That(() => internet.ToAsciiDomain("high\ud800.example"), Throws.Exception);
        Assert.That(() => internet.ToUnicodeDomain("xn--.example"), Throws.Exception);
    }

    static bool IsExampleTree(string value)
    {
        return value == "example.com" ||
            value == "example.net" ||
            value == "example.org" ||
            value.EndsWith(".example.com", StringComparison.Ordinal) ||
            value.EndsWith(".example.net", StringComparison.Ordinal) ||
            value.EndsWith(".example.org", StringComparison.Ordinal);
    }

    static bool IsIpv4Documentation(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2 ||
            bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100 ||
            bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113;
    }

    static bool IsIpv6Documentation(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 16 &&
            bytes[0] == 0x20 &&
            bytes[1] == 0x01 &&
            bytes[2] == 0x0d &&
            bytes[3] == 0xb8;
    }
}
