using System.Text;

namespace http.Tests;

public class Http2ExtensionIntegrationTests
{
    [Test]
    public void UnknownFramesArePreservedOrIgnoredWithoutDesynchronizingInput()
    {
        byte[] unknownPayload = { 0x00, 0x7f, 0x80, 0xff };
        byte[] pingPayload = Encoding.ASCII.GetBytes("12345678");
        byte[] input = Concat(
            BuildWire(0xfe, 0xa5, 7, unknownPayload),
            BuildWire(0x06, 0x00, 0, pingPayload));

        Http2Connection preservedConnection = CreateReadyClient(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult preserved = ProcessBytes(
            preservedConnection, input);

        Http2ConnectionOptions discardedOptions =
            new Http2ConnectionOptions();
        discardedOptions.PreserveUnknownFrames = false;
        Http2Connection discardedConnection = CreateReadyClient(
            discardedOptions, Array.Empty<Http2Setting>());
        Http2ConnectionResult discarded = ProcessBytes(
            discardedConnection, input);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(preserved.Consumed, Is.EqualTo(input.Length));
            Assert.That(preserved.Events, Has.Count.EqualTo(2));
            AssertUnknownFrame(
                preserved.Events[0].Frame!, unknownPayload);
            Assert.That(preserved.Events[1].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Ping));
            Assert.That(preserved.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
            Assert.That(preserved.Actions[0].Frame!.OpaqueData,
                Is.EqualTo(pingPayload));

            Assert.That(discarded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(discarded.Consumed, Is.EqualTo(input.Length));
            Assert.That(discarded.Events, Has.Count.EqualTo(1));
            Assert.That(discarded.Events[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Ping));
            Assert.That(discarded.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgePing));
            Assert.That(discarded.Snapshot!.Streams, Is.Empty);
        });
    }

    [Test]
    public void UnknownSettingsRemainOrderedAndSemanticallyInert()
    {
        Http2Connection connection = CreateReadyClient(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult first = ProcessFrame(
            connection,
            CreateSettings(
                Setting(0xf0f0, 0xf1234567u),
                Setting(Http2SettingIdentifiers.HeaderTableSize, 123u),
                Setting(0xf0f1, 0x89abcdefu)));
        Http2ConnectionResult second = ProcessFrame(
            connection,
            CreateSettings(Setting(0xf0f0, 7u)));
        Http2Settings settings = second.Snapshot!.RemoteSettings;

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgeSettings));
            Assert.That(first.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.SettingsChanged));
            Assert.That(settings.Values.Select(item => item.Identifier),
                Is.EqualTo(new[]
                {
                    0xf0f0,
                    Http2SettingIdentifiers.HeaderTableSize,
                    0xf0f1,
                    0xf0f0
                }));
            Assert.That(settings.Values.Select(item => item.Value),
                Is.EqualTo(new uint[]
                {
                    0xf1234567u, 123u, 0x89abcdefu, 7u
                }));
            Assert.That(settings.Values[0].IsKnown, Is.False);
            Assert.That(settings.Values[2].IsKnown, Is.False);
            Assert.That(settings.Values[3].IsKnown, Is.False);
            Assert.That(settings.HasHeaderTableSize, Is.True);
            Assert.That(settings.HeaderTableSize, Is.EqualTo(123));
            Assert.That(settings.HasEnableConnectProtocol, Is.False);
            Assert.That(settings.HasNoRfc7540Priorities, Is.False);
        });
    }

    [Test]
    public void LegacyAndExtensiblePrioritySignalsRemainDistinct()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.NoRfc7540Priorities, 1u));
        Http2Connection connection = CreateReadyServer(
            options, Array.Empty<Http2Setting>());
        HttpHeaders headers = RequestHeaders();
        headers.Fields.Add(Field(
            "priority", "u=6, i, client-extension=token"));
        Http2Frame legacy = CreateEncodedHeaders(1, false, headers);
        legacy.HasPriority = true;
        legacy.ExclusiveDependency = true;
        legacy.StreamDependency = 0;
        legacy.Weight = 256;

        Http2ConnectionResult opened = ProcessFrame(connection, legacy);
        Http2StreamSnapshot initial = FindStream(opened.Snapshot!, 1);
        Http2ConnectionResult updated = ProcessFrame(
            connection,
            CreatePriorityUpdate(
                1, "u=1, update-extension=\"kept\""));
        Http2StreamSnapshot current = FindStream(updated.Snapshot!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(opened.Events.Single().Frame!.PriorityIgnored,
                Is.True);
            Assert.That(initial.ClientPriority!.Urgency, Is.EqualTo(6));
            Assert.That(initial.ClientPriority.Incremental, Is.True);
            Assert.That(initial.ClientPriority.UnknownParameters.Single()
                .DictionaryKey, Is.EqualTo("client-extension"));
            Assert.That(initial.Priority!.Urgency, Is.EqualTo(6));
            Assert.That(updated.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(current.PriorityUpdate!.Urgency, Is.EqualTo(1));
            Assert.That(current.PriorityUpdate.UnknownParameters.Single()
                .DictionaryKey, Is.EqualTo("update-extension"));
            Assert.That(current.PriorityUpdate.UnknownParameters[0]
                .TextValue, Is.EqualTo("kept"));
            Assert.That(current.Priority!.Urgency, Is.EqualTo(1));
            Assert.That(current.Priority.Incremental, Is.False);
            Assert.That(current.Priority.UnknownParameters.Single()
                .DictionaryKey, Is.EqualTo("update-extension"));
        });
    }

    [Test]
    public void EveryOriginFlagOctetUsesItsDefinedCompatibilityRule()
    {
        int flags = 0;
        while (flags <= byte.MaxValue)
        {
            Http2Connection connection = CreateReadyClient(
                new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
            Http2Frame origin = new Http2Frame();
            origin.FrameType = Http2FrameType.Origin;
            origin.Flags = (byte)flags;
            origin.Origins.Add("https://extension.test");

            Http2ConnectionResult result = ProcessFrame(connection, origin);
            bool incompatible = (flags & 0x0f) != 0;

            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Done), flags.ToString());
            Assert.That(result.Events.Single().Frame!.OriginIgnored,
                Is.EqualTo(incompatible), flags.ToString());
            Assert.That(result.Snapshot!.OriginSetInitialized,
                Is.EqualTo(!incompatible), flags.ToString());
            Assert.That(result.Snapshot.Origins,
                incompatible ? Is.Empty :
                    Is.EqualTo(new[] { "https://extension.test" }),
                flags.ToString());
            flags = flags + 1;
        }
    }

    [Test]
    public void ExtendedConnectRequiresBothSettingsAndLocalPolicy()
    {
        Http2ConnectionOptions enabledOptions = new Http2ConnectionOptions();
        enabledOptions.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.EnableConnectProtocol, 1u));
        Http2Connection enabledServer = CreateReadyServer(
            enabledOptions, Array.Empty<Http2Setting>());
        Http2ConnectionResult acceptedInbound = ProcessFrame(
            enabledServer,
            CreateEncodedHeaders(1, false, ExtendedConnectHeaders()));

        Http2Connection absentServer = CreateReadyServer(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult absentInbound = ProcessFrame(
            absentServer,
            CreateEncodedHeaders(1, false, ExtendedConnectHeaders()));

        Http2ConnectionOptions disabledOptions = new Http2ConnectionOptions();
        disabledOptions.EnableExtendedConnect = false;
        disabledOptions.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.EnableConnectProtocol, 1u));
        Http2Connection disabledServer = CreateReadyServer(
            disabledOptions, Array.Empty<Http2Setting>());
        Http2ConnectionResult disabledInbound = ProcessFrame(
            disabledServer,
            CreateEncodedHeaders(1, false, ExtendedConnectHeaders()));

        Http2Connection enabledClient = CreateReadyClient(
            new Http2ConnectionOptions(),
            new[]
            {
                Setting(Http2SettingIdentifiers.EnableConnectProtocol, 1u)
            });
        Http2ConnectionResult acceptedOutbound = QueueRequest(
            enabledClient, ExtendedConnectRequest());

        Http2Connection absentClient = CreateReadyClient(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        Http2ConnectionResult absentOutbound = QueueRequest(
            absentClient, ExtendedConnectRequest());

        Assert.Multiple(() =>
        {
            Assert.That(acceptedInbound.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acceptedInbound.Events.Single().Request!
                .ConnectProtocol, Is.EqualTo("websocket"));
            Assert.That(acceptedInbound.Events[0].Message!.IsTunnel,
                Is.True);
            AssertConnectionError(
                absentInbound,
                "http2-connect-protocol-not-enabled",
                Http2ErrorScope.Stream);
            AssertConnectionError(
                disabledInbound,
                "http2-connect-protocol-not-enabled",
                Http2ErrorScope.Stream);
            Assert.That(acceptedOutbound.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acceptedOutbound.Actions.Single().Message!.IsTunnel,
                Is.True);
            Assert.That(absentOutbound.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(absentOutbound.ErrorCodeName,
                Is.EqualTo("http2-extended-connect-not-negotiated"));
            Assert.That(absentOutbound.Actions, Is.Empty);
        });
    }

    static void AssertUnknownFrame(Http2Frame frame, byte[] payload)
    {
        Assert.That(frame.FrameType, Is.EqualTo(Http2FrameType.Unknown));
        Assert.That(frame.TypeCode, Is.EqualTo(0xfe));
        Assert.That(frame.IsKnownType, Is.False);
        Assert.That(frame.Flags, Is.EqualTo(0xa5));
        Assert.That(frame.StreamId, Is.EqualTo(7));
        Assert.That(frame.Payload, Is.EqualTo(payload));
    }

    static void AssertConnectionError(
        Http2ConnectionResult result,
        string errorCodeName,
        Http2ErrorScope scope)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
        Assert.That(result.ErrorScope, Is.EqualTo(scope));
        Assert.That(result.ErrorCode,
            Is.EqualTo(Http2ErrorCode.ProtocolError));
    }

    static Http2Connection CreateReadyClient(
        Http2ConnectionOptions options,
        Http2Setting[] remoteSettings)
    {
        options.EndpointRole = Http2EndpointRole.Client;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        Assert.That(ProcessFrame(
            connection, CreateSettings(remoteSettings)).Status,
            Is.EqualTo(HttpOperationStatus.Done));
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
            SerializeFrame(CreateSettings(remoteSettings)));
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

    static Http2ConnectionResult ProcessBytes(
        Http2Connection connection,
        byte[] bytes)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = bytes;
        return connection.ProcessHttp2Input(input);
    }

    static Http2ConnectionResult QueueRequest(
        Http2Connection connection,
        HttpRequest request)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Request = request;
        input.EndStream = false;
        return connection.ProcessHttp2Input(input);
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
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = encoded.Bytes;
        return frame;
    }

    static Http2Frame CreatePriorityUpdate(int streamId, string value)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PriorityUpdate;
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = streamId;
        frame.PriorityFieldValue = Encoding.ASCII.GetBytes(value);
        return frame;
    }

    static HttpRequest ExtendedConnectRequest()
    {
        HttpRequest request = new HttpRequest();
        request.Method = HttpMethods.Connect;
        request.ConnectProtocol = "websocket";
        request.Scheme = "https";
        request.Authority = "server.example";
        request.Target = "/chat";
        return request;
    }

    static HttpHeaders ExtendedConnectHeaders()
    {
        return Headers(
            Field(":method", "CONNECT"),
            Field(":protocol", "websocket"),
            Field(":scheme", "https"),
            Field(":authority", "server.example"),
            Field(":path", "/chat"));
    }

    static HttpHeaders RequestHeaders()
    {
        return Headers(
            Field(":method", "GET"),
            Field(":scheme", "https"),
            Field(":authority", "server.example"),
            Field(":path", "/"));
    }

    static Http2StreamSnapshot FindStream(
        Http2ConnectionSnapshot snapshot,
        int streamId)
    {
        return snapshot.Streams.Single(item => item.StreamId == streamId);
    }

    static byte[] SerializeFrame(Http2Frame frame)
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
        byte[] result = new byte[
            Http2Constants.FrameHeaderSize + payload.Length];
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

    static HttpHeaders Headers(params HttpHeaderField[] fields)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.AddRange(fields);
        return headers;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }
}
