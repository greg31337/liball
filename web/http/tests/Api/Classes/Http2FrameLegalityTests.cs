namespace http.Tests;

public class Http2FrameLegalityTests
{
    [Test]
    public void DataOnIdleStreamIsConnectionProtocolError()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("http2-frame-on-idle-stream"));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.Snapshot!.Streams, Is.Empty);
        });
    }

    [Test]
    public void DataAfterPeerEndIsStreamErrorAndResetsOnlyThatStream()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(connection, CreateHeaders(1, true));

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, false));
        Http2StreamSnapshot stream = FindStream(result.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("http2-frame-after-remote-end"));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.IsReady, Is.True);
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(result.Actions[0].StreamId, Is.EqualTo(1));
            Assert.That(result.Events.Single().ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(result.Events[0].StreamId, Is.EqualTo(1));
            Assert.That(stream.StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(stream.IsReset, Is.True);
            Assert.That(stream.ClosedByLocal, Is.True);
        });
    }

    [Test]
    public void ClosedAndImplicitlyClosedStreamsUseStreamClosedConnectionError()
    {
        Http2Connection closedConnection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(closedConnection, CreateHeaders(1, true));
        QueueFrame(closedConnection, CreateHeaders(1, true));
        Http2ConnectionResult closed = ProcessFrame(
            closedConnection, CreateData(1, false));

        Http2Connection implicitConnection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(implicitConnection, CreateHeaders(5, false));
        Http2ConnectionResult implicitlyClosed = ProcessFrame(
            implicitConnection, CreateData(3, false));

        Assert.Multiple(() =>
        {
            Assert.That(closed.ErrorCodeName,
                Is.EqualTo("http2-frame-on-closed-stream"));
            Assert.That(closed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(closed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(closed.IsTerminal, Is.True);
            Assert.That(implicitlyClosed.ErrorCodeName,
                Is.EqualTo("http2-frame-on-closed-stream"));
            Assert.That(implicitlyClosed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(implicitlyClosed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(implicitlyClosed.IsTerminal, Is.True);
        });
    }

    [Test]
    public void EndpointRolesControlNewStreamsAndPushPromises()
    {
        Http2Connection serverSend = CreateReadyConnection(
            Http2EndpointRole.Server);
        Http2ConnectionResult serverHeaders = QueueFrame(
            serverSend, CreateHeaders(0, false));

        Http2Connection clientSend = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(clientSend, CreateHeaders(0, true));
        Http2Frame localPush = CreatePushPromise(1, 2);
        Http2ConnectionResult clientPush = QueueFrame(clientSend, localPush);

        Http2Connection clientReceive = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult serverInitiatedHeaders = ProcessFrame(
            clientReceive, CreateHeaders(2, false));

        Http2Connection serverReceive = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(serverReceive, CreateHeaders(1, false));
        Http2ConnectionResult receivedPush = ProcessFrame(
            serverReceive, CreatePushPromise(1, 2));

        Assert.Multiple(() =>
        {
            Assert.That(serverHeaders.ErrorCodeName,
                Is.EqualTo("http2-server-stream-requires-push"));
            Assert.That(serverHeaders.IsTerminal, Is.False);
            Assert.That(clientPush.ErrorCodeName,
                Is.EqualTo("http2-client-cannot-push"));
            Assert.That(clientPush.IsTerminal, Is.False);
            Assert.That(serverInitiatedHeaders.ErrorCodeName,
                Is.EqualTo("http2-frame-on-idle-stream"));
            Assert.That(serverInitiatedHeaders.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(serverInitiatedHeaders.IsTerminal, Is.True);
            Assert.That(receivedPush.ErrorCodeName,
                Is.EqualTo("http2-server-cannot-receive-push"));
            Assert.That(receivedPush.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(receivedPush.IsTerminal, Is.True);
        });
    }

    [Test]
    public void ReservedStreamDirectionsPermitOnlyTheirLegalFrames()
    {
        Http2Connection server = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(server, CreateHeaders(1, false));
        QueueFrame(server, CreatePushPromise(1, 2));
        Http2ConnectionResult allowedWindow = ProcessFrame(
            server, CreateWindowUpdate(2));
        Http2ConnectionResult invalidData = ProcessFrame(
            server, CreateData(2, false));

        Http2Connection client = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(client, CreateHeaders(0, true));
        ProcessFrame(client, CreatePushPromise(1, 2));
        Http2ConnectionResult localWindow = QueueFrame(
            client, CreateWindowUpdate(2));
        Http2ConnectionResult localData = QueueFrame(
            client, CreateData(2, false));
        Http2ConnectionResult pushedHeaders = ProcessFrame(
            client, CreateHeaders(2, false));

        Assert.Multiple(() =>
        {
            Assert.That(allowedWindow.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(invalidData.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(invalidData.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(localWindow.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(localData.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(localData.ErrorCodeName,
                Is.EqualTo("http2-local-frame-invalid-for-stream-state"));
            Assert.That(localData.IsTerminal, Is.False);
            Assert.That(FindStream(pushedHeaders.Snapshot!, 2).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedLocal));
        });
    }

    [Test]
    public void HalfClosedLocalRejectsLocalDataButAcceptsPeerCompletion()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(connection, CreateHeaders(0, true));

        Http2ConnectionResult localData = QueueFrame(
            connection, CreateData(1, false));
        Http2ConnectionResult remoteHeaders = ProcessFrame(
            connection, CreateHeaders(1, false));
        Http2ConnectionResult remoteData = ProcessFrame(
            connection, CreateData(1, true));

        Assert.Multiple(() =>
        {
            Assert.That(localData.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(localData.ErrorCodeName,
                Is.EqualTo("http2-local-frame-invalid-for-stream-state"));
            Assert.That(remoteHeaders.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(remoteData.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(remoteData.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
        });
    }

    [Test]
    public void PriorityAndExpectedClosureRaceFramesDoNotReopenStreams()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        Http2ConnectionResult idlePriority = ProcessFrame(
            connection, CreatePriority(3));
        ProcessFrame(connection, CreateHeaders(1, true));
        QueueFrame(connection, CreateHeaders(1, true));
        Http2ConnectionResult lateWindow = ProcessFrame(
            connection, CreateWindowUpdate(1));
        Http2ConnectionResult lateReset = ProcessFrame(
            connection, CreateReset(1));

        Assert.Multiple(() =>
        {
            Assert.That(idlePriority.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(idlePriority.Snapshot!.Streams, Is.Empty);
            Assert.That(lateWindow.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lateReset.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lateReset.IsTerminal, Is.False);
            Assert.That(FindStream(lateReset.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(lateReset.Snapshot!, 1).IsReset, Is.False);
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

        byte[] settings = BuildWire(0x04, 0, 0, Array.Empty<byte>());
        byte[] inputBytes = settings;
        if (role == Http2EndpointRole.Server)
        {
            inputBytes = Concat(
                Http2Constants.ClientConnectionPreface, settings);
        }

        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = inputBytes;
        Http2ConnectionResult ready = connection.ProcessHttp2Input(input);
        Assert.That(ready.IsReady, Is.True);
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

    static Http2ConnectionResult ProcessFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Frame = frame;
        return connection.ProcessHttp2Input(input);
    }

    static Http2Frame CreateHeaders(int streamId, bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndStream = endStream;
        frame.EndHeaders = true;
        return frame;
    }

    static Http2Frame CreateData(int streamId, bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.Data = new byte[] { 0x01 };
        frame.EndStream = endStream;
        return frame;
    }

    static Http2Frame CreatePushPromise(
        int streamId,
        int promisedStreamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = streamId;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.EndHeaders = true;
        frame.HeaderBlockFragment = new byte[] { 0x82, 0x87, 0x84 };
        return frame;
    }

    static Http2Frame CreateWindowUpdate(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.StreamId = streamId;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = 1;
        return frame;
    }

    static Http2Frame CreatePriority(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Priority;
        frame.StreamId = streamId;
        frame.Weight = 16;
        return frame;
    }

    static Http2Frame CreateReset(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = streamId;
        frame.HasErrorCode = true;
        frame.ErrorCode = Http2ErrorCode.Cancel;
        frame.ErrorCodeValue = (uint)Http2ErrorCode.Cancel;
        frame.IsKnownErrorCode = true;
        return frame;
    }

    static Http2StreamSnapshot FindStream(
        Http2ConnectionSnapshot snapshot,
        int streamId)
    {
        return snapshot.Streams.Single(item => item.StreamId == streamId);
    }

    static byte[] BuildWire(
        byte type,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] wire = new byte[9 + payload.Length];
        wire[0] = (byte)(payload.Length >> 16);
        wire[1] = (byte)(payload.Length >> 8);
        wire[2] = (byte)payload.Length;
        wire[3] = type;
        wire[4] = flags;
        wire[5] = (byte)(streamId >> 24);
        wire[6] = (byte)(streamId >> 16);
        wire[7] = (byte)(streamId >> 8);
        wire[8] = (byte)streamId;
        payload.CopyTo(wire, 9);
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
