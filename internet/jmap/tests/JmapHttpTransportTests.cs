using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class JmapHttpTransportTests
{
    [Test]
    public void ConstructorAndSessionContentTypeValidateTheirInputs()
    {
        Assert.That(() => new JmapHttpTransport(null!, null),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("discoveryOptions"));

        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        script.Responses.Enqueue(Response(HttpStatusCode.OK, "{}", "text/plain"));
        JmapHttpTransport transport = Transport(script, null);
        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(async () =>
            await transport.GetTransportSessionAsync(
                "https://example.test/session", CancellationToken.None))!;

        Assert.That(error.Message, Does.Contain("application/json"));
    }

    [Test]
    public void MetadataSafetyHelpersCoverAbsentAndHostileValues()
    {
        JmapHttpTransport transport = Transport(new ScriptedHttpMessageHandler(), null);
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(Array.Empty<byte>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.GetContentType(response), Is.Null);
            Assert.That(transport.IsSafeAuthorizationValue(null), Is.True);
            Assert.That(transport.IsSafeAuthorizationValue("Bearer ok"), Is.True);
            Assert.That(transport.IsSafeAuthorizationValue("bad\rvalue"), Is.False);
            Assert.That(transport.IsSafeAuthorizationValue("bad\nvalue"), Is.False);
            Assert.That(transport.IsSafeEventId(null), Is.True);
            Assert.That(transport.IsSafeEventId(new string('x',
                EventSourceParser.MaximumLineCharacters + 1)), Is.False);
            Assert.That(transport.IsSafeEventId("bad\ud800id"), Is.False);
            Assert.That(transport.IsSafeEventId("bad\0id"), Is.False);
        }
    }

    [Test]
    public async Task SessionGetNegotiatesJsonAuthorizesRedirectsAndDisposesResponse()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        script.Responses.Enqueue(Redirect("/current-session"));
        OwnedResponseContent sessionContent = new OwnedResponseContent(
            RfcExampleFixtures.SessionJson("state-1"), JmapMediaTypes.Json);
        HttpResponseMessage success = new HttpResponseMessage(HttpStatusCode.OK);
        success.Content = sessionContent;
        script.Responses.Enqueue(success);
        FakeJmapAuthorizationProvider authorization = new FakeJmapAuthorizationProvider();
        JmapHttpTransport transport = Transport(script, authorization);

        JmapSession session = await transport.GetTransportSessionAsync(
            "https://example.test/session",
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.State, Is.EqualTo("state-1"));
            Assert.That(script.Requests.Select(item => item.Method), Is.EqualTo(new[] { "GET", "GET" }));
            Assert.That(script.Requests.Select(item => item.Accept), Is.EqualTo(new[]
            {
                JmapMediaTypes.Json,
                JmapMediaTypes.Json
            }));
            Assert.That(script.Requests.Select(item => item.Authorization), Is.EqualTo(new[]
            {
                "Bearer transport-secret",
                "Bearer transport-secret"
            }));
            Assert.That(authorization.ResourceUrls, Is.EqualTo(new[] { "https://example.test/session" }));
            Assert.That(sessionContent.WasDisposed, Is.True);
            Assert.That(sessionContent.ResponseStream.WasDisposed, Is.True);
        }
    }

    [Test]
    public async Task ApiPostSendsEncodedRequestHeadersAndDecodesJsonResponse()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        OwnedResponseContent responseContent = new OwnedResponseContent(
            RfcExampleFixtures.CoreEchoResponseJson, "application/json; charset=utf-8");
        HttpResponseMessage success = new HttpResponseMessage(HttpStatusCode.OK);
        success.Content = responseContent;
        script.Responses.Enqueue(success);
        FakeJmapAuthorizationProvider authorization = new FakeJmapAuthorizationProvider();
        JmapHttpTransport transport = Transport(script, authorization);

        JmapResponse response = await transport.SendTransportApiRequestAsync(
            "https://api.example.test/jmap",
            Request(),
            CancellationToken.None);

        RecordedHttpRequest sent = script.Requests[0];
        using JsonDocument requestJson = JsonDocument.Parse(sent.Body);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.SessionState, Is.EqualTo("state-2"));
            Assert.That(response.MethodResponses[0].Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(sent.Uri, Is.EqualTo("https://api.example.test/jmap"));
            Assert.That(sent.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(sent.Accept, Does.Contain(JmapMediaTypes.Json));
            Assert.That(sent.Accept, Does.Contain(JmapMediaTypes.ProblemJson));
            Assert.That(sent.Authorization, Is.EqualTo("Bearer transport-secret"));
            Assert.That(requestJson.RootElement.GetProperty("using")[0].GetString(), Is.EqualTo(JmapCapabilities.Core));
            Assert.That(requestJson.RootElement.GetProperty("methodCalls")[0][0].GetString(), Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(authorization.ResourceUrls, Is.EqualTo(new[] { "https://api.example.test/jmap" }));
            Assert.That(responseContent.WasDisposed, Is.True);
            Assert.That(responseContent.ResponseStream.WasDisposed, Is.True);
        }
    }

    [Test]
    public void ApiRequestLevelProblemIsDecodedWithRawResponseData()
    {
        string problemJson = RfcExampleFixtures.NotRequestProblemJson;
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        script.Responses.Enqueue(Response(HttpStatusCode.BadRequest, problemJson, JmapMediaTypes.ProblemJson));
        JmapHttpTransport transport = Transport(script, null);

        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(
            async () => await transport.SendTransportApiRequestAsync(
                "https://api.example.test/jmap",
                Request(),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(400));
            Assert.That(error.ErrorToken, Is.EqualTo("urn:ietf:params:jmap:error:notRequest"));
            Assert.That(error.Problem!.Detail, Is.EqualTo("Malformed request"));
            Assert.That(Encoding.UTF8.GetString(error.ResponseBody!), Is.EqualTo(problemJson));
            Assert.That(script.Requests, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void ApiRedirectIsNotFollowedAndCannotForwardAuthorization()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        HttpResponseMessage redirect = Redirect("https://other.test/jmap");
        redirect.Content = new StringContent("", Encoding.UTF8, JmapMediaTypes.Json);
        script.Responses.Enqueue(redirect);
        JmapHttpTransport transport = Transport(script, new FakeJmapAuthorizationProvider());

        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(
            async () => await transport.SendTransportApiRequestAsync(
                "https://api.example.test/jmap",
                Request(),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(302));
            Assert.That(script.Requests, Has.Count.EqualTo(1));
            Assert.That(script.Requests[0].Authorization, Is.EqualTo("Bearer transport-secret"));
        }
    }

    [Test]
    public void SuccessfulResponsesRequireJsonMediaTypeAndPreserveRawBodyInFailure()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        script.Responses.Enqueue(Response(HttpStatusCode.OK, "not json media", "text/plain"));
        JmapHttpTransport transport = Transport(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await transport.SendTransportApiRequestAsync(
                "https://api.example.test/jmap",
                Request(),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(200));
            Assert.That(error.Endpoint, Is.EqualTo("https://api.example.test/jmap"));
            Assert.That(Encoding.UTF8.GetString(error.ResponseBody!), Is.EqualTo("not json media"));
        }
    }

    [Test]
    public void SessionHttpFailureCarriesStatusBodyAndRetryGuidance()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        script.Responses.Enqueue(Response(HttpStatusCode.ServiceUnavailable, "temporarily offline", "text/plain"));
        JmapHttpTransport transport = Transport(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await transport.GetTransportSessionAsync(
                "https://example.test/session",
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(503));
            Assert.That(error.IsRetryable, Is.True);
            Assert.That(Encoding.UTF8.GetString(error.ResponseBody!), Is.EqualTo("temporarily offline"));
        }
    }

    [Test]
    public void DeclaredAndStreamingResponseSizeLimitsAreEnforced()
    {
        ScriptedHttpMessageHandler declaredScript = new ScriptedHttpMessageHandler();
        OwnedResponseContent declaredContent = new OwnedResponseContent("123456", JmapMediaTypes.Json);
        HttpResponseMessage declaredResponse = new HttpResponseMessage(HttpStatusCode.OK);
        declaredResponse.Content = declaredContent;
        declaredScript.Responses.Enqueue(declaredResponse);
        JmapHttpTransport declaredTransport = Transport(declaredScript, null);
        declaredTransport.MaximumResponseBytes = 5;

        JmapTransportException declaredError = Assert.ThrowsAsync<JmapTransportException>(
            async () => await declaredTransport.GetTransportSessionAsync(
                "https://example.test/session",
                CancellationToken.None))!;

        ScriptedHttpMessageHandler streamingScript = new ScriptedHttpMessageHandler();
        UnknownLengthContent streamingContent = new UnknownLengthContent(Encoding.UTF8.GetBytes("123456"));
        streamingContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(JmapMediaTypes.Json);
        HttpResponseMessage streamingResponse = new HttpResponseMessage(HttpStatusCode.OK);
        streamingResponse.Content = streamingContent;
        streamingScript.Responses.Enqueue(streamingResponse);
        JmapHttpTransport streamingTransport = Transport(streamingScript, null);
        streamingTransport.MaximumResponseBytes = 5;

        JmapTransportException streamingError = Assert.ThrowsAsync<JmapTransportException>(
            async () => await streamingTransport.GetTransportSessionAsync(
                "https://example.test/session",
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(declaredError.Message, Does.Contain("size limit"));
            Assert.That(streamingError.Message, Does.Contain("size limit"));
            Assert.That(declaredError.HttpStatus, Is.EqualTo(200));
            Assert.That(declaredError.HasHttpStatus, Is.True);
            Assert.That(declaredContent.WasDisposed, Is.True);
            Assert.That(streamingContent.WasDisposed, Is.True);
            Assert.That(streamingContent.ResponseStream.WasDisposed, Is.True);
        }
    }

    [Test]
    public async Task FragmentedSessionApiAndProblemBodiesAreReadToCompletion()
    {
        ScriptedHttpMessageHandler sessionScript = new ScriptedHttpMessageHandler();
        FragmentedContent sessionContent = new FragmentedContent(
            RfcExampleFixtures.SessionJson("fragmented-state"), JmapMediaTypes.Json, 1);
        HttpResponseMessage sessionResponse = new HttpResponseMessage(HttpStatusCode.OK);
        sessionResponse.Content = sessionContent;
        sessionScript.Responses.Enqueue(sessionResponse);
        JmapSession session = await Transport(sessionScript, null).GetTransportSessionAsync(
            "https://example.test/session", CancellationToken.None);

        ScriptedHttpMessageHandler apiScript = new ScriptedHttpMessageHandler();
        FragmentedContent apiContent = new FragmentedContent(
            RfcExampleFixtures.CoreEchoResponseJson, JmapMediaTypes.Json, 2);
        HttpResponseMessage apiResponse = new HttpResponseMessage(HttpStatusCode.OK);
        apiResponse.Content = apiContent;
        apiScript.Responses.Enqueue(apiResponse);
        JmapResponse response = await Transport(apiScript, null).SendTransportApiRequestAsync(
            "https://api.example.test/jmap", Request(), CancellationToken.None);

        ScriptedHttpMessageHandler problemScript = new ScriptedHttpMessageHandler();
        FragmentedContent problemContent = new FragmentedContent(
            RfcExampleFixtures.NotRequestProblemJson, JmapMediaTypes.ProblemJson, 3);
        HttpResponseMessage problemResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
        problemResponse.Content = problemContent;
        problemScript.Responses.Enqueue(problemResponse);
        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(async () =>
            await Transport(problemScript, null).SendTransportApiRequestAsync(
                "https://api.example.test/jmap", Request(), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.State, Is.EqualTo("fragmented-state"));
            Assert.That(response.SessionState, Is.EqualTo("state-2"));
            Assert.That(error.ErrorToken, Is.EqualTo("urn:ietf:params:jmap:error:notRequest"));
            Assert.That(sessionContent.ResponseStream.ReadCount, Is.GreaterThan(1));
            Assert.That(apiContent.ResponseStream.ReadCount, Is.GreaterThan(1));
            Assert.That(problemContent.ResponseStream.ReadCount, Is.GreaterThan(1));
            Assert.That(sessionContent.WasDisposed, Is.True);
            Assert.That(apiContent.WasDisposed, Is.True);
            Assert.That(problemContent.WasDisposed, Is.True);
            Assert.That(sessionContent.ResponseStream.WasDisposed, Is.True);
            Assert.That(apiContent.ResponseStream.WasDisposed, Is.True);
            Assert.That(problemContent.ResponseStream.WasDisposed, Is.True);
        }
    }

    [Test]
    public void InvalidAuthorizationHeaderIsRejectedBeforeHttpDispatch()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        FakeJmapAuthorizationProvider authorization = new FakeJmapAuthorizationProvider();
        authorization.Value = "Bearer secret\r\nX-Injected: yes";
        JmapHttpTransport transport = Transport(script, authorization);

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await transport.SendTransportApiRequestAsync(
                "https://api.example.test/jmap",
                Request(),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties, Is.EqualTo(new[] { "authorization" }));
            Assert.That(error.Message, Does.Not.Contain("secret"));
            Assert.That(script.Requests, Is.Empty);
        }
    }

    [Test]
    public void HttpDispatchFailuresAreMappedToRetryableTransportExceptions()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        HttpRequestException cause = new HttpRequestException("network unavailable");
        script.Failure = cause;
        JmapHttpTransport transport = Transport(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await transport.SendTransportApiRequestAsync(
                "https://api.example.test/jmap",
                Request(),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Endpoint, Is.EqualTo("https://api.example.test/jmap"));
            Assert.That(error.IsRetryable, Is.True);
            Assert.That(error.HasHttpStatus, Is.False);
            Assert.That(error.InnerException, Is.SameAs(cause));
        }
    }

    [Test]
    public void ResponseStreamFailuresAreMappedAndTheResponseIsDisposed()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        System.IO.IOException cause = new System.IO.IOException("connection reset");
        FailingReadContent content = new FailingReadContent(cause);
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = content;
        script.Responses.Enqueue(response);
        JmapHttpTransport transport = Transport(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await transport.GetTransportSessionAsync(
                "https://example.test/session",
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(200));
            Assert.That(error.IsRetryable, Is.True);
            Assert.That(error.InnerException, Is.SameAs(cause));
            Assert.That(content.WasDisposed, Is.True);
            Assert.That(content.ResponseStream.WasDisposed, Is.True);
        }
    }

    [Test]
    public void ApiRejectsUnexpectedSuccessStatusEvenWithValidJsonBody()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        script.Responses.Enqueue(Response(
            HttpStatusCode.Created, RfcExampleFixtures.CoreEchoResponseJson, JmapMediaTypes.Json));
        JmapHttpTransport transport = Transport(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await transport.SendTransportApiRequestAsync(
                "https://api.example.test/jmap",
                Request(),
                CancellationToken.None))!;

        Assert.That(error.HttpStatus, Is.EqualTo(201));
    }

    [Test]
    public void CancellationBeforeRequestDispatchReturnsCanceledTask()
    {
        ScriptedHttpMessageHandler script = new ScriptedHttpMessageHandler();
        JmapHttpTransport transport = Transport(script, new FakeJmapAuthorizationProvider());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<JmapResponse> request = transport.SendTransportApiRequestAsync(
            "https://api.example.test/jmap",
            Request(),
            cancellation.Token);

        Assert.That(async () => await request, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.IsCanceled, Is.True);
            Assert.That(script.Requests, Is.Empty);
        }
    }

    private static JmapHttpTransport Transport(
        ScriptedHttpMessageHandler script,
        IJmapAuthorizationProvider? authorizationProvider)
    {
        JmapDiscoveryOptions options = new JmapDiscoveryOptions();
        JmapHttpTransport transport = new JmapHttpTransport(options, authorizationProvider);
        transport.Client = new HttpClient(script);
        return transport;
    }

    private static JmapRequest Request()
    {
        return PublicApiTestHelper.CoreEchoRequest("call-1", "{\"value\":7}");
    }

    private static HttpResponseMessage Redirect(string location)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(body, Encoding.UTF8, mediaType);
        return response;
    }

    private sealed class OwnedResponseContent : HttpContent
    {
        public TrackingMemoryStream ResponseStream;
        public bool WasDisposed;

        public OwnedResponseContent(string body, string mediaType)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            ResponseStream = new TrackingMemoryStream(bytes);
            Headers.ContentLength = bytes.Length;
            Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return ResponseStream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = ResponseStream.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(ResponseStream);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class UnknownLengthContent : HttpContent
    {
        public TrackingMemoryStream ResponseStream;
        public bool WasDisposed;

        public UnknownLengthContent(byte[] bytes)
        {
            ResponseStream = new TrackingMemoryStream(bytes);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return ResponseStream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(ResponseStream);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public bool WasDisposed;

        public TrackingMemoryStream(byte[] bytes) : base(bytes)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingReadContent : HttpContent
    {
        public FailingReadStream ResponseStream;
        public bool WasDisposed;

        public FailingReadContent(Exception failure)
        {
            ResponseStream = new FailingReadStream(failure);
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(JmapMediaTypes.Json);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return Task.CompletedTask;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(ResponseStream);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingReadStream : MemoryStream
    {
        public Exception Failure;
        public bool WasDisposed;

        public FailingReadStream(Exception failure)
        {
            Failure = failure;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            return Task.FromException<int>(Failure);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FragmentedContent : HttpContent
    {
        public FragmentedResponseStream ResponseStream;
        public bool WasDisposed;

        public FragmentedContent(string body, string mediaType, int fragmentSize)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            ResponseStream = new FragmentedResponseStream(bytes, fragmentSize);
            Headers.ContentLength = bytes.Length;
            Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return ResponseStream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = ResponseStream.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(ResponseStream);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FragmentedResponseStream : MemoryStream
    {
        public int FragmentSize;
        public int ReadCount;
        public bool WasDisposed;

        public FragmentedResponseStream(byte[] bytes, int fragmentSize) : base(bytes)
        {
            FragmentSize = fragmentSize;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            ReadCount = ReadCount + 1;
            int fragmentedCount = count < FragmentSize ? count : FragmentSize;
            return base.ReadAsync(buffer, offset, fragmentedCount, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
