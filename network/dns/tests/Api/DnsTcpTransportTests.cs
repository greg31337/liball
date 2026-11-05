using System.Net;
using System.Net.Sockets;

namespace dns.Tests.Api;

public class DnsTcpTransportTests
{
    [Test]
    public void ExchangesFragmentedLengthPrefixedResponse()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x1101, "fragmented.example.");
        byte[] responseBytes = Encode(Response(query));
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            Assert.That(ReadFrame(connection), Is.EqualTo(Encode(query)));
            SendFrameFragmented(connection, responseBytes);
        });
        DnsTransportRequest request = Request(listener, query);
        request.QueryMessage = null;
        request.Options.ReuseConnections = false;

        DnsTransportResult result = new DnsTcpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(result.ResponseBytes, Is.EqualTo(responseBytes));
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x1101));
            Assert.That(result.Endpoint.AddressFamily, Is.EqualTo(4));
            Assert.That(result.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void ReusesOneConnectionForSequentialQueries()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] firstQuery = ReadFrame(connection);
            SendFrame(connection, ResponseBytes(firstQuery));
            byte[] secondQuery = ReadFrame(connection);
            SendFrame(connection, ResponseBytes(secondQuery));
        });
        DnsTcpTransport transport = new DnsTcpTransport();
        DnsMessage first = Query(0x1201, "first.example.");
        DnsMessage second = Query(0x1202, "second.example.");

        DnsTransportResult firstResult = transport.Exchange(
            Request(listener, first));
        DnsTransportResult secondResult = transport.Exchange(
            Request(listener, second));
        serverTask.GetAwaiter().GetResult();
        transport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(secondResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(firstResult.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x1201));
            Assert.That(secondResult.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x1202));
        });
    }

    [Test]
    public void IdleAndLifetimeExpiryRetireReusableConnections()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        List<Socket> accepted = new List<Socket>();
        Task serverTask = Task.Run(() =>
        {
            int index = 0;
            while (index < 3)
            {
                Socket connection = listener.Accept();
                accepted.Add(connection);
                byte[] queryBytes = ReadFrame(connection);
                SendFrame(connection, ResponseBytes(queryBytes));
                index++;
            }
        });
        DnsTcpTransport transport = new DnsTcpTransport();
        DnsTransportRequest first = Request(
            listener, Query(0x1301, "one.example."));
        first.Options.IdleConnectionTimeoutMilliseconds = 100;
        first.Options.ConnectionLifetimeMilliseconds = 1000;

        DnsTransportResult firstResult = transport.Exchange(first);
        transport.ConnectionLastUsedAt.MonotonicMilliseconds -= 101;
        DnsTransportRequest second = Request(
            listener, Query(0x1302, "two.example."));
        second.Options.IdleConnectionTimeoutMilliseconds = 100;
        second.Options.ConnectionLifetimeMilliseconds = 1000;
        DnsTransportResult secondResult = transport.Exchange(second);
        transport.ConnectionCreatedAt.MonotonicMilliseconds -= 1001;
        DnsTransportRequest third = Request(
            listener, Query(0x1303, "three.example."));
        third.Options.IdleConnectionTimeoutMilliseconds = 100;
        third.Options.ConnectionLifetimeMilliseconds = 1000;
        DnsTransportResult thirdResult = transport.Exchange(third);

        serverTask.GetAwaiter().GetResult();
        transport.CloseTransport();
        foreach (Socket connection in accepted)
        {
            connection.Dispose();
        }

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(secondResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(thirdResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(accepted, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void DisabledReuseCreatesAConnectionForEveryExchange()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        int acceptedCount = 0;
        Task serverTask = Task.Run(() =>
        {
            int index = 0;
            while (index < 2)
            {
                using Socket connection = listener.Accept();
                acceptedCount++;
                byte[] queryBytes = ReadFrame(connection);
                SendFrame(connection, ResponseBytes(queryBytes));
                index++;
            }
        });
        DnsTcpTransport transport = new DnsTcpTransport();
        DnsTransportRequest first = Request(
            listener, Query(0x1401, "one.example."));
        first.Options.ReuseConnections = false;
        DnsTransportRequest second = Request(
            listener, Query(0x1402, "two.example."));
        second.Options.ReuseConnections = false;

        DnsTransportResult firstResult = transport.Exchange(first);
        DnsTransportResult secondResult = transport.Exchange(second);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(secondResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(acceptedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReportsDeclaredResponseAboveConfiguredLimit()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x1501, "large.example.");
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] ignored = ReadFrame(connection);
            connection.Send([0, 100]);
        });
        DnsTransportRequest request = Request(listener, query);
        request.Options.MaximumResponseLength = 50;

        DnsTransportResult result = new DnsTcpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(result.ResponseBytes, Is.Empty);
            Assert.That(result.ResponseMessage, Is.Null);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void PartialPayloadClosureIsAFrameProtocolError()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x1601, "partial.example.");
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] ignored = ReadFrame(connection);
            connection.Send([0, 12, 0x16, 0x01, 0x80]);
            connection.Shutdown(SocketShutdown.Send);
        });

        DnsTransportResult result = new DnsTcpTransport().Exchange(
            Request(listener, query));
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.ResponseBytes,
                Is.EqualTo(new byte[] { 0x16, 0x01, 0x80 }));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
        });
    }

    [Test]
    public void ZeroLengthAndPartialPrefixFramesAreProtocolErrors()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        Task serverTask = Task.Run(() =>
        {
            using (Socket zeroLength = listener.Accept())
            {
                byte[] ignored = ReadFrame(zeroLength);
                zeroLength.Send([0, 0]);
            }

            using (Socket partialPrefix = listener.Accept())
            {
                byte[] ignored = ReadFrame(partialPrefix);
                partialPrefix.Send([0]);
                partialPrefix.Shutdown(SocketShutdown.Send);
            }
        });
        DnsTcpTransport transport = new DnsTcpTransport();

        DnsTransportResult zeroLengthResult = transport.Exchange(
            Request(listener, Query(0x1651, "zero.example.")));
        DnsTransportResult partialPrefixResult = transport.Exchange(
            Request(listener, Query(0x1652, "prefix.example.")));
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(zeroLengthResult.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(zeroLengthResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
            Assert.That(zeroLengthResult.Diagnostics[0].Field,
                Is.EqualTo("tcpFrame.messageLength"));
            Assert.That(partialPrefixResult.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(partialPrefixResult.ResponseBytes, Is.Empty);
            Assert.That(partialPrefixResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
            Assert.That(partialPrefixResult.Diagnostics[0].Reason,
                Does.Contain("length prefix"));
            Assert.That(transport.Connection, Is.Null);
        });
    }

    [Test]
    public void TimesOutWhileWaitingForDeclaredPayload()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x1701, "timeout.example.");
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] ignored = ReadFrame(connection);
            connection.Send([0, 12]);
            Thread.Sleep(150);
        });
        DnsTransportRequest request = Request(listener, query);
        request.Options.AttemptTimeoutMilliseconds = 40;

        DnsTransportResult result = new DnsTcpTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.Cancelled, Is.False);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void ObservesCancellationWhileReceivingFramePayload()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        using ManualResetEventSlim prefixSent = new ManualResetEventSlim();
        using ManualResetEventSlim releaseServer = new ManualResetEventSlim();
        DnsMessage query = Query(0x1751, "cancel.example.");
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] ignored = ReadFrame(connection);
            connection.Send([0, 12]);
            prefixSent.Set();
            releaseServer.Wait(1000);
        });
        DnsTransportRequest request = Request(listener, query);
        request.Options.AttemptTimeoutMilliseconds = 2000;
        Task<DnsTransportResult> exchangeTask = Task.Run(
            () => new DnsTcpTransport().Exchange(request));
        Assert.That(prefixSent.Wait(1000), Is.True);

        request.CancellationRequested = true;
        DnsTransportResult result = exchangeTask.GetAwaiter().GetResult();
        releaseServer.Set();
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
    public void RejectsMismatchedResponseAndRetiresConnection()
    {
        using Socket listener = BindListener(AddressFamily.InterNetwork);
        DnsMessage query = Query(0x1801, "match.example.");
        DnsMessage mismatch = Response(query);
        mismatch.Header.Identifier = 0x1802;
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] ignored = ReadFrame(connection);
            SendFrame(connection, Encode(mismatch));
        });
        DnsTcpTransport transport = new DnsTcpTransport();

        DnsTransportResult result = transport.Exchange(
            Request(listener, query));
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResponseMismatch));
            Assert.That(transport.Connection, Is.Null);
        });
    }

    [Test]
    public void ExchangesOverIpv6AndHonorsClosedState()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not supported on this platform.");
        }

        using Socket listener = BindListener(AddressFamily.InterNetworkV6);
        DnsMessage query = Query(0x1901, "v6.example.");
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] queryBytes = ReadFrame(connection);
            SendFrame(connection, ResponseBytes(queryBytes));
        });
        DnsTcpTransport transport = new DnsTcpTransport();

        DnsTransportResult success = transport.Exchange(
            Request(listener, query));
        serverTask.GetAwaiter().GetResult();
        transport.CloseTransport();
        DnsTransportResult closed = transport.Exchange(
            Request(listener, query));

        Assert.Multiple(() =>
        {
            Assert.That(success.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(success.Endpoint.AddressFamily, Is.EqualTo(6));
            Assert.That(closed.Status, Is.EqualTo(DnsTransportStatus.Closed));
        });
    }

    static Socket BindListener(AddressFamily addressFamily)
    {
        Socket listener = new Socket(
            addressFamily, SocketType.Stream, ProtocolType.Tcp);
        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            listener.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
        }
        else
        {
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        }

        listener.Listen(8);
        return listener;
    }

    static DnsTransportRequest Request(Socket listener, DnsMessage query)
    {
        IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint.HasAddress = true;
        request.Endpoint.Address = endpoint.Address.GetAddressBytes();
        request.Endpoint.AddressFamily =
            endpoint.AddressFamily == AddressFamily.InterNetwork ? 4 : 6;
        request.Endpoint.Port = endpoint.Port;
        request.Options.Kind = DnsTransportKind.Tcp;
        request.Options.AttemptTimeoutMilliseconds = 1000;
        request.Options.MaximumResponseLength = 4096;
        request.Options.ReuseConnections = true;
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        return request;
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
        byte[] frame = Frame(message);
        int sent = 0;
        while (sent < frame.Length)
        {
            sent += connection.Send(
                frame, sent, frame.Length - sent, SocketFlags.None);
        }
    }

    static void SendFrameFragmented(Socket connection, byte[] message)
    {
        byte[] frame = Frame(message);
        int index = 0;
        while (index < frame.Length)
        {
            connection.Send(frame, index, 1, SocketFlags.None);
            Thread.Sleep(1);
            index++;
        }
    }

    static byte[] Frame(byte[] message)
    {
        byte[] frame = new byte[message.Length + 2];
        frame[0] = (byte)(message.Length >> 8);
        frame[1] = (byte)(message.Length & 255);
        Array.Copy(message, 0, frame, 2, message.Length);
        return frame;
    }

    static byte[] ResponseBytes(byte[] queryBytes)
    {
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            queryBytes, new DnsDecodeOptions());
        Assert.That(decoded.Success, Is.True);
        return Encode(Response(decoded.Message!));
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
}
