using System.Net;
using System.Net.Http;
using System.Text;

namespace jmap.Tests;

public class JmapHttpUploadTests
{
    [Test]
    public void MissingRequestAndUnsafeAuthorizationAreRejectedBeforeDispatch()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        UploadAuthorizationProvider authorization = new UploadAuthorizationProvider();
        authorization.Value = "Bearer secret\r\nInjected: yes";
        JmapHttpTransport transport = Transport(script, authorization);

        Assert.That(() => transport.ValidateUploadRequest(null!),
            Throws.TypeOf<JmapValidationException>());
        BlobUploadRequest missingContent = UploadRequest(
            new MemoryStream(new byte[] { 1 }), true, 1, "image/png");
        missingContent.Content = null;
        Assert.That(() => transport.ValidateUploadRequest(missingContent),
            Throws.TypeOf<JmapValidationException>());
        MemoryStream pastEnd = new MemoryStream(new byte[] { 1 });
        pastEnd.Position = 2;
        Assert.That(() => transport.ValidateUploadRequest(
            UploadRequest(pastEnd, false, 0, "image/png")),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(() => transport.ValidateUploadRequest(
            UploadRequest(new MemoryStream(new byte[] { 1 }), false, 0, "image/png")),
            Throws.Nothing);
        Assert.That(async () => await transport.SendTransportUploadAsync(
            "https://upload.example.test/{accountId}",
            UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1, "image/png"),
            CancellationToken.None), Throws.TypeOf<JmapValidationException>());
        Assert.That(script.Requests, Is.Empty);
    }

    [Test]
    public void ContentRemovedWhileAuthorizationIsResolvedIsRejectedBeforeDispatch()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        UploadAuthorizationProvider authorization = new UploadAuthorizationProvider();
        BlobUploadRequest request = UploadRequest(
            new MemoryStream(new byte[] { 1 }), true, 1, "image/png");
        authorization.OnGet = () => request.Content = null;
        JmapHttpTransport transport = Transport(script, authorization);

        Assert.That(async () => await transport.SendTransportUploadAsync(
            "https://upload.example.test/{accountId}", request, CancellationToken.None),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(script.Requests, Is.Empty);
    }

    [Test]
    public void SuccessfulUploadRequiresJsonAndConsistentBlobMetadata()
    {
        UploadHttpHandler mediaScript = new UploadHttpHandler();
        HttpResponseMessage plain = new HttpResponseMessage(HttpStatusCode.OK);
        plain.Content = new StringContent("{}", Encoding.UTF8, "text/plain");
        mediaScript.Responses.Enqueue(plain);
        Assert.That(async () => await Transport(mediaScript, null).SendTransportUploadAsync(
            "https://upload.example.test/{accountId}",
            UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1, "image/png"),
            CancellationToken.None), Throws.TypeOf<JmapTransportException>());

        JmapHttpTransport transport = Transport(new UploadHttpHandler(), null);
        BlobUploadRequest request = UploadRequest(
            new MemoryStream(new byte[] { 1 }), true, 1, "image/png");
        BlobUploadResponse invalidBlob = new BlobUploadResponse
        {
            AccountId = "account_1",
            BlobId = "bad blob",
            Type = "image/png",
            Size = 1
        };
        BlobUploadResponse invalidType = new BlobUploadResponse
        {
            AccountId = "account_1",
            BlobId = "blob-1",
            Type = "bad type",
            Size = 1
        };
        BlobUploadResponse invalidSize = new BlobUploadResponse
        {
            AccountId = "account_1",
            BlobId = "blob-1",
            Type = "image/png",
            Size = 2
        };
        Assert.That(() => transport.ValidateUploadResponse(invalidBlob, request),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => transport.ValidateUploadResponse(invalidType, request),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => transport.ValidateUploadResponse(invalidSize, request),
            Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public async Task KnownLengthUploadExpandsUrlStreamsBodyAndDecodesTypedResponse()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        script.Responses.Enqueue(JsonResponse(
            HttpStatusCode.Created,
            "{\"accountId\":\"account_1\",\"blobId\":\"blob-1\",\"type\":\"text/plain; charset=utf-8\",\"size\":6,\"scan\":\"clean\"}"));
        UploadAuthorizationProvider authorization = new UploadAuthorizationProvider();
        JmapHttpTransport transport = Transport(script, authorization);
        TrackingUploadStream content = new TrackingUploadStream(Encoding.UTF8.GetBytes("abcdef"));
        BlobUploadRequest request = UploadRequest(content, true, 6, "text/plain; charset=utf-8");

        BlobUploadResponse response = await transport.SendTransportUploadAsync(
            "https://upload.example.test/{accountId}/blob",
            request,
            CancellationToken.None);

        RecordedUpload sent = script.Requests[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent.Uri, Is.EqualTo("https://upload.example.test/account_1/blob"));
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(sent.ContentType, Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(sent.HasContentLength, Is.True);
            Assert.That(sent.ContentLength, Is.EqualTo(6));
            Assert.That(sent.IsChunked, Is.False);
            Assert.That(Encoding.UTF8.GetString(sent.Body), Is.EqualTo("abcdef"));
            Assert.That(sent.Accept, Does.Contain(JmapMediaTypes.Json));
            Assert.That(sent.Accept, Does.Contain(JmapMediaTypes.ProblemJson));
            Assert.That(sent.Authorization, Is.EqualTo("Bearer upload-secret"));
            Assert.That(authorization.ResourceUrls, Is.EqualTo(new[] { sent.Uri }));
            Assert.That(response.AccountId, Is.EqualTo("account_1"));
            Assert.That(response.BlobId, Is.EqualTo("blob-1"));
            Assert.That(response.Type, Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(response.Size, Is.EqualTo(6));
            Assert.That(response.UnknownProperties["scan"].GetString(), Is.EqualTo("clean"));
            Assert.That(content.WasDisposed, Is.False);
        }
    }

    [Test]
    public async Task UnknownLengthUploadUsesChunkedTransferWithoutBufferingOrDisposal()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        script.Responses.Enqueue(JsonResponse(
            HttpStatusCode.OK,
            "{\"accountId\":\"account_1\",\"blobId\":\"blob-2\",\"type\":\"application/octet-stream\",\"size\":4}"));
        JmapHttpTransport transport = Transport(script, null);
        NonSeekableUploadStream content = new NonSeekableUploadStream(new byte[] { 0, 1, 2, 3 });
        BlobUploadRequest request = UploadRequest(content, false, 0, JmapMediaTypes.OctetStream);

        BlobUploadResponse response = await transport.SendTransportUploadAsync(
            "https://upload.example.test/account_1/blob",
            request,
            CancellationToken.None);

        RecordedUpload sent = script.Requests[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent.HasContentLength, Is.False);
            Assert.That(sent.IsChunked, Is.True);
            Assert.That(sent.Body, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
            Assert.That(response.Size, Is.EqualTo(4));
            Assert.That(content.WasDisposed, Is.False);
        }
    }

    [Test]
    public void KnownLengthOverLimitIsRejectedBeforeAuthorizationOrReading()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        UploadAuthorizationProvider authorization = new UploadAuthorizationProvider();
        JmapHttpTransport transport = Transport(script, authorization);
        transport.MaximumUploadBytes = 5;
        TrackingUploadStream content = new TrackingUploadStream(Encoding.UTF8.GetBytes("abcdef"));
        BlobUploadRequest request = UploadRequest(content, true, 6, "text/plain");

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await transport.SendTransportUploadAsync(
                "https://upload.example.test/{accountId}",
                request,
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties, Does.Contain("contentLength"));
            Assert.That(script.Requests, Is.Empty);
            Assert.That(authorization.ResourceUrls, Is.Empty);
            Assert.That(content.Position, Is.Zero);
            Assert.That(content.WasDisposed, Is.False);
        }
    }

    [Test]
    public void UploadRequestValidationReportsAllInvalidInputFields()
    {
        JmapHttpTransport transport = Transport(new UploadHttpHandler(), null);
        BlobUploadRequest request = new BlobUploadRequest();
        request.AccountId = "bad/account";
        request.MediaType = "invalid\r\nX-Header: injected";
        request.Content = new MemoryStream();
        request.Content.Dispose();
        request.HasContentLength = true;
        request.ContentLength = -1;

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await transport.SendTransportUploadAsync(
                "https://upload.example.test/{accountId}",
                request,
                CancellationToken.None))!;

        Assert.That(error.InvalidProperties, Is.EqualTo(new[]
        {
            "accountId",
            "mediaType",
            "content",
            "contentLength"
        }));
    }

    [Test]
    public void DeclaredLengthMustMatchSeekableContentLength()
    {
        JmapHttpTransport transport = Transport(new UploadHttpHandler(), null);
        TrackingUploadStream content = new TrackingUploadStream(new byte[] { 1, 2, 3 });
        BlobUploadRequest request = UploadRequest(content, true, 2, "image/png");

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await transport.SendTransportUploadAsync(
                "https://upload.example.test/{accountId}",
                request,
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties, Does.Contain("contentLength"));
            Assert.That(content.Position, Is.Zero);
            Assert.That(content.WasDisposed, Is.False);
        }
    }

    [Test]
    public void UploadProblemResponseIsDecodedAndCallerStreamRemainsOpen()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        script.Responses.Enqueue(ProblemResponse(
            (HttpStatusCode)413,
            "{\"type\":\"urn:ietf:params:jmap:error:limit\",\"status\":413,\"limit\":\"maxSizeUpload\"}"));
        JmapHttpTransport transport = Transport(script, null);
        TrackingUploadStream content = new TrackingUploadStream(new byte[] { 1, 2, 3 });

        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(
            async () => await transport.SendTransportUploadAsync(
                "https://upload.example.test/{accountId}",
                UploadRequest(content, true, 3, "image/png"),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(413));
            Assert.That(error.Problem!.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(error.Problem.Limit, Is.EqualTo("maxSizeUpload"));
            Assert.That(content.WasDisposed, Is.False);
        }
    }

    [Test]
    public void CancellationDuringDispatchDetachesAndPreservesCallerStream()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        UploadHttpHandler script = new UploadHttpHandler();
        script.CancelSource = cancellation;
        JmapHttpTransport transport = Transport(script, null);
        TrackingUploadStream content = new TrackingUploadStream(new byte[] { 1, 2, 3 });

        Task<BlobUploadResponse> upload = transport.SendTransportUploadAsync(
            "https://upload.example.test/{accountId}",
            UploadRequest(content, true, 3, "image/png"),
            cancellation.Token);

        Assert.That(async () => await upload, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.IsCanceled, Is.True);
            Assert.That(content.WasDisposed, Is.False);
            Assert.That(script.Requests, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void InconsistentTypedUploadResponseIsRejectedAsProtocolFailure()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        script.Responses.Enqueue(JsonResponse(
            HttpStatusCode.Created,
            "{\"accountId\":\"other\",\"blobId\":\"blob-1\",\"type\":\"image/png\",\"size\":3}"));
        JmapHttpTransport transport = Transport(script, null);

        JmapProtocolException error = Assert.ThrowsAsync<JmapProtocolException>(
            async () => await transport.SendTransportUploadAsync(
                "https://upload.example.test/{accountId}",
                UploadRequest(new MemoryStream(new byte[] { 1, 2, 3 }), true, 3, "image/png"),
                CancellationToken.None))!;

        Assert.That(error.Path, Is.EqualTo("accountId"));
    }

    [Test]
    public void UploadPostDoesNotFollowRedirects()
    {
        UploadHttpHandler script = new UploadHttpHandler();
        HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
        redirect.Headers.Location = new Uri("https://other.test/upload");
        redirect.Content = new StringContent("", Encoding.UTF8, JmapMediaTypes.Json);
        script.Responses.Enqueue(redirect);
        JmapHttpTransport transport = Transport(script, new UploadAuthorizationProvider());

        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(
            async () => await transport.SendTransportUploadAsync(
                "https://upload.example.test/{accountId}",
                UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1, "image/png"),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(307));
            Assert.That(script.Requests, Has.Count.EqualTo(1));
        }
    }

    private static JmapHttpTransport Transport(
        UploadHttpHandler script,
        IJmapAuthorizationProvider? authorizationProvider)
    {
        JmapHttpTransport transport = new JmapHttpTransport(new JmapDiscoveryOptions(), authorizationProvider);
        transport.Client = new HttpClient(script);
        return transport;
    }

    private static BlobUploadRequest UploadRequest(
        Stream content,
        bool hasContentLength,
        long contentLength,
        string mediaType)
    {
        BlobUploadRequest request = new BlobUploadRequest();
        request.AccountId = "account_1";
        request.MediaType = mediaType;
        request.Content = content;
        request.HasContentLength = hasContentLength;
        request.ContentLength = contentLength;
        return request;
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(json, Encoding.UTF8, JmapMediaTypes.Json);
        return response;
    }

    private static HttpResponseMessage ProblemResponse(HttpStatusCode status, string json)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(json, Encoding.UTF8, JmapMediaTypes.ProblemJson);
        return response;
    }

    private sealed class RecordedUpload
    {
        public string? Uri;
        public string? Method;
        public string? ContentType;
        public bool HasContentLength;
        public long ContentLength;
        public bool IsChunked;
        public byte[] Body = Array.Empty<byte>();
        public string? Accept;
        public string? Authorization;
    }

    private sealed class UploadHttpHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses = new Queue<HttpResponseMessage>();
        public List<RecordedUpload> Requests = new List<RecordedUpload>();
        public CancellationTokenSource? CancelSource;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RecordedUpload recorded = new RecordedUpload();
            recorded.Uri = request.RequestUri!.AbsoluteUri;
            recorded.Method = request.Method.Method;
            recorded.ContentType = request.Content!.Headers.GetValues("Content-Type").Single();
            recorded.HasContentLength = request.Content.Headers.ContentLength.HasValue;
            recorded.ContentLength = recorded.HasContentLength ? request.Content.Headers.ContentLength.GetValueOrDefault() : 0;
            recorded.IsChunked = request.Headers.TransferEncodingChunked.GetValueOrDefault();
            recorded.Accept = string.Join(", ", request.Headers.Accept.Select(value => value.MediaType));
            recorded.Authorization = request.Headers.Authorization?.ToString();
            Requests.Add(recorded);
            if (CancelSource != null)
            {
                CancelSource.Cancel();
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            recorded.Body = request.Content.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(Responses.Dequeue());
        }
    }

    private sealed class UploadAuthorizationProvider : IJmapAuthorizationProvider
    {
        public List<string> ResourceUrls = new List<string>();
        public string Value = "Bearer upload-secret";
        public Action? OnGet;

        public Task<string> GetAuthorizationValueAsync(string resourceUrl, CancellationToken cancellationToken)
        {
            ResourceUrls.Add(resourceUrl);
            if (OnGet != null)
            {
                OnGet();
            }

            return Task.FromResult(Value);
        }
    }

    private sealed class TrackingUploadStream : MemoryStream
    {
        public bool WasDisposed;

        public TrackingUploadStream(byte[] bytes) : base(bytes)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class NonSeekableUploadStream : MemoryStream
    {
        public bool WasDisposed;

        public NonSeekableUploadStream(byte[] bytes) : base(bytes)
        {
        }

        public override bool CanSeek => false;

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
