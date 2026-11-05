using System.Net;
using System.Net.Sockets;
using System.Text;
using iso20022;

namespace iso20022.Tests;

public class ArtifactAcquisitionApiTests
{
    [Test]
    public void LoopbackServerValidatesCacheHeadersAcrossExplicitRedirects()
    {
        LocalHttpResponse redirect = new LocalHttpResponse
        {
            StatusCode = 302,
            ReasonPhrase = "Found"
        };
        redirect.Headers.Add("Location", "/catalogues/current");
        LocalHttpResponse unchanged = new LocalHttpResponse
        {
            StatusCode = 304,
            ReasonPhrase = "Not Modified"
        };
        unchanged.Headers.Add("ETag", "\"catalogue-2026-q1\"");
        using LocalHttpFixture fixture = new LocalHttpFixture(redirect, unchanged);
        using HttpClient httpClient = CreateHttpClient();
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest request = CreateDownloadRequest(fixture.BaseUri + "catalogues/latest");
        request.IfNoneMatch = "\"catalogue-2026-q1\"";
        request.IfModifiedSince = CreateDateTime(2026, 5, 27, 13, 28, 0);

        CatalogueDownloadResult result = client.DownloadCatalogue(request);
        fixture.WaitForCompletion();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.NotModified, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(304));
            Assert.That(result.FinalUri, Is.EqualTo(fixture.BaseUri + "catalogues/current"));
            Assert.That(result.RedirectUris,
                Is.EqualTo(new[] { fixture.BaseUri + "catalogues/current" }));
            Assert.That(result.EntityTag, Is.EqualTo("\"catalogue-2026-q1\""));
            Assert.That(fixture.Requests, Has.Count.EqualTo(2));
            Assert.That(fixture.Requests[0].Path, Is.EqualTo("/catalogues/latest"));
            Assert.That(fixture.Requests[1].Path, Is.EqualTo("/catalogues/current"));
            Assert.That(fixture.Requests[0].Headers["If-None-Match"],
                Is.EqualTo("\"catalogue-2026-q1\""));
            Assert.That(fixture.Requests[1].Headers["If-None-Match"],
                Is.EqualTo("\"catalogue-2026-q1\""));
            Assert.That(fixture.Requests[0].Headers["If-Modified-Since"],
                Is.EqualTo("Wed, 27 May 2026 13:28:00 GMT"));
            Assert.That(result.Content, Is.Null);
        });
    }

    [Test]
    public void LoopbackServerRejectsBadMediaTruncatedPartialContentAndHashMismatch()
    {
        LocalHttpResponse badMedia = CreateContentResponse(
            200,
            "OK",
            "not a catalogue"u8.ToArray(),
            "text/plain");
        LocalHttpResponse truncated = CreateContentResponse(
            206,
            "Partial Content",
            new byte[] { 1, 2, 3 },
            Iso20022MediaTypes.Zip);
        truncated.Headers.Add("Content-Range", "bytes 0-2/10");
        byte[] downloaded = "catalogue bytes"u8.ToArray();
        LocalHttpResponse mismatched = CreateContentResponse(
            200,
            "OK",
            downloaded,
            Iso20022MediaTypes.Zip);
        using LocalHttpFixture fixture = new LocalHttpFixture(badMedia, truncated, mismatched);
        using HttpClient httpClient = CreateHttpClient();
        ICatalogueClient client = new CatalogueClient(httpClient);

        CatalogueDownloadResult mediaResult = client.DownloadCatalogue(
            CreateDownloadRequest(fixture.BaseUri + "bad-media"));
        CatalogueDownloadResult truncatedResult = client.DownloadCatalogue(
            CreateDownloadRequest(fixture.BaseUri + "truncated"));
        CatalogueDownloadRequest hashRequest = CreateDownloadRequest(fixture.BaseUri + "hash");
        hashRequest.ExpectedHash = new IsoBinaryValue { Value = new byte[32] };
        CatalogueDownloadResult hashResult = client.DownloadCatalogue(hashRequest);
        fixture.WaitForCompletion();

        Assert.Multiple(() =>
        {
            Assert.That(mediaResult.Success, Is.False);
            Assert.That(mediaResult.ErrorCode, Is.EqualTo("catalogue.media_type_rejected"));
            Assert.That(mediaResult.Content, Is.Null);
            Assert.That(truncatedResult.Success, Is.False);
            Assert.That(truncatedResult.ErrorCode, Is.EqualTo("catalogue.partial_content"));
            Assert.That(truncatedResult.Content, Is.Null);
            Assert.That(hashResult.Success, Is.False);
            Assert.That(hashResult.ErrorCode, Is.EqualTo("catalogue.hash_mismatch"));
            Assert.That(hashResult.Content, Is.Null);
            Assert.That(fixture.Requests.Select(item => item.Path),
                Is.EqualTo(new[] { "/bad-media", "/truncated", "/hash" }));
        });
    }

    [Test]
    public async Task LoopbackServerDownloadObservesCancellationWhileStreaming()
    {
        byte[] body = new byte[163840];
        Array.Fill(body, (byte)65);
        ManualResetEventSlim firstChunkSent = new ManualResetEventSlim();
        LocalHttpResponse response = CreateContentResponse(
            200,
            "OK",
            body,
            Iso20022MediaTypes.Zip);
        response.FirstChunkLength = 1;
        response.DelayAfterFirstChunkMilliseconds = 200;
        response.FirstChunkSent = firstChunkSent;
        response.IgnoreClientDisconnect = true;
        using LocalHttpFixture fixture = new LocalHttpFixture(response);
        using HttpClient httpClient = CreateHttpClient();
        ICatalogueClient client = new CatalogueClient(httpClient);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        CatalogueDownloadRequest request = CreateDownloadRequest(fixture.BaseUri + "slow");
        request.CancellationToken = cancellation.Token;

        Task<CatalogueDownloadResult> download = Task.Run(() => client.DownloadCatalogue(request));
        Assert.That(firstChunkSent.Wait(TimeSpan.FromSeconds(5)), Is.True);
        cancellation.Cancel();

        Assert.That(
            async () => await download,
            Throws.InstanceOf<OperationCanceledException>());
        await fixture.WaitForCompletionAsync();
        Assert.That(fixture.Requests.Select(item => item.Path), Is.EqualTo(new[] { "/slow" }));
    }

    [Test]
    public void ArtifactStoreCancellationLeavesNoPublishedOrStagedCoordinate()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-artifact-atomic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            ArtifactStoreRequest request = CreateStoreRequest(root);
            request.CancellationToken = cancellation.Token;
            IArtifactStore store = new FileArtifactStore();
            using CancelAfterReadStream input = new CancelAfterReadStream(
                "content that must never be published"u8.ToArray(),
                cancellation);

            Assert.That(
                () => store.StoreArtifactStream(input, request),
                Throws.InstanceOf<OperationCanceledException>());

            request.CancellationToken = default;
            ArtifactStoreResult opened = store.OpenArtifact(request);
            Assert.Multiple(() =>
            {
                Assert.That(opened.Success, Is.True, opened.ErrorMessage);
                Assert.That(opened.Found, Is.False);
                Assert.That(
                    Directory.GetDirectories(root, ".staging-*", SearchOption.AllDirectories),
                    Is.Empty);
                Assert.That(
                    Directory.GetFiles(root, "artifact.bin", SearchOption.AllDirectories),
                    Is.Empty);
                Assert.That(
                    Directory.GetFiles(root, "manifest.dat", SearchOption.AllDirectories),
                    Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false
        };
        return new HttpClient(handler);
    }

    private static CatalogueDownloadRequest CreateDownloadRequest(string uri)
    {
        CatalogueDownloadRequest request = new CatalogueDownloadRequest
        {
            Uri = uri,
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.ExternalCodeSet,
                Edition = "2026-Q1",
                Identifier = "external-code-sets",
                Version = "1"
            },
            MaxArtifactBytes = 1048576
        };
        request.AcceptedMediaTypes.Add(Iso20022MediaTypes.Zip);
        return request;
    }

    private static ArtifactStoreRequest CreateStoreRequest(string root)
    {
        ArtifactCoordinate coordinate = new ArtifactCoordinate
        {
            Kind = ArtifactKind.ExternalCodeSet,
            Edition = "2026-Q1",
            Identifier = "external-code-sets",
            Version = "1"
        };
        return new ArtifactStoreRequest
        {
            RootPath = root,
            Coordinate = coordinate,
            Artifact = new ArtifactManifest
            {
                Coordinate = coordinate,
                OriginUri = "https://example.test/catalogue.zip",
                MediaType = Iso20022MediaTypes.Zip
            },
            MaxArtifactBytes = 4096
        };
    }

    private static IsoDateTimeValue CreateDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        return new IsoDateTimeValue
        {
            Date = new IsoDateValue
            {
                Year = year,
                Month = month,
                Day = day,
                HasTimezone = true,
                IsUtc = true
            },
            Time = new IsoTimeValue
            {
                Hour = hour,
                Minute = minute,
                Second = second,
                HasTimezone = true,
                IsUtc = true
            }
        };
    }

    private static LocalHttpResponse CreateContentResponse(
        int statusCode,
        string reasonPhrase,
        byte[] body,
        string mediaType)
    {
        LocalHttpResponse response = new LocalHttpResponse
        {
            StatusCode = statusCode,
            ReasonPhrase = reasonPhrase,
            Body = body
        };
        response.Headers.Add("Content-Type", mediaType);
        return response;
    }

    private sealed class CancelAfterReadStream : Stream
    {
        private readonly MemoryStream content;
        private readonly CancellationTokenSource cancellation;
        private bool cancelled;

        internal CancelAfterReadStream(byte[] bytes, CancellationTokenSource cancellation)
        {
            content = new MemoryStream(bytes);
            this.cancellation = cancellation;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => content.Length;
        public override long Position
        {
            get => content.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = content.Read(buffer, offset, Math.Min(count, 4));
            if (!cancelled && read > 0)
            {
                cancelled = true;
                cancellation.Cancel();
            }

            return read;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class LocalHttpFixture : IDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource stopping = new CancellationTokenSource();
        private readonly LocalHttpResponse[] responses;
        private readonly Task server;

        internal string BaseUri;
        internal List<LocalHttpRequest> Requests = new List<LocalHttpRequest>();

        internal LocalHttpFixture(params LocalHttpResponse[] responses)
        {
            this.responses = responses;
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndpoint;
            BaseUri = "http://127.0.0.1:" + endpoint.Port + "/";
            server = Task.Run(ServeResponsesAsync);
        }

        internal void WaitForCompletion()
        {
            Assert.That(server.Wait(TimeSpan.FromSeconds(5)), Is.True, "Local HTTP fixture timed out.");
            server.GetAwaiter().GetResult();
        }

        internal async Task WaitForCompletionAsync()
        {
            await server.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private async Task ServeResponsesAsync()
        {
            int index = 0;
            while (index < responses.Length)
            {
                using TcpClient client = await listener.AcceptTcpClientAsync(stopping.Token);
                using NetworkStream stream = client.GetStream();
                LocalHttpRequest request = await ReadRequestAsync(stream);
                lock (Requests)
                {
                    Requests.Add(request);
                }

                LocalHttpResponse response = responses[index];
                try
                {
                    await WriteResponseAsync(stream, response);
                }
                catch (IOException) when (response.IgnoreClientDisconnect)
                {
                }
                catch (SocketException) when (response.IgnoreClientDisconnect)
                {
                }

                index++;
            }
        }

        private async Task<LocalHttpRequest> ReadRequestAsync(NetworkStream stream)
        {
            using StreamReader reader = new StreamReader(
                stream,
                Encoding.ASCII,
                false,
                1024,
                true);
            string requestLine = await reader.ReadLineAsync(stopping.Token) ?? string.Empty;
            string[] requestParts = requestLine.Split(' ');
            LocalHttpRequest request = new LocalHttpRequest();
            if (requestParts.Length > 1)
            {
                request.Path = requestParts[1];
            }

            string? line = await reader.ReadLineAsync(stopping.Token);
            while (line != null && line.Length > 0)
            {
                int separator = line.IndexOf(':');
                if (separator > 0)
                {
                    request.Headers[line.Substring(0, separator)] =
                        line.Substring(separator + 1).Trim();
                }

                line = await reader.ReadLineAsync(stopping.Token);
            }

            return request;
        }

        private static async Task WriteResponseAsync(
            NetworkStream stream,
            LocalHttpResponse response)
        {
            int contentLength = response.DeclaredContentLength >= 0
                ? response.DeclaredContentLength
                : response.Body.Length;
            StringBuilder headers = new StringBuilder();
            headers.Append("HTTP/1.1 ");
            headers.Append(response.StatusCode);
            headers.Append(' ');
            headers.Append(response.ReasonPhrase);
            headers.Append("\r\n");
            foreach (KeyValuePair<string, string> header in response.Headers)
            {
                headers.Append(header.Key);
                headers.Append(": ");
                headers.Append(header.Value);
                headers.Append("\r\n");
            }

            headers.Append("Content-Length: ");
            headers.Append(contentLength);
            headers.Append("\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(Encoding.ASCII.GetBytes(headers.ToString()));
            if (response.FirstChunkLength > 0)
            {
                await stream.WriteAsync(response.Body.AsMemory(0, response.FirstChunkLength));
                await stream.FlushAsync();
                response.FirstChunkSent?.Set();
                await Task.Delay(response.DelayAfterFirstChunkMilliseconds);
                await stream.WriteAsync(response.Body.AsMemory(
                    response.FirstChunkLength,
                    response.Body.Length - response.FirstChunkLength));
            }
            else
            {
                await stream.WriteAsync(response.Body);
            }

            await stream.FlushAsync();
        }

        public void Dispose()
        {
            stopping.Cancel();
            listener.Stop();
            if (!server.IsCompleted)
            {
                try
                {
                    server.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException exception) when (
                    exception.InnerException is OperationCanceledException ||
                    exception.InnerException is SocketException)
                {
                }
            }

            stopping.Dispose();
        }
    }

    private sealed class LocalHttpRequest
    {
        internal string Path = string.Empty;
        internal Dictionary<string, string> Headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class LocalHttpResponse
    {
        internal int StatusCode = 200;
        internal string ReasonPhrase = "OK";
        internal Dictionary<string, string> Headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal byte[] Body = Array.Empty<byte>();
        internal int DeclaredContentLength = -1;
        internal int FirstChunkLength;
        internal int DelayAfterFirstChunkMilliseconds;
        internal ManualResetEventSlim? FirstChunkSent;
        internal bool IgnoreClientDisconnect;
    }
}
