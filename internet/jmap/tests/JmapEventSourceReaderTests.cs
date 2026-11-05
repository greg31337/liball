using System.Text;

namespace jmap.Tests;

public class JmapEventSourceReaderTests
{
    [Test]
    public async Task ConnectExpandsOptionsAndReadsBufferedEventsIncrementally()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        transport.Streams.Enqueue(new ChunkedEventStream(Encoding.UTF8.GetBytes(
            ": ready\n\nevent: state\nid: event-1\ndata: {\"@type\":\"StateChange\",\"changed\":{\"account-1\":{\"Email\":\"email-1\"}}}\n\n"), 3));
        JmapEventSourceReader reader = Reader(transport);
        EventSourceOptions options = new EventSourceOptions
        {
            Types = new List<string> { JmapDataTypes.Email, JmapDataTypes.Mailbox },
            CloseAfterState = true,
            PingIntervalSeconds = 45,
            LastEventId = "prior-event"
        };

        await reader.ConnectEventSourceAsync(options, CancellationToken.None);
        JmapEvent comment = await reader.ReadEventSourceAsync(CancellationToken.None);
        JmapEvent state = await reader.ReadEventSourceAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.Urls.Single(), Is.EqualTo(
                "https://events.example.test/stream?types=Email%2CMailbox&closeafter=state&ping=45"));
            Assert.That(transport.LastEventIds.Single(), Is.EqualTo("prior-event"));
            Assert.That(comment.Kind, Is.EqualTo(JmapEventKind.Comment));
            Assert.That(comment.Comment, Is.EqualTo("ready"));
            Assert.That(state.Kind, Is.EqualTo(JmapEventKind.State));
            Assert.That(state.Id, Is.EqualTo("event-1"));
            Assert.That(state.StateChange!.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("email-1"));
        }

        reader.CloseEventSource();
    }

    [Test]
    public async Task CleanEndReconnectsWithLatestIdAndServerRetryDelay()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        TrackingEventStream first = new TrackingEventStream(EventBytes("event-1", "email-1", true));
        TrackingEventStream second = new TrackingEventStream(EventBytes("event-2", "email-2", false));
        transport.Streams.Enqueue(first);
        transport.Streams.Enqueue(second);
        JmapEventSourceReader reader = Reader(transport);

        await reader.ConnectEventSourceAsync(
            new EventSourceOptions { LastEventId = "prior-event" },
            CancellationToken.None);
        JmapEvent firstEvent = await reader.ReadEventSourceAsync(CancellationToken.None);
        JmapEvent secondEvent = await reader.ReadEventSourceAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstEvent.Id, Is.EqualTo("event-1"));
            Assert.That(firstEvent.HasRetryMilliseconds, Is.True);
            Assert.That(firstEvent.RetryMilliseconds, Is.Zero);
            Assert.That(secondEvent.Id, Is.EqualTo("event-2"));
            Assert.That(transport.LastEventIds, Is.EqualTo(new[] { "prior-event", "event-1" }));
            Assert.That(first.WasDisposed, Is.True);
            Assert.That(second.WasDisposed, Is.False);
        }

        reader.CloseEventSource();
        Assert.That(second.WasDisposed, Is.True);
    }

    [Test]
    public void ReadCancellationAndCloseDisposeTheActiveStream()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        BlockingEventStream stream = new BlockingEventStream();
        transport.Streams.Enqueue(stream);
        JmapEventSourceReader reader = Reader(transport);
        reader.ConnectEventSourceAsync(new EventSourceOptions(), CancellationToken.None).GetAwaiter().GetResult();
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        Task<JmapEvent> reading = reader.ReadEventSourceAsync(cancellation.Token);
        cancellation.Cancel();

        Assert.That(async () => await reading, Throws.InstanceOf<OperationCanceledException>());
        reader.CloseEventSource();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stream.WasDisposed, Is.True);
            Assert.That(
                async () => await reader.ReadEventSourceAsync(CancellationToken.None),
                Throws.TypeOf<InvalidOperationException>());
        }
    }

    [Test]
    public void CloseCancelsAnActiveRead()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        BlockingEventStream stream = new BlockingEventStream();
        transport.Streams.Enqueue(stream);
        JmapEventSourceReader reader = Reader(transport);
        reader.ConnectEventSourceAsync(new EventSourceOptions(), CancellationToken.None).GetAwaiter().GetResult();

        Task<JmapEvent> reading = reader.ReadEventSourceAsync(CancellationToken.None);
        Assert.That(stream.Entered.Wait(5000), Is.True);
        reader.CloseEventSource();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(async () => await reading, Throws.InstanceOf<OperationCanceledException>());
            Assert.That(stream.WasDisposed, Is.True);
        }
    }

    [Test]
    public void StreamFailuresAreMappedWhileMalformedEventsAreSurfaced()
    {
        RecordingEventSourceTransport failingTransport = new RecordingEventSourceTransport();
        failingTransport.Streams.Enqueue(new FailingEventStream(new IOException("broken stream")));
        JmapEventSourceReader failingReader = Reader(failingTransport);
        failingReader.ConnectEventSourceAsync(new EventSourceOptions(), CancellationToken.None).GetAwaiter().GetResult();

        JmapTransportException transportError = Assert.ThrowsAsync<JmapTransportException>(
            async () => await failingReader.ReadEventSourceAsync(CancellationToken.None))!;

        RecordingEventSourceTransport malformedTransport = new RecordingEventSourceTransport();
        malformedTransport.Streams.Enqueue(new MemoryStream(Encoding.UTF8.GetBytes(
            "event: state\ndata: not-json\n\n")));
        JmapEventSourceReader malformedReader = Reader(malformedTransport);
        malformedReader.ConnectEventSourceAsync(new EventSourceOptions(), CancellationToken.None).GetAwaiter().GetResult();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transportError.IsRetryable, Is.True);
            Assert.That(transportError.Endpoint, Does.StartWith("https://events.example.test/stream"));
            Assert.That(transportError.InnerException, Is.TypeOf<IOException>());
            Assert.That(
                async () => await malformedReader.ReadEventSourceAsync(CancellationToken.None),
                Throws.InstanceOf<System.Text.Json.JsonException>());
        }

        failingReader.CloseEventSource();
        malformedReader.CloseEventSource();
    }

    [Test]
    public void InvalidOptionsFailBeforeOpeningTransport()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        JmapEventSourceReader reader = Reader(transport);

        JmapValidationException types = Assert.ThrowsAsync<JmapValidationException>(
            async () => await reader.ConnectEventSourceAsync(
                new EventSourceOptions { Types = new List<string> { JmapDataTypes.Email, JmapDataTypes.Email } },
                CancellationToken.None))!;
        JmapValidationException ping = Assert.ThrowsAsync<JmapValidationException>(
            async () => await reader.ConnectEventSourceAsync(
                new EventSourceOptions { PingIntervalSeconds = -1 },
                CancellationToken.None))!;
        JmapValidationException eventId = Assert.ThrowsAsync<JmapValidationException>(
            async () => await reader.ConnectEventSourceAsync(
                new EventSourceOptions { LastEventId = "bad\nid" },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(types.InvalidProperties, Is.EqualTo(new[] { "types" }));
            Assert.That(ping.InvalidProperties, Is.EqualTo(new[] { "pingIntervalSeconds" }));
            Assert.That(eventId.InvalidProperties, Is.EqualTo(new[] { "lastEventId" }));
            Assert.That(transport.Urls, Is.Empty);
        }
    }

    [Test]
    public void ConstructorOptionsAndUnreadableTransportStreamsAreRejected()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        MemoryStream closed = new MemoryStream();
        closed.Dispose();
        transport.Streams.Enqueue(null!);
        transport.Streams.Enqueue(closed);
        JmapEventSourceReader reader = Reader(transport);
        EventSourceOptions[] invalidOptions =
        {
            new EventSourceOptions { Types = new List<string>() },
            new EventSourceOptions { Types = new List<string> { "*", JmapDataTypes.Email } },
            new EventSourceOptions { Types = new List<string> { "Vendor,Type" } },
            new EventSourceOptions
            {
                LastEventId = new string('x', EventSourceParser.MaximumLineCharacters + 1)
            }
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new JmapEventSourceReader(null!, transport),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new JmapEventSourceReader("https://events.example.test", null!),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(async () => await reader.ConnectEventSourceAsync(
                null!, CancellationToken.None), Throws.TypeOf<JmapValidationException>());
            foreach (EventSourceOptions invalid in invalidOptions)
            {
                Assert.That(async () => await reader.ConnectEventSourceAsync(
                    invalid, CancellationToken.None), Throws.TypeOf<JmapValidationException>());
            }
            Assert.That(async () => await reader.ConnectEventSourceAsync(
                new EventSourceOptions(), CancellationToken.None),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(async () => await reader.ConnectEventSourceAsync(
                new EventSourceOptions(), CancellationToken.None),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public async Task ReconnectingOrReplacingConnectionsDisposesStreamsAndHonorsCancellation()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        TrackingEventStream first = new TrackingEventStream(EventBytes("one", "s1", false));
        TrackingEventStream replacement = new TrackingEventStream(EventBytes("two", "s2", false));
        transport.Streams.Enqueue(first);
        transport.Streams.Enqueue(replacement);
        JmapEventSourceReader reader = Reader(transport);

        await reader.ConnectEventSourceAsync(new EventSourceOptions(), CancellationToken.None);
        await reader.ConnectEventSourceAsync(new EventSourceOptions(), CancellationToken.None);
        JmapEvent value = await reader.ReadEventSourceAsync(CancellationToken.None);
        reader.CloseEventSource();

        JmapEventSourceReader reconnecting = Reader(new RecordingEventSourceTransport());
        reconnecting.ReconnectDelayMilliseconds = -1;
        using CancellationTokenSource canceled = new CancellationTokenSource();
        canceled.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.WasDisposed, Is.True);
            Assert.That(value.Id, Is.EqualTo("two"));
            Assert.That(replacement.WasDisposed, Is.True);
            Assert.That(() => reconnecting.Reconnect(canceled.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(() => reconnecting.RequireActiveStream(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => reconnecting.RequireParser(),
                Throws.TypeOf<InvalidOperationException>());
        }
    }

    [Test]
    public void ReconnectValidatesReplacementStreamsAndClosedReaderState()
    {
        RecordingEventSourceTransport invalidTransport = new RecordingEventSourceTransport();
        MemoryStream closed = new MemoryStream();
        closed.Dispose();
        invalidTransport.Streams.Enqueue(null!);
        invalidTransport.Streams.Enqueue(closed);
        JmapEventSourceReader invalidReader = Reader(invalidTransport);
        invalidReader.ReconnectDelayMilliseconds = 0;
        Assert.That(() => invalidReader.Reconnect(CancellationToken.None),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => invalidReader.Reconnect(CancellationToken.None),
            Throws.TypeOf<JmapProtocolException>());

        RecordingEventSourceTransport closedTransport = new RecordingEventSourceTransport();
        TrackingEventStream discarded = new TrackingEventStream(EventBytes("new", "s2", false));
        closedTransport.Streams.Enqueue(discarded);
        JmapEventSourceReader closedReader = Reader(closedTransport);
        closedReader.ReconnectDelayMilliseconds = 0;
        closedReader.ClosedByCaller = true;
        Assert.That(() => closedReader.Reconnect(CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>());

        RecordingEventSourceTransport replacementTransport = new RecordingEventSourceTransport();
        TrackingEventStream old = new TrackingEventStream(EventBytes("old", "s1", false));
        TrackingEventStream replacement = new TrackingEventStream(EventBytes("new", "s2", false));
        replacementTransport.Streams.Enqueue(replacement);
        JmapEventSourceReader replacementReader = Reader(replacementTransport);
        replacementReader.ExpandedUrl = null;
        replacementReader.ActiveStream = old;
        replacementReader.ConnectionCancellation = new CancellationTokenSource();
        replacementReader.ReconnectDelayMilliseconds = 0;
        replacementReader.Reconnect(CancellationToken.None);
        replacementReader.ExpandedUrl = null;
        JmapEvent reconnected = replacementReader.ReadEventSourceAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        replacementReader.CloseEventSource();

        RecordingEventSourceTransport canceledTransport = new RecordingEventSourceTransport();
        TrackingEventStream canceledStream = new TrackingEventStream(EventBytes("unused", "s3", false));
        using CancellationTokenSource canceledDuringOpen = new CancellationTokenSource();
        canceledTransport.Streams.Enqueue(canceledStream);
        canceledTransport.CancelAfterOpen = canceledDuringOpen;
        JmapEventSourceReader canceledReader = Reader(canceledTransport);
        canceledReader.ReconnectDelayMilliseconds = 0;
        Assert.That(() => canceledReader.Reconnect(canceledDuringOpen.Token),
            Throws.InstanceOf<OperationCanceledException>());

        using CancellationTokenSource canceled = new CancellationTokenSource();
        canceled.Cancel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(discarded.WasDisposed, Is.True);
            Assert.That(old.WasDisposed, Is.True);
            Assert.That(replacement.WasDisposed, Is.True);
            Assert.That(reconnected.Id, Is.EqualTo("new"));
            Assert.That(canceledStream.WasDisposed, Is.True);
            Assert.That(() => replacementReader.WaitForReconnectDelay(
                (long)int.MaxValue + 1L, canceled.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }
    }

    [Test]
    public async Task WildcardEventTypeIsAcceptedAsTheOnlyFilter()
    {
        RecordingEventSourceTransport transport = new RecordingEventSourceTransport();
        transport.Streams.Enqueue(new MemoryStream(EventBytes("one", "s1", false)));
        JmapEventSourceReader reader = Reader(transport);

        await reader.ConnectEventSourceAsync(
            new EventSourceOptions { Types = new List<string> { "*" } }, CancellationToken.None);
        JmapEvent value = await reader.ReadEventSourceAsync(CancellationToken.None);
        reader.CloseEventSource();

        Assert.That(value.Id, Is.EqualTo("one"));
        Assert.That(transport.Urls[0], Does.Contain("types=%2A"));
    }

    private static JmapEventSourceReader Reader(RecordingEventSourceTransport transport)
    {
        return new JmapEventSourceReader(
            "https://events.example.test/stream?types={types}&closeafter={closeafter}&ping={ping}",
            transport);
    }

    private static byte[] EventBytes(string id, string state, bool retryImmediately)
    {
        string retry = retryImmediately ? "retry: 0\n" : "";
        return Encoding.UTF8.GetBytes(
            retry + "event: state\nid: " + id +
            "\ndata: {\"@type\":\"StateChange\",\"changed\":{\"account-1\":{\"Email\":\"" + state + "\"}}}\n\n");
    }

    private sealed class RecordingEventSourceTransport : IJmapTransport
    {
        public Queue<Stream> Streams = new Queue<Stream>();
        public List<string> Urls = new List<string>();
        public List<string?> LastEventIds = new List<string?>();
        public CancellationTokenSource? CancelAfterOpen;

        public Task<Stream> OpenTransportEventSourceAsync(
            string eventSourceUrl,
            string? lastEventId,
            CancellationToken cancellationToken)
        {
            Urls.Add(eventSourceUrl);
            LastEventIds.Add(lastEventId);
            if (CancelAfterOpen != null)
            {
                CancelAfterOpen.Cancel();
            }

            return Task.FromResult(Streams.Dequeue());
        }

        public Task<JmapSession> GetTransportSessionAsync(string sessionUrl, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BlobUploadResponse> SendTransportUploadAsync(
            string uploadUrl,
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BlobDownloadResponse> SendTransportDownloadAsync(
            string downloadUrl,
            BlobDownloadRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private class TrackingEventStream : MemoryStream
    {
        public bool WasDisposed;

        public TrackingEventStream(byte[] bytes) : base(bytes)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ChunkedEventStream : TrackingEventStream
    {
        public int MaximumChunk;

        public ChunkedEventStream(byte[] bytes, int maximumChunk) : base(bytes)
        {
            MaximumChunk = maximumChunk;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int permitted = count < MaximumChunk ? count : MaximumChunk;
            return base.ReadAsync(buffer, offset, permitted, cancellationToken);
        }
    }

    private sealed class BlockingEventStream : MemoryStream
    {
        public bool WasDisposed;
        public ManualResetEventSlim Entered = new ManualResetEventSlim();

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            Entered.Set();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingEventStream : MemoryStream
    {
        public Exception Failure;

        public FailingEventStream(Exception failure)
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
    }
}
