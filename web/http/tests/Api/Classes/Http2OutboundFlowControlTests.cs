namespace http.Tests;

public class Http2OutboundFlowControlTests
{
    [Test]
    public void DataReservationCountsPaddingAndDecrementsBothSendWindows()
    {
        Http2Connection connection = CreateReadyClient();
        QueueFrame(connection, CreateHeaders(0, false));
        Http2Frame data = CreateData(1, 3, false);
        data.IsPadded = true;
        data.PaddingLength = 2;

        Http2ConnectionResult result = QueueFrame(connection, data);
        Http2StreamSnapshot stream = FindStream(result.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions.Single().Capacity, Is.EqualTo(6));
            Assert.That(result.Snapshot!.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 6));
            Assert.That(stream.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 6));
            Assert.That(stream.SentContentLength, Is.EqualTo(3));
        });
    }

    [Test]
    public void StreamBlockedDataRemainsCallerOwnedUntilWindowUpdate()
    {
        Http2Connection connection = CreateReadyClient(
            Setting(Http2SettingIdentifiers.InitialWindowSize, 5u));
        QueueFrame(connection, CreateHeaders(0, false));
        Http2Action action = SendAction(CreateData(1, 6, false));

        Http2ConnectionResult blocked =
            connection.QueueHttp2Action(action);
        Http2ConnectionResult update = ProcessFrame(
            connection, CreateWindowUpdate(1, 1));
        Http2ConnectionResult sent =
            connection.QueueHttp2Action(action);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(blocked.ErrorCodeName,
                Is.EqualTo("http2-send-flow-control-blocked"));
            Assert.That(blocked.Actions, Is.Empty);
            Assert.That(blocked.Snapshot!.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(FindStream(blocked.Snapshot, 1).SendWindow,
                Is.EqualTo(5));
            Assert.That(FindStream(blocked.Snapshot, 1).SentContentLength,
                Is.Zero);
            Assert.That(action.Capacity, Is.EqualTo(6));
            Assert.That(update.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(update.Snapshot!, 1).SendWindow,
                Is.EqualTo(6));
            Assert.That(sent.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(sent.Actions.Single(), Is.SameAs(action));
            Assert.That(sent.Snapshot!.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 6));
            Assert.That(FindStream(sent.Snapshot, 1).SendWindow, Is.Zero);
        });
    }

    [Test]
    public void ConnectionWindowBlocksWithoutHiddenBufferingAndCanResume()
    {
        Http2Connection connection = CreateReadyClient(
            Setting(Http2SettingIdentifiers.InitialWindowSize, 100000u));
        QueueFrame(connection, CreateHeaders(0, false));
        QueueFrame(connection, CreateData(1, 16384, false));
        QueueFrame(connection, CreateData(1, 16384, false));
        QueueFrame(connection, CreateData(1, 16384, false));
        QueueFrame(connection, CreateData(1, 16383, false));
        Http2Action action = SendAction(CreateData(1, 1, false));

        Http2ConnectionResult blocked =
            connection.QueueHttp2Action(action);
        Http2ConnectionResult update = ProcessFrame(
            connection, CreateWindowUpdate(0, 1));
        Http2ConnectionResult sent =
            connection.QueueHttp2Action(action);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(blocked.Actions, Is.Empty);
            Assert.That(blocked.Snapshot!.SendWindow, Is.Zero);
            Assert.That(FindStream(blocked.Snapshot, 1).SendWindow,
                Is.EqualTo(34465));
            Assert.That(update.Snapshot!.SendWindow, Is.EqualTo(1));
            Assert.That(sent.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(sent.Snapshot!.SendWindow, Is.Zero);
            Assert.That(FindStream(sent.Snapshot, 1).SentContentLength,
                Is.EqualTo(65536));
        });
    }

    [Test]
    public void InitialWindowReductionCanRemainNegativeUntilEnoughCreditArrives()
    {
        Http2Connection connection = CreateReadyClient(
            Setting(Http2SettingIdentifiers.InitialWindowSize, 100u));
        QueueFrame(connection, CreateHeaders(0, false));
        QueueFrame(connection, CreateData(1, 80, false));
        ProcessFrame(
            connection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.InitialWindowSize, 10u)));
        Http2ConnectionResult negative = ProcessFrame(
            connection, CreateWindowUpdate(1, 50));
        Http2Action action = SendAction(CreateData(1, 1, false));
        Http2ConnectionResult stillBlocked =
            connection.QueueHttp2Action(action);
        Http2ConnectionResult positive = ProcessFrame(
            connection, CreateWindowUpdate(1, 21));
        Http2ConnectionResult sent =
            connection.QueueHttp2Action(action);

        Assert.Multiple(() =>
        {
            Assert.That(FindStream(negative.Snapshot!, 1).SendWindow,
                Is.EqualTo(-20));
            Assert.That(stillBlocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(stillBlocked.Actions, Is.Empty);
            Assert.That(FindStream(positive.Snapshot!, 1).SendWindow,
                Is.EqualTo(1));
            Assert.That(sent.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(sent.Snapshot!, 1).SendWindow, Is.Zero);
        });
    }

    [Test]
    public void WindowUpdateOverflowUsesTheApplicableProtocolScope()
    {
        Http2Connection connectionOverflow = CreateReadyClient();
        Http2ConnectionResult connectionResult = ProcessFrame(
            connectionOverflow,
            CreateWindowUpdate(0, Http2Constants.MaximumWindowSize));

        Http2Connection streamOverflow = CreateReadyClient(
            Setting(
                Http2SettingIdentifiers.InitialWindowSize,
                (uint)Http2Constants.MaximumWindowSize));
        QueueFrame(streamOverflow, CreateHeaders(0, false));
        Http2ConnectionResult streamResult = ProcessFrame(
            streamOverflow, CreateWindowUpdate(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(connectionResult.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(connectionResult.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(connectionResult.ErrorCodeName,
                Is.EqualTo("http2-connection-window-update-overflow"));
            Assert.That(connectionResult.IsTerminal, Is.True);
            Assert.That(streamResult.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(streamResult.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(streamResult.ErrorCodeName,
                Is.EqualTo("http2-stream-window-update-overflow"));
            Assert.That(streamResult.IsTerminal, Is.False);
            Assert.That(streamResult.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
        });
    }

    [Test]
    public void LateWindowUpdateOnClosedStreamIsIgnored()
    {
        Http2Connection connection = CreateReadyClient();
        QueueFrame(connection, CreateHeaders(0, true));
        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateData(1, 0, true));

        Http2ConnectionResult result = ProcessFrame(
            connection,
            CreateWindowUpdate(1, Http2Constants.MaximumWindowSize));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(FindStream(result.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(result.Snapshot!, 1).SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
        });
    }

    [Test]
    public void MismatchedExplicitReservationIsRejectedWithoutMutation()
    {
        Http2Connection connection = CreateReadyClient();
        QueueFrame(connection, CreateHeaders(0, false));
        Http2Action action = SendAction(CreateData(1, 3, false));
        action.Capacity = 2;

        Http2ConnectionResult result =
            connection.QueueHttp2Action(action);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("invalid-http2-capacity-reservation"));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Snapshot!.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(FindStream(result.Snapshot, 1).SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(FindStream(result.Snapshot, 1).SentContentLength,
                Is.Zero);
        });
    }

    static Http2Connection CreateReadyClient(
        params Http2Setting[] settings)
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        Http2Connection connection = new Http2Connection();
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionResult ready = ProcessFrame(
            connection, CreateSettings(settings));
        Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2ConnectionResult QueueFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        return connection.QueueHttp2Action(SendAction(frame));
    }

    static Http2Action SendAction(Http2Frame frame)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        return action;
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
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = Array.Empty<byte>();
        return frame;
    }

    static Http2Frame CreateData(
        int streamId,
        int length,
        bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.Data = new byte[length];
        frame.EndStream = endStream;
        return frame;
    }

    static Http2Frame CreateSettings(params Http2Setting[] settings)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        frame.Settings.Values.AddRange(settings);
        return frame;
    }

    static Http2Frame CreateWindowUpdate(int streamId, int increment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.StreamId = streamId;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = increment;
        return frame;
    }

    static Http2Setting Setting(int identifier, uint value)
    {
        Http2Setting setting = new Http2Setting();
        setting.Identifier = identifier;
        setting.Value = value;
        return setting;
    }

    static Http2StreamSnapshot FindStream(
        Http2ConnectionSnapshot snapshot,
        int streamId)
    {
        return snapshot.Streams.Single(item => item.StreamId == streamId);
    }
}
