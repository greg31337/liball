using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace dns.Tests.Api;

public class DnsQuicTransportTests
{
    [Test]
    public void FramesQueryAndReadsFragmentedFinTerminatedResponse()
    {
        DnsTransportRequest request = Request(
            Query(0x6301, "frame.example."));
        byte[] responseBytes = Encode(Response(request.QueryMessage!));
        byte[] responseFrame = QuicFrameOperations.CreateFrame(responseBytes);
        using MemoryStream written = new MemoryStream();
        DnsClockValue deadline = TransportTime.CreateDeadline(
            1000, false, new DnsClockValue());

        QuicFrameResult write = QuicFrameOperations.WriteQuery(
            written,
            request.QueryBytes,
            request,
            deadline,
            CancellationToken.None);
        using ChunkedReadStream responseStream = new ChunkedReadStream(
            responseFrame, 1);
        QuicFrameResult read = QuicFrameOperations.ReadResponse(
            responseStream,
            4096,
            request,
            deadline,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(write.Success, Is.True);
            Assert.That(written.ToArray(),
                Is.EqualTo(QuicFrameOperations.CreateFrame(
                    request.QueryBytes)));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Bytes, Is.EqualTo(responseBytes));
        });
    }

    [Test]
    public void RejectsZeroLengthPartialOversizedAndTrailingFrames()
    {
        DnsTransportRequest request = Request(
            Query(0x6302, "invalid.example."));
        DnsClockValue deadline = TransportTime.CreateDeadline(
            1000, false, new DnsClockValue());
        QuicFrameResult zero = ReadFrame(
            [0, 0], 64, request, deadline);
        QuicFrameResult partial = ReadFrame(
            [0, 4, 1, 2], 64, request, deadline);
        QuicFrameResult oversized = ReadFrame(
            [0, 65], 64, request, deadline);
        QuicFrameResult trailing = ReadFrame(
            [0, 2, 1, 2, 3], 64, request, deadline);

        Assert.Multiple(() =>
        {
            Assert.That(zero.ProtocolError, Is.True);
            Assert.That(partial.ProtocolError, Is.True);
            Assert.That(partial.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(oversized.TooLarge, Is.True);
            Assert.That(trailing.ProtocolError, Is.True);
            Assert.That(trailing.DiagnosticReason,
                Does.Contain("after its declared message"));
        });
    }

    [Test]
    public void BoundsPendingFrameReadByDeadlineAndLiveCancellation()
    {
        DnsTransportRequest timeoutRequest = Request(
            Query(0x6303, "timeout.example."));
        using PendingReadStream timeoutStream = new PendingReadStream();
        QuicFrameResult timeout = QuicFrameOperations.ReadResponse(
            timeoutStream,
            64,
            timeoutRequest,
            TransportTime.CreateDeadline(20, false, new DnsClockValue()),
            CancellationToken.None);

        DnsTransportRequest cancelledRequest = Request(
            Query(0x6304, "cancel.example."));
        cancelledRequest.CancellationRequested = true;
        using PendingReadStream cancelledStream = new PendingReadStream();
        QuicFrameResult cancelled = QuicFrameOperations.ReadResponse(
            cancelledStream,
            64,
            cancelledRequest,
            TransportTime.CreateDeadline(1000, false, new DnsClockValue()),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(timeout.TimedOut, Is.True);
            Assert.That(timeout.Cancelled, Is.False);
            Assert.That(cancelled.Cancelled, Is.True);
            Assert.That(cancelled.TimedOut, Is.False);
        });
    }

    [Test]
    public void AppliesDefaultPortAndAuthenticationNamePrecedence()
    {
        DnsEndpoint standard = new DnsEndpoint();
        standard.Host = "endpoint.test";
        DnsEndpoint custom = new DnsEndpoint();
        custom.Host = "endpoint.test";
        custom.Port = 8853;
        DnsTransportRequest configured = Request(
            Query(0x6305, "name.example."));
        configured.Endpoint.Host = "endpoint.test";
        configured.Options.Quic.AuthenticationName = "configured.test";
        DnsTransportRequest host = Request(
            Query(0x6306, "host.example."));
        host.Endpoint.Host = "endpoint.test";
        DnsTransportRequest numeric = Request(
            Query(0x6307, "numeric.example."));
        IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 853);

        Assert.Multiple(() =>
        {
            Assert.That(QuicTransportOperations.ApplyDefaultPort(standard).Port,
                Is.EqualTo(DnsPorts.DnsOverQuic));
            Assert.That(QuicTransportOperations.ApplyDefaultPort(custom).Port,
                Is.EqualTo(8853));
            Assert.That(QuicTransportOperations.ReadAuthenticationName(
                configured, remote), Is.EqualTo("configured.test"));
            Assert.That(QuicTransportOperations.ReadAuthenticationName(
                host, remote), Is.EqualTo("endpoint.test"));
            Assert.That(QuicTransportOperations.ReadAuthenticationName(
                numeric, remote), Is.EqualTo("127.0.0.1"));
        });
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    public async Task ExchangesOneQueryOnABidirectionalLoopbackStreamWhenSupported()
    {
        if (!QuicConnection.IsSupported)
        {
            Assert.Ignore("The current platform has no QUIC implementation.");
        }

        using X509Certificate2 certificate = CreateServerCertificate();
        List<SslApplicationProtocol> protocols =
            new List<SslApplicationProtocol>();
        protocols.Add(new SslApplicationProtocol("doq"));
        QuicListenerOptions listenerOptions = new QuicListenerOptions();
        listenerOptions.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
        listenerOptions.ApplicationProtocols = protocols;
        listenerOptions.ConnectionOptionsCallback =
            (connection, clientHello, cancellationToken) =>
            {
                SslServerAuthenticationOptions authentication =
                    new SslServerAuthenticationOptions();
                authentication.ApplicationProtocols = protocols;
                authentication.ServerCertificate = certificate;
                QuicServerConnectionOptions options =
                    new QuicServerConnectionOptions();
                options.DefaultCloseErrorCode = 0;
                options.DefaultStreamErrorCode = 1;
                options.ServerAuthenticationOptions = authentication;
                return ValueTask.FromResult(options);
            };
        await using QuicListener listener = await QuicListener.ListenAsync(
            listenerOptions);
        IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint;
        DnsMessage query = Query(0x6314, "loopback.example.");
        DnsTransportRequest request = Request(query);
        request.Endpoint.Port = endpoint.Port;
        request.Options.Quic.AuthenticationName = "resolver.test";
        request.Options.Quic.ValidateCertificate = false;
        request.Options.AttemptTimeoutMilliseconds = 3000;
        Task serverTask = Task.Run(
            () => ServeOneQuicExchange(listener, request, query));

        DnsTransportResult result = new DnsQuicTransport().Exchange(request);
        await serverTask;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.ResponseMessage, Is.Not.Null);
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x6314));
            Assert.That(result.ResponseBytes,
                Is.EqualTo(Encode(Response(query))));
        });
    }

    [Test]
    public void ExchangesThroughProviderAndCorrelatesDnsResponse()
    {
        DnsMessage query = Query(0x6308, "provider.example.");
        FakeProvider provider = new FakeProvider();
        provider.Result.Status = DnsTransportStatus.Success;
        provider.Result.ResponseBytes = Encode(Response(query));
        DnsQuicTransport transport = new DnsQuicTransport();
        transport.ExchangeProvider = provider;
        DnsTransportRequest request = Request(query);
        request.QueryMessage = null;
        request.Options.Quic.AuthenticationName = "resolver.test";

        DnsTransportResult result = transport.Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Quic));
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x6308));
            Assert.That(result.Endpoint.Port,
                Is.EqualTo(DnsPorts.DnsOverQuic));
            Assert.That(provider.AuthenticationName,
                Is.EqualTo("resolver.test"));
            Assert.That(provider.Endpoint!.Port,
                Is.EqualTo(DnsPorts.DnsOverQuic));
            Assert.That(provider.ExchangeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void MapsProviderLimitsProtocolErrorsAndResponseMismatch()
    {
        DnsMessage query = Query(0x6309, "errors.example.");
        FakeProvider largeProvider = new FakeProvider();
        largeProvider.Result.Status = DnsTransportStatus.ResponseTooLarge;
        DnsTransportResult large = Exchange(largeProvider, query);

        FakeProvider protocolProvider = new FakeProvider();
        protocolProvider.Result.Status = DnsTransportStatus.ProtocolError;
        protocolProvider.Result.DiagnosticReason = "The peer reset the stream.";
        DnsTransportResult protocol = Exchange(protocolProvider, query);

        DnsMessage mismatchMessage = Response(query);
        mismatchMessage.Header.Identifier = 0x6310;
        FakeProvider mismatchProvider = new FakeProvider();
        mismatchProvider.Result.Status = DnsTransportStatus.Success;
        mismatchProvider.Result.ResponseBytes = Encode(mismatchMessage);
        DnsTransportResult mismatch = Exchange(mismatchProvider, query);

        Assert.Multiple(() =>
        {
            Assert.That(large.Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(large.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(protocol.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(protocol.Diagnostics[0].Reason,
                Is.EqualTo("The peer reset the stream."));
            Assert.That(mismatch.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(mismatch.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResponseMismatch));
        });
    }

    [Test]
    public void MapsProviderTimeoutCancellationAndMalformedDns()
    {
        DnsMessage query = Query(0x6315, "provider-outcomes.example.");
        FakeProvider timeoutProvider = new FakeProvider();
        timeoutProvider.Result.Status = DnsTransportStatus.TimedOut;
        timeoutProvider.Result.TimedOut = true;
        DnsTransportResult timeout = Exchange(timeoutProvider, query);

        FakeProvider cancellationProvider = new FakeProvider();
        cancellationProvider.Result.Status = DnsTransportStatus.Cancelled;
        cancellationProvider.Result.Cancelled = true;
        DnsTransportResult cancellation = Exchange(
            cancellationProvider, query);

        byte[] malformedBytes = [0, 1, 2, 3];
        FakeProvider malformedProvider = new FakeProvider();
        malformedProvider.Result.Status = DnsTransportStatus.Success;
        malformedProvider.Result.ResponseBytes = malformedBytes;
        DnsTransportResult malformed = Exchange(malformedProvider, query);

        Assert.Multiple(() =>
        {
            Assert.That(timeout.Status,
                Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(timeout.TimedOut, Is.True);
            Assert.That(timeout.Cancelled, Is.False);
            Assert.That(cancellation.Status,
                Is.EqualTo(DnsTransportStatus.Cancelled));
            Assert.That(cancellation.Cancelled, Is.True);
            Assert.That(cancellation.TimedOut, Is.False);
            Assert.That(malformed.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(malformed.ResponseBytes, Is.EqualTo(malformedBytes));
            Assert.That(malformed.ResponseMessage, Is.Null);
            Assert.That(malformed.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
        });
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    public void MapsQuicErrorsToTransportOutcomes()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() &&
            !OperatingSystem.IsWindows())
        {
            Assert.Ignore("The QUIC error API is unavailable on this platform.");
        }

        DnsTransportStatus timeout = QuicFailureOperations.MapQuicError(
            QuicError.ConnectionTimeout);
        DnsTransportStatus connectionAbort =
            QuicFailureOperations.MapQuicError(
                QuicError.ConnectionAborted);
        DnsTransportStatus streamAbort =
            QuicFailureOperations.MapQuicError(
                QuicError.StreamAborted);
        DnsTransportStatus refused = QuicFailureOperations.MapQuicError(
            QuicError.ConnectionRefused);
        Assert.Multiple(() =>
        {
            Assert.That(timeout, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(connectionAbort,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(streamAbort,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(refused,
                Is.EqualTo(DnsTransportStatus.NetworkError));
        });
    }

    [Test]
    public void ReportsUnavailableProviderWithoutAttemptingExchange()
    {
        FakeProvider provider = new FakeProvider();
        provider.Supported = false;
        DnsQuicTransport transport = new DnsQuicTransport();
        transport.ExchangeProvider = provider;

        DnsTransportResult result = transport.Exchange(Request(
            Query(0x6311, "unsupported.example.")));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.Unsupported));
            Assert.That(provider.ExchangeCount, Is.Zero);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnsupportedVersion));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("transport.quic"));
        });
    }

    [Test]
    public void BuiltInProviderReportsActualPlatformAvailability()
    {
        BuiltInQuicExchangeProvider provider =
            new BuiltInQuicExchangeProvider();
        if (provider.IsSupported())
        {
            Assert.Ignore("The current platform has a QUIC client provider.");
        }

        DnsTransportResult result = new DnsQuicTransport().Exchange(Request(
            Query(0x6312, "platform.example.")));

        Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Unsupported));
    }

    [Test]
    public void QueryClientRoutesDirectQuicRequestsAndHonorsClosedState()
    {
        CapturingTransport capturing = new CapturingTransport();
        DnsQueryClient client = new DnsQueryClient();
        client.NextServerOffset = 0;
        client.QuicTransport = capturing;
        DnsQueryRequest queryRequest = new DnsQueryRequest();
        queryRequest.Question.Name = Parse("client.example.");
        queryRequest.Question.RecordType = DnsRecordType.A;
        queryRequest.Question.RecordTypeValue = 1;
        queryRequest.Question.Class = DnsClass.Internet;
        queryRequest.Question.ClassValue = 1;
        queryRequest.Servers.Add(NumericEndpoint());
        queryRequest.TransportOptions.Kind = DnsTransportKind.Quic;
        queryRequest.TransportOptions.MaximumAttempts = 1;

        DnsQueryResult queryResult = client.Query(queryRequest);
        DnsQuicTransport closedTransport = new DnsQuicTransport();
        closedTransport.CloseTransport();
        DnsTransportResult closed = closedTransport.Exchange(Request(
            Query(0x6313, "closed.example.")));

        Assert.Multiple(() =>
        {
            Assert.That(queryResult.Success, Is.True);
            Assert.That(capturing.Requests, Has.Count.EqualTo(1));
            Assert.That(capturing.Requests[0].Options.Kind,
                Is.EqualTo(DnsTransportKind.Quic));
            Assert.That(closed.Status, Is.EqualTo(DnsTransportStatus.Closed));
        });
    }

    static QuicFrameResult ReadFrame(
        byte[] bytes,
        int maximumLength,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        using ChunkedReadStream stream = new ChunkedReadStream(bytes, 1);
        return QuicFrameOperations.ReadResponse(
            stream,
            maximumLength,
            request,
            deadline,
            CancellationToken.None);
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    static async Task ServeOneQuicExchange(
        QuicListener listener,
        DnsTransportRequest request,
        DnsMessage query)
    {
        using CancellationTokenSource cancellation =
            new CancellationTokenSource(5000);
        await using QuicConnection connection =
            await listener.AcceptConnectionAsync(cancellation.Token);
        await using QuicStream stream =
            await connection.AcceptInboundStreamAsync(cancellation.Token);
        DnsClockValue deadline = TransportTime.CreateDeadline(
            3000, false, new DnsClockValue());
        QuicFrameResult received = QuicFrameOperations.ReadResponse(
            stream,
            4096,
            request,
            deadline,
            cancellation.Token);
        Assert.That(received.Success, Is.True);
        Assert.That(received.Bytes, Is.EqualTo(request.QueryBytes));
        byte[] responseFrame = QuicFrameOperations.CreateFrame(
            Encode(Response(query)));
        await stream.WriteAsync(responseFrame, cancellation.Token);
        stream.CompleteWrites();
    }

    static X509Certificate2 CreateServerCertificate()
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest(
            "CN=resolver.test",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature,
                false));
        SubjectAlternativeNameBuilder names =
            new SubjectAlternativeNameBuilder();
        names.AddDnsName("resolver.test");
        request.CertificateExtensions.Add(names.Build());
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    static DnsTransportResult Exchange(
        FakeProvider provider,
        DnsMessage query)
    {
        DnsQuicTransport transport = new DnsQuicTransport();
        transport.ExchangeProvider = provider;
        return transport.Exchange(Request(query));
    }

    static DnsTransportRequest Request(DnsMessage query)
    {
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint = NumericEndpoint();
        request.Options.Kind = DnsTransportKind.Quic;
        request.Options.AttemptTimeoutMilliseconds = 1000;
        request.Options.MaximumResponseLength = 4096;
        request.Options.Quic.IdleTimeoutMilliseconds = 1000;
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        return request;
    }

    static DnsEndpoint NumericEndpoint()
    {
        DnsEndpoint endpoint = new DnsEndpoint();
        endpoint.HasAddress = true;
        endpoint.Address = IPAddress.Loopback.GetAddressBytes();
        endpoint.AddressFamily = 4;
        return endpoint;
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

    sealed class FakeProvider : IQuicExchangeProvider
    {
        internal bool Supported = true;
        internal int ExchangeCount;
        internal IPEndPoint? Endpoint;
        internal string? AuthenticationName;
        internal QuicProviderResult Result = new QuicProviderResult();

        public bool IsSupported()
        {
            return this.Supported;
        }

        public QuicProviderResult Exchange(
            DnsTransportRequest request,
            IPEndPoint endpoint,
            string authenticationName,
            DnsClockValue deadline)
        {
            this.ExchangeCount++;
            this.Endpoint = endpoint;
            this.AuthenticationName = authenticationName;
            return this.Result;
        }
    }

    sealed class CapturingTransport : IDnsTransport
    {
        internal List<DnsTransportRequest> Requests =
            new List<DnsTransportRequest>();

        public DnsTransportResult Exchange(DnsTransportRequest request)
        {
            this.Requests.Add(request);
            DnsTransportResult result = new DnsTransportResult();
            result.Status = DnsTransportStatus.Success;
            result.Transport = DnsTransportKind.Quic;
            result.Endpoint = request.Endpoint;
            result.ResponseMessage = Response(request.QueryMessage!);
            result.ResponseBytes = Encode(result.ResponseMessage);
            return result;
        }

        public void CloseTransport()
        {
        }
    }

    sealed class ChunkedReadStream : Stream
    {
        readonly byte[] bytes;
        readonly int maximumChunk;
        int position;

        internal ChunkedReadStream(byte[] bytes, int maximumChunk)
        {
            this.bytes = bytes;
            this.maximumChunk = maximumChunk;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => this.bytes.Length;
        public override long Position
        {
            get => this.position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = this.bytes.Length - this.position;
            if (remaining == 0)
            {
                return 0;
            }

            int read = Math.Min(
                Math.Min(count, this.maximumChunk), remaining);
            Array.Copy(this.bytes, this.position, buffer, offset, read);
            this.position += read;
            return read;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            byte[] temporary = new byte[buffer.Length];
            int read = this.Read(temporary, 0, temporary.Length);
            temporary.AsMemory(0, read).CopyTo(buffer);
            return ValueTask.FromResult(read);
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
            throw new NotSupportedException();
        }
    }

    sealed class PendingReadStream : Stream
    {
        readonly TaskCompletionSource<int> pending =
            new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.pending.Task.GetAwaiter().GetResult();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            return new ValueTask<int>(this.pending.Task);
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
            throw new NotSupportedException();
        }
    }
}
