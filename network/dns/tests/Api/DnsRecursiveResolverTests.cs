using System.Text;

namespace dns.Tests.Api;

public class DnsRecursiveResolverTests
{
    [Test]
    public void LoadsValidRootZoneAndSeedsNameServerAndGlueCacheEntries()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        DnsZone zone = RootZone(
            [192, 0, 2, 1],
            [32, 1, 13, 184, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 2]);

        DnsRootHints hints = resolver.LoadRootHints(zone, "test root hints");

        DnsCacheLookup rootNs = Lookup(
            cache,
            Name(true),
            DnsRecordType.Ns);
        DnsCacheLookup ipv4 = Lookup(
            cache,
            Name(true, "a", "root-servers", "net"),
            DnsRecordType.A);
        DnsCacheLookup ipv6 = Lookup(
            cache,
            Name(true, "b", "root-servers", "net"),
            DnsRecordType.Aaaa);
        Assert.Multiple(() =>
        {
            Assert.That(hints.Valid, Is.True);
            Assert.That(hints.Source, Is.EqualTo("test root hints"));
            Assert.That(hints.LoadedAt.MonotonicMilliseconds,
                Is.GreaterThan(0));
            Assert.That(Text(hints.Root.Zone), Is.EqualTo("."));
            Assert.That(hints.Root.NameServers.Select(Text),
                Is.EqualTo(new[]
                {
                    "a.root-servers.net.",
                    "b.root-servers.net."
                }));
            Assert.That(hints.Root.GlueAddresses, Has.Count.EqualTo(2));
            Assert.That(hints.Root.GlueAddresses[0].Addresses[0],
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(hints.Root.GlueAddresses[1].AddressFamilies,
                Is.EqualTo(new[] { 6 }));
            Assert.That(hints.Root.AuthorityRecordSets,
                Has.Count.EqualTo(1));
            Assert.That(hints.Root.InBailiwick, Is.True);
            Assert.That(rootNs.Found && rootNs.Fresh, Is.True);
            Assert.That(rootNs.Entry!.Credibility, Is.EqualTo(4));
            Assert.That(ipv4.Entry!.RecordSets[0].Records[0]
                .Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(ipv6.Found && ipv6.Fresh, Is.True);
        });
    }

    [Test]
    public void ReloadingRootHintsReplacesCachedGlue()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        resolver.LoadRootHints(RootZone(
            [192, 0, 2, 1],
            Ipv6(1)), "first");

        DnsRootHints refreshed = resolver.LoadRootHints(RootZone(
            [198, 51, 100, 9],
            Ipv6(9)), "refreshed");

        DnsCacheLookup lookup = Lookup(
            cache,
            Name(true, "a", "root-servers", "net"),
            DnsRecordType.A);
        Assert.Multiple(() =>
        {
            Assert.That(refreshed.Source, Is.EqualTo("refreshed"));
            Assert.That(refreshed.Root.GlueAddresses[0].Addresses[0],
                Is.EqualTo(new byte[] { 198, 51, 100, 9 }));
            Assert.That(lookup.Entry!.RecordSets[0].Records,
                Has.Count.EqualTo(1));
            Assert.That(lookup.Entry.RecordSets[0].Records[0]
                .Data!.Address!.Address,
                Is.EqualTo(new byte[] { 198, 51, 100, 9 }));
        });
    }

    [Test]
    public void ValidationRejectsMissingOrMisalignedRootGlue()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        DnsZone missing = RootZone([192, 0, 2, 1], Ipv6(2));
        missing.Nodes[0].Children.RemoveAt(1);

        Assert.That(
            () => resolver.LoadRootHints(missing, "missing glue"),
            Throws.TypeOf<DnsResolutionException>());

        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "valid");
        hints.Root.GlueAddresses[0].CanonicalName =
            Name(true, "different", "example");

        Assert.That(
            () => resolver.ValidateRootHints(hints),
            Throws.TypeOf<DnsResolutionException>());

        DnsZone cyclic = RootZone([192, 0, 2, 1], Ipv6(2));
        cyclic.Nodes[0].Children.Add(cyclic.Nodes[0]);
        Assert.That(
            () => resolver.LoadRootHints(cyclic, "cyclic"),
            Throws.TypeOf<DnsResolutionException>());
    }

    [Test]
    public void ValidationReturnsAnIndependentNormalizedCopy()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        DnsRootHints loaded = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "copy");
        loaded.Valid = false;
        loaded.Root.GlueAddresses.Reverse();

        DnsRootHints validated = resolver.ValidateRootHints(loaded);
        validated.Root.NameServers[0].Labels[0][0] = (byte)'z';
        validated.Root.GlueAddresses[0].Addresses[0][0] = 203;
        validated.Root.AuthorityRecordSets[0].Records[0]
            .Data!.Name!.Name.Labels[0][0] = (byte)'y';

        Assert.Multiple(() =>
        {
            Assert.That(validated.Valid, Is.True);
            Assert.That(Text(loaded.Root.NameServers[0]),
                Is.EqualTo("a.root-servers.net."));
            Assert.That(loaded.Root.GlueAddresses[0].Addresses[0][0],
                Is.EqualTo(32));
            Assert.That(loaded.Root.GlueAddresses[1].Addresses[0][0],
                Is.EqualTo(192));
            Assert.That(Text(loaded.Root.AuthorityRecordSets[0].Records[0]
                .Data!.Name!.Name), Is.EqualTo("a.root-servers.net."));
            Assert.That(Text(validated.Root.GlueAddresses[0].CanonicalName),
                Is.EqualTo("a.root-servers.net."));
            Assert.That(validated.Root.InBailiwick, Is.True);
            Assert.That(validated.Root.Lame, Is.False);
            Assert.That(validated.Root.Depth, Is.Zero);
        });
    }

    [Test]
    public void SelectsLongestCachedDelegationAndFallsBackToRoot()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "selection");
        StoreDelegation(
            cache,
            Name(true, "example"),
            Name(true, "ns", "example"),
            [203, 0, 113, 53]);

        DnsDelegation selected = resolver.SelectClosestDelegation(
            Name(true, "www", "deep", "example"),
            hints);
        DnsDelegation fallback = resolver.SelectClosestDelegation(
            Name(true, "www", "other"),
            hints);

        Assert.Multiple(() =>
        {
            Assert.That(Text(selected.Zone), Is.EqualTo("example."));
            Assert.That(selected.Depth, Is.EqualTo(1));
            Assert.That(selected.NameServers.Select(Text),
                Is.EqualTo(new[] { "ns.example." }));
            Assert.That(selected.GlueAddresses, Has.Count.EqualTo(1));
            Assert.That(selected.GlueAddresses[0].Addresses[0],
                Is.EqualTo(new byte[] { 203, 0, 113, 53 }));
            Assert.That(Text(fallback.Zone), Is.EqualTo("."));
            Assert.That(fallback.NameServers, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void IterativeEntryPointReportsSelectedStartingDelegation()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "entry point");
        StoreDelegation(
            cache,
            Name(true, "example"),
            Name(true, "ns", "example"),
            [203, 0, 113, 53]);
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = Name(true, "www", "example");
        request.RecordType = DnsRecordType.A;
        request.RecordTypeValue = 1;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("www.example."));
            Assert.That(result.Delegations, Has.Count.EqualTo(1));
            Assert.That(Text(result.Delegations[0].Zone),
                Is.EqualTo("example."));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
            Assert.That(client.Requests[0].Servers[0].Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 53 }));
        });
    }

    [Test]
    public void IterativeQueryUsesRdZeroAndSelectedAuthorityEndpoint()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "query");
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = Name(true, "www", "example");
        request.RecordType = DnsRecordType.A;
        request.RecordTypeValue = 1;
        DnsRecursiveOptions options = NonMinimizedOptions();
        options.UseEdns = false;
        options.ValidateDnssec = false;
        options.AllowTcpFallback = false;
        options.AllowEdnsDowngrade = false;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            options);

        DnsQueryRequest sent = client.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(sent.RecursionDesired, Is.False);
            Assert.That(sent.UseEdns, Is.False);
            Assert.That(sent.RequestDnssec, Is.False);
            Assert.That(sent.AllowTcpFallback, Is.False);
            Assert.That(sent.AllowEdnsDowngrade, Is.False);
            Assert.That(sent.TransportOptions.Kind,
                Is.EqualTo(DnsTransportKind.Udp));
            Assert.That(sent.TransportOptions.MaximumAttempts, Is.EqualTo(2));
            Assert.That(sent.TransportOptions.TotalTimeoutMilliseconds,
                Is.GreaterThan(0));
            Assert.That(sent.Servers, Has.Count.EqualTo(1));
            Assert.That(sent.Servers[0].Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(Text(sent.Question.Name),
                Is.EqualTo("www.example."));
        });
    }

    [Test]
    public void IterativeQueryRandomizesAsciiCaseAndUsesEntropyIdentifier()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeEntropy entropy =
            new ScriptedIterativeEntropy(0x1234, 1);
        resolver.Entropy = entropy;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "randomized query");
        DnsRecursiveOptions options = NonMinimizedOptions();
        options.RandomizeQueryCase = true;

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            options);

        DnsQueryRequest sent = client.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(Text(sent.Question.Name),
                Is.EqualTo("WWW.EXAMPLE."));
            Assert.That(sent.Message, Is.Not.Null);
            Assert.That(sent.Message!.Header.Identifier, Is.EqualTo(0x1234));
            Assert.That(Text(sent.Message.Questions.Single().Name),
                Is.EqualTo("WWW.EXAMPLE."));
            Assert.That(result.Response!.Header.Identifier,
                Is.EqualTo(0x1234));
        });
    }

    [Test]
    public void RejectsWrongQuestionCaseAndUsesFreshEntropyForAlternate()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        resolver.Entropy = new ScriptedIterativeEntropy(0x2000, 1);
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(WrongQuestionCaseAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "case validation");
        DnsRecursiveOptions options = NonMinimizedOptions();
        options.RandomizeQueryCase = true;

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(client.Requests[0].Message!.Header.Identifier,
                Is.EqualTo(0x2000));
            Assert.That(client.Requests[1].Message!.Header.Identifier,
                Is.EqualTo(0x2001));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == DnsDiagnosticCode.ResponseMismatch), Is.True);
        });
    }

    [Test]
    public void RejectsWrongResponseIdentifierAndTriesAlternateAuthority()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        resolver.Entropy = new ScriptedIterativeEntropy(0x3000, 0);
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(WrongIdentifierAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "identifier validation");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == DnsDiagnosticCode.ResponseMismatch), Is.True);
        });
    }

    [Test]
    public void RejectsResponseFromUnexpectedSourceAndTriesAlternateAuthority()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        resolver.Entropy = new ScriptedIterativeEntropy(0x4000, 0);
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(WrongSourceAnswer);
        client.Responses.Enqueue(MatchedSourceAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "source validation");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == DnsDiagnosticCode.ResponseMismatch), Is.True);
        });
    }

    [Test]
    public void LearnsAndReusesReducedEdnsPayloadForAnAuthority()
    {
        ScriptedIterativeTransport transport =
            new ScriptedIterativeTransport();
        transport.Enqueue(request =>
        {
            Assert.That(EdnsPayload(request.QueryMessage!),
                Is.EqualTo(DnsProtocolLimits.DefaultEdnsUdpPayloadSize));
            return TransportAnswer(
                request,
                DnsResponseCode.NoError,
                true,
                900,
                false);
        });
        transport.Enqueue(request =>
        {
            Assert.That(EdnsPayload(request.QueryMessage!), Is.EqualTo(900));
            return TransportAnswer(
                request,
                DnsResponseCode.NoError,
                true,
                900,
                false);
        });
        DnsRecursiveResolver resolver = ResolverWithTransport(transport);
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "EDNS payload learning");
        DnsRecursiveOptions options = IterativeTransportOptions();

        DnsResolveResult first = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            options);
        DnsResolveRequest secondRequest = AddressRequest();
        secondRequest.Name = Name(true, "other", "example");
        DnsResolveResult second = resolver.ResolveIteratively(
            secondRequest,
            hints,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(second.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(resolver.ServerStates, Has.Count.EqualTo(1));
            Assert.That(resolver.ServerStates[0].HasEdnsCapability, Is.True);
            Assert.That(resolver.ServerStates[0].SupportsEdns, Is.True);
            Assert.That(resolver.ServerStates[0].EdnsUdpPayloadSize,
                Is.EqualTo(900));
        });
    }

    [Test]
    public void LearnsNoEdnsFallbackAndStartsLaterQueryWithoutEdns()
    {
        ScriptedIterativeTransport transport =
            new ScriptedIterativeTransport();
        transport.Enqueue(request =>
        {
            Assert.That(EdnsPayload(request.QueryMessage!),
                Is.GreaterThan(0));
            return TransportAnswer(
                request,
                DnsResponseCode.FormatError,
                false,
                0,
                false);
        });
        transport.Enqueue(request =>
        {
            Assert.That(EdnsPayload(request.QueryMessage!), Is.Zero);
            return TransportAnswer(
                request,
                DnsResponseCode.NoError,
                false,
                0,
                false);
        });
        transport.Enqueue(request =>
        {
            Assert.That(EdnsPayload(request.QueryMessage!), Is.Zero);
            return TransportAnswer(
                request,
                DnsResponseCode.NoError,
                false,
                0,
                false);
        });
        DnsRecursiveResolver resolver = ResolverWithTransport(transport);
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "EDNS downgrade learning");
        DnsRecursiveOptions options = IterativeTransportOptions();

        DnsResolveResult first = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            options);
        DnsResolveRequest secondRequest = AddressRequest();
        secondRequest.Name = Name(true, "other", "example");
        DnsResolveResult second = resolver.ResolveIteratively(
            secondRequest,
            hints,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(first.Queries[0].EdnsDowngraded, Is.True);
            Assert.That(first.Queries[0].Attempts, Has.Count.EqualTo(2));
            Assert.That(second.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(resolver.ServerStates[0].HasEdnsCapability, Is.True);
            Assert.That(resolver.ServerStates[0].SupportsEdns, Is.False);
        });
    }

    [Test]
    public void RetriesTruncatedIterativeUdpAnswerOverTcp()
    {
        ScriptedIterativeTransport transport =
            new ScriptedIterativeTransport();
        transport.Enqueue(request =>
        {
            Assert.That(request.Options.Kind, Is.EqualTo(DnsTransportKind.Udp));
            return TransportAnswer(
                request,
                DnsResponseCode.NoError,
                true,
                900,
                true);
        });
        transport.Enqueue(request =>
        {
            Assert.That(request.Options.Kind, Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(request.QueryMessage!.Header.Identifier,
                Is.EqualTo(0x5000));
            return TransportAnswer(
                request,
                DnsResponseCode.NoError,
                true,
                900,
                false);
        });
        DnsRecursiveResolver resolver = ResolverWithTransport(transport);
        resolver.Entropy = new ScriptedIterativeEntropy(0x5000, 0);
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "TCP fallback");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            IterativeTransportOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Queries[0].UsedFallback, Is.True);
            Assert.That(result.Queries[0].Attempts.Select(item =>
                item.Transport), Is.EqualTo(new[]
                {
                    DnsTransportKind.Udp,
                    DnsTransportKind.Tcp
                }));
        });
    }

    [Test]
    public void SuppressesFailedAuthorityAndPrefersReachableServerLater()
    {
        ScriptedIterativeTransport transport =
            new ScriptedIterativeTransport();
        transport.Enqueue(TransportTimeout);
        transport.Enqueue(TransportTimeout);
        transport.Enqueue(request => TransportAnswer(
            request,
            DnsResponseCode.NoError,
            true,
            900,
            false));
        transport.Enqueue(request => TransportAnswer(
            request,
            DnsResponseCode.NoError,
            true,
            900,
            false));
        DnsRecursiveResolver resolver = ResolverWithTransport(transport);
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "authority reachability");
        DnsRecursiveOptions options = IterativeTransportOptions();

        DnsResolveResult first = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            options);
        DnsResolveRequest secondRequest = AddressRequest();
        secondRequest.Name = Name(true, "other", "example");
        DnsResolveResult second = resolver.ResolveIteratively(
            secondRequest,
            hints,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(second.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(transport.Requests.Select(item =>
                item.Endpoint.AddressFamily), Is.EqualTo(new[]
                {
                    4,
                    4,
                    6,
                    6
                }));
            Assert.That(resolver.ServerStates.Single(item =>
                item.Endpoint.AddressFamily == 4).ConsecutiveFailures,
                Is.EqualTo(2));
            Assert.That(resolver.ReachabilityStates.Single(item =>
                item.Endpoint.AddressFamily == 4).HasSuppressedUntil,
                Is.True);
        });
    }

    [Test]
    public void AcceptsInBailiwickGlueAndContinuesAtTheDelegation()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(ReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "referral");
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = Name(true, "www", "example");
        request.RecordType = DnsRecordType.A;
        request.RecordTypeValue = 1;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Delegations, Has.Count.EqualTo(2));
            Assert.That(Text(result.Delegations[1].Zone),
                Is.EqualTo("example."));
            Assert.That(result.Delegations[1].NameServers.Select(Text),
                Is.EqualTo(new[]
                {
                    "ns1.example.",
                    "ns2.example."
                }));
            Assert.That(result.Delegations[1].AuthorityRecordSets,
                Has.Count.EqualTo(1));
            Assert.That(result.Delegations[1].GlueAddresses,
                Has.Count.EqualTo(1));
            Assert.That(result.Delegations[1].GlueAddresses[0].Addresses[0],
                Is.EqualTo(new byte[] { 203, 0, 113, 10 }));
            Assert.That(result.Delegations[1].InBailiwick, Is.True);
            Assert.That(result.Delegations[1].Depth, Is.EqualTo(1));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(client.Requests[1].Servers[0].Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 10 }));
            Assert.That(Lookup(
                cache,
                Name(true, "example"),
                DnsRecordType.Ns).Entry!.Credibility, Is.EqualTo(3));
            Assert.That(Lookup(
                cache,
                Name(true, "ns1", "example"),
                DnsRecordType.A).Entry!.Credibility, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReusesLearnedDelegationAndGlueForASiblingQuery()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        resolver.Cache = new MemoryDnsCache();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(ReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "delegation reuse");

        DnsResolveResult first = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());
        DnsResolveRequest siblingRequest = AddressRequest();
        siblingRequest.Name = Name(true, "other", "example");
        DnsResolveResult sibling = resolver.ResolveIteratively(
            siblingRequest,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(sibling.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(3));
            Assert.That(client.Requests.Select(item =>
                Text(item.Question.Name)), Is.EqualTo(new[]
                {
                    "www.example.",
                    "www.example.",
                    "other.example."
                }));
            Assert.That(client.Requests[0].Servers[0].Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(client.Requests[1].Servers[0].Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 10 }));
            Assert.That(client.Requests[2].Servers[0].Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 10 }));
            Assert.That(sibling.Delegations, Has.Count.EqualTo(1));
            Assert.That(Text(sibling.Delegations[0].Zone),
                Is.EqualTo("example."));
        });
    }

    [Test]
    public void RejectsSiblingAndOutOfZoneAdditionalAddresses()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(MixedGlueReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "mixed glue");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        DnsDelegation referral = result.Delegations[1];
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(referral.GlueAddresses, Has.Count.EqualTo(1));
            Assert.That(Text(referral.GlueAddresses[0].CanonicalName),
                Is.EqualTo("ns1.example."));
            Assert.That(Lookup(
                cache,
                Name(true, "sibling", "example"),
                DnsRecordType.A).Found, Is.False);
            Assert.That(Lookup(
                cache,
                Name(true, "ns", "external", "net"),
                DnsRecordType.A).Found, Is.False);
            Assert.That(client.Requests[1].Servers[0].Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 10 }));
        });
    }

    [Test]
    public void ReferralGlueDoesNotReplaceHigherCredibilityCacheData()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(ReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "credibility");
        DnsClockValue now = TransportTime.ReadClock();
        DnsCacheEntry trusted = CacheEntry(
            AddressSet(
                Name(true, "ns1", "example"),
                DnsRecordType.A,
                90,
                [198, 51, 100, 44]),
            now);
        trusted.Credibility = 4;
        cache.Store(trusted);

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        DnsCacheLookup retained = Lookup(
            cache,
            Name(true, "ns1", "example"),
            DnsRecordType.A);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests[1].Servers[0].Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 10 }));
            Assert.That(retained.Entry!.Credibility, Is.EqualTo(4));
            Assert.That(retained.Entry.RecordSets[0].Records[0]
                .Data!.Address!.Address,
                Is.EqualTo(new byte[] { 198, 51, 100, 44 }));
        });
    }

    [Test]
    public void SuppressesALameReferralAndUsesAnotherAuthority()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(InvalidReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "invalid referral");
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = Name(true, "www", "example");
        request.RecordType = DnsRecordType.A;
        request.RecordTypeValue = 1;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Delegations, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "referral"), Is.True);
            Assert.That(result.Delegations[0].Lame, Is.False);
            Assert.That(client.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ResolvesAnOutOfBailiwickNameServerAddressAndResumes()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(ReferralWithoutGlueAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "recursive name server");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        DnsCacheLookup address = Lookup(
            cache,
            Name(true, "ns", "external", "net"),
            DnsRecordType.A);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(3));
            Assert.That(Text(client.Requests[1].Question.Name),
                Is.EqualTo("ns.external.net."));
            Assert.That(client.Requests[1].Question.RecordType,
                Is.EqualTo(DnsRecordType.A));
            Assert.That(Text(client.Requests[2].Question.Name),
                Is.EqualTo("www.example."));
            Assert.That(client.Requests[2].Servers[0].Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 100 }));
            Assert.That(address.Found && address.Fresh, Is.True);
            Assert.That(address.Entry!.Credibility, Is.EqualTo(4));
        });
    }

    [Test]
    public void StopsWhenAReferralNameServerHasNoAddressRecords()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(ReferralWithoutGlueAnswer);
        client.Responses.Enqueue(EmptyNonterminalAnswer);
        client.Responses.Enqueue(EmptyNonterminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "missing name-server addresses");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.ServerFailure));
            Assert.That(client.Requests.Select(item =>
                Text(item.Question.Name)), Is.EqualTo(new[]
                {
                    "www.example.",
                    "ns.external.net.",
                    "ns.external.net."
                }));
            Assert.That(client.Requests.Select(item =>
                item.Question.RecordTypeValue), Is.EqualTo(new[]
                {
                    1,
                    1,
                    28
                }));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "authority"), Is.True);
            Assert.That(result.Delegations, Has.Count.EqualTo(2));
            Assert.That(result.Delegations[1].GlueAddresses, Is.Empty);
        });
    }

    [Test]
    public void ReportsACyclicNameServerAddressDependency()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(CyclicReferralAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "cyclic name server");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Loop));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "dependency"), Is.True);
        });
    }

    [Test]
    public void RetriesTheQuestionAtAnAlternateAuthorityEndpoint()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TimedOutAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "alternate authority");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(client.Requests[0].Servers[0].AddressFamily,
                Is.EqualTo(4));
            Assert.That(client.Requests[1].Servers[0].AddressFamily,
                Is.EqualTo(6));
        });
    }

    [Test]
    public void MarksADelegationLameWhenNoAuthorityMakesProgress()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(InvalidReferralAnswer);
        client.Responses.Enqueue(InvalidReferralAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "lame authorities");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.ServerFailure));
            Assert.That(result.Delegations[0].Lame, Is.True);
            Assert.That(client.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void FollowsCnameAcrossAuthoritativeResponses()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(CnameAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "cname answer");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps[0].RecordType,
                Is.EqualTo(DnsRecordType.Cname));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("target.example."));
            Assert.That(result.TimeToLive, Is.EqualTo(25));
            Assert.That(client.Requests.Select(item => Text(
                item.Question.Name)), Is.EqualTo(new[]
                {
                    "www.example.",
                    "target.example."
                }));
        });
    }

    [Test]
    public void CompletesCnameChainFromOneAuthoritativeResponse()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(CnameWithTerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "complete cname answer");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("target.example."));
            Assert.That(result.RecordSets, Has.Count.EqualTo(1));
            Assert.That(Text(result.RecordSets[0].Name),
                Is.EqualTo("target.example."));
            Assert.That(result.RecordSets[0].Records[0].Data!
                .Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 101 }));
            Assert.That(result.TimeToLive, Is.EqualTo(25));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SynthesizesDnameAndContinuesAtTheTarget()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(DnameAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "dname answer");
        DnsResolveRequest request = AddressRequest();
        request.Name = Name(true, "www", "sub", "example");

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps[0].Synthesized, Is.True);
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("www.sub.example.net."));
            Assert.That(Text(client.Requests[1].Question.Name),
                Is.EqualTo("www.sub.example.net."));
        });
    }

    [Test]
    public void ReturnsAuthoritativeNameErrorWithNegativeTtl()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(NegativeNameAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "negative name");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.NameError));
            Assert.That(result.RecordSets, Is.Empty);
            Assert.That(result.HasTimeToLive, Is.True);
            Assert.That(result.TimeToLive, Is.EqualTo(60));
        });
    }

    [Test]
    public void ReusesCachedAuthoritativeAnswerWithAnswerCredibility()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "positive cache");

        DnsResolveResult first = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());
        DnsResolveResult cached = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());
        DnsCacheLookup lookup = Lookup(
            cache,
            Name(true, "www", "example"),
            DnsRecordType.A);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(cached.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(cached.FromCache, Is.True);
            Assert.That(cached.RecordSets, Has.Count.EqualTo(1));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
            Assert.That(lookup.Entry!.Credibility, Is.EqualTo(4));
            Assert.That(lookup.Entry.Authoritative, Is.True);
        });
    }

    [Test]
    public void ReusesCachedAuthoritativeNegativeAnswer()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(NegativeNameAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "negative cache");

        DnsResolveResult first = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());
        DnsResolveResult cached = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.NameError));
            Assert.That(cached.Status,
                Is.EqualTo(DnsResolveStatus.NameError));
            Assert.That(cached.FromCache, Is.True);
            Assert.That(cached.TimeToLive, Is.EqualTo(60));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ReturnsNoDataForTypeAbsenceAndEmptyNonterminal()
    {
        DnsRootHints typeHints;
        DnsRecursiveResolver typeResolver = ResolverWithRoot(
            NoDataAnswer,
            out typeHints);
        DnsResolveResult typeAbsent = typeResolver.ResolveIteratively(
            AddressRequest(),
            typeHints,
            NonMinimizedOptions());

        DnsRootHints emptyHints;
        DnsRecursiveResolver emptyResolver = ResolverWithRoot(
            EmptyNonterminalAnswer,
            out emptyHints);
        DnsResolveResult empty = emptyResolver.ResolveIteratively(
            AddressRequest(),
            emptyHints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(typeAbsent.Status,
                Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(empty.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(typeAbsent.TimeToLive, Is.EqualTo(60));
            Assert.That(empty.TimeToLive, Is.EqualTo(60));
        });
    }

    [Test]
    public void AcceptsTheExpandedOwnerOfAWildcardAnswer()
    {
        DnsRootHints hints;
        DnsRecursiveResolver resolver = ResolverWithRoot(
            TerminalAnswer,
            out hints);

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.RecordSets, Has.Count.EqualTo(1));
            Assert.That(Text(result.RecordSets[0].Name),
                Is.EqualTo("www.example."));
            Assert.That(result.RecordSets[0].Records[0].Data!
                .Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 100 }));
        });
    }

    [Test]
    public void MinimizesQuestionsOneLabelBeyondEachDelegation()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(ReferralAnswer);
        client.Responses.Enqueue(DeepReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "minimized questions");
        DnsResolveRequest request = AddressRequest();
        request.Name = Name(true, "www", "deep", "example");

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            MinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests.Select(item => Text(
                item.Question.Name)), Is.EqualTo(new[]
                {
                    "example.",
                    "deep.example.",
                    "www.deep.example."
                }));
            Assert.That(client.Requests.Select(item =>
                item.Question.RecordType), Is.EqualTo(new[]
                {
                    DnsRecordType.Ns,
                    DnsRecordType.Ns,
                    DnsRecordType.A
                }));
        });
    }

    [Test]
    public void FallsBackPerZoneWithoutDisablingLaterMinimization()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(RefusedAnswer);
        client.Responses.Enqueue(ReferralAnswer);
        client.Responses.Enqueue(DeepReferralAnswer);
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "minimization fallback");
        DnsResolveRequest request = AddressRequest();
        request.Name = Name(true, "www", "deep", "example");

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            MinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests.Select(item => Text(
                item.Question.Name)), Is.EqualTo(new[]
                {
                    "example.",
                    "www.deep.example.",
                    "deep.example.",
                    "www.deep.example."
                }));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "minimization"), Is.True);
        });
    }

    [Test]
    public void StopsWhenMinimizationFallbackIsDisabled()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(RefusedAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "fallback disabled");
        DnsResolveRequest request = AddressRequest();
        request.Name = Name(true, "www", "deep", "example");
        DnsRecursiveOptions options = MinimizedOptions();
        options.AllowMinimizationFallback = false;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.ServerFailure));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
            Assert.That(Text(client.Requests[0].Question.Name),
                Is.EqualTo("example."));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "minimization"), Is.True);
        });
    }

    [Test]
    public void EnforcesQueryWorkDepthAndReferralBudgets()
    {
        DnsRootHints queryHints;
        DnsRecursiveResolver queryResolver = ResolverWithRoot(
            TerminalAnswer,
            out queryHints);
        DnsRecursiveOptions noQueries = NonMinimizedOptions();
        noQueries.MaximumQueries = 0;
        DnsResolveResult queryLimited = queryResolver.ResolveIteratively(
            AddressRequest(),
            queryHints,
            noQueries);

        DnsRootHints workHints;
        DnsRecursiveResolver workResolver = ResolverWithRoot(
            TerminalAnswer,
            out workHints);
        DnsRecursiveOptions noCandidateWork = NonMinimizedOptions();
        noCandidateWork.MaximumWork = 1;
        DnsResolveResult workLimited = workResolver.ResolveIteratively(
            AddressRequest(),
            workHints,
            noCandidateWork);

        DnsRootHints depthHints;
        DnsRecursiveResolver depthResolver = ResolverWithRoot(
            ReferralAnswer,
            out depthHints);
        DnsRecursiveOptions noChildDepth = NonMinimizedOptions();
        noChildDepth.MaximumDepth = 0;
        DnsResolveResult depthLimited = depthResolver.ResolveIteratively(
            AddressRequest(),
            depthHints,
            noChildDepth);

        DnsRootHints referralHints;
        DnsRecursiveResolver referralResolver = ResolverWithRoot(
            ReferralAnswer,
            out referralHints);
        DnsRecursiveOptions noReferrals = NonMinimizedOptions();
        noReferrals.MaximumReferralDepth = 0;
        DnsResolveResult referralLimited = referralResolver.ResolveIteratively(
            AddressRequest(),
            referralHints,
            noReferrals);

        Assert.Multiple(() =>
        {
            AssertResourceLimit(queryLimited, "queries");
            Assert.That(((ScriptedIterativeClient)queryResolver.QueryClient)
                .Requests, Is.Empty);
            AssertResourceLimit(workLimited, "work");
            Assert.That(((ScriptedIterativeClient)workResolver.QueryClient)
                .Requests, Is.Empty);
            AssertResourceLimit(depthLimited, "referrals");
            Assert.That(depthLimited.Delegations, Has.Count.EqualTo(1));
            AssertResourceLimit(referralLimited, "referrals");
            Assert.That(referralLimited.Delegations, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void MapsQueryTransportFailureAndRetainsQueryMetadata()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TimedOutAnswer);
        client.Responses.Enqueue(TimedOutAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "timeout");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());
        DnsResolveResult cached = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.TimedOut));
            Assert.That(result.Queries, Has.Count.EqualTo(2));
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Delegations, Has.Count.EqualTo(1));
            Assert.That(cached.Status,
                Is.EqualTo(DnsResolveStatus.ServerFailure));
            Assert.That(cached.FromCache, Is.True);
            Assert.That(client.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void RejectsAnAnswerThatArrivesAfterTheTotalLifetime()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClock clock = new ScriptedIterativeClock(1000);
        resolver.Clock = clock;
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(request =>
        {
            clock.MonotonicMilliseconds = 1002;
            return TerminalAnswer(request);
        });
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "lifetime");
        DnsRecursiveOptions options = NonMinimizedOptions();
        options.TotalTimeoutMilliseconds = 1;

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.TimedOut));
            Assert.That(result.RecordSets, Is.Empty);
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == DnsDiagnosticCode.ResourceLimitExceeded &&
                item.Field == "lifetime"), Is.True);
        });
    }

    [Test]
    public void UsesAStaleAnswerOnlyAfterIterativeRefreshFails()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        DnsClockValue now = TransportTime.ReadClock();
        DnsCacheEntry entry = CacheEntry(
            AddressSet(
                Name(true, "www", "example"),
                DnsRecordType.A,
                1,
                [192, 0, 2, 44]),
            now);
        entry.StoredAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds - 2000;
        entry.StoredAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds - 2000;
        entry.ExpiresAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds - 1000;
        entry.ExpiresAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds - 1000;
        entry.HasStaleUntil = true;
        entry.StaleUntil.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds + 60000;
        entry.StaleUntil.MonotonicMilliseconds =
            now.MonotonicMilliseconds + 60000;
        cache.Store(entry);
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TimedOutAnswer);
        client.Responses.Enqueue(TimedOutAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "stale fallback");
        DnsResolveRequest request = AddressRequest();
        request.Options.AllowStale = true;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.FromCache, Is.True);
            Assert.That(result.Stale, Is.True);
            Assert.That(result.RecordSets[0].Records[0]
                .Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 44 }));
            Assert.That(result.Queries, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "cache.stale"), Is.True);
        });
    }

    [Test]
    public void RefreshesAFreshAnswerAtThePrefetchThreshold()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        DnsClockValue now = TransportTime.ReadClock();
        DnsCacheEntry entry = CacheEntry(
            AddressSet(
                Name(true, "www", "example"),
                DnsRecordType.A,
                100,
                [192, 0, 2, 45]),
            now);
        entry.StoredAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds - 95000;
        entry.StoredAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds - 95000;
        entry.ExpiresAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds + 5000;
        entry.ExpiresAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds + 5000;
        cache.Store(entry);
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TerminalAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "prefetch");
        DnsResolveRequest request = AddressRequest();
        request.Options.Prefetch = true;
        request.Options.PrefetchThresholdPercent = 10;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.FromCache, Is.False);
            Assert.That(result.RecordSets[0].Records[0]
                .Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 100 }));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RetainsTheFreshAnswerWhenPrefetchFails()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        DnsClockValue now = TransportTime.ReadClock();
        DnsCacheEntry entry = CacheEntry(
            AddressSet(
                Name(true, "www", "example"),
                DnsRecordType.A,
                100,
                [192, 0, 2, 46]),
            now);
        entry.StoredAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds - 95000;
        entry.StoredAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds - 95000;
        entry.ExpiresAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds + 5000;
        entry.ExpiresAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds + 5000;
        cache.Store(entry);
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(TimedOutAnswer);
        client.Responses.Enqueue(TimedOutAnswer);
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "prefetch fallback");
        DnsResolveRequest request = AddressRequest();
        request.Options.Prefetch = true;
        request.Options.PrefetchThresholdPercent = 10;

        DnsResolveResult result = resolver.ResolveIteratively(
            request,
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.FromCache, Is.True);
            Assert.That(result.Stale, Is.False);
            Assert.That(result.RecordSets[0].Records[0]
                .Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 46 }));
            Assert.That(result.Queries, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "cache.prefetch"), Is.True);
        });
    }

    [TestCase(DnsExtendedErrorCode.Blocked, DnsResolveStatus.Refused)]
    [TestCase(DnsExtendedErrorCode.NotSupported, DnsResolveStatus.Unsupported)]
    [TestCase(DnsExtendedErrorCode.DnssecBogus, DnsResolveStatus.ProtocolError)]
    public void MapsExtendedDnsErrorsToFinalFailureStatus(
        DnsExtendedErrorCode code,
        DnsResolveStatus expected)
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(request => EdeFailureAnswer(request, code));
        client.Responses.Enqueue(request => EdeFailureAnswer(request, code));
        resolver.QueryClient = client;
        DnsRootHints hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "extended error");

        DnsResolveResult result = resolver.ResolveIteratively(
            AddressRequest(),
            hints,
            NonMinimizedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "response.ede" &&
                item.Reason.Contains("policy")), Is.True);
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "resolution"), Is.True);
        });
    }

    static DnsRecursiveResolver ResolverWithRoot(
        Func<DnsQueryRequest, DnsQueryResult> response,
        out DnsRootHints hints)
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        ScriptedIterativeClient client = new ScriptedIterativeClient();
        client.Responses.Enqueue(response);
        resolver.QueryClient = client;
        hints = resolver.LoadRootHints(
            RootZone([192, 0, 2, 1], Ipv6(2)),
            "budget");
        return resolver;
    }

    static DnsResolveRequest AddressRequest()
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = Name(true, "www", "example");
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        return result;
    }

    static DnsRecursiveOptions NonMinimizedOptions()
    {
        DnsRecursiveOptions result = new DnsRecursiveOptions();
        result.MinimizeQueryName = false;
        result.RandomizeQueryCase = false;
        return result;
    }

    static DnsRecursiveOptions MinimizedOptions()
    {
        DnsRecursiveOptions result = new DnsRecursiveOptions();
        result.RandomizeQueryCase = false;
        return result;
    }

    static DnsRecursiveOptions IterativeTransportOptions()
    {
        DnsRecursiveOptions result = NonMinimizedOptions();
        result.UseEdns = true;
        result.ValidateDnssec = false;
        result.AllowEdnsDowngrade = true;
        result.AllowTcpFallback = true;
        return result;
    }

    static DnsRecursiveResolver ResolverWithTransport(
        ScriptedIterativeTransport transport)
    {
        DnsQueryClient client = new DnsQueryClient();
        client.NextServerOffset = 0;
        client.UdpTransport = transport;
        client.TcpTransport = transport;
        DnsRecursiveResolver result = new DnsRecursiveResolver();
        result.QueryClient = client;
        result.Entropy = new ScriptedIterativeEntropy(0x6000, 0);
        return result;
    }

    static void AssertResourceLimit(DnsResolveResult result, string field)
    {
        Assert.That(result.Status,
            Is.EqualTo(DnsResolveStatus.DepthLimitExceeded));
        Assert.That(result.Diagnostics.Any(item =>
            item.Code == DnsDiagnosticCode.ResourceLimitExceeded &&
            item.Field == field), Is.True);
    }

    static DnsQueryResult TerminalAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Header.Flags = response.Header.Flags |
            DnsHeaderFlag.AuthoritativeAnswer;
        response.Header.FlagsValue = (int)response.Header.Flags;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = request.Question.Name;
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 30;
        record.Data = new DnsRecordData();
        record.Data.Address = new DnsAddressData();
        record.Data.Address.Address = [192, 0, 2, 100];
        response.Answers.Add(record);
        return SuccessfulQuery(response);
    }

    static DnsQueryResult WrongQuestionCaseAnswer(DnsQueryRequest request)
    {
        DnsQueryResult result = TerminalAnswer(request);
        DnsQuestion question = new DnsQuestion();
        question.Name = Name(true, "www", "example");
        question.RecordType = request.Question.RecordType;
        question.RecordTypeValue = request.Question.RecordTypeValue;
        question.Class = request.Question.Class;
        question.ClassValue = request.Question.ClassValue;
        result.Response!.Questions[0] = question;
        return result;
    }

    static DnsQueryResult WrongIdentifierAnswer(DnsQueryRequest request)
    {
        DnsQueryResult result = TerminalAnswer(request);
        result.Response!.Header.Identifier =
            (ushort)(result.Response.Header.Identifier + 1);
        return result;
    }

    static DnsQueryResult WrongSourceAnswer(DnsQueryRequest request)
    {
        DnsQueryResult result = TerminalAnswer(request);
        DnsTransportResult attempt = new DnsTransportResult();
        attempt.Status = DnsTransportStatus.Success;
        attempt.Transport = DnsTransportKind.Udp;
        attempt.Endpoint.HasAddress = true;
        attempt.Endpoint.AddressFamily = 4;
        attempt.Endpoint.Address = [198, 51, 100, 1];
        attempt.Endpoint.Port = DnsPorts.Dns;
        result.Attempts.Add(attempt);
        return result;
    }

    static DnsQueryResult MatchedSourceAnswer(DnsQueryRequest request)
    {
        DnsQueryResult result = TerminalAnswer(request);
        DnsTransportResult attempt = new DnsTransportResult();
        attempt.Status = DnsTransportStatus.Success;
        attempt.Transport = DnsTransportKind.Udp;
        attempt.Endpoint = request.Servers[0];
        result.Attempts.Add(attempt);
        return result;
    }

    static DnsTransportResult TransportAnswer(
        DnsTransportRequest request,
        DnsResponseCode responseCode,
        bool includeEdns,
        int udpPayloadSize,
        bool truncated)
    {
        DnsMessage query = request.QueryMessage!;
        DnsMessage response = new DnsMessage();
        response.Header.Identifier = query.Header.Identifier;
        response.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        if (truncated)
        {
            response.Header.Flags = response.Header.Flags |
                DnsHeaderFlag.Truncated;
        }

        response.Header.FlagsValue = (int)response.Header.Flags;
        response.Header.ResponseCode = responseCode;
        response.Header.ResponseCodeValue = (int)responseCode;
        response.Questions = new DnsMessageCodec().CloneMessage(
            query).Questions;
        if (responseCode == DnsResponseCode.NoError)
        {
            DnsResourceRecord answer = AddressRecord(
                response.Questions[0].Name,
                [192, 0, 2, 100]);
            answer.TimeToLive = 30;
            response.Answers.Add(answer);
        }

        if (includeEdns)
        {
            response = EdnsOperations.PrepareResponse(
                query,
                response,
                udpPayloadSize).Message;
        }

        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            response,
            new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        DnsTransportResult result = TransportResult(request);
        result.Status = DnsTransportStatus.Success;
        result.ResponseMessage = response;
        result.ResponseBytes = encoded.Bytes;
        result.Truncated = truncated;
        return result;
    }

    static DnsTransportResult TransportTimeout(DnsTransportRequest request)
    {
        DnsTransportResult result = TransportResult(request);
        result.Status = DnsTransportStatus.TimedOut;
        result.TimedOut = true;
        return result;
    }

    static DnsTransportResult TransportResult(DnsTransportRequest request)
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Endpoint = request.Endpoint;
        result.Transport = request.Options.Kind;
        result.StartedAt.UnixTimeMilliseconds =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        result.StartedAt.MonotonicMilliseconds = Environment.TickCount64;
        result.CompletedAt.UnixTimeMilliseconds =
            result.StartedAt.UnixTimeMilliseconds;
        result.CompletedAt.MonotonicMilliseconds =
            result.StartedAt.MonotonicMilliseconds;
        return result;
    }

    static int EdnsPayload(DnsMessage message)
    {
        DnsResourceRecord? opt = MessageOperations.FindOptRecord(message);
        if (opt == null || opt.Data == null || opt.Data.Edns == null)
        {
            return 0;
        }

        return opt.Data.Edns.UdpPayloadSize;
    }

    static DnsQueryResult CnameAnswer(DnsQueryRequest request)
    {
        return AliasAnswer(
            request,
            request.Question.Name,
            DnsRecordType.Cname,
            Name(true, "target", "example"),
            25);
    }

    static DnsQueryResult CnameWithTerminalAnswer(DnsQueryRequest request)
    {
        DnsQueryResult result = CnameAnswer(request);
        result.Response!.Answers.Add(AddressRecord(
            Name(true, "target", "example"),
            [192, 0, 2, 101]));
        return result;
    }

    static DnsQueryResult DnameAnswer(DnsQueryRequest request)
    {
        return AliasAnswer(
            request,
            Name(true, "example"),
            DnsRecordType.Dname,
            Name(true, "example", "net"),
            40);
    }

    static DnsQueryResult AliasAnswer(
        DnsQueryRequest request,
        DnsName owner,
        DnsRecordType recordType,
        DnsName target,
        uint timeToLive)
    {
        DnsMessage response = Response(request);
        response.Header.Flags = response.Header.Flags |
            DnsHeaderFlag.AuthoritativeAnswer;
        response.Header.FlagsValue = (int)response.Header.Flags;
        DnsResourceRecord record = NameServerRecord(owner, target);
        record.RecordType = recordType;
        record.RecordTypeValue = (int)recordType;
        record.TimeToLive = timeToLive;
        response.Answers.Add(record);
        return SuccessfulQuery(response);
    }

    static DnsQueryResult NegativeNameAnswer(DnsQueryRequest request)
    {
        DnsMessage response = NegativeResponse(
            request,
            DnsResponseCode.NameError);
        return SuccessfulQuery(response);
    }

    static DnsQueryResult NoDataAnswer(DnsQueryRequest request)
    {
        DnsMessage response = NegativeResponse(
            request,
            DnsResponseCode.NoError);
        DnsResourceRecord other = AddressRecord(
            request.Question.Name,
            Ipv6(9));
        other.RecordType = DnsRecordType.Aaaa;
        other.RecordTypeValue = 28;
        response.Answers.Add(other);
        return SuccessfulQuery(response);
    }

    static DnsQueryResult EmptyNonterminalAnswer(DnsQueryRequest request)
    {
        return SuccessfulQuery(NegativeResponse(
            request,
            DnsResponseCode.NoError));
    }

    static DnsMessage NegativeResponse(
        DnsQueryRequest request,
        DnsResponseCode responseCode)
    {
        DnsMessage response = Response(request);
        response.Header.Flags = response.Header.Flags |
            DnsHeaderFlag.AuthoritativeAnswer;
        response.Header.FlagsValue = (int)response.Header.Flags;
        response.Header.ResponseCode = responseCode;
        response.Header.ResponseCodeValue = (int)responseCode;
        DnsResourceRecord soa = new DnsResourceRecord();
        soa.Name = Name(true);
        soa.RecordType = DnsRecordType.Soa;
        soa.RecordTypeValue = 6;
        soa.Class = DnsClass.Internet;
        soa.ClassValue = 1;
        soa.TimeToLive = 300;
        soa.Data = new DnsRecordData();
        soa.Data.Soa = new DnsSoaData();
        soa.Data.Soa.PrimaryNameServer =
            Name(true, "a", "root-servers", "net");
        soa.Data.Soa.ResponsibleMailbox =
            Name(true, "hostmaster", "root");
        soa.Data.Soa.Minimum = 60;
        response.Authorities.Add(soa);
        return response;
    }

    static DnsQueryResult ReferralAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Authorities.Add(NameServerRecord(
            Name(true, "example"),
            Name(true, "ns1", "example")));
        response.Authorities.Add(NameServerRecord(
            Name(true, "example"),
            Name(true, "ns2", "example")));
        DnsResourceRecord glue = new DnsResourceRecord();
        glue.Name = Name(true, "ns1", "example");
        glue.RecordType = DnsRecordType.A;
        glue.RecordTypeValue = 1;
        glue.Class = DnsClass.Internet;
        glue.ClassValue = 1;
        glue.TimeToLive = 60;
        glue.Data = new DnsRecordData();
        glue.Data.Address = new DnsAddressData();
        glue.Data.Address.Address = [203, 0, 113, 10];
        response.Additionals.Add(glue);
        return SuccessfulQuery(response);
    }

    static DnsQueryResult DeepReferralAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Authorities.Add(NameServerRecord(
            Name(true, "deep", "example"),
            Name(true, "ns", "deep", "example")));
        response.Additionals.Add(AddressRecord(
            Name(true, "ns", "deep", "example"),
            [203, 0, 113, 20]));
        return SuccessfulQuery(response);
    }

    static DnsQueryResult RefusedAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Header.ResponseCode = DnsResponseCode.Refused;
        response.Header.ResponseCodeValue = (int)DnsResponseCode.Refused;
        return SuccessfulQuery(response);
    }

    static DnsQueryResult InvalidReferralAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Authorities.Add(NameServerRecord(
            Name(true, "unrelated"),
            Name(true, "ns", "unrelated")));
        return SuccessfulQuery(response);
    }

    static DnsQueryResult MixedGlueReferralAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Authorities.Add(NameServerRecord(
            Name(true, "example"),
            Name(true, "ns1", "example")));
        response.Authorities.Add(NameServerRecord(
            Name(true, "example"),
            Name(true, "ns", "external", "net")));
        response.Additionals.Add(AddressRecord(
            Name(true, "ns1", "example"),
            [203, 0, 113, 10]));
        response.Additionals.Add(AddressRecord(
            Name(true, "sibling", "example"),
            [203, 0, 113, 11]));
        response.Additionals.Add(AddressRecord(
            Name(true, "ns", "external", "net"),
            [203, 0, 113, 12]));
        return SuccessfulQuery(response);
    }

    static DnsQueryResult ReferralWithoutGlueAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Authorities.Add(NameServerRecord(
            Name(true, "example"),
            Name(true, "ns", "external", "net")));
        return SuccessfulQuery(response);
    }

    static DnsQueryResult CyclicReferralAnswer(DnsQueryRequest request)
    {
        DnsMessage response = Response(request);
        response.Authorities.Add(NameServerRecord(
            Name(true, "example"),
            Name(true, "ns1", "example")));
        return SuccessfulQuery(response);
    }

    static DnsQueryResult TimedOutAnswer(DnsQueryRequest request)
    {
        DnsQueryResult result = new DnsQueryResult();
        result.Status = DnsTransportStatus.TimedOut;
        return result;
    }

    static DnsQueryResult EdeFailureAnswer(
        DnsQueryRequest request,
        DnsExtendedErrorCode code)
    {
        DnsMessage response = Response(request);
        response.Header.ResponseCode = DnsResponseCode.ServerFailure;
        response.Header.ResponseCodeValue =
            (int)DnsResponseCode.ServerFailure;
        DnsResourceRecord opt = MessageOperations.CreateOptRecord();
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.ExtendedDnsError;
        option.CodeValue = (int)DnsEdnsOptionCode.ExtendedDnsError;
        option.ExtendedError = new DnsEdnsExtendedError();
        option.ExtendedError.Code = code;
        option.ExtendedError.CodeValue = (int)code;
        option.ExtendedError.ExtraText = "policy detail";
        opt.Data!.Edns!.Options.Add(option);
        response.Additionals.Add(opt);
        return SuccessfulQuery(response);
    }

    static DnsMessage Response(DnsQueryRequest request)
    {
        DnsMessage result = new DnsMessage();
        if (request.Message != null)
        {
            result.Header.Identifier = request.Message.Header.Identifier;
        }

        result.Header.Flags = DnsHeaderFlag.Response;
        result.Header.FlagsValue = (int)result.Header.Flags;
        result.Header.Opcode = DnsOpcode.Query;
        result.Header.OpcodeValue = 0;
        result.Header.ResponseCode = DnsResponseCode.NoError;
        result.Header.ResponseCodeValue = 0;
        result.Questions.Add(request.Question);
        return result;
    }

    static DnsResourceRecord NameServerRecord(DnsName owner, DnsName target)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = owner;
        result.RecordType = DnsRecordType.Ns;
        result.RecordTypeValue = 2;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        result.Data = new DnsRecordData();
        result.Data.Name = new DnsNameData();
        result.Data.Name.Name = target;
        return result;
    }

    static DnsResourceRecord AddressRecord(DnsName owner, byte[] address)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = owner;
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 60;
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = address;
        return result;
    }

    static DnsQueryResult SuccessfulQuery(DnsMessage response)
    {
        DnsQueryResult result = new DnsQueryResult();
        result.Success = true;
        result.Status = DnsTransportStatus.Success;
        result.Response = response;
        return result;
    }

    static DnsZone RootZone(byte[] ipv4, byte[] ipv6)
    {
        DnsName root = Name(true);
        DnsName first = Name(true, "a", "root-servers", "net");
        DnsName second = Name(true, "b", "root-servers", "net");
        DnsZone zone = new DnsZone();
        zone.Apex = root;
        zone.Class = DnsClass.Internet;
        zone.ClassValue = 1;
        DnsZoneNode rootNode = new DnsZoneNode();
        rootNode.Name = root;
        rootNode.RecordSets.Add(NameServerSet(root, 3600, first, second));
        DnsZoneNode firstNode = new DnsZoneNode();
        firstNode.Name = first;
        firstNode.RecordSets.Add(AddressSet(
            first,
            DnsRecordType.A,
            300,
            ipv4));
        DnsZoneNode secondNode = new DnsZoneNode();
        secondNode.Name = second;
        secondNode.RecordSets.Add(AddressSet(
            second,
            DnsRecordType.Aaaa,
            600,
            ipv6));
        rootNode.Children.Add(firstNode);
        rootNode.Children.Add(secondNode);
        zone.Nodes.Add(rootNode);
        return zone;
    }

    static DnsResourceRecordSet NameServerSet(
        DnsName owner,
        uint ttl,
        params DnsName[] targets)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = owner;
        result.RecordType = DnsRecordType.Ns;
        result.RecordTypeValue = 2;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = ttl;
        int index = 0;
        while (index < targets.Length)
        {
            DnsResourceRecord record = new DnsResourceRecord();
            record.Name = owner;
            record.RecordType = DnsRecordType.Ns;
            record.RecordTypeValue = 2;
            record.Class = DnsClass.Internet;
            record.ClassValue = 1;
            record.TimeToLive = ttl;
            record.Data = new DnsRecordData();
            record.Data.Name = new DnsNameData();
            record.Data.Name.Name = targets[index];
            result.Records.Add(record);
            index = index + 1;
        }

        return result;
    }

    static DnsResourceRecordSet AddressSet(
        DnsName owner,
        DnsRecordType recordType,
        uint ttl,
        params byte[][] addresses)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = owner;
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = ttl;
        int index = 0;
        while (index < addresses.Length)
        {
            DnsResourceRecord record = new DnsResourceRecord();
            record.Name = owner;
            record.RecordType = recordType;
            record.RecordTypeValue = (int)recordType;
            record.Class = DnsClass.Internet;
            record.ClassValue = 1;
            record.TimeToLive = ttl;
            record.Data = new DnsRecordData();
            record.Data.Address = new DnsAddressData();
            record.Data.Address.Address = addresses[index];
            result.Records.Add(record);
            index = index + 1;
        }

        return result;
    }

    static void StoreDelegation(
        MemoryDnsCache cache,
        DnsName zone,
        DnsName nameServer,
        byte[] address)
    {
        DnsClockValue now = TransportTime.ReadClock();
        DnsResourceRecordSet nsSet = NameServerSet(
            zone,
            120,
            nameServer);
        cache.Store(CacheEntry(nsSet, now));
        DnsResourceRecordSet addressSet = AddressSet(
            nameServer,
            DnsRecordType.A,
            90,
            address);
        cache.Store(CacheEntry(addressSet, now));
    }

    static DnsCacheEntry CacheEntry(
        DnsResourceRecordSet set,
        DnsClockValue now)
    {
        DnsCacheEntry result = new DnsCacheEntry();
        result.Kind = DnsCacheEntryKind.Positive;
        result.Name = set.Name;
        result.RecordType = set.RecordType;
        result.RecordTypeValue = set.RecordTypeValue;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.RecordSets.Add(set);
        result.SecurityStatus = DnsSecurityStatus.Insecure;
        result.StoredAt.UnixTimeMilliseconds = now.UnixTimeMilliseconds;
        result.StoredAt.MonotonicMilliseconds = now.MonotonicMilliseconds;
        result.OriginalTimeToLive = set.TimeToLive;
        result.ExpiresAt.UnixTimeMilliseconds =
            now.UnixTimeMilliseconds + (long)set.TimeToLive * 1000;
        result.ExpiresAt.MonotonicMilliseconds =
            now.MonotonicMilliseconds + (long)set.TimeToLive * 1000;
        result.Credibility = 3;
        return result;
    }

    static DnsCacheLookup Lookup(
        MemoryDnsCache cache,
        DnsName name,
        DnsRecordType recordType)
    {
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = name;
        request.RecordType = recordType;
        request.RecordTypeValue = (int)recordType;
        request.Class = DnsClass.Internet;
        request.ClassValue = 1;
        return cache.Lookup(request, TransportTime.ReadClock());
    }

    static byte[] Ipv6(byte last)
    {
        return [32, 1, 13, 184, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, last];
    }

    static DnsName Name(bool absolute, params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = absolute;
        int index = 0;
        while (index < labels.Length)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return result;
    }

    static string Text(DnsName? name)
    {
        if (name == null)
        {
            return string.Empty;
        }

        if (name.Labels.Count == 0)
        {
            return name.IsAbsolute ? "." : string.Empty;
        }

        string result = string.Empty;
        int index = 0;
        while (index < name.Labels.Count)
        {
            if (index > 0)
            {
                result = result + ".";
            }

            result = result + Encoding.ASCII.GetString(name.Labels[index]);
            index = index + 1;
        }

        return name.IsAbsolute ? result + "." : result;
    }

    class ScriptedIterativeClient : IDnsQueryClient
    {
        internal Queue<Func<DnsQueryRequest, DnsQueryResult>> Responses = new();
        internal List<DnsQueryRequest> Requests = new();

        public DnsQueryResult Query(DnsQueryRequest request)
        {
            Requests.Add(request);
            DnsQueryResult result = Responses.Dequeue()(request);
            if (result.Success && result.Attempts.Count == 0)
            {
                DnsTransportResult attempt = new DnsTransportResult();
                attempt.Status = DnsTransportStatus.Success;
                attempt.Transport = DnsTransportKind.Udp;
                attempt.Endpoint = request.Servers[0];
                result.Attempts.Add(attempt);
            }

            return result;
        }
    }

    class ScriptedIterativeEntropy : IDnsIterativeEntropy
    {
        internal ushort Identifier;
        internal int CaseBit;

        internal ScriptedIterativeEntropy(ushort identifier, int caseBit)
        {
            Identifier = identifier;
            CaseBit = caseBit;
        }

        public ushort NextIdentifier()
        {
            ushort result = Identifier;
            Identifier = (ushort)(Identifier + 1);
            return result;
        }

        public int NextCaseBit()
        {
            return CaseBit;
        }
    }

    class ScriptedIterativeClock : IDnsIterativeClock
    {
        internal long UnixTimeMilliseconds;
        internal long MonotonicMilliseconds;

        internal ScriptedIterativeClock(long monotonicMilliseconds)
        {
            UnixTimeMilliseconds = monotonicMilliseconds;
            MonotonicMilliseconds = monotonicMilliseconds;
        }

        public DnsClockValue ReadClock()
        {
            DnsClockValue result = new DnsClockValue();
            result.UnixTimeMilliseconds = UnixTimeMilliseconds;
            result.MonotonicMilliseconds = MonotonicMilliseconds;
            return result;
        }
    }

    class ScriptedIterativeTransport : IDnsTransport
    {
        internal Queue<Func<DnsTransportRequest, DnsTransportResult>> Steps =
            new Queue<Func<DnsTransportRequest, DnsTransportResult>>();
        internal List<DnsTransportRequest> Requests =
            new List<DnsTransportRequest>();

        public DnsTransportResult Exchange(DnsTransportRequest request)
        {
            Requests.Add(request);
            return Steps.Dequeue()(request);
        }

        public void CloseTransport()
        {
        }

        internal void Enqueue(
            Func<DnsTransportRequest, DnsTransportResult> step)
        {
            Steps.Enqueue(step);
        }
    }
}
