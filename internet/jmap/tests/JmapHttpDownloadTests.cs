using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace jmap.Tests;

public class JmapHttpDownloadTests
{
    [Test]
    public void MissingRequestRelativeExpansionAndUnsafeAuthorizationAreRejected()
    {
        JmapHttpTransport validation = Transport(new DownloadHttpHandler(), null);
        Assert.That(() => validation.ValidateDownloadRequest(null!),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(() => validation.SendDownload(
            "{accountId}/{blobId}/{name}?type={type}",
            Request("application/octet-stream", "blob.bin"), CancellationToken.None),
            Throws.TypeOf<JmapValidationException>());

        DownloadHttpHandler script = new DownloadHttpHandler();
        DownloadAuthorizationProvider authorization = new DownloadAuthorizationProvider();
        authorization.Value = "Bearer secret\nInjected: yes";
        JmapHttpTransport transport = Transport(script, authorization);
        Assert.That(async () => await transport.SendTransportDownloadAsync(
            "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
            Request("application/octet-stream", "blob.bin"), CancellationToken.None),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(script.Requests, Is.Empty);
    }

    [Test]
    public void UnexpectedSuccessStatusAndFilenameSafetyAreEnforced()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        LazyDownloadContent content = new LazyDownloadContent(
            new byte[] { 1 }, "application/octet-stream", true);
        HttpResponseMessage created = new HttpResponseMessage(HttpStatusCode.Created);
        created.Content = content;
        script.Responses.Enqueue(created);
        JmapHttpTransport transport = Transport(script, null);

        Assert.That(async () => await transport.SendTransportDownloadAsync(
            "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
            Request("application/octet-stream", "blob.bin"), CancellationToken.None),
            Throws.TypeOf<JmapTransportException>());

        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(Array.Empty<byte>());
        response.Content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(
            "attachment; filename=\"file.bin\"");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.GetDownloadFileName(response), Is.EqualTo("file.bin"));
            Assert.That(transport.IsSafeFileName(null), Is.False);
            Assert.That(transport.IsSafeFileName(""), Is.False);
            Assert.That(transport.IsSafeFileName("."), Is.False);
            Assert.That(transport.IsSafeFileName(".."), Is.False);
            Assert.That(transport.IsSafeFileName("safe.bin"), Is.True);
            Assert.That(transport.IsSafeFileName("bad/name"), Is.False);
            Assert.That(transport.IsSafeFileName("bad\\name"), Is.False);
            Assert.That(transport.IsSafeFileName("bad\"name"), Is.False);
            Assert.That(transport.IsSafeFileName("bad\u007fname"), Is.False);
        }
    }

    [Test]
    public void DownloadRejectsMissingContentTypeAndUnsafeFilename()
    {
        DownloadHttpHandler missingTypeScript = new DownloadHttpHandler();
        HttpResponseMessage missingType = new HttpResponseMessage(HttpStatusCode.OK);
        missingType.Content = new ByteArrayContent(new byte[] { 1 });
        missingTypeScript.Responses.Enqueue(missingType);
        JmapProtocolException typeError = Assert.ThrowsAsync<JmapProtocolException>(async () =>
            await Transport(missingTypeScript, null).SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                Request("application/octet-stream", "blob.bin"), CancellationToken.None))!;

        DownloadHttpHandler nameScript = new DownloadHttpHandler();
        LazyDownloadContent namedContent = new LazyDownloadContent(
            new byte[] { 1 }, "application/octet-stream", true);
        namedContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
        namedContent.Headers.ContentDisposition.FileName = "bad/name";
        HttpResponseMessage badName = new HttpResponseMessage(HttpStatusCode.OK);
        badName.Content = namedContent;
        nameScript.Responses.Enqueue(badName);
        JmapProtocolException nameError = Assert.ThrowsAsync<JmapProtocolException>(async () =>
            await Transport(nameScript, null).SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                Request("application/octet-stream", "blob.bin"), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(typeError.Path, Is.EqualTo("type"));
            Assert.That(nameError.Path, Is.EqualTo("name"));
        }
    }

    [Test]
    public async Task DownloadExpandsUrlAndReturnsLazyBinaryStreamWithResponseMetadata()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        LazyDownloadContent content = new LazyDownloadContent(
            Encoding.UTF8.GetBytes("binary body"),
            "application/pdf",
            true);
        content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(
            "attachment; filename*=UTF-8''report%20%C3%A9.pdf");
        HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = content;
        httpResponse.Headers.CacheControl = CacheControlHeaderValue.Parse(
            "private, immutable, max-age=31536000");
        script.Responses.Enqueue(httpResponse);
        DownloadAuthorizationProvider authorization = new DownloadAuthorizationProvider();
        JmapHttpTransport transport = Transport(script, authorization);

        BlobDownloadResponse response = await transport.SendTransportDownloadAsync(
            "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
            Request("application/pdf", "report é.pdf"),
            CancellationToken.None);

        RecordedDownload sent = script.Requests[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent.Method, Is.EqualTo("GET"));
            Assert.That(sent.Uri, Is.EqualTo(
                "https://download.example.test/account_1/blob_1/report%20%C3%A9.pdf?type=application%2Fpdf"));
            Assert.That(sent.Accept, Does.Contain("application/pdf"));
            Assert.That(sent.Accept, Does.Contain(JmapMediaTypes.ProblemJson));
            Assert.That(sent.Authorization, Is.EqualTo("Bearer download-secret"));
            Assert.That(authorization.ResourceUrls, Is.EqualTo(new[] { sent.Uri }));
            Assert.That(response.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(response.FileName, Is.EqualTo("report é.pdf"));
            Assert.That(response.HasContentLength, Is.True);
            Assert.That(response.ContentLength, Is.EqualTo(11));
            Assert.That(response.CacheControl, Does.Contain("private"));
            Assert.That(response.CacheControl, Does.Contain("immutable"));
            Assert.That(response.CacheControl, Does.Contain("max-age=31536000"));
            Assert.That(content.ResponseStream.ReadCount, Is.Zero);
            Assert.That(content.WasDisposed, Is.False);
        }

        using Stream downloaded = response.Content!;
        using MemoryStream copy = new MemoryStream();
        await downloaded.CopyToAsync(copy);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Encoding.UTF8.GetString(copy.ToArray()), Is.EqualTo("binary body"));
            Assert.That(content.ResponseStream.ReadCount, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task UnknownLengthDownloadRemainsStreamingAndAllowsMissingOptionalHeaders()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        LazyDownloadContent content = new LazyDownloadContent(new byte[] { 0, 1, 2, 3 }, "image/png", false);
        HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = content;
        script.Responses.Enqueue(httpResponse);
        JmapHttpTransport transport = Transport(script, null);

        BlobDownloadResponse response = await transport.SendTransportDownloadAsync(
            "https://download.example.test/account_1/blob_1/image.png?type=image%2Fpng",
            Request("image/png", "image.png"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.HasContentLength, Is.False);
            Assert.That(response.ContentLength, Is.Zero);
            Assert.That(response.FileName, Is.Null);
            Assert.That(response.CacheControl, Is.Null);
            Assert.That(content.ResponseStream.ReadCount, Is.Zero);
        }

        using Stream downloaded = response.Content!;
        byte[] bytes = new byte[4];
        int count = await downloaded.ReadAsync(bytes);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(count, Is.EqualTo(4));
            Assert.That(bytes, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        }
    }

    [Test]
    public void DownloadRequestValidationReportsAllUnsafeFieldsBeforeDispatch()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        DownloadAuthorizationProvider authorization = new DownloadAuthorizationProvider();
        JmapHttpTransport transport = Transport(script, authorization);
        BlobDownloadRequest request = new BlobDownloadRequest();
        request.AccountId = "bad/account";
        request.BlobId = "bad blob";
        request.Type = "text/plain\r\nX-Injected: yes";
        request.Name = "../secret.txt";

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await transport.SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                request,
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties, Is.EqualTo(new[] { "accountId", "blobId", "type", "name" }));
            Assert.That(script.Requests, Is.Empty);
            Assert.That(authorization.ResourceUrls, Is.Empty);
        }
    }

    [Test]
    public void ProblemResponseIsDecodedAndResponseResourcesAreDisposed()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        OwnedErrorContent content = new OwnedErrorContent(
            "{\"type\":\"urn:ietf:params:jmap:error:limit\",\"status\":403,\"limit\":\"downloadAccess\"}");
        HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);
        httpResponse.Content = content;
        script.Responses.Enqueue(httpResponse);
        JmapHttpTransport transport = Transport(script, null);

        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(
            async () => await transport.SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                Request("application/octet-stream", "blob.bin"),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(403));
            Assert.That(error.Problem!.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(error.Problem.Limit, Is.EqualTo("downloadAccess"));
            Assert.That(content.WasDisposed, Is.True);
            Assert.That(content.ResponseStream.WasDisposed, Is.True);
        }
    }

    [TestCase("text/plain", "other.txt", "name")]
    [TestCase("image/png", null, "type")]
    public void InconsistentResponseMetadataIsRejectedAndDisposed(
        string responseType,
        string? responseName,
        string expectedPath)
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        LazyDownloadContent content = new LazyDownloadContent(new byte[] { 1 }, responseType, true);
        if (responseName != null)
        {
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            content.Headers.ContentDisposition.FileName = responseName;
        }

        HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = content;
        script.Responses.Enqueue(httpResponse);
        JmapHttpTransport transport = Transport(script, null);

        JmapProtocolException error = Assert.ThrowsAsync<JmapProtocolException>(
            async () => await transport.SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                Request("text/plain", "expected.txt"),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Path, Is.EqualTo(expectedPath));
            Assert.That(content.WasDisposed, Is.True);
        }
    }

    [Test]
    public void CancellationDuringDispatchReturnsCanceledTaskWithoutAResponseStream()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        DownloadHttpHandler script = new DownloadHttpHandler();
        script.CancelSource = cancellation;
        JmapHttpTransport transport = Transport(script, null);

        Task<BlobDownloadResponse> download = transport.SendTransportDownloadAsync(
            "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
            Request("application/octet-stream", "blob.bin"),
            cancellation.Token);

        Assert.That(async () => await download, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(download.IsCanceled, Is.True);
            Assert.That(script.Requests, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void StreamAcquisitionFailureIsMappedAndDisposesTheResponse()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        FailingDownloadContent content = new FailingDownloadContent(new IOException("broken response stream"));
        HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = content;
        script.Responses.Enqueue(httpResponse);
        JmapHttpTransport transport = Transport(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await transport.SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                Request("application/octet-stream", "blob.bin"),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(200));
            Assert.That(error.IsRetryable, Is.True);
            Assert.That(error.InnerException, Is.TypeOf<IOException>());
            Assert.That(content.WasDisposed, Is.True);
        }
    }

    [Test]
    public void DownloadGetDoesNotFollowRedirects()
    {
        DownloadHttpHandler script = new DownloadHttpHandler();
        HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
        redirect.Headers.Location = new Uri("https://untrusted.example.test/download");
        redirect.Content = new StringContent("");
        script.Responses.Enqueue(redirect);
        JmapHttpTransport transport = Transport(script, new DownloadAuthorizationProvider());

        JmapRequestException error = Assert.ThrowsAsync<JmapRequestException>(
            async () => await transport.SendTransportDownloadAsync(
                "https://download.example.test/{accountId}/{blobId}/{name}?type={type}",
                Request("application/octet-stream", "blob.bin"),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.HttpStatus, Is.EqualTo(307));
            Assert.That(script.Requests, Has.Count.EqualTo(1));
        }
    }

    private static JmapHttpTransport Transport(
        DownloadHttpHandler script,
        IJmapAuthorizationProvider? authorizationProvider)
    {
        JmapHttpTransport transport = new JmapHttpTransport(new JmapDiscoveryOptions(), authorizationProvider);
        transport.Client = new HttpClient(script);
        return transport;
    }

    private static BlobDownloadRequest Request(string type, string name)
    {
        BlobDownloadRequest request = new BlobDownloadRequest();
        request.AccountId = "account_1";
        request.BlobId = "blob_1";
        request.Type = type;
        request.Name = name;
        return request;
    }

    private sealed class RecordedDownload
    {
        public string? Method;
        public string? Uri;
        public string? Accept;
        public string? Authorization;
    }

    private sealed class DownloadHttpHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses = new Queue<HttpResponseMessage>();
        public List<RecordedDownload> Requests = new List<RecordedDownload>();
        public CancellationTokenSource? CancelSource;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RecordedDownload recorded = new RecordedDownload();
            recorded.Method = request.Method.Method;
            recorded.Uri = request.RequestUri!.AbsoluteUri;
            recorded.Accept = string.Join(", ", request.Headers.Accept.Select(value => value.MediaType));
            recorded.Authorization = request.Headers.Authorization?.ToString();
            Requests.Add(recorded);
            if (CancelSource != null)
            {
                CancelSource.Cancel();
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            return Task.FromResult(Responses.Dequeue());
        }
    }

    private sealed class DownloadAuthorizationProvider : IJmapAuthorizationProvider
    {
        public List<string> ResourceUrls = new List<string>();
        public string Value = "Bearer download-secret";

        public Task<string> GetAuthorizationValueAsync(string resourceUrl, CancellationToken cancellationToken)
        {
            ResourceUrls.Add(resourceUrl);
            return Task.FromResult(Value);
        }
    }

    private sealed class LazyDownloadContent : HttpContent
    {
        public TrackingDownloadStream ResponseStream;
        public bool HasLength;
        public bool WasDisposed;

        public LazyDownloadContent(byte[] bytes, string mediaType, bool hasLength)
        {
            ResponseStream = new TrackingDownloadStream(bytes);
            HasLength = hasLength;
            Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            if (hasLength)
            {
                Headers.ContentLength = bytes.Length;
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return ResponseStream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = ResponseStream.Length;
            return HasLength;
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

    private sealed class OwnedErrorContent : HttpContent
    {
        public TrackingDownloadStream ResponseStream;
        public bool WasDisposed;

        public OwnedErrorContent(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ResponseStream = new TrackingDownloadStream(bytes);
            Headers.ContentType = new MediaTypeHeaderValue(JmapMediaTypes.ProblemJson);
            Headers.ContentLength = bytes.Length;
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

    private sealed class FailingDownloadContent : HttpContent
    {
        public Exception Failure;
        public bool WasDisposed;

        public FailingDownloadContent(Exception failure)
        {
            Failure = failure;
            Headers.ContentType = new MediaTypeHeaderValue(JmapMediaTypes.OctetStream);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return Task.FromException(Failure);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromException<Stream>(Failure);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingDownloadStream : MemoryStream
    {
        public int ReadCount;
        public bool WasDisposed;

        public TrackingDownloadStream(byte[] bytes) : base(bytes)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount = ReadCount + 1;
            return base.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            ReadCount = ReadCount + 1;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
