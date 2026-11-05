using System.Net;
using System.Net.Sockets;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetIpv4Tests
{
    [Test]
    public void DefaultTextAndTypedAddressesUseRfc5737AndAreDeterministic()
    {
        InternetProvider firstText = Fake.Create(8201).Internet();
        InternetProvider secondText = Fake.Create(8201).Internet();
        InternetProvider firstTyped = Fake.Create(8202).Internet();
        InternetProvider secondTyped = Fake.Create(8202).Internet();

        int index = 0;
        while (index < 512)
        {
            string text = firstText.Ipv4();
            IPAddress address = firstTyped.Ipv4Address();

            Assert.That(text, Is.EqualTo(secondText.Ipv4()));
            Assert.That(address, Is.EqualTo(secondTyped.Ipv4Address()));
            Assert.That(IPAddress.Parse(text).AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(address.AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(IsDocumentation(IPAddress.Parse(text)), Is.True);
            Assert.That(IsDocumentation(address), Is.True);
            index++;
        }
    }

    [Test]
    public void ExplicitAddressCategoriesStayInsideTheirDeclaredBlocks()
    {
        InternetProvider internet = NonReservedInternet(8203);

        int index = 0;
        while (index < 512)
        {
            Assert.That(IsDocumentation(IPAddress.Parse(internet.Ipv4FromKind(NetworkAddressKind.Documentation))), Is.True);
            Assert.That(IsPrivate(IPAddress.Parse(internet.Ipv4FromKind(NetworkAddressKind.Private))), Is.True);
            Assert.That(IsLoopback(IPAddress.Parse(internet.Ipv4FromKind(NetworkAddressKind.Loopback))), Is.True);
            Assert.That(IsLinkLocal(IPAddress.Parse(internet.Ipv4FromKind(NetworkAddressKind.LinkLocal))), Is.True);
            Assert.That(IsMulticast(IPAddress.Parse(internet.Ipv4FromKind(NetworkAddressKind.Multicast))), Is.True);
            Assert.That(IsPublicShaped(IPAddress.Parse(internet.Ipv4FromKind(NetworkAddressKind.Public))), Is.True);
            index++;
        }
    }

    [Test]
    public void ReservedModeRejectsEveryExplicitPublicAddressForm()
    {
        InternetProvider internet = Fake.Create(8213).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(() => internet.Ipv4FromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4AddressFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.CidrFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.CidrFromKindAndPrefixLength(NetworkAddressKind.Public, 24), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4CidrFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4CidrFromKindAndPrefixLength(NetworkAddressKind.Public, 24), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4RangeFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Public, 24), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4EndpointFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv4EndpointFromKindAndPort(NetworkAddressKind.Public, 443), Throws.TypeOf<FakeException>());
        });
    }

    [TestCase(0, "0.0.0.0")]
    [TestCase(1, "128.0.0.0")]
    [TestCase(8, "255.0.0.0")]
    [TestCase(12, "255.240.0.0")]
    [TestCase(16, "255.255.0.0")]
    [TestCase(24, "255.255.255.0")]
    [TestCase(25, "255.255.255.128")]
    [TestCase(31, "255.255.255.254")]
    [TestCase(32, "255.255.255.255")]
    public void ExplicitMasksMatchPrefixLength(int prefixLength, string expected)
    {
        Assert.That(Fake.Create(8204).Internet().Ipv4MaskFromPrefixLength(prefixLength), Is.EqualTo(expected));
    }

    [Test]
    public void GeneratedMasksAreContiguousAndDeterministic()
    {
        InternetProvider first = Fake.Create(8205).Internet();
        InternetProvider second = Fake.Create(8205).Internet();

        int index = 0;
        while (index < 256)
        {
            string mask = first.Ipv4Mask();
            uint number = Number(IPAddress.Parse(mask));

            Assert.That(mask, Is.EqualTo(second.Ipv4Mask()));
            Assert.That(number, Is.Not.EqualTo(0U));
            Assert.That(((~number) & ((~number) + 1U)), Is.EqualTo(0U));
            index++;
        }
    }

    [Test]
    public void DefaultCidrsAreAlignedRfc5737Networks()
    {
        InternetProvider first = Fake.Create(8206).Internet();
        InternetProvider second = Fake.Create(8206).Internet();

        int index = 0;
        while (index < 256)
        {
            string cidr = first.Cidr();
            string alias = second.Ipv4Cidr();

            Assert.That(alias, Is.EqualTo(cidr));
            Assert.That(cidr, Is.AnyOf(
                "192.0.2.0/24",
                "198.51.100.0/24",
                "203.0.113.0/24"));
            index++;
        }
    }

    [Test]
    public void RangesExposeAlignedInclusiveMetadataForEveryCategory()
    {
        InternetProvider internet = NonReservedInternet(8207);

        foreach (NetworkAddressKind kind in Enum.GetValues<NetworkAddressKind>())
        {
            Ipv4Range range = internet.Ipv4RangeFromKindAndPrefixLength(kind, 24);
            uint network = Number(range.NetworkAddress);
            uint last = Number(range.LastAddress);
            uint mask = Number(range.Netmask);

            Assert.Multiple(() =>
            {
                Assert.That(range.Kind, Is.EqualTo(kind));
                Assert.That(range.PrefixLength, Is.EqualTo(24));
                Assert.That(range.AddressCount, Is.EqualTo(256UL));
                Assert.That(network & mask, Is.EqualTo(network));
                Assert.That(last, Is.EqualTo(network + 255U));
                Assert.That(range.Cidr, Is.EqualTo(range.NetworkAddress + "/24"));
                Assert.That(range.Contains(range.NetworkAddress), Is.True);
                Assert.That(range.Contains(range.LastAddress), Is.True);
                Assert.That(range.Contains(IPAddress.IPv6Loopback), Is.False);
                Assert.That(AddressMatchesKind(range.NetworkAddress, kind), Is.True);
                Assert.That(AddressMatchesKind(range.LastAddress, kind), Is.True);
            });
        }
    }

    [Test]
    public void CategoryBoundaryPrefixesProduceCompleteNativeRanges()
    {
        InternetProvider internet = NonReservedInternet(8208);
        Ipv4Range documentation = internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Documentation, 24);
        Ipv4Range privateRange = internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Private, 8);
        Ipv4Range loopback = internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Loopback, 8);
        Ipv4Range linkLocal = internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.LinkLocal, 16);
        Ipv4Range multicast = internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Multicast, 4);
        Ipv4Range publicRange = internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Public, 8);

        Assert.Multiple(() =>
        {
            Assert.That(documentation.AddressCount, Is.EqualTo(256UL));
            Assert.That(privateRange.NetworkAddress.ToString(), Is.EqualTo("10.0.0.0"));
            Assert.That(privateRange.LastAddress.ToString(), Is.EqualTo("10.255.255.255"));
            Assert.That(loopback.NetworkAddress.ToString(), Is.EqualTo("127.0.0.0"));
            Assert.That(loopback.LastAddress.ToString(), Is.EqualTo("127.255.255.255"));
            Assert.That(linkLocal.NetworkAddress.ToString(), Is.EqualTo("169.254.0.0"));
            Assert.That(linkLocal.LastAddress.ToString(), Is.EqualTo("169.254.255.255"));
            Assert.That(multicast.NetworkAddress.ToString(), Is.EqualTo("224.0.0.0"));
            Assert.That(multicast.LastAddress.ToString(), Is.EqualTo("239.255.255.255"));
            Assert.That(publicRange.AddressCount, Is.EqualTo(16777216UL));
            Assert.That(IsPublicShaped(publicRange.NetworkAddress), Is.True);
            Assert.That(IsPublicShaped(publicRange.LastAddress), Is.True);
        });
    }

    [Test]
    public void EndpointsUseDocumentationAddressesDynamicPortsAndExplicitValues()
    {
        InternetProvider first = Fake.Create(8209).Internet();
        InternetProvider second = Fake.Create(8209).Internet();

        int index = 0;
        while (index < 256)
        {
            IPEndPoint endpoint = first.Ipv4Endpoint();

            Assert.That(endpoint, Is.EqualTo(second.Ipv4Endpoint()));
            Assert.That(IsDocumentation(endpoint.Address), Is.True);
            Assert.That(endpoint.Port, Is.InRange(49152, 65535));
            index++;
        }

        IPEndPoint explicitEndpoint = first.Ipv4EndpointFromKindAndPort(NetworkAddressKind.Loopback, 443);
        Assert.That(IsLoopback(explicitEndpoint.Address), Is.True);
        Assert.That(explicitEndpoint.Port, Is.EqualTo(443));
    }

    [Test]
    public void InvalidCategoriesPrefixesPortsAndNullContainmentAreRejected()
    {
        InternetProvider internet = Fake.Create(8210).Internet();

        Assert.That(
            () => internet.Ipv4FromKind((NetworkAddressKind)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("kind"));
        Assert.That(
            () => internet.Ipv4MaskFromPrefixLength(-1),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => internet.Ipv4MaskFromPrefixLength(33),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.Documentation, 23),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => internet.Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind.LinkLocal, 15),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => internet.Ipv4EndpointFromPort(-1),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));
        Assert.That(
            () => internet.Ipv4EndpointFromPort(65536),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));
        Assert.That(
            () => internet.Ipv4Range().Contains(null),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("address"));
    }

    [Test]
    public void Ipv4OutputsHonorConfiguredOutputLimits()
    {
        FakeOptions options = FakeOptions.Create(8211);
        options.MaximumOutputSize = 6;
        InternetProvider internet = Fake.Create(options).Internet();

        Assert.That(() => internet.Ipv4(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Ipv4MaskFromPrefixLength(24), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Cidr(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Ipv4Range(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Ipv4Endpoint(), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static bool AddressMatchesKind(IPAddress address, NetworkAddressKind kind)
    {
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
                return IsDocumentation(address);
            case NetworkAddressKind.Private:
                return IsPrivate(address);
            case NetworkAddressKind.Loopback:
                return IsLoopback(address);
            case NetworkAddressKind.LinkLocal:
                return IsLinkLocal(address);
            case NetworkAddressKind.Multicast:
                return IsMulticast(address);
            case NetworkAddressKind.Public:
                return IsPublicShaped(address);
            default:
                return false;
        }
    }

    static InternetProvider NonReservedInternet(long seed)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        return Fake.Create(options).Internet();
    }

    static bool IsDocumentation(IPAddress address)
    {
        uint value = Number(address);
        return value >= Number(IPAddress.Parse("192.0.2.0")) && value <= Number(IPAddress.Parse("192.0.2.255")) ||
            value >= Number(IPAddress.Parse("198.51.100.0")) && value <= Number(IPAddress.Parse("198.51.100.255")) ||
            value >= Number(IPAddress.Parse("203.0.113.0")) && value <= Number(IPAddress.Parse("203.0.113.255"));
    }

    static bool IsPrivate(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
            bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
            bytes[0] == 192 && bytes[1] == 168;
    }

    static bool IsLoopback(IPAddress address)
    {
        return address.GetAddressBytes()[0] == 127;
    }

    static bool IsLinkLocal(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }

    static bool IsMulticast(IPAddress address)
    {
        byte first = address.GetAddressBytes()[0];
        return first >= 224 && first <= 239;
    }

    static bool IsPublicShaped(IPAddress address)
    {
        byte first = address.GetAddressBytes()[0];
        return new byte[]
        {
            1, 8, 9, 11, 23, 31, 45, 46, 50, 52, 54, 64,
            66, 67, 68, 69, 70, 72, 74, 75, 76, 77, 78, 80,
            81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92,
            93, 94, 95
        }.Contains(first);
    }

    static uint Number(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];
    }
}
