namespace sip.Tests;

public class SipEnumResolverTests
{
    [Test]
    public void BoundedRegexpExpandsSupportedCaptureAndRejectsUnsafeForms()
    {
        SipEnumRegexpResult expanded = SipEnumRegexp.Apply(
            "!^\\+1([0-9]+)$!sip:\\1@example.com!", "+12025550123", 128);
        SipEnumRegexpResult unanchored = SipEnumRegexp.Apply(
            "!.*!sip:user@example.com!", "+12025550123", 128);
        SipEnumRegexpResult unsupported = SipEnumRegexp.Apply(
            "!^((.*)+)+$!sip:\\1@example.com!", "+12025550123", 128);
        SipEnumRegexpResult limited = SipEnumRegexp.Apply(
            "!^.*$!sip:user@example.com!", "+12025550123", 8);

        Assert.Multiple(() =>
        {
            Assert.That(expanded.Success, Is.True, expanded.ErrorMessage);
            Assert.That(expanded.Output, Is.EqualTo("sip:2025550123@example.com"));
            Assert.That(unanchored.Success, Is.False);
            Assert.That(unsupported.Success, Is.False);
            Assert.That(limited.Success, Is.False);
        });
    }

    [Test]
    public void BoundedRegexpSupportsLiteralCaptureAndEscapedDelimiterReplacements()
    {
        SipEnumRegexpResult literal = SipEnumRegexp.Apply(
            "!^\\+12025550123$!sip:literal@example.com!", "+12025550123", 128);
        SipEnumRegexpResult backreference = SipEnumRegexp.Apply(
            "!^\\+1([0-9]+)$!sip:$1@example.com!", "+12025550123", 128);
        SipEnumRegexpResult escapedDelimiter = SipEnumRegexp.Apply(
            "/^\\+1([0-9]+)$/sip:\\1@example.com;note=left\\/right/",
            "+12025550123", 128);

        Assert.Multiple(() =>
        {
            Assert.That(literal.Success, Is.True, literal.ErrorMessage);
            Assert.That(literal.Output, Is.EqualTo("sip:literal@example.com"));
            Assert.That(backreference.Success, Is.True, backreference.ErrorMessage);
            Assert.That(backreference.Output, Is.EqualTo("sip:2025550123@example.com"));
            Assert.That(escapedDelimiter.Success, Is.True, escapedDelimiter.ErrorMessage);
            Assert.That(escapedDelimiter.Output,
                Is.EqualTo("sip:2025550123@example.com;note=left/right"));
        });
    }

    [TestCase("a^.*$asip:user@example.coma")]
    [TestCase("!.*!sip:user@example.com!")]
    [TestCase("!^.*$!sip:user@example.com!g")]
    [TestCase("!^((.*)+)+$!sip:\\1@example.com!")]
    [TestCase("!^.*$!sip:\\1@example.com!")]
    [TestCase("!^\\+1([a-z]+)$!sip:\\1@example.com!")]
    public void BoundedRegexpRejectsMalformedAndPotentiallyLoopingExpressions(
        string expression)
    {
        SipEnumRegexpResult result = SipEnumRegexp.Apply(
            expression, "+12025550123", 128);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void BoundedRegexpEnforcesExpressionPatternAndExactOutputLimits()
    {
        SipEnumRegexpResult exact = SipEnumRegexp.Apply(
            "!^.*$!sip:user@example.com!", "+12025550123", 20);
        SipEnumRegexpResult outputOverflow = SipEnumRegexp.Apply(
            "!^.*$!sip:user@example.com!", "+12025550123", 19);
        string longPattern = "^" + new string('1', 255) + "$";
        SipEnumRegexpResult patternOverflow = SipEnumRegexp.Apply(
            "!" + longPattern + "!sip:user@example.com!", "+12025550123", 128);
        SipEnumRegexpResult expressionOverflow = SipEnumRegexp.Apply(
            "!^.*$!" + new string('x', 1018) + "!", "+12025550123", 2048);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True, exact.ErrorMessage);
            Assert.That(exact.Output, Is.EqualTo("sip:user@example.com"));
            Assert.That(outputOverflow.Success, Is.False);
            Assert.That(outputOverflow.ErrorMessage, Does.Contain("URI limit"));
            Assert.That(patternOverflow.Success, Is.False);
            Assert.That(expressionOverflow.Success, Is.False);
        });
    }

    [Test]
    public void ResolvesGlobalNumberInNaptrPreferenceOrderAndAppliesEnumTtl()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Serve(server, query =>
        {
            questions.Add(QuestionName(query) + ":" + QuestionType(query));
            return OrderedEnumResponse(query);
        });
        SipDnsOptions options = Options(LocalPort(server));
        options.EnableEnum = true;
        SipResolutionRequest request = Request(ParseUri("tel:+1-202-555-0123"));
        request.UseEnum = true;
        request.CorrelationId = "enum-operation";
        SipDnsResolver resolver = new SipDnsResolver();
        resolver.ConfigureResolutionSources(new SipCryptographicRandomSource(),
            new SipManualMonotonicClock(5000));

        SipResolutionResult result = resolver.ResolveTarget(request, options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "3.2.1.0.5.5.5.2.0.2.1.e164.arpa:35"
            }));
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success),
                result.ErrorMessage);
            Assert.That(result.CorrelationId, Is.EqualTo("enum-operation"));
            Assert.That(result.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "192.0.2.1", "192.0.2.2" }));
            Assert.That(result.Candidates.Select(candidate => candidate.Transport),
                Is.EqualTo(new SipTransportKind[]
                {
                    SipTransportKind.Tcp, SipTransportKind.Udp
                }));
            Assert.That(result.Candidates.Select(candidate => candidate.Port),
                Is.EqualTo(new int[] { 5070, 5080 }));
            Assert.That(result.Candidates.Select(candidate => candidate.ExpiresAtMilliseconds),
                Is.EqualTo(new long[] { 45000, 65000 }));
        });
    }

    [Test]
    public void SecureEnumPolicyRejectsSipCredentialsAndHeadersThenUsesSips()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, SecurePolicyResponse);
        SipDnsOptions options = Options(LocalPort(server));
        options.EnableEnum = true;
        SipResolutionRequest request = new SipResolutionRequest();
        request.Domain = "+12025550123";
        request.Secure = true;
        request.UseEnum = true;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(request, options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success),
                result.ErrorMessage);
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].Host, Is.EqualTo("192.0.2.5"));
            Assert.That(result.Candidates[0].Port, Is.EqualTo(5061));
            Assert.That(result.Candidates[0].Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(result.Candidates[0].Secure, Is.True);
        });
    }

    [Test]
    public void FiltersUnsupportedNaptrRulesBeforeSelectingTheFirstUsableOrder()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, FilteredOrderingResponse);
        SipDnsOptions options = Options(LocalPort(server));
        options.EnableEnum = true;
        SipResolutionRequest request = Request(ParseUri("tel:+12025550123"));
        request.UseEnum = true;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(request, options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success),
                result.ErrorMessage);
            Assert.That(result.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "192.0.2.6" }));
        });
    }

    [Test]
    public void ReportsNoRecordsWhenEveryEnumRuleIsRejectedByPolicy()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, RejectedPolicyResponse);
        SipDnsOptions options = Options(LocalPort(server));
        options.EnableEnum = true;
        SipResolutionRequest request = Request(ParseUri("tel:+12025550123"));
        request.UseEnum = true;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(request, options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.NoRecords));
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("no permitted SIP URI mappings"));
        });
    }

    [Test]
    public void PropagatesEnumCnameLoopFailure()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        List<string> questions = new List<string>();
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] first = server.Receive(ref remote);
            questions.Add(QuestionName(first));
            byte[] toAlias = CnameResponse(first, "enum-alias.example.com");
            server.Send(toAlias, toAlias.Length, remote);
            byte[] second = server.Receive(ref remote);
            questions.Add(QuestionName(second));
            byte[] toOrigin = CnameResponse(second,
                "3.2.1.0.5.5.5.2.0.2.1.e164.arpa");
            server.Send(toOrigin, toOrigin.Length, remote);
        });
        SipDnsOptions options = Options(LocalPort(server));
        options.EnableEnum = true;
        SipResolutionRequest request = Request(ParseUri("tel:+12025550123"));
        request.UseEnum = true;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(request, options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(result.ErrorMessage, Does.Contain("loop"));
            Assert.That(questions, Is.EqualTo(new string[]
            {
                "3.2.1.0.5.5.5.2.0.2.1.e164.arpa",
                "enum-alias.example.com"
            }));
        });
    }

    [Test]
    public void EnforcesConfiguredDnsRecordLimitDuringEnumLookup()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, OrderedEnumResponse);
        SipDnsOptions options = Options(LocalPort(server));
        options.EnableEnum = true;
        options.MaxRecordCount = 2;
        SipResolutionRequest request = Request(ParseUri("tel:+12025550123"));
        request.UseEnum = true;

        SipResolutionResult result = new SipDnsResolver().ResolveTarget(request, options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("record limit"));
        });
    }

    [Test]
    public void EnumRequiresBothRequestAndResolverPolicyForAGlobalNumber()
    {
        SipResolutionRequest disabled = Request(ParseUri("tel:+12025550123"));
        disabled.UseEnum = true;
        SipResolutionRequest local = Request(
            ParseUri("tel:5550123;phone-context=+1"));
        local.UseEnum = true;
        SipDnsOptions enabledOptions = new SipDnsOptions();
        enabledOptions.EnableEnum = true;

        SipResolutionResult disabledResult = new SipDnsResolver().ResolveTarget(
            disabled, new SipDnsOptions());
        SipResolutionResult localResult = new SipDnsResolver().ResolveTarget(
            local, enabledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(disabledResult.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(disabledResult.ErrorMessage, Does.Contain("disabled"));
            Assert.That(localResult.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(localResult.ErrorMessage, Does.Contain("global E.164"));
        });
    }

    static Task Serve(System.Net.Sockets.UdpClient server,
        System.Func<byte[], byte[]> responseFactory)
    {
        return Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = server.Receive(ref remote);
            byte[] response = responseFactory(query);
            server.Send(response, response.Length, remote);
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
        options.EnableCache = false;
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

    static byte[] OrderedEnumResponse(byte[] query)
    {
        List<byte> response = StartResponse(query, 3);
        AddEnumRecord(response, "E2U+SIP",
            "!^\\+1([0-9]+)$!sip:second@192.0.2.2:5080!", 10, 20, 60);
        AddEnumRecord(response, "E2U+SIP",
            "!^.*$!sip:ignored@192.0.2.3:5090!", 20, 1, 90);
        AddEnumRecord(response, "E2U+SIP",
            "!^\\+1([0-9]+)$!sip:first-\\1@192.0.2.1:5070;transport=tcp!",
            10, 10, 40);
        return response.ToArray();
    }

    static byte[] SecurePolicyResponse(byte[] query)
    {
        List<byte> response = StartResponse(query, 4);
        AddEnumRecord(response, "E2U+SIP",
            "!^.*$!sip:insecure@192.0.2.2!", 5, 1, 60);
        AddEnumRecord(response, "E2U+SIPS",
            "!^.*$!sips:alice:secret@192.0.2.3!", 5, 2, 60);
        AddEnumRecord(response, "E2U+SIPS",
            "!^.*$!sips:alice@192.0.2.4?Subject=x!", 5, 3, 60);
        AddEnumRecord(response, "E2U+SIPS",
            "!^.*$!sips:alice@192.0.2.5!", 10, 1, 60);
        return response.ToArray();
    }

    static byte[] FilteredOrderingResponse(byte[] query)
    {
        List<byte> response = StartResponse(query, 7);
        AddNaptrRecord(response, "u", "E2U+EMAIL",
            "!^.*$!mailto:user@example.com!", ".", 1, 1, 60);
        AddNaptrRecord(response, "u", "E2U+SIP",
            "!^((.*)+)+$!sip:loop@192.0.2.1!", ".", 2, 1, 60);
        AddNaptrRecord(response, "s", "E2U+SIP",
            "!^.*$!sip:wrong-flag@192.0.2.2!", ".", 3, 1, 60);
        AddNaptrRecord(response, "u", "E2U+SIP",
            "!^.*$!sip:wrong-replacement@192.0.2.3!", "next.example.com",
            3, 2, 60);
        AddNaptrRecord(response, "u", "E2U+SIP",
            "!^.*$!sips:wrong-scheme@192.0.2.4!", ".", 3, 3, 60);
        AddNaptrRecord(response, "U", "e2u+sip",
            "!^.*$!sip:accepted@192.0.2.6!", ".", 3, 4, 60);
        AddNaptrRecord(response, "u", "E2U+SIP",
            "!^.*$!sip:later-order@192.0.2.7!", ".", 4, 1, 60);
        return response.ToArray();
    }

    static byte[] RejectedPolicyResponse(byte[] query)
    {
        List<byte> response = StartResponse(query, 5);
        AddEnumRecord(response, "E2U+EMAIL",
            "!^.*$!sip:unsupported@192.0.2.1!", 1, 1, 60);
        AddNaptrRecord(response, "s", "E2U+SIP",
            "!^.*$!sip:wrong-flag@192.0.2.2!", ".", 1, 2, 60);
        AddNaptrRecord(response, "u", "E2U+SIP",
            "!^.*$!sip:replacement@192.0.2.3!", "next.example.com", 1, 3, 60);
        AddEnumRecord(response, "E2U+SIP",
            "!^.*$!sips:wrong-scheme@192.0.2.4!", 1, 4, 60);
        AddEnumRecord(response, "E2U+SIP",
            "!^.*$!sip:user:password@192.0.2.5!", 1, 5, 60);
        return response.ToArray();
    }

    static byte[] CnameResponse(byte[] query, string target)
    {
        List<byte> response = StartResponse(query, 1);
        List<byte> data = new List<byte>();
        AddName(data, target);
        AddRecordHeader(response, 5, 60, data.Count);
        response.AddRange(data);
        return response.ToArray();
    }

    static void AddEnumRecord(List<byte> response, string service, string regexp,
        int order, int preference, uint ttl)
    {
        AddNaptrRecord(response, "u", service, regexp, ".",
            order, preference, ttl);
    }

    static void AddNaptrRecord(List<byte> response, string flags, string service,
        string regexp, string replacement, int order, int preference, uint ttl)
    {
        List<byte> data = new List<byte>();
        AddUInt16(data, order);
        AddUInt16(data, preference);
        AddText(data, flags);
        AddText(data, service);
        AddText(data, regexp);
        AddName(data, replacement);
        AddRecordHeader(response, 35, ttl, data.Count);
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
        return query[offset] << 8 | query[offset + 1];
    }

    static void AddText(List<byte> bytes, string text)
    {
        byte[] encoded = System.Text.Encoding.ASCII.GetBytes(text);
        bytes.Add((byte)encoded.Length);
        bytes.AddRange(encoded);
    }

    static void AddName(List<byte> bytes, string name)
    {
        if (name == ".")
        {
            bytes.Add(0);
            return;
        }
        foreach (string label in name.Split('.'))
        {
            byte[] encoded = System.Text.Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)encoded.Length);
            bytes.AddRange(encoded);
        }
        bytes.Add(0);
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
}
