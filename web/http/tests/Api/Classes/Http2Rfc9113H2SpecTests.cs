namespace http.Tests;

public class Http2Rfc9113H2SpecTests
{
    [Test]
    public void Section34RejectsEveryCorruptedClientPrefaceOctet()
    {
        for (int index = 0;
            index < Http2Constants.ClientConnectionPreface.Length;
            index++)
        {
            Http2Connection connection = CreateConnection(
                Http2EndpointRole.Server, new Http2ConnectionOptions());
            byte[] preface = Http2Constants.ClientConnectionPreface.ToArray();
            preface[index] = (byte)(preface[index] ^ 0x01);

            Http2ConnectionResult result = ProcessBytes(
                connection, Concat(preface, Serialize(CreateSettings())));

            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected),
                $"Corrupted preface octet {index} was accepted.");
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("invalid-http2-client-preface"));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
        }
    }

    [Test]
    public void Section653AppliesAndAcknowledgesBatchedSettingsInOrder()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2Frame first = CreateSettings();
        first.Settings!.Values.Add(Setting(
            Http2SettingIdentifiers.MaxConcurrentStreams, 5u));
        Http2Frame second = CreateSettings();
        second.Settings!.Values.Add(Setting(
            Http2SettingIdentifiers.MaxConcurrentStreams, 2u));

        Http2ConnectionResult result = ProcessBytes(
            connection, Concat(Serialize(first), Serialize(second)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.AcknowledgeSettings,
                    Http2ActionKind.AcknowledgeSettings
                }));
            Assert.That(result.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.SettingsChanged
                }));
            Assert.That(result.Events[0].Settings!.MaxConcurrentStreams,
                Is.EqualTo(5));
            Assert.That(result.Events[1].Settings!.MaxConcurrentStreams,
                Is.EqualTo(2));
            Assert.That(result.Snapshot!.RemoteSettings.MaxConcurrentStreams,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void Section511AcceptsTheLargestPeerStreamIdentifier()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());

        Http2ConnectionResult result = ProcessFrame(
            connection,
            CreateHeaders(
                Http2Constants.MaximumStreamIdentifier,
                true,
                true,
                CreateRequestBlock()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Snapshot!.HighestRemoteStreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(result.Snapshot.LastProcessedRemoteStreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(result.Snapshot.Streams.Single().StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedRemote));
        });
    }

    [Test]
    public void Section51RejectsEveryDisallowedIdleStreamFrameClass()
    {
        Http2Frame[] frames =
        [
            CreateData(1, false),
            CreateReset(1),
            CreateWindowUpdate(1),
            CreateContinuation(1, true, new byte[] { 0x82 })
        ];

        foreach (Http2Frame frame in frames)
        {
            Http2Connection connection = CreateReadyConnection(
                Http2EndpointRole.Server, new Http2ConnectionOptions());
            Http2ConnectionResult result = ProcessFrame(connection, frame);

            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected),
                $"{frame.FrameType} was accepted on an idle stream.");
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
        }
    }

    [Test]
    public void Sections51And610AllowContinuationAfterEndStream()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2Frame headers = CreateHeaders(
            1, true, false, new byte[] { 0x82 });

        Http2ConnectionResult started = ProcessFrame(connection, headers);
        Http2ConnectionResult middle = ProcessFrame(
            connection,
            CreateContinuation(
                1, false, new byte[] { 0x87, 0x01, 0x01 }));
        Http2ConnectionResult completed = ProcessFrame(
            connection,
            CreateContinuation(1, true, new byte[] { (byte)'x', 0x84 }));

        Assert.Multiple(() =>
        {
            Assert.That(started.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(started.Events.Single().Message, Is.Null);
            Assert.That(middle.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(middle.Events.Single().Message, Is.Null);
            Assert.That(completed.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Events[0].Message!.IsComplete,
                Is.True);
            Assert.That(completed.Events[0].Message!.Headers.Fields
                .Select(field => field.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":authority", ":path"
                }));
            Assert.That(FindStream(completed.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedRemote));
        });
    }

    [Test]
    public void Section512RefusalReleasesConcurrencyAfterActiveStreamCloses()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.MaxConcurrentStreams, 1u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, options);

        Http2ConnectionResult first = ProcessFrame(
            connection,
            CreateHeaders(
                1, false, true, CreateRequestBlock()));
        Http2ConnectionResult refused = ProcessFrame(
            connection,
            CreateHeaders(
                3, false, true, CreateRequestBlock()));
        ProcessFrame(connection, CreateData(1, true));
        QueueFrame(
            connection,
            CreateHeaders(1, true, true, new byte[] { 0x88 }));
        Http2ConnectionResult admitted = ProcessFrame(
            connection,
            CreateHeaders(
                5, false, true, CreateRequestBlock()));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(refused.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(refused.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(refused.ErrorCode,
                Is.EqualTo(Http2ErrorCode.RefusedStream));
            Assert.That(refused.IsTerminal, Is.False);
            Assert.That(refused.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(admitted.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(admitted.Snapshot!.HighestRemoteStreamId,
                Is.EqualTo(5));
            Assert.That(admitted.Snapshot.ActiveRemoteStreams,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void Section66RejectsRegressingPromisedStreamIdentifiers()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        QueueFrame(
            connection,
            CreateHeaders(
                0, true, true, CreateRequestBlock()));

        Http2ConnectionResult first = ProcessFrame(
            connection,
            CreatePushPromise(1, 4));
        Http2ConnectionResult regressed = ProcessFrame(
            connection,
            CreatePushPromise(1, 2));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(first.Snapshot!, 4).StreamState,
                Is.EqualTo(Http2StreamState.ReservedRemote));
            Assert.That(regressed.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(regressed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(regressed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(regressed.ErrorCodeName,
                Is.EqualTo("http2-remote-stream-id-not-monotonic"));
            Assert.That(regressed.IsTerminal, Is.True);
        });
    }

    [Test]
    public void Sections64And67KeepResetAndPingSemanticsIndependent()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(
            connection,
            CreateHeaders(
                1, false, true, CreateRequestBlock()));
        Http2ConnectionResult reset = ProcessFrame(
            connection, CreateReset(1));
        byte[] firstData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] secondData = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        Http2ConnectionResult pings = ProcessBytes(
            connection,
            Concat(
                Serialize(CreatePing(false, firstData)),
                Serialize(CreatePing(true, secondData))));

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reset.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.StreamReset));
            Assert.That(FindStream(reset.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(pings.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(pings.IsTerminal, Is.False);
            Assert.That(pings.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
            Assert.That(pings.Actions[0].OpaqueData, Is.EqualTo(firstData));
            Assert.That(pings.Events, Has.Count.EqualTo(2));
            Assert.That(pings.Events[0].Frame!.Acknowledgement, Is.False);
            Assert.That(pings.Events[1].Frame!.Acknowledgement, Is.True);
        });
    }

    [Test]
    public void Section68AllowsExistingStreamsAndControlFramesAfterGoAway()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        QueueFrame(
            connection,
            CreateHeaders(
                0, true, true, CreateRequestBlock()));

        Http2ConnectionResult goAway = ProcessFrame(
            connection, CreateGoAway(1));
        Http2ConnectionResult newStream = QueueFrame(
            connection,
            CreateHeaders(
                0, true, true, CreateRequestBlock()));
        Http2ConnectionResult response = ProcessFrame(
            connection,
            CreateHeaders(1, true, true, new byte[] { 0x88 }));
        Http2ConnectionResult ping = ProcessFrame(
            connection,
            CreatePing(
                false,
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));

        Assert.Multiple(() =>
        {
            Assert.That(goAway.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(goAway.Snapshot!.ReceivedGoAway, Is.True);
            Assert.That(newStream.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(newStream.ErrorCodeName,
                Is.EqualTo("http2-goaway-received"));
            Assert.That(newStream.IsTerminal, Is.False);
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(response.Events[0].Message!.IsComplete, Is.True);
            Assert.That(FindStream(response.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(ping.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(ping.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
        });
    }

    [Test]
    public void Sections541And542ProduceScopeSpecificShutdownActions()
    {
        Http2Connection connectionError = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2ConnectionResult invalidPing = ProcessBytes(
            connectionError,
            BuildWire(0x06, 0, 0, new byte[7]));

        Http2Connection streamErrorConnection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(
            streamErrorConnection,
            CreateHeaders(
                1, true, true, CreateRequestBlock()));
        Http2ConnectionResult lateData = ProcessFrame(
            streamErrorConnection, CreateData(1, false));
        Http2ConnectionResult stillUsable = ProcessFrame(
            streamErrorConnection,
            CreatePing(false, new byte[Http2Constants.PingPayloadSize]));

        Assert.Multiple(() =>
        {
            Assert.That(invalidPing.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(invalidPing.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(invalidPing.IsTerminal, Is.True);
            Assert.That(invalidPing.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
            Assert.That(lateData.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(lateData.ErrorCode,
                Is.EqualTo(Http2ErrorCode.StreamClosed));
            Assert.That(lateData.IsTerminal, Is.False);
            Assert.That(lateData.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(stillUsable.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(stillUsable.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
        });
    }

    static Http2Connection CreateConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options)
    {
        options.EndpointRole = role;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options)
    {
        Http2Connection connection = CreateConnection(role, options);
        if (role == Http2EndpointRole.Server)
        {
            Http2ConnectionResult ready = ProcessBytes(
                connection,
                Concat(
                    Http2Constants.ClientConnectionPreface,
                    Serialize(CreateSettings())));
            Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
        }
        else
        {
            Http2ConnectionResult ready = ProcessFrame(
                connection, CreateSettings());
            Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
        }

        return connection;
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

    static Http2Frame CreateSettings()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
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

    static Http2Frame CreateHeaders(
        int streamId,
        bool endStream,
        bool endHeaders,
        byte[] block)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndStream = endStream;
        frame.EndHeaders = endHeaders;
        frame.HeaderBlockFragment = block;
        return frame;
    }

    static Http2Frame CreateContinuation(
        int streamId,
        bool endHeaders,
        byte[] block)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Continuation;
        frame.StreamId = streamId;
        frame.EndHeaders = endHeaders;
        frame.HeaderBlockFragment = block;
        return frame;
    }

    static Http2Frame CreateData(int streamId, bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.EndStream = endStream;
        frame.Data = new byte[] { 0x01 };
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

    static Http2Frame CreateWindowUpdate(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.StreamId = streamId;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = 1;
        return frame;
    }

    static Http2Frame CreatePushPromise(
        int streamId,
        int promisedStreamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.HeaderBlockFragment = CreateRequestBlock();
        return frame;
    }

    static byte[] CreateRequestBlock()
    {
        return new byte[]
        {
            0x82, 0x87, 0x01, 0x01, (byte)'x', 0x84
        };
    }

    static Http2Frame CreatePing(bool acknowledgement, byte[] data)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.Acknowledgement = acknowledgement;
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
        frame.ErrorCodeValue = 0;
        frame.IsKnownErrorCode = true;
        return frame;
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

    static byte[] BuildWire(
        byte type,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = type;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }
}
