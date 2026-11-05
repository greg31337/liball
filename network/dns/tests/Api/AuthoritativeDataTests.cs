namespace dns.Tests.Api;

public class AuthoritativeDataTests
{
    [Test]
    public void AuthorizationPolicyDefaultsToQueriesOnlyAndOwnsRules()
    {
        DnsAuthorizationPolicy first = new DnsAuthorizationPolicy();
        DnsAuthorizationPolicy second = new DnsAuthorizationPolicy();
        first.AllowedClientAddresses.Add(new byte[] { 192, 0, 2, 0 });
        first.AllowedClientAddressFamilies.Add(4);
        first.AllowedClientPrefixLengths.Add(24);
        first.DeniedClientAddresses.Add(new byte[] { 0x20, 0x01, 0x0d, 0xb8 });
        first.DeniedClientAddressFamilies.Add(6);
        first.DeniedClientPrefixLengths.Add(32);
        first.AllowedTsigKeyNames.Add(new DnsName());
        first.AllowedZones.Add(new DnsName());

        Assert.Multiple(() =>
        {
            Assert.That(first.AllowQueries, Is.True);
            Assert.That(first.AllowRecursion, Is.False);
            Assert.That(first.AllowTransfers, Is.False);
            Assert.That(first.AllowUpdates, Is.False);
            Assert.That(first.AllowNotify, Is.False);
            Assert.That(first.RequireTsigForTransfers, Is.True);
            Assert.That(first.RequireTsigForUpdates, Is.True);
            Assert.That(first.RequireTsigForNotify, Is.True);
            Assert.That(first.RateLimitWindowMilliseconds, Is.EqualTo(1000));
            Assert.That(first.AllowedClientAddresses[0],
                Is.EqualTo(new byte[] { 192, 0, 2, 0 }));
            Assert.That(first.AllowedClientAddressFamilies, Is.EqualTo(new[] { 4 }));
            Assert.That(first.AllowedClientPrefixLengths, Is.EqualTo(new[] { 24 }));
            Assert.That(first.DeniedClientAddresses, Has.Count.EqualTo(1));
            Assert.That(first.DeniedClientAddressFamilies, Is.EqualTo(new[] { 6 }));
            Assert.That(first.DeniedClientPrefixLengths, Is.EqualTo(new[] { 32 }));
            Assert.That(first.AllowedTsigKeyNames, Has.Count.EqualTo(1));
            Assert.That(first.AllowedZones, Has.Count.EqualTo(1));
            Assert.That(second.AllowedClientAddresses, Is.Empty);
            Assert.That(second.DeniedClientAddresses, Is.Empty);
            Assert.That(second.AllowedTsigKeyNames, Is.Empty);
            Assert.That(second.AllowedZones, Is.Empty);
        });
    }

    [Test]
    public void AuthoritativeOptionsUseBoundedDnsDefaultsAndOwnPolicies()
    {
        DnsAuthoritativeOptions first = new DnsAuthoritativeOptions();
        DnsAuthoritativeOptions second = new DnsAuthoritativeOptions();
        first.Authorization.AllowTransfers = true;
        first.Security.TrustAnchors.Add(new DnsTrustAnchor());

        Assert.Multiple(() =>
        {
            Assert.That(first.RecursionAvailable, Is.False);
            Assert.That(first.ServeDnssec, Is.True);
            Assert.That(first.EnableEdns, Is.True);
            Assert.That(first.IncludeAdditionalRecords, Is.True);
            Assert.That(first.MinimalResponses, Is.False);
            Assert.That(first.MinimizeAnyResponses, Is.True);
            Assert.That(first.EchoRecursionDesired, Is.True);
            Assert.That(first.MaximumUdpResponseLength,
                Is.EqualTo(DnsProtocolLimits.DefaultEdnsUdpPayloadSize));
            Assert.That(first.MaximumTcpResponseLength,
                Is.EqualTo(DnsProtocolLimits.MaximumTcpMessageLength));
            Assert.That(first.MaximumAliasDepth, Is.EqualTo(16));
            Assert.That(first.MaximumResponseRecords,
                Is.EqualTo(DnsProtocolLimits.MaximumSectionEntryCount));
            Assert.That(second.Authorization.AllowTransfers, Is.False);
            Assert.That(second.Security.TrustAnchors, Is.Empty);
            Assert.That(second.Authorization, Is.Not.SameAs(first.Authorization));
            Assert.That(second.Security, Is.Not.SameAs(first.Security));
        });
    }

    [Test]
    public void AuthoritativeRequestsOwnClientContextAndExactWireBytes()
    {
        DnsAuthoritativeRequest first = new DnsAuthoritativeRequest();
        DnsAuthoritativeRequest second = new DnsAuthoritativeRequest();
        DnsTsigResult tsig = new DnsTsigResult();
        first.MessageBytes = [0x12, 0x34];
        first.Transport = DnsTransportKind.Tcp;
        first.Tsig = tsig;
        first.ReceivedAt.UnixTimeMilliseconds = long.MaxValue;
        first.CancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(first.MessageBytes, Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(first.Transport, Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(first.Tsig, Is.SameAs(tsig));
            Assert.That(first.ReceivedAt.UnixTimeMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(first.CancellationRequested, Is.True);
            Assert.That(second.Message, Is.Not.SameAs(first.Message));
            Assert.That(second.ClientEndpoint, Is.Not.SameAs(first.ClientEndpoint));
            Assert.That(second.ServerEndpoint, Is.Not.SameAs(first.ServerEndpoint));
            Assert.That(second.Options, Is.Not.SameAs(first.Options));
            Assert.That(second.ReceivedAt, Is.Not.SameAs(first.ReceivedAt));
        });
    }

    [Test]
    public void AuthoritativeResultsRetainExtendedResponseAndOwnSections()
    {
        DnsAuthoritativeResult first = new DnsAuthoritativeResult();
        DnsAuthoritativeResult second = new DnsAuthoritativeResult();
        DnsMessage response = new DnsMessage();
        first.Success = true;
        first.Kind = DnsAuthoritativeAnswerKind.Referral;
        first.ResponseCode = (DnsResponseCode)4095;
        first.ResponseCodeValue = 4095;
        first.Response = response;
        first.ResponseBytes = [0xab];
        first.Zone = new DnsName();
        first.AnswerRecordSets.Add(new DnsResourceRecordSet());
        first.AuthorityRecordSets.Add(new DnsResourceRecordSet());
        first.AdditionalRecordSets.Add(new DnsResourceRecordSet());
        first.Authoritative = true;
        first.Truncated = true;
        first.SecurityStatus = DnsSecurityStatus.Secure;
        first.Tsig = new DnsTsigResult();
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Kind, Is.EqualTo(DnsAuthoritativeAnswerKind.Referral));
            Assert.That((int)first.ResponseCode, Is.EqualTo(4095));
            Assert.That(first.ResponseCodeValue, Is.EqualTo(4095));
            Assert.That(first.Response, Is.SameAs(response));
            Assert.That(first.ResponseBytes, Is.EqualTo(new byte[] { 0xab }));
            Assert.That(first.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(first.AuthorityRecordSets, Has.Count.EqualTo(1));
            Assert.That(first.AdditionalRecordSets, Has.Count.EqualTo(1));
            Assert.That(first.Authoritative, Is.True);
            Assert.That(first.Truncated, Is.True);
            Assert.That(first.SecurityStatus, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(first.Tsig, Is.Not.Null);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.AnswerRecordSets, Is.Empty);
            Assert.That(second.AuthorityRecordSets, Is.Empty);
            Assert.That(second.AdditionalRecordSets, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ServerOptionsStartBoundedAndOwnEndpointsKeysAndPolicy()
    {
        DnsServerOptions first = new DnsServerOptions();
        DnsServerOptions second = new DnsServerOptions();
        first.Endpoints.Add(new DnsEndpoint());
        first.TsigKeys.Add(new DnsTsigKey());
        first.Authoritative.Authorization.AllowNotify = true;

        Assert.Multiple(() =>
        {
            Assert.That(first.EnableUdp, Is.True);
            Assert.That(first.EnableTcp, Is.True);
            Assert.That(first.TcpListenBacklog, Is.EqualTo(128));
            Assert.That(first.MaximumConcurrentRequests, Is.EqualTo(256));
            Assert.That(first.MaximumRequestLength,
                Is.EqualTo(DnsProtocolLimits.MaximumTcpMessageLength));
            Assert.That(first.TcpIdleTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(first.TcpConnectionLifetimeMilliseconds, Is.EqualTo(60000));
            Assert.That(first.RequestTimeoutMilliseconds, Is.EqualTo(5000));
            Assert.That(first.MaximumTcpRequestsPerConnection, Is.EqualTo(100));
            Assert.That(first.StopRequested, Is.False);
            Assert.That(first.Endpoints, Has.Count.EqualTo(1));
            Assert.That(first.TsigKeys, Has.Count.EqualTo(1));
            Assert.That(second.Endpoints, Is.Empty);
            Assert.That(second.TsigKeys, Is.Empty);
            Assert.That(second.Authoritative.Authorization.AllowNotify, Is.False);
            Assert.That(second.Authoritative, Is.Not.SameAs(first.Authoritative));
        });
    }

    [Test]
    public void ServerRunResultsPreserveSignedCountsAndOwnRuntimeState()
    {
        DnsServerRunResult first = new DnsServerRunResult();
        DnsServerRunResult second = new DnsServerRunResult();
        first.Status = DnsServerStatus.Cancelled;
        first.BoundEndpoints.Add(new DnsEndpoint());
        first.StartedAt.MonotonicMilliseconds = 100;
        first.CompletedAt.MonotonicMilliseconds = 200;
        first.RequestCount = long.MaxValue;
        first.UdpRequestCount = 5;
        first.TcpRequestCount = 6;
        first.FailedRequestCount = 1;
        first.Cancelled = true;
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsServerStatus.Cancelled));
            Assert.That(first.BoundEndpoints, Has.Count.EqualTo(1));
            Assert.That(first.StartedAt.MonotonicMilliseconds, Is.EqualTo(100));
            Assert.That(first.CompletedAt.MonotonicMilliseconds, Is.EqualTo(200));
            Assert.That(first.RequestCount, Is.EqualTo(long.MaxValue));
            Assert.That(first.UdpRequestCount, Is.EqualTo(5));
            Assert.That(first.TcpRequestCount, Is.EqualTo(6));
            Assert.That(first.FailedRequestCount, Is.EqualTo(1));
            Assert.That(first.Cancelled, Is.True);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.BoundEndpoints, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(second.StartedAt, Is.Not.SameAs(first.StartedAt));
            Assert.That(second.CompletedAt, Is.Not.SameAs(first.CompletedAt));
        });
    }
}
