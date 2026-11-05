using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace jmap.Tests;

public class JmapHttpEventSourceTests
{
    [Test]
    public void EventSourceRejectsUnsafeAuthorizationBeforeDispatch()
    {
        EventSourceHttpHandler handler = new EventSourceHttpHandler();
        EventSourceAuthorization authorization = new EventSourceAuthorization();
        authorization.Value = "Bearer secret\rInjected: yes";
        JmapHttpTransport transport = Transport(handler, authorization);

        Assert.That(async () => await transport.OpenTransportEventSourceAsync(
            "https://events.example.test/stream", null, CancellationToken.None),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task EventSourceGetUsesStreamingNegotiationAuthorizationAndResumeHeader()
    {
        EventSourceHttpHandler handler = new EventSourceHttpHandler();
        LazyEventSourceContent content = new LazyEventSourceContent("event: ping\ndata: {\"interval\":30}\n\n", JmapMediaTypes.EventStream);
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = content;
        handler.Responses.Enqueue(response);
        EventSourceAuthorization authorization = new EventSourceAuthorization();
        JmapHttpTransport transport = Transport(handler, authorization);

        Stream stream = await transport.OpenTransportEventSourceAsync(
            "https://events.example.test/stream",
            "event-9",
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Requests, Has.Count.EqualTo(1));
            Assert.That(handler.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(handler.Requests[0].Accept, Is.EqualTo(JmapMediaTypes.EventStream));
            Assert.That(handler.Requests[0].Authorization, Is.EqualTo("Bearer event-secret"));
            Assert.That(handler.Requests[0].LastEventId, Is.EqualTo("event-9"));
            Assert.That(handler.Requests[0].CacheControl, Does.Contain("no-cache"));
            Assert.That(authorization.Urls, Is.EqualTo(new[] { "https://events.example.test/stream" }));
            Assert.That(content.ReadCount, Is.Zero);
        }

        byte[] buffer = new byte[128];
        int read = await stream.ReadAsync(buffer);
        Assert.That(read, Is.GreaterThan(0));
        stream.Dispose();
        Assert.That(content.Stream.WasDisposed, Is.True);
    }

    [Test]
    public void EventSourceRejectsUnsafeResumeIdBeforeAuthorizationOrDispatch()
    {
        EventSourceHttpHandler handler = new EventSourceHttpHandler();
        EventSourceAuthorization authorization = new EventSourceAuthorization();
        JmapHttpTransport transport = Transport(handler, authorization);

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await transport.OpenTransportEventSourceAsync(
                "https://events.example.test/stream",
                "safe\r\nX-Injected: yes",
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties, Is.EqualTo(new[] { "lastEventId" }));
            Assert.That(handler.Requests, Is.Empty);
            Assert.That(authorization.Urls, Is.Empty);
        }
    }

    [Test]
    public void EventSourceStatusAndMediaTypeFailuresAreSurfacedAndDisposed()
    {
        EventSourceHttpHandler statusHandler = new EventSourceHttpHandler();
        LazyEventSourceContent statusContent = new LazyEventSourceContent("temporarily unavailable", "text/plain");
        HttpResponseMessage unavailable = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        unavailable.Content = statusContent;
        statusHandler.Responses.Enqueue(unavailable);
        JmapHttpTransport statusTransport = Transport(statusHandler, null);

        JmapTransportException statusError = Assert.ThrowsAsync<JmapTransportException>(
            async () => await statusTransport.OpenTransportEventSourceAsync(
                "https://events.example.test/stream",
                null,
                CancellationToken.None))!;

        EventSourceHttpHandler typeHandler = new EventSourceHttpHandler();
        LazyEventSourceContent typeContent = new LazyEventSourceContent("not events", "text/plain");
        HttpResponseMessage wrongType = new HttpResponseMessage(HttpStatusCode.OK);
        wrongType.Content = typeContent;
        typeHandler.Responses.Enqueue(wrongType);
        JmapHttpTransport typeTransport = Transport(typeHandler, null);

        JmapProtocolException typeError = Assert.ThrowsAsync<JmapProtocolException>(
            async () => await typeTransport.OpenTransportEventSourceAsync(
                "https://events.example.test/stream",
                null,
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statusError.HttpStatus, Is.EqualTo(503));
            Assert.That(statusError.IsRetryable, Is.True);
            Assert.That(Encoding.UTF8.GetString(statusError.ResponseBody!), Is.EqualTo("temporarily unavailable"));
            Assert.That(statusContent.WasDisposed, Is.True);
            Assert.That(typeError.Path, Is.EqualTo("contentType"));
            Assert.That(typeContent.WasDisposed, Is.True);
        }
    }

    private static JmapHttpTransport Transport(
        EventSourceHttpHandler handler,
        IJmapAuthorizationProvider? authorization)
    {
        JmapHttpTransport transport = new JmapHttpTransport(new JmapDiscoveryOptions(), authorization);
        transport.Client = new HttpClient(handler);
        return transport;
    }

    private sealed class RecordedEventSourceRequest
    {
        public string? Method;
        public string? Accept;
        public string? Authorization;
        public string? LastEventId;
        public string? CacheControl;
    }

    private sealed class EventSourceHttpHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses = new Queue<HttpResponseMessage>();
        public List<RecordedEventSourceRequest> Requests = new List<RecordedEventSourceRequest>();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RecordedEventSourceRequest recorded = new RecordedEventSourceRequest();
            recorded.Method = request.Method.Method;
            recorded.Accept = string.Join(", ", request.Headers.Accept.Select(value => value.MediaType));
            recorded.Authorization = request.Headers.Authorization?.ToString();
            if (request.Headers.TryGetValues("Last-Event-ID", out IEnumerable<string>? values))
            {
                recorded.LastEventId = values.Single();
            }

            recorded.CacheControl = request.Headers.CacheControl?.ToString();
            Requests.Add(recorded);
            return Task.FromResult(Responses.Dequeue());
        }
    }

    private sealed class EventSourceAuthorization : IJmapAuthorizationProvider
    {
        public List<string> Urls = new List<string>();
        public string Value = "Bearer event-secret";

        public Task<string> GetAuthorizationValueAsync(string resourceUrl, CancellationToken cancellationToken)
        {
            Urls.Add(resourceUrl);
            return Task.FromResult(Value);
        }
    }

    private sealed class LazyEventSourceContent : HttpContent
    {
        public TrackingEventSourceStream Stream;
        public int ReadCount;
        public bool WasDisposed;

        public LazyEventSourceContent(string body, string mediaType)
        {
            Stream = new TrackingEventSourceStream(Encoding.UTF8.GetBytes(body), this);
            Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return Stream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(Stream);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingEventSourceStream : MemoryStream
    {
        public LazyEventSourceContent Owner;
        public bool WasDisposed;

        public TrackingEventSourceStream(byte[] bytes, LazyEventSourceContent owner) : base(bytes)
        {
            Owner = owner;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            Owner.ReadCount = Owner.ReadCount + 1;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
