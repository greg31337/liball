using System.Net;
using System.Net.Sockets;

namespace dns.Tests.Api;

public class DnsServerTests
{
    [Test]
    public void ServesBoundedAuthoritativeUdpResponseAndStopsGracefully()
    {
        MemoryDnsZoneStore store = StoreWithAddressRecords(20);
        DnsServer server = new DnsServer(store);
        DnsServerOptions options = Options(ReserveUdpPort());
        options.Authoritative.MaximumUdpResponseLength = 96;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        byte[] responseBytes = Exchange(
            options.Endpoints[0],
            Encode(Query(0x1234, "www.example.")));
        DnsDecodeResult decoded = Decode(responseBytes);
        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message, Is.Not.Null);
            Assert.That(decoded.Message!.Header.Identifier,
                Is.EqualTo(0x1234));
            Assert.That(decoded.Message.Header.FlagsValue &
                (int)DnsHeaderFlag.Response, Is.Not.Zero);
            Assert.That(decoded.Message.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Not.Zero);
            Assert.That(decoded.Message.Header.FlagsValue &
                (int)DnsHeaderFlag.Truncated, Is.Not.Zero);
            Assert.That(responseBytes.Length, Is.LessThanOrEqualTo(96));
            Assert.That(result.Status, Is.EqualTo(DnsServerStatus.Cancelled));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.RequestCount, Is.EqualTo(1));
            Assert.That(result.UdpRequestCount, Is.EqualTo(1));
            Assert.That(result.TcpRequestCount, Is.Zero);
            Assert.That(result.FailedRequestCount, Is.Zero);
            Assert.That(result.BoundEndpoints, Has.Count.EqualTo(1));
            Assert.That(result.CompletedAt.MonotonicMilliseconds,
                Is.GreaterThanOrEqualTo(
                    result.StartedAt.MonotonicMilliseconds));
        });
    }

    [Test]
    public void MalformedAndOversizedDatagramsDoNotStopUdpService()
    {
        MemoryDnsZoneStore store = StoreWithAddressRecords(1);
        DnsServer server = new DnsServer(store);
        DnsServerOptions options = Options(ReserveUdpPort());
        options.MaximumRequestLength = 64;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));
        byte[] malformed = new byte[DnsProtocolLimits.HeaderLength];
        malformed[0] = 0x45;
        malformed[1] = 0x67;
        malformed[4] = 0;
        malformed[5] = 1;

        byte[] formatErrorBytes = Exchange(
            options.Endpoints[0], malformed);
        DnsDecodeResult formatError = Decode(formatErrorBytes);
        SendWithoutResponse(
            options.Endpoints[0],
            new byte[options.MaximumRequestLength + 1]);
        byte[] validBytes = Exchange(
            options.Endpoints[0],
            Encode(Query(0x89ab, "www.example.")));
        DnsDecodeResult valid = Decode(validBytes);
        options.StopRequested = true;
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(formatError.Success, Is.True);
            Assert.That(formatError.Message, Is.Not.Null);
            Assert.That(formatError.Message!.Header.Identifier,
                Is.EqualTo(0x4567));
            Assert.That(formatError.Message.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.FormatError));
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Message!.Header.Identifier,
                Is.EqualTo(0x89ab));
            Assert.That(result.Status, Is.EqualTo(DnsServerStatus.Cancelled));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.RequestCount, Is.EqualTo(3));
            Assert.That(result.UdpRequestCount, Is.EqualTo(3));
            Assert.That(result.FailedRequestCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void CancelledBeforeRunDoesNotBindOrAcceptRequests()
    {
        DnsServerOptions options = Options(ReserveUdpPort());
        options.StopRequested = true;

        DnsServerRunResult result = new DnsServer(
            StoreWithAddressRecords(1)).RunServer(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsServerStatus.Cancelled));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.BoundEndpoints, Is.Empty);
            Assert.That(result.RequestCount, Is.Zero);
            Assert.That(result.StartedAt.UnixTimeMilliseconds,
                Is.GreaterThan(0));
            Assert.That(result.CompletedAt.MonotonicMilliseconds,
                Is.GreaterThanOrEqualTo(
                    result.StartedAt.MonotonicMilliseconds));
        });
    }

    [Test]
    public void ServesUdpAndFragmentedPipelinedTcpOnOneEndpoint()
    {
        MemoryDnsZoneStore store = StoreWithAddressRecords(2);
        DnsServer server = new DnsServer(store);
        DnsServerOptions options = BothOptions(ReserveDnsPort());
        options.MaximumTcpRequestsPerConnection = 2;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        DnsDecodeResult udp = Decode(Exchange(
            options.Endpoints[0],
            Encode(Query(0x2010, "www.example."))));
        using Socket client = ConnectTcp(options.Endpoints[0]);
        SendFragmented(
            client,
            Frame(Encode(Query(0x2020, "www.example."))));
        SendAll(
            client,
            Frame(Encode(Query(0x2030, "www.example."))));
        DnsDecodeResult first = Decode(ReadFrame(client));
        DnsDecodeResult second = Decode(ReadFrame(client));
        Assert.That(WaitForClosure(client, 2000), Is.True);
        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(udp.Success, Is.True);
            Assert.That(udp.Message!.Header.Identifier, Is.EqualTo(0x2010));
            Assert.That(first.Success, Is.True);
            Assert.That(first.Message!.Header.Identifier, Is.EqualTo(0x2020));
            Assert.That(second.Success, Is.True);
            Assert.That(second.Message!.Header.Identifier, Is.EqualTo(0x2030));
            Assert.That(result.Status, Is.EqualTo(DnsServerStatus.Cancelled));
            Assert.That(result.RequestCount, Is.EqualTo(3));
            Assert.That(result.UdpRequestCount, Is.EqualTo(1));
            Assert.That(result.TcpRequestCount, Is.EqualTo(2));
            Assert.That(result.FailedRequestCount, Is.Zero);
            Assert.That(result.BoundEndpoints, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ServesConcurrentUdpAndTcpClientsIndependently()
    {
        DnsServer server = new DnsServer(StoreWithAddressRecords(2));
        DnsServerOptions options = BothOptions(ReserveDnsPort());
        options.MaximumTcpRequestsPerConnection = 1;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));
        List<Task<DnsDecodeResult>> exchanges =
            new List<Task<DnsDecodeResult>>();
        int index = 0;
        while (index < 8)
        {
            ushort identifier = (ushort)(0x2800 + index);
            if (index % 2 == 0)
            {
                exchanges.Add(Task.Run(() => Decode(Exchange(
                    options.Endpoints[0],
                    Encode(Query(identifier, "www.example."))))));
            }
            else
            {
                exchanges.Add(Task.Run(() =>
                {
                    using Socket client = ConnectTcp(options.Endpoints[0]);
                    SendAll(
                        client,
                        Frame(Encode(Query(identifier, "www.example."))));
                    return Decode(ReadFrame(client));
                }));
            }

            index = index + 1;
        }

        Assert.That(Task.WaitAll(exchanges.ToArray(), 5000), Is.True);
        List<DnsDecodeResult> responses = exchanges.Select(
            exchange => exchange.GetAwaiter().GetResult()).ToList();
        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(responses.All(response => response.Success), Is.True);
            Assert.That(responses.Select(response =>
                response.Message!.Header.Identifier).Order(),
                Is.EqualTo(Enumerable.Range(0, 8).Select(value =>
                    (ushort)(0x2800 + value))));
            Assert.That(result.RequestCount, Is.EqualTo(8));
            Assert.That(result.UdpRequestCount, Is.EqualTo(4));
            Assert.That(result.TcpRequestCount, Is.EqualTo(4));
            Assert.That(result.FailedRequestCount, Is.Zero);
            Assert.That(result.Status, Is.EqualTo(DnsServerStatus.Cancelled));
        });
    }

    [Test]
    public void TcpMalformedAndOversizedFramesDoNotStopListener()
    {
        DnsServer server = new DnsServer(StoreWithAddressRecords(1));
        DnsServerOptions options = TcpOptions(ReserveTcpPort());
        options.MaximumRequestLength = 64;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));
        byte[] malformed = new byte[DnsProtocolLimits.HeaderLength];
        malformed[0] = 0x31;
        malformed[1] = 0x32;
        malformed[5] = 1;

        using (Socket malformedClient = ConnectTcp(options.Endpoints[0]))
        {
            SendAll(malformedClient, Frame(malformed));
            SendAll(
                malformedClient,
                Frame(Encode(Query(0x3133, "www.example."))));
            DnsDecodeResult formatError = Decode(ReadFrame(malformedClient));
            DnsDecodeResult valid = Decode(ReadFrame(malformedClient));
            Assert.Multiple(() =>
            {
                Assert.That(formatError.Message!.Header.Identifier,
                    Is.EqualTo(0x3132));
                Assert.That(formatError.Message.Header.ResponseCodeValue,
                    Is.EqualTo((int)DnsResponseCode.FormatError));
                Assert.That(valid.Message!.Header.Identifier,
                    Is.EqualTo(0x3133));
            });
        }

        using (Socket oversizedClient = ConnectTcp(options.Endpoints[0]))
        {
            int oversizedLength = options.MaximumRequestLength + 1;
            SendAll(
                oversizedClient,
                [(byte)(oversizedLength >> 8),
                    (byte)(oversizedLength & 255)]);
            Assert.That(
                WaitForClosure(oversizedClient, 2000),
                Is.True);
        }

        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(result.RequestCount, Is.EqualTo(3));
            Assert.That(result.TcpRequestCount, Is.EqualTo(3));
            Assert.That(result.FailedRequestCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void TcpIdleAndLifetimeLimitsCloseClientsWithoutStoppingServer()
    {
        DnsServer server = new DnsServer(StoreWithAddressRecords(1));
        DnsServerOptions options = TcpOptions(ReserveTcpPort());
        options.TcpIdleTimeoutMilliseconds = 60;
        options.TcpConnectionLifetimeMilliseconds = 1000;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        using (Socket partial = ConnectTcp(options.Endpoints[0]))
        {
            SendAll(partial, [0]);
            Assert.That(WaitForClosure(partial, 2000), Is.True);
        }

        options.TcpIdleTimeoutMilliseconds = 1000;
        options.TcpConnectionLifetimeMilliseconds = 60;
        using (Socket quiet = ConnectTcp(options.Endpoints[0]))
        {
            Assert.That(WaitForClosure(quiet, 2000), Is.True);
        }

        options.TcpConnectionLifetimeMilliseconds = 1000;
        using (Socket validClient = ConnectTcp(options.Endpoints[0]))
        {
            SendAll(
                validClient,
                Frame(Encode(Query(0x4142, "www.example."))));
            DnsDecodeResult valid = Decode(ReadFrame(validClient));
            Assert.That(valid.Message!.Header.Identifier,
                Is.EqualTo(0x4142));
        }

        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsServerStatus.Cancelled));
            Assert.That(result.RequestCount, Is.EqualTo(2));
            Assert.That(result.TcpRequestCount, Is.EqualTo(2));
            Assert.That(result.FailedRequestCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
        });
    }

    [Test]
    public void DispatchesAuthorizedUpdateNotifyAndStableUpdateDenial()
    {
        MemoryDnsZoneStore store = StoreWithAddressRecords(1);
        DnsServer server = new DnsServer(store);
        DnsServerOptions options = Options(ReserveUdpPort());
        options.Authoritative.Authorization.AllowUpdates = true;
        options.Authoritative.Authorization.RequireTsigForUpdates = false;
        options.Authoritative.Authorization.AllowNotify = true;
        options.Authoritative.Authorization.RequireTsigForNotify = false;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        DnsDecodeResult update = Decode(Exchange(
            options.Endpoints[0],
            Encode(UpdateMessage(0x5010, "added.example."))));
        DnsDecodeResult notify = Decode(Exchange(
            options.Endpoints[0],
            Encode(NotifyMessage(0x5020, 2))));
        options.Authoritative.Authorization.AllowUpdates = false;
        DnsDecodeResult denied = Decode(Exchange(
            options.Endpoints[0],
            Encode(UpdateMessage(0x5030, "denied.example."))));
        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);
        DnsZone zone = store.ReadZone(
            Parse("example."),
            DnsClass.Internet,
            (int)DnsClass.Internet)!;

        Assert.Multiple(() =>
        {
            Assert.That(update.Message!.Header.Opcode,
                Is.EqualTo(DnsOpcode.Update));
            Assert.That(update.Message.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.NoError));
            Assert.That(notify.Message!.Header.Opcode,
                Is.EqualTo(DnsOpcode.Notify));
            Assert.That(notify.Message.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.NoError));
            Assert.That(denied.Message!.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.Refused));
            Assert.That(store.FindExactNode(zone, Parse("added.example.")),
                Is.Not.Null);
            Assert.That(store.FindExactNode(zone, Parse("denied.example.")),
                Is.Null);
            Assert.That(result.RequestCount, Is.EqualTo(3));
            Assert.That(result.FailedRequestCount, Is.Zero);
        });
    }

    [Test]
    public void StreamsAuthorizedAxfrAndUpToDateIxfrOverTcp()
    {
        MemoryDnsZoneStore store = StoreWithAddressRecords(20);
        DnsServer server = new DnsServer(store);
        DnsServerOptions options = TcpOptions(ReserveTcpPort());
        options.Authoritative.Authorization.AllowTransfers = true;
        options.Authoritative.Authorization.RequireTsigForTransfers = false;
        options.Authoritative.MaximumTcpResponseLength = 150;
        options.MaximumTcpRequestsPerConnection = 1;
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        List<DnsMessage> axfr;
        using (Socket client = ConnectTcp(options.Endpoints[0]))
        {
            SendAll(client, Frame(Encode(TransferQuery(
                0x6010, DnsRecordType.Axfr))));
            axfr = DecodeFrames(ReadFramesUntilClosure(client));
        }

        List<DnsMessage> ixfr;
        using (Socket client = ConnectTcp(options.Endpoints[0]))
        {
            SendAll(client, Frame(Encode(IxfrQuery(0x6020, 1))));
            ixfr = DecodeFrames(ReadFramesUntilClosure(client));
        }

        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);
        List<DnsResourceRecord> axfrAnswers = Answers(axfr);
        List<DnsResourceRecord> ixfrAnswers = Answers(ixfr);

        Assert.Multiple(() =>
        {
            Assert.That(axfr, Has.Count.GreaterThan(1));
            Assert.That(axfr[0].Header.Identifier, Is.EqualTo(0x6010));
            Assert.That(axfrAnswers, Has.Count.GreaterThan(2));
            Assert.That(axfrAnswers[0].RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(axfrAnswers[axfrAnswers.Count - 1].RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(ixfr, Has.Count.EqualTo(1));
            Assert.That(ixfr[0].Header.Identifier, Is.EqualTo(0x6020));
            Assert.That(ixfrAnswers, Has.Count.EqualTo(1));
            Assert.That(ixfrAnswers[0].Data!.Soa!.Serial, Is.EqualTo(1));
            Assert.That(result.RequestCount, Is.EqualTo(2));
            Assert.That(result.TcpRequestCount, Is.EqualTo(2));
            Assert.That(result.FailedRequestCount, Is.Zero);
        });
    }

    [Test]
    public void MapsUdpTransferAndDisabledNotifyToProtocolResponses()
    {
        DnsServer server = new DnsServer(StoreWithAddressRecords(1));
        DnsServerOptions options = Options(ReserveUdpPort());
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        DnsDecodeResult transfer = Decode(Exchange(
            options.Endpoints[0],
            Encode(TransferQuery(0x7010, DnsRecordType.Axfr))));
        DnsDecodeResult notify = Decode(Exchange(
            options.Endpoints[0],
            Encode(NotifyMessage(0x7020, 1))));
        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(transfer.Message!.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.Refused));
            Assert.That(notify.Message!.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.Refused));
            Assert.That(result.RequestCount, Is.EqualTo(2));
            Assert.That(result.FailedRequestCount, Is.Zero);
        });
    }

    [Test]
    public void SelectsConfiguredTsigKeyForUpdateAndSignsResponse()
    {
        MemoryDnsZoneStore store = StoreWithAddressRecords(1);
        DnsTsigKey key = ServerKey();
        DnsMessage update = UpdateMessage(0x8010, "signed.example.");
        DnsTsigResult signed = new DnsTsigService().SignTsig(
            update,
            key,
            [],
            TransportTime.ReadClock());
        Assert.That(signed.Success, Is.True);
        DnsServer server = new DnsServer(store);
        DnsServerOptions options = Options(ReserveUdpPort());
        options.TsigKeys.Add(key);
        options.Authoritative.Authorization.AllowUpdates = true;
        options.Authoritative.Authorization.RequireTsigForUpdates = true;
        options.Authoritative.Authorization.AllowedTsigKeyNames.Add(
            Parse("server-key."));
        Task<DnsServerRunResult> run = Task.Run(
            () => server.RunServer(options));

        DnsDecodeResult response = Decode(Exchange(
            options.Endpoints[0],
            Encode(signed.Message!)));
        DnsTsigResult verified = new DnsTsigService().VerifyTsig(
            response.Message!,
            key,
            signed.Mac,
            TransportTime.ReadClock());
        server.StopServer();
        DnsServerRunResult result = AwaitRun(run);

        Assert.Multiple(() =>
        {
            Assert.That(response.Message!.Header.ResponseCodeValue,
                Is.EqualTo((int)DnsResponseCode.NoError));
            Assert.That(response.Message.Additionals[^1].RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Authenticated, Is.True);
            Assert.That(result.FailedRequestCount, Is.Zero);
        });
    }

    static DnsServerRunResult AwaitRun(Task<DnsServerRunResult> run)
    {
        Assert.That(run.Wait(3000), Is.True);
        return run.GetAwaiter().GetResult();
    }

    static DnsServerOptions Options(int port)
    {
        DnsServerOptions result = new DnsServerOptions();
        result.EnableTcp = false;
        result.Endpoints.Add(Endpoint(port));
        return result;
    }

    static DnsServerOptions TcpOptions(int port)
    {
        DnsServerOptions result = new DnsServerOptions();
        result.EnableUdp = false;
        result.Endpoints.Add(Endpoint(port));
        return result;
    }

    static DnsServerOptions BothOptions(int port)
    {
        DnsServerOptions result = new DnsServerOptions();
        result.Endpoints.Add(Endpoint(port));
        return result;
    }

    static DnsEndpoint Endpoint(int port)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.HasAddress = true;
        result.Address = IPAddress.Loopback.GetAddressBytes();
        result.AddressFamily = 4;
        result.Port = port;
        return result;
    }

    static int ReserveUdpPort()
    {
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    static int ReserveTcpPort()
    {
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    static int ReserveDnsPort()
    {
        using Socket tcp = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        tcp.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)tcp.LocalEndPoint!).Port;
        using Socket udp = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Loopback, port));
        return port;
    }

    static Socket ConnectTcp(DnsEndpoint endpoint)
    {
        IPEndPoint remote = new IPEndPoint(
            new IPAddress(endpoint.Address), endpoint.Port);
        int attempt = 0;
        while (attempt < 50)
        {
            Socket socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            try
            {
                socket.Connect(remote);
                socket.ReceiveTimeout = 3000;
                socket.SendTimeout = 3000;
                return socket;
            }
            catch (SocketException)
            {
                socket.Dispose();
                Thread.Sleep(20);
            }

            attempt = attempt + 1;
        }

        Assert.Fail("The TCP server did not accept a connection.");
        return new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
    }

    static byte[] ReadFrame(Socket socket)
    {
        byte[] prefix = ReceiveExact(socket, 2);
        int length = (prefix[0] << 8) | prefix[1];
        return ReceiveExact(socket, length);
    }

    static byte[] ReceiveExact(Socket socket, int length)
    {
        byte[] result = new byte[length];
        int received = 0;
        while (received < length)
        {
            int count = socket.Receive(
                result,
                received,
                length - received,
                SocketFlags.None);
            Assert.That(count, Is.GreaterThan(0));
            received = received + count;
        }

        return result;
    }

    static byte[] Frame(byte[] message)
    {
        byte[] result = new byte[message.Length + 2];
        result[0] = (byte)(message.Length >> 8);
        result[1] = (byte)(message.Length & 255);
        Array.Copy(message, 0, result, 2, message.Length);
        return result;
    }

    static void SendAll(Socket socket, byte[] bytes)
    {
        int sent = 0;
        while (sent < bytes.Length)
        {
            sent = sent + socket.Send(
                bytes,
                sent,
                bytes.Length - sent,
                SocketFlags.None);
        }
    }

    static void SendFragmented(Socket socket, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            Assert.That(
                socket.Send(bytes, index, 1, SocketFlags.None),
                Is.EqualTo(1));
            index = index + 1;
        }
    }

    static bool WaitForClosure(Socket socket, int timeoutMilliseconds)
    {
        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            if (socket.Poll(10000, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[1];
                return socket.Receive(buffer, SocketFlags.None) == 0;
            }

            Thread.Sleep(5);
        }

        return false;
    }

    static List<byte[]> ReadFramesUntilClosure(Socket socket)
    {
        List<byte[]> result = new List<byte[]>();
        int frameCount = 0;
        while (frameCount < 100)
        {
            Assert.That(
                socket.Poll(3000000, SelectMode.SelectRead),
                Is.True);
            byte[] prefix = new byte[2];
            int first = socket.Receive(prefix, 0, 2, SocketFlags.None);
            if (first == 0)
            {
                return result;
            }

            int prefixLength = first;
            while (prefixLength < 2)
            {
                prefixLength = prefixLength + socket.Receive(
                    prefix,
                    prefixLength,
                    2 - prefixLength,
                    SocketFlags.None);
            }

            int messageLength = (prefix[0] << 8) | prefix[1];
            result.Add(ReceiveExact(socket, messageLength));
            frameCount = frameCount + 1;
        }

        Assert.Fail("The TCP server did not terminate its response stream.");
        return result;
    }

    static List<DnsMessage> DecodeFrames(List<byte[]> frames)
    {
        List<DnsMessage> result = new List<DnsMessage>();
        int index = 0;
        while (index < frames.Count)
        {
            DnsDecodeResult decoded = Decode(frames[index]);
            Assert.That(decoded.Success, Is.True);
            result.Add(decoded.Message!);
            index = index + 1;
        }

        return result;
    }

    static List<DnsResourceRecord> Answers(List<DnsMessage> messages)
    {
        List<DnsResourceRecord> result = new List<DnsResourceRecord>();
        int messageIndex = 0;
        while (messageIndex < messages.Count)
        {
            int answerIndex = 0;
            while (answerIndex < messages[messageIndex].Answers.Count)
            {
                result.Add(messages[messageIndex].Answers[answerIndex]);
                answerIndex = answerIndex + 1;
            }

            messageIndex = messageIndex + 1;
        }

        return result;
    }

    static byte[] Exchange(DnsEndpoint endpoint, byte[] request)
    {
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        IPEndPoint remote = new IPEndPoint(
            new IPAddress(endpoint.Address), endpoint.Port);
        byte[] buffer = new byte[DnsProtocolLimits.MaximumTcpMessageLength];
        EndPoint source = new IPEndPoint(IPAddress.Any, 0);
        int attempt = 0;
        while (attempt < 30)
        {
            socket.SendTo(request, remote);
            if (socket.Poll(100000, SelectMode.SelectRead))
            {
                int length = socket.ReceiveFrom(buffer, ref source);
                byte[] response = new byte[length];
                Array.Copy(buffer, response, length);
                return response;
            }

            attempt = attempt + 1;
        }

        Assert.Fail("The UDP server did not return a response.");
        return [];
    }

    static void SendWithoutResponse(DnsEndpoint endpoint, byte[] request)
    {
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.SendTo(
            request,
            new IPEndPoint(new IPAddress(endpoint.Address), endpoint.Port));
        Thread.Sleep(50);
    }

    static DnsDecodeResult Decode(byte[] bytes)
    {
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.MaximumMessageLength = bytes.Length;
        return new DnsMessageCodec().TryDecodeMessage(bytes, options);
    }

    static byte[] Encode(DnsMessage message)
    {
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        return encoded.Bytes;
    }

    static DnsMessage Query(ushort identifier, string name)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = identifier;
        result.Header.Flags = DnsHeaderFlag.RecursionDesired;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = (int)DnsRecordType.A;
        question.Class = DnsClass.Internet;
        question.ClassValue = (int)DnsClass.Internet;
        result.Questions.Add(question);
        return result;
    }

    static DnsMessage UpdateMessage(ushort identifier, string owner)
    {
        DnsUpdateRequest request = new DnsUpdateRequest();
        request.Zone = Parse("example.");
        DnsUpdateChange change = new DnsUpdateChange();
        change.Kind = DnsUpdateChangeKind.Add;
        change.Name = Parse(owner);
        change.RecordType = DnsRecordType.A;
        change.RecordTypeValue = (int)DnsRecordType.A;
        change.TimeToLive = 300;
        DnsResourceRecord record = Record(owner, DnsRecordType.A);
        record.Data = new DnsRecordData();
        record.Data.Address = new DnsAddressData();
        record.Data.Address.Address = [192, 0, 2, 90];
        change.Records.Add(record);
        request.Changes.Add(change);
        DnsUpdateMessageResult built =
            UpdateMessageOperations.BuildUpdateMessage(request, identifier);
        Assert.That(built.Success, Is.True);
        return built.Message!;
    }

    static DnsMessage NotifyMessage(ushort identifier, uint serial)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = identifier;
        result.Header.Opcode = DnsOpcode.Notify;
        result.Header.OpcodeValue = (int)DnsOpcode.Notify;
        result.Header.Flags = DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)DnsHeaderFlag.AuthoritativeAnswer;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("example.");
        question.RecordType = DnsRecordType.Soa;
        question.RecordTypeValue = (int)DnsRecordType.Soa;
        question.Class = DnsClass.Internet;
        question.ClassValue = (int)DnsClass.Internet;
        result.Questions.Add(question);
        result.Answers.Add(SoaRecord(serial));
        result.Header.QuestionCount = 1;
        result.Header.AnswerCount = 1;
        return result;
    }

    static DnsMessage TransferQuery(
        ushort identifier,
        DnsRecordType type)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = identifier;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("example.");
        question.RecordType = type;
        question.RecordTypeValue = (int)type;
        question.Class = DnsClass.Internet;
        question.ClassValue = (int)DnsClass.Internet;
        result.Questions.Add(question);
        result.Header.QuestionCount = 1;
        return result;
    }

    static DnsMessage IxfrQuery(ushort identifier, uint serial)
    {
        DnsMessage result = TransferQuery(identifier, DnsRecordType.Ixfr);
        result.Authorities.Add(SoaRecord(serial));
        result.Header.AuthorityCount = 1;
        return result;
    }

    static DnsTsigKey ServerKey()
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Parse("server-key.");
        result.AlgorithmName = Parse("hmac-sha256.");
        result.Secret = System.Text.Encoding.ASCII.GetBytes(
            "dns-server-test-secret");
        return result;
    }

    static MemoryDnsZoneStore StoreWithAddressRecords(int addressCount)
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsZone zone = new DnsZone();
        zone.Apex = Parse("example.");
        zone.Class = DnsClass.Internet;
        zone.ClassValue = (int)DnsClass.Internet;
        DnsZoneNode apex = new DnsZoneNode();
        apex.Name = Parse("example.");
        apex.RecordSets.Add(SoaSet());
        apex.RecordSets.Add(NameServerSet());
        zone.Nodes.Add(apex);
        DnsZoneNode host = new DnsZoneNode();
        host.Name = Parse("www.example.");
        host.RecordSets.Add(AddressSet(addressCount));
        zone.Nodes.Add(host);
        Assert.That(store.AddZone(zone).Valid, Is.True);
        return store;
    }

    static DnsResourceRecordSet SoaSet()
    {
        DnsResourceRecordSet result = Set(
            "example.", DnsRecordType.Soa);
        DnsResourceRecord record = Record(
            "example.", DnsRecordType.Soa);
        record.Data = new DnsRecordData();
        record.Data.Soa = new DnsSoaData();
        record.Data.Soa.PrimaryNameServer = Parse("ns.example.");
        record.Data.Soa.ResponsibleMailbox = Parse("hostmaster.example.");
        record.Data.Soa.Serial = 1;
        record.Data.Soa.Refresh = 3600;
        record.Data.Soa.Retry = 600;
        record.Data.Soa.Expire = 86400;
        record.Data.Soa.Minimum = 300;
        result.Records.Add(record);
        return result;
    }

    static DnsResourceRecord SoaRecord(uint serial)
    {
        DnsResourceRecord result = Record(
            "example.", DnsRecordType.Soa);
        result.Data = new DnsRecordData();
        result.Data.Soa = new DnsSoaData();
        result.Data.Soa.PrimaryNameServer = Parse("ns.example.");
        result.Data.Soa.ResponsibleMailbox = Parse("hostmaster.example.");
        result.Data.Soa.Serial = serial;
        result.Data.Soa.Refresh = 3600;
        result.Data.Soa.Retry = 600;
        result.Data.Soa.Expire = 86400;
        result.Data.Soa.Minimum = 300;
        return result;
    }

    static DnsResourceRecordSet NameServerSet()
    {
        DnsResourceRecordSet result = Set(
            "example.", DnsRecordType.Ns);
        DnsResourceRecord record = Record(
            "example.", DnsRecordType.Ns);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = Parse("ns.example.");
        result.Records.Add(record);
        return result;
    }

    static DnsResourceRecordSet AddressSet(int count)
    {
        DnsResourceRecordSet result = Set(
            "www.example.", DnsRecordType.A);
        int index = 0;
        while (index < count)
        {
            DnsResourceRecord record = Record(
                "www.example.", DnsRecordType.A);
            record.Data = new DnsRecordData();
            record.Data.Address = new DnsAddressData();
            record.Data.Address.Address =
                [192, 0, 2, (byte)(index + 1)];
            result.Records.Add(record);
            index = index + 1;
        }

        return result;
    }

    static DnsResourceRecordSet Set(
        string name,
        DnsRecordType type)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = Parse(name);
        result.RecordType = type;
        result.RecordTypeValue = (int)type;
        result.Class = DnsClass.Internet;
        result.ClassValue = (int)DnsClass.Internet;
        result.TimeToLive = 300;
        return result;
    }

    static DnsResourceRecord Record(
        string name,
        DnsRecordType type)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(name);
        result.RecordType = type;
        result.RecordTypeValue = (int)type;
        result.Class = DnsClass.Internet;
        result.ClassValue = (int)DnsClass.Internet;
        result.TimeToLive = 300;
        return result;
    }

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }
}
