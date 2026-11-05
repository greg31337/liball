using System.Net;
using System.Net.Sockets;

namespace dns.Tests.Api;

public class DnsQueryClientFallbackTests
{
    [Test]
    public void RetriesTruncatedUdpResponseOverTcpWithSameQuery()
    {
        using Socket tcp = BindTcp();
        using Socket udp = BindUdp(Port(tcp));
        byte[] udpQuery = [];
        byte[] tcpQuery = [];
        Task udpTask = Task.Run(() =>
        {
            Datagram received = ReceiveUdp(udp);
            udpQuery = received.Bytes;
            DnsMessage query = Decode(received.Bytes);
            udp.SendTo(
                Encode(Response(query, true)), received.RemoteEndpoint);
        });
        Task tcpTask = Task.Run(() =>
        {
            using Socket connection = tcp.Accept();
            tcpQuery = ReadFrame(connection);
            DnsMessage query = Decode(tcpQuery);
            SendFrame(connection, Encode(Response(query, false)));
        });
        DnsQueryRequest request = Request(Port(tcp), "fallback.example.");
        request.RequestDnssec = true;
        request.CheckingDisabled = true;

        DnsQueryResult result = new DnsQueryClient().Query(request);
        udpTask.GetAwaiter().GetResult();
        tcpTask.GetAwaiter().GetResult();
        DnsMessage sent = Decode(udpQuery);
        DnsEdnsData edns = sent.Additionals[0].Data!.Edns!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(2));
            Assert.That(result.Attempts[0].Transport,
                Is.EqualTo(DnsTransportKind.Udp));
            Assert.That(result.Attempts[0].Truncated, Is.True);
            Assert.That(result.Attempts[1].Transport,
                Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(result.Attempts[1].UsedFallback, Is.True);
            Assert.That(result.Attempts[1].FallbackFrom,
                Is.EqualTo(DnsTransportKind.Udp));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.Header.Identifier,
                Is.EqualTo(sent.Header.Identifier));
            Assert.That(result.Response.Header.Flags & DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.None));
            Assert.That(result.ServerStates, Has.Count.EqualTo(1));
            Assert.That(result.ServerStates[0].AttemptCount, Is.EqualTo(2));
            Assert.That(result.ServerStates[0].ConsecutiveFailures, Is.Zero);
            Assert.That(result.ServerStates[0].HasSuccessfulExchange, Is.True);
            Assert.That(result.ServerStates[0].PreferredTransport,
                Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(result.ServerStates[0].LastAttempt.MonotonicMilliseconds,
                Is.EqualTo(result.ServerStates[0].LastSuccess.MonotonicMilliseconds));
            Assert.That(tcpQuery, Is.EqualTo(udpQuery));
            Assert.That(sent.Header.Flags & DnsHeaderFlag.CheckingDisabled,
                Is.EqualTo(DnsHeaderFlag.CheckingDisabled));
            Assert.That(edns.Flags & DnsEdnsFlag.DnssecAnswerOk,
                Is.EqualTo(DnsEdnsFlag.DnssecAnswerOk));
            Assert.That(edns.UdpPayloadSize,
                Is.EqualTo(DnsProtocolLimits.DefaultEdnsUdpPayloadSize));
        });
    }

    [Test]
    public void RetriesLocallyOversizedUdpResponseOverTcp()
    {
        using Socket tcp = BindTcp();
        using Socket udp = BindUdp(Port(tcp));
        Task udpTask = Task.Run(() =>
        {
            Datagram received = ReceiveUdp(udp);
            byte[] oversized = new byte[65];
            oversized[0] = received.Bytes[0];
            oversized[1] = received.Bytes[1];
            udp.SendTo(oversized, received.RemoteEndpoint);
        });
        Task tcpTask = Task.Run(() =>
        {
            using Socket connection = tcp.Accept();
            DnsMessage query = Decode(ReadFrame(connection));
            SendFrame(connection, Encode(Response(query, false)));
        });
        DnsQueryRequest request = Request(Port(tcp), "a.");
        request.TransportOptions.MaximumResponseLength = 64;

        DnsQueryResult result = new DnsQueryClient().Query(request);
        udpTask.GetAwaiter().GetResult();
        tcpTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(2));
            Assert.That(result.Attempts[0].Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(result.Attempts[0].Truncated, Is.True);
            Assert.That(result.Attempts[1].Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void DisabledFallbackReturnsTruncatedUdpResponse()
    {
        using Socket udp = BindUdp(0);
        Task udpTask = Task.Run(() =>
        {
            Datagram received = ReceiveUdp(udp);
            DnsMessage query = Decode(received.Bytes);
            udp.SendTo(
                Encode(Response(query, true)), received.RemoteEndpoint);
        });
        DnsQueryRequest request = Request(Port(udp), "no-fallback.example.");
        request.AllowTcpFallback = false;

        DnsQueryResult result = new DnsQueryClient().Query(request);
        udpTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.Attempts, Has.Count.EqualTo(1));
            Assert.That(result.Attempts[0].Truncated, Is.True);
            Assert.That(result.Response!.Header.Flags & DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.Truncated));
        });
    }

    [Test]
    public void DirectTcpSkipsUdpAndFallbackMetadata()
    {
        using Socket tcp = BindTcp();
        Task tcpTask = Task.Run(() =>
        {
            using Socket connection = tcp.Accept();
            DnsMessage query = Decode(ReadFrame(connection));
            SendFrame(connection, Encode(Response(query, false)));
        });
        DnsQueryRequest request = Request(Port(tcp), "direct.example.");
        request.TransportOptions.Kind = DnsTransportKind.Tcp;

        DnsQueryResult result = new DnsQueryClient().Query(request);
        tcpTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.Attempts, Has.Count.EqualTo(1));
            Assert.That(result.Attempts[0].Transport,
                Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(result.Attempts[0].UsedFallback, Is.False);
        });
    }

    [Test]
    public void TcpFallbackRetainsOriginalTotalDeadline()
    {
        using Socket tcp = BindTcp();
        using Socket udp = BindUdp(Port(tcp));
        Task udpTask = Task.Run(() =>
        {
            Datagram received = ReceiveUdp(udp);
            Thread.Sleep(30);
            DnsMessage query = Decode(received.Bytes);
            udp.SendTo(
                Encode(Response(query, true)), received.RemoteEndpoint);
        });
        Task tcpTask = Task.Run(() =>
        {
            using Socket connection = tcp.Accept();
            byte[] ignored = ReadFrame(connection);
            connection.Send([0, 12]);
            Thread.Sleep(150);
        });
        DnsQueryRequest request = Request(Port(tcp), "deadline.example.");
        request.TransportOptions.AttemptTimeoutMilliseconds = 1000;
        request.TransportOptions.TotalTimeoutMilliseconds = 70;

        DnsQueryResult result = new DnsQueryClient().Query(request);
        udpTask.GetAwaiter().GetResult();
        tcpTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(2));
            Assert.That(result.Attempts[1].TimedOut, Is.True);
            Assert.That(result.Attempts[1].UsedFallback, Is.True);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(500));
        });
    }

    [Test]
    public void CompleteUdpResponseDoesNotOpenTcpConnection()
    {
        using Socket udp = BindUdp(0);
        Task udpTask = Task.Run(() =>
        {
            Datagram received = ReceiveUdp(udp);
            DnsMessage query = Decode(received.Bytes);
            udp.SendTo(
                Encode(Response(query, false)), received.RemoteEndpoint);
        });
        DnsQueryRequest request = Request(Port(udp), "complete.example.");

        DnsQueryResult result = new DnsQueryClient().Query(request);
        udpTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.Attempts, Has.Count.EqualTo(1));
            Assert.That(result.Attempts[0].Transport,
                Is.EqualTo(DnsTransportKind.Udp));
            Assert.That(result.ResponseBytes, Is.Not.Empty);
        });
    }

    static Socket BindTcp()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        socket.Listen(4);
        return socket;
    }

    static Socket BindUdp(int port)
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.ReceiveTimeout = 2000;
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
        return socket;
    }

    static int Port(Socket socket)
    {
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    static DnsQueryRequest Request(int port, string name)
    {
        DnsQueryRequest request = new DnsQueryRequest();
        request.Question.Name = Parse(name);
        request.Question.RecordType = DnsRecordType.A;
        request.Question.RecordTypeValue = 1;
        request.Question.Class = DnsClass.Internet;
        request.Question.ClassValue = 1;
        DnsEndpoint endpoint = new DnsEndpoint();
        endpoint.HasAddress = true;
        endpoint.AddressFamily = 4;
        endpoint.Address = [127, 0, 0, 1];
        endpoint.Port = port;
        request.Servers.Add(endpoint);
        request.TransportOptions.Kind = DnsTransportKind.Udp;
        request.TransportOptions.AttemptTimeoutMilliseconds = 1000;
        request.TransportOptions.TotalTimeoutMilliseconds = 2000;
        request.TransportOptions.MaximumResponseLength = 4096;
        return request;
    }

    static Datagram ReceiveUdp(Socket socket)
    {
        byte[] buffer = new byte[4096];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        int length = socket.ReceiveFrom(buffer, ref remote);
        Datagram result = new Datagram();
        result.Bytes = buffer.Take(length).ToArray();
        result.RemoteEndpoint = remote;
        return result;
    }

    static byte[] ReadFrame(Socket connection)
    {
        byte[] prefix = ReceiveExact(connection, 2);
        int length = (prefix[0] << 8) | prefix[1];
        return ReceiveExact(connection, length);
    }

    static byte[] ReceiveExact(Socket connection, int length)
    {
        byte[] bytes = new byte[length];
        int received = 0;
        while (received < length)
        {
            int count = connection.Receive(
                bytes, received, length - received, SocketFlags.None);
            if (count == 0)
            {
                throw new InvalidOperationException("The test connection closed early.");
            }

            received += count;
        }

        return bytes;
    }

    static void SendFrame(Socket connection, byte[] message)
    {
        byte[] frame = new byte[message.Length + 2];
        frame[0] = (byte)(message.Length >> 8);
        frame[1] = (byte)(message.Length & 255);
        Array.Copy(message, 0, frame, 2, message.Length);
        int sent = 0;
        while (sent < frame.Length)
        {
            sent += connection.Send(
                frame, sent, frame.Length - sent, SocketFlags.None);
        }
    }

    static DnsMessage Decode(byte[] bytes)
    {
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            bytes, new DnsDecodeOptions());
        Assert.That(decoded.Success, Is.True);
        return decoded.Message!;
    }

    static byte[] Encode(DnsMessage message)
    {
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        return encoded.Bytes;
    }

    static DnsMessage Response(DnsMessage query, bool truncated)
    {
        DnsMessage response = new DnsMessageCodec().CloneMessage(query);
        response.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired |
            DnsHeaderFlag.RecursionAvailable;
        response.Header.FlagsValue = (int)response.Header.Flags;
        if (truncated)
        {
            response.Header.Flags = response.Header.Flags |
                DnsHeaderFlag.Truncated;
            response.Header.FlagsValue = (int)response.Header.Flags;
        }

        return response;
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
