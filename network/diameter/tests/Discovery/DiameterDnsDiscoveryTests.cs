using System.Net;

namespace diameter.Tests;

public class DiameterDnsDiscoveryTests
{
    [Test]
    public void ExtendedNaptrSrvAndAddressQueriesProduceOrderedBoundedEndpoints()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);
        request.MaximumResults = 3;

        DiameterDnsQueryResult naptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example",
            DnsMessageCodec.NaptrRecordType);
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            5, 1, "S", "aaa+ap1:diameter.tcp", "other.example", 120));
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            10, 1, "S", "aaa+ap4:diameter.tcp", "_diameter._tcp.example", 120));
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            20, 1, "A", "aaa+ap4:diameter.tcp", "unused.example", 120));
        resolver.SetFakeDnsResponse("example", DnsMessageCodec.NaptrRecordType, naptr);

        DiameterDnsQueryResult srv = DiscoveryTestData.CreateSuccessfulDnsResult(
            "_diameter._tcp.example",
            DnsMessageCodec.SrvRecordType);
        srv.SrvRecords.Add(DiscoveryTestData.CreateSrvRecord(
            0, 1, 3868, "first.example", 60));
        srv.SrvRecords.Add(DiscoveryTestData.CreateSrvRecord(
            0, 3, 3869, "preferred.example", 60));
        srv.SrvRecords.Add(DiscoveryTestData.CreateSrvRecord(
            1, 100, 3870, "later.example", 60));
        resolver.SetFakeDnsResponse(
            "_diameter._tcp.example",
            DnsMessageCodec.SrvRecordType,
            srv);

        DiameterDnsQueryResult preferredIpv4 =
            DiscoveryTestData.CreateSuccessfulDnsResult(
                "preferred.example",
                DnsMessageCodec.Ipv4RecordType);
        preferredIpv4.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "preferred.example", "192.0.2.2", 20));
        resolver.SetFakeDnsResponse(
            "preferred.example",
            DnsMessageCodec.Ipv4RecordType,
            preferredIpv4);

        DiameterDnsQueryResult preferredIpv6 =
            DiscoveryTestData.CreateSuccessfulDnsResult(
                "preferred.example",
                DnsMessageCodec.Ipv6RecordType);
        preferredIpv6.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "preferred.example", "2001:db8::2", 25));
        resolver.SetFakeDnsResponse(
            "preferred.example",
            DnsMessageCodec.Ipv6RecordType,
            preferredIpv6);

        DiameterDnsQueryResult firstIpv4 = DiscoveryTestData.CreateSuccessfulDnsResult(
            "first.example",
            DnsMessageCodec.Ipv4RecordType);
        firstIpv4.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "first.example", "192.0.2.1", 10));
        resolver.SetFakeDnsResponse(
            "first.example",
            DnsMessageCodec.Ipv4RecordType,
            firstIpv4);

        DiameterDnsDiscovery discovery = new DiameterDnsDiscovery(
            resolver,
            clock,
            new SequenceDiscoveryRandom(4, 0, 0, 4, 0, 0));

        DiameterDiscoveryResult first = discovery.ResolveDnsDiscovery(request, 1000);
        first.NaptrRecords[0].Service = "mutated";
        DiameterDiscoveryResult second = discovery.ResolveDnsDiscovery(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(first.Resolved, Is.True);
            Assert.That(first.Negative, Is.False);
            Assert.That(first.FromCache, Is.False);
            Assert.That(first.Endpoints, Has.Count.EqualTo(3));
            Assert.That(first.Endpoints[0].Host, Is.EqualTo("preferred.example"));
            Assert.That(first.Endpoints[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.2")));
            Assert.That(first.Endpoints[0].Port, Is.EqualTo(3869));
            Assert.That(first.Endpoints[1].Address,
                Is.EqualTo(IPAddress.Parse("2001:db8::2")));
            Assert.That(first.Endpoints[2].Host, Is.EqualTo("first.example"));
            Assert.That(first.Endpoints[2].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.1")));
            Assert.That(first.Endpoints[2].Port, Is.EqualTo(3868));
            Assert.That(first.ExpiresAt, Is.EqualTo(clock.UtcTime.AddSeconds(10)));
            Assert.That(first.Code, Is.EqualTo("dns-discovery-success"));
            Assert.That(second.FromCache, Is.True);
            Assert.That(second.NaptrRecords[0].Service, Is.EqualTo("aaa+ap1:diameter.tcp"));
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(1));
            Assert.That(resolver.CountFakeDnsQueries(
                "unused.example", DnsMessageCodec.Ipv4RecordType), Is.Zero);
            Assert.That(resolver.CountFakeDnsQueries(
                "later.example", DnsMessageCodec.Ipv4RecordType), Is.Zero);
        });
    }

    [Test]
    public void NaptrAddressFlagUsesSecureDefaultPortWithoutSrvLookup()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.Tls);

        DiameterDnsQueryResult naptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example",
            DnsMessageCodec.NaptrRecordType);
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            10, 1, "A", "aaa+ap4:diameter.tls.tcp", "secure.example", 60));
        resolver.SetFakeDnsResponse("example", DnsMessageCodec.NaptrRecordType, naptr);

        DiameterDnsQueryResult addresses = DiscoveryTestData.CreateSuccessfulDnsResult(
            "secure.example",
            DnsMessageCodec.Ipv4RecordType);
        addresses.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "secure.example", "198.51.100.8", 30));
        resolver.SetFakeDnsResponse(
            "secure.example",
            DnsMessageCodec.Ipv4RecordType,
            addresses);

        DiameterDnsDiscovery discovery = new DiameterDnsDiscovery(
            resolver,
            clock,
            new SequenceDiscoveryRandom(0));
        DiameterDiscoveryResult result = discovery.ResolveDnsDiscovery(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resolved, Is.True);
            Assert.That(result.Endpoints, Has.Count.EqualTo(1));
            Assert.That(result.Endpoints[0].Port,
                Is.EqualTo(DiameterProtocolConstants.SecurePort));
            Assert.That(result.Endpoints[0].Transport,
                Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(result.Endpoints[0].Security,
                Is.EqualTo(DiameterSecurityMode.Tls));
            Assert.That(resolver.CountFakeDnsQueries(
                "_diameters._tcp.example",
                DnsMessageCodec.SrvRecordType), Is.Zero);
            Assert.That(resolver.CountFakeDnsQueries(
                "secure.example", DnsMessageCodec.Ipv6RecordType), Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingNaptrRecordsUseTransportSpecificSrvFallback()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.Dtls);

        DiameterDnsQueryResult naptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example",
            DnsMessageCodec.NaptrRecordType);
        resolver.SetFakeDnsResponse("example", DnsMessageCodec.NaptrRecordType, naptr);

        DiameterDnsQueryResult srv = DiscoveryTestData.CreateSuccessfulDnsResult(
            "_diameters._sctp.example",
            DnsMessageCodec.SrvRecordType);
        srv.SrvRecords.Add(DiscoveryTestData.CreateSrvRecord(
            0, 0, 5658, "dtls.example", 60));
        resolver.SetFakeDnsResponse(
            "_diameters._sctp.example",
            DnsMessageCodec.SrvRecordType,
            srv);

        DiameterDnsQueryResult addresses = DiscoveryTestData.CreateSuccessfulDnsResult(
            "dtls.example",
            DnsMessageCodec.Ipv4RecordType);
        addresses.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "dtls.example", "203.0.113.9", 60));
        resolver.SetFakeDnsResponse(
            "dtls.example",
            DnsMessageCodec.Ipv4RecordType,
            addresses);

        DiameterDnsDiscovery discovery = new DiameterDnsDiscovery(
            resolver,
            clock,
            new SequenceDiscoveryRandom(0));
        DiameterDiscoveryResult result = discovery.ResolveDnsDiscovery(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resolved, Is.True);
            Assert.That(result.Endpoints[0].Port, Is.EqualTo(5658));
            Assert.That(result.Endpoints[0].Transport,
                Is.EqualTo(DiameterTransportKind.Sctp));
            Assert.That(result.Endpoints[0].Security,
                Is.EqualTo(DiameterSecurityMode.Dtls));
            Assert.That(resolver.CountFakeDnsQueries(
                "_diameters._sctp.example",
                DnsMessageCodec.SrvRecordType), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExtendedApplicationMismatchAbandonsLegacyAndSrvFallback()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.None);
        DiameterDnsQueryResult naptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example",
            DnsMessageCodec.NaptrRecordType);
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            1, 1, "S", "aaa:diameter.tcp", "legacy.example", 60));
        naptr.NaptrRecords.Add(DiscoveryTestData.CreateNaptrRecord(
            2, 1, "S", "aaa+ap1:diameter.tcp", "other.example", 60));
        resolver.SetFakeDnsResponse("example", DnsMessageCodec.NaptrRecordType, naptr);

        DiameterDnsDiscovery discovery = new DiameterDnsDiscovery(
            resolver,
            clock,
            new SequenceDiscoveryRandom(0));
        DiameterDiscoveryResult result = discovery.ResolveDnsDiscovery(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resolved, Is.False);
            Assert.That(result.Negative, Is.True);
            Assert.That(result.Code, Is.EqualTo("dns-naptr-no-compatible-service"));
            Assert.That(resolver.CountFakeDnsQueries(
                "_diameter._tcp.example",
                DnsMessageCodec.SrvRecordType), Is.Zero);
            Assert.That(resolver.CountFakeDnsQueries(
                "legacy.example", DnsMessageCodec.SrvRecordType), Is.Zero);
        });
    }

    [Test]
    public void CacheHonorsPositiveAndNegativeTtlBypassesAndInvalidation()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiscoveryCache cache = new DiscoveryCache(clock, 60);

        DiameterDnsQueryResult negative = new DiameterDnsQueryResult();
        negative.QueryName = "missing.example";
        negative.RecordType = DnsMessageCodec.Ipv4RecordType;
        negative.ResponseCode = 3;
        negative.Code = "dns-name-error";
        resolver.SetFakeDnsResponse(
            "missing.example",
            DnsMessageCodec.Ipv4RecordType,
            negative);

        DnsLookupResult first = cache.ResolveCachedDnsQuery(
            resolver, "missing.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        first.Result.Code = "mutated";
        DnsLookupResult second = cache.ResolveCachedDnsQuery(
            resolver, "MISSING.EXAMPLE.", DnsMessageCodec.Ipv4RecordType, 1000, true);
        clock.AdvanceDiscoveryTime(61);
        DnsLookupResult expired = cache.ResolveCachedDnsQuery(
            resolver, "missing.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        cache.InvalidateDnsName("missing.example");
        DnsLookupResult invalidated = cache.ResolveCachedDnsQuery(
            resolver, "missing.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        DnsLookupResult bypassed = cache.ResolveCachedDnsQuery(
            resolver, "missing.example", DnsMessageCodec.Ipv4RecordType, 1000, false);

        DiameterDnsQueryResult positive = DiscoveryTestData.CreateSuccessfulDnsResult(
            "positive.example",
            DnsMessageCodec.Ipv4RecordType);
        positive.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "positive.example", "192.0.2.44", 10));
        resolver.SetFakeDnsResponse(
            "positive.example",
            DnsMessageCodec.Ipv4RecordType,
            positive);
        cache.ResolveCachedDnsQuery(
            resolver, "positive.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        DnsLookupResult positiveCached = cache.ResolveCachedDnsQuery(
            resolver, "positive.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        clock.AdvanceDiscoveryTime(11);
        DnsLookupResult positiveExpired = cache.ResolveCachedDnsQuery(
            resolver, "positive.example", DnsMessageCodec.Ipv4RecordType, 1000, true);

        Assert.Multiple(() =>
        {
            Assert.That(first.FromCache, Is.False);
            Assert.That(first.ExpiresAt, Is.EqualTo(
                new DateTimeOffset(2026, 8, 29, 18, 1, 0, TimeSpan.Zero)));
            Assert.That(second.FromCache, Is.True);
            Assert.That(second.Result.Code, Is.EqualTo("dns-name-error"));
            Assert.That(expired.FromCache, Is.False);
            Assert.That(invalidated.FromCache, Is.False);
            Assert.That(bypassed.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "missing.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(4));
            Assert.That(positiveCached.FromCache, Is.True);
            Assert.That(positiveExpired.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "positive.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void DiscoveryCacheInvalidationForcesFreshDependencyQueries()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Tcp, DiameterSecurityMode.None);
        DiameterDnsQueryResult naptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example", DnsMessageCodec.NaptrRecordType);
        resolver.SetFakeDnsResponse("example", DnsMessageCodec.NaptrRecordType, naptr);
        DiameterDnsQueryResult srv = DiscoveryTestData.CreateSuccessfulDnsResult(
            "_diameter._tcp.example", DnsMessageCodec.SrvRecordType);
        resolver.SetFakeDnsResponse(
            "_diameter._tcp.example", DnsMessageCodec.SrvRecordType, srv);
        DiameterDnsDiscovery discovery = new DiameterDnsDiscovery(
            resolver, clock, new SequenceDiscoveryRandom(0));

        discovery.ResolveDnsDiscovery(request, 1000);
        DiameterDiscoveryResult cached = discovery.ResolveDnsDiscovery(request, 1000);
        discovery.InvalidateDiscoveryCache("example");
        DiameterDiscoveryResult refreshed = discovery.ResolveDnsDiscovery(request, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(cached.FromCache, Is.True);
            Assert.That(refreshed.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(2));
            Assert.That(resolver.CountFakeDnsQueries(
                "_diameter._tcp.example", DnsMessageCodec.SrvRecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void NegativeCacheUsesAuthoritativeSoaLifetimeWhenAvailable()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiscoveryCache cache = new DiscoveryCache(clock, 60);
        DiameterDnsQueryResult negative = new DiameterDnsQueryResult();
        negative.QueryName = "short.example";
        negative.RecordType = DnsMessageCodec.Ipv4RecordType;
        negative.ResponseCode = 3;
        negative.NegativeTimeToLiveSeconds = 5;
        resolver.SetFakeDnsResponse(
            "short.example",
            DnsMessageCodec.Ipv4RecordType,
            negative);

        DnsLookupResult first = cache.ResolveCachedDnsQuery(
            resolver, "short.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        clock.AdvanceDiscoveryTime(4);
        DnsLookupResult cached = cache.ResolveCachedDnsQuery(
            resolver, "short.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        clock.AdvanceDiscoveryTime(2);
        DnsLookupResult expired = cache.ResolveCachedDnsQuery(
            resolver, "short.example", DnsMessageCodec.Ipv4RecordType, 1000, true);

        Assert.Multiple(() =>
        {
            Assert.That(first.ExpiresAt, Is.EqualTo(
                new DateTimeOffset(2026, 8, 29, 18, 0, 5, TimeSpan.Zero)));
            Assert.That(cached.FromCache, Is.True);
            Assert.That(expired.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "short.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void CacheDoesNotStoreTransientErrorsOrZeroTtlRecords()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiscoveryCache cache = new DiscoveryCache(clock, 60);
        DiameterDnsQueryResult transient = new DiameterDnsQueryResult();
        transient.QueryName = "transient.example";
        transient.RecordType = DnsMessageCodec.Ipv4RecordType;
        transient.ResponseCode = 2;
        transient.Code = "dns-response-error";
        resolver.SetFakeDnsResponse(
            "transient.example",
            DnsMessageCodec.Ipv4RecordType,
            transient);

        DnsLookupResult transientFirst = cache.ResolveCachedDnsQuery(
            resolver, "transient.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        DnsLookupResult transientSecond = cache.ResolveCachedDnsQuery(
            resolver, "transient.example", DnsMessageCodec.Ipv4RecordType, 1000, true);

        DiameterDnsQueryResult zeroTtl = DiscoveryTestData.CreateSuccessfulDnsResult(
            "zero.example",
            DnsMessageCodec.Ipv4RecordType);
        zeroTtl.AddressRecords.Add(DiscoveryTestData.CreateAddressRecord(
            "zero.example",
            "192.0.2.90",
            0));
        resolver.SetFakeDnsResponse(
            "zero.example",
            DnsMessageCodec.Ipv4RecordType,
            zeroTtl);
        DnsLookupResult zeroFirst = cache.ResolveCachedDnsQuery(
            resolver, "zero.example", DnsMessageCodec.Ipv4RecordType, 1000, true);
        DnsLookupResult zeroSecond = cache.ResolveCachedDnsQuery(
            resolver, "zero.example", DnsMessageCodec.Ipv4RecordType, 1000, true);

        Assert.Multiple(() =>
        {
            Assert.That(transientFirst.FromCache, Is.False);
            Assert.That(transientFirst.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(transientSecond.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "transient.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
            Assert.That(zeroFirst.FromCache, Is.False);
            Assert.That(zeroFirst.ExpiresAt, Is.EqualTo(clock.UtcTime));
            Assert.That(zeroSecond.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "zero.example", DnsMessageCodec.Ipv4RecordType), Is.EqualTo(2));
        });
    }

    [Test]
    public void RealmInvalidationPreservesUnrelatedDnsCacheEntries()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        FakeDnsResolver resolver = new FakeDnsResolver();
        DiameterDnsQueryResult firstNaptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "example",
            DnsMessageCodec.NaptrRecordType);
        DiameterDnsQueryResult firstSrv = DiscoveryTestData.CreateSuccessfulDnsResult(
            "_diameter._tcp.example",
            DnsMessageCodec.SrvRecordType);
        DiameterDnsQueryResult secondNaptr = DiscoveryTestData.CreateSuccessfulDnsResult(
            "other.example",
            DnsMessageCodec.NaptrRecordType);
        DiameterDnsQueryResult secondSrv = DiscoveryTestData.CreateSuccessfulDnsResult(
            "_diameter._tcp.other.example",
            DnsMessageCodec.SrvRecordType);
        resolver.SetFakeDnsResponse(
            "example", DnsMessageCodec.NaptrRecordType, firstNaptr);
        resolver.SetFakeDnsResponse(
            "_diameter._tcp.example", DnsMessageCodec.SrvRecordType, firstSrv);
        resolver.SetFakeDnsResponse(
            "other.example", DnsMessageCodec.NaptrRecordType, secondNaptr);
        resolver.SetFakeDnsResponse(
            "_diameter._tcp.other.example", DnsMessageCodec.SrvRecordType, secondSrv);
        DiameterDnsDiscovery discovery = new DiameterDnsDiscovery(
            resolver, clock, new SequenceDiscoveryRandom(0));
        DiameterDiscoveryRequest firstRequest = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Tcp, DiameterSecurityMode.None);
        DiameterDiscoveryRequest secondRequest = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Tcp, DiameterSecurityMode.None);
        secondRequest.Realm.Value = "other.example";

        discovery.ResolveDnsDiscovery(firstRequest, 1000);
        discovery.ResolveDnsDiscovery(secondRequest, 1000);
        discovery.InvalidateDiscoveryCache("example");
        DiameterDiscoveryResult otherCached = discovery.ResolveDnsDiscovery(
            secondRequest,
            1000);
        DiameterDiscoveryResult firstRefreshed = discovery.ResolveDnsDiscovery(
            firstRequest,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(otherCached.FromCache, Is.True);
            Assert.That(firstRefreshed.FromCache, Is.False);
            Assert.That(resolver.CountFakeDnsQueries(
                "other.example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(1));
            Assert.That(resolver.CountFakeDnsQueries(
                "example", DnsMessageCodec.NaptrRecordType), Is.EqualTo(2));
        });
    }
}
