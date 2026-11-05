namespace uri.Tests;

public class UriNormalizerIpTests
{
    [TestCase(0, 0, 0, 0, "0.0.0.0")]
    [TestCase(0, 9, 10, 99, "0.9.10.99")]
    [TestCase(100, 101, 199, 200, "100.101.199.200")]
    [TestCase(249, 250, 254, 255, "249.250.254.255")]
    public void Ipv4SerializationUsesCanonicalDecimalDottedQuad(
        byte first,
        byte second,
        byte third,
        byte fourth,
        string expected)
    {
        byte[] addressBytes = new byte[] { first, second, third, fourth };

        string normalized = UriNormalizerCore.NormalizeIpv4Address(addressBytes);

        Assert.That(normalized, Is.EqualTo(expected));
    }

    [TestCase("2001:0DB8:0000:0000:0000:0000:0000:0001", "2001:db8::1")]
    [TestCase("2001:db8:0:1:1:1:1:1", "2001:db8:0:1:1:1:1:1")]
    [TestCase("2001:0:0:1:0:0:0:1", "2001:0:0:1::1")]
    [TestCase("2001:db8:0:0:1:0:0:1", "2001:db8::1:0:0:1")]
    [TestCase("2001:0db8:0001:0002:0003:0004:0005:0006", "2001:db8:1:2:3:4:5:6")]
    [TestCase("2001:db8:1:2:0:0:0:0", "2001:db8:1:2::")]
    [TestCase("0:0:0:0:1:2:3:4", "::1:2:3:4")]
    [TestCase("0:0:0:0:0:0:0:0", "::")]
    [TestCase("0:0:0:0:0:0:0:1", "::1")]
    [TestCase("ABCD:EF01:2345:6789:ABCD:EF01:2345:6789", "abcd:ef01:2345:6789:abcd:ef01:2345:6789")]
    public void Ipv6SerializationAppliesRfc5952CanonicalHexadecimalForm(
        string input,
        string expected)
    {
        AssertIpv6Normalization(input, expected);
    }

    [TestCase("1:0:0:2:0:0:3:4", "1::2:0:0:3:4")]
    [TestCase("0:0:1:2:3:4:0:0", "::1:2:3:4:0:0")]
    [TestCase("1:2:0:0:3:4:0:0", "1:2::3:4:0:0")]
    [TestCase("0:0:1:0:0:2:0:0", "::1:0:0:2:0:0")]
    public void EqualLengthZeroRunTiesCompressTheFirstRun(
        string input,
        string expected)
    {
        AssertIpv6Normalization(input, expected);
    }

    [TestCase("0:0:0:0:0:FFFF:192.0.2.128", "::ffff:192.0.2.128")]
    [TestCase("::ffff:c000:0280", "::ffff:192.0.2.128")]
    [TestCase("0:0:0:0:0:0:192.0.2.128", "::192.0.2.128")]
    [TestCase("::c000:0280", "::192.0.2.128")]
    [TestCase("0:0:0:0:ffff:0:192.0.2.128", "::ffff:0:192.0.2.128")]
    [TestCase("64:ff9b:0:0:0:0:192.0.2.128", "64:ff9b::192.0.2.128")]
    [TestCase("2001:db8:1:2:0:5efe:192.0.2.128", "2001:db8:1:2:0:5efe:192.0.2.128")]
    [TestCase("2001:db8:1:2:200:5efe:c000:0280", "2001:db8:1:2:200:5efe:192.0.2.128")]
    public void RecognizedEmbeddedIpv4AddressesUseCanonicalMixedNotation(
        string input,
        string expected)
    {
        AssertIpv6Normalization(input, expected);
    }

    [TestCase("2001:db8::192.0.2.128", "2001:db8::c000:280")]
    [TestCase("2001:db8:0:0:0:0:c000:0280", "2001:db8::c000:280")]
    public void UnknownEmbeddedPrefixesUseCanonicalHexadecimalNotation(
        string input,
        string expected)
    {
        AssertIpv6Normalization(input, expected);
    }

    [TestCase("::", "::")]
    [TestCase("::1", "::1")]
    public void UnspecifiedAndLoopbackDoNotUseAmbiguousDottedNotation(
        string input,
        string expected)
    {
        AssertIpv6Normalization(input, expected);
    }

    [Test]
    public void AddressSerializersRejectIncorrectByteLengths()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeIpv4Address(new byte[3]));
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeIpv4Address(new byte[5]));
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeIpv6Address(new byte[15]));
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeIpv6Address(new byte[17]));
        });
    }

    [TestCase("192.0.2.128")]
    [TestCase("255.255.255.255")]
    public void Ipv4SerializationIsIdempotentThroughStrictParsing(string input)
    {
        IpV4ParseResult firstParse = IpV4Parser.Parse(input.AsSpan(), 0);
        string first = UriNormalizerCore.NormalizeIpv4Address(firstParse.AddressBytes);
        IpV4ParseResult secondParse = IpV4Parser.Parse(first.AsSpan(), 0);
        string second = UriNormalizerCore.NormalizeIpv4Address(secondParse.AddressBytes);

        Assert.Multiple(() =>
        {
            Assert.That(firstParse.Success, Is.True);
            Assert.That(secondParse.Success, Is.True);
            Assert.That(second, Is.EqualTo(first));
        });
    }

    [TestCase("2001:0DB8:0:0:1:0:0:1")]
    [TestCase("0:0:0:0:0:FFFF:192.0.2.128")]
    [TestCase("64:ff9b:0:0:0:0:192.0.2.128")]
    [TestCase("2001:db8::192.0.2.128")]
    public void Ipv6SerializationIsIdempotentThroughStrictParsing(string input)
    {
        IpV6ParseResult firstParse = IpV6Parser.Parse(input.AsSpan(), 0);
        string first = UriNormalizerCore.NormalizeIpv6Address(firstParse.AddressBytes);
        IpV6ParseResult secondParse = IpV6Parser.Parse(first.AsSpan(), 0);
        string second = UriNormalizerCore.NormalizeIpv6Address(secondParse.AddressBytes);

        Assert.Multiple(() =>
        {
            Assert.That(firstParse.Success, Is.True, input);
            Assert.That(secondParse.Success, Is.True, first);
            Assert.That(second, Is.EqualTo(first));
        });
    }

    private static void AssertIpv6Normalization(string input, string expected)
    {
        IpV6ParseResult parsed = IpV6Parser.Parse(input.AsSpan(), 0);

        Assert.That(parsed.Success, Is.True, input);

        string normalized = UriNormalizerCore.NormalizeIpv6Address(parsed.AddressBytes);

        Assert.That(normalized, Is.EqualTo(expected), input);
    }
}
