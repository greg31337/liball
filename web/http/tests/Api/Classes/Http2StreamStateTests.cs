namespace http.Tests;

public class Http2StreamStateTests
{
    [Test]
    public void ClientAllocatesOddIdsAndRejectsParityOrMonotonicRegression()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, 100);

        Http2ConnectionResult first = QueueFrame(
            connection, CreateHeaders(0, false));
        Http2ConnectionResult second = QueueFrame(
            connection, CreateHeaders(0, true));
        Http2ConnectionResult skipped = QueueFrame(
            connection, CreateHeaders(7, false));
        Http2ConnectionResult wrongParity = QueueFrame(
            connection, CreateHeaders(8, false));
        Http2ConnectionResult regression = QueueFrame(
            connection, CreateHeaders(5, false));
        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Actions.Single().StreamId, Is.EqualTo(1));
            Assert.That(first.Actions[0].Frame!.StreamId, Is.EqualTo(1));
            Assert.That(second.Actions.Single().StreamId, Is.EqualTo(3));
            Assert.That(skipped.Actions.Single().StreamId, Is.EqualTo(7));
            Assert.That(wrongParity.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(wrongParity.ErrorCodeName,
                Is.EqualTo("invalid-http2-local-stream-id"));
            Assert.That(regression.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(regression.ErrorCodeName,
                Is.EqualTo("http2-local-stream-id-not-monotonic"));
            Assert.That(snapshot.NextLocalStreamId, Is.EqualTo(9));
            Assert.That(snapshot.ActiveLocalStreams, Is.EqualTo(3));
            Assert.That(snapshot.IsTerminal, Is.False);
            Assert.That(snapshot.Streams.Select(item => item.StreamId),
                Is.EqualTo(new[] { 1, 3, 7 }));
            Assert.That(snapshot.Streams.Select(item => item.StreamState),
                Is.EqualTo(new[]
                {
                    Http2StreamState.Open,
                    Http2StreamState.HalfClosedLocal,
                    Http2StreamState.Open
                }));
        });
    }

    [Test]
    public void ServerPushAllocationReservesEvenStreamThenSendsItClosed()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, 100);
        ProcessFrame(connection, CreateHeaders(1, false));
        Http2Frame push = new Http2Frame();
        push.FrameType = Http2FrameType.PushPromise;
        push.StreamId = 1;
        push.EndHeaders = true;

        Http2ConnectionResult reserved = QueueFrame(connection, push);
        Http2ConnectionSnapshot reservedSnapshot =
            connection.InspectHttp2Connection();
        Http2ConnectionResult headers = QueueFrame(
            connection, CreateHeaders(2, false));
        Http2ConnectionSnapshot halfClosed =
            connection.InspectHttp2Connection();
        Http2Frame data = new Http2Frame();
        data.FrameType = Http2FrameType.Data;
        data.StreamId = 2;
        data.Data = new byte[] { 0xaa };
        data.EndStream = true;
        Http2ConnectionResult closed = QueueFrame(connection, data);
        Http2ConnectionSnapshot final =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(push.HasPromisedStreamId, Is.True);
            Assert.That(push.PromisedStreamId, Is.EqualTo(2));
            Assert.That(reservedSnapshot.NextLocalStreamId, Is.EqualTo(4));
            Assert.That(reservedSnapshot.ActiveLocalStreams, Is.Zero);
            Assert.That(FindStream(reservedSnapshot, 2).StreamState,
                Is.EqualTo(Http2StreamState.ReservedLocal));
            Assert.That(FindStream(reservedSnapshot, 2).IsPushed, Is.True);
            Assert.That(FindStream(reservedSnapshot, 2).AssociatedStreamId,
                Is.EqualTo(1));
            Assert.That(headers.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(halfClosed, 2).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedRemote));
            Assert.That(FindStream(halfClosed, 2).RemoteEndStream, Is.True);
            Assert.That(halfClosed.ActiveLocalStreams, Is.EqualTo(1));
            Assert.That(closed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(final, 2).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(final, 2).LocalEndStream, Is.True);
            Assert.That(final.ActiveLocalStreams, Is.Zero);
        });
    }

    [Test]
    public void ReceivedPushMovesThroughReservedRemoteAndHalfClosedLocal()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, 100);
        QueueFrame(connection, CreateHeaders(0, true));
        Http2Frame push = new Http2Frame();
        push.FrameType = Http2FrameType.PushPromise;
        push.StreamId = 1;
        push.HasPromisedStreamId = true;
        push.PromisedStreamId = 2;
        push.EndHeaders = true;
        push.HeaderBlockFragment = new byte[] { 0x82, 0x87, 0x84 };

        Http2ConnectionResult reserved = ProcessFrame(connection, push);
        Http2ConnectionSnapshot reservedSnapshot =
            connection.InspectHttp2Connection();
        Http2ConnectionResult headers = ProcessFrame(
            connection, CreateHeaders(2, false));
        Http2ConnectionSnapshot halfClosed =
            connection.InspectHttp2Connection();
        Http2Frame data = new Http2Frame();
        data.FrameType = Http2FrameType.Data;
        data.StreamId = 2;
        data.Data = new byte[] { 0x01, 0x02 };
        data.EndStream = true;
        Http2ConnectionResult closed = ProcessFrame(connection, data);
        Http2ConnectionSnapshot final =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(reservedSnapshot, 2).StreamState,
                Is.EqualTo(Http2StreamState.ReservedRemote));
            Assert.That(reservedSnapshot.HighestRemoteStreamId, Is.EqualTo(2));
            Assert.That(reservedSnapshot.ActiveRemoteStreams, Is.Zero);
            Assert.That(headers.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(halfClosed, 2).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedLocal));
            Assert.That(FindStream(halfClosed, 2).LocalEndStream, Is.True);
            Assert.That(halfClosed.ActiveRemoteStreams, Is.EqualTo(1));
            Assert.That(closed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(final, 2).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(final, 2).ReceivedContentLength,
                Is.EqualTo(2));
            Assert.That(final.ActiveRemoteStreams, Is.Zero);
        });
    }

    [Test]
    public void EndStreamFromBothDirectionsClosesAnOpenStream()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, 100);

        QueueFrame(connection, CreateHeaders(0, true));
        Http2ConnectionSnapshot localEnded =
            connection.InspectHttp2Connection();
        ProcessFrame(connection, CreateHeaders(1, false));
        Http2Frame data = new Http2Frame();
        data.FrameType = Http2FrameType.Data;
        data.StreamId = 1;
        data.Data = new byte[] { 1, 2, 3 };
        data.EndStream = true;
        ProcessFrame(connection, data);
        Http2ConnectionSnapshot closed =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(FindStream(localEnded, 1).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedLocal));
            Assert.That(FindStream(closed, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(closed, 1).LocalEndStream, Is.True);
            Assert.That(FindStream(closed, 1).RemoteEndStream, Is.True);
            Assert.That(FindStream(closed, 1).ReceivedHeaders, Is.True);
            Assert.That(FindStream(closed, 1).ReceivedContentLength,
                Is.EqualTo(3));
            Assert.That(closed.ActiveLocalStreams, Is.Zero);
        });
    }

    [Test]
    public void PeerStreamIdsRequireRemoteParityAndMonotonicIncrease()
    {
        Http2Connection monotonic = CreateReadyConnection(
            Http2EndpointRole.Server, 100);
        Http2ConnectionResult one = ProcessFrame(
            monotonic, CreateHeaders(1, false));
        Http2ConnectionResult five = ProcessFrame(
            monotonic, CreateHeaders(5, false));
        Http2ConnectionResult three = ProcessFrame(
            monotonic, CreateHeaders(3, false));
        Http2Connection connectionParity = CreateReadyConnection(
            Http2EndpointRole.Server, 100);
        Http2ConnectionResult parity = ProcessFrame(
            connectionParity, CreateHeaders(2, false));

        Assert.Multiple(() =>
        {
            Assert.That(one.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(five.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(three.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(three.ErrorCodeName,
                Is.EqualTo("http2-remote-stream-id-not-monotonic"));
            Assert.That(three.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(three.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(three.IsTerminal, Is.True);
            Assert.That(three.Snapshot!.HighestRemoteStreamId, Is.EqualTo(5));
            Assert.That(parity.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parity.ErrorCodeName,
                Is.EqualTo("invalid-http2-remote-stream-id"));
            Assert.That(parity.IsTerminal, Is.True);
        });
    }

    [Test]
    public void ResetStreamClosesAndRetainsOnlyConfiguredRecentRecords()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, 2);

        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateHeaders(3, false));
        ProcessFrame(connection, CreateHeaders(5, false));
        ProcessFrame(connection, CreateReset(5));
        ProcessFrame(connection, CreateReset(1));
        ProcessFrame(connection, CreateReset(3));

        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.HighestRemoteStreamId, Is.EqualTo(5));
            Assert.That(snapshot.ActiveRemoteStreams, Is.Zero);
            Assert.That(snapshot.Streams.Select(item => item.StreamId),
                Is.EqualTo(new[] { 1, 3 }));
            Assert.That(snapshot.Streams.All(item =>
                item.StreamState == Http2StreamState.Closed), Is.True);
            Assert.That(snapshot.Streams.All(item => item.IsReset), Is.True);
            Assert.That(snapshot.Streams.All(item =>
                item.ErrorCode == Http2ErrorCode.Cancel), Is.True);
        });
    }

    [Test]
    public void ExhaustedLocalIdentifierSpaceRejectsFurtherAllocation()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, 100);
        Http2ConnectionResult last = QueueFrame(
            connection,
            CreateHeaders(Http2Constants.MaximumStreamIdentifier, false));
        Http2ConnectionResult exhausted = QueueFrame(
            connection, CreateHeaders(0, false));
        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(last.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(snapshot.NextLocalStreamId, Is.Zero);
            Assert.That(exhausted.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(exhausted.ErrorCodeName,
                Is.EqualTo("http2-stream-id-exhausted"));
            Assert.That(exhausted.IsTerminal, Is.False);
        });
    }

    [Test]
    public void ResetClearsAllStreamAllocationAndRetentionState()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, 100);
        QueueFrame(connection, CreateHeaders(0, false));
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Server;

        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reset.Snapshot!.NextLocalStreamId, Is.EqualTo(2));
            Assert.That(reset.Snapshot.HighestRemoteStreamId, Is.Zero);
            Assert.That(reset.Snapshot.ActiveLocalStreams, Is.Zero);
            Assert.That(reset.Snapshot.ActiveRemoteStreams, Is.Zero);
            Assert.That(reset.Snapshot.Streams, Is.Empty);
        });
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        int retainedClosedStreams)
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = role;
        options.MaximumRetainedClosedStreams = retainedClosedStreams;
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
        Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
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
        byte[] wire = new byte[
            Http2Constants.FrameHeaderSize + payload.Length];
        wire[0] = (byte)(payload.Length >> 16);
        wire[1] = (byte)(payload.Length >> 8);
        wire[2] = (byte)payload.Length;
        wire[3] = type;
        wire[4] = flags;
        wire[5] = (byte)(streamId >> 24);
        wire[6] = (byte)(streamId >> 16);
        wire[7] = (byte)(streamId >> 8);
        wire[8] = (byte)streamId;
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
