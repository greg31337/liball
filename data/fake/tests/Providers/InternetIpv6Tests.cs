using System.Net;
using System.Net.Sockets;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetIpv6Tests
{
    [Test]
    public void DefaultTextAndTypedAddressesUseRfc3849AndAreDeterministic()
    {
        InternetProvider firstText = Fake.Create(8301).Internet();
        InternetProvider secondText = Fake.Create(8301).Internet();
        InternetProvider firstTyped = Fake.Create(8302).Internet();
        InternetProvider secondTyped = Fake.Create(8302).Internet();

        int index = 0;
        while (index < 512)
        {
            string text = firstText.Ipv6();
            IPAddress parsed = IPAddress.Parse(text);
            IPAddress address = firstTyped.Ipv6Address();

            Assert.That(text, Is.EqualTo(secondText.Ipv6()));
            Assert.That(address, Is.EqualTo(secondTyped.Ipv6Address()));
            Assert.That(parsed.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(address.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(IsDocumentation(parsed), Is.True);
            Assert.That(IsDocumentation(address), Is.True);
            Assert.That(text, Is.EqualTo(firstText.CanonicalIpv6(text)));
            Assert.That(text, Is.EqualTo(text.ToLowerInvariant()));
            index++;
        }
    }

    [Test]
    public void ExplicitAddressCategoriesStayInsideTheirDeclaredBlocks()
    {
        InternetProvider internet = NonReservedInternet(8303);

        int index = 0;
        while (index < 512)
        {
            Assert.That(IsDocumentation(IPAddress.Parse(internet.Ipv6FromKind(NetworkAddressKind.Documentation))), Is.True);
            Assert.That(IsPrivate(IPAddress.Parse(internet.Ipv6FromKind(NetworkAddressKind.Private))), Is.True);
            Assert.That(IPAddress.Parse(internet.Ipv6FromKind(NetworkAddressKind.Loopback)), Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(IsLinkLocal(IPAddress.Parse(internet.Ipv6FromKind(NetworkAddressKind.LinkLocal))), Is.True);
            Assert.That(IPAddress.Parse(internet.Ipv6FromKind(NetworkAddressKind.Multicast)).IsIPv6Multicast, Is.True);
            Assert.That(IsPublicShaped(IPAddress.Parse(internet.Ipv6FromKind(NetworkAddressKind.Public))), Is.True);
            index++;
        }
    }

    [Test]
    public void ReservedModeRejectsEveryExplicitPublicAddressForm()
    {
        InternetProvider internet = Fake.Create(8313).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(() => internet.Ipv6FromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv6AddressFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv6CidrFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Public, 64), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv6EndpointFromKind(NetworkAddressKind.Public), Throws.TypeOf<FakeException>());
            Assert.That(() => internet.Ipv6EndpointFromKindAndPort(NetworkAddressKind.Public, 443), Throws.TypeOf<FakeException>());
        });
    }

    [TestCase("2001:0DB8:0000:0000:0000:FF00:0042:8329", "2001:db8::ff00:42:8329")]
    [TestCase("2001:db8:0:1:1:1:1:1", "2001:db8:0:1:1:1:1:1")]
    [TestCase("2001:0:0:1:0:0:1:1", "2001::1:0:0:1:1")]
    [TestCase("0:0:0:0:0:0:0:0", "::")]
    [TestCase("0:0:0:0:0:0:0:1", "::1")]
    [TestCase("2001:db8:1:2:0:0:0:0", "2001:db8:1:2::")]
    [TestCase("2001:db8:0:0:1:0:0:1", "2001:db8::1:0:0:1")]
    public void CanonicalFormattingFollowsRfc5952ZeroAndCaseRules(string input, string expected)
    {
        Assert.That(Fake.Create(8304).Internet().CanonicalIpv6(input), Is.EqualTo(expected));
    }

    [Test]
    public void CanonicalFormattingDoesNotAdvanceTheProviderRandomStream()
    {
        InternetProvider first = Fake.Create(8305).Internet();
        InternetProvider second = Fake.Create(8305).Internet();

        Assert.That(first.CanonicalIpv6("2001:0db8:0:0:0:0:0:1"), Is.EqualTo("2001:db8::1"));
        Assert.That(first.Ipv6(), Is.EqualTo(second.Ipv6()));
    }

    [Test]
    public void DefaultCidrsUseAlignedCategoryAppropriatePrefixes()
    {
        InternetProvider internet = NonReservedInternet(8306);

        foreach (NetworkAddressKind kind in Enum.GetValues<NetworkAddressKind>())
        {
            string cidr = internet.Ipv6CidrFromKind(kind);
            string[] pieces = cidr.Split('/');
            IPAddress network = IPAddress.Parse(pieces[0]);
            int prefixLength = int.Parse(pieces[1]);

            Assert.Multiple(() =>
            {
                Assert.That(prefixLength, Is.EqualTo(
                    kind == NetworkAddressKind.Loopback ? 128 : 64));
                Assert.That(IsAligned(network, prefixLength), Is.True);
                Assert.That(AddressMatchesKind(network, kind), Is.True);
                Assert.That(cidr, Is.EqualTo(internet.CanonicalIpv6(pieces[0]) + "/" + prefixLength));
            });
        }
    }

    [Test]
    public void CategoryBoundaryCidrsProduceCompleteNativePrefixes()
    {
        InternetProvider internet = NonReservedInternet(8307);

        string documentation = internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Documentation, 32);
        string privateCidr = internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Private, 8);
        string loopback = internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Loopback, 128);
        string linkLocal = internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.LinkLocal, 10);
        string multicast = internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Multicast, 8);
        string publicCidr = internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Public, 12);

        Assert.Multiple(() =>
        {
            Assert.That(documentation, Is.EqualTo("2001:db8::/32"));
            Assert.That(privateCidr, Is.EqualTo("fd00::/8"));
            Assert.That(loopback, Is.EqualTo("::1/128"));
            Assert.That(linkLocal, Is.EqualTo("fe80::/10"));
            Assert.That(multicast, Is.EqualTo("ff00::/8"));
            Assert.That(publicCidr, Is.AnyOf(
                "2400::/12",
                "2600::/12",
                "2800::/12",
                "2a00::/12",
                "2c00::/12"));
        });
    }

    [Test]
    public void DefaultDocumentationCidrIsDeterministicAndAligned()
    {
        InternetProvider first = Fake.Create(8308).Internet();
        InternetProvider second = Fake.Create(8308).Internet();

        int index = 0;
        while (index < 256)
        {
            string cidr = first.Ipv6Cidr();
            string[] pieces = cidr.Split('/');

            Assert.That(cidr, Is.EqualTo(second.Ipv6Cidr()));
            Assert.That(pieces[1], Is.EqualTo("64"));
            Assert.That(IsDocumentation(IPAddress.Parse(pieces[0])), Is.True);
            Assert.That(IsAligned(IPAddress.Parse(pieces[0]), 64), Is.True);
            index++;
        }
    }

    [Test]
    public void EndpointsUseDocumentationAddressesDynamicPortsAndBracketedText()
    {
        InternetProvider first = Fake.Create(8309).Internet();
        InternetProvider second = Fake.Create(8309).Internet();

        int index = 0;
        while (index < 256)
        {
            IPEndPoint endpoint = first.Ipv6Endpoint();

            Assert.That(endpoint, Is.EqualTo(second.Ipv6Endpoint()));
            Assert.That(endpoint.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(IsDocumentation(endpoint.Address), Is.True);
            Assert.That(endpoint.Port, Is.InRange(49152, 65535));
            Assert.That(endpoint.ToString(), Does.StartWith("["));
            Assert.That(endpoint.ToString(), Does.Contain("]:"));
            index++;
        }

        IPEndPoint explicitEndpoint = first.Ipv6EndpointFromKindAndPort(NetworkAddressKind.Loopback, 443);
        Assert.That(explicitEndpoint.Address, Is.EqualTo(IPAddress.IPv6Loopback));
        Assert.That(explicitEndpoint.Port, Is.EqualTo(443));
        Assert.That(explicitEndpoint.ToString(), Is.EqualTo("[::1]:443"));
    }

    [Test]
    public void InvalidCategoriesPrefixesPortsAndCanonicalInputsAreRejected()
    {
        InternetProvider internet = Fake.Create(8310).Internet();
        InternetProvider nonReserved = NonReservedInternet(8310);

        Assert.That(
            () => internet.Ipv6FromKind((NetworkAddressKind)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("kind"));
        Assert.That(
            () => internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Documentation, 31),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => internet.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Loopback, 127),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => nonReserved.Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind.Public, 129),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("prefixLength"));
        Assert.That(
            () => internet.Ipv6EndpointFromPort(-1),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));
        Assert.That(
            () => internet.Ipv6EndpointFromPort(65536),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));
        Assert.That(
            () => internet.CanonicalIpv6(null),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("address"));
        Assert.That(
            () => internet.CanonicalIpv6(" "),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("address"));
        Assert.That(
            () => internet.CanonicalIpv6("192.0.2.1"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("address"));
        Assert.That(
            () => internet.CanonicalIpv6("[2001:db8::1]"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("address"));
        Assert.That(
            () => internet.CanonicalIpv6("fe80::1%3"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("address"));
        Assert.That(
            () => internet.CanonicalIpv6("not-an-address"),
            Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Ipv6OutputsHonorConfiguredOutputLimits()
    {
        FakeOptions options = FakeOptions.Create(8311);
        options.MaximumOutputSize = 8;
        InternetProvider internet = Fake.Create(options).Internet();

        Assert.That(() => internet.Ipv6(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Ipv6Address(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Ipv6Cidr(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Ipv6Endpoint(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => internet.CanonicalIpv6("2001:db8::1"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void DocumentationModeRejectsARegisteredNonRfc3849Prefix()
    {
        Fake fake = Fake.Create(8312);
        fake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["internet.ipv6.documentation-prefix"] = "2001:db9::/32"
            });

        Assert.That(
            () => fake.Internet().Ipv6(),
            Throws.TypeOf<FakeException>().With.Message.Contains("RFC 3849"));
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
                return address.Equals(IPAddress.IPv6Loopback);
            case NetworkAddressKind.LinkLocal:
                return IsLinkLocal(address);
            case NetworkAddressKind.Multicast:
                return address.IsIPv6Multicast;
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
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 16 &&
            bytes[0] == 0x20 &&
            bytes[1] == 0x01 &&
            bytes[2] == 0x0d &&
            bytes[3] == 0xb8;
    }

    static bool IsPrivate(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 16 && bytes[0] == 0xfd;
    }

    static bool IsLinkLocal(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 16 && bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80;
    }

    static bool IsPublicShaped(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 16 &&
            (bytes[0] == 0x24 ||
             bytes[0] == 0x26 ||
             bytes[0] == 0x28 ||
             bytes[0] == 0x2a ||
             bytes[0] == 0x2c) &&
            (bytes[1] & 0xf0) == 0;
    }

    static bool IsAligned(IPAddress address, int prefixLength)
    {
        byte[] bytes = address.GetAddressBytes();
        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        int index = fullBytes;
        if (remainingBits != 0)
        {
            int hostMask = (1 << (8 - remainingBits)) - 1;
            if ((bytes[index] & hostMask) != 0)
            {
                return false;
            }
            index++;
        }
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
