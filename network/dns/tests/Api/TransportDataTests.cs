namespace dns.Tests.Api;

public class TransportDataTests
{
    [Test]
    public void EndpointRetainsHostAndExactAddressBytes()
    {
        DnsEndpoint endpoint = new DnsEndpoint();
        endpoint.Host = "resolver.example";
        endpoint.HasAddress = true;
        endpoint.AddressFamily = 6;
        endpoint.Address = [0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
        endpoint.Port = DnsPorts.DnsOverTls;

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Host, Is.EqualTo("resolver.example"));
            Assert.That(endpoint.AddressFamily, Is.EqualTo(6));
            Assert.That(endpoint.Address, Has.Length.EqualTo(16));
            Assert.That(endpoint.Port, Is.EqualTo(853));
        });
    }

    [Test]
    public void ClockValueRetainsIndependentWallAndMonotonicTimes()
    {
        DnsClockValue value = new DnsClockValue();
        value.UnixTimeMilliseconds = long.MaxValue;
        value.MonotonicMilliseconds = long.MinValue;

        Assert.Multiple(() =>
        {
            Assert.That(value.UnixTimeMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(value.MonotonicMilliseconds, Is.EqualTo(long.MinValue));
        });
    }

    [Test]
    public void TransportOptionsStartBoundedAndReusable()
    {
        DnsTransportOptions options = new DnsTransportOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Kind, Is.EqualTo(DnsTransportKind.Udp));
            Assert.That(options.AttemptTimeoutMilliseconds, Is.EqualTo(2000));
            Assert.That(options.TotalTimeoutMilliseconds, Is.EqualTo(5000));
            Assert.That(options.MaximumAttempts, Is.EqualTo(3));
            Assert.That(options.MaximumResponseLength,
                Is.EqualTo(DnsProtocolLimits.MaximumTcpMessageLength));
            Assert.That(options.ConnectUdpSocket, Is.True);
            Assert.That(options.ReuseConnections, Is.True);
            Assert.That(options.IdleConnectionTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(options.ConnectionLifetimeMilliseconds, Is.EqualTo(60000));
        });
    }

    [Test]
    public void ProtocolSpecificOptionsAreIndependentlyOwned()
    {
        DnsTransportOptions first = new DnsTransportOptions();
        DnsTransportOptions second = new DnsTransportOptions();
        first.Tls.AuthenticationName = "dot.example";
        first.Https.Uri = "https://doh.example/dns-query";
        first.Https.BootstrapEndpoint = new DnsEndpoint();
        first.Quic.AuthenticationName = "doq.example";

        Assert.Multiple(() =>
        {
            Assert.That(first.Tls.ValidateCertificate, Is.True);
            Assert.That(first.Tls.CheckCertificateRevocation, Is.True);
            Assert.That(first.Https.MediaType, Is.EqualTo("application/dns-message"));
            Assert.That(first.Https.UseGet, Is.False);
            Assert.That(first.Https.AllowRedirects, Is.False);
            Assert.That(first.Quic.ValidateCertificate, Is.True);
            Assert.That(first.Quic.AllowZeroRoundTripData, Is.False);
            Assert.That(second.Tls.AuthenticationName, Is.Null);
            Assert.That(second.Https.Uri, Is.Null);
            Assert.That(second.Https.BootstrapEndpoint, Is.Null);
            Assert.That(second.Quic.AuthenticationName, Is.Null);
            Assert.That(second.Tls, Is.Not.SameAs(first.Tls));
            Assert.That(second.Https, Is.Not.SameAs(first.Https));
            Assert.That(second.Quic, Is.Not.SameAs(first.Quic));
        });
    }

    [Test]
    public void TransportRequestOwnsEndpointOptionsAndDeadline()
    {
        DnsTransportRequest first = new DnsTransportRequest();
        DnsTransportRequest second = new DnsTransportRequest();
        first.QueryBytes = [0x12, 0x34];
        first.HasDeadline = true;
        first.Deadline.MonotonicMilliseconds = 5000;
        first.CancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(first.QueryBytes, Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(first.HasDeadline, Is.True);
            Assert.That(first.Deadline.MonotonicMilliseconds, Is.EqualTo(5000));
            Assert.That(first.CancellationRequested, Is.True);
            Assert.That(second.QueryBytes, Is.Empty);
            Assert.That(second.Endpoint, Is.Not.SameAs(first.Endpoint));
            Assert.That(second.Options, Is.Not.SameAs(first.Options));
            Assert.That(second.Deadline, Is.Not.SameAs(first.Deadline));
        });
    }

    [Test]
    public void TransportResultRetainsFallbackAndTimingMetadata()
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Status = DnsTransportStatus.Success;
        result.Transport = DnsTransportKind.Tcp;
        result.ResponseBytes = [0xab];
        result.StartedAt.MonotonicMilliseconds = 100;
        result.CompletedAt.MonotonicMilliseconds = 125;
        result.ElapsedMilliseconds = 25;
        result.Truncated = true;
        result.UsedFallback = true;
        result.FallbackFrom = DnsTransportKind.Udp;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(result.ResponseBytes, Is.EqualTo(new byte[] { 0xab }));
            Assert.That(result.ElapsedMilliseconds, Is.EqualTo(25));
            Assert.That(result.Truncated, Is.True);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.FallbackFrom, Is.EqualTo(DnsTransportKind.Udp));
        });
    }

    [Test]
    public void UnknownTransportAndStatusValuesRemainRepresentable()
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Transport = (DnsTransportKind)100;
        result.Status = (DnsTransportStatus)101;

        Assert.Multiple(() =>
        {
            Assert.That((int)result.Transport, Is.EqualTo(100));
            Assert.That((int)result.Status, Is.EqualTo(101));
        });
    }

    [Test]
    public void QueryRequestsOwnServersAndUseSafeDefaults()
    {
        DnsQueryRequest first = new DnsQueryRequest();
        DnsQueryRequest second = new DnsQueryRequest();
        first.Servers.Add(new DnsEndpoint());

        Assert.Multiple(() =>
        {
            Assert.That(first.RecursionDesired, Is.True);
            Assert.That(first.UseEdns, Is.True);
            Assert.That(first.RequestDnssec, Is.False);
            Assert.That(first.CheckingDisabled, Is.False);
            Assert.That(first.AllowTcpFallback, Is.True);
            Assert.That(first.AllowEdnsDowngrade, Is.True);
            Assert.That(first.Servers, Has.Count.EqualTo(1));
            Assert.That(second.Servers, Is.Empty);
            Assert.That(second.Question, Is.Not.SameAs(first.Question));
            Assert.That(second.TransportOptions, Is.Not.SameAs(first.TransportOptions));
        });
    }

    [Test]
    public void QueryResultsOwnAttemptStateAndDiagnostics()
    {
        DnsQueryResult first = new DnsQueryResult();
        DnsQueryResult second = new DnsQueryResult();
        first.Attempts.Add(new DnsTransportResult());
        first.ServerStates.Add(new DnsServerState());
        first.Diagnostics.Add(new DnsDiagnostic());
        first.ResponseBytes = [0xcd];

        Assert.Multiple(() =>
        {
            Assert.That(first.Attempts, Has.Count.EqualTo(1));
            Assert.That(first.ServerStates, Has.Count.EqualTo(1));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(first.ResponseBytes, Is.EqualTo(new byte[] { 0xcd }));
            Assert.That(second.Attempts, Is.Empty);
            Assert.That(second.ServerStates, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(second.ResponseBytes, Is.Empty);
        });
    }

    [Test]
    public void ServerStateRetainsHealthAndCapabilityObservations()
    {
        DnsServerState state = new DnsServerState();
        state.PreferredTransport = DnsTransportKind.Quic;
        state.AttemptCount = 7;
        state.ConsecutiveFailures = 2;
        state.HasSuccessfulExchange = true;
        state.LastAttempt.MonotonicMilliseconds = 1000;
        state.LastSuccess.MonotonicMilliseconds = 900;
        state.HasEdnsCapability = true;
        state.SupportsEdns = true;
        state.EdnsUdpPayloadSize = 1232;
        state.HasSuppressedUntil = true;
        state.SuppressedUntil.MonotonicMilliseconds = 2000;

        Assert.Multiple(() =>
        {
            Assert.That(state.PreferredTransport, Is.EqualTo(DnsTransportKind.Quic));
            Assert.That(state.AttemptCount, Is.EqualTo(7));
            Assert.That(state.ConsecutiveFailures, Is.EqualTo(2));
            Assert.That(state.HasSuccessfulExchange, Is.True);
            Assert.That(state.LastSuccess.MonotonicMilliseconds, Is.EqualTo(900));
            Assert.That(state.SupportsEdns, Is.True);
            Assert.That(state.EdnsUdpPayloadSize, Is.EqualTo(1232));
            Assert.That(state.SuppressedUntil.MonotonicMilliseconds, Is.EqualTo(2000));
        });
    }
}
