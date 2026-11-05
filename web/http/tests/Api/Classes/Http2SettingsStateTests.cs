using System.Text;

namespace http.Tests;

public class Http2SettingsStateTests
{
    [Test]
    public void PeerSettingsApplyInOrderAndAreAcknowledgedImmediately()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        ProcessFrame(connection, CreateHeaders(1));
        Http2Frame update = CreateSettings(
            Setting(Http2SettingIdentifiers.InitialWindowSize, 1000u),
            Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 7u),
            Setting(Http2SettingIdentifiers.MaxFrameSize, 32768u),
            Setting(Http2SettingIdentifiers.MaxHeaderListSize, 100u),
            Setting(Http2SettingIdentifiers.EnableConnectProtocol, 1u));

        Http2ConnectionResult result = ProcessFrame(connection, update);
        ProcessFrame(connection, CreateHeaders(3));
        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgeSettings));
            Assert.That(result.Actions[0].Frame!.Acknowledgement, Is.True);
            Assert.That(result.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.SettingsChanged));
            Assert.That(snapshot.RemoteSettings.InitialWindowSize,
                Is.EqualTo(1000));
            Assert.That(snapshot.RemoteSettings.MaxConcurrentStreams,
                Is.EqualTo(7));
            Assert.That(snapshot.RemoteSettings.MaxFrameSize,
                Is.EqualTo(32768));
            Assert.That(snapshot.RemoteSettings.MaxHeaderListSize,
                Is.EqualTo(100));
            Assert.That(snapshot.RemoteSettings.EnableConnectProtocol,
                Is.True);
            Assert.That(FindStream(snapshot, 1).SendWindow,
                Is.EqualTo(1000));
            Assert.That(FindStream(snapshot, 3).SendWindow,
                Is.EqualTo(1000));
        });
    }

    [Test]
    public void SettingsAcknowledgementsMatchOutstandingFramesInFifoOrder()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        Http2ConnectionResult firstQueued = QueueSettings(
            connection,
            Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 1u));
        Http2ConnectionResult secondQueued = QueueSettings(
            connection,
            Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 2u));

        Http2ConnectionResult initialAck = ProcessFrame(
            connection, CreateSettingsAck());
        Http2ConnectionResult firstAck = ProcessFrame(
            connection, CreateSettingsAck());
        Http2ConnectionResult secondAck = ProcessFrame(
            connection, CreateSettingsAck());
        Http2ConnectionResult extraAck = ProcessFrame(
            connection, CreateSettingsAck());

        Assert.Multiple(() =>
        {
            Assert.That(firstQueued.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondQueued.Snapshot!.LocalSettings.MaxConcurrentStreams,
                Is.EqualTo(2));
            Assert.That(initialAck.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(initialAck.Snapshot!.InitialSettingsAcknowledged,
                Is.True);
            Assert.That(firstAck.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondAck.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(extraAck.ErrorCodeName,
                Is.EqualTo("http2-unexpected-settings-ack"));
            Assert.That(extraAck.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(extraAck.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(extraAck.IsTerminal, Is.True);
        });
    }

    [Test]
    public void HeaderTableReductionTakesEffectAtSettingsAcknowledgement()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.HeaderTableSize, 0u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            options,
            Array.Empty<Http2Setting>());
        QueueFrame(connection, CreateHeaders(0));

        Http2ConnectionResult beforeAck = ProcessFrame(
            connection,
            CreateHeaders(1, new byte[] { 0x88 }));
        Http2ConnectionResult acknowledgement = ProcessFrame(
            connection, CreateSettingsAck());
        Http2ConnectionResult afterAck = ProcessFrame(
            connection,
            CreateHeaders(1, new byte[] { 0x88 }));

        Assert.Multiple(() =>
        {
            Assert.That(beforeAck.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acknowledgement.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(afterAck.ErrorCodeName,
                Is.EqualTo("required-hpack-table-size-update"));
            Assert.That(afterAck.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(afterAck.ErrorCode,
                Is.EqualTo(Http2ErrorCode.CompressionError));
            Assert.That(afterAck.IsTerminal, Is.True);
        });
    }

    [Test]
    public void PeerHeaderTableAndHeaderListSettingsConstrainEncoder()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        ProcessFrame(
            connection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.HeaderTableSize, 0u)));
        HttpHeaders oneField = new HttpHeaders();
        oneField.Fields.Add(CreateField(":method", "GET"));
        Http2ConnectionResult tableLimited = QueueMessageHeaders(
            connection, oneField);

        ProcessFrame(
            connection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.MaxHeaderListSize, 33u)));
        HttpHeaders oversized = new HttpHeaders();
        oversized.Fields.Add(CreateField("a", "1"));
        Http2ConnectionResult listLimited = QueueMessageHeaders(
            connection, oversized);

        Assert.Multiple(() =>
        {
            Assert.That(tableLimited.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(tableLimited.Actions.Single().Frame!
                .HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x20, 0x82 }));
            Assert.That(listLimited.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(listLimited.ErrorCodeName,
                Is.EqualTo("hpack-header-list-size-limit-exceeded"));
            Assert.That(listLimited.IsTerminal, Is.False);
        });
    }

    [Test]
    public void PeerMaximumFrameSizeChangesOnlyOutboundFrameLimit()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.Limits.MaximumFrameSize = 32768;
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.MaxFrameSize, 32768u));
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client,
            options,
            Array.Empty<Http2Setting>());
        QueueFrame(connection, CreateHeaders(0));

        Http2ConnectionResult beforeUpdate = QueueFrame(
            connection, CreateData(1, 20000));
        ProcessFrame(
            connection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.MaxFrameSize, 32768u)));
        Http2ConnectionResult afterUpdate = QueueFrame(
            connection, CreateData(1, 20000));

        Assert.Multiple(() =>
        {
            Assert.That(beforeUpdate.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(beforeUpdate.ErrorCodeName,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(afterUpdate.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(afterUpdate.Actions.Single().Frame!.Data,
                Has.Length.EqualTo(20000));
        });
    }

    [Test]
    public void LocalInitialWindowUpdateChangesExistingAndFutureReceiveWindows()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        ProcessFrame(connection, CreateHeaders(1));

        Http2ConnectionResult update = QueueSettings(
            connection,
            Setting(Http2SettingIdentifiers.InitialWindowSize, 1234u));
        ProcessFrame(connection, CreateHeaders(3));
        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(update.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(update.Snapshot!.LocalSettings.InitialWindowSize,
                Is.EqualTo(1234));
            Assert.That(FindStream(snapshot, 1).ReceiveWindow,
                Is.EqualTo(1234));
            Assert.That(FindStream(snapshot, 3).ReceiveWindow,
                Is.EqualTo(1234));
        });
    }

    [Test]
    public void EnablePushConnectAndPrioritySettingRulesAreEnforced()
    {
        Http2Connection clientPushConnection = CreateConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        Http2ConnectionResult serverEnabledPush = ProcessFrame(
            clientPushConnection,
            CreateSettings(
                Setting(Http2SettingIdentifiers.EnablePush, 1u)));

        Http2Connection connectConnection = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        ProcessFrame(
            connectConnection,
            CreateSettings(
                Setting(
                    Http2SettingIdentifiers.EnableConnectProtocol, 1u)));
        Http2ConnectionResult disabledConnect = ProcessFrame(
            connectConnection,
            CreateSettings(
                Setting(
                    Http2SettingIdentifiers.EnableConnectProtocol, 0u)));

        Http2Connection latePriorityConnection = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        Http2ConnectionResult latePriority = ProcessFrame(
            latePriorityConnection,
            CreateSettings(
                Setting(
                    Http2SettingIdentifiers.NoRfc7540Priorities, 1u)));

        Http2Connection duplicatePriorityConnection = CreateConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        Http2ConnectionResult conflictingInitialPriority = ProcessFrame(
            duplicatePriorityConnection,
            CreateSettings(
                Setting(
                    Http2SettingIdentifiers.NoRfc7540Priorities, 1u),
                Setting(
                    Http2SettingIdentifiers.NoRfc7540Priorities, 0u)));

        Http2Connection priorityConnection = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(
                    Http2SettingIdentifiers.NoRfc7540Priorities, 1u)
            });
        Http2Frame priority = new Http2Frame();
        priority.FrameType = Http2FrameType.Priority;
        priority.StreamId = 1;
        priority.Weight = 16;
        Http2ConnectionResult ignoredPriority = ProcessFrame(
            priorityConnection, priority);
        Http2ConnectionResult changedPriority = ProcessFrame(
            priorityConnection,
            CreateSettings(
                Setting(
                    Http2SettingIdentifiers.NoRfc7540Priorities, 0u)));

        Assert.Multiple(() =>
        {
            Assert.That(serverEnabledPush.ErrorCodeName,
                Is.EqualTo("http2-server-enable-push"));
            Assert.That(serverEnabledPush.IsTerminal, Is.True);
            Assert.That(disabledConnect.ErrorCodeName,
                Is.EqualTo("http2-connect-protocol-disabled"));
            Assert.That(disabledConnect.IsTerminal, Is.True);
            Assert.That(latePriority.ErrorCodeName,
                Is.EqualTo("http2-priority-setting-not-initial"));
            Assert.That(latePriority.IsTerminal, Is.True);
            Assert.That(conflictingInitialPriority.ErrorCodeName,
                Is.EqualTo("http2-priority-setting-changed"));
            Assert.That(conflictingInitialPriority.IsTerminal, Is.True);
            Assert.That(ignoredPriority.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(ignoredPriority.Events.Single().Frame!
                .PriorityIgnored,
                Is.True);
            Assert.That(changedPriority.ErrorCodeName,
                Is.EqualTo("http2-priority-setting-changed"));
            Assert.That(changedPriority.IsTerminal, Is.True);
        });
    }

    [Test]
    public void LocalRoleAndOneWaySettingViolationsAreRejectedWithoutSend()
    {
        Http2Connection server = CreateReadyConnection(
            Http2EndpointRole.Server,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        Http2ConnectionResult serverPush = QueueSettings(
            server,
            Setting(Http2SettingIdentifiers.EnablePush, 1u));

        Http2Connection connect = CreateReadyConnection(
            Http2EndpointRole.Client,
            InitialSettingsOptions(
                Setting(
                    Http2SettingIdentifiers.EnableConnectProtocol, 1u)),
            Array.Empty<Http2Setting>());
        Http2ConnectionResult connectDisabled = QueueSettings(
            connect,
            Setting(Http2SettingIdentifiers.EnableConnectProtocol, 0u));

        Http2Connection priority = CreateReadyConnection(
            Http2EndpointRole.Client,
            new Http2ConnectionOptions(),
            Array.Empty<Http2Setting>());
        Http2ConnectionResult latePriority = QueueSettings(
            priority,
            Setting(
                Http2SettingIdentifiers.NoRfc7540Priorities, 1u));

        Assert.Multiple(() =>
        {
            Assert.That(serverPush.ErrorCodeName,
                Is.EqualTo("http2-server-enable-push"));
            Assert.That(serverPush.Actions, Is.Empty);
            Assert.That(serverPush.IsTerminal, Is.False);
            Assert.That(connectDisabled.ErrorCodeName,
                Is.EqualTo("http2-connect-protocol-disabled"));
            Assert.That(connectDisabled.Actions, Is.Empty);
            Assert.That(latePriority.ErrorCodeName,
                Is.EqualTo("http2-priority-setting-not-initial"));
            Assert.That(latePriority.Actions, Is.Empty);
        });
    }

    [Test]
    public void InvalidInitialWindowSettingUsesFlowControlError()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        byte[] invalidSettings = BuildSettingsWire(
            Setting(
                Http2SettingIdentifiers.InitialWindowSize,
                0x80000000u));
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = Concat(
            Http2Constants.ClientConnectionPreface, invalidSettings);

        Http2ConnectionResult result = connection.ProcessHttp2Input(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.FlowControlError));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    static Http2ConnectionOptions InitialSettingsOptions(
        Http2Setting setting)
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(setting);
        return options;
    }

    static Http2Connection CreateConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options)
    {
        Http2Connection connection = new Http2Connection();
        options.EndpointRole = role;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options,
        Http2Setting[] remoteSettings)
    {
        Http2Connection connection = CreateConnection(role, options);
        Http2Frame settings = CreateSettings(remoteSettings);
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
            input.Frame = settings;
        }

        Http2ConnectionResult ready = connection.ProcessHttp2Input(input);
        Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
        Assert.That(ready.IsReady, Is.True);
        return connection;
    }

    static Http2ConnectionResult QueueSettings(
        Http2Connection connection,
        Http2Setting setting)
    {
        return QueueFrame(connection, CreateSettings(setting));
    }

    static Http2ConnectionResult QueueMessageHeaders(
        Http2Connection connection,
        HttpHeaders headers)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.Headers = headers;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = CreateHeaders(0);
        action.Message = message;
        return connection.QueueHttp2Action(action);
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

    static Http2Frame CreateHeaders(int streamId)
    {
        return CreateHeaders(streamId, Array.Empty<byte>());
    }

    static Http2Frame CreateHeaders(int streamId, byte[] fragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.HeaderBlockFragment = fragment;
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

    static Http2Frame CreateSettings(params Http2Setting[] settings)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        frame.Settings.Values.AddRange(settings);
        return frame;
    }

    static Http2Frame CreateSettingsAck()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Acknowledgement = true;
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

    static byte[] BuildSettingsWire(params Http2Setting[] settings)
    {
        byte[] payload = new byte[settings.Length * 6];
        int index = 0;
        while (index < settings.Length)
        {
            int offset = index * 6;
            payload[offset] = (byte)(settings[index].Identifier >> 8);
            payload[offset + 1] = (byte)settings[index].Identifier;
            payload[offset + 2] = (byte)(settings[index].Value >> 24);
            payload[offset + 3] = (byte)(settings[index].Value >> 16);
            payload[offset + 4] = (byte)(settings[index].Value >> 8);
            payload[offset + 5] = (byte)settings[index].Value;
            index = index + 1;
        }

        byte[] wire = new byte[9 + payload.Length];
        wire[0] = (byte)(payload.Length >> 16);
        wire[1] = (byte)(payload.Length >> 8);
        wire[2] = (byte)payload.Length;
        wire[3] = 0x04;
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
