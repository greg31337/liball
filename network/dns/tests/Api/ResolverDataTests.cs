namespace dns.Tests.Api;

public class ResolverDataTests
{
    [Test]
    public void ResolverConfigurationUsesSystemStyleDefaultsAndOwnsCollections()
    {
        DnsResolverConfiguration first = new DnsResolverConfiguration();
        DnsResolverConfiguration second = new DnsResolverConfiguration();
        first.Servers.Add(new DnsResolverServer());
        first.SearchDomains.Add(new DnsName());

        Assert.Multiple(() =>
        {
            Assert.That(first.NameDots, Is.EqualTo(1));
            Assert.That(first.Attempts, Is.EqualTo(2));
            Assert.That(first.TimeoutMilliseconds, Is.EqualTo(5000));
            Assert.That(first.RotateServers, Is.False);
            Assert.That(first.UseTcp, Is.False);
            Assert.That(first.TrustAuthenticData, Is.False);
            Assert.That(first.UseEdns, Is.True);
            Assert.That(second.Servers, Is.Empty);
            Assert.That(second.SearchDomains, Is.Empty);
        });
    }

    [Test]
    public void ResolverServersOwnEndpointsAndRetainSelectionMetadata()
    {
        DnsResolverServer first = new DnsResolverServer();
        DnsResolverServer second = new DnsResolverServer();
        first.Transport = DnsTransportKind.Tls;
        first.AuthenticationName = "resolver.example";
        first.Priority = 7;
        first.Endpoint.Port = DnsPorts.DnsOverTls;

        Assert.Multiple(() =>
        {
            Assert.That(first.Enabled, Is.True);
            Assert.That(first.Transport, Is.EqualTo(DnsTransportKind.Tls));
            Assert.That(first.AuthenticationName, Is.EqualTo("resolver.example"));
            Assert.That(first.Priority, Is.EqualTo(7));
            Assert.That(first.Endpoint.Port, Is.EqualTo(853));
            Assert.That(second.Endpoint, Is.Not.SameAs(first.Endpoint));
        });
    }

    [Test]
    public void ResolverOptionsStartBoundedAndOwnConfiguration()
    {
        DnsResolverOptions first = new DnsResolverOptions();
        DnsResolverOptions second = new DnsResolverOptions();
        first.Configuration.Servers.Add(new DnsResolverServer());
        first.SpecialUsePolicies.Add(new DnsSpecialUsePolicy());

        Assert.Multiple(() =>
        {
            Assert.That(first.UseCache, Is.True);
            Assert.That(first.FollowAliases, Is.True);
            Assert.That(first.RejectOnionNames, Is.True);
            Assert.That(first.HandleLocalhostNames, Is.True);
            Assert.That(first.SpecialUsePolicies, Has.Count.EqualTo(1));
            Assert.That(first.MaximumAliasDepth, Is.EqualTo(16));
            Assert.That(first.TotalTimeoutMilliseconds, Is.EqualTo(5000));
            Assert.That(first.PrefetchThresholdPercent, Is.EqualTo(10));
            Assert.That(second.Configuration.Servers, Is.Empty);
            Assert.That(second.SpecialUsePolicies, Is.Empty);
            Assert.That(second.Configuration, Is.Not.SameAs(first.Configuration));
        });
    }

    [Test]
    public void SpecialUsePoliciesOwnSuffixRecordsAndDefaults()
    {
        DnsSpecialUsePolicy first = new DnsSpecialUsePolicy();
        DnsSpecialUsePolicy second = new DnsSpecialUsePolicy();
        first.RecordSets.Add(new DnsResourceRecordSet());

        Assert.Multiple(() =>
        {
            Assert.That(first.ApplyToSubdomains, Is.True);
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.NameError));
            Assert.That(first.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(first.RecordSets, Has.Count.EqualTo(1));
            Assert.That(second.RecordSets, Is.Empty);
            Assert.That(second.Suffix, Is.Not.SameAs(first.Suffix));
        });
    }

    [Test]
    public void ResolveRequestsPreserveUnknownCodesAndOwnPolicy()
    {
        DnsResolveRequest first = new DnsResolveRequest();
        DnsResolveRequest second = new DnsResolveRequest();
        first.RecordType = (DnsRecordType)65280;
        first.RecordTypeValue = 65280;
        first.Class = (DnsClass)65000;
        first.ClassValue = 65000;
        first.CancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That((int)first.RecordType, Is.EqualTo(65280));
            Assert.That(first.RecordTypeValue, Is.EqualTo(65280));
            Assert.That((int)first.Class, Is.EqualTo(65000));
            Assert.That(first.ClassValue, Is.EqualTo(65000));
            Assert.That(first.CancellationRequested, Is.True);
            Assert.That(second.Class, Is.EqualTo(DnsClass.Internet));
            Assert.That(second.ClassValue, Is.EqualTo(1));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.Options, Is.Not.SameAs(first.Options));
        });
    }

    [Test]
    public void ResolveResultsOwnResolutionHistoryAndMetadata()
    {
        DnsResolveResult first = new DnsResolveResult();
        DnsResolveResult second = new DnsResolveResult();
        first.Status = DnsResolveStatus.Success;
        first.SecurityStatus = DnsSecurityStatus.Secure;
        first.FromCache = true;
        first.Stale = true;
        first.HasTimeToLive = true;
        first.TimeToLive = uint.MaxValue;
        first.RecordSets.Add(new DnsResourceRecordSet());
        first.AliasSteps.Add(new DnsAliasStep());
        first.Delegations.Add(new DnsDelegation());
        first.Queries.Add(new DnsQueryResult());
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(first.SecurityStatus, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(first.FromCache, Is.True);
            Assert.That(first.Stale, Is.True);
            Assert.That(first.TimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(first.RecordSets, Has.Count.EqualTo(1));
            Assert.That(first.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(first.Delegations, Has.Count.EqualTo(1));
            Assert.That(first.Queries, Has.Count.EqualTo(1));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.RecordSets, Is.Empty);
            Assert.That(second.AliasSteps, Is.Empty);
            Assert.That(second.Delegations, Is.Empty);
            Assert.That(second.Queries, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AliasStepsRepresentDirectAndSynthesizedLinks()
    {
        DnsAliasStep step = new DnsAliasStep();
        DnsResourceRecord record = new DnsResourceRecord();
        step.RecordType = DnsRecordType.Dname;
        step.RecordTypeValue = 39;
        step.Synthesized = true;
        step.Record = record;

        Assert.Multiple(() =>
        {
            Assert.That(step.RecordType, Is.EqualTo(DnsRecordType.Dname));
            Assert.That(step.RecordTypeValue, Is.EqualTo(39));
            Assert.That(step.Synthesized, Is.True);
            Assert.That(step.Record, Is.SameAs(record));
            Assert.That(step.Owner, Is.Not.SameAs(step.Target));
        });
    }

    [Test]
    public void CacheEntriesRetainPositiveNegativeAndStaleTimingState()
    {
        DnsCacheEntry first = new DnsCacheEntry();
        DnsCacheEntry second = new DnsCacheEntry();
        first.Kind = DnsCacheEntryKind.NegativeData;
        first.RecordType = (DnsRecordType)65281;
        first.RecordTypeValue = 65281;
        first.OriginalTimeToLive = uint.MaxValue;
        first.StoredAt.MonotonicMilliseconds = 1000;
        first.ExpiresAt.MonotonicMilliseconds = 2000;
        first.HasStaleUntil = true;
        first.StaleUntil.MonotonicMilliseconds = 3000;
        first.SecurityStatus = DnsSecurityStatus.Insecure;
        first.Authoritative = true;
        first.Credibility = 4;
        first.RecordSets.Add(new DnsResourceRecordSet());
        first.NegativeSoa = new DnsResourceRecord();

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(DnsCacheEntryKind.NegativeData));
            Assert.That(first.RecordTypeValue, Is.EqualTo(65281));
            Assert.That(first.OriginalTimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(first.StoredAt.MonotonicMilliseconds, Is.EqualTo(1000));
            Assert.That(first.ExpiresAt.MonotonicMilliseconds, Is.EqualTo(2000));
            Assert.That(first.StaleUntil.MonotonicMilliseconds, Is.EqualTo(3000));
            Assert.That(first.Authoritative, Is.True);
            Assert.That(first.Credibility, Is.EqualTo(4));
            Assert.That(first.NegativeSoa, Is.Not.Null);
            Assert.That(second.RecordSets, Is.Empty);
            Assert.That(second.StoredAt, Is.Not.SameAs(first.StoredAt));
            Assert.That(second.ExpiresAt, Is.Not.SameAs(first.ExpiresAt));
            Assert.That(second.StaleUntil, Is.Not.SameAs(first.StaleUntil));
        });
    }

    [Test]
    public void CacheLookupsDistinguishFreshStaleAndPrefetchState()
    {
        DnsCacheLookup lookup = new DnsCacheLookup();
        DnsCacheEntry entry = new DnsCacheEntry();
        lookup.Found = true;
        lookup.Fresh = false;
        lookup.Stale = true;
        lookup.NeedsPrefetch = true;
        lookup.Entry = entry;
        lookup.HasRemainingTimeToLive = true;
        lookup.RemainingTimeToLive = 0;
        lookup.LookedUpAt.MonotonicMilliseconds = 4000;

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Found, Is.True);
            Assert.That(lookup.Fresh, Is.False);
            Assert.That(lookup.Stale, Is.True);
            Assert.That(lookup.NeedsPrefetch, Is.True);
            Assert.That(lookup.Entry, Is.SameAs(entry));
            Assert.That(lookup.HasRemainingTimeToLive, Is.True);
            Assert.That(lookup.RemainingTimeToLive, Is.Zero);
            Assert.That(lookup.LookedUpAt.MonotonicMilliseconds, Is.EqualTo(4000));
        });
    }

    [Test]
    public void RootHintsAndDelegationsOwnReferralState()
    {
        DnsRootHints firstHints = new DnsRootHints();
        DnsRootHints secondHints = new DnsRootHints();
        firstHints.Source = "root.hints";
        firstHints.Valid = true;
        firstHints.LoadedAt.UnixTimeMilliseconds = 100;
        firstHints.Root.NameServers.Add(new DnsName());
        firstHints.Root.GlueAddresses.Add(new DnsAddressResult());
        firstHints.Root.AuthorityRecordSets.Add(new DnsResourceRecordSet());
        firstHints.Root.Secure = true;
        firstHints.Root.InBailiwick = true;
        firstHints.Root.Depth = 1;

        Assert.Multiple(() =>
        {
            Assert.That(firstHints.Source, Is.EqualTo("root.hints"));
            Assert.That(firstHints.Valid, Is.True);
            Assert.That(firstHints.Root.NameServers, Has.Count.EqualTo(1));
            Assert.That(firstHints.Root.GlueAddresses, Has.Count.EqualTo(1));
            Assert.That(firstHints.Root.AuthorityRecordSets, Has.Count.EqualTo(1));
            Assert.That(firstHints.Root.Secure, Is.True);
            Assert.That(firstHints.Root.InBailiwick, Is.True);
            Assert.That(firstHints.Root.Depth, Is.EqualTo(1));
            Assert.That(secondHints.Root, Is.Not.SameAs(firstHints.Root));
            Assert.That(secondHints.LoadedAt, Is.Not.SameAs(firstHints.LoadedAt));
            Assert.That(secondHints.Root.NameServers, Is.Empty);
        });
    }

    [Test]
    public void RecursiveOptionsStartWithBoundedHardenedDefaults()
    {
        DnsRecursiveOptions options = new DnsRecursiveOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.MaximumQueries, Is.EqualTo(64));
            Assert.That(options.MaximumWork, Is.EqualTo(10000));
            Assert.That(options.MaximumDepth, Is.EqualTo(32));
            Assert.That(options.MaximumAliasDepth, Is.EqualTo(16));
            Assert.That(options.MaximumReferralDepth, Is.EqualTo(32));
            Assert.That(options.TotalTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(options.EdnsUdpPayloadSize,
                Is.EqualTo(DnsProtocolLimits.DefaultEdnsUdpPayloadSize));
            Assert.That(options.MinimizeQueryName, Is.True);
            Assert.That(options.AllowMinimizationFallback, Is.True);
            Assert.That(options.RandomizeQueryCase, Is.True);
            Assert.That(options.UseEdns, Is.True);
            Assert.That(options.AllowEdnsDowngrade, Is.True);
            Assert.That(options.AllowTcpFallback, Is.True);
            Assert.That(options.ValidateDnssec, Is.True);
        });
    }

    [Test]
    public void AddressAndMailResultsOwnValuesAndRetainDnsMetadata()
    {
        DnsAddressResult firstAddress = new DnsAddressResult();
        DnsAddressResult secondAddress = new DnsAddressResult();
        DnsMailResult firstMail = new DnsMailResult();
        DnsMailResult secondMail = new DnsMailResult();
        DnsMessage response = new DnsMessage();
        firstAddress.Status = DnsResolveStatus.Success;
        firstAddress.Addresses.Add(new byte[] { 192, 0, 2, 1 });
        firstAddress.AddressFamilies.Add(4);
        firstAddress.AliasSteps.Add(new DnsAliasStep());
        firstAddress.Response = response;
        firstAddress.SecurityStatus = DnsSecurityStatus.Secure;
        firstAddress.HasTimeToLive = true;
        firstAddress.TimeToLive = 300;
        firstMail.Status = DnsResolveStatus.Success;
        firstMail.Exchanges.Add(new DnsPreferenceNameData());
        firstMail.Response = response;
        firstMail.SecurityStatus = DnsSecurityStatus.Insecure;
        firstMail.HasTimeToLive = true;
        firstMail.TimeToLive = 600;

        Assert.Multiple(() =>
        {
            Assert.That(firstAddress.Addresses[0], Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(firstAddress.AddressFamilies, Is.EqualTo(new[] { 4 }));
            Assert.That(firstAddress.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(firstAddress.Response, Is.SameAs(response));
            Assert.That(firstAddress.SecurityStatus, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(firstAddress.TimeToLive, Is.EqualTo(300));
            Assert.That(secondAddress.Addresses, Is.Empty);
            Assert.That(secondAddress.AddressFamilies, Is.Empty);
            Assert.That(secondAddress.AliasSteps, Is.Empty);
            Assert.That(firstMail.Exchanges, Has.Count.EqualTo(1));
            Assert.That(firstMail.Response, Is.SameAs(response));
            Assert.That(firstMail.SecurityStatus, Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(firstMail.TimeToLive, Is.EqualTo(600));
            Assert.That(secondMail.Exchanges, Is.Empty);
        });
    }

    [Test]
    public void ServiceResultsOwnEndpointsAndTextWhileEndpointsRetainSelectionData()
    {
        DnsServiceResult first = new DnsServiceResult();
        DnsServiceResult second = new DnsServiceResult();
        DnsServiceEndpoint endpoint = new DnsServiceEndpoint();
        DnsSvcbData binding = new DnsSvcbData();
        endpoint.Priority = 1;
        endpoint.Weight = 20;
        endpoint.Port = 443;
        endpoint.Addresses.Add(new byte[] { 192, 0, 2, 2 });
        endpoint.AddressFamilies.Add(4);
        endpoint.ServiceBinding = binding;
        endpoint.SecurityStatus = DnsSecurityStatus.Secure;
        endpoint.HasTimeToLive = true;
        endpoint.TimeToLive = 120;
        first.Status = DnsResolveStatus.Success;
        first.Endpoints.Add(endpoint);
        first.TextValues.Add(new byte[] { 0, 255 });
        first.SecurityStatus = DnsSecurityStatus.Secure;
        first.HasTimeToLive = true;
        first.TimeToLive = 120;

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Priority, Is.EqualTo(1));
            Assert.That(endpoint.Weight, Is.EqualTo(20));
            Assert.That(endpoint.Port, Is.EqualTo(443));
            Assert.That(endpoint.Addresses[0], Is.EqualTo(new byte[] { 192, 0, 2, 2 }));
            Assert.That(endpoint.AddressFamilies, Is.EqualTo(new[] { 4 }));
            Assert.That(endpoint.ServiceBinding, Is.SameAs(binding));
            Assert.That(endpoint.SecurityStatus, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(endpoint.TimeToLive, Is.EqualTo(120));
            Assert.That(first.Endpoints, Has.Count.EqualTo(1));
            Assert.That(first.TextValues[0], Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(first.SecurityStatus, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(first.TimeToLive, Is.EqualTo(120));
            Assert.That(second.Endpoints, Is.Empty);
            Assert.That(second.TextValues, Is.Empty);
        });
    }
}
