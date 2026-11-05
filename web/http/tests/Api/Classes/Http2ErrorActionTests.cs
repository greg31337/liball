namespace http.Tests;

public class Http2ErrorActionTests
{
    [Test]
    public void ConnectionErrorProducesOneDataRichTerminalGoAway()
    {
        Http2Connection connection = CreateReadyServer();
        ProcessFrame(connection, CreateHeaders(1, true, Array.Empty<byte>()));

        Http2ConnectionResult failed = ProcessFrame(
            connection, CreatePing(new byte[7]));
        Http2Action shutdown = failed.Actions.Single();
        Http2Event error = failed.Events.Single();
        Http2ConnectionResult laterInput = ProcessFrame(
            connection, CreatePing(new byte[8]));
        Http2ConnectionResult laterOutput = QueueFrame(
            connection, CreatePing(new byte[8]));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(failed.IsTerminal, Is.True);
            Assert.That(failed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(failed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(failed.ErrorCodeValue,
                Is.EqualTo((uint)Http2ErrorCode.FrameSizeError));
            Assert.That(failed.IsKnownErrorCode, Is.True);

            Assert.That(shutdown.ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
            Assert.That(shutdown.HasLastStreamId, Is.True);
            Assert.That(shutdown.LastStreamId, Is.EqualTo(1));
            Assert.That(shutdown.HasErrorCode, Is.True);
            Assert.That(shutdown.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(shutdown.ErrorCodeValue,
                Is.EqualTo((uint)Http2ErrorCode.FrameSizeError));
            Assert.That(shutdown.IsKnownErrorCode, Is.True);
            Assert.That(shutdown.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.GoAway));
            Assert.That(shutdown.Frame.LastStreamId, Is.EqualTo(1));
            Assert.That(shutdown.Frame.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(shutdown.Frame.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));

            Assert.That(error.EventKind, Is.EqualTo(Http2EventKind.Error));
            Assert.That(error.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(error.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(error.ErrorCodeValue,
                Is.EqualTo((uint)Http2ErrorCode.FrameSizeError));
            Assert.That(error.IsKnownErrorCode, Is.True);

            Assert.That(failed.Snapshot!.LastProcessedRemoteStreamId,
                Is.EqualTo(1));
            Assert.That(failed.Snapshot.SentGoAway, Is.True);
            Assert.That(failed.Snapshot.SentLastStreamId, Is.EqualTo(1));
            Assert.That(failed.Snapshot.SentGoAwayErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(laterInput.Status,
                Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(laterInput.Actions, Is.Empty);
            Assert.That(laterInput.Events, Is.Empty);
            Assert.That(laterOutput.Status,
                Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(laterOutput.Actions, Is.Empty);
        });
    }

    [Test]
    public void MalformedNewStreamDoesNotAdvanceTheProcessedWatermark()
    {
        Http2Connection connection = CreateReadyServer();
        Http2ConnectionResult accepted = ProcessFrame(
            connection, CreateHeaders(1, true, Array.Empty<byte>()));
        Http2ConnectionResult failed = ProcessFrame(
            connection, CreateHeaders(3, true, new byte[] { 0x80 }));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Snapshot!.LastProcessedRemoteStreamId,
                Is.EqualTo(1));
            Assert.That(failed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.CompressionError));
            Assert.That(failed.Snapshot!.HighestRemoteStreamId,
                Is.EqualTo(3));
            Assert.That(failed.Snapshot.LastProcessedRemoteStreamId,
                Is.EqualTo(1));
            Assert.That(failed.Actions.Single().Frame!.LastStreamId,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void StreamErrorProducesRstStreamAndConnectionRemainsUsable()
    {
        Http2Connection connection = CreateReadyServer();
        ProcessFrame(connection, CreateHeaders(1, true, Array.Empty<byte>()));

        Http2ConnectionResult failed = ProcessFrame(
            connection, CreateData(1));
        Http2Action reset = failed.Actions.Single();
        Http2Event error = failed.Events.Single();
        Http2StreamSnapshot stream = failed.Snapshot!.Streams.Single(
            item => item.StreamId == 1);
        Http2ConnectionResult ping = ProcessFrame(
            connection, CreatePing(new byte[8]));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(failed.IsTerminal, Is.False);
            Assert.That(failed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(failed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(reset.ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(reset.StreamId, Is.EqualTo(1));
            Assert.That(reset.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(reset.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.RstStream));
            Assert.That(reset.Frame.StreamId, Is.EqualTo(1));
            Assert.That(reset.Frame.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(error.EventKind, Is.EqualTo(Http2EventKind.Error));
            Assert.That(error.StreamId, Is.EqualTo(1));
            Assert.That(error.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(stream.IsReset, Is.True);
            Assert.That(stream.ResetByLocal, Is.True);
            Assert.That(stream.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(ping.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(ping.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
        });
    }

    [Test]
    public void ErrorGoAwayNeverIncreasesAnEarlierShutdownBoundary()
    {
        Http2Connection connection = CreateReadyServer();
        ProcessFrame(connection, CreateHeaders(1, true, Array.Empty<byte>()));
        ProcessFrame(connection, CreateHeaders(3, true, Array.Empty<byte>()));
        Http2ConnectionResult graceful = QueueFrame(
            connection, CreateGoAway(1));

        Http2ConnectionResult failed = ProcessFrame(
            connection, CreatePing(new byte[7]));

        Assert.Multiple(() =>
        {
            Assert.That(graceful.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(graceful.Snapshot!.LastProcessedRemoteStreamId,
                Is.EqualTo(3));
            Assert.That(graceful.Snapshot.SentLastStreamId, Is.EqualTo(1));
            Assert.That(failed.Actions.Single().Frame!.LastStreamId,
                Is.EqualTo(1));
            Assert.That(failed.Snapshot!.SentLastStreamId, Is.EqualTo(1));
            Assert.That(failed.Snapshot.SentGoAwayErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
        });
    }

    [Test]
    public void FinalGracefulBoundaryExcludesARefusedStream()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        Http2Setting maximum = new Http2Setting();
        maximum.Identifier = Http2SettingIdentifiers.MaxConcurrentStreams;
        maximum.Value = 1u;
        options.LocalSettings.Values.Add(maximum);
        Http2Connection connection = CreateReadyServerWithOptions(options);
        ProcessFrame(connection, CreateHeaders(1, false, Array.Empty<byte>()));
        Http2ConnectionResult refused = ProcessFrame(
            connection, CreateHeaders(3, false, Array.Empty<byte>()));

        Http2ConnectionResult first = QueueGracefulGoAway(connection);
        Http2ConnectionResult second = QueueGracefulGoAway(connection);

        Assert.Multiple(() =>
        {
            Assert.That(refused.ErrorCode,
                Is.EqualTo(Http2ErrorCode.RefusedStream));
            Assert.That(refused.Snapshot!.HighestRemoteStreamId,
                Is.EqualTo(3));
            Assert.That(refused.Snapshot.LastProcessedRemoteStreamId,
                Is.EqualTo(1));
            Assert.That(first.Actions.Single().Frame!.LastStreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(second.Actions.Single().Frame!.LastStreamId,
                Is.EqualTo(1));
        });
    }

    static Http2Connection CreateReadyServer()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        return CreateReadyServerWithOptions(options);
    }

    static Http2Connection CreateReadyServerWithOptions(
        Http2ConnectionOptions options)
    {
        options.EndpointRole = Http2EndpointRole.Server;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));

        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = Concat(
            Http2Constants.ClientConnectionPreface,
            new byte[] { 0, 0, 0, 4, 0, 0, 0, 0, 0 });
        Assert.That(connection.ProcessHttp2Input(input).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        return connection;
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

    static Http2ConnectionResult QueueFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        return connection.QueueHttp2Action(action);
    }

    static Http2Frame CreateHeaders(
        int streamId,
        bool endStream,
        byte[] headerBlock)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = headerBlock;
        return frame;
    }

    static Http2Frame CreateData(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.Data = new byte[] { 0x01 };
        return frame;
    }

    static Http2Frame CreatePing(byte[] data)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.OpaqueData = data;
        return frame;
    }

    static Http2Frame CreateGoAway(int lastStreamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = lastStreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = Http2ErrorCode.NoError;
        frame.ErrorCodeValue = (uint)Http2ErrorCode.NoError;
        frame.IsKnownErrorCode = true;
        return frame;
    }

    static byte[] Concat(byte[] left, byte[] right)
    {
        byte[] result = new byte[left.Length + right.Length];
        left.CopyTo(result, 0);
        right.CopyTo(result, left.Length);
        return result;
    }
}
