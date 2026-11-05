namespace http.Tests;

public class Http2ConcurrentStreamTests
{
    [Test]
    public void PeerLimitBlocksNewLocalStreamsWithoutConsumingIdentifiers()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 1u)
            });
        Http2ConnectionResult first = QueueFrame(
            connection, CreateHeaders(0, false));
        ProcessFrame(
            connection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 0u)));
        Http2ConnectionResult blocked = QueueFrame(
            connection, CreateHeaders(0, false));
        QueueFrame(connection, CreateReset(1));
        Http2ConnectionResult stillBlocked = QueueFrame(
            connection, CreateHeaders(0, false));
        ProcessFrame(
            connection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 1u)));
        Http2ConnectionResult resumed = QueueFrame(
            connection, CreateHeaders(0, false));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Snapshot!.ActiveLocalStreams, Is.EqualTo(1));
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(blocked.ErrorCodeName,
                Is.EqualTo("http2-maximum-local-streams-reached"));
            Assert.That(blocked.Actions, Is.Empty);
            Assert.That(stillBlocked.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(stillBlocked.Snapshot!.NextLocalStreamId,
                Is.EqualTo(3));
            Assert.That(resumed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(resumed.Actions.Single().Frame!.StreamId,
                Is.EqualTo(3));
            Assert.That(resumed.Snapshot!.ActiveLocalStreams, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExcessPeerStreamIsRefusedAndCleanupReleasesTheLimit()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.MaxConcurrentStreams, 1u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server,
            options,
            Array.Empty<Http2Setting>());
        ProcessFrame(connection, CreateHeaders(1, false));

        byte[] refusedHeaderBlock = Convert.FromHexString(
            "408825a849e95ba97d7f8925a849e95bb8e8b4bf");
        Http2Frame refusedHeaders = CreateHeaders(
            3, false, refusedHeaderBlock[..10]);
        refusedHeaders.EndHeaders = false;
        Http2ConnectionResult refused = ProcessFrame(
            connection, refusedHeaders);
        Http2ConnectionResult completedRefusal = ProcessFrame(
            connection,
            CreateContinuation(3, refusedHeaderBlock[10..]));
        ProcessFrame(connection, CreateReset(1));
        Http2ConnectionResult accepted = ProcessFrame(
            connection, CreateHeaders(5, false, new byte[] { 0xbe }));
        Http2StreamSnapshot refusedStream =
            FindStream(refused.Snapshot!, 3);

        Assert.Multiple(() =>
        {
            Assert.That(refused.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(refused.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(refused.ErrorCode,
                Is.EqualTo(Http2ErrorCode.RefusedStream));
            Assert.That(refused.IsTerminal, Is.False);
            Assert.That(refused.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(refused.Actions[0].ErrorCode,
                Is.EqualTo(Http2ErrorCode.RefusedStream));
            Assert.That(refused.Snapshot!.HighestRemoteStreamId,
                Is.EqualTo(3));
            Assert.That(refused.Snapshot.ActiveRemoteStreams,
                Is.EqualTo(1));
            Assert.That(refusedStream.StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(refusedStream.IsRetryEligible, Is.True);
            Assert.That(completedRefusal.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completedRefusal.Events, Is.Empty);
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(accepted.Snapshot!.ActiveRemoteStreams,
                Is.EqualTo(1));
            Assert.That(FindStream(accepted.Snapshot, 5).StreamState,
                Is.EqualTo(Http2StreamState.Open));
        });
    }

    [Test]
    public void ReservedLocalPushStreamsCountOnlyWhenActivated()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 1u)
            });
        ProcessFrame(connection, CreateHeaders(1, false));
        QueueFrame(connection, CreatePushPromise(1, 2));
        Http2ConnectionResult reserved = QueueFrame(
            connection, CreatePushPromise(1, 4));

        Http2ConnectionResult firstOpened = QueueFrame(
            connection, CreateHeaders(2, false));
        Http2ConnectionResult blocked = QueueFrame(
            connection, CreateHeaders(4, false));
        QueueFrame(connection, CreateData(2, true));
        Http2ConnectionResult secondOpened = QueueFrame(
            connection, CreateHeaders(4, false));

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Snapshot!.ActiveLocalStreams, Is.Zero);
            Assert.That(FindStream(reserved.Snapshot, 2).StreamState,
                Is.EqualTo(Http2StreamState.ReservedLocal));
            Assert.That(FindStream(reserved.Snapshot, 4).StreamState,
                Is.EqualTo(Http2StreamState.ReservedLocal));
            Assert.That(firstOpened.Snapshot!.ActiveLocalStreams,
                Is.EqualTo(1));
            Assert.That(blocked.ErrorCodeName,
                Is.EqualTo("http2-maximum-local-streams-reached"));
            Assert.That(blocked.Actions, Is.Empty);
            Assert.That(FindStream(blocked.Snapshot!, 4).StreamState,
                Is.EqualTo(Http2StreamState.ReservedLocal));
            Assert.That(secondOpened.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondOpened.Snapshot!.ActiveLocalStreams,
                Is.EqualTo(1));
            Assert.That(FindStream(secondOpened.Snapshot, 4).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedRemote));
        });
    }

    [Test]
    public void ReservedRemotePushStreamsAreRefusedOnlyOnActivation()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.MaxConcurrentStreams, 1u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            options,
            Array.Empty<Http2Setting>());
        QueueFrame(connection, CreateHeaders(0, false));
        ProcessFrame(connection, CreatePushPromise(1, 2));
        Http2ConnectionResult reserved = ProcessFrame(
            connection, CreatePushPromise(1, 4));

        Http2ConnectionResult firstOpened = ProcessFrame(
            connection, CreateHeaders(2, false));
        Http2ConnectionResult refused = ProcessFrame(
            connection, CreateHeaders(4, false));
        Http2StreamSnapshot refusedStream =
            FindStream(refused.Snapshot!, 4);

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Snapshot!.ActiveRemoteStreams, Is.Zero);
            Assert.That(FindStream(reserved.Snapshot, 2).StreamState,
                Is.EqualTo(Http2StreamState.ReservedRemote));
            Assert.That(FindStream(reserved.Snapshot, 4).StreamState,
                Is.EqualTo(Http2StreamState.ReservedRemote));
            Assert.That(firstOpened.Snapshot!.ActiveRemoteStreams,
                Is.EqualTo(1));
            Assert.That(refused.ErrorCode,
                Is.EqualTo(Http2ErrorCode.RefusedStream));
            Assert.That(refused.IsTerminal, Is.False);
            Assert.That(refused.Snapshot!.ActiveRemoteStreams,
                Is.EqualTo(1));
            Assert.That(refusedStream.StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(refusedStream.IsPushed, Is.True);
            Assert.That(refusedStream.IsRetryEligible, Is.False);
        });
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options,
        Http2Setting[] remoteSettings)
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
                BuildSettingsWire(remoteSettings));
        }
        else
        {
            input.Frame = CreateSettings(remoteSettings);
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
        return CreateHeaders(streamId, endStream, Array.Empty<byte>());
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

    static byte[] BuildSettingsWire(Http2Setting[] settings)
    {
        byte[] payload = new byte[settings.Length * 6];
        int index = 0;
        while (index < settings.Length)
        {
            int offset = index * 6;
            payload[offset] =
                (byte)(settings[index].Identifier >> 8 & 0xff);
            payload[offset + 1] =
                (byte)(settings[index].Identifier & 0xff);
            payload[offset + 2] =
                (byte)(settings[index].Value >> 24 & 0xff);
            payload[offset + 3] =
                (byte)(settings[index].Value >> 16 & 0xff);
            payload[offset + 4] =
                (byte)(settings[index].Value >> 8 & 0xff);
            payload[offset + 5] =
                (byte)(settings[index].Value & 0xff);
            index = index + 1;
        }

        byte[] wire = new byte[Http2Constants.FrameHeaderSize +
            payload.Length];
        wire[0] = (byte)(payload.Length >> 16 & 0xff);
        wire[1] = (byte)(payload.Length >> 8 & 0xff);
        wire[2] = (byte)(payload.Length & 0xff);
        wire[3] = 0x04;
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
