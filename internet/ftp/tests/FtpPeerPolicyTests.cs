using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpPeerPolicyTests
{
    [Test]
    public void IPv4_mapped_IPv6_normalizes_to_native_IPv4_for_identity_and_family()
    {
        IPAddress ipv4 = IPAddress.Parse("192.0.2.25");
        IPAddress mapped = ipv4.MapToIPv6();
        IPAddress nativeIpv6 = IPAddress.Parse("2001:db8::25");

        IPAddress normalized = FtpPeerPolicy.NormalizeAddress(mapped);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo(ipv4));
            Assert.That(normalized.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(FtpPeerPolicy.GetAddressFamily(mapped),
                Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(FtpPeerPolicy.IsSameAddress(mapped, ipv4), Is.True);
            Assert.That(FtpPeerPolicy.IsAddressFamilyMatch(mapped, ipv4), Is.True);
            Assert.That(FtpPeerPolicy.IsAddressFamilyMatch(mapped, nativeIpv6), Is.False);
            Assert.That(FtpPeerPolicy.IsSameAddress(mapped, nativeIpv6), Is.False);
        });
    }

    [Test]
    public void Scope_identifiers_are_retained_only_for_scoped_IPv6_addresses()
    {
        IPAddress global = IPAddress.Parse("2001:db8::10");
        IPAddress globalScopeThree = new IPAddress(global.GetAddressBytes(), 3);
        IPAddress globalScopeFour = new IPAddress(global.GetAddressBytes(), 4);
        IPAddress linkLocal = IPAddress.Parse("fe80::10");
        IPAddress linkScopeThree = new IPAddress(linkLocal.GetAddressBytes(), 3);
        IPAddress linkScopeFour = new IPAddress(linkLocal.GetAddressBytes(), 4);
        IPAddress multicast = IPAddress.Parse("ff02::1");
        IPAddress multicastScoped = new IPAddress(multicast.GetAddressBytes(), 7);

        Assert.Multiple(() =>
        {
            Assert.That(FtpPeerPolicy.NormalizeAddress(globalScopeThree).ScopeId, Is.Zero);
            Assert.That(FtpPeerPolicy.IsSameAddress(
                globalScopeThree, globalScopeFour), Is.True);
            Assert.That(FtpPeerPolicy.NormalizeAddress(linkScopeThree).ScopeId,
                Is.EqualTo(3));
            Assert.That(FtpPeerPolicy.IsSameAddress(
                linkScopeThree, linkScopeFour), Is.False);
            Assert.That(FtpPeerPolicy.IsAddressFamilyMatch(
                linkScopeThree, linkScopeFour), Is.True);
            Assert.That(FtpPeerPolicy.NormalizeAddress(multicastScoped).ScopeId,
                Is.EqualTo(7));
        });
    }

    [Test]
    public void IPv4_classification_covers_loopback_private_shared_link_and_multicast_ranges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpPeerPolicy.IsUnspecifiedAddress(IPAddress.Any), Is.True);
            Assert.That(FtpPeerPolicy.IsLoopbackAddress(
                IPAddress.Parse("127.255.1.2")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("10.255.255.255")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("172.16.0.1")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("172.31.255.255")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("172.32.0.1")), Is.False);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("192.168.10.1")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("100.64.0.1")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("100.128.0.1")), Is.False);
            Assert.That(FtpPeerPolicy.IsLinkLocalAddress(
                IPAddress.Parse("169.254.20.1")), Is.True);
            Assert.That(FtpPeerPolicy.IsMulticastAddress(
                IPAddress.Parse("239.255.255.255")), Is.True);
            Assert.That(FtpPeerPolicy.IsMulticastAddress(
                IPAddress.Parse("240.0.0.1")), Is.False);
            Assert.That(FtpPeerPolicy.IsPrivateOrLocalAddress(
                IPAddress.Parse("198.51.100.20")), Is.False);
        });
    }

    [TestCase("9.255.255.255", false)]
    [TestCase("10.0.0.0", true)]
    [TestCase("10.255.255.255", true)]
    [TestCase("11.0.0.0", false)]
    [TestCase("100.63.255.255", false)]
    [TestCase("100.64.0.0", true)]
    [TestCase("100.127.255.255", true)]
    [TestCase("100.128.0.0", false)]
    [TestCase("172.15.255.255", false)]
    [TestCase("172.16.0.0", true)]
    [TestCase("172.31.255.255", true)]
    [TestCase("172.32.0.0", false)]
    [TestCase("192.167.255.255", false)]
    [TestCase("192.168.0.0", true)]
    [TestCase("192.168.255.255", true)]
    [TestCase("192.169.0.0", false)]
    [TestCase("fbff:ffff:ffff:ffff::1", false)]
    [TestCase("fc00::", true)]
    [TestCase("fdff:ffff:ffff:ffff::1", true)]
    [TestCase("fe00::", false)]
    public void Private_network_classification_honors_exact_CIDR_boundaries(
        string addressText,
        bool expected)
    {
        IPAddress address = IPAddress.Parse(addressText);

        Assert.That(FtpPeerPolicy.IsPrivateAddress(address), Is.EqualTo(expected));
    }

    [Test]
    public void IPv6_classification_covers_loopback_unique_site_link_multicast_and_global()
    {
        IPAddress global = IPAddress.Parse("2001:db8::20");

        Assert.Multiple(() =>
        {
            Assert.That(FtpPeerPolicy.IsUnspecifiedAddress(IPAddress.IPv6Any), Is.True);
            Assert.That(FtpPeerPolicy.IsLoopbackAddress(IPAddress.IPv6Loopback), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("fc00::1")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("fdff:ffff::1")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("fec0::1")), Is.True);
            Assert.That(FtpPeerPolicy.IsLinkLocalAddress(
                IPAddress.Parse("fe80::1")), Is.True);
            Assert.That(FtpPeerPolicy.IsMulticastAddress(
                IPAddress.Parse("ff05::1")), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(global), Is.False);
            Assert.That(FtpPeerPolicy.IsLinkLocalAddress(global), Is.False);
            Assert.That(FtpPeerPolicy.IsMulticastAddress(global), Is.False);
            Assert.That(FtpPeerPolicy.IsPrivateOrLocalAddress(global), Is.False);
        });
    }

    [Test]
    public void Mapped_addresses_use_their_IPv4_classification()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpPeerPolicy.IsUnspecifiedAddress(
                IPAddress.Any.MapToIPv6()), Is.True);
            Assert.That(FtpPeerPolicy.IsLoopbackAddress(
                IPAddress.Loopback.MapToIPv6()), Is.True);
            Assert.That(FtpPeerPolicy.IsPrivateAddress(
                IPAddress.Parse("10.0.0.1").MapToIPv6()), Is.True);
            Assert.That(FtpPeerPolicy.IsLinkLocalAddress(
                IPAddress.Parse("169.254.1.1").MapToIPv6()), Is.True);
            Assert.That(FtpPeerPolicy.IsMulticastAddress(
                IPAddress.Parse("224.0.0.1").MapToIPv6()), Is.True);
        });
    }

    [Test]
    public void Target_filters_allow_routable_addresses_and_block_unspecified_multicast_and_reserved()
    {
        IPAddress mappedPublic = IPAddress.Parse("198.51.100.25").MapToIPv6();
        IPAddress ipv6Global = IPAddress.Parse("2001:db8::25");

        Assert.Multiple(() =>
        {
            Assert.That(FtpPeerPolicy.IsProhibitedPassiveTarget(mappedPublic), Is.False);
            Assert.That(FtpPeerPolicy.IsProhibitedActiveTarget(mappedPublic), Is.False);
            Assert.That(FtpPeerPolicy.IsProhibitedActiveTarget(ipv6Global), Is.False);
            Assert.That(FtpPeerPolicy.IsProhibitedAdvertisedAddress(ipv6Global), Is.False);
            Assert.That(FtpPeerPolicy.IsProhibitedPassiveTarget(IPAddress.Any), Is.True);
            Assert.That(FtpPeerPolicy.IsProhibitedActiveTarget(IPAddress.IPv6Any), Is.True);
            Assert.That(FtpPeerPolicy.IsProhibitedActiveTarget(
                IPAddress.Parse("224.0.0.1")), Is.True);
            Assert.That(FtpPeerPolicy.IsProhibitedActiveTarget(
                IPAddress.Parse("240.0.0.1")), Is.True);
            Assert.That(FtpPeerPolicy.IsProhibitedAdvertisedAddress(
                IPAddress.Parse("0.1.2.3")), Is.True);
            Assert.That(FtpPeerPolicy.IsProhibitedAdvertisedAddress(
                IPAddress.Parse("ff02::1")), Is.True);
        });
    }

    [Test]
    public void Connectors_store_normalized_control_peers_and_match_native_PORT_targets()
    {
        IPAddress mappedLoopback = IPAddress.Loopback.MapToIPv6();
        FtpDataConnectionOptions passiveOptions = new FtpDataConnectionOptions();
        FtpPassiveConnector passive = new FtpPassiveConnector(
            mappedLoopback, passiveOptions);
        FtpDataConnectionOptions activeOptions = new FtpDataConnectionOptions();
        activeOptions.Mode = FtpDataConnectionMode.Active;
        FtpServerActiveConnector active = new FtpServerActiveConnector(
            mappedLoopback, activeOptions);
        FtpActiveConnector clientActive = new FtpActiveConnector(
            mappedLoopback, IPAddress.Loopback, activeOptions);

        FtpReply reply = active.ConfigurePort("127,0,0,1,195,80");

        Assert.Multiple(() =>
        {
            Assert.That(passive.ControlPeerAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(passive.ControlPeerFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(active.ControlPeerAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(active.ControlFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(clientActive.ControlPeerAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(clientActive.ControlLocalAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(clientActive.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
        });
    }

    [Test]
    public void Mapped_EPRT_address_is_rejected_when_it_conflicts_with_declared_IPv6_protocol()
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.Mode = FtpDataConnectionMode.Active;
        FtpServerActiveConnector connector = new FtpServerActiveConnector(
            IPAddress.Loopback, options);

        Assert.Throws<FtpProtocolException>(() =>
            connector.ConfigureExtendedPort("|2|::ffff:127.0.0.1|50000|"));
    }
}
