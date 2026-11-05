using websocket;

namespace websocket.Tests;

public class WebSocketLocalCloseCoordinatorTests
{
    [Test]
    public void ServerCloseSerializesCodeAndReasonAndRecordsFacts()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(256);
        WebSocketConnectionStateMachine state = OpenState(options);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            state);
        WebSocketCloseInfo close = Close(1000, "bye");

        WebSocketLocalCloseResult result =
            coordinator.StartLocalClose(close);
        close.Code = 1001;
        close.Reason = "changed";
        byte[] wire = Drain(scheduler);
        WebSocketCloseInfo facts = coordinator.GetLocalCloseInfo();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.CloseQueued, Is.True);
            Assert.That(wire,
                Is.EqualTo(new byte[]
                {
                    0x88, 0x05, 0x03, 0xE8, 0x62, 0x79, 0x65
                }));
            Assert.That(state.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(state.LocalInitiatedClosing, Is.True);
            Assert.That(coordinator.CanSendApplicationData(), Is.False);
            Assert.That(facts.Code, Is.EqualTo(1000));
            Assert.That(facts.HasKnownCode, Is.True);
            Assert.That(facts.KnownCode,
                Is.EqualTo(WebSocketCloseCode.NormalClosure));
            Assert.That(facts.Reason, Is.EqualTo("bye"));
            Assert.That(facts.InitiatedLocally, Is.True);
            Assert.That(facts.Sent, Is.False);
        });
    }

    [Test]
    public void EmptyServerCloseUsesAnEmptyControlPayload()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            OpenState(options));

        WebSocketLocalCloseResult result =
            coordinator.StartLocalClose(new WebSocketCloseInfo());

        Assert.Multiple(() =>
        {
            Assert.That(result.CloseQueued, Is.True);
            Assert.That(Drain(scheduler),
                Is.EqualTo(new byte[] { 0x88, 0x00 }));
        });
    }

    [Test]
    public void ClientCloseUsesTheConfiguredMaskKey()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Client);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            OpenState(options));

        coordinator.StartLocalClose(Close(1000, ""));

        Assert.Multiple(() =>
        {
            Assert.That(keys.CallCount, Is.EqualTo(1));
            Assert.That(Drain(scheduler),
                Is.EqualTo(new byte[]
                {
                    0x88, 0x82,
                    0x01, 0x02, 0x03, 0x04,
                    0x02, 0xEA
                }));
        });
    }

    [Test]
    public void InvalidLocalCloseDetailsDoNotChangeConnectionOrScheduler()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(256);
        WebSocketConnectionStateMachine state = OpenState(options);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            state);
        WebSocketCloseInfo reasonWithoutCode = new WebSocketCloseInfo
        {
            HasReason = true,
            Reason = "reason"
        };
        WebSocketCloseInfo invalidCode = Close(1006, "");
        WebSocketCloseInfo invalidText = Close(1000, "\uD800");
        WebSocketCloseInfo oversized = Close(1000, new string('a', 124));

        WebSocketLocalCloseResult missing =
            coordinator.StartLocalClose(null);
        WebSocketLocalCloseResult reason =
            coordinator.StartLocalClose(reasonWithoutCode);
        WebSocketLocalCloseResult code =
            coordinator.StartLocalClose(invalidCode);
        WebSocketLocalCloseResult text =
            coordinator.StartLocalClose(invalidText);
        WebSocketLocalCloseResult limit =
            coordinator.StartLocalClose(oversized);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, WebSocketErrorKind.State,
                "local-close-invalid");
            AssertFailure(reason, WebSocketErrorKind.State,
                "local-close-reason-without-code");
            AssertFailure(code, WebSocketErrorKind.State,
                "local-close-code-invalid");
            AssertFailure(text, WebSocketErrorKind.State,
                "local-close-reason-invalid");
            Assert.That(limit.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(limit.Error.Identifier,
                Is.EqualTo("local-close-reason-limit-exceeded"));
            Assert.That(state.State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(coordinator.CanSendApplicationData(), Is.True);
            Assert.That(coordinator.HasLocalCloseInfo(), Is.False);
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(scheduler.GetDataSendsStopped(), Is.False);
        });
    }

    [Test]
    public void DuplicateCloseIsIdempotentButConflictingDetailsAreRejected()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            OpenState(options));

        WebSocketLocalCloseResult first =
            coordinator.StartLocalClose(Close(1000, "done"));
        int pendingAfterFirst = scheduler.GetPendingByteCount();
        WebSocketLocalCloseResult duplicate =
            coordinator.StartLocalClose(Close(1000, "done"));
        WebSocketLocalCloseResult conflict =
            coordinator.StartLocalClose(Close(1001, "away"));

        Assert.Multiple(() =>
        {
            Assert.That(first.CloseQueued, Is.True);
            Assert.That(duplicate.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(duplicate.Accepted, Is.True);
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(duplicate.CloseQueued, Is.False);
            Assert.That(conflict.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(conflict.Error.Identifier,
                Is.EqualTo("local-close-details-conflict"));
            Assert.That(scheduler.GetPendingByteCount(),
                Is.EqualTo(pendingAfterFirst));
        });
    }

    [Test]
    public void ResponseToRemoteCloseDoesNotClaimLocalInitiation()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        WebSocketConnectionStateMachine state = OpenState(options);
        state.RecordRemoteClose();
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            state);

        WebSocketLocalCloseResult result =
            coordinator.StartLocalClose(Close(1000, ""));
        WebSocketCloseInfo facts = coordinator.GetLocalCloseInfo();

        Assert.Multiple(() =>
        {
            Assert.That(result.CloseQueued, Is.True);
            Assert.That(facts.InitiatedLocally, Is.False);
            Assert.That(state.LocalInitiatedClosing, Is.False);
            Assert.That(state.LocalCloseStarted, Is.True);
        });
    }

    [Test]
    public void BackpressuredCloseCanBeRetriedWithTheSameDetails()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(8);
        scheduler.TryQueueControlFrame([9, 8, 7, 6, 5, 4]);
        var firstByte = new byte[1];
        scheduler.WritePendingBytes(firstByte);
        WebSocketConnectionStateMachine state = OpenState(options);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            state);

        WebSocketLocalCloseResult blocked =
            coordinator.StartLocalClose(Close(1000, ""));
        WebSocketConnectionState stateAfterBlocked = state.State;
        bool dataStoppedAfterBlocked = scheduler.GetDataSendsStopped();
        var priorControl = new byte[5];
        scheduler.WritePendingBytes(priorControl);
        WebSocketLocalCloseResult retried =
            coordinator.StartLocalClose(Close(1000, ""));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.Accepted, Is.False);
            Assert.That(blocked.Changed, Is.False);
            Assert.That(stateAfterBlocked,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(dataStoppedAfterBlocked, Is.False);
            Assert.That(priorControl,
                Is.EqualTo(new byte[] { 8, 7, 6, 5, 4 }));
            Assert.That(retried.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(retried.CloseQueued, Is.True);
            Assert.That(Drain(scheduler),
                Is.EqualTo(new byte[] { 0x88, 0x02, 0x03, 0xE8 }));
        });
    }

    [Test]
    public void CloseFinishesCurrentDataThenPrecedesControlAndDropsQueuedData()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        scheduler.TryQueueDataFrames([[1, 2, 3], [4, 5]]);
        var prefix = new byte[1];
        scheduler.WritePendingBytes(prefix);
        scheduler.TryQueueControlFrame([9, 9]);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            OpenState(options));

        coordinator.StartLocalClose(new WebSocketCloseInfo());
        byte[] remaining = Drain(scheduler);

        Assert.Multiple(() =>
        {
            Assert.That(prefix, Is.EqualTo(new byte[] { 1 }));
            Assert.That(remaining,
                Is.EqualTo(new byte[] { 2, 3, 0x88, 0x00, 9, 9 }));
            Assert.That(remaining, Does.Not.Contain(4));
            Assert.That(remaining, Does.Not.Contain(5));
            Assert.That(scheduler.GetPendingDataByteCount(), Is.Zero);
        });
    }

    [Test]
    public void LocalCloseRejectsNewDataAndStopsHeartbeatTracking()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.Heartbeat.Enabled = true;
        options.Heartbeat.IdleIntervalMilliseconds = 100;
        options.Heartbeat.PongTimeoutMilliseconds = 50;
        options.Heartbeat.TokenLength = 4;
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(256);
        WebSocketEventQueue events = new WebSocketEventQueue(8);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            events);
        var heartbeat = new WebSocketHeartbeatManager(options, handler);
        heartbeat.AdvanceHeartbeatTime(100);
        WebSocketLocalCloseCoordinator coordinator =
            new WebSocketLocalCloseCoordinator(
                options,
                scheduler,
                OpenState(options),
                heartbeat);

        coordinator.StartLocalClose(new WebSocketCloseInfo());

        Assert.Multiple(() =>
        {
            Assert.That(heartbeat.HasOutstandingHeartbeatPing(), Is.False);
            Assert.That(handler.GetOutstandingTrackedPingCount(), Is.Zero);
            Assert.That(scheduler.TryQueueDataFrame([1]), Is.False);
            Assert.That(scheduler.TryQueueDataFrames([[2], [3]]), Is.False);
            Assert.That(scheduler.GetPendingDataByteCount(), Is.Zero);
            Assert.That(scheduler.GetPendingCloseByteCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void CompletingCloseOutputRecordsSentFactExactlyOnce()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        WebSocketConnectionStateMachine state = OpenState(options);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            state);
        coordinator.StartLocalClose(Close(1000, ""));
        var prefix = new byte[3];
        scheduler.WritePendingBytes(prefix);

        WebSocketLocalCloseResult before =
            coordinator.SynchronizeLocalCloseSent();
        var suffix = new byte[1];
        scheduler.WritePendingBytes(suffix);
        WebSocketLocalCloseResult completed =
            coordinator.SynchronizeLocalCloseSent();
        WebSocketLocalCloseResult duplicate =
            coordinator.SynchronizeLocalCloseSent();
        WebSocketLocalCloseResult repeatedRequest =
            coordinator.StartLocalClose(Close(1000, ""));
        WebSocketCloseInfo firstFacts = coordinator.GetLocalCloseInfo();
        firstFacts.Reason = "mutated";
        WebSocketCloseInfo secondFacts = coordinator.GetLocalCloseInfo();

        Assert.Multiple(() =>
        {
            Assert.That(before.Changed, Is.False);
            Assert.That(before.CloseSent, Is.False);
            Assert.That(completed.Changed, Is.True);
            Assert.That(completed.CloseSent, Is.True);
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(duplicate.CloseSent, Is.True);
            Assert.That(repeatedRequest.Changed, Is.False);
            Assert.That(repeatedRequest.CloseSent, Is.True);
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(state.LocalCloseSent, Is.True);
            Assert.That(secondFacts.Sent, Is.True);
            Assert.That(secondFacts.Reason, Is.EqualTo(""));
        });
    }

    [Test]
    public void FrameAndOutputLimitsAreRejectedBeforeClosingStarts()
    {
        WebSocketConnectionOptions frameOptions = Options(
            WebSocketRole.Server);
        frameOptions.MaximumFramePayloadLength = 1;
        frameOptions.MaximumMessagePayloadLength = 1;
        frameOptions.MaximumDecompressedMessagePayloadLength = 1;
        frameOptions.AutomaticFramePayloadLength = 1;
        WebSocketSendScheduler frameScheduler =
            new WebSocketSendScheduler(32);
        WebSocketConnectionStateMachine frameState = OpenState(frameOptions);
        WebSocketLocalCloseCoordinator frameCoordinator = Coordinator(
            frameOptions,
            frameScheduler,
            frameState);
        WebSocketConnectionOptions outputOptions = Options(
            WebSocketRole.Server);
        WebSocketSendScheduler outputScheduler =
            new WebSocketSendScheduler(3);
        WebSocketConnectionStateMachine outputState = OpenState(outputOptions);
        WebSocketLocalCloseCoordinator outputCoordinator = Coordinator(
            outputOptions,
            outputScheduler,
            outputState);

        WebSocketLocalCloseResult frame =
            frameCoordinator.StartLocalClose(Close(1000, ""));
        WebSocketLocalCloseResult output =
            outputCoordinator.StartLocalClose(Close(1000, ""));

        Assert.Multiple(() =>
        {
            Assert.That(frame.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(frame.Error.Identifier,
                Is.EqualTo("local-close-frame-limit-exceeded"));
            Assert.That(output.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(output.Error.Identifier,
                Is.EqualTo("local-close-output-limit-exceeded"));
            Assert.That(frameState.State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(outputState.State,
                Is.EqualTo(WebSocketConnectionState.Open));
        });
    }

    [Test]
    public void TerminalStateDoesNotQueueANewCloseFrame()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketConnectionStateMachine state = OpenState(options);
        state.AbortConnection();
        WebSocketSendScheduler scheduler = new WebSocketSendScheduler(32);
        WebSocketLocalCloseCoordinator coordinator = Coordinator(
            options,
            scheduler,
            state);

        WebSocketLocalCloseResult result =
            coordinator.StartLocalClose(new WebSocketCloseInfo());

        Assert.Multiple(() =>
        {
            AssertFailure(result, WebSocketErrorKind.State,
                "local-close-state-invalid");
            Assert.That(coordinator.HasLocalCloseInfo(), Is.False);
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(scheduler.GetDataSendsStopped(), Is.False);
        });
    }

    static WebSocketConnectionOptions Options(WebSocketRole role)
    {
        return new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            AutomaticFramePayloadLength = 32,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 8
        };
    }

    static WebSocketConnectionStateMachine OpenState(
        WebSocketConnectionOptions options)
    {
        var state = new WebSocketConnectionStateMachine(
            options.Role,
            options.Transport,
            options.CloseTimeoutMilliseconds);
        state.OpenConnection();
        return state;
    }

    static WebSocketLocalCloseCoordinator Coordinator(
        WebSocketConnectionOptions options,
        WebSocketSendScheduler scheduler,
        WebSocketConnectionStateMachine state)
    {
        return new WebSocketLocalCloseCoordinator(
            options,
            scheduler,
            state,
            null);
    }

    static WebSocketCloseInfo Close(int code, string reason)
    {
        return new WebSocketCloseInfo
        {
            HasCode = true,
            Code = code,
            HasReason = reason.Length != 0,
            Reason = reason
        };
    }

    static byte[] Drain(WebSocketSendScheduler scheduler)
    {
        byte[] output = new byte[scheduler.GetPendingByteCount()];
        int written = scheduler.WritePendingBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static void AssertFailure(
        WebSocketLocalCloseResult result,
        WebSocketErrorKind kind,
        string identifier)
    {
        Assert.That(result.Status,
            Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.HasError, Is.True);
        Assert.That(result.Error.Kind, Is.EqualTo(kind));
        Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
    }
}
