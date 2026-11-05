using websocket;

namespace websocket.Tests;

public class WebSocketConnectionFailureHandlerTests
{
    [TestCase(
        WebSocketErrorKind.Protocol,
        WebSocketParseStatus.ProtocolFailure,
        1002,
        WebSocketCloseCode.ProtocolError)]
    [TestCase(
        WebSocketErrorKind.Compression,
        WebSocketParseStatus.ProtocolFailure,
        1002,
        WebSocketCloseCode.ProtocolError)]
    [TestCase(
        WebSocketErrorKind.Utf8,
        WebSocketParseStatus.ProtocolFailure,
        1007,
        WebSocketCloseCode.InvalidFramePayloadData)]
    [TestCase(
        WebSocketErrorKind.Limit,
        WebSocketParseStatus.LimitRejected,
        1009,
        WebSocketCloseCode.MessageTooBig)]
    public void FailureCategorySelectsCloseAndQueuesOneCopiedErrorEvent(
        WebSocketErrorKind kind,
        WebSocketParseStatus status,
        int closeCode,
        WebSocketCloseCode knownCode)
    {
        FailureTestContext context = Context(32, 4);
        WebSocketError error = Error(kind, "first-failure");

        WebSocketConnectionFailureResult result =
            context.Handler.HandleConnectionFailure(status, error);
        error.Identifier = "mutated";
        WebSocketEvent connectionEvent = RequiredEvent(
            context.Events.DequeueEvent());
        byte[] close = Drain(context.Scheduler);

        Assert.Multiple(() =>
        {
            Assert.That(result.Changed, Is.True);
            Assert.That(result.CloseQueued, Is.True);
            Assert.That(result.ClosePending, Is.False);
            Assert.That(result.ErrorEventQueued, Is.True);
            Assert.That(result.Error.CloseCode, Is.EqualTo(closeCode));
            Assert.That(close,
                Is.EqualTo(new byte[]
                {
                    0x88, 0x02,
                    (byte)(closeCode >> 8),
                    (byte)closeCode
                }));
            Assert.That(connectionEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(connectionEvent.HasError, Is.True);
            Assert.That(connectionEvent.Error.Kind, Is.EqualTo(kind));
            Assert.That(connectionEvent.Error.CloseCode,
                Is.EqualTo(closeCode));
            Assert.That(connectionEvent.Error.KnownCloseCode,
                Is.EqualTo(knownCode));
            Assert.That(connectionEvent.Error.Identifier,
                Is.EqualTo("first-failure"));
            Assert.That(context.State.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(context.State.HasFailure, Is.True);
            Assert.That(context.State.FailureError.Identifier,
                Is.EqualTo("first-failure"));
            Assert.That(context.Local.GetLocalCloseInfo().Code,
                Is.EqualTo(closeCode));
            Assert.That(context.Scheduler.GetDataSendsStopped(), Is.True);
        });
    }

    [Test]
    public void PermanentCloseOutputLimitEntersFailedStateWithoutRecursion()
    {
        FailureTestContext context = Context(3, 4);

        WebSocketConnectionFailureResult first =
            context.Handler.HandleConnectionFailure(
                WebSocketParseStatus.ProtocolFailure,
                Error(WebSocketErrorKind.Protocol, "wire-invalid"));
        WebSocketConnectionFailureResult repeated =
            context.Handler.HandleConnectionFailure(
                WebSocketParseStatus.ProtocolFailure,
                Error(WebSocketErrorKind.Utf8, "second-failure"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Changed, Is.True);
            Assert.That(first.CloseQueued, Is.False);
            Assert.That(context.Handler.HasFailedFailureClose(), Is.True);
            Assert.That(context.State.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(context.Scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("wire-invalid"));
            Assert.That(context.Events.GetEventCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void TemporaryOutputPressureRetriesTheSameFailureCloseOnce()
    {
        FailureTestContext context = Context(8, 4);
        context.Scheduler.TryQueueControlFrame([1, 2, 3, 4, 5, 6]);
        var prefix = new byte[1];
        context.Scheduler.WritePendingBytes(prefix);

        WebSocketConnectionFailureResult blocked =
            context.Handler.HandleConnectionFailure(
                WebSocketParseStatus.ProtocolFailure,
                Error(WebSocketErrorKind.Protocol, "bad-frame"));
        var control = new byte[5];
        context.Scheduler.WritePendingBytes(control);
        WebSocketConnectionFailureResult retried =
            context.Handler.SynchronizeFailureHandling();
        WebSocketConnectionFailureResult repeated =
            context.Handler.HandleConnectionFailure(
                WebSocketParseStatus.ProtocolFailure,
                Error(WebSocketErrorKind.Utf8, "later-error"));
        byte[] close = Drain(context.Scheduler);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.ClosePending, Is.True);
            Assert.That(context.State.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(retried.CloseQueued, Is.True);
            Assert.That(retried.ClosePending, Is.False);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Error.Identifier, Is.EqualTo("bad-frame"));
            Assert.That(close,
                Is.EqualTo(new byte[] { 0x88, 0x02, 0x03, 0xEA }));
            Assert.That(context.Events.GetEventCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExistingLocalCloseIsNotReplacedByFailureClose()
    {
        FailureTestContext context = Context(32, 4);
        WebSocketCloseInfo normal = new WebSocketCloseInfo();
        normal.HasCode = true;
        normal.Code = WebSocketCloseCodes.NormalClosure;
        context.Local.StartLocalClose(normal);
        int pending = context.Scheduler.GetPendingByteCount();

        WebSocketConnectionFailureResult result =
            context.Handler.HandleConnectionFailure(
                WebSocketParseStatus.ProtocolFailure,
                Error(WebSocketErrorKind.Protocol, "after-close"));

        Assert.Multiple(() =>
        {
            Assert.That(result.CloseQueued, Is.False);
            Assert.That(context.Scheduler.GetPendingByteCount(),
                Is.EqualTo(pending));
            Assert.That(context.Local.GetLocalCloseInfo().Code,
                Is.EqualTo(WebSocketCloseCodes.NormalClosure));
            Assert.That(context.State.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(context.State.HasFailure, Is.True);
        });
    }

    [Test]
    public void FullEventQueueRetainsExactlyOneErrorUntilCapacityReturns()
    {
        FailureTestContext context = Context(32, 1);
        WebSocketEvent prior = new WebSocketEvent();
        prior.Type = WebSocketEventType.Ping;
        context.Events.TryEnqueueEvent(prior);

        context.Handler.HandleConnectionFailure(
            WebSocketParseStatus.LimitRejected,
            Error(WebSocketErrorKind.Limit, "too-large"));
        WebSocketEvent first = RequiredEvent(context.Events.DequeueEvent());
        WebSocketConnectionFailureResult synchronized =
            context.Handler.SynchronizeFailureHandling();
        WebSocketEvent error = RequiredEvent(context.Events.DequeueEvent());
        context.Handler.SynchronizeFailureHandling();

        Assert.Multiple(() =>
        {
            Assert.That(first.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(synchronized.ErrorEventQueued, Is.True);
            Assert.That(error.Type, Is.EqualTo(WebSocketEventType.Error));
            Assert.That(error.Error.Identifier, Is.EqualTo("too-large"));
            Assert.That(context.Events.GetEventCount(), Is.Zero);
            Assert.That(context.Handler.HasPendingErrorEvent(), Is.False);
        });
    }

    [Test]
    public void FailureCloseCanRecordSentThenEndsAsOriginalFailure()
    {
        FailureTestContext context = Context(32, 4);
        context.Handler.HandleConnectionFailure(
            WebSocketParseStatus.ProtocolFailure,
            Error(WebSocketErrorKind.Protocol, "protocol-first"));
        Drain(context.Scheduler);

        WebSocketLocalCloseResult sent =
            context.Local.SynchronizeLocalCloseSent();
        WebSocketConnectionTransitionResult ended =
            context.State.EndTransport();

        Assert.Multiple(() =>
        {
            Assert.That(sent.Changed, Is.True);
            Assert.That(sent.CloseSent, Is.True);
            Assert.That(context.Local.GetLocalCloseInfo().Sent, Is.True);
            Assert.That(ended.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(ended.Error.Identifier,
                Is.EqualTo("protocol-first"));
            Assert.That(context.State.TransportEnded, Is.True);
            Assert.That(context.State.CleanClose, Is.False);
        });
    }

    [Test]
    public void FrameCodecFailureUsesConnectionFailurePath()
    {
        WebSocketConnectionOptions options = Options();
        options.Role = WebSocketRole.Server;
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
        var pump = new WebSocketInboundFramePump(
            new WebSocketFrameCodec(
                WebSocketRole.Server,
                options.MaximumFramePayloadLength,
                []),
            consumer);

        WebSocketInboundFramePumpResult result =
            pump.PumpInboundFrames([0x81, 0x00]);
        WebSocketEvent connectionEvent = RequiredEvent(
            consumer.ReadStreamingEvent());
        byte[] close = Drain(consumer.GetStreamingSendScheduler());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("frame-mask-required"));
            Assert.That(connectionEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(connectionEvent.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(close,
                Is.EqualTo(new byte[] { 0x88, 0x02, 0x03, 0xEA }));
            Assert.That(consumer.GetStreamingStateMachine().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(consumer.GetStreamingStateMachine().HasFailure,
                Is.True);
        });
    }

    static FailureTestContext Context(int outputCapacity, int eventCapacity)
    {
        WebSocketConnectionOptions options = Options();
        var scheduler = new WebSocketSendScheduler(outputCapacity);
        var events = new WebSocketEventQueue(eventCapacity);
        var pingPong = new WebSocketPingPongHandler(
            options,
            scheduler,
            events);
        var heartbeat = new WebSocketHeartbeatManager(options, pingPong);
        var state = new WebSocketConnectionStateMachine(
            options.Role,
            options.Transport,
            options.CloseTimeoutMilliseconds);
        state.OpenConnection();
        var local = new WebSocketLocalCloseCoordinator(
            options,
            scheduler,
            state,
            heartbeat);
        var handler = new WebSocketConnectionFailureHandler(
            scheduler,
            events,
            state,
            local,
            heartbeat);
        return new FailureTestContext
        {
            Scheduler = scheduler,
            Events = events,
            State = state,
            Local = local,
            Handler = handler
        };
    }

    static WebSocketConnectionOptions Options()
    {
        return new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Server,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = 128,
            MaximumBufferedOutputBytes = 128,
            MaximumQueuedEvents = 8,
            AutomaticFramePayloadLength = 32
        };
    }

    static WebSocketError Error(WebSocketErrorKind kind, string identifier)
    {
        return new WebSocketError
        {
            Kind = kind,
            Identifier = identifier,
            Diagnostic = "Failure diagnostic."
        };
    }

    static byte[] Drain(WebSocketSendScheduler scheduler)
    {
        byte[] output = new byte[scheduler.GetPendingByteCount()];
        int written = scheduler.WritePendingBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}

internal class FailureTestContext
{
    internal WebSocketSendScheduler Scheduler =
        new WebSocketSendScheduler(1);
    internal WebSocketEventQueue Events = new WebSocketEventQueue(1);
    internal WebSocketConnectionStateMachine State = null!;
    internal WebSocketLocalCloseCoordinator Local = null!;
    internal WebSocketConnectionFailureHandler Handler = null!;
}
