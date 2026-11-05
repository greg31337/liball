namespace http.Tests;

public class Http2FlowControlExhaustiveTests
{
    [Test]
    public void InboundConnectionAndStreamWindowsAllowTheExactBoundary()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1));
        ProcessFrame(connection, CreateData(1, 16384));
        ProcessFrame(connection, CreateData(1, 16384));
        ProcessFrame(connection, CreateData(1, 16384));
        Http2ConnectionResult exhausted = ProcessFrame(
            connection, CreateData(1, 16383));
        Http2ConnectionResult empty = ProcessFrame(
            connection, CreateData(1, 0));
        Http2ConnectionResult exceeded = ProcessFrame(
            connection, CreateData(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exhausted.Snapshot!.ReceiveWindow, Is.Zero);
            Assert.That(FindStream(exhausted.Snapshot, 1).ReceiveWindow,
                Is.Zero);
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Snapshot!.ReceiveWindow, Is.Zero);
            Assert.That(exceeded.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(exceeded.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(exceeded.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(exceeded.Snapshot!.ReceiveWindow, Is.Zero);
        });
    }

    [Test]
    public void InboundStreamsHaveIndependentWindowsAndSharedConnectionCredit()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.InitialWindowSize, 5u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, options);
        ProcessFrame(connection, CreateHeaders(1));
        ProcessFrame(connection, CreateHeaders(3));

        Http2ConnectionResult first = ProcessFrame(
            connection, CreateData(1, 5));
        Http2ConnectionResult second = ProcessFrame(
            connection, CreateData(3, 5));
        Http2ConnectionResult released =
            connection.ReleaseHttp2Capacity(1, 5);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(second.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 10));
            Assert.That(FindStream(second.Snapshot, 1).ReceiveWindow, Is.Zero);
            Assert.That(FindStream(second.Snapshot, 3).ReceiveWindow, Is.Zero);
            Assert.That(released.Actions, Has.Count.EqualTo(1));
            Assert.That(released.Actions[0].Frame!.StreamId, Is.EqualTo(1));
            Assert.That(released.Actions[0].Capacity, Is.EqualTo(5));
            Assert.That(released.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 10));
            Assert.That(released.Snapshot.PendingReceiveWindowCredit,
                Is.EqualTo(5));
            Assert.That(FindStream(released.Snapshot, 1).ReceiveWindow,
                Is.EqualTo(5));
            Assert.That(FindStream(released.Snapshot, 3).ReceiveWindow,
                Is.Zero);
        });
    }

    [Test]
    public void MaximumPaddingIsAutomaticallyCreditedWithoutApplicationRelease()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.InitialWindowSize, 256u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, options);
        ProcessFrame(connection, CreateHeaders(1));
        Http2Frame data = CreateData(1, 0);
        data.IsPadded = true;
        data.PaddingLength = 255;

        Http2ConnectionResult result = ProcessFrame(connection, data);
        Http2StreamSnapshot stream = FindStream(result.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events.Single().Frame!.FlowControlledLength,
                Is.EqualTo(256));
            Assert.That(result.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 256));
            Assert.That(result.Snapshot.UnreleasedReceiveCapacity, Is.Zero);
            Assert.That(result.Snapshot.PendingReceiveWindowCredit,
                Is.EqualTo(256));
            Assert.That(stream.ReceiveWindow, Is.EqualTo(256));
            Assert.That(stream.UnreleasedReceiveCapacity, Is.Zero);
            Assert.That(stream.PendingReceiveWindowCredit, Is.Zero);
            Assert.That(result.Actions.Single().Frame!.StreamId,
                Is.EqualTo(1));
            Assert.That(result.Actions[0].Capacity, Is.EqualTo(256));
        });
    }

    [Test]
    public void ZeroWindowIncrementUsesConnectionOrStreamProtocolScope()
    {
        Http2Connection connectionTarget = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        Http2ConnectionResult connectionError = ProcessFrame(
            connectionTarget, CreateWindowUpdate(0, 0));

        Http2Connection streamTarget = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        QueueFrame(streamTarget, CreateHeaders(0));
        Http2ConnectionResult streamError = ProcessFrame(
            streamTarget, CreateWindowUpdate(1, 0));

        Assert.Multiple(() =>
        {
            Assert.That(connectionError.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(connectionError.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(connectionError.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(connectionError.IsTerminal, Is.True);
            Assert.That(connectionError.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
            Assert.That(streamError.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(streamError.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(streamError.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(streamError.IsTerminal, Is.False);
            Assert.That(streamError.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
        });
    }

    [Test]
    public void DuplicateInitialWindowSettingsApplyTheirNetDeltaInOrder()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            new Http2ConnectionOptions(),
            Setting(Http2SettingIdentifiers.InitialWindowSize, 100u));
        QueueFrame(connection, CreateHeaders(0));
        QueueFrame(connection, CreateData(1, 80));
        Http2Frame settings = CreateSettings(
            Setting(Http2SettingIdentifiers.InitialWindowSize, 0u),
            Setting(Http2SettingIdentifiers.InitialWindowSize, 10u));

        Http2ConnectionResult changed = ProcessFrame(connection, settings);
        Http2ConnectionResult blocked = QueueFrame(
            connection, CreateData(1, 1));
        Http2ConnectionResult opened = QueueFrame(
            connection, CreateHeaders(0));

        Assert.Multiple(() =>
        {
            Assert.That(changed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(changed.Snapshot!.RemoteInitialStreamWindow,
                Is.EqualTo(10));
            Assert.That(FindStream(changed.Snapshot, 1).SendWindow,
                Is.EqualTo(-70));
            Assert.That(FindStream(changed.Snapshot, 1).IsFlowControlBlocked,
                Is.True);
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(opened.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(opened.Snapshot!, 3).SendWindow,
                Is.EqualTo(10));
        });
    }

    [Test]
    public void WindowUpdatesAcceptTheMaximumBoundaryAndRejectTheNextOctet()
    {
        int increment = Http2Constants.MaximumWindowSize -
            Http2Constants.DefaultInitialWindowSize + 1;
        Http2Connection connectionTarget = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        QueueFrame(connectionTarget, CreateHeaders(0));
        QueueFrame(connectionTarget, CreateData(1, 1));
        Http2ConnectionResult exactConnection = ProcessFrame(
            connectionTarget, CreateWindowUpdate(0, increment));
        Http2ConnectionResult connectionOverflow = ProcessFrame(
            connectionTarget, CreateWindowUpdate(0, 1));

        Http2Connection streamTarget = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        QueueFrame(streamTarget, CreateHeaders(0));
        QueueFrame(streamTarget, CreateData(1, 1));
        Http2ConnectionResult exactStream = ProcessFrame(
            streamTarget, CreateWindowUpdate(1, increment));
        Http2ConnectionResult streamOverflow = ProcessFrame(
            streamTarget, CreateWindowUpdate(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(exactConnection.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exactConnection.Snapshot!.SendWindow,
                Is.EqualTo(Http2Constants.MaximumWindowSize));
            Assert.That(connectionOverflow.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(connectionOverflow.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(connectionOverflow.IsTerminal, Is.True);
            Assert.That(exactStream.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(exactStream.Snapshot!, 1).SendWindow,
                Is.EqualTo(Http2Constants.MaximumWindowSize));
            Assert.That(streamOverflow.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(streamOverflow.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(streamOverflow.IsTerminal, Is.False);
        });
    }

    [Test]
    public void ZeroLengthDataCanPassAZeroStreamWindowWithoutReservation()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            new Http2ConnectionOptions(),
            Setting(Http2SettingIdentifiers.InitialWindowSize, 0u));
        QueueFrame(connection, CreateHeaders(0));

        Http2ConnectionResult empty = QueueFrame(
            connection, CreateData(1, 0));
        Http2ConnectionResult blocked = QueueFrame(
            connection, CreateData(1, 1));
        Http2ConnectionResult credited = ProcessFrame(
            connection, CreateWindowUpdate(1, 1));
        Http2ConnectionResult sent = QueueFrame(
            connection, CreateData(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Actions.Single().Capacity, Is.Zero);
            Assert.That(empty.Snapshot!.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(FindStream(empty.Snapshot, 1).SendWindow, Is.Zero);
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(blocked.Actions, Is.Empty);
            Assert.That(FindStream(credited.Snapshot!, 1).SendWindow,
                Is.EqualTo(1));
            Assert.That(sent.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(sent.Snapshot!, 1).SendWindow, Is.Zero);
        });
    }

    [Test]
    public void ClosingAStreamDiscardsPendingStreamCreditButKeepsCapacity()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1));
        ProcessFrame(connection, CreateData(1, 10));
        Http2ConnectionResult released =
            connection.ReleaseHttp2Capacity(1, 1);

        Http2ConnectionResult closed = ProcessFrame(
            connection, CreateReset(1));
        Http2ConnectionResult remainder =
            connection.ReleaseHttp2Capacity(1, 9);

        Assert.Multiple(() =>
        {
            Assert.That(released.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(released.Snapshot!.PendingReceiveWindowCredit,
                Is.EqualTo(1));
            Assert.That(FindStream(released.Snapshot, 1)
                .PendingReceiveWindowCredit, Is.EqualTo(1));
            Assert.That(closed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(closed.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(closed.Snapshot!, 1)
                .PendingReceiveWindowCredit, Is.Zero);
            Assert.That(FindStream(closed.Snapshot!, 1)
                .UnreleasedReceiveCapacity, Is.EqualTo(9));
            Assert.That(remainder.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(remainder.Snapshot!.UnreleasedReceiveCapacity,
                Is.Zero);
            Assert.That(remainder.Snapshot.PendingReceiveWindowCredit,
                Is.EqualTo(10));
            Assert.That(FindStream(remainder.Snapshot, 1)
                .PendingReceiveWindowCredit, Is.Zero);
        });
    }

    [Test]
    public void ResetRestoresAllFlowControlState()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1));
        ProcessFrame(connection, CreateData(1, 100));
        connection.ReleaseHttp2Capacity(1, 1);
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;

        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reset.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(reset.Snapshot.SendWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(reset.Snapshot.UnreleasedReceiveCapacity, Is.Zero);
            Assert.That(reset.Snapshot.PendingReceiveWindowCredit, Is.Zero);
            Assert.That(reset.Snapshot.IsFlowControlBlocked, Is.False);
            Assert.That(reset.Snapshot.Streams, Is.Empty);
        });
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options,
        params Http2Setting[] remoteSettings)
    {
        options.EndpointRole = role;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionInput input = new Http2ConnectionInput();
        if (role == Http2EndpointRole.Server)
        {
            input.HasBytes = true;
            input.Bytes = Concat(
                Http2Constants.ClientConnectionPreface,
                Serialize(CreateSettings(remoteSettings)));
        }
        else
        {
            input.Frame = CreateSettings(remoteSettings);
        }

        Assert.That(connection.ProcessHttp2Input(input).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        return connection;
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

    static Http2Frame CreateHeaders(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.HeaderBlockFragment = Array.Empty<byte>();
        return frame;
    }

    static Http2Frame CreateData(int streamId, int length)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.Data = new byte[length];
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

    static Http2Frame CreateReset(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = streamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Stream;
        frame.ErrorCode = Http2ErrorCode.Cancel;
        frame.ErrorCodeValue = (uint)Http2ErrorCode.Cancel;
        frame.IsKnownErrorCode = true;
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

    static byte[] Serialize(Http2Frame frame)
    {
        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return result.Bytes;
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }
}
