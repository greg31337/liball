namespace dns.Tests.Api;

public class DnsQueryClientPolicyTests
{
    [Test]
    public void StartsAtConfiguredRotationAndAdvancesAcrossServers()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request => Failure(
            request, DnsTransportStatus.ProtocolError));
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        DnsQueryClient client = Client(transport, 1);
        DnsQueryRequest request = Request(3, 3);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(transport.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1002, 1003 }));
            Assert.That(result.ServerStates, Has.Count.EqualTo(3));
            Assert.That(result.ServerStates[0].AttemptCount, Is.Zero);
            Assert.That(result.ServerStates[1].AttemptCount, Is.EqualTo(1));
            Assert.That(result.ServerStates[1].ConsecutiveFailures,
                Is.EqualTo(1));
            Assert.That(result.ServerStates[2].AttemptCount, Is.EqualTo(1));
            Assert.That(result.ServerStates[2].HasSuccessfulExchange, Is.True);
            Assert.That(result.ServerStates[2].ConsecutiveFailures, Is.Zero);
            Assert.That(result.ServerStates[2].LastSuccess.UnixTimeMilliseconds,
                Is.GreaterThan(0));
            Assert.That(client.NextServerOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void RotatesStartingServerBetweenQueries()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(2, 1);

        DnsQueryResult first = client.Query(request);
        DnsQueryResult second = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(transport.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1001, 1002 }));
            Assert.That(client.NextServerOffset, Is.Zero);
        });
    }

    [Test]
    public void RetryBudgetCyclesServersWithoutExceedingMaximumAttempts()
    {
        ScriptedTransport transport = new ScriptedTransport();
        int index = 0;
        while (index < 5)
        {
            transport.Enqueue(request => Failure(
                request, DnsTransportStatus.TimedOut));
            index++;
        }

        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(2, 5);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.Attempts, Has.Count.EqualTo(5));
            Assert.That(transport.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1001, 1002, 1001, 1002, 1001 }));
            Assert.That(result.ServerStates[0].AttemptCount, Is.EqualTo(3));
            Assert.That(result.ServerStates[0].ConsecutiveFailures,
                Is.EqualTo(3));
            Assert.That(result.ServerStates[1].AttemptCount, Is.EqualTo(2));
            Assert.That(result.ServerStates[1].ConsecutiveFailures,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void DirectTcpRetriesNextServerWithoutUsingUdp()
    {
        ScriptedTransport udp = new ScriptedTransport();
        ScriptedTransport tcp = new ScriptedTransport();
        tcp.Enqueue(request => Failure(
            request, DnsTransportStatus.NetworkError));
        tcp.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        DnsQueryClient client = new DnsQueryClient();
        client.NextServerOffset = 0;
        client.UdpTransport = udp;
        client.TcpTransport = tcp;
        DnsQueryRequest request = Request(2, 2);
        request.TransportOptions.Kind = DnsTransportKind.Tcp;

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedFallback, Is.False);
            Assert.That(udp.Requests, Is.Empty);
            Assert.That(tcp.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1001, 1002 }));
            Assert.That(result.Attempts.Select(item => item.Transport),
                Is.EqualTo(new[]
                {
                    DnsTransportKind.Tcp,
                    DnsTransportKind.Tcp
                }));
            Assert.That(result.ServerStates[0].AttemptCount, Is.EqualTo(1));
            Assert.That(result.ServerStates[0].ConsecutiveFailures,
                Is.EqualTo(1));
            Assert.That(result.ServerStates[0].HasSuccessfulExchange,
                Is.False);
            Assert.That(result.ServerStates[1].AttemptCount, Is.EqualTo(1));
            Assert.That(result.ServerStates[1].ConsecutiveFailures, Is.Zero);
            Assert.That(result.ServerStates[1].HasSuccessfulExchange, Is.True);
            Assert.That(result.ServerStates[1].PreferredTransport,
                Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(result.ServerStates[1].LastAttempt.MonotonicMilliseconds,
                Is.EqualTo(result.ServerStates[1].LastSuccess.MonotonicMilliseconds));
        });
    }

    [Test]
    public void SharesOneTotalDeadlineAcrossServerRetries()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request => Failure(
            request, DnsTransportStatus.NetworkError));
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(2, 2);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(transport.Requests, Has.Count.EqualTo(2));
            Assert.That(transport.Requests.All(item => item.HasDeadline),
                Is.True);
            Assert.That(transport.Requests[1].Deadline.MonotonicMilliseconds,
                Is.EqualTo(transport.Requests[0].Deadline.MonotonicMilliseconds));
            Assert.That(transport.Requests[1].Deadline.UnixTimeMilliseconds,
                Is.EqualTo(transport.Requests[0].Deadline.UnixTimeMilliseconds));
        });
    }

    [Test]
    public void TotalLifetimeExpiryPreventsAnotherServerAttempt()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request =>
        {
            Thread.Sleep(30);
            return Failure(request, DnsTransportStatus.NetworkError);
        });
        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(2, 2);
        request.TransportOptions.TotalTimeoutMilliseconds = 10;

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.Attempts, Has.Count.EqualTo(1));
            Assert.That(transport.Requests, Has.Count.EqualTo(1));
            Assert.That(result.ServerStates[0].AttemptCount, Is.EqualTo(1));
            Assert.That(result.ServerStates[1].AttemptCount, Is.Zero);
        });
    }

    [Test]
    public void RetriesServerFailureNotImplementedAndRefusedResponses()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.ServerFailure, false, 0));
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NotImplemented, false, 0));
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.Refused, false, 0));
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(4, 4);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(4));
            Assert.That(transport.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1001, 1002, 1003, 1004 }));
            Assert.That(new DnsMessageCodec().ReadEffectiveResponseCode(
                result.Response!), Is.EqualTo((int)DnsResponseCode.NoError));
        });
    }

    [Test]
    public void FormErrorWithoutOptDowngradesEdnsOnSameServer()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request =>
        {
            Assert.That(HasOpt(request.QueryMessage!), Is.True);
            return Answer(request, DnsResponseCode.FormatError, false, 0);
        });
        transport.Enqueue(request =>
        {
            Assert.That(HasOpt(request.QueryMessage!), Is.False);
            return Answer(request, DnsResponseCode.NoError, false, 0);
        });
        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(1, 2);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EdnsDowngraded, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(2));
            Assert.That(transport.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1001, 1001 }));
            Assert.That(result.ServerStates[0].HasEdnsCapability, Is.True);
            Assert.That(result.ServerStates[0].SupportsEdns, Is.False);
            Assert.That(result.ServerStates[0].EdnsUdpPayloadSize,
                Is.EqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
        });
    }

    [Test]
    public void RequiredDnssecKeepsEdnsWhenTryingNextServer()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request =>
        {
            Assert.That(HasOpt(request.QueryMessage!), Is.True);
            return Answer(request, DnsResponseCode.FormatError, false, 0);
        });
        transport.Enqueue(request =>
        {
            Assert.That(HasOpt(request.QueryMessage!), Is.True);
            return Answer(request, DnsResponseCode.NoError, true, 1400);
        });
        DnsQueryClient client = Client(transport, 0);
        DnsQueryRequest request = Request(2, 2);
        request.RequestDnssec = true;

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EdnsDowngraded, Is.False);
            Assert.That(transport.Requests.Select(item => item.Endpoint.Port),
                Is.EqualTo(new[] { 1001, 1002 }));
            Assert.That(result.ServerStates[0].HasEdnsCapability, Is.True);
            Assert.That(result.ServerStates[0].SupportsEdns, Is.False);
            Assert.That(result.ServerStates[1].HasEdnsCapability, Is.True);
            Assert.That(result.ServerStates[1].SupportsEdns, Is.True);
            Assert.That(result.ServerStates[1].EdnsUdpPayloadSize,
                Is.EqualTo(1400));
        });
    }

    [Test]
    public void InitialRandomSelectionLeavesValidRotationState()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Enqueue(request => Answer(
            request, DnsResponseCode.NoError, false, 0));
        DnsQueryClient client = Client(transport, -1);
        DnsQueryRequest request = Request(3, 1);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(transport.Requests[0].Endpoint.Port,
                Is.InRange(1001, 1003));
            Assert.That(client.NextServerOffset, Is.InRange(0, 2));
        });
    }

    static DnsQueryClient Client(ScriptedTransport transport, int offset)
    {
        DnsQueryClient client = new DnsQueryClient();
        client.NextServerOffset = offset;
        client.UdpTransport = transport;
        client.TcpTransport = transport;
        return client;
    }

    static DnsQueryRequest Request(int serverCount, int maximumAttempts)
    {
        DnsQueryRequest request = new DnsQueryRequest();
        request.Question.Name = Parse("policy.example.");
        request.Question.RecordType = DnsRecordType.A;
        request.Question.RecordTypeValue = 1;
        request.Question.Class = DnsClass.Internet;
        request.Question.ClassValue = 1;
        request.TransportOptions.Kind = DnsTransportKind.Udp;
        request.TransportOptions.MaximumAttempts = maximumAttempts;
        request.TransportOptions.AttemptTimeoutMilliseconds = 1000;
        request.TransportOptions.TotalTimeoutMilliseconds = 5000;
        request.AllowTcpFallback = true;
        int index = 0;
        while (index < serverCount)
        {
            DnsEndpoint endpoint = new DnsEndpoint();
            endpoint.HasAddress = true;
            endpoint.AddressFamily = 4;
            endpoint.Address = [127, 0, 0, 1];
            endpoint.Port = 1001 + index;
            request.Servers.Add(endpoint);
            index++;
        }

        return request;
    }

    static DnsTransportResult Failure(
        DnsTransportRequest request,
        DnsTransportStatus status)
    {
        DnsTransportResult result = BaseResult(request);
        result.Status = status;
        result.TimedOut = status == DnsTransportStatus.TimedOut;
        return result;
    }

    static DnsTransportResult Answer(
        DnsTransportRequest request,
        DnsResponseCode code,
        bool includeOpt,
        int payloadSize)
    {
        DnsMessage query = request.QueryMessage!;
        DnsMessage response = new DnsMessage();
        response.Header.Identifier = query.Header.Identifier;
        response.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionAvailable;
        response.Header.FlagsValue = (int)response.Header.Flags;
        response.Header.ResponseCode = code;
        response.Header.ResponseCodeValue = (int)code;
        response.Questions = new DnsMessageCodec().CloneMessage(query).Questions;
        if (includeOpt)
        {
            response = EdnsOperations.PrepareResponse(
                query, response, payloadSize).Message;
        }

        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            response, new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        DnsTransportResult result = BaseResult(request);
        result.Status = DnsTransportStatus.Success;
        result.ResponseMessage = response;
        result.ResponseBytes = encoded.Bytes;
        return result;
    }

    static DnsTransportResult BaseResult(DnsTransportRequest request)
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

    static bool HasOpt(DnsMessage message)
    {
        return message.Additionals.Any(record =>
            record.RecordTypeValue == 41 || record.RecordType == DnsRecordType.Opt);
    }

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    class ScriptedTransport : IDnsTransport
    {
        public Queue<Func<DnsTransportRequest, DnsTransportResult>> Steps =
            new Queue<Func<DnsTransportRequest, DnsTransportResult>>();
        public List<DnsTransportRequest> Requests =
            new List<DnsTransportRequest>();

        public DnsTransportResult Exchange(DnsTransportRequest request)
        {
            Requests.Add(request);
            return Steps.Dequeue()(request);
        }

        public void CloseTransport()
        {
        }

        public void Enqueue(
            Func<DnsTransportRequest, DnsTransportResult> step)
        {
            Steps.Enqueue(step);
        }
    }
}
