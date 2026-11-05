using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetCompletionTests
{
    [Test]
    public void GenericAddressesSelectBothFamiliesDeterministicallyAndRemainInCategory()
    {
        InternetProvider first = Fake.Create(9101).Internet();
        InternetProvider second = Fake.Create(9101).Internet();
        bool sawIpv4 = false;
        bool sawIpv6 = false;

        int index = 0;
        while (index < 512)
        {
            IPAddress address = first.IpAddress();

            Assert.That(address, Is.EqualTo(second.IpAddress()));
            Assert.That(IsDocumentation(address), Is.True);
            sawIpv4 = sawIpv4 || address.AddressFamily == AddressFamily.InterNetwork;
            sawIpv6 = sawIpv6 || address.AddressFamily == AddressFamily.InterNetworkV6;
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(sawIpv4, Is.True);
            Assert.That(sawIpv6, Is.True);
            Assert.That(IPAddress.IsLoopback(first.IpAddressFromKind(NetworkAddressKind.Loopback)), Is.True);
        });
    }

    [Test]
    public void GenericEndpointsSelectBothFamiliesAndHonorExplicitPorts()
    {
        InternetProvider first = Fake.Create(9102).Internet();
        InternetProvider second = Fake.Create(9102).Internet();
        bool sawIpv4 = false;
        bool sawIpv6 = false;

        int index = 0;
        while (index < 512)
        {
            IPEndPoint endpoint = first.Endpoint();

            Assert.That(endpoint, Is.EqualTo(second.Endpoint()));
            Assert.That(IsDocumentation(endpoint.Address), Is.True);
            Assert.That(endpoint.Port, Is.InRange(49152, 65535));
            sawIpv4 = sawIpv4 || endpoint.AddressFamily == AddressFamily.InterNetwork;
            sawIpv6 = sawIpv6 || endpoint.AddressFamily == AddressFamily.InterNetworkV6;
            index++;
        }

        IPEndPoint zero = first.EndpointFromPort(0);
        IPEndPoint maximum = first.EndpointFromKindAndPort(NetworkAddressKind.Loopback, 65535);
        Assert.Multiple(() =>
        {
            Assert.That(sawIpv4, Is.True);
            Assert.That(sawIpv6, Is.True);
            Assert.That(zero.Port, Is.Zero);
            Assert.That(IsDocumentation(zero.Address), Is.True);
            Assert.That(maximum.Port, Is.EqualTo(65535));
            Assert.That(IPAddress.IsLoopback(maximum.Address), Is.True);
        });
    }

    [Test]
    public void Ipv6MasksCoverEveryBoundaryShape()
    {
        InternetProvider internet = Fake.Create(9103).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(internet.Ipv6MaskFromPrefixLength(0), Is.EqualTo("::"));
            Assert.That(internet.Ipv6MaskFromPrefixLength(1), Is.EqualTo("8000::"));
            Assert.That(internet.Ipv6MaskFromPrefixLength(64), Is.EqualTo("ffff:ffff:ffff:ffff::"));
            Assert.That(internet.Ipv6MaskFromPrefixLength(127), Is.EqualTo("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe"));
            Assert.That(internet.Ipv6MaskFromPrefixLength(128), Is.EqualTo("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"));
            Assert.That(() => internet.Ipv6MaskFromPrefixLength(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
            Assert.That(() => internet.Ipv6MaskFromPrefixLength(129),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        });
    }

    [Test]
    public void Ipv6RangesExposeAlignedInclusiveMetadataAndLargeCounts()
    {
        InternetProvider first = Fake.Create(9104).Internet();
        InternetProvider second = Fake.Create(9104).Internet();
        Ipv6Range range = first.Ipv6Range();
        Ipv6Range duplicate = second.Ipv6Range();
        Ipv6Range documentation = first.Ipv6RangeFromKindAndPrefixLength(
            NetworkAddressKind.Documentation,
            32);
        Ipv6Range loopback = first.Ipv6RangeFromKindAndPrefixLength(
            NetworkAddressKind.Loopback,
            128);

        Assert.Multiple(() =>
        {
            Assert.That(range.Cidr, Is.EqualTo(duplicate.Cidr));
            Assert.That(range.NetworkAddress, Is.EqualTo(duplicate.NetworkAddress));
            Assert.That(range.LastAddress, Is.EqualTo(duplicate.LastAddress));
            Assert.That(range.Kind, Is.EqualTo(NetworkAddressKind.Documentation));
            Assert.That(range.PrefixLength, Is.EqualTo(64));
            Assert.That(range.Netmask.ToString(), Is.EqualTo("ffff:ffff:ffff:ffff::"));
            Assert.That(range.AddressCount, Is.EqualTo(BigInteger.One << 64));
            Assert.That(range.Cidr, Is.EqualTo(range.NetworkAddress + "/64"));
            Assert.That(range.Contains(range.NetworkAddress), Is.True);
            Assert.That(range.Contains(range.LastAddress), Is.True);
            Assert.That(range.Contains(IPAddress.Loopback), Is.False);
            Assert.That(documentation.NetworkAddress.ToString(), Is.EqualTo("2001:db8::"));
            Assert.That(documentation.LastAddress.ToString(), Is.EqualTo("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff"));
            Assert.That(documentation.AddressCount, Is.EqualTo(BigInteger.One << 96));
            Assert.That(loopback.NetworkAddress, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(loopback.LastAddress, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(loopback.AddressCount, Is.EqualTo(BigInteger.One));
            Assert.That(() => range.Contains(null),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("address"));
        });
    }

    [Test]
    public void GravatarHashesNormalizeEmailAndDoNotAdvanceEntropy()
    {
        InternetProvider inspected = Fake.Create(9105).Internet();
        InternetProvider untouched = Fake.Create(9105).Internet();

        string hash = inspected.GravatarHashFromEmail(" MyEmailAddress@example.com ");

        Assert.Multiple(() =>
        {
            Assert.That(hash, Is.EqualTo("0bc83cb571cd1c50ba6f3e8a78ef1346"));
            Assert.That(inspected.GravatarHashFromEmail("myemailaddress@EXAMPLE.COM"), Is.EqualTo(hash));
            Assert.That(() => inspected.GravatarHashFromEmail(null),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("email"));
            Assert.That(() => inspected.GravatarHashFromEmail(" \t"),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("email"));
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        });

        InternetProvider firstGenerated = Fake.Create(9112).Internet();
        InternetProvider secondGenerated = Fake.Create(9112).Internet();
        string generated = firstGenerated.GravatarHash();
        Assert.That(generated, Is.EqualTo(secondGenerated.GravatarHash()));
        Assert.That(generated, Does.Match("^[0-9a-f]{32}$"));
    }

    [Test]
    public void UrlShortCodesAreBoundedBase62Values()
    {
        InternetProvider first = Fake.Create(9106).Internet();
        InternetProvider second = Fake.Create(9106).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(first.UrlShortCode(), Is.EqualTo(second.UrlShortCode()));
            Assert.That(first.UrlShortCodeFromLength(1), Is.EqualTo(second.UrlShortCodeFromLength(1)));
            Assert.That(first.UrlShortCodeFromLength(64), Is.EqualTo(second.UrlShortCodeFromLength(64)));
        });

        string value = Fake.Create(9107).Internet().UrlShortCodeFromLength(64);
        Assert.That(value, Has.Length.EqualTo(64));
        Assert.That(value, Does.Match("^[0-9A-Za-z]{64}$"));
    }

    [Test]
    public void HttpHeaderRecordsKeepNamesValuesAndSyntheticStatusCorrelated()
    {
        FakeOptions options = FakeOptions.Create(9108);
        options.ReferenceTime = new DateTimeOffset(2024, 2, 29, 13, 34, 56, TimeSpan.FromHours(-5));
        InternetProvider internet = Fake.Create(options).Internet();

        foreach (string name in internet.HttpHeaders())
        {
            HttpHeaderRecord record = internet.HttpHeaderRecordFromName(name.ToLowerInvariant());

            Assert.Multiple(() =>
            {
                Assert.That(record.Name, Is.EqualTo(name));
                Assert.That(record.Value, Is.Not.Null);
                Assert.That(record.Text, Is.EqualTo(record.Name + ": " + record.Value));
                Assert.That(record.Text, Does.Not.Contain("\r"));
                Assert.That(record.Text, Does.Not.Contain("\n"));
                Assert.That(record.IsSynthetic, Is.True);
                Assert.That(record.RepresentsLiveCredential, Is.False);
            });
        }

        HttpHeaderRecord date = internet.HttpHeaderRecordFromName("date");
        HttpHeaderRecord explicitRecord = internet.HttpHeaderRecordFromNameAndValue(
            "X-Fixture",
            "one\ttwo");
        Assert.Multiple(() =>
        {
            Assert.That(date.Value, Is.EqualTo(options.ReferenceTime.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)));
            Assert.That(explicitRecord.Name, Is.EqualTo("X-Fixture"));
            Assert.That(explicitRecord.Value, Is.EqualTo("one\ttwo"));
            Assert.That(explicitRecord.Text, Is.EqualTo("X-Fixture: one\ttwo"));
            Assert.That(() => internet.HttpHeaderRecordFromName("X-Unknown"),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("name"));
            Assert.That(() => internet.HttpHeaderRecordFromNameAndValue("Bad Name", "value"),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("name"));
            Assert.That(() => internet.HttpHeaderRecordFromNameAndValue("X-Fixture", "safe\r\nInjected: true"),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("value"));
        });
    }

    [Test]
    public void NewInternetOperationsHonorLimitsAndRejectArgumentsBeforeEntropy()
    {
        InternetProvider inspected = Fake.Create(9109).Internet();
        InternetProvider untouched = Fake.Create(9109).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.IpAddressFromKind((NetworkAddressKind)999), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.EndpointFromKind((NetworkAddressKind)999), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.EndpointFromKindAndPort((NetworkAddressKind)999, 443), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.EndpointFromPort(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.EndpointFromPort(65536), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.UrlShortCodeFromLength(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        });

        FakeOptions countOptions = FakeOptions.Create(9110);
        countOptions.MaximumCount = 3;
        InternetProvider countLimited = Fake.Create(countOptions).Internet();
        FakeOptions outputOptions = FakeOptions.Create(9111);
        outputOptions.MaximumOutputSize = 31;
        InternetProvider outputLimited = Fake.Create(outputOptions).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(() => countLimited.UrlShortCodeFromLength(4), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputLimited.UrlShortCodeFromLength(32), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputLimited.GravatarHashFromEmail("fixture@example.com"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputLimited.Ipv6Range(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputLimited.HttpHeaderRecordFromNameAndValue("X-Long", new string('x', 26)), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
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
}
