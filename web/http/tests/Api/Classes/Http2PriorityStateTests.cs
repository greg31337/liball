using System.Text;

namespace http.Tests;

public class Http2PriorityStateTests
{
    [Test]
    public void NegotiatedLegacyPrioritySignalsAreAcceptedAndMarkedIgnored()
    {
        Http2ConnectionOptions localIgnoreOptions = InitialSettingsOptions(
            Setting(Http2SettingIdentifiers.NoRfc7540Priorities, 1u));
        Http2Connection localIgnore = CreateReadyServer(
            localIgnoreOptions, Array.Empty<Http2Setting>());
        ProcessFrame(localIgnore, CreateHeaders(1, false));
        Http2ConnectionResult ignoredByLocal = ProcessFrame(
            localIgnore, CreateLegacyPriority(1));

        Http2Connection peerIgnore = CreateReadyServer(
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(Http2SettingIdentifiers.NoRfc7540Priorities, 1u)
            });
        ProcessFrame(peerIgnore, CreateHeaders(1, false));
        Http2ConnectionResult ignoredByPeer = ProcessFrame(
            peerIgnore, CreateLegacyPriority(1));

        Http2Connection accepted = CreateReadyServer(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        ProcessFrame(accepted, CreateHeaders(1, false));
        Http2ConnectionResult retained = ProcessFrame(
            accepted, CreateLegacyPriority(1));

        Http2Connection client = CreateReadyClient(
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(Http2SettingIdentifiers.NoRfc7540Priorities, 1u)
            });
        QueueFrame(client, CreateHeaders(0, false));
        Http2ConnectionResult outbound = QueueFrame(
            client, CreateLegacyPriority(1));

        Assert.Multiple(() =>
        {
            Assert.That(ignoredByLocal.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(ignoredByLocal.Events.Single().Frame!.PriorityIgnored,
                Is.True);
            Assert.That(ignoredByPeer.Events.Single().Frame!.PriorityIgnored,
                Is.True);
            Assert.That(retained.Events.Single().Frame!.PriorityIgnored,
                Is.False);
            Assert.That(outbound.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(outbound.Actions.Single().Frame!.PriorityIgnored,
                Is.True);
        });
    }

    [Test]
    public void PriorityUpdateRolesAndIdlePushTargetsAreEnforced()
    {
        Http2Connection client = CreateReadyClient(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult serverUpdate = ProcessFrame(
            client, CreatePriorityUpdate(1, false, "u=1"));

        Http2Connection server = CreateReadyServer(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult idlePush = ProcessFrame(
            server, CreatePriorityUpdate(2, true, "u=2"));

        Http2Connection outboundServer = CreateReadyServer(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult rejectedOutbound = QueueFrame(
            outboundServer, CreatePriorityUpdate(1, false, "u=3"));

        Http2ConnectionOptions disabledOptions = new Http2ConnectionOptions();
        disabledOptions.EnablePriorityUpdate = false;
        Http2Connection disabled = CreateReadyServer(
            disabledOptions, Array.Empty<Http2Setting>());
        Http2ConnectionResult ignored = ProcessFrame(
            disabled, CreatePriorityUpdate(1, false, "u=0"));
        Http2ConnectionResult opened = ProcessFrame(
            disabled, CreateHeaders(1, true));

        Assert.Multiple(() =>
        {
            Assert.That(serverUpdate.ErrorCodeName,
                Is.EqualTo("http2-server-sent-priority-update"));
            Assert.That(serverUpdate.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(serverUpdate.IsTerminal, Is.True);
            Assert.That(idlePush.ErrorCodeName,
                Is.EqualTo("http2-priority-update-idle-push"));
            Assert.That(idlePush.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(idlePush.IsTerminal, Is.True);
            Assert.That(rejectedOutbound.ErrorCodeName,
                Is.EqualTo("http2-server-cannot-send-priority-update"));
            Assert.That(rejectedOutbound.IsTerminal, Is.False);
            Assert.That(rejectedOutbound.Actions, Is.Empty);
            Assert.That(ignored.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(opened.Snapshot!, 1).Priority, Is.Null);
        });
    }

    [Test]
    public void LatestIdleRequestUpdateIsAppliedWhenStreamOpens()
    {
        Http2Connection server = CreateReadyServer(
            InitialSettingsOptions(Setting(
                Http2SettingIdentifiers.MaxConcurrentStreams, 2u)),
            Array.Empty<Http2Setting>());

        Http2ConnectionResult first = ProcessFrame(
            server, CreatePriorityUpdate(3, false, "u=7, i"));
        Http2ConnectionResult replacement = ProcessFrame(
            server, CreatePriorityUpdate(3, false, "u=1"));
        Http2ConnectionResult opened = ProcessFrame(
            server, CreateHeaders(3, true));
        Http2StreamSnapshot stream = FindStream(opened.Snapshot!, 3);

        Http2Connection limited = CreateReadyServer(
            InitialSettingsOptions(Setting(
                Http2SettingIdentifiers.MaxConcurrentStreams, 1u)),
            Array.Empty<Http2Setting>());
        ProcessFrame(limited, CreatePriorityUpdate(1, false, "u=1"));
        Http2ConnectionResult exceeded = ProcessFrame(
            limited, CreatePriorityUpdate(3, false, "u=2"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(replacement.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(stream.PriorityUpdate, Is.Not.Null);
            Assert.That(stream.PriorityUpdate!.Urgency, Is.EqualTo(1));
            Assert.That(stream.PriorityUpdate.HasIncremental, Is.False);
            Assert.That(stream.Priority!.Urgency, Is.EqualTo(1));
            Assert.That(exceeded.ErrorCodeName,
                Is.EqualTo("http2-priority-update-concurrency"));
            Assert.That(exceeded.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(exceeded.IsTerminal, Is.True);
        });
    }

    [Test]
    public void ClientAndServerHeaderSignalsMergeUntilUpdateOverridesThem()
    {
        Http2Connection client = CreateReadyClient(
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(Http2SettingIdentifiers.NoRfc7540Priorities, 1u)
            });
        Http2ConnectionResult request = QueueMessageHeaders(
            client, 0, true, CreatePriorityHeaders("u=5, i", false));
        Http2ConnectionResult response = ProcessFrame(
            client,
            CreateEncodedHeaders(
                1, false, CreatePriorityHeaders("u=1", true)));
        Http2StreamSnapshot merged = FindStream(response.Snapshot!, 1);

        Http2ConnectionResult updated = QueueFrame(
            client, CreatePriorityUpdate(1, false, "u=6"));
        Http2StreamSnapshot overridden = FindStream(updated.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(merged.ClientPriority!.Urgency, Is.EqualTo(5));
            Assert.That(merged.ClientPriority.Incremental, Is.True);
            Assert.That(merged.ServerPriority!.Urgency, Is.EqualTo(1));
            Assert.That(merged.Priority!.Urgency, Is.EqualTo(1));
            Assert.That(merged.Priority.Incremental, Is.True);
            Assert.That(overridden.PriorityUpdate!.Urgency, Is.EqualTo(6));
            Assert.That(overridden.Priority!.Urgency, Is.EqualTo(6));
            Assert.That(overridden.Priority.HasIncremental, Is.False);
            Assert.That(overridden.Priority.Incremental, Is.False);
        });
    }

    [Test]
    public void PushStreamCanBeReprioritizedOnlyAfterReservation()
    {
        Http2Connection server = CreateReadyServer(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        ProcessFrame(server, CreateHeaders(1, true));
        QueuePush(server, 1, 2);

        Http2ConnectionResult updated = ProcessFrame(
            server, CreatePriorityUpdate(2, true, "u=0, i"));
        Http2StreamSnapshot pushed = FindStream(updated.Snapshot!, 2);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(pushed.StreamState,
                Is.EqualTo(Http2StreamState.ReservedLocal));
            Assert.That(pushed.PriorityUpdate!.Urgency, Is.Zero);
            Assert.That(pushed.Priority!.Incremental, Is.True);
        });
    }

    static Http2ConnectionOptions InitialSettingsOptions(
        Http2Setting setting)
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(setting);
        return options;
    }

    static Http2Connection CreateReadyClient(
        Http2ConnectionOptions options,
        Http2Setting[] remoteSettings)
    {
        options.EndpointRole = Http2EndpointRole.Client;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        ProcessFrame(connection, CreateSettings(remoteSettings));
        return connection;
    }

    static Http2Connection CreateReadyServer(
        Http2ConnectionOptions options,
        Http2Setting[] remoteSettings)
    {
        options.EndpointRole = Http2EndpointRole.Server;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = Concat(
            Http2Constants.ClientConnectionPreface,
            BuildSettingsWire(remoteSettings));
        Assert.That(connection.ProcessHttp2Input(input).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2ConnectionResult QueueMessageHeaders(
        Http2Connection connection,
        int streamId,
        bool endStream,
        HttpHeaders headers)
    {
        Http2Frame frame = CreateHeaders(streamId, endStream);
        Http2MessageResult message = new Http2MessageResult();
        message.Headers = headers;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        action.Message = message;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult QueuePush(
        Http2Connection connection,
        int streamId,
        int promisedStreamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = streamId;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.EndHeaders = true;
        Http2MessageResult message = new Http2MessageResult();
        message.Headers.Fields.Add(CreateField(":method", "GET"));
        message.Headers.Fields.Add(CreateField(":scheme", "https"));
        message.Headers.Fields.Add(CreateField(":path", "/asset"));
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        action.Message = message;
        return connection.QueueHttp2Action(action);
    }

    static HttpHeaders CreatePriorityHeaders(
        string priority,
        bool response)
    {
        HttpHeaders headers = new HttpHeaders();
        if (response)
        {
            headers.Fields.Add(CreateField(":status", "200"));
        }
        else
        {
            headers.Fields.Add(CreateField(":method", "GET"));
            headers.Fields.Add(CreateField(":scheme", "https"));
            headers.Fields.Add(CreateField(":path", "/"));
        }

        headers.Fields.Add(CreateField("priority", priority));
        return headers;
    }

    static Http2Frame CreateEncodedHeaders(
        int streamId,
        bool endStream,
        HttpHeaders headers)
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = HpackIndexingMode.WithoutIndexing;
        options.UseHuffman = false;
        HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
            headers, options);
        Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
        Http2Frame frame = CreateHeaders(streamId, endStream);
        frame.HeaderBlockFragment = encoded.Bytes;
        return frame;
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

    static Http2Frame CreateLegacyPriority(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Priority;
        frame.StreamId = streamId;
        frame.HasPriority = true;
        frame.StreamDependency = 0;
        frame.Weight = 16;
        return frame;
    }

    static Http2Frame CreatePriorityUpdate(
        int streamId,
        bool push,
        string value)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PriorityUpdate;
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = streamId;
        frame.PrioritizedElementIsPush = push;
        frame.PriorityFieldValue = Encoding.ASCII.GetBytes(value);
        return frame;
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

    static Http2Frame CreateSettings(Http2Setting[] settings)
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

    static HttpHeaderField CreateField(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
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
            payload[offset] = (byte)(settings[index].Identifier >> 8 & 0xff);
            payload[offset + 1] =
                (byte)(settings[index].Identifier & 0xff);
            payload[offset + 2] =
                (byte)(settings[index].Value >> 24 & 0xff);
            payload[offset + 3] =
                (byte)(settings[index].Value >> 16 & 0xff);
            payload[offset + 4] =
                (byte)(settings[index].Value >> 8 & 0xff);
            payload[offset + 5] = (byte)(settings[index].Value & 0xff);
            index = index + 1;
        }

        byte[] header = new byte[9];
        header[0] = (byte)(payload.Length >> 16 & 0xff);
        header[1] = (byte)(payload.Length >> 8 & 0xff);
        header[2] = (byte)(payload.Length & 0xff);
        header[3] = (byte)Http2FrameType.Settings;
        return Concat(header, payload);
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
