namespace uri.Tests;

public class Rfc5952CanonicalIpv6Tests
{
    [TestCase("2001:db8:0:0:1:0:0:1")]
    [TestCase("2001:0db8:0:0:1:0:0:1")]
    [TestCase("2001:db8::1:0:0:1")]
    [TestCase("2001:db8::0:1:0:0:1")]
    [TestCase("2001:0db8::1:0:0:1")]
    [TestCase("2001:db8:0:0:1::1")]
    [TestCase("2001:db8:0000:0:1::1")]
    [TestCase("2001:DB8:0:0:1::1")]
    public void IntroductionSpellingsHaveOneCanonicalRepresentation(string host)
    {
        AssertHostNormalization(host, "2001:db8::1:0:0:1");
    }

    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:0001")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:001")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:01")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:1")]
    [TestCase("0001:0020:0300:4000:0005:0060:0700:8000")]
    public void LeadingZerosAreSuppressedFromEveryField(string host)
    {
        string expected = "2001:db8:aaaa:bbbb:cccc:dddd:eeee:1";
        if (host[0] == '0')
        {
            expected = "1:20:300:4000:5:60:700:8000";
        }

        AssertHostNormalization(host, expected);
    }

    [TestCase("2001:DB8:AAAA:BBBB:CCCC:DDDD:EEEE:AAAA", "2001:db8:aaaa:bbbb:cccc:dddd:eeee:aaaa")]
    [TestCase("ABCD:EF01:2345:6789:ABCD:EF01:2345:6789", "abcd:ef01:2345:6789:abcd:ef01:2345:6789")]
    [TestCase("F:Ff:fF:FfF:ffff:aBcD:DefA:BEEF", "f:ff:ff:fff:ffff:abcd:defa:beef")]
    public void HexadecimalDigitsAreLowercase(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [TestCase("2001:db8:0:0:0:0:2:1", "2001:db8::2:1")]
    [TestCase("2001:db8:0:0:0:0:0:1", "2001:db8::1")]
    [TestCase("2001:db8::0:1", "2001:db8::1")]
    [TestCase("0:0:0:0:1:2:3:4", "::1:2:3:4")]
    [TestCase("1:2:3:4:0:0:0:0", "1:2:3:4::")]
    [TestCase("0:0:0:0:0:0:0:0", "::")]
    public void LongZeroRunsAreCompressedToTheirMaximumExtent(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [TestCase("2001:db8:0:1:1:1:1:1", "2001:db8:0:1:1:1:1:1")]
    [TestCase("2001:db8::1:1:1:1:1", "2001:db8:0:1:1:1:1:1")]
    [TestCase("0:1:2:3:4:5:6:7", "0:1:2:3:4:5:6:7")]
    [TestCase("::1:2:3:4:5:6:7", "0:1:2:3:4:5:6:7")]
    [TestCase("1:2:3:4:5:6:7:0", "1:2:3:4:5:6:7:0")]
    [TestCase("1:2:3:4:5:6:7::", "1:2:3:4:5:6:7:0")]
    public void ASingleZeroFieldIsNeverCompressed(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [TestCase("2001:0:0:1:0:0:0:1", "2001:0:0:1::1")]
    [TestCase("2001:0:0:0:1:0:0:1", "2001::1:0:0:1")]
    [TestCase("1:0:0:2:0:0:0:3", "1:0:0:2::3")]
    public void TheLongestAvailableZeroRunIsCompressed(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [TestCase("2001:db8:0:0:1:0:0:1", "2001:db8::1:0:0:1")]
    [TestCase("0:0:1:2:3:4:0:0", "::1:2:3:4:0:0")]
    [TestCase("1:0:0:2:3:4:0:0", "1::2:3:4:0:0")]
    [TestCase("0:0:1:0:0:2:0:0", "::1:0:0:2:0:0")]
    public void EqualLengthZeroRunTiesUseTheFirstRun(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [TestCase("0:0:0:0:0:FFFF:192.0.2.128", "::ffff:192.0.2.128")]
    [TestCase("::ffff:c000:0280", "::ffff:192.0.2.128")]
    [TestCase("0:0:0:0:ffff:0:c000:0280", "::ffff:0:192.0.2.128")]
    [TestCase("64:ff9b:0:0:0:0:c000:0280", "64:ff9b::192.0.2.128")]
    [TestCase("2001:db8:1:2:0:5efe:c000:0280", "2001:db8:1:2:0:5efe:192.0.2.128")]
    [TestCase("2001:db8:1:2:200:5efe:c000:0280", "2001:db8:1:2:200:5efe:192.0.2.128")]
    [TestCase("0:0:0:0:0:0:192.0.2.128", "::192.0.2.128")]
    public void RecognizedEmbeddedIpv4PrefixesUseMixedNotation(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [TestCase("2001:db8::192.0.2.128", "2001:db8::c000:280")]
    [TestCase("2001:db8:0:0:0:0:c000:0280", "2001:db8::c000:280")]
    [TestCase("0:0:0:0:0:0:0:0", "::")]
    [TestCase("0:0:0:0:0:0:0:1", "::1")]
    public void OtherAddressesUseUnambiguousHexadecimalNotation(
        string host,
        string expected)
    {
        AssertHostNormalization(host, expected);
    }

    [Test]
    public void SyntaxComparisonRecognizesAlternateIpv6Spellings()
    {
        string[] forms = new string[]
        {
            "x://[2001:db8:0:0:1:0:0:1]/",
            "x://[2001:0db8::1:0:0:1]/",
            "x://[2001:DB8:0:0:1::1]/",
            "x://[2001:db8::1:0:0:1]/"
        };
        IUriComparer comparer = new UriComparer();

        for (int index = 0; index < forms.Length; index += 1)
        {
            UriComparisonResult result = comparer.CompareSyntax(
                forms[index],
                forms[0],
                new UriComparisonOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, forms[index]);
                Assert.That(result.Equal, Is.True, forms[index]);
                Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None), forms[index]);
            });
        }
    }

    [TestCase("2001:0DB8:0000:0000:0000:0000:0000:0001")]
    [TestCase("2001:db8:0:0:1:0:0:1")]
    [TestCase("2001:0:0:1:0:0:0:1")]
    [TestCase("2001:db8::1:1:1:1:1")]
    [TestCase("0:0:0:0:0:FFFF:192.0.2.128")]
    [TestCase("64:ff9b:0:0:0:0:c000:0280")]
    [TestCase("2001:db8::192.0.2.128")]
    [TestCase("::")]
    public void CanonicalIpv6UriNormalizationIsIdempotent(string host)
    {
        IUriNormalizer normalizer = new UriNormalizer();
        string input = "x://[" + host + "]:8080/path?query#fragment";

        string once = normalizer.NormalizeSyntax(
            input,
            new UriNormalizationOptions());
        string twice = normalizer.NormalizeSyntax(
            once,
            new UriNormalizationOptions());

        Assert.That(twice, Is.EqualTo(once), host);
    }

    private static void AssertHostNormalization(string host, string expected)
    {
        IUriNormalizer normalizer = new UriNormalizer();
        string input = "x://[" + host + "]:8080/path?query#fragment";

        string actual = normalizer.NormalizeSyntax(
            input,
            new UriNormalizationOptions());

        Assert.That(
            actual,
            Is.EqualTo("x://[" + expected + "]:8080/path?query#fragment"),
            host);
    }
}
