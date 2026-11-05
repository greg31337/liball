using System.Net;
using System.Net.Sockets;
using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetStandardsConformanceTests
{
    [TestCase("他们为什么不说中文", "xn--ihqwcrb4cv8a8dqg056pqjye")]
    [TestCase("なぜみんな日本語を話してくれないのか", "xn--n8jok5ay5dzabd5bym9f0cm5685rrjetr6pdxa")]
    [TestCase("почемужеонинеговорятпорусски", "xn--b1abfaaepdrnnbgefbadotcwatmq2g4l")]
    [TestCase("3年B組金八先生", "xn--3b-ww4c5e180e575a65lsy2b")]
    public void Rfc3492LabelsUseCanonicalAceAndRoundTrip(
        string unicodeLabel,
        string expectedAceLabel)
    {
        InternetProvider internet = Fake.Create(9201).Internet();
        string unicode = unicodeLabel + ".example";
        string ascii = expectedAceLabel + ".example";

        Assert.That(internet.ToAsciiDomain(unicode), Is.EqualTo(ascii));
        Assert.That(
            internet.ToUnicodeDomain(ascii),
            Is.EqualTo(unicode.ToLowerInvariant().Normalize(NormalizationForm.FormC)));
        Assert.That(internet.ToAsciiDomain(internet.ToUnicodeDomain(ascii)), Is.EqualTo(ascii));
    }

    [Test]
    public void PunycodeValidationRejectsMalformedAceAndUts39MixedScriptSpoofs()
    {
        InternetProvider internet = Fake.Create(9202).Internet();
        string[] malformedAce = new string[]
        {
            "xn--a.example",
            "xn--0.example",
            "xn--abc-.example",
            "xn--.example"
        };
        string[] mixedScript = new string[]
        {
            "pаypаl.example",
            "pαypal.example",
            "Сirсlе.example"
        };

        foreach (string value in malformedAce)
        {
            Assert.That(() => internet.ToAsciiDomain(value), Throws.Exception, value);
        }
        foreach (string value in mixedScript)
        {
            Assert.That(
                () => internet.ToAsciiDomain(value),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("domainName"),
                value);
        }
    }

    [Test]
    public void GeneratedIdnLabelsAvoidLatinGreekAndCyrillicMixing()
    {
        InternetProvider internet = Fake.Create(9203).Internet();

        int index = 0;
        while (index < 512)
        {
            string value = internet.IdnDomainName();
            string label = value.Substring(0, value.IndexOf('.', StringComparison.Ordinal));

            Assert.That(ScriptCount(label), Is.LessThanOrEqualTo(1), label);
            Assert.That(internet.ToUnicodeDomain(internet.ToAsciiDomain(value)), Is.EqualTo(value));
            index++;
        }
    }

    [Test]
    public void UnicodeDomainOutputLimitAppliesToDecodedOutputNotLongerAceInput()
    {
        FakeOptions exactOptions = FakeOptions.Create(9204);
        exactOptions.MaximumOutputSize = 14;
        InternetProvider exact = Fake.Create(exactOptions).Internet();
        FakeOptions shortOptions = FakeOptions.Create(9204);
        shortOptions.MaximumOutputSize = 13;
        InternetProvider shortOutput = Fake.Create(shortOptions).Internet();

        Assert.That(exact.ToUnicodeDomain("xn--bcher-kva.example"), Is.EqualTo("bücher.example"));
        Assert.That(
            () => shortOutput.ToUnicodeDomain("xn--bcher-kva.example"),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("domainName"));
    }

    [TestCase(";/?:@&=+$,#", "%3B%2F%3F%3A%40%26%3D%2B%24%2C%23")]
    [TestCase("%2f", "%252f")]
    [TestCase("café", "caf%C3%A9")]
    [TestCase("\uD83D\uDE00", "%F0%9F%98%80")]
    public void Rfc3986ComponentDataUsesUtf8AndUppercasePercentTriplets(
        string value,
        string expected)
    {
        string encoded = Fake.Create(9205).Internet().EncodeUriComponent(value);

        Assert.That(encoded, Is.EqualTo(expected));
        Assert.That(Uri.UnescapeDataString(encoded), Is.EqualTo(value));
    }

    [Test]
    public void Rfc4632MasksAreContiguousForEveryIpv4Prefix()
    {
        InternetProvider internet = Fake.Create(9206).Internet();

        int prefixLength = 0;
        while (prefixLength <= 32)
        {
            byte[] mask = IPAddress.Parse(
                internet.Ipv4MaskFromPrefixLength(prefixLength))
                .GetAddressBytes();

            AssertMask(mask, prefixLength);
            prefixLength++;
        }
    }

    [Test]
    public void Rfc5737LocaleDataCannotReplaceReservedDefaultsWithAnotherNetwork()
    {
        Fake fake = Fake.Create(9207);
        fake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["internet.ipv4.documentation-prefixes"] = new string[]
                {
                    "192.0.3.0/24"
                }
            });

        Assert.That(
            () => fake.Internet().Ipv4(),
            Throws.TypeOf<FakeException>().With.Message.Contains("RFC 5737"));
    }

    [Test]
    public void Ipv4CidrOutputLimitCountsOnlyTheReturnedCidr()
    {
        FakeOptions exactOptions = FakeOptions.Create(9208);
        exactOptions.MaximumOutputSize = 12;
        Fake exactFake = Fake.Create(exactOptions);
        exactFake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["internet.ipv4.documentation-prefixes"] = new string[]
                {
                    "192.0.2.0/24"
                }
            });
        FakeOptions shortOptions = FakeOptions.Create(9208);
        shortOptions.MaximumOutputSize = 11;
        Fake shortFake = Fake.Create(shortOptions);
        shortFake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["internet.ipv4.documentation-prefixes"] = new string[]
                {
                    "192.0.2.0/24"
                }
            });

        Assert.That(exactFake.Internet().Ipv4Cidr(), Is.EqualTo("192.0.2.0/24"));
        Assert.That(() => shortFake.Internet().Ipv4Cidr(), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase("2001:0db8:0:0:1:0:0:1", "2001:db8::1:0:0:1")]
    [TestCase("2001:db8:0:1:1:1:1:1", "2001:db8:0:1:1:1:1:1")]
    [TestCase("2001:db8:0:0:1:0:0:0", "2001:db8:0:0:1::")]
    [TestCase("0:0:0:0:0:ffff:c000:0280", "::ffff:192.0.2.128")]
    [TestCase("0:0:0:0:0:0:0:0", "::")]
    public void Rfc5952CanonicalizationSuppressesLeadingZerosAndChoosesTheFirstLongestRun(
        string value,
        string expected)
    {
        InternetProvider internet = Fake.Create(9209).Internet();

        Assert.That(internet.CanonicalIpv6(value), Is.EqualTo(expected));
        Assert.That(internet.CanonicalIpv6(expected), Is.EqualTo(expected));
    }

    [Test]
    public void Rfc4291Ipv6MasksAndRangeBoundariesCoverAllBits()
    {
        InternetProvider internet = Fake.Create(9210).Internet();
        int prefixLength = 0;
        while (prefixLength <= 128)
        {
            byte[] mask = IPAddress.Parse(
                internet.Ipv6MaskFromPrefixLength(prefixLength))
                .GetAddressBytes();

            AssertMask(mask, prefixLength);
            prefixLength++;
        }

        Ipv6Range documentation = internet.Ipv6RangeFromKindAndPrefixLength(
            NetworkAddressKind.Documentation,
            32);
        Ipv6Range host = internet.Ipv6RangeFromKindAndPrefixLength(
            NetworkAddressKind.Documentation,
            128);
        Assert.Multiple(() =>
        {
            Assert.That(documentation.NetworkAddress.ToString(), Is.EqualTo("2001:db8::"));
            Assert.That(documentation.LastAddress.ToString(), Is.EqualTo("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff"));
            Assert.That(documentation.Cidr, Is.EqualTo("2001:db8::/32"));
            Assert.That(host.NetworkAddress, Is.EqualTo(host.LastAddress));
            Assert.That(host.AddressCount, Is.EqualTo(System.Numerics.BigInteger.One));
        });
    }

    [Test]
    public void GenericAddressCategoriesRemainInsideTheirIpv4OrIpv6Blocks()
    {
        FakeOptions options = FakeOptions.Create(9211);
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        InternetProvider internet = Fake.Create(options).Internet();

        foreach (NetworkAddressKind kind in Enum.GetValues<NetworkAddressKind>())
        {
            int index = 0;
            while (index < 128)
            {
                IPAddress address = internet.IpAddressFromKind(kind);
                Assert.That(AddressMatchesKind(address, kind), Is.True, address + " " + kind);
                index++;
            }
        }
    }

    [Test]
    public void ReservedDefaultsNeverProduceContactableInternetTargets()
    {
        InternetProvider internet = Fake.Create(9212).Internet();

        int index = 0;
        while (index < 256)
        {
            IPAddress address = internet.IpAddress();
            IPEndPoint endpoint = internet.Endpoint();
            Uri uri = internet.Uri();

            Assert.That(IsDocumentation(address), Is.True, address.ToString());
            Assert.That(IsDocumentation(endpoint.Address), Is.True, endpoint.ToString());
            Assert.That(IsExampleHost(uri.Host), Is.True, uri.ToString());
            index++;
        }
    }

    [Test]
    public void HttpHeaderRecordsRejectNameSeparatorsAndEveryLineInjectionForm()
    {
        InternetProvider internet = Fake.Create(9213).Internet();
        string[] invalidNames = new string[]
        {
            "Bad Name", "Bad:Name", "Bad(Name)", "Bad@Name", "Bad/Name", "Bad[Name]"
        };
        string[] invalidValues = new string[]
        {
            "one\rtwo", "one\ntwo", "one\r\nInjected: true", "one\0two",
            "one\u000btwo", "one\u007ftwo", "one\u0080two"
        };

        foreach (string name in invalidNames)
        {
            Assert.That(
                () => internet.HttpHeaderRecordFromNameAndValue(name, "value"),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("name"),
                name);
        }
        foreach (string value in invalidValues)
        {
            Assert.That(
                () => internet.HttpHeaderRecordFromNameAndValue("X-Fixture", value),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("value"));
        }

        HttpHeaderRecord valid = internet.HttpHeaderRecordFromNameAndValue(
            "X-Fixture",
            "one\ttwo");
        Assert.That(valid.Text, Is.EqualTo("X-Fixture: one\ttwo"));
    }

    static void AssertMask(byte[] bytes, int prefixLength)
    {
        int bit = 0;
        while (bit < bytes.Length * 8)
        {
            int value = bytes[bit / 8] & (1 << (7 - bit % 8));
            Assert.That(value != 0, Is.EqualTo(bit < prefixLength), "bit " + bit);
            bit++;
        }
    }

    static int ScriptCount(string value)
    {
        bool latin = false;
        bool greek = false;
        bool cyrillic = false;
        int index = 0;
        while (index < value.Length)
        {
            int codePoint = char.ConvertToUtf32(value, index);
            latin = latin || IsLatin(codePoint);
            greek = greek || IsGreek(codePoint);
            cyrillic = cyrillic || IsCyrillic(codePoint);
            index = index + (codePoint > 0xffff ? 2 : 1);
        }
        return (latin ? 1 : 0) + (greek ? 1 : 0) + (cyrillic ? 1 : 0);
    }

    static bool IsLatin(int value)
    {
        return value >= 0x0041 && value <= 0x005a ||
            value >= 0x0061 && value <= 0x007a ||
            value >= 0x00c0 && value <= 0x024f ||
            value >= 0x1e00 && value <= 0x1eff;
    }

    static bool IsGreek(int value)
    {
        return value >= 0x0370 && value <= 0x03ff ||
            value >= 0x1f00 && value <= 0x1fff;
    }

    static bool IsCyrillic(int value)
    {
        return value >= 0x0400 && value <= 0x052f ||
            value >= 0x2de0 && value <= 0x2dff ||
            value >= 0xa640 && value <= 0xa69f;
    }

    static bool AddressMatchesKind(IPAddress address, NetworkAddressKind kind)
    {
        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            switch (kind)
            {
                case NetworkAddressKind.Documentation:
                    return IsDocumentation(address);
                case NetworkAddressKind.Private:
                    return bytes[0] == 10 ||
                        bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                        bytes[0] == 192 && bytes[1] == 168;
                case NetworkAddressKind.Loopback:
                    return bytes[0] == 127;
                case NetworkAddressKind.LinkLocal:
                    return bytes[0] == 169 && bytes[1] == 254;
                case NetworkAddressKind.Multicast:
                    return bytes[0] >= 224 && bytes[0] <= 239;
                case NetworkAddressKind.Public:
                    return IsPublicIpv4FirstOctet(bytes[0]);
                default:
                    return false;
            }
        }
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
                return IsDocumentation(address);
            case NetworkAddressKind.Private:
                return bytes[0] == 0xfd;
            case NetworkAddressKind.Loopback:
                return address.Equals(IPAddress.IPv6Loopback);
            case NetworkAddressKind.LinkLocal:
                return bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80;
            case NetworkAddressKind.Multicast:
                return bytes[0] == 0xff;
            case NetworkAddressKind.Public:
                return (bytes[0] == 0x24 || bytes[0] == 0x26 ||
                    bytes[0] == 0x28 || bytes[0] == 0x2a ||
                    bytes[0] == 0x2c) && (bytes[1] & 0xf0) == 0;
            default:
                return false;
        }
    }

    static bool IsDocumentation(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2 ||
                bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100 ||
                bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113;
        }
        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
            bytes[0] == 0x20 && bytes[1] == 0x01 &&
            bytes[2] == 0x0d && bytes[3] == 0xb8;
    }

    static bool IsExampleHost(string value)
    {
        return value == "example.com" || value.EndsWith(".example.com", StringComparison.Ordinal) ||
            value == "example.net" || value.EndsWith(".example.net", StringComparison.Ordinal) ||
            value == "example.org" || value.EndsWith(".example.org", StringComparison.Ordinal);
    }

    static bool IsPublicIpv4FirstOctet(byte value)
    {
        return new byte[]
        {
            1, 8, 9, 11, 23, 31, 45, 46, 50, 52, 54, 64,
            66, 67, 68, 69, 70, 72, 74, 75, 76, 77, 78, 80,
            81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92,
            93, 94, 95
        }.Contains(value);
    }
}
