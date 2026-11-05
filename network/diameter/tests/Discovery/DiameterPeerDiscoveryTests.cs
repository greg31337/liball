using System.Net;

namespace diameter.Tests;

public class DiameterPeerDiscoveryTests
{
    [Test]
    public void StaticPeersTakePrecedenceAndResultsOwnTheirData()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        RecordingDiscoveryAdmissionPolicy policy =
            new RecordingDiscoveryAdmissionPolicy();
        DiameterPeerConfiguration configured = CreateStaticPeer(
            "configured.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.20");
        List<DiameterPeerConfiguration> configuredPeers =
            new List<DiameterPeerConfiguration> { configured };
        IDiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            configuredPeers,
            resolver,
            clock,
            policy);

        configured.Identity.Value = "mutated-source.example";
        configured.Endpoints[0].Address = IPAddress.Parse("192.0.2.99");
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);
        DiameterDiscoveryResult first = discovery.DiscoverDiameterPeers(request, 1000);
        first.Peers[0].Identity.Value = "mutated-result.example";
        first.Endpoints[0].Address = IPAddress.Parse("192.0.2.88");
        DiameterDiscoveryResult second = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(first.Resolved, Is.True);
            Assert.That(first.FromCache, Is.False);
            Assert.That(first.Code, Is.EqualTo("static-peer-discovery-success"));
            Assert.That(first.Peers, Has.Count.EqualTo(1));
            Assert.That(first.Peers[0].Source, Is.EqualTo(DiameterPeerSource.Static));
            Assert.That(second.Peers[0].Identity.Value,
                Is.EqualTo("configured.example"));
            Assert.That(second.Endpoints[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.20")));
            Assert.That(second.Request, Is.Not.SameAs(request));
            Assert.That(policy.Requests, Is.Empty);
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.Zero);
        });
    }

    [Test]
    public void NonMatchingStaticPeerFallsThroughToAdmittedDnsPeer()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        RecordingDiscoveryAdmissionPolicy policy =
            new RecordingDiscoveryAdmissionPolicy();
        DiameterPeerConfiguration configured = CreateStaticPeer(
            "configured.example",
            DiameterApplicationIds.BaseAccounting,
            "192.0.2.20");
        ConfigureDirectAddressDiscovery(
            resolver,
            "dynamic.example",
            "198.51.100.20",
            30);
        DiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration> { configured },
            resolver,
            clock,
            policy);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        DiameterDiscoveryResult result = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resolved, Is.True);
            Assert.That(result.Code, Is.EqualTo("dynamic-peer-discovery-success"));
            Assert.That(result.Peers, Has.Count.EqualTo(1));
            Assert.That(result.Peers[0].Identity.Value,
                Is.EqualTo("dynamic.example"));
            Assert.That(result.Peers[0].Source, Is.EqualTo(DiameterPeerSource.Dynamic));
            Assert.That(policy.Requests, Has.Count.EqualTo(1));
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(1));
        });
    }

    [Test]
    public void AdmittedDynamicPeerIsReusedUntilItsDnsExpiry()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        RecordingDiscoveryAdmissionPolicy policy =
            new RecordingDiscoveryAdmissionPolicy();
        ConfigureDirectAddressDiscovery(
            resolver,
            "dynamic.example",
            "198.51.100.30",
            30);
        IDiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            policy);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        DiameterDiscoveryResult first = discovery.DiscoverDiameterPeers(request, 1000);
        first.Peers[0].Identity.Value = "mutated.example";
        DiameterDiscoveryResult cached = discovery.DiscoverDiameterPeers(request, 1000);
        clock.AdvanceDiscoveryTime(31);
        DiameterDiscoveryResult refreshed = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(first.Peers[0].Source, Is.EqualTo(DiameterPeerSource.Dynamic));
            Assert.That(first.Peers[0].ExpiresAt,
                Is.EqualTo(new DateTimeOffset(2026, 8, 29, 18, 0, 30, TimeSpan.Zero)));
            Assert.That(first.Peers[0].RequiredApplications[0].Id,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(cached.Resolved, Is.True);
            Assert.That(cached.FromCache, Is.True);
            Assert.That(cached.Code, Is.EqualTo("dynamic-peer-cache-success"));
            Assert.That(cached.Peers[0].Identity.Value, Is.EqualTo("dynamic.example"));
            Assert.That(refreshed.Resolved, Is.True);
            Assert.That(refreshed.Code, Is.EqualTo("dynamic-peer-discovery-success"));
            Assert.That(policy.Requests, Has.Count.EqualTo(2));
            Assert.That(policy.Requests[0].PeerIdentity.Value,
                Is.EqualTo("dynamic.example"));
            Assert.That(policy.Requests[0].PeerRealm.Value, Is.EqualTo("example"));
            Assert.That(policy.Requests[0].Connection.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(policy.Requests[0].Connection.RemoteEndpoint.Address,
                Is.EqualTo(IPAddress.Parse("198.51.100.30")));
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(1));
            Assert.That(resolver.CountFakeDnsQueries(
                "dynamic.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectedDynamicPeerIsNotReturnedOrStored()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        RecordingDiscoveryAdmissionPolicy policy =
            new RecordingDiscoveryAdmissionPolicy();
        policy.RejectedIdentity = "denied.example";
        ConfigureDirectAddressDiscovery(
            resolver,
            "denied.example",
            "203.0.113.40",
            30);
        DiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            policy);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        DiameterDiscoveryResult first = discovery.DiscoverDiameterPeers(request, 1000);
        DiameterDiscoveryResult second = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(first.Resolved, Is.False);
            Assert.That(first.Negative, Is.True);
            Assert.That(first.Code, Is.EqualTo("dynamic-peer-admission-rejected"));
            Assert.That(first.Endpoints, Is.Empty);
            Assert.That(first.Peers, Is.Empty);
            Assert.That(second.Resolved, Is.False);
            Assert.That(policy.Requests, Has.Count.EqualTo(2));
            Assert.That(discovery.DynamicPeers, Is.Empty);
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidationRemovesDynamicPeersAndTheirDnsDependencies()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        ConfigureDirectAddressDiscovery(
            resolver,
            "dynamic.example",
            "203.0.113.50",
            60);
        IDiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            null);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        discovery.DiscoverDiameterPeers(request, 1000);
        DiameterDiscoveryResult cached = discovery.DiscoverDiameterPeers(request, 1000);
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        discovery.InvalidateDiameterDiscoveryCache(realm);
        DiameterDiscoveryResult refreshed = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(cached.FromCache, Is.True);
            Assert.That(refreshed.FromCache, Is.False);
            Assert.That(refreshed.Code, Is.EqualTo("dynamic-peer-discovery-success"));
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(2));
            Assert.That(resolver.CountFakeDnsQueries(
                "dynamic.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void DisabledDnsDiscoveryReturnsItsPublicOutcomeWithoutQueryingDns()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        IDiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            null);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);
        request.AllowDnsDiscovery = false;

        DiameterDiscoveryResult result = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resolved, Is.False);
            Assert.That(result.Negative, Is.True);
            Assert.That(result.FromCache, Is.False);
            Assert.That(result.Code, Is.EqualTo("dns-discovery-disabled"));
            Assert.That(result.Endpoints, Is.Empty);
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.Zero);
        });
    }

    [Test]
    public void NaptrFailureAndEmptyFallbackReturnDistinctPublicOutcomes()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver failedResolver = new FakeDnsResolver();
        DiameterDnsQueryResult failedNaptr = new DiameterDnsQueryResult();
        failedNaptr.QueryName = "example";
        failedNaptr.RecordType = DnsMessageCodec.NaptrRecordType;
        failedNaptr.ResponseCode = 2;
        failedNaptr.Code = "dns-response-error";
        failedResolver.SetFakeDnsResponse(
            "example",
            DnsMessageCodec.NaptrRecordType,
            failedNaptr);
        IDiameterPeerDiscovery failedDiscovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            failedResolver,
            clock,
            null);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        DiameterDiscoveryResult failed = failedDiscovery.DiscoverDiameterPeers(
            request,
            1000);

        FakeDnsResolver emptyResolver = new FakeDnsResolver();
        IDiameterPeerDiscovery emptyDiscovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            emptyResolver,
            clock,
            null);
        DiameterDiscoveryResult empty = emptyDiscovery.DiscoverDiameterPeers(
            request,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Resolved, Is.False);
            Assert.That(failed.Negative, Is.True);
            Assert.That(failed.Code, Is.EqualTo("dns-naptr-query-failed"));
            Assert.That(failedResolver.CountFakeDnsQueries(
                "_diameter._tcp.example",
                DnsMessageCodec.SrvRecordType), Is.Zero);
            Assert.That(empty.Resolved, Is.False);
            Assert.That(empty.Negative, Is.True);
            Assert.That(empty.Code, Is.EqualTo("dns-discovery-empty"));
            Assert.That(emptyResolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(1));
            Assert.That(emptyResolver.CountFakeDnsQueries(
                "_diameter._tcp.example",
                DnsMessageCodec.SrvRecordType), Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroTtlDiscoveryReturnsExpiredOutcomeBeforeAdmission()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        RecordingDiscoveryAdmissionPolicy policy =
            new RecordingDiscoveryAdmissionPolicy();
        ConfigureDirectAddressDiscovery(
            resolver,
            "expired.example",
            "203.0.113.70",
            0);
        DiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            policy);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        DiameterDiscoveryResult result = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resolved, Is.False);
            Assert.That(result.Negative, Is.True);
            Assert.That(result.Code, Is.EqualTo("dynamic-peer-expired"));
            Assert.That(result.ExpiresAt, Is.EqualTo(clock.UtcTime));
            Assert.That(result.Endpoints, Is.Empty);
            Assert.That(result.Peers, Is.Empty);
            Assert.That(policy.Requests, Is.Empty);
            Assert.That(discovery.DynamicPeers, Is.Empty);
        });
    }

    [Test]
    public void CacheBypassForcesDnsResolutionAndDynamicReadmission()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        RecordingDiscoveryAdmissionPolicy policy =
            new RecordingDiscoveryAdmissionPolicy();
        ConfigureDirectAddressDiscovery(
            resolver,
            "fresh.example",
            "203.0.113.80",
            30);
        IDiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            policy);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);

        DiameterDiscoveryResult first = discovery.DiscoverDiameterPeers(request, 1000);
        request.AllowCachedResults = false;
        DiameterDiscoveryResult bypassed = discovery.DiscoverDiameterPeers(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo("dynamic-peer-discovery-success"));
            Assert.That(bypassed.Resolved, Is.True);
            Assert.That(bypassed.FromCache, Is.False);
            Assert.That(bypassed.Code, Is.EqualTo("dynamic-peer-discovery-success"));
            Assert.That(policy.Requests, Has.Count.EqualTo(2));
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(2));
            Assert.That(resolver.CountFakeDnsQueries(
                "fresh.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void ConstructionAndOperationsRejectInvalidPeerDiscoveryInputs()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterPeerConfiguration dynamicPeer = CreateStaticPeer(
            "dynamic.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.60");
        dynamicPeer.Source = DiameterPeerSource.Dynamic;
        dynamicPeer.ExpiresAt = clock.UtcTime.AddMinutes(1);
        DiameterPeerConfiguration duplicateFirst = CreateStaticPeer(
            "duplicate.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.61");
        DiameterPeerConfiguration duplicateSecond = CreateStaticPeer(
            "DUPLICATE.EXAMPLE",
            DiameterApplicationIds.CreditControl,
            "192.0.2.62");
        DiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            null);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);
        DiameterIdentity invalidRealm = new DiameterIdentity();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerDiscovery(null!, resolver, clock, null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerDiscovery(
                    new List<DiameterPeerConfiguration>(),
                    null!,
                    clock,
                    null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerDiscovery(
                    new List<DiameterPeerConfiguration>(),
                    resolver,
                    null!,
                    null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerDiscovery(
                    new List<DiameterPeerConfiguration> { null! },
                    resolver,
                    clock,
                    null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerDiscovery(
                    new List<DiameterPeerConfiguration> { dynamicPeer },
                    resolver,
                    clock,
                    null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerDiscovery(
                    new List<DiameterPeerConfiguration>
                    {
                        duplicateFirst,
                        duplicateSecond
                    },
                    resolver,
                    clock,
                    null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                discovery.DiscoverDiameterPeers(request, 0));
            Assert.Throws<ArgumentException>(() =>
                discovery.DiscoverDiameterPeers(null, 1000));
            Assert.Throws<ArgumentException>(() =>
                discovery.InvalidateDiameterDiscoveryCache(null!));
            Assert.Throws<ArgumentException>(() =>
                discovery.InvalidateDiameterDiscoveryCache(invalidRealm));
        });
    }

    [Test]
    public void InternalDiscoveryBoundariesHandleExpiryFilteringAndPolicyFailures()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterPeerDiscovery discovery = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            null);
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);
        DiameterDiscoveryResult defaultExpiry = new DiameterDiscoveryResult();
        defaultExpiry.Endpoints.Add(new DiameterEndpoint());
        DiameterDiscoveryResult expired = discovery.AdmitDynamicDiscoveryPeers(
            defaultExpiry,
            request);

        DiameterPeerConfiguration cached = CreateStaticPeer(
            "cached.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.70");
        cached.Source = DiameterPeerSource.Dynamic;
        cached.ExpiresAt = clock.UtcTime;
        discovery.StoreDynamicPeer(cached);
        DiameterDiscoveryResult filtered =
            discovery.DiscoverCachedDynamicPeers(request);

        DiameterDiscoveryResult candidates = new DiameterDiscoveryResult();
        DiameterEndpoint first = new DiameterEndpoint();
        first.Identity.Value = "same.example";
        first.Host = "same.example";
        first.Address = IPAddress.Loopback;
        first.Transport = DiameterTransportKind.Tcp;
        DiameterEndpoint second = new DiameterEndpoint();
        second.Identity.Value = "SAME.EXAMPLE";
        second.Host = "same.example";
        second.Address = IPAddress.IPv6Loopback;
        second.Transport = DiameterTransportKind.Tcp;
        candidates.Endpoints.Add(first);
        candidates.Endpoints.Add(second);
        DiameterEndpoint third = new DiameterEndpoint();
        third.Identity.Value = "different.example";
        third.Host = "different.example";
        third.Address = IPAddress.Parse("192.0.2.72");
        third.Transport = DiameterTransportKind.Tcp;
        candidates.Endpoints.Add(third);
        candidates.ExpiresAt = clock.UtcTime.AddSeconds(30);
        List<DiameterPeerConfiguration> combined =
            discovery.CreateDynamicPeerCandidates(candidates, request);

        DiameterPeerConfiguration unrestricted = CreateStaticPeer(
            "unrestricted.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.71");
        unrestricted.RequiredApplications.Clear();
        Assert.Multiple(() =>
        {
            Assert.That(expired.Code, Is.EqualTo("dynamic-peer-expired"));
            Assert.That(filtered.Resolved, Is.False);
            Assert.That(combined, Has.Count.EqualTo(2));
            Assert.That(combined[0].Endpoints, Has.Count.EqualTo(2));
            Assert.That(discovery.PeerSupportsApplication(
                unrestricted,
                request.Application), Is.True);
        });

        DiameterPeerDiscovery rejecting = new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>(),
            resolver,
            clock,
            new NullDiscoveryAdmissionPolicy());
        Assert.Throws<DiameterPeerException>(() =>
            rejecting.EvaluateDynamicPeerAdmission(combined[0]));

        DiameterDiscoveryResult noMatches = new DiameterDiscoveryResult();
        DiameterPeerConfiguration disabled = CreateStaticPeer(
            "disabled.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.73");
        disabled.Enabled = false;
        discovery.AddMatchingPeerToDiscovery(
            noMatches, disabled, request, false);
        DiameterPeerConfiguration wrongRealm = CreateStaticPeer(
            "realm.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.74");
        wrongRealm.Realm.Value = "other.example";
        discovery.AddMatchingPeerToDiscovery(
            noMatches, wrongRealm, request, false);
        DiameterPeerConfiguration wrongSecurity = CreateStaticPeer(
            "security.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.75");
        wrongSecurity.Endpoints[0].Security = DiameterSecurityMode.Tls;
        discovery.AddMatchingPeerToDiscovery(
            noMatches, wrongSecurity, request, false);
        DiameterPeerConfiguration wrongTransport = CreateStaticPeer(
            "transport.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.79");
        wrongTransport.Endpoints[0].Transport = DiameterTransportKind.Sctp;
        discovery.AddMatchingPeerToDiscovery(
            noMatches, wrongTransport, request, false);
        Assert.That(noMatches.Endpoints, Is.Empty);

        DiameterPeerConfiguration later = CreateStaticPeer(
            "later.example",
            DiameterApplicationIds.CreditControl,
            "192.0.2.76");
        later.Source = DiameterPeerSource.Dynamic;
        later.ExpiresAt = clock.UtcTime.AddMinutes(2);
        DiameterDiscoveryResult retainedExpiry = new DiameterDiscoveryResult();
        retainedExpiry.ExpiresAt = clock.UtcTime.AddMinutes(1);
        discovery.AddMatchingPeerToDiscovery(
            retainedExpiry, later, request, true);
        Assert.That(retainedExpiry.ExpiresAt,
            Is.EqualTo(clock.UtcTime.AddMinutes(1)));

        Assert.DoesNotThrow(() => new DiameterPeerDiscovery(
            new List<DiameterPeerConfiguration>
            {
                CreateStaticPeer(
                    "first-distinct.example",
                    DiameterApplicationIds.CreditControl,
                    "192.0.2.77"),
                CreateStaticPeer(
                    "second-distinct.example",
                    DiameterApplicationIds.CreditControl,
                    "192.0.2.78")
            },
            resolver,
            clock,
            null));
    }

    private static DiameterPeerConfiguration CreateStaticPeer(
        string identity,
        uint applicationId,
        string address)
    {
        DiameterPeerConfiguration peer = new DiameterPeerConfiguration();
        peer.Identity.Value = identity;
        peer.Realm.Value = "example";
        peer.Source = DiameterPeerSource.Static;
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = identity;
        endpoint.Host = identity;
        endpoint.Address = IPAddress.Parse(address);
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        endpoint.Security = DiameterSecurityMode.None;
        peer.Endpoints.Add(endpoint);
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        peer.RequiredApplications.Add(application);
        return peer;
    }

    private static void ConfigureDirectAddressDiscovery(
        FakeDnsResolver resolver,
        string host,
        string address,
        uint timeToLive)
    {
        DiameterDnsQueryResult naptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example",
            DnsMessageCodec.NaptrRecordType);
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            10,
            1,
            "A",
            "aaa+ap4:diameter.tcp",
            host,
            60));
        resolver.SetFakeDnsResponse("example", DnsMessageCodec.NaptrRecordType, naptr);

        DiameterDnsQueryResult ipv4 = DiscoveryTestData.CreateSuccessfulDnsResult(
            host,
            DnsMessageCodec.Ipv4RecordType);
        ipv4.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            host,
            address,
            timeToLive));
        resolver.SetFakeDnsResponse(host, DnsMessageCodec.Ipv4RecordType, ipv4);
    }
}

internal class NullDiscoveryAdmissionPolicy : IPeerAdmissionPolicy
{
    public DiameterPeerAdmissionResult EvaluatePeerAdmission(
        DiameterPeerAdmissionRequest request)
    {
        return null!;
    }
}
