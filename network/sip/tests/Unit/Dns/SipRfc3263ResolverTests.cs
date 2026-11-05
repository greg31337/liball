namespace sip.Tests;

public class SipRfc3263ResolverTests
{
    [TestCase("sip:alice@192.0.2.1", SipTransportKind.Unknown, false,
        SipTransportKind.Udp, 5060, false)]
    [TestCase("sip:alice@192.0.2.1;transport=tcp",
        SipTransportKind.Unknown, false, SipTransportKind.Tcp, 5060, false)]
    [TestCase("sips:alice@192.0.2.1", SipTransportKind.Unknown, false,
        SipTransportKind.Tls, 5061, true)]
    [TestCase("sips:alice@192.0.2.1;transport=tcp",
        SipTransportKind.Unknown, false, SipTransportKind.Tls, 5061, true)]
    [TestCase("sip:alice@192.0.2.1", SipTransportKind.Tcp, true,
        SipTransportKind.Tls, 5061, true)]
    [TestCase("sip:alice@192.0.2.1;transport=wss",
        SipTransportKind.Unknown, false,
        SipTransportKind.SecureWebSocket, 5061, true)]
    [TestCase("sip:alice@192.0.2.1;transport=sctp",
        SipTransportKind.Unknown, false, SipTransportKind.Sctp, 5060, false)]
    [TestCase("sips:alice@[2001:db8::1]:5071;transport=tcp",
        SipTransportKind.Unknown, false, SipTransportKind.Tls, 5071, true)]
    public void AppliesNumericUriTransportSecurityDecisionTable(string uri,
        SipTransportKind requestedTransport, bool requireSecure,
        SipTransportKind expectedTransport, int expectedPort,
        bool expectedSecure)
    {
        SipResolutionRequest request = Request(ParseUri(uri));
        request.Transport = requestedTransport;
        request.Secure = requireSecure;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, new SipDnsOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].Transport,
                Is.EqualTo(expectedTransport));
            Assert.That(result.Candidates[0].Port, Is.EqualTo(expectedPort));
            Assert.That(result.Candidates[0].Secure, Is.EqualTo(expectedSecure));
            Assert.That(result.Candidates[0].RecordKind, Is.EqualTo(
                uri.Contains('[') ? SipDnsRecordKind.Aaaa : SipDnsRecordKind.A));
            Assert.That(result.Candidates[0].ExpiresAtMilliseconds,
                Is.EqualTo(-1));
        });
    }

    [Test]
    public void ResolvesNumericSipAndSipsTargetsWithoutDns()
    {
        SipResolutionRequest sip = Request(ParseUri("sip:alice@192.0.2.10"));
        SipResolutionRequest sips = Request(ParseUri("sips:alice@[2001:db8::1];transport=tcp"));
        SipResolutionRequest explicitTarget = Request(
            ParseUri("sip:alice@192.0.2.20:5090;transport=tcp"));
        SipResolutionRequest multicastTarget = Request(
            ParseUri("sip:alice@ignored.example;maddr=192.0.2.30;transport=tcp"));

        SipDnsResolver resolver = new SipDnsResolver();
        SipResolutionResult sipResult = resolver.ResolveTarget(sip, new SipDnsOptions());
        SipResolutionResult sipsResult = resolver.ResolveTarget(sips, new SipDnsOptions());
        SipResolutionResult explicitResult = resolver.ResolveTarget(
            explicitTarget, new SipDnsOptions());
        SipResolutionResult multicastResult = resolver.ResolveTarget(
            multicastTarget, new SipDnsOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sipResult.Candidates.Single().Host, Is.EqualTo("192.0.2.10"));
            Assert.That(sipResult.Candidates.Single().Port, Is.EqualTo(5060));
            Assert.That(sipResult.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(sipsResult.Candidates.Single().Host, Is.EqualTo("2001:db8::1"));
            Assert.That(sipsResult.Candidates.Single().Port, Is.EqualTo(5061));
            Assert.That(sipsResult.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(sipsResult.Candidates.Single().Secure, Is.True);
            Assert.That(explicitResult.Candidates.Single().Port, Is.EqualTo(5090));
            Assert.That(explicitResult.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(multicastResult.Candidates.Single().Host, Is.EqualTo("192.0.2.30"));
        });
    }

    [Test]
    public void ExplicitPortSkipsNaptrAndSrvButResolvesAddresses()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 2, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            return type == 1 ? AddressResponse(query, new byte[] { 192, 0, 2, 21 })
                : EmptyResponse(query);
        });
        SipResolutionRequest request = Request(
            ParseUri("sip:alice@port.example:5090;transport=tcp"));

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(questions, Is.EqualTo(new string[] { "port.example:1", "port.example:28" }));
            Assert.That(result.Candidates.Single().Host, Is.EqualTo("192.0.2.21"));
            Assert.That(result.Candidates.Single().Port, Is.EqualTo(5090));
            Assert.That(result.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Tcp));
        });
    }

    [TestCase("sip:alice@port-default.example:5091",
        SipTransportKind.Udp, false)]
    [TestCase("sips:alice@port-default.example:5091",
        SipTransportKind.Tls, true)]
    public void ExplicitPortUsesSchemeDefaultTransportWithoutServiceLookup(
        string uri, SipTransportKind expectedTransport, bool expectedSecure)
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 2, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            return type == 1
                ? AddressResponse(query, new byte[] { 192, 0, 2, 22 })
                : EmptyResponse(query);
        });

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            Request(ParseUri(uri)), Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "port-default.example:1", "port-default.example:28"
            }));
            Assert.That(result.Status,
                Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Candidates.Single().Port, Is.EqualTo(5091));
            Assert.That(result.Candidates.Single().Transport,
                Is.EqualTo(expectedTransport));
            Assert.That(result.Candidates.Single().Secure,
                Is.EqualTo(expectedSecure));
        });
    }

    [Test]
    public void ExplicitTransportUsesSrvBeforeAddressLookup()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 3, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            if (type == 33) return SrvResponse(query, 2, 30, 5070, "edge.example", 100);
            if (type == 1) return AddressResponse(query, new byte[] { 198, 51, 100, 20 });
            return EmptyResponse(query);
        });
        SipResolutionRequest request = Request(
            ParseUri("sip:alice@transport.example;transport=udp"));
        long before = System.Environment.TickCount64;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        SipServerCandidate candidate = result.Candidates.Single();
        Assert.Multiple(() =>
        {
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "_sip._udp.transport.example:33", "edge.example:1", "edge.example:28"
            }));
            Assert.That(candidate.Host, Is.EqualTo("198.51.100.20"));
            Assert.That(candidate.Port, Is.EqualTo(5070));
            Assert.That(candidate.Priority, Is.EqualTo(2));
            Assert.That(candidate.Weight, Is.EqualTo(30));
            Assert.That(candidate.ExpiresAtMilliseconds,
                Is.InRange(before + 49_000, before + 51_000));
        });
    }

    [Test]
    public void ExplicitTransportFallsBackFromMissingSrvToDefaultPortAddresses()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 3, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            return type == 1
                ? AddressResponse(query, new byte[] { 198, 51, 100, 30 })
                : EmptyResponse(query);
        });
        SipResolutionRequest request = Request(
            ParseUri("sip:alice@transport-fallback.example;transport=tcp"));

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "_sip._tcp.transport-fallback.example:33",
                "transport-fallback.example:1",
                "transport-fallback.example:28"
            }));
            Assert.That(result.Status,
                Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Candidates.Single().Transport,
                Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(result.Candidates.Single().Port,
                Is.EqualTo(SipTransportNames.SipPort));
        });
    }

    [Test]
    public void NaptrSelectsTransportAndSrvReplacement()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 4, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            if (type == 35) return NaptrResponse(query, "SIP+D2T",
                "_sip._tcp.naptr.example", 10, 20);
            if (type == 33) return SrvResponse(query, 1, 5, 5080, "tcp.example", 90);
            if (type == 1) return AddressResponse(query, new byte[] { 203, 0, 113, 44 });
            return EmptyResponse(query);
        });
        SipResolutionRequest request = Request(ParseUri("sip:alice@naptr.example"));
        request.CorrelationId = "dns-correlation";

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "naptr.example:35", "_sip._tcp.naptr.example:33",
                "tcp.example:1", "tcp.example:28"
            }));
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.CorrelationId, Is.EqualTo("dns-correlation"));
            Assert.That(result.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(result.Candidates.Single().Port, Is.EqualTo(5080));
        });
    }

    [Test]
    public void OrdersEqualOrderNaptrServicesByPreference()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 7, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            if (type == 35)
            {
                return OrderedNaptrResponse(query);
            }
            if (type == 33 && name.StartsWith("_sip._udp."))
            {
                return SrvResponse(query, 10, 5, 5070,
                    "udp-target.example", 100);
            }
            if (type == 33)
            {
                return SrvResponse(query, 20, 7, 5080,
                    "tcp-target.example", 90);
            }
            if (type == 1 && name == "udp-target.example")
            {
                return AddressResponse(query, new byte[] { 192, 0, 2, 70 });
            }
            if (type == 1 && name == "tcp-target.example")
            {
                return AddressResponse(query, new byte[] { 192, 0, 2, 80 });
            }
            return EmptyResponse(query);
        });
        SipDnsResolver resolver = new SipDnsResolver();
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        resolver.ConfigureResolutionSources(
            new Rfc3263RandomSource(), clock);

        SipResolutionResult result = resolver.ResolveTarget(
            Request(ParseUri("sip:alice@ordered.example")),
            Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions[1], Is.EqualTo(
                "_sip._udp.ordered.example:33"));
            Assert.That(questions[4], Is.EqualTo(
                "_sip._tcp.ordered.example:33"));
            Assert.That(result.Candidates.Select(candidate => candidate.Transport),
                Is.EqualTo(new SipTransportKind[]
                {
                    SipTransportKind.Udp, SipTransportKind.Tcp
                }));
            Assert.That(result.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[]
                {
                    "192.0.2.70", "192.0.2.80"
                }));
            Assert.That(result.Candidates.Select(candidate => candidate.Priority),
                Is.EqualTo(new int[] { 10, 20 }));
            Assert.That(result.Candidates.Select(candidate => candidate.Weight),
                Is.EqualTo(new int[] { 5, 7 }));
            Assert.That(result.Candidates[0].ExpiresAtMilliseconds,
                Is.EqualTo(51000));
        });
    }

    [Test]
    public void ExcludesUnrelatedAddressAnswersFromCandidates()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, 2, (query, name, type) =>
            type == 1 ? AddressResponseWithUnrelatedAnswer(query)
                : EmptyResponse(query));
        SipResolutionRequest request = Request(
            ParseUri("sip:alice@target.example:5090;transport=tcp"));

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "192.0.2.42" }));
            Assert.That(result.Candidates.Single().Port, Is.EqualTo(5090));
        });
    }

    [Test]
    public void FallsBackToDefaultAddressWhenNaptrAndSrvAreAbsent()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 6, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            if (type == 1) return AddressResponse(query, new byte[] { 192, 0, 2, 99 });
            return EmptyResponse(query);
        });
        SipResolutionRequest request = Request(ParseUri("sip:alice@fallback.example"));

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "fallback.example:35", "_sip._udp.fallback.example:33",
                "_sip._tcp.fallback.example:33", "_sip._sctp.fallback.example:33",
                "fallback.example:1", "fallback.example:28"
            }));
            Assert.That(result.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(result.Candidates.Single().Port, Is.EqualTo(5060));
        });
    }

    [Test]
    public void SipsResolutionSelectsOnlySecureNaptrServices()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 4, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            if (type == 35) return NaptrResponse(query, "SIPS+D2T",
                "_sips._tcp.secure.example", 5, 5);
            if (type == 33) return SrvResponse(query, 0, 0, 5061, "tls.example", 60);
            if (type == 1) return AddressResponse(query, new byte[] { 192, 0, 2, 61 });
            return EmptyResponse(query);
        });

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            Request(ParseUri("sips:alice@secure.example")), Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions[1], Is.EqualTo("_sips._tcp.secure.example:33"));
            Assert.That(result.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(result.Candidates.Single().Secure, Is.True);
        });
    }

    [Test]
    public void SipResolutionCanPreferLowestOrderSecureNaptrService()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, 4, (query, name, type) =>
        {
            questions.Add(name + ":" + type);
            if (type == 35) return PreferredNaptrResponse(query);
            if (type == 33) return SrvResponse(query, 0, 0, 5061, "preferred.example", 60);
            if (type == 1) return AddressResponse(query, new byte[] { 192, 0, 2, 62 });
            return EmptyResponse(query);
        });

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            Request(ParseUri("sip:alice@choice.example")), Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions[1], Is.EqualTo("_sips._tcp.choice.example:33"));
            Assert.That(questions, Does.Not.Contain("_sip._tcp.choice.example:33"));
            Assert.That(result.Candidates.Single().Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(result.Candidates.Single().Secure, Is.True);
        });
    }

    [Test]
    public void RejectsInsecureOrConflictingTransportSelections()
    {
        SipResolutionRequest insecure = Request(
            ParseUri("sip:alice@example.com;transport=udp"));
        insecure.Secure = true;
        SipResolutionRequest conflicting = Request(
            ParseUri("sip:alice@example.com;transport=udp"));
        conflicting.Transport = SipTransportKind.Tcp;

        SipDnsResolver resolver = new SipDnsResolver();
        SipResolutionResult insecureResult = resolver.ResolveTarget(insecure, new SipDnsOptions());
        SipResolutionResult conflictResult = resolver.ResolveTarget(conflicting, new SipDnsOptions());

        Assert.Multiple(() =>
        {
            Assert.That(insecureResult.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(insecureResult.ErrorMessage, Does.Contain("secure target"));
            Assert.That(conflictResult.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(conflictResult.ErrorMessage, Does.Contain("different transports"));
        });
    }

    [TestCase("sip:alice@example.com;transport=udp", SipTransportKind.Unknown,
        true, "secure target")]
    [TestCase("sip:alice@example.com;transport=udp", SipTransportKind.Tcp,
        false, "different transports")]
    [TestCase("sip:alice@example.com;transport=quic", SipTransportKind.Unknown,
        false, "not supported")]
    [TestCase("tel:+12025550123", SipTransportKind.Unknown,
        false, "Only SIP and SIPS")]
    public void ReportsInvalidUriTransportAndSecurityDecisions(string uri,
        SipTransportKind transport, bool secure, string errorFragment)
    {
        SipResolutionRequest request = Request(ParseUri(uri));
        request.Transport = transport;
        request.Secure = secure;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(
            request, new SipDnsOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.ErrorMessage, Does.Contain(errorFragment));
        });
    }

    [Test]
    public void ReportsInvalidDomainPortAndResolverOptions()
    {
        SipResolutionRequest invalidDomain = new SipResolutionRequest();
        invalidDomain.Domain = "bad..example";
        SipResolutionRequest invalidPort = Request(
            ParseUri("sip:alice@example.com"));
        invalidPort.Uri!.Port = 0;
        SipDnsOptions invalidOptions = new SipDnsOptions();
        invalidOptions.TimeoutMilliseconds = 0;

        SipDnsResolver resolver = new SipDnsResolver();
        SipResolutionResult domain = resolver.ResolveTarget(
            invalidDomain, new SipDnsOptions());
        SipResolutionResult port = resolver.ResolveTarget(
            invalidPort, new SipDnsOptions());
        SipResolutionResult options = resolver.ResolveTarget(
            Request(ParseUri("sip:alice@192.0.2.1")), invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(domain.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(domain.ErrorMessage, Does.Contain("domain is invalid"));
            Assert.That(port.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(port.ErrorMessage, Does.Contain("port is invalid"));
            Assert.That(options.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(options.ErrorMessage, Does.Contain("options are invalid"));
        });
    }

    static Task Serve(System.Net.Sockets.UdpClient server, int count,
        System.Func<byte[], string, int, byte[]> responseFactory)
    {
        return Task.Run(() =>
        {
            int index = 0;
            while (index < count)
            {
                System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                    System.Net.IPAddress.Loopback, 0);
                byte[] query = server.Receive(ref remote);
                string name = QuestionName(query);
                int type = QuestionType(query);
                byte[] response = responseFactory(query, name, type);
                server.Send(response, response.Length, remote);
                index++;
            }
        });
    }

    static System.Net.Sockets.UdpClient CreateServer()
    {
        return new System.Net.Sockets.UdpClient(
            new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
    }

    static int LocalPort(System.Net.Sockets.UdpClient server)
    {
        return ((System.Net.IPEndPoint)server.Client.LocalEndPoint!).Port;
    }

    static SipDnsOptions Options(int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = "127.0.0.1";
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        SipDnsOptions options = new SipDnsOptions();
        options.NameServers = new SipEndpointAddress[] { endpoint };
        options.TimeoutMilliseconds = 1000;
        options.MaxAttempts = 1;
        options.MaxRecordCount = 16;
        return options;
    }

    static SipResolutionRequest Request(SipUri uri)
    {
        SipResolutionRequest request = new SipResolutionRequest();
        request.Uri = uri;
        return request;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Uri!;
    }

    static byte[] EmptyResponse(byte[] query)
    {
        return StartResponse(query, 0).ToArray();
    }

    static byte[] AddressResponse(byte[] query, byte[] address)
    {
        List<byte> response = StartResponse(query, 1);
        AddRecordHeader(response, 1, 50, address.Length);
        response.AddRange(address);
        return response.ToArray();
    }

    static byte[] AddressResponseWithUnrelatedAnswer(byte[] query)
    {
        List<byte> response = StartResponse(query, 2);
        AddName(response, "poison.example");
        AddUInt16(response, 1);
        AddUInt16(response, 1);
        AddUInt32(response, 50);
        AddUInt16(response, 4);
        response.AddRange(new byte[] { 192, 0, 2, 66 });
        AddRecordHeader(response, 1, 50, 4);
        response.AddRange(new byte[] { 192, 0, 2, 42 });
        return response.ToArray();
    }

    static byte[] SrvResponse(byte[] query, int priority, int weight, int port,
        string target, uint ttl)
    {
        List<byte> data = new List<byte>();
        AddUInt16(data, priority);
        AddUInt16(data, weight);
        AddUInt16(data, port);
        AddName(data, target);
        List<byte> response = StartResponse(query, 1);
        AddRecordHeader(response, 33, ttl, data.Count);
        response.AddRange(data);
        return response.ToArray();
    }

    static byte[] NaptrResponse(byte[] query, string service, string replacement,
        int order, int preference)
    {
        List<byte> data = new List<byte>();
        AddUInt16(data, order);
        AddUInt16(data, preference);
        AddText(data, "s");
        AddText(data, service);
        AddText(data, "");
        AddName(data, replacement);
        List<byte> response = StartResponse(query, 1);
        AddRecordHeader(response, 35, 120, data.Count);
        response.AddRange(data);
        return response.ToArray();
    }

    static byte[] PreferredNaptrResponse(byte[] query)
    {
        List<byte> response = StartResponse(query, 2);
        AddNaptrRecord(response, "SIPS+D2T", "_sips._tcp.choice.example", 10, 10);
        AddNaptrRecord(response, "SIP+D2T", "_sip._tcp.choice.example", 20, 10);
        return response.ToArray();
    }

    static byte[] OrderedNaptrResponse(byte[] query)
    {
        List<byte> response = StartResponse(query, 2);
        AddNaptrRecord(response, "SIP+D2T",
            "_sip._tcp.ordered.example", 10, 20);
        AddNaptrRecord(response, "SIP+D2U",
            "_sip._udp.ordered.example", 10, 10);
        return response.ToArray();
    }

    static void AddNaptrRecord(List<byte> response, string service, string replacement,
        int order, int preference)
    {
        List<byte> data = new List<byte>();
        AddUInt16(data, order);
        AddUInt16(data, preference);
        AddText(data, "s");
        AddText(data, service);
        AddText(data, "");
        AddName(data, replacement);
        AddRecordHeader(response, 35, 120, data.Count);
        response.AddRange(data);
    }

    static List<byte> StartResponse(byte[] query, int answers)
    {
        List<byte> response = new List<byte>();
        response.Add(query[0]);
        response.Add(query[1]);
        AddUInt16(response, 0x8180);
        AddUInt16(response, 1);
        AddUInt16(response, answers);
        AddUInt16(response, 0);
        AddUInt16(response, 0);
        response.AddRange(query.Skip(12));
        return response;
    }

    static void AddRecordHeader(List<byte> response, int type, uint ttl, int length)
    {
        AddPointer(response, 12);
        AddUInt16(response, type);
        AddUInt16(response, 1);
        AddUInt32(response, ttl);
        AddUInt16(response, length);
    }

    static string QuestionName(byte[] query)
    {
        List<string> labels = new List<string>();
        int offset = 12;
        while (query[offset] != 0)
        {
            int length = query[offset++];
            labels.Add(System.Text.Encoding.ASCII.GetString(query, offset, length));
            offset += length;
        }
        return string.Join('.', labels);
    }

    static int QuestionType(byte[] query)
    {
        int offset = 12;
        while (query[offset] != 0)
        {
            offset += query[offset] + 1;
        }
        offset++;
        return (query[offset] << 8) | query[offset + 1];
    }

    static void AddName(List<byte> bytes, string name)
    {
        foreach (string label in name.Split('.'))
        {
            byte[] encoded = System.Text.Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)encoded.Length);
            bytes.AddRange(encoded);
        }
        bytes.Add(0);
    }

    static void AddText(List<byte> bytes, string text)
    {
        byte[] encoded = System.Text.Encoding.ASCII.GetBytes(text);
        bytes.Add((byte)encoded.Length);
        bytes.AddRange(encoded);
    }

    static void AddPointer(List<byte> bytes, int offset)
    {
        bytes.Add((byte)(0xc0 | offset >> 8));
        bytes.Add((byte)offset);
    }

    static void AddUInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    class Rfc3263RandomSource : ISipRandomSource
    {
        public void FillBytes(byte[] bytes)
        {
            System.Array.Clear(bytes);
        }
    }
}
