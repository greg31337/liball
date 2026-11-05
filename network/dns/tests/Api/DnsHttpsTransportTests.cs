using System.Net;
using System.Net.Http.Headers;

namespace dns.Tests.Api;

public class DnsHttpsTransportTests
{
    [Test]
    public void SendsPostWithDnsMediaTypeAndReturnsCorrelatedResponse()
    {
        DnsMessage query = Query(0x6201, "post.example.");
        byte[] responseBytes = Encode(Response(query));
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot => Task.FromResult(
            HttpResponse(HttpStatusCode.OK, responseBytes)));
        DnsTransportRequest request = Request(query);

        DnsTransportResult result = Transport(handler).Exchange(request);

        RequestSnapshot sent = handler.Requests[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Https));
            Assert.That(result.ResponseBytes, Is.EqualTo(responseBytes));
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x6201));
            Assert.That(sent.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(sent.Uri.AbsoluteUri,
                Is.EqualTo("https://resolver.test/dns-query"));
            Assert.That(sent.Body, Is.EqualTo(request.QueryBytes));
            Assert.That(sent.ContentType,
                Is.EqualTo("application/dns-message"));
            Assert.That(sent.Accept,
                Is.EqualTo("application/dns-message"));
        });
    }

    [Test]
    public void SendsGetWithUnpaddedBase64UrlDnsParameter()
    {
        DnsMessage query = Query(0x6202, "get.example.");
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot => Task.FromResult(
            HttpResponse(HttpStatusCode.OK, Encode(Response(query)))));
        DnsTransportRequest request = Request(query);
        request.Options.Https.Uri =
            "https://resolver.test/dns-query?token=one";
        request.Options.Https.UseGet = true;

        DnsTransportResult result = Transport(handler).Exchange(request);

        string expected = Convert.ToBase64String(request.QueryBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        RequestSnapshot sent = handler.Requests[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(sent.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(sent.Body, Is.Empty);
            Assert.That(sent.Uri.Query, Does.Contain("token=one"));
            Assert.That(sent.Uri.Query, Does.Contain("dns=" + expected));
            Assert.That(sent.Uri.Query, Does.Not.Contain("+"));
            Assert.That(sent.Uri.Query, Does.Not.Contain("/"));
            Assert.That(sent.Uri.Query, Does.Not.EndWith("="));
        });
    }

    [Test]
    public void FollowsAllowedRelativeRedirectAndAppliesHttpGetSemantics()
    {
        DnsMessage query = Query(0x6203, "redirect.example.");
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot =>
        {
            HttpResponseMessage redirect = new HttpResponseMessage(
                HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("/alternate", UriKind.Relative);
            return Task.FromResult(redirect);
        });
        handler.Enqueue(snapshot => Task.FromResult(
            HttpResponse(HttpStatusCode.OK, Encode(Response(query)))));
        DnsTransportRequest request = Request(query);
        request.Options.Https.AllowRedirects = true;
        request.Options.Https.MaximumRedirects = 1;

        DnsTransportResult result = Transport(handler).Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
            Assert.That(handler.Requests[0].Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Requests[1].Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.Requests[1].Uri.AbsolutePath,
                Is.EqualTo("/alternate"));
            Assert.That(handler.Requests[1].Uri.Query,
                Does.Contain("dns="));
        });
    }

    [Test]
    public void RejectsDisallowedUnsafeAndExcessRedirects()
    {
        DnsMessage query = Query(0x6204, "policy.example.");
        RecordingHandler disallowedHandler = RedirectHandler(
            new Uri("/next", UriKind.Relative));
        DnsTransportRequest disallowedRequest = Request(query);
        DnsTransportResult disallowed =
            Transport(disallowedHandler).Exchange(disallowedRequest);

        RecordingHandler unsafeHandler = RedirectHandler(
            new Uri("http://resolver.test/dns-query"));
        DnsTransportRequest unsafeRequest = Request(query);
        unsafeRequest.Options.Https.AllowRedirects = true;
        unsafeRequest.Options.Https.MaximumRedirects = 1;
        DnsTransportResult unsafeResult =
            Transport(unsafeHandler).Exchange(unsafeRequest);

        RecordingHandler excessiveHandler = RedirectHandler(
            new Uri("/next", UriKind.Relative));
        DnsTransportRequest excessiveRequest = Request(query);
        excessiveRequest.Options.Https.AllowRedirects = true;
        excessiveRequest.Options.Https.MaximumRedirects = 1;
        excessiveHandler.Enqueue(snapshot =>
        {
            HttpResponseMessage redirect = new HttpResponseMessage(
                HttpStatusCode.TemporaryRedirect);
            redirect.Headers.Location = new Uri("/again", UriKind.Relative);
            return Task.FromResult(redirect);
        });
        DnsTransportResult excessive =
            Transport(excessiveHandler).Exchange(excessiveRequest);

        Assert.Multiple(() =>
        {
            Assert.That(disallowed.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(unsafeResult.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(excessive.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(disallowed.Diagnostics[0].Field,
                Is.EqualTo("http.redirect"));
            Assert.That(unsafeResult.Diagnostics[0].Field,
                Is.EqualTo("http.redirect"));
            Assert.That(excessiveHandler.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void BootstrapEndpointUsesOriginPortAndRestrictsRedirectOrigin()
    {
        DnsMessage query = Query(0x6205, "bootstrap.example.");
        RecordingHandler handler = RedirectHandler(
            new Uri("https://other.test/dns-query"));
        DnsTransportRequest request = Request(query);
        request.Options.Https.Uri =
            "https://resolver.test:8443/dns-query";
        request.Options.Https.AllowRedirects = true;
        request.Options.Https.MaximumRedirects = 1;
        request.Options.Https.BootstrapEndpoint = new DnsEndpoint();
        request.Options.Https.BootstrapEndpoint.HasAddress = true;
        request.Options.Https.BootstrapEndpoint.Address =
            IPAddress.Loopback.GetAddressBytes();
        request.Options.Https.BootstrapEndpoint.AddressFamily = 4;

        DnsTransportResult result = Transport(handler).Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.Endpoint.HasAddress, Is.True);
            Assert.That(result.Endpoint.Address,
                Is.EqualTo(IPAddress.Loopback.GetAddressBytes()));
            Assert.That(result.Endpoint.Port, Is.EqualTo(8443));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain("another origin"));
        });
    }

    [Test]
    public void RejectsHttpFailureUnexpectedMediaTypeAndOversizedBody()
    {
        DnsMessage query = Query(0x6206, "response.example.");
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot => Task.FromResult(
            HttpResponse(HttpStatusCode.ServiceUnavailable, [])));
        HttpResponseMessage wrongMedia = HttpResponse(
            HttpStatusCode.OK, Encode(Response(query)));
        wrongMedia.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");
        handler.Enqueue(snapshot => Task.FromResult(wrongMedia));
        handler.Enqueue(snapshot => Task.FromResult(HttpResponse(
            HttpStatusCode.OK, new byte[65])));
        DnsTransportRequest statusRequest = Request(query);
        DnsTransportRequest mediaRequest = Request(query);
        DnsTransportRequest sizeRequest = Request(query);
        sizeRequest.Options.MaximumResponseLength = 64;
        DnsHttpsTransport transport = Transport(handler);

        DnsTransportResult status = transport.Exchange(statusRequest);
        DnsTransportResult media = transport.Exchange(mediaRequest);
        DnsTransportResult size = transport.Exchange(sizeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(status.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(status.Diagnostics[0].Field,
                Is.EqualTo("http.statusCode"));
            Assert.That(media.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(media.Diagnostics[0].Field,
                Is.EqualTo("http.contentType"));
            Assert.That(size.Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(size.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void BoundsAnUnknownLengthResponseBody()
    {
        DnsMessage query = Query(0x6207, "stream.example.");
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot =>
        {
            HttpResponseMessage response = new HttpResponseMessage(
                HttpStatusCode.OK);
            response.Content = new UnknownLengthContent(new byte[65]);
            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/dns-message");
            return Task.FromResult(response);
        });
        DnsTransportRequest request = Request(query);
        request.Options.MaximumResponseLength = 64;

        DnsTransportResult result = Transport(handler).Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(result.ResponseBytes, Is.Empty);
        });
    }

    [Test]
    public void TimesOutAndCancelsThePendingHttpOperation()
    {
        DnsMessage query = Query(0x6208, "timeout.example.");
        RecordingHandler handler = new RecordingHandler();
        TaskCompletionSource<HttpResponseMessage> pending =
            new TaskCompletionSource<HttpResponseMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        handler.Enqueue(snapshot => pending.Task);
        DnsTransportRequest request = Request(query);
        request.Options.AttemptTimeoutMilliseconds = 30;

        DnsTransportResult result = Transport(handler).Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void ObservesLiveCancellationDuringPendingHttpOperation()
    {
        using ManualResetEventSlim started = new ManualResetEventSlim();
        DnsMessage query = Query(0x6258, "cancel.example.");
        RecordingHandler handler = new RecordingHandler();
        TaskCompletionSource<HttpResponseMessage> pending =
            new TaskCompletionSource<HttpResponseMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        handler.Enqueue(snapshot =>
        {
            started.Set();
            return pending.Task;
        });
        DnsTransportRequest request = Request(query);
        Task<DnsTransportResult> exchange = Task.Run(
            () => Transport(handler).Exchange(request));
        Assert.That(started.Wait(1000), Is.True);

        request.CancellationRequested = true;
        DnsTransportResult result = exchange.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.Cancelled));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void RejectsMismatchedDnsResponse()
    {
        DnsMessage query = Query(0x6209, "mismatch.example.");
        DnsMessage response = Response(query);
        response.Header.Identifier = 0x6210;
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot => Task.FromResult(HttpResponse(
            HttpStatusCode.OK, Encode(response))));

        DnsTransportResult result = Transport(handler).Exchange(
            Request(query));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResponseMismatch));
        });
    }

    [Test]
    public void RejectsMalformedDnsResponseAndPreservesReceivedBytes()
    {
        byte[] malformed = [0, 1, 2, 3];
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot => Task.FromResult(HttpResponse(
            HttpStatusCode.OK, malformed)));

        DnsTransportResult result = Transport(handler).Exchange(
            Request(Query(0x6212, "malformed.example.")));

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
    public void AcceptsResponseBodyAtTheExactConfiguredLimit()
    {
        DnsMessage query = Query(0x6213, "exact-limit.example.");
        byte[] responseBytes = Encode(Response(query));
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot => Task.FromResult(HttpResponse(
            HttpStatusCode.OK, responseBytes)));
        DnsTransportRequest request = Request(query);
        request.Options.MaximumResponseLength = responseBytes.Length;

        DnsTransportResult result = Transport(handler).Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.ResponseBytes, Is.EqualTo(responseBytes));
            Assert.That(result.ResponseMessage, Is.Not.Null);
        });
    }

    [TestCase(HttpStatusCode.TemporaryRedirect)]
    [TestCase(HttpStatusCode.PermanentRedirect)]
    public void PreservesPostAcrossMethodPreservingRedirects(
        HttpStatusCode redirectStatus)
    {
        DnsMessage query = Query(0x6214, "preserve-post.example.");
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot =>
        {
            HttpResponseMessage redirect = new HttpResponseMessage(
                redirectStatus);
            redirect.Headers.Location = new Uri("/alternate", UriKind.Relative);
            return Task.FromResult(redirect);
        });
        handler.Enqueue(snapshot => Task.FromResult(HttpResponse(
            HttpStatusCode.OK, Encode(Response(query)))));
        DnsTransportRequest request = Request(query);
        request.Options.Https.AllowRedirects = true;
        request.Options.Https.MaximumRedirects = 1;

        DnsTransportResult result = Transport(handler).Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
            Assert.That(handler.Requests[1].Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Requests[1].Body,
                Is.EqualTo(request.QueryBytes));
            Assert.That(handler.Requests[1].ContentType,
                Is.EqualTo("application/dns-message"));
        });
    }

    [Test]
    public void ObservesLiveCancellationWhileReadingResponseBody()
    {
        using ManualResetEventSlim readStarted = new ManualResetEventSlim();
        DnsMessage query = Query(0x6215, "body-cancel.example.");
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot =>
        {
            HttpResponseMessage response = new HttpResponseMessage(
                HttpStatusCode.OK);
            response.Content = new StreamContent(
                new PendingReadStream(readStarted));
            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/dns-message");
            return Task.FromResult(response);
        });
        DnsTransportRequest request = Request(query);
        Task<DnsTransportResult> exchange = Task.Run(
            () => Transport(handler).Exchange(request));
        Assert.That(readStarted.Wait(1000), Is.True);

        request.CancellationRequested = true;
        DnsTransportResult result = exchange.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.Cancelled));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.ResponseMessage, Is.Null);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void QueryClientRoutesDirectHttpsRequestsAndHonorsClosedState()
    {
        CapturingTransport capturing = new CapturingTransport();
        DnsQueryClient client = new DnsQueryClient();
        client.NextServerOffset = 0;
        client.HttpsTransport = capturing;
        DnsQueryRequest queryRequest = new DnsQueryRequest();
        queryRequest.Question.Name = Parse("client.example.");
        queryRequest.Question.RecordType = DnsRecordType.A;
        queryRequest.Question.RecordTypeValue = 1;
        queryRequest.Question.Class = DnsClass.Internet;
        queryRequest.Question.ClassValue = 1;
        queryRequest.Servers.Add(new DnsEndpoint
        {
            HasAddress = true,
            Address = IPAddress.Loopback.GetAddressBytes(),
            AddressFamily = 4
        });
        queryRequest.TransportOptions.Kind = DnsTransportKind.Https;
        queryRequest.TransportOptions.Https.Uri =
            "https://resolver.test/dns-query";
        queryRequest.TransportOptions.MaximumAttempts = 1;

        DnsQueryResult queryResult = client.Query(queryRequest);
        DnsHttpsTransport closedTransport = new DnsHttpsTransport();
        closedTransport.CloseTransport();
        DnsTransportResult closed = closedTransport.Exchange(
            Request(Query(0x6211, "closed.example.")));

        Assert.Multiple(() =>
        {
            Assert.That(queryResult.Success, Is.True);
            Assert.That(capturing.Requests, Has.Count.EqualTo(1));
            Assert.That(capturing.Requests[0].Options.Kind,
                Is.EqualTo(DnsTransportKind.Https));
            Assert.That(closed.Status, Is.EqualTo(DnsTransportStatus.Closed));
        });
    }

    static DnsHttpsTransport Transport(RecordingHandler handler)
    {
        DnsHttpsTransport transport = new DnsHttpsTransport();
        transport.MessageHandler = handler;
        return transport;
    }

    static RecordingHandler RedirectHandler(Uri location)
    {
        RecordingHandler handler = new RecordingHandler();
        handler.Enqueue(snapshot =>
        {
            HttpResponseMessage redirect = new HttpResponseMessage(
                HttpStatusCode.Found);
            redirect.Headers.Location = location;
            return Task.FromResult(redirect);
        });
        return handler;
    }

    static DnsTransportRequest Request(DnsMessage query)
    {
        DnsTransportRequest request = new DnsTransportRequest();
        request.Options.Kind = DnsTransportKind.Https;
        request.Options.AttemptTimeoutMilliseconds = 1000;
        request.Options.MaximumResponseLength = 4096;
        request.Options.Https.Uri =
            "https://resolver.test/dns-query";
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        return request;
    }

    static HttpResponseMessage HttpResponse(
        HttpStatusCode status,
        byte[] bytes)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new ByteArrayContent(bytes);
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/dns-message");
        return response;
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

    sealed class RecordingHandler : HttpMessageHandler
    {
        internal List<RequestSnapshot> Requests = new List<RequestSnapshot>();
        internal Queue<Func<RequestSnapshot, Task<HttpResponseMessage>>>
            Responses =
                new Queue<Func<RequestSnapshot, Task<HttpResponseMessage>>>();

        internal void Enqueue(
            Func<RequestSnapshot, Task<HttpResponseMessage>> response)
        {
            this.Responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestSnapshot snapshot = new RequestSnapshot();
            snapshot.Method = request.Method;
            snapshot.Uri = request.RequestUri!;
            snapshot.Accept = request.Headers.Accept.FirstOrDefault()?.MediaType;
            if (request.Content != null)
            {
                snapshot.Body = request.Content.ReadAsByteArrayAsync(
                    cancellationToken).GetAwaiter().GetResult();
                snapshot.ContentType =
                    request.Content.Headers.ContentType?.MediaType;
            }

            this.Requests.Add(snapshot);
            return this.Responses.Dequeue()(snapshot);
        }
    }

    sealed class RequestSnapshot
    {
        internal HttpMethod Method = HttpMethod.Get;
        internal Uri Uri = new Uri("https://invalid.test/");
        internal byte[] Body = [];
        internal string? Accept;
        internal string? ContentType;
    }

    sealed class UnknownLengthContent : HttpContent
    {
        readonly byte[] bytes;

        internal UnknownLengthContent(byte[] bytes)
        {
            this.bytes = bytes;
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            return stream.WriteAsync(this.bytes).AsTask();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    sealed class PendingReadStream : Stream
    {
        readonly ManualResetEventSlim readStarted;

        internal PendingReadStream(ManualResetEventSlim readStarted)
        {
            this.readStarted = readStarted;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            this.readStarted.Set();
            return new ValueTask<int>(WaitForCancellation(cancellationToken));
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

        static async Task<int> WaitForCancellation(
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
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
            result.Transport = DnsTransportKind.Https;
            result.Endpoint = request.Endpoint;
            result.ResponseMessage = Response(request.QueryMessage!);
            result.ResponseBytes = Encode(result.ResponseMessage);
            return result;
        }

        public void CloseTransport()
        {
        }
    }
}
