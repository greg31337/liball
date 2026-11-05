namespace sip.Tests;

public class SipConnectionAliasTableTests
{
    [TestCase("Example.COM", "example.com", true)]
    [TestCase("example.com.", "EXAMPLE.COM", true)]
    [TestCase("bücher.example", "xn--bcher-kva.example", true)]
    [TestCase("proxy.example.com", "example.com", false)]
    [TestCase("*.example.com", "proxy.example.com", false)]
    [TestCase("*.example.com", "*.EXAMPLE.COM", true)]
    public void MatchesSipDomainsExactlyWithoutWildcardExpansion(string identity,
        string domain, bool expected)
    {
        Assert.That(SipCertificateDomainIdentity.MatchesDomains(
            new string[] { identity }, domain), Is.EqualTo(expected));
    }

    [Test]
    public void ClientAliasRequiresTlsTupleAndMatchingAuthenticatedIdentity()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipReliableConnectionPool pool = new SipReliableConnectionPool(clock, 4);
        SipReliableConnectionCore authenticated = Core("authenticated",
            "192.0.2.10", 5061, true, false,
            new string[] { "proxy.example.com", "virtual.example.com" });
        SipReliableConnectionCore untrusted = Core("untrusted",
            "192.0.2.11", 5061, false, false,
            new string[] { "proxy.example.com" });
        SipEndpointAddress resolved = Endpoint("192.0.2.10", 5061);
        pool.AddConnection(authenticated);
        pool.AddConnection(untrusted);

        bool added = pool.AddClientAlias(authenticated, resolved,
            "proxy.example.com");
        bool untrustedAdded = pool.AddClientAlias(untrusted,
            Endpoint("192.0.2.11", 5061), "proxy.example.com");
        SipConnectionInfo tamperedSnapshot = authenticated.GetConnectionInfo();
        tamperedSnapshot.CertificateIdentity!.ChainValid = false;
        tamperedSnapshot.CertificateIdentity.Domains[0] = "attacker.example.com";
        SipReliableConnectionCore? matched = pool.FindAliasedConnection(
            resolved, "VIRTUAL.EXAMPLE.COM");
        SipReliableConnectionCore? wrongDomain = pool.FindAliasedConnection(
            resolved, "other.example.com");
        SipReliableConnectionCore? wrongPort = pool.FindAliasedConnection(
            Endpoint("192.0.2.10", 5070), "proxy.example.com");

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(untrustedAdded, Is.False);
            Assert.That(matched, Is.SameAs(authenticated));
            Assert.That(wrongDomain, Is.Null);
            Assert.That(wrongPort, Is.Null);
            Assert.That(pool.CountAliases(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReverseAliasRequiresValuelessMarkerAndMutuallyAuthenticatedPeer()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipReliableConnectionPool pool = new SipReliableConnectionPool(clock, 4);
        SipReliableConnectionCore mutual = Core("mutual", "198.51.100.8",
            49160, true, true, new string[] { "client.example.com" });
        SipReliableConnectionCore serverOnly = Core("server-only", "198.51.100.9",
            49161, true, false, new string[] { "client.example.com" });
        pool.AddConnection(mutual);
        pool.AddConnection(serverOnly);
        SipViaValue via = AliasVia("client.example.com", -1, false);
        via.Received = "203.0.113.200";

        bool added = pool.AddServerAlias(mutual, via);
        bool withoutClientIdentity = pool.AddServerAlias(serverOnly, via);
        bool valuedAlias = pool.AddServerAlias(mutual,
            AliasVia("client.example.com", 5061, true));
        SipReliableConnectionCore? matched = pool.FindAliasedConnection(
            Endpoint("198.51.100.8", 5061), "client.example.com");
        SipReliableConnectionCore? spoofedSource = pool.FindAliasedConnection(
            Endpoint("203.0.113.200", 5061), "client.example.com");
        pool.RemoveConnection("mutual");

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(withoutClientIdentity, Is.False);
            Assert.That(valuedAlias, Is.False);
            Assert.That(matched, Is.SameAs(mutual));
            Assert.That(spoofedSource, Is.Null);
            Assert.That(pool.CountAliases(), Is.Zero);
        });
    }

    static SipReliableConnectionCore Core(string id, string remoteHost,
        int remotePort, bool chainValid, bool mutualTls, string[] domains)
    {
        SipCertificateIdentity certificate = new SipCertificateIdentity();
        certificate.ChainValid = chainValid;
        certificate.MutualTls = mutualTls;
        certificate.Domains = domains;
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tls;
        connection.LocalAddress = Endpoint("127.0.0.1", 5061);
        connection.RemoteAddress = Endpoint(remoteHost, remotePort);
        connection.Secure = true;
        connection.CertificateIdentity = certificate;
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = SipTransportKind.Tls;
        options.MaxQueueCount = 8;
        options.IdleTimeoutMilliseconds = 60000;
        return new SipReliableConnectionCore(connection, options,
            new SipParseOptions(), new SipManualMonotonicClock(0));
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = host;
        result.Port = port;
        result.Transport = SipTransportKind.Tls;
        result.Secure = true;
        return result;
    }

    static SipViaValue AliasVia(string host, int port, bool valued)
    {
        SipUriParameter alias = new SipUriParameter();
        alias.Name = "alias";
        alias.HasValue = valued;
        alias.Value = valued ? "yes" : null;
        SipViaValue result = new SipViaValue();
        result.ProtocolName = "SIP";
        result.ProtocolVersion = "2.0";
        result.Transport = "TLS";
        result.Host = host;
        result.Port = port;
        result.Parameters = new SipUriParameter[] { alias };
        return result;
    }
}
