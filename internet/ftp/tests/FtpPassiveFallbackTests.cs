using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpPassiveFallbackTests
{
    [TestCase(500)]
    [TestCase(501)]
    [TestCase(502)]
    [TestCase(504)]
    [TestCase(522)]
    public void Standard_EPSV_unsupported_replies_enable_IPV4_PASV_fallback(int replyCode)
    {
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions());

        FtpCommand command = connector.CreatePassiveFallbackCommand(
            CreateReply(replyCode, "EPSV unavailable"));

        Assert.Multiple(() =>
        {
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Pasv));
            Assert.That(command.Verb, Is.EqualTo("PASV"));
            Assert.That(command.Argument, Is.Null);
        });
    }

    [Test]
    public void PASV_fallback_rejects_nonstandard_failures_disabled_policy_and_IPV6()
    {
        FtpPassiveConnector ipv4 = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions());
        FtpDataConnectionOptions disabledOptions = CreateOptions();
        disabledOptions.AllowPassiveFallback = false;
        FtpPassiveConnector disabled = new FtpPassiveConnector(
            IPAddress.Loopback, disabledOptions);
        FtpPassiveConnector ipv6 = new FtpPassiveConnector(
            IPAddress.IPv6Loopback, CreateOptions());
        FtpDataConnectionOptions ipv6PasvOnly = CreateOptions();
        ipv6PasvOnly.UseExtendedPassive = false;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                ipv4.CreatePassiveFallbackCommand(CreateReply(229, "EPSV succeeded")));
            Assert.Throws<FtpProtocolException>(() =>
                ipv4.CreatePassiveFallbackCommand(CreateReply(425, "data setup failed")));
            Assert.Throws<FtpProtocolException>(() =>
                ipv4.CreatePassiveFallbackCommand(CreateReply(421, "control closing")));
            Assert.Throws<FtpProtocolException>(() =>
                disabled.CreatePassiveFallbackCommand(CreateReply(500, "unsupported")));
            Assert.Throws<FtpProtocolException>(() =>
                ipv6.CreatePassiveFallbackCommand(CreateReply(500, "unsupported")));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveConnector(IPAddress.IPv6Loopback, ipv6PasvOnly));
        });
    }

    [Test]
    public void Successful_EPSV_ALL_state_prohibits_PASV_fallback()
    {
        FtpDataConnectionOptions options = CreateOptions();
        options.SendExtendedPassiveAll = true;
        options.AllowPassiveFallback = false;
        FtpPassiveConnector connector = new FtpPassiveConnector(IPAddress.Loopback, options);
        connector.CreateExtendedPassiveAllCommand();
        connector.ApplyExtendedPassiveAllReply(CreateReply(200, "EPSV only"));

        Assert.Multiple(() =>
        {
            Assert.That(connector.ExtendedPassiveOnly, Is.True);
            Assert.Throws<FtpProtocolException>(() => connector.CreatePassiveCommand());
            Assert.Throws<FtpProtocolException>(() =>
                connector.CreatePassiveFallbackCommand(CreateReply(500, "unsupported")));
        });
    }

    [Test]
    public void Default_PASV_policy_ignores_advertised_address_and_connects_to_control_peer()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        FtpReply reply = CreatePassiveReply(IPAddress.Parse("203.0.113.77"), port);
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions());

        FtpPassiveConnection result = connector.OpenPassiveConnection(
            reply, 5000, default(CancellationToken));
        using Socket accepted = listener.AcceptSocket();
        using Socket connection = result.Connection!;
        IPEndPoint remote = (IPEndPoint)connection.RemoteEndPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Extended, Is.False);
            Assert.That(result.UsedControlPeerAddress, Is.True);
            Assert.That(result.Endpoint.Host, Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(result.Endpoint.Port, Is.EqualTo(port));
            Assert.That(remote.Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(accepted.Connected, Is.True);
        });
    }

    [Test]
    public void Trusted_PASV_policy_uses_a_safe_advertised_address_when_peer_match_is_disabled()
    {
        FtpDataConnectionOptions options = CreateTrustedOptions();
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Parse("10.0.0.1"), options);
        FtpEndpoint advertised = CreateEndpoint(IPAddress.Parse("10.0.0.2"), 50000);

        IPAddress selected = connector.SelectPassiveAddress(advertised);

        Assert.That(selected, Is.EqualTo(IPAddress.Parse("10.0.0.2")));
    }

    [Test]
    public void Exact_peer_match_rejects_a_different_trusted_PASV_address()
    {
        FtpDataConnectionOptions options = CreateTrustedOptions();
        options.RequireDataPeerMatch = true;
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Parse("10.0.0.1"), options);
        FtpEndpoint advertised = CreateEndpoint(IPAddress.Parse("10.0.0.2"), 50000);

        Assert.Throws<FtpConnectionException>(() => connector.SelectPassiveAddress(advertised));
    }

    [Test]
    public void NAT_boundary_policy_rejects_private_public_mismatches_unless_disabled()
    {
        FtpDataConnectionOptions strictOptions = CreateTrustedOptions();
        FtpPassiveConnector strictPublicControl = new FtpPassiveConnector(
            IPAddress.Parse("198.51.100.10"), strictOptions);
        FtpPassiveConnector strictPrivateControl = new FtpPassiveConnector(
            IPAddress.Parse("10.0.0.10"), strictOptions);
        FtpEndpoint privateAdvertised = CreateEndpoint(IPAddress.Parse("10.0.0.20"), 50000);
        FtpEndpoint publicAdvertised = CreateEndpoint(
            IPAddress.Parse("198.51.100.20"), 50000);
        FtpDataConnectionOptions permissiveOptions = CreateTrustedOptions();
        permissiveOptions.RejectPassiveAddressMismatch = false;
        FtpPassiveConnector permissive = new FtpPassiveConnector(
            IPAddress.Parse("198.51.100.10"), permissiveOptions);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConnectionException>(() =>
                strictPublicControl.SelectPassiveAddress(privateAdvertised));
            Assert.Throws<FtpConnectionException>(() =>
                strictPrivateControl.SelectPassiveAddress(publicAdvertised));
            Assert.That(permissive.SelectPassiveAddress(privateAdvertised),
                Is.EqualTo(IPAddress.Parse("10.0.0.20")));
        });
    }

    [TestCase(true, true, true, "10.0.0.20", "control")]
    [TestCase(false, true, false, "198.51.100.10", "advertised")]
    [TestCase(false, true, false, "198.51.100.20", "rejected")]
    [TestCase(false, false, true, "10.0.0.20", "rejected")]
    [TestCase(false, false, false, "10.0.0.20", "advertised")]
    [TestCase(false, false, true, "198.51.100.20", "advertised")]
    public void PASV_address_trust_modes_apply_ignore_peer_and_NAT_policies_in_order(
        bool ignoreAdvertised,
        bool requirePeerMatch,
        bool rejectNatMismatch,
        string advertisedText,
        string expected)
    {
        IPAddress controlAddress = IPAddress.Parse("198.51.100.10");
        IPAddress advertisedAddress = IPAddress.Parse(advertisedText);
        FtpDataConnectionOptions options = CreateOptions();
        options.IgnorePassiveAdvertisedAddress = ignoreAdvertised;
        options.RequireDataPeerMatch = requirePeerMatch;
        options.RejectPassiveAddressMismatch = rejectNatMismatch;
        FtpPassiveConnector connector = new FtpPassiveConnector(
            controlAddress, options);
        FtpEndpoint endpoint = CreateEndpoint(advertisedAddress, 50000);

        if (expected == "rejected")
        {
            Assert.Throws<FtpConnectionException>(() =>
                connector.SelectPassiveAddress(endpoint));
            return;
        }

        IPAddress selected = connector.SelectPassiveAddress(endpoint);
        IPAddress expectedAddress = expected == "control" ?
            controlAddress : advertisedAddress;
        Assert.That(selected, Is.EqualTo(expectedAddress));
    }

    [TestCase("0.0.0.0")]
    [TestCase("224.0.0.1")]
    [TestCase("239.255.255.255")]
    [TestCase("255.255.255.255")]
    public void Trusted_PASV_policy_never_selects_prohibited_targets(string addressText)
    {
        FtpDataConnectionOptions options = CreateTrustedOptions();
        options.RejectPassiveAddressMismatch = false;
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Parse("198.51.100.10"), options);
        FtpEndpoint advertised = CreateEndpoint(IPAddress.Parse(addressText), 50000);

        Assert.Throws<FtpConnectionException>(() => connector.SelectPassiveAddress(advertised));
    }

    [Test]
    public void Ignored_prohibited_advertisement_selects_only_the_safe_control_peer()
    {
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions());
        FtpEndpoint advertised = CreateEndpoint(IPAddress.Any, 50000);

        IPAddress selected = connector.SelectPassiveAddress(advertised);

        Assert.That(selected, Is.EqualTo(IPAddress.Loopback));
    }

    [Test]
    public void Malformed_PASV_reply_is_rejected_before_address_selection_or_connection()
    {
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions());
        FtpReply malformed = CreateReply(227, "Entering Passive Mode (1,2,3,999,4,5)");

        Assert.Throws<FtpProtocolException>(() =>
            connector.OpenPassiveConnection(malformed, 5000, default(CancellationToken)));
    }

    private static FtpDataConnectionOptions CreateOptions()
    {
        return new FtpDataConnectionOptions();
    }

    private static FtpDataConnectionOptions CreateTrustedOptions()
    {
        FtpDataConnectionOptions options = CreateOptions();
        options.IgnorePassiveAdvertisedAddress = false;
        options.RequireDataPeerMatch = false;
        return options;
    }

    private static FtpEndpoint CreateEndpoint(IPAddress address, int port)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = address.ToString();
        endpoint.Port = port;
        endpoint.AddressFamily = FtpAddressFamily.IPv4;
        return endpoint;
    }

    private static FtpReply CreatePassiveReply(IPAddress address, int port)
    {
        return FtpEndpointCodec.CreatePassiveReply(CreateEndpoint(address, port));
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
