namespace http.Tests;

public class Http2ResetPingStateTests
{
    [Test]
    public void SettingsAndPingExposeCallerOwnedTimeoutActions()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        ProcessFrame(connection, CreateSettings(false));

        Http2ConnectionResult initialAcknowledgement = ProcessFrame(
            connection, CreateSettings(true));
        Http2Frame settingsUpdate = CreateSettings(false);
        settingsUpdate.Settings!.Values.Add(Setting(
            Http2SettingIdentifiers.MaxConcurrentStreams, 5u));
        Http2ConnectionResult queuedSettings = QueueFrame(
            connection, settingsUpdate);
        Http2ConnectionResult settingsAcknowledgement = ProcessFrame(
            connection, CreateSettings(true));

        byte[] pingData =
            [0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef];
        Http2ConnectionResult queuedPing = QueueFrame(
            connection, CreatePing(false, pingData));
        Http2ConnectionResult unmatched = ProcessFrame(
            connection,
            CreatePing(
                true,
                new byte[]
                {
                    0xff, 0xee, 0xdd, 0xcc,
                    0xbb, 0xaa, 0x99, 0x88
                }));
        Http2ConnectionResult acknowledged = ProcessFrame(
            connection, CreatePing(true, pingData));

        Assert.Multiple(() =>
        {
            Assert.That(reset.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.SendConnectionPreface,
                    Http2ActionKind.SendFrame,
                    Http2ActionKind.StartSettingsTimeout
                }));
            Assert.That(initialAcknowledgement.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.CancelSettingsTimeout));
            Assert.That(queuedSettings.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.SendFrame,
                    Http2ActionKind.StartSettingsTimeout
                }));
            Assert.That(queuedSettings.Actions[1].Settings!
                .MaxConcurrentStreams,
                Is.EqualTo(5));
            Assert.That(settingsAcknowledgement.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.CancelSettingsTimeout));
            Assert.That(queuedPing.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.SendFrame,
                    Http2ActionKind.StartPingTimeout
                }));
            Assert.That(queuedPing.Actions[1].OpaqueData,
                Is.EqualTo(pingData));
            Assert.That(unmatched.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(unmatched.Actions, Is.Empty);
            Assert.That(unmatched.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(acknowledged.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.CancelPingTimeout));
            Assert.That(acknowledged.Actions[0].OpaqueData,
                Is.EqualTo(pingData));
            Assert.That(acknowledged.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.PingAcknowledged));
            Assert.That(acknowledged.Events[0].PingData,
                Is.EqualTo(pingData));
        });
    }

    [Test]
    public void PingRequestsAreAcknowledgedExactlyAndAckSendingIsManaged()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        byte[] pingData =
            [0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80];

        Http2ConnectionResult received = ProcessFrame(
            connection, CreatePing(false, pingData));
        Http2ConnectionResult manualAcknowledgement = QueueFrame(
            connection, CreatePing(true, pingData));

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(received.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
            Assert.That(received.Actions[0].Frame!.Acknowledgement, Is.True);
            Assert.That(received.Actions[0].Frame!.OpaqueData,
                Is.EqualTo(pingData));
            Assert.That(received.Actions[0].OpaqueData,
                Is.EqualTo(pingData));
            Assert.That(received.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(manualAcknowledgement.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(manualAcknowledgement.ErrorCodeName,
                Is.EqualTo("http2-ping-ack-managed"));
            Assert.That(manualAcknowledgement.Actions, Is.Empty);
        });
    }

    [Test]
    public void ReceivedResetClosesStreamAndDeliversExactCancellation()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client);
        QueueFrame(connection, CreateHeaders(0, Array.Empty<byte>()));

        Http2ConnectionResult result = ProcessFrame(
            connection,
            CreateReset(1, Http2ErrorCode.RefusedStream));
        Http2StreamSnapshot stream = FindStream(result.Snapshot!, 1);
        Http2Event resetEvent = result.Events.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(resetEvent.EventKind,
                Is.EqualTo(Http2EventKind.StreamReset));
            Assert.That(resetEvent.StreamId, Is.EqualTo(1));
            Assert.That(resetEvent.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(resetEvent.ErrorCode,
                Is.EqualTo(Http2ErrorCode.RefusedStream));
            Assert.That(resetEvent.ErrorCodeValue,
                Is.EqualTo((uint)Http2ErrorCode.RefusedStream));
            Assert.That(stream.StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(stream.IsReset, Is.True);
            Assert.That(stream.ResetByLocal, Is.False);
            Assert.That(stream.IsRetryEligible, Is.True);
        });
    }

    [Test]
    public void LocalResetDiscardsLateFramesAndMaintainsSharedState()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(connection, CreateHeaders(1, Array.Empty<byte>()));
        Http2ConnectionResult cancellation = QueueFrame(
            connection, CreateReset(1, Http2ErrorCode.Cancel));

        byte[] indexedHeader = Convert.FromHexString(
            "408825a849e95ba97d7f8925a849e95bb8e8b4bf");
        Http2ConnectionResult lateHeaders = ProcessFrame(
            connection, CreateHeaders(1, indexedHeader));
        Http2ConnectionResult firstLateData = ProcessFrame(
            connection, CreateData(1, 16384));
        Http2ConnectionResult lateData = ProcessFrame(
            connection, CreateData(1, 16384));
        Http2ConnectionResult lateReset = ProcessFrame(
            connection, CreateReset(1, Http2ErrorCode.Cancel));
        Http2ConnectionResult nextStream = ProcessFrame(
            connection, CreateHeaders(3, new byte[] { 0xbe }));
        Http2StreamSnapshot cancelled =
            FindStream(nextStream.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(cancellation.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(cancellation.Snapshot!.ActiveRemoteStreams,
                Is.Zero);
            Assert.That(lateHeaders.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lateHeaders.Events, Is.Empty);
            Assert.That(firstLateData.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(firstLateData.Events, Is.Empty);
            Assert.That(firstLateData.Actions, Is.Empty);
            Assert.That(lateData.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lateData.Events, Is.Empty);
            Assert.That(lateData.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ReleaseCapacity));
            Assert.That(lateData.Actions[0].Frame!.StreamId, Is.Zero);
            Assert.That(lateReset.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lateReset.Events, Is.Empty);
            Assert.That(lateReset.Actions, Is.Empty);
            Assert.That(nextStream.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(nextStream.Events.Single().Message!.Headers.Fields
                .Single().Name,
                Is.EqualTo("custom-key"));
            Assert.That(cancelled.IsReset, Is.True);
            Assert.That(cancelled.ResetByLocal, Is.True);
            Assert.That(cancelled.ErrorCode,
                Is.EqualTo(Http2ErrorCode.Cancel));
        });
    }

    [Test]
    public void FramesAfterPeerResetAreNotAcceptedAsLocalResetRaces()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(connection, CreateHeaders(1, Array.Empty<byte>()));
        ProcessFrame(connection, CreateReset(1, Http2ErrorCode.Cancel));

        Http2ConnectionResult result = ProcessFrame(
            connection, CreateData(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void MalformedControlFramesUsePreciseConnectionErrorCodes()
    {
        Http2Connection pingSizeConnection = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult pingSize = ProcessBytes(
            pingSizeConnection, BuildWire(0x06, 0, new byte[7]));

        Http2Connection resetSizeConnection = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult resetSize = ProcessBytes(
            resetSizeConnection, BuildWire(0x03, 1, new byte[3]));

        Http2Connection pingStreamConnection = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult pingStream = ProcessBytes(
            pingStreamConnection, BuildWire(0x06, 1, new byte[8]));

        Http2Connection resetStreamConnection = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult resetStream = ProcessBytes(
            resetStreamConnection, BuildWire(0x03, 0, new byte[4]));

        Assert.Multiple(() =>
        {
            Assert.That(pingSize.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(pingSize.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(resetSize.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(resetSize.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(pingStream.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(resetStream.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
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
            input.Frame = CreateSettings(false);
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

    static Http2ConnectionResult ProcessBytes(
        Http2Connection connection,
        byte[] bytes)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = bytes;
        return connection.ProcessHttp2Input(input);
    }

    static Http2Frame CreateHeaders(
        int streamId,
        byte[] headerBlockFragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.HeaderBlockFragment = headerBlockFragment;
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

    static Http2Frame CreateReset(
        int streamId,
        Http2ErrorCode errorCode)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = streamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Stream;
        frame.ErrorCode = errorCode;
        frame.ErrorCodeValue = (uint)errorCode;
        frame.IsKnownErrorCode = true;
        return frame;
    }

    static Http2Frame CreatePing(bool acknowledgement, byte[] opaqueData)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.Acknowledgement = acknowledgement;
        frame.OpaqueData = opaqueData;
        return frame;
    }

    static Http2Frame CreateSettings(bool acknowledgement)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Acknowledgement = acknowledgement;
        frame.Settings = new Http2Settings();
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
