namespace http.Tests;

public class Http2InboundFlowControlTests
{
    [Test]
    public void PaddedDataConsumesBothWindowsAndAutomaticallyCreditsPadding()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.InitialWindowSize, 6u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, options);
        ProcessFrame(connection, CreateHeaders(1, false));
        Http2Frame data = CreateData(1, 3, false);
        data.IsPadded = true;
        data.PaddingLength = 2;

        Http2ConnectionResult result = ProcessFrame(connection, data);
        Http2StreamSnapshot stream = FindStream(result.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events.Single().Frame!.FlowControlledLength,
                Is.EqualTo(6));
            Assert.That(result.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 6));
            Assert.That(stream.ReceiveWindow, Is.EqualTo(3));
            Assert.That(stream.ReceivedContentLength, Is.EqualTo(3));
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ReleaseCapacity));
            Assert.That(result.Actions[0].Frame!.StreamId, Is.EqualTo(1));
            Assert.That(result.Actions[0].Capacity, Is.EqualTo(3));
        });
    }

    [Test]
    public void ReleasedCapacityIsBatchedForConnectionAndStreamWindows()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateData(1, 16000, false));
        ProcessFrame(connection, CreateData(1, 16000, false));
        ProcessFrame(connection, CreateData(1, 8000, false));

        Http2ConnectionResult first =
            connection.ReleaseHttp2Capacity(1, 10000);
        Http2ConnectionResult second =
            connection.ReleaseHttp2Capacity(1, 22768);
        Http2StreamSnapshot stream = FindStream(second.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Actions, Is.Empty);
            Assert.That(first.Snapshot!.ReceiveWindow, Is.EqualTo(25535));
            Assert.That(FindStream(first.Snapshot, 1).ReceiveWindow,
                Is.EqualTo(25535));
            Assert.That(second.Actions, Has.Count.EqualTo(2));
            Assert.That(second.Actions.All(item =>
                item.ActionKind == Http2ActionKind.ReleaseCapacity), Is.True);
            Assert.That(second.Actions[0].Frame!.StreamId, Is.Zero);
            Assert.That(second.Actions[0].Frame!.WindowIncrement,
                Is.EqualTo(32768));
            Assert.That(second.Actions[1].Frame!.StreamId, Is.EqualTo(1));
            Assert.That(second.Actions[1].Frame!.WindowIncrement,
                Is.EqualTo(32768));
            Assert.That(second.Snapshot!.ReceiveWindow, Is.EqualTo(58303));
            Assert.That(stream.ReceiveWindow, Is.EqualTo(58303));
        });
    }

    [Test]
    public void InitialWindowReductionFlushesAlreadyReleasedStreamCredit()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateData(1, 10, false));
        Http2ConnectionResult released =
            connection.ReleaseHttp2Capacity(1, 10);
        Http2Frame settings = CreateSettings();
        settings.Settings!.Values.Add(Setting(
            Http2SettingIdentifiers.InitialWindowSize, 0u));

        Http2ConnectionResult result = QueueFrame(connection, settings);

        Assert.Multiple(() =>
        {
            Assert.That(released.Actions, Is.Empty);
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions, Has.Count.EqualTo(3));
            Assert.That(result.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Settings));
            Assert.That(result.Actions[1].ActionKind,
                Is.EqualTo(Http2ActionKind.StartSettingsTimeout));
            Assert.That(result.Actions[2].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.WindowUpdate));
            Assert.That(result.Actions[2].Frame!.StreamId, Is.EqualTo(1));
            Assert.That(result.Actions[2].Capacity, Is.EqualTo(10));
            Assert.That(FindStream(result.Snapshot!, 1).ReceiveWindow,
                Is.Zero);
        });
    }

    [Test]
    public void ConnectionWindowViolationIsAConnectionFlowControlError()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateData(1, 16384, false));
        ProcessFrame(connection, CreateData(1, 16384, false));
        ProcessFrame(connection, CreateData(1, 16384, false));

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, 16384, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("http2-connection-flow-control-exceeded"));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.Snapshot!.ReceiveWindow, Is.EqualTo(16383));
        });
    }

    [Test]
    public void StreamWindowViolationResetsOnlyTheStream()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.InitialWindowSize, 5u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, options);
        ProcessFrame(connection, CreateHeaders(1, false));

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, 6, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("http2-stream-flow-control-exceeded"));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Actions[0].ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(result.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 6));
            Assert.That(FindStream(result.Snapshot, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
        });
    }

    [Test]
    public void DataAfterRemoteEndStillConsumesConnectionCapacity()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1, true));

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, 10, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(result.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 10));
            Assert.That(FindStream(result.Snapshot, 1).ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
        });
    }

    [Test]
    public void DataRacingWithLocalResetIsDiscardedAndConnectionCounted()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1, false));
        QueueFrame(connection, CreateReset(1));

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, 10, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 10));
            Assert.That(FindStream(result.Snapshot, 1).ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
        });
    }

    [Test]
    public void CapacityReleaseAfterTrimmedStreamRestoresOnlyConnectionWindow()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.MaximumRetainedClosedStreams = 0;
        options.Limits.MaximumFrameSize = 32768;
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.MaxFrameSize, 32768u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, options);
        QueueFrame(connection, CreateHeaders(0, true));
        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateData(1, 32768, true));

        Http2ConnectionResult result =
            connection.ReleaseHttp2Capacity(1, 32768);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions, Has.Count.EqualTo(1));
            Assert.That(result.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.WindowUpdate));
            Assert.That(result.Actions[0].Frame!.StreamId, Is.Zero);
            Assert.That(result.Actions[0].Capacity, Is.EqualTo(32768));
            Assert.That(result.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(result.Snapshot.Streams, Is.Empty);
        });
    }

    [Test]
    public void InvalidOrExcessCapacityReleaseDoesNotChangeWindows()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(connection, CreateHeaders(1, false));
        ProcessFrame(connection, CreateData(1, 10, false));

        Http2ConnectionResult invalid =
            connection.ReleaseHttp2Capacity(1, 0);
        Http2ConnectionResult excess =
            connection.ReleaseHttp2Capacity(1, 11);
        Http2ConnectionResult connectionRelease =
            connection.ReleaseHttp2Capacity(0, 10);
        Http2ConnectionResult streamRelease =
            connection.ReleaseHttp2Capacity(1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.ErrorCodeName,
                Is.EqualTo("invalid-http2-capacity-release"));
            Assert.That(excess.ErrorCodeName,
                Is.EqualTo("http2-stream-capacity-over-release"));
            Assert.That(excess.Actions, Is.Empty);
            Assert.That(connectionRelease.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(streamRelease.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(streamRelease.Actions, Is.Empty);
            Assert.That(excess.Snapshot!.ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 10));
            Assert.That(FindStream(excess.Snapshot, 1).ReceiveWindow,
                Is.EqualTo(Http2Constants.DefaultInitialWindowSize - 10));
        });
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options)
    {
        options.EndpointRole = role;
        Http2Connection connection = new Http2Connection();
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionInput input = new Http2ConnectionInput();
        if (role == Http2EndpointRole.Server)
        {
            input.HasBytes = true;
            input.Bytes = Concat(
                Http2Constants.ClientConnectionPreface,
                BuildEmptySettingsWire());
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

    static Http2Frame CreateSettings()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
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

    static byte[] BuildEmptySettingsWire()
    {
        return new byte[]
        {
            0x00, 0x00, 0x00,
            0x04, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
