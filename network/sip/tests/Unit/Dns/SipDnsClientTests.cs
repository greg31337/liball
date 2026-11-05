namespace sip.Tests;

public class SipDnsClientTests
{
    [Test]
    public void ResolvesAddressRecordsOverUdp()
    {
        using System.Net.Sockets.UdpClient server = CreateUdpServer();
        int port = LocalPort(server);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = server.Receive(ref remote);
            byte[] response = BuildAddressResponse(query, new byte[] { 192, 0, 2, 40 });
            server.Send(response, response.Length, remote);
        });

        SipDnsClientResult result = SipDnsClient.QueryRecords("example.com",
            SipDnsRecordKind.A, Options(port, 1, 1000), null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Attempts, Is.EqualTo(1));
            Assert.That(result.UsedTcp, Is.False);
            Assert.That(result.Response!.Records.Single().Address, Is.EqualTo("192.0.2.40"));
        });
    }

    [Test]
    public void IgnoresUnrelatedTransactionsBeforeMatchingResponse()
    {
        using System.Net.Sockets.UdpClient server = CreateUdpServer();
        int port = LocalPort(server);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = server.Receive(ref remote);
            byte[] unrelated = BuildAddressResponse(query, new byte[] { 192, 0, 2, 1 });
            unrelated[1] = (byte)(unrelated[1] + 1);
            byte[] matching = BuildAddressResponse(query, new byte[] { 192, 0, 2, 2 });
            server.Send(unrelated, unrelated.Length, remote);
            server.Send(matching, matching.Length, remote);
        });

        SipDnsClientResult result = SipDnsClient.QueryRecords("example.com",
            SipDnsRecordKind.A, Options(port, 1, 1000), null);
        responder.GetAwaiter().GetResult();

        Assert.That(result.Response!.Records.Single().Address, Is.EqualTo("192.0.2.2"));
    }

    [Test]
    public void IgnoresSameTransactionResponsesForDifferentQuestions()
    {
        using System.Net.Sockets.UdpClient server = CreateUdpServer();
        int port = LocalPort(server);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = server.Receive(ref remote);
            byte[] unrelated = BuildAddressResponse(
                query, new byte[] { 192, 0, 2, 1 });
            unrelated[13] = (byte)'x';
            unrelated[6] = 0xff;
            unrelated[7] = 0xff;
            byte[] matching = BuildAddressResponse(
                query, new byte[] { 192, 0, 2, 2 });
            server.Send(unrelated, unrelated.Length, remote);
            server.Send(matching, matching.Length, remote);
        });

        SipDnsClientResult result = SipDnsClient.QueryRecords("example.com",
            SipDnsRecordKind.A, Options(port, 1, 1000), null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Attempts, Is.EqualTo(1));
            Assert.That(result.Response!.Records.Single().Address,
                Is.EqualTo("192.0.2.2"));
        });
    }

    [Test]
    public void RetriesAcrossConfiguredNameServers()
    {
        using System.Net.Sockets.UdpClient blackhole = CreateUdpServer();
        using System.Net.Sockets.UdpClient responsive = CreateUdpServer();
        int responsivePort = LocalPort(responsive);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = responsive.Receive(ref remote);
            byte[] response = BuildAddressResponse(query, new byte[] { 198, 51, 100, 8 });
            responsive.Send(response, response.Length, remote);
        });
        SipDnsOptions options = Options(LocalPort(blackhole), 2, 50);
        options.NameServers = new SipEndpointAddress[]
        {
            Endpoint(LocalPort(blackhole)), Endpoint(responsivePort)
        };

        SipDnsClientResult result = SipDnsClient.QueryRecords(
            "retry.example", SipDnsRecordKind.A, options, null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Attempts, Is.EqualTo(2));
            Assert.That(result.NameServer, Does.EndWith(":" + responsivePort));
        });
    }

    [Test]
    public void RetriesTruncatedUdpResponsesOverTcp()
    {
        using System.Net.Sockets.TcpListener tcp = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        tcp.Start();
        int port = ((System.Net.IPEndPoint)tcp.LocalEndpoint).Port;
        using System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient(
            new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

        Task udpResponder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = udp.Receive(ref remote);
            byte[] response = BuildEmptyResponse(query, 0x8380);
            udp.Send(response, response.Length, remote);
        });
        Task tcpResponder = Task.Run(() =>
        {
            using System.Net.Sockets.TcpClient client = tcp.AcceptTcpClient();
            using System.Net.Sockets.NetworkStream stream = client.GetStream();
            byte[] length = ReadExact(stream, 2);
            int queryLength = (length[0] << 8) | length[1];
            byte[] query = ReadExact(stream, queryLength);
            byte[] response = BuildAddressResponse(query, new byte[] { 203, 0, 113, 9 });
            byte[] responseLength = new byte[]
            {
                (byte)(response.Length >> 8), (byte)response.Length
            };
            stream.Write(responseLength);
            stream.Write(response);
        });

        SipDnsClientResult result = SipDnsClient.QueryRecords("tcp.example",
            SipDnsRecordKind.A, Options(port, 1, 1000), null);
        Task.WhenAll(udpResponder, tcpResponder).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.UsedTcp, Is.True);
            Assert.That(result.Response!.Records.Single().Address, Is.EqualTo("203.0.113.9"));
        });
    }

    [Test]
    public void FollowsBoundedCnameChainsAndRetainsChainRecords()
    {
        using System.Net.Sockets.UdpClient server = CreateUdpServer();
        int port = LocalPort(server);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] first = server.Receive(ref remote);
            byte[] cname = BuildCnameResponse(first, "alias.example.com");
            server.Send(cname, cname.Length, remote);
            byte[] second = server.Receive(ref remote);
            byte[] address = BuildAddressResponse(second, new byte[] { 192, 0, 2, 55 });
            server.Send(address, address.Length, remote);
        });

        SipDnsClientResult result = SipDnsClient.QueryRecords("origin.example.com",
            SipDnsRecordKind.A, Options(port, 1, 1000), null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Success), result.ErrorMessage);
            Assert.That(result.Attempts, Is.EqualTo(2));
            Assert.That(result.CnameDepth, Is.EqualTo(1));
            Assert.That(result.Response!.Records.Select(record => record.Kind),
                Is.EqualTo(new SipDnsRecordKind[] { SipDnsRecordKind.Cname, SipDnsRecordKind.A }));
        });
    }

    [Test]
    public void RejectsCnameDepthOverflow()
    {
        using System.Net.Sockets.UdpClient server = CreateUdpServer();
        int port = LocalPort(server);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = server.Receive(ref remote);
            byte[] cname = BuildCnameResponse(query, "alias.example.com");
            server.Send(cname, cname.Length, remote);
        });
        SipDnsOptions options = Options(port, 1, 1000);
        options.MaxCnameDepth = 0;

        SipDnsClientResult result = SipDnsClient.QueryRecords(
            "origin.example.com", SipDnsRecordKind.A, options, null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(result.ErrorMessage, Does.Contain("depth limit"));
        });
    }

    [Test]
    public void RejectsCyclicCnameChains()
    {
        using System.Net.Sockets.UdpClient server = CreateUdpServer();
        int port = LocalPort(server);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] first = server.Receive(ref remote);
            byte[] toAlias = BuildCnameResponse(first, "alias.example.com");
            server.Send(toAlias, toAlias.Length, remote);
            byte[] second = server.Receive(ref remote);
            byte[] toOrigin = BuildCnameResponse(second, "origin.example.com");
            server.Send(toOrigin, toOrigin.Length, remote);
        });

        SipDnsClientResult result = SipDnsClient.QueryRecords("origin.example.com",
            SipDnsRecordKind.A, Options(port, 1, 1000), null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(result.ErrorMessage, Does.Contain("loop"));
            Assert.That(result.Attempts, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReportsTimeoutCancellationAndMalformedResponses()
    {
        using System.Net.Sockets.UdpClient blackhole = CreateUdpServer();
        SipDnsClientResult timeout = SipDnsClient.QueryRecords("timeout.example",
            SipDnsRecordKind.A, Options(LocalPort(blackhole), 2, 20), null);

        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        SipDnsClientResult cancelled = SipDnsClient.QueryRecords("cancelled.example",
            SipDnsRecordKind.A, Options(LocalPort(blackhole), 1, 1000), cancellation);

        using System.Net.Sockets.UdpClient malformedServer = CreateUdpServer();
        int malformedPort = LocalPort(malformedServer);
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = malformedServer.Receive(ref remote);
            malformedServer.Send(query, query.Length, remote);
        });
        SipDnsClientResult malformed = SipDnsClient.QueryRecords("malformed.example",
            SipDnsRecordKind.A, Options(malformedPort, 1, 1000), null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(timeout.Status, Is.EqualTo(SipResolutionStatus.Timeout));
            Assert.That(timeout.Attempts, Is.EqualTo(2));
            Assert.That(cancelled.Status, Is.EqualTo(SipResolutionStatus.Cancelled));
            Assert.That(cancelled.Attempts, Is.Zero);
            Assert.That(malformed.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(malformed.ErrorMessage, Does.Contain("not a response"));
        });
    }

    static System.Net.Sockets.UdpClient CreateUdpServer()
    {
        return new System.Net.Sockets.UdpClient(
            new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
    }

    static int LocalPort(System.Net.Sockets.UdpClient server)
    {
        return ((System.Net.IPEndPoint)server.Client.LocalEndPoint!).Port;
    }

    static SipDnsOptions Options(int port, int attempts, int timeout)
    {
        SipDnsOptions options = new SipDnsOptions();
        options.NameServers = new SipEndpointAddress[] { Endpoint(port) };
        options.MaxAttempts = attempts;
        options.TimeoutMilliseconds = timeout;
        options.MaxRecordCount = 16;
        options.MaxCnameDepth = 4;
        return options;
    }

    static SipEndpointAddress Endpoint(int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = "127.0.0.1";
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        return endpoint;
    }

    static byte[] BuildEmptyResponse(byte[] query, int flags)
    {
        List<byte> response = StartResponse(query, flags, 0);
        return response.ToArray();
    }

    static byte[] BuildAddressResponse(byte[] query, byte[] address)
    {
        List<byte> response = StartResponse(query, 0x8180, 1);
        AddPointer(response, 12);
        AddUInt16(response, 1);
        AddUInt16(response, 1);
        AddUInt32(response, 60);
        AddUInt16(response, address.Length);
        response.AddRange(address);
        return response.ToArray();
    }

    static byte[] BuildCnameResponse(byte[] query, string target)
    {
        List<byte> response = StartResponse(query, 0x8180, 1);
        AddPointer(response, 12);
        AddUInt16(response, 5);
        AddUInt16(response, 1);
        AddUInt32(response, 120);
        List<byte> encodedTarget = new List<byte>();
        AddName(encodedTarget, target);
        AddUInt16(response, encodedTarget.Count);
        response.AddRange(encodedTarget);
        return response.ToArray();
    }

    static List<byte> StartResponse(byte[] query, int flags, int answerCount)
    {
        List<byte> response = new List<byte>();
        response.Add(query[0]);
        response.Add(query[1]);
        AddUInt16(response, flags);
        AddUInt16(response, 1);
        AddUInt16(response, answerCount);
        AddUInt16(response, 0);
        AddUInt16(response, 0);
        response.AddRange(query.Skip(12));
        return response;
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

    static byte[] ReadExact(System.IO.Stream stream, int length)
    {
        byte[] bytes = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = stream.Read(bytes, offset, length - offset);
            Assert.That(read, Is.GreaterThan(0));
            offset += read;
        }
        return bytes;
    }
}
