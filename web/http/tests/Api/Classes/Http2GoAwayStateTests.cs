namespace http.Tests;

public class Http2GoAwayStateTests
{
    [Test]
    public void ReceivedGoAwayStopsNewStreamsAndMarksHigherStreamsRetryable()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));
        byte[] debugData = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        Http2ConnectionResult received = ProcessFrame(
            connection,
            CreateGoAway(
                3,
                Http2ErrorCode.Cancel,
                debugData));
        debugData[0] = 0;
        Http2ConnectionResult existingStream = QueueFrame(
            connection, CreateData(1, true));
        Http2ConnectionResult newStream = QueueFrame(
            connection, CreateHeaders(0, false, Array.Empty<byte>()));
        Http2StreamSnapshot fifth = FindStream(received.Snapshot!, 5);

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(received.IsTerminal, Is.False);
            Assert.That(received.Events.Single().Frame!.DebugData,
                Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe, 0xef }));
            Assert.That(received.Snapshot!.ReceivedGoAway, Is.True);
            Assert.That(received.Snapshot.ReceivedLastStreamId,
                Is.EqualTo(3));
            Assert.That(received.Snapshot.ReceivedGoAwayErrorCode,
                Is.EqualTo(Http2ErrorCode.Cancel));
            Assert.That(received.Snapshot.ReceivedGoAwayErrorCodeValue,
                Is.EqualTo((uint)Http2ErrorCode.Cancel));
            Assert.That(received.Snapshot.ReceivedGoAwayDebugData,
                Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe, 0xef }));
            Assert.That(received.Snapshot.ActiveLocalStreams,
                Is.EqualTo(2));
            Assert.That(fifth.StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(fifth.IsRetryEligible, Is.True);
            Assert.That(existingStream.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(newStream.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(newStream.ErrorCodeName,
                Is.EqualTo("http2-goaway-received"));
            Assert.That(newStream.Snapshot!.NextLocalStreamId,
                Is.EqualTo(7));
        });
    }

    [Test]
    public void ReceivedGoAwayLastStreamCanOnlyDecrease()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));

        Http2ConnectionResult first = ProcessFrame(
            connection,
            CreateGoAway(5, Http2ErrorCode.NoError, Array.Empty<byte>()));
        Http2ConnectionResult lower = ProcessFrame(
            connection,
            CreateGoAway(1, Http2ErrorCode.NoError, Array.Empty<byte>()));
        Http2ConnectionResult increased = ProcessFrame(
            connection,
            CreateGoAway(3, Http2ErrorCode.NoError, Array.Empty<byte>()));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lower.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lower.Snapshot!.ReceivedLastStreamId,
                Is.EqualTo(1));
            Assert.That(FindStream(lower.Snapshot, 3).IsRetryEligible,
                Is.True);
            Assert.That(FindStream(lower.Snapshot, 5).IsRetryEligible,
                Is.True);
            Assert.That(increased.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(increased.ErrorCodeName,
                Is.EqualTo("http2-goaway-last-stream-increased"));
            Assert.That(increased.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(increased.IsTerminal, Is.True);
        });
    }

    [Test]
    public void SentGoAwayOnlyDecreasesAndDiscardsHigherPeerStreams()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(connection, CreateHeaders(1, false, Array.Empty<byte>()));
        ProcessFrame(connection, CreateHeaders(3, false, Array.Empty<byte>()));
        Http2ConnectionResult sent = QueueFrame(
            connection,
            CreateGoAway(3, Http2ErrorCode.NoError, new byte[] { 0x01 }));

        Http2ConnectionResult discarded = ProcessFrame(
            connection,
            CreateHeaders(
                5,
                false,
                Convert.FromHexString(
                    "408825a849e95ba97d7f8925a849e95bb8e8b4bf")));
        Http2ConnectionResult synchronized = ProcessFrame(
            connection, CreateHeaders(3, false, new byte[] { 0xbe }));
        Http2ConnectionResult increased = QueueFrame(
            connection,
            CreateGoAway(5, Http2ErrorCode.NoError, Array.Empty<byte>()));
        Http2ConnectionResult decreased = QueueFrame(
            connection,
            CreateGoAway(1, Http2ErrorCode.NoError, new byte[] { 0x02 }));

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(sent.Snapshot!.SentGoAway, Is.True);
            Assert.That(sent.Snapshot.SentLastStreamId, Is.EqualTo(3));
            Assert.That(sent.Snapshot.SentGoAwayDebugData,
                Is.EqualTo(new byte[] { 0x01 }));
            Assert.That(discarded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(discarded.Events, Is.Empty);
            Assert.That(discarded.Snapshot!.HighestRemoteStreamId,
                Is.EqualTo(3));
            Assert.That(synchronized.Events.Single().Message!.Headers.Fields
                .Single().Name,
                Is.EqualTo("custom-key"));
            Assert.That(increased.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(increased.ErrorCodeName,
                Is.EqualTo("http2-local-goaway-last-stream-increased"));
            Assert.That(increased.Actions, Is.Empty);
            Assert.That(decreased.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decreased.Snapshot!.SentLastStreamId,
                Is.EqualTo(1));
            Assert.That(decreased.Snapshot.SentGoAwayDebugData,
                Is.EqualTo(new byte[] { 0x02 }));
        });
    }

    [Test]
    public void GracefulShutdownUsesTwoStagesAndTerminatesAfterDrain()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(connection, CreateHeaders(1, true, Array.Empty<byte>()));

        Http2ConnectionResult first = QueueGracefulGoAway(connection);
        ProcessFrame(connection, CreateHeaders(3, true, Array.Empty<byte>()));
        Http2ConnectionResult second = QueueGracefulGoAway(connection);
        Http2ConnectionResult firstCompletion = QueueFrame(
            connection, CreateHeaders(1, true, Array.Empty<byte>()));
        Http2ConnectionResult finalCompletion = QueueFrame(
            connection, CreateHeaders(3, true, Array.Empty<byte>()));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
            Assert.That(first.Actions[0].Frame!.LastStreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(first.Actions[0].Frame!.ErrorCode,
                Is.EqualTo(Http2ErrorCode.NoError));
            Assert.That(first.IsTerminal, Is.False);
            Assert.That(second.Actions.Single().Frame!.LastStreamId,
                Is.EqualTo(3));
            Assert.That(second.IsTerminal, Is.False);
            Assert.That(firstCompletion.IsTerminal, Is.False);
            Assert.That(finalCompletion.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(finalCompletion.IsTerminal, Is.True);
            Assert.That(finalCompletion.IsReady, Is.False);
        });
    }

    [Test]
    public void ExplicitErrorGoAwayIsADataRichTerminalAction()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client);
        byte[] debugData = new byte[] { 0xca, 0xfe };
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendGoAway;
        action.HasLastStreamId = true;
        action.LastStreamId = 0;
        action.HasErrorCode = true;
        action.ErrorCode = Http2ErrorCode.InternalError;
        action.ErrorCodeValue = (uint)Http2ErrorCode.InternalError;
        action.IsKnownErrorCode = true;
        action.OpaqueData = debugData;

        Http2ConnectionResult result =
            connection.QueueHttp2Action(action);
        debugData[0] = 0;
        Http2ConnectionResult afterTerminal = QueueFrame(
            connection, CreatePing(new byte[8]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
            Assert.That(result.Actions[0].Frame!.DebugData,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(result.Snapshot!.SentGoAwayErrorCode,
                Is.EqualTo(Http2ErrorCode.InternalError));
            Assert.That(result.Snapshot.SentGoAwayDebugData,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(afterTerminal.Status,
                Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(afterTerminal.Actions, Is.Empty);
        });
    }

    [Test]
    public void PostGoAwayPushContinuationsAreMinimallyProcessedAndDiscarded()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(connection, CreateHeaders(0, false, Array.Empty<byte>()));
        QueueFrame(
            connection,
            CreateGoAway(0, Http2ErrorCode.NoError, Array.Empty<byte>()));
        byte[] headerBlock = Convert.FromHexString(
            "408825a849e95ba97d7f8925a849e95bb8e8b4bf");

        Http2ConnectionResult promised = ProcessFrame(
            connection,
            CreatePushPromise(1, 2, false, headerBlock[..10]));
        Http2ConnectionResult continued = ProcessFrame(
            connection, CreateContinuation(1, headerBlock[10..]));
        Http2ConnectionResult synchronized = ProcessFrame(
            connection,
            CreatePushPromise(1, 4, true, new byte[] { 0xbe }));

        Assert.Multiple(() =>
        {
            Assert.That(promised.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(promised.Events, Is.Empty);
            Assert.That(continued.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(continued.Events, Is.Empty);
            Assert.That(synchronized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(synchronized.Events, Is.Empty);
            Assert.That(synchronized.IsTerminal, Is.False);
            Assert.That(synchronized.Snapshot!.Streams.Select(
                item => item.StreamId),
                Is.EqualTo(new[] { 1 }));
        });
    }

    [Test]
    public void InvalidGoAwayMetadataUsesConnectionProtocolErrors()
    {
        Http2Connection parityConnection = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult parity = ProcessFrame(
            parityConnection,
            CreateGoAway(2, Http2ErrorCode.NoError, Array.Empty<byte>()));

        Http2Connection sizeConnection = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult size = ProcessBytes(
            sizeConnection, BuildWire(0x07, 0, new byte[7]));

        Assert.Multiple(() =>
        {
            Assert.That(parity.ErrorCodeName,
                Is.EqualTo("http2-goaway-last-stream-parity"));
            Assert.That(parity.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(parity.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(size.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(size.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
        });
    }

    static Http2Connection CreateReadyConnection(Http2EndpointRole role)
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = role;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionInput input = new Http2ConnectionInput();
        if (role == Http2EndpointRole.Server)
        {
            input.HasBytes = true;
            input.Bytes = Concat(
                Http2Constants.ClientConnectionPreface,
                BuildWire(0x04, 0, Array.Empty<byte>()));
        }
        else
        {
            input.Frame = CreateSettings();
        }

        Http2ConnectionResult ready = connection.ProcessHttp2Input(input);
        Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2ConnectionResult QueueFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult QueueGracefulGoAway(
        Http2Connection connection)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendGoAway;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult ProcessFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Frame = frame;
        return connection.ProcessHttp2Input(input);
    }

    static Http2ConnectionResult ProcessBytes(
        Http2Connection connection,
        byte[] bytes)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = bytes;
        return connection.ProcessHttp2Input(input);
    }

    static Http2Frame CreateSettings()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        return frame;
    }

    static Http2Frame CreateHeaders(
        int streamId,
        bool endStream,
        byte[] headerBlockFragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = headerBlockFragment;
        return frame;
    }

    static Http2Frame CreateData(int streamId, bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.EndStream = endStream;
        frame.Data = Array.Empty<byte>();
        return frame;
    }

    static Http2Frame CreatePing(byte[] opaqueData)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.OpaqueData = opaqueData;
        return frame;
    }

    static Http2Frame CreatePushPromise(
        int streamId,
        int promisedStreamId,
        bool endHeaders,
        byte[] headerBlockFragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = streamId;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.EndHeaders = endHeaders;
        frame.HeaderBlockFragment = headerBlockFragment;
        return frame;
    }

    static Http2Frame CreateContinuation(
        int streamId,
        byte[] headerBlockFragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Continuation;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.HeaderBlockFragment = headerBlockFragment;
        return frame;
    }

    static Http2Frame CreateGoAway(
        int lastStreamId,
        Http2ErrorCode errorCode,
        byte[] debugData)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = lastStreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = errorCode;
        frame.ErrorCodeValue = (uint)errorCode;
        frame.IsKnownErrorCode = true;
        frame.DebugData = debugData;
        return frame;
    }

    static Http2StreamSnapshot FindStream(
        Http2ConnectionSnapshot snapshot,
        int streamId)
    {
        return snapshot.Streams.Single(item => item.StreamId == streamId);
    }

    static byte[] BuildWire(byte type, int streamId, byte[] payload)
    {
        byte[] wire = new byte[Http2Constants.FrameHeaderSize +
            payload.Length];
        wire[0] = (byte)(payload.Length >> 16 & 0xff);
        wire[1] = (byte)(payload.Length >> 8 & 0xff);
        wire[2] = (byte)(payload.Length & 0xff);
        wire[3] = type;
        wire[5] = (byte)(streamId >> 24 & 0x7f);
        wire[6] = (byte)(streamId >> 16 & 0xff);
        wire[7] = (byte)(streamId >> 8 & 0xff);
        wire[8] = (byte)(streamId & 0xff);
        payload.CopyTo(wire, Http2Constants.FrameHeaderSize);
        return wire;
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
