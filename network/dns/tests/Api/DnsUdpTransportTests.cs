using System.Net;
using System.Net.Sockets;

namespace dns.Tests.Api;

public class DnsUdpTransportTests
{
    [Test]
    public void ExchangesCorrelatedResponseOverIpv4()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x1234, "www.example.");
        byte[] queryBytes = Encode(query);
        byte[] responseBytes = Encode(Response(query));
        Task serverTask = Task.Run(() => Reply(server, responseBytes));
        DnsTransportRequest request = Request(server, query, queryBytes);
        request.QueryMessage = null;

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Udp));
            Assert.That(result.ResponseBytes, Is.EqualTo(responseBytes));
            Assert.That(result.ResponseMessage, Is.Not.Null);
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x1234));
            Assert.That(result.Endpoint.HasAddress, Is.True);
            Assert.That(result.Endpoint.AddressFamily, Is.EqualTo(4));
            Assert.That(result.StartedAt.UnixTimeMilliseconds,
                Is.GreaterThan(0));
            Assert.That(result.CompletedAt.MonotonicMilliseconds,
                Is.GreaterThanOrEqualTo(result.StartedAt.MonotonicMilliseconds));
            Assert.That(result.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void ExchangesCorrelatedResponseOverIpv6()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not supported on this platform.");
        }

        using Socket server = BindServer(AddressFamily.InterNetworkV6);
        DnsMessage query = Query(0x4321, "v6.example.");
        byte[] queryBytes = Encode(query);
        byte[] responseBytes = Encode(Response(query));
        Task serverTask = Task.Run(() => Reply(server, responseBytes));
        DnsTransportRequest request = Request(server, query, queryBytes);

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Endpoint.AddressFamily, Is.EqualTo(6));
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x4321));
        });
    }

    [Test]
    public void UnconnectedExchangeFiltersSourceIdentifierQuestionAndDirection()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        using Socket rogue = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x5050, "wanted.example.");
        byte[] queryBytes = Encode(query);
        byte[] correct = Encode(Response(query));
        Task serverTask = Task.Run(() =>
        {
            Datagram received = Receive(server);
            rogue.SendTo(correct, received.RemoteEndpoint);
            Thread.Sleep(10);
            DnsMessage wrongIdentifier = Response(query);
            wrongIdentifier.Header.Identifier = 0x5051;
            server.SendTo(Encode(wrongIdentifier), received.RemoteEndpoint);
            Thread.Sleep(10);
            DnsMessage wrongQuestion = Response(query);
            wrongQuestion.Questions[0].Name = Parse("other.example.");
            server.SendTo(Encode(wrongQuestion), received.RemoteEndpoint);
            Thread.Sleep(10);
            server.SendTo(queryBytes, received.RemoteEndpoint);
            Thread.Sleep(10);
            server.SendTo(correct, received.RemoteEndpoint);
        });
        DnsTransportRequest request = Request(server, query, queryBytes);
        request.Options.ConnectUdpSocket = false;

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.ResponseBytes, Is.EqualTo(correct));
            Assert.That(result.ResponseMessage!.Questions[0].Name.Labels[0],
                Is.EqualTo(query.Questions[0].Name.Labels[0]));
        });
    }

    [Test]
    public void TimesOutWhenServerDoesNotRespond()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x6000, "timeout.example.");
        byte[] queryBytes = Encode(query);
        Task<Datagram> serverTask = Task.Run(() => Receive(server));
        DnsTransportRequest request = Request(server, query, queryBytes);
        request.Options.AttemptTimeoutMilliseconds = 40;

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        Datagram ignored = serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.Cancelled, Is.False);
            Assert.That(result.ResponseBytes, Is.Empty);
            Assert.That(result.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void ObservesCancellationRequestedDuringReceive()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x6100, "cancel.example.");
        DnsTransportRequest request = Request(server, query, Encode(query));
        request.Options.AttemptTimeoutMilliseconds = 2000;
        using ManualResetEventSlim received = new ManualResetEventSlim();
        Task serverTask = Task.Run(() =>
        {
            Datagram ignored = Receive(server);
            received.Set();
        });
        Task<DnsTransportResult> exchangeTask = Task.Run(
            () => new DnsUdpTransport().Exchange(request));
        Assert.That(received.Wait(1000), Is.True);

        request.CancellationRequested = true;
        DnsTransportResult result = exchangeTask.GetAwaiter().GetResult();
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Cancelled));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void ReportsResponseLargerThanConfiguredReceiveLimit()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x6200, "large.example.");
        byte[] responseBytes = Encode(Response(query));
        Task serverTask = Task.Run(() => Reply(server, responseBytes));
        DnsTransportRequest request = Request(server, query, Encode(query));
        request.Options.MaximumResponseLength = responseBytes.Length - 1;

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(result.Truncated, Is.True);
            Assert.That(result.ResponseBytes, Has.Length.EqualTo(responseBytes.Length));
            Assert.That(result.ResponseMessage, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void IgnoresOversizedDatagramForAnotherIdentifier()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x6250, "limit.example.");
        byte[] queryBytes = Encode(query);
        byte[] correct = Encode(Response(query));
        byte[] unrelated = new byte[correct.Length + 1];
        unrelated[0] = 0x62;
        unrelated[1] = 0x51;
        unrelated[2] = 0x80;
        Task serverTask = Task.Run(() =>
        {
            Datagram received = Receive(server);
            server.SendTo(unrelated, received.RemoteEndpoint);
            Thread.Sleep(10);
            server.SendTo(correct, received.RemoteEndpoint);
        });
        DnsTransportRequest request = Request(server, query, queryBytes);
        request.Options.MaximumResponseLength = correct.Length;

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Truncated, Is.False);
            Assert.That(result.ResponseBytes, Is.EqualTo(correct));
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x6250));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ReportsMalformedMatchingResponseAsProtocolError()
    {
        using Socket server = BindServer(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x6300, "malformed.example.");
        byte[] malformed = [0x63, 0x00, 0x80];
        Task serverTask = Task.Run(() => Reply(server, malformed));
        DnsTransportRequest request = Request(server, query, Encode(query));

        DnsTransportResult result = new DnsUdpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.ResponseBytes, Is.EqualTo(malformed));
            Assert.That(result.ResponseMessage, Is.Null);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
        });
    }

    [Test]
    public void ClosedTransportRejectsSubsequentExchange()
    {
        DnsMessage query = Query(0x6400, "closed.example.");
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint.HasAddress = true;
        request.Endpoint.AddressFamily = 4;
        request.Endpoint.Address = [127, 0, 0, 1];
        request.Endpoint.Port = 53;
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        DnsUdpTransport transport = new DnsUdpTransport();
        transport.CloseTransport();

        DnsTransportResult result = transport.Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Closed));
            Assert.That(result.ResponseBytes, Is.Empty);
            Assert.That(result.CompletedAt.UnixTimeMilliseconds,
                Is.GreaterThan(0));
        });
    }

    static Socket BindServer(AddressFamily addressFamily)
    {
        Socket socket = new Socket(
            addressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.ReceiveTimeout = 2000;
        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
        }
        else
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        }

        return socket;
    }

    static DnsTransportRequest Request(
        Socket server,
        DnsMessage query,
        byte[] queryBytes)
    {
        IPEndPoint serverEndpoint = (IPEndPoint)server.LocalEndPoint!;
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint.HasAddress = true;
        request.Endpoint.Address = serverEndpoint.Address.GetAddressBytes();
        request.Endpoint.AddressFamily =
            serverEndpoint.AddressFamily == AddressFamily.InterNetwork ? 4 : 6;
        request.Endpoint.Port = serverEndpoint.Port;
        request.Options.Kind = DnsTransportKind.Udp;
        request.Options.AttemptTimeoutMilliseconds = 1000;
        request.Options.MaximumResponseLength = 4096;
        request.QueryMessage = query;
        request.QueryBytes = queryBytes;
        return request;
    }

    static void Reply(Socket server, byte[] response)
    {
        Datagram received = Receive(server);
        server.SendTo(response, received.RemoteEndpoint);
    }

    static Datagram Receive(Socket server)
    {
        byte[] buffer = new byte[4096];
        EndPoint remote;
        if (server.AddressFamily == AddressFamily.InterNetworkV6)
        {
            remote = new IPEndPoint(IPAddress.IPv6Any, 0);
        }
        else
        {
            remote = new IPEndPoint(IPAddress.Any, 0);
        }

        int length = server.ReceiveFrom(buffer, ref remote);
        Datagram result = new Datagram();
        result.Bytes = buffer.Take(length).ToArray();
        result.RemoteEndpoint = remote;
        return result;
    }

    static DnsMessage Query(ushort identifier, string name)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = identifier;
        message.Header.Flags = DnsHeaderFlag.RecursionDesired;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        message.Questions.Add(question);
        return message;
    }

    static DnsMessage Response(DnsMessage query)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = query.Header.Identifier;
        message.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired |
            DnsHeaderFlag.RecursionAvailable;
        DnsQuestion question = new DnsQuestion();
        question.Name = MessageOperations.CloneMessage(query).Questions[0].Name;
        question.RecordType = query.Questions[0].RecordType;
        question.RecordTypeValue = query.Questions[0].RecordTypeValue;
        question.Class = query.Questions[0].Class;
        question.ClassValue = query.Questions[0].ClassValue;
        message.Questions.Add(question);
        return message;
    }

    static byte[] Encode(DnsMessage message)
    {
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        return encoded.Bytes;
    }

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    class Datagram
    {
        public byte[] Bytes = [];
        public EndPoint RemoteEndpoint = null!;
    }
}
