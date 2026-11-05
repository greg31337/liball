using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;

namespace dns.Tests.Api;

public class TransportLifecycleTests
{
    [Test]
    public void CloseIsIdempotentAndPreventsEveryTransportFromRestarting()
    {
        IDnsTransport[] transports =
        [
            new DnsUdpTransport(),
            new DnsTcpTransport(),
            new DnsTlsTransport(),
            new DnsHttpsTransport(),
            new DnsQuicTransport()
        ];
        DnsTransportKind[] kinds =
        [
            DnsTransportKind.Udp,
            DnsTransportKind.Tcp,
            DnsTransportKind.Tls,
            DnsTransportKind.Https,
            DnsTransportKind.Quic
        ];
        List<DnsTransportStatus> statuses =
            new List<DnsTransportStatus>();
        int index = 0;
        while (index < transports.Length)
        {
            transports[index].CloseTransport();
            transports[index].CloseTransport();
            statuses.Add(transports[index].Exchange(
                Request(kinds[index], 0x6401))
                .Status);
            index++;
        }

        Assert.That(statuses, Is.All.EqualTo(DnsTransportStatus.Closed));
    }

    [Test]
    public void SerializesConcurrentTcpExchangesOnReusableConnection()
    {
        using Socket listener = BindListener();
        using ManualResetEventSlim firstReceived =
            new ManualResetEventSlim();
        using ManualResetEventSlim releaseFirst =
            new ManualResetEventSlim();
        using ManualResetEventSlim secondReceived =
            new ManualResetEventSlim();
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            byte[] first = ReadFrame(connection);
            firstReceived.Set();
            Assert.That(releaseFirst.Wait(2000), Is.True);
            SendFrame(connection, ResponseBytes(first));
            byte[] second = ReadFrame(connection);
            secondReceived.Set();
            SendFrame(connection, ResponseBytes(second));
        });
        DnsTcpTransport transport = new DnsTcpTransport();
        DnsTransportRequest firstRequest = TcpRequest(
            listener, Query(0x6402, "first.example."));
        DnsTransportRequest secondRequest = TcpRequest(
            listener, Query(0x6403, "second.example."));
        Task<DnsTransportResult> firstExchange = Task.Run(
            () => transport.Exchange(firstRequest));
        Assert.That(firstReceived.Wait(1000), Is.True);

        Task<DnsTransportResult> secondExchange = Task.Run(
            () => transport.Exchange(secondRequest));
        Assert.That(secondReceived.Wait(100), Is.False);
        releaseFirst.Set();
        DnsTransportResult firstResult =
            firstExchange.GetAwaiter().GetResult();
        DnsTransportResult secondResult =
            secondExchange.GetAwaiter().GetResult();
        serverTask.GetAwaiter().GetResult();
        transport.CloseTransport();
        transport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(secondResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(secondReceived.IsSet, Is.True);
            Assert.That(transport.Connection, Is.Null);
            Assert.That(transport.ConnectionEndpoint, Is.Null);
        });
    }

    [Test]
    public void CloseWaitsForActiveHttpsExchangeThenStopsQueuedWork()
    {
        BlockingHandler handler = new BlockingHandler();
        DnsHttpsTransport transport = new DnsHttpsTransport();
        transport.MessageHandler = handler;
        DnsTransportRequest activeRequest = Request(
            DnsTransportKind.Https, 0x6404);
        Task<DnsTransportResult> exchangeTask = Task.Run(
            () => transport.Exchange(activeRequest));
        Assert.That(handler.Started.Wait(1000), Is.True);

        Task closeTask = Task.Run(() => transport.CloseTransport());
        Assert.That(closeTask.Wait(100), Is.False);
        handler.Complete(Encode(Response(activeRequest.QueryMessage!)));
        DnsTransportResult activeResult =
            exchangeTask.GetAwaiter().GetResult();
        Assert.That(closeTask.Wait(1000), Is.True);
        DnsTransportResult stopped = transport.Exchange(Request(
            DnsTransportKind.Https, 0x6405));

        Assert.Multiple(() =>
        {
            Assert.That(activeResult.Status,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(stopped.Status,
                Is.EqualTo(DnsTransportStatus.Closed));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void TlsCloseOwnsAndReleasesRetainedStreamAndSocket()
    {
        TrackingStream innerStream = new TrackingStream();
        DnsTlsTransport transport = new DnsTlsTransport();
        transport.Stream = new SslStream(innerStream, false);
        transport.Connection = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        transport.ConnectionEndpoint = new IPEndPoint(
            IPAddress.Loopback, DnsPorts.DnsOverTls);
        transport.ConnectionAuthenticationName = "resolver.test";

        transport.CloseTransport();
        transport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(innerStream.Disposed, Is.True);
            Assert.That(transport.Stream, Is.Null);
            Assert.That(transport.Connection, Is.Null);
            Assert.That(transport.ConnectionEndpoint, Is.Null);
            Assert.That(transport.ConnectionAuthenticationName, Is.Null);
        });
    }

    static DnsTransportRequest Request(
        DnsTransportKind kind,
        ushort identifier)
    {
        DnsMessage query = Query(identifier, "lifecycle.example.");
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint.HasAddress = true;
        request.Endpoint.Address = IPAddress.Loopback.GetAddressBytes();
        request.Endpoint.AddressFamily = 4;
        request.Options.Kind = kind;
        request.Options.AttemptTimeoutMilliseconds = 1000;
        request.Options.MaximumResponseLength = 4096;
        request.Options.Https.Uri =
            "https://resolver.test/dns-query";
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        return request;
    }

    static Socket BindListener()
    {
        Socket listener = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(4);
        return listener;
    }

    static DnsTransportRequest TcpRequest(
        Socket listener,
        DnsMessage query)
    {
        IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;
        DnsTransportRequest request = Request(
            DnsTransportKind.Tcp, query.Header.Identifier);
        request.Endpoint.Address = endpoint.Address.GetAddressBytes();
        request.Endpoint.Port = endpoint.Port;
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        request.Options.ReuseConnections = true;
        request.Options.IdleConnectionTimeoutMilliseconds = 1000;
        request.Options.ConnectionLifetimeMilliseconds = 5000;
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
                throw new InvalidOperationException(
                    "The lifecycle test connection closed early.");
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
        question.Name = new DnsNameService().ParseName(name, null);
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

    sealed class BlockingHandler : HttpMessageHandler
    {
        internal ManualResetEventSlim Started = new ManualResetEventSlim();
        internal TaskCompletionSource<HttpResponseMessage> Response =
            new TaskCompletionSource<HttpResponseMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        internal int RequestCount;

        internal void Complete(byte[] bytes)
        {
            HttpResponseMessage response = new HttpResponseMessage(
                HttpStatusCode.OK);
            response.Content = new ByteArrayContent(bytes);
            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/dns-message");
            this.Response.SetResult(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.RequestCount++;
            this.Started.Set();
            return this.Response.Task;
        }
    }

    sealed class TrackingStream : Stream
    {
        internal bool Disposed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            this.Disposed = true;
            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
