namespace http.Tests;

public class Http2ConnectionPrefaceTests
{
    [Test]
    public void ClientResetProducesMagicThenInitialSettingsActions()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;

        Http2ConnectionResult result =
            connection.ResetHttp2Connection(options);
        Http2FrameResult settings = new Http2FrameCodec()
            .SerializeHttp2Frame(result.Actions[1].Frame!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.SendConnectionPreface,
                    Http2ActionKind.SendFrame,
                    Http2ActionKind.StartSettingsTimeout
                }));
            Assert.That(result.Actions[0].Bytes,
                Is.EqualTo(Http2Constants.ClientConnectionPreface));
            Assert.That(result.Actions[0].Bytes,
                Is.Not.SameAs(Http2Constants.ClientConnectionPreface));
            Assert.That(result.Actions[0].Frame, Is.Null);
            Assert.That(result.Actions[1].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Settings));
            Assert.That(result.Actions[1].Frame!.Acknowledgement, Is.False);
            Assert.That(settings.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(settings.Bytes,
                Is.EqualTo(new byte[]
                {
                    0x00, 0x00, 0x00, 0x04, 0x00,
                    0x00, 0x00, 0x00, 0x00
                }));
            Assert.That(result.Snapshot!.EndpointRole,
                Is.EqualTo(Http2EndpointRole.Client));
            Assert.That(result.Snapshot.NextLocalStreamId, Is.EqualTo(1));
            Assert.That(result.Snapshot.SentPreface, Is.True);
            Assert.That(result.Snapshot.ReceivedPreface, Is.False);
        });
    }

    [Test]
    public void ServerResetProducesOnlyItsInitialSettingsFrame()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Server;

        Http2ConnectionResult result =
            connection.ResetHttp2Connection(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Actions, Has.Count.EqualTo(2));
            Assert.That(result.Actions[0].ActionKind,
                Is.EqualTo(Http2ActionKind.SendFrame));
            Assert.That(result.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Settings));
            Assert.That(result.Actions[1].ActionKind,
                Is.EqualTo(Http2ActionKind.StartSettingsTimeout));
            Assert.That(result.Snapshot!.EndpointRole,
                Is.EqualTo(Http2EndpointRole.Server));
            Assert.That(result.Snapshot.NextLocalStreamId, Is.EqualTo(2));
            Assert.That(result.Snapshot.SentPreface, Is.True);
            Assert.That(result.Snapshot.ReceivedPreface, Is.False);
        });
    }

    [Test]
    public void InitialSettingValuesPopulateLocalAndRemoteEffectiveState()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        Http2Setting localSetting = new Http2Setting();
        localSetting.Identifier = Http2SettingIdentifiers.HeaderTableSize;
        localSetting.Value = 2048;
        options.LocalSettings.Values.Add(localSetting);

        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        byte[] remotePayload =
        [
            0x00, 0x04, 0x00, 0x00, 0x80, 0x00
        ];
        Http2ConnectionResult ready = ProcessBytes(
            connection, BuildWire(0x04, 0, 0, remotePayload));

        Assert.Multiple(() =>
        {
            Assert.That(reset.Snapshot!.LocalSettings.HasHeaderTableSize,
                Is.True);
            Assert.That(reset.Snapshot.LocalSettings.HeaderTableSize,
                Is.EqualTo(2048));
            Assert.That(reset.Actions[1].Settings!.HeaderTableSize,
                Is.EqualTo(2048));
            Assert.That(ready.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(ready.Snapshot!.RemoteSettings.HasInitialWindowSize,
                Is.True);
            Assert.That(ready.Snapshot.RemoteSettings.InitialWindowSize,
                Is.EqualTo(32768));
            Assert.That(ready.Events[0].Settings!.InitialWindowSize,
                Is.EqualTo(32768));
        });
    }

    [Test]
    public void ClientAcceptsServerInitialSettingsAtEveryByteBoundary()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Client);
        byte[] settings = BuildSettings(false);
        Http2ConnectionResult result = new Http2ConnectionResult();

        int index = 0;
        while (index < settings.Length)
        {
            Http2ConnectionInput input = new Http2ConnectionInput();
            input.HasBytes = true;
            input.Bytes = new[] { settings[index] };
            result = connection.ProcessHttp2Input(input);
            Assert.That(result.Consumed, Is.EqualTo(1));
            if (index < settings.Length - 1)
            {
                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.NeedInput));
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.Actions, Is.Empty);
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsReady, Is.True);
            Assert.That(result.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.ConnectionReady
                }));
            Assert.That(result.Actions, Has.Count.EqualTo(1));
            Assert.That(result.Actions[0].ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgeSettings));
            Assert.That(result.Actions[0].Frame!.Acknowledgement, Is.True);
            Assert.That(result.Snapshot!.ReceivedPreface, Is.True);
            Assert.That(result.Snapshot.ReceivedInitialSettings, Is.True);
        });
    }

    [Test]
    public void ClientAcceptsDecodedInitialSettingsFrame()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Client);
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Frame = frame;

        Http2ConnectionResult result =
            connection.ProcessHttp2Input(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.Zero);
            Assert.That(result.IsReady, Is.True);
            Assert.That(result.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.AcknowledgeSettings));
            Assert.That(result.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.ConnectionReady
                }));
        });
    }

    [Test]
    public void ServerAcceptsFragmentedClientMagicAndInitialSettings()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Server);
        byte[] preface = Concat(
            Http2Constants.ClientConnectionPreface,
            BuildSettings(false));
        Http2ConnectionInput firstInput = new Http2ConnectionInput();
        firstInput.HasBytes = true;
        firstInput.Bytes = preface.AsSpan(0, 11).ToArray();
        Http2ConnectionInput secondInput = new Http2ConnectionInput();
        secondInput.HasBytes = true;
        secondInput.Bytes = preface.AsSpan(11).ToArray();

        Http2ConnectionResult first =
            connection.ProcessHttp2Input(firstInput);
        Http2ConnectionResult second =
            connection.ProcessHttp2Input(secondInput);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Consumed, Is.EqualTo(11));
            Assert.That(first.IsReady, Is.False);
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(second.Consumed, Is.EqualTo(preface.Length - 11));
            Assert.That(second.IsReady, Is.True);
            Assert.That(second.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.ConnectionReady
                }));
            Assert.That(second.Snapshot!.ReceivedPreface, Is.True);
            Assert.That(second.Snapshot.ReceivedInitialSettings, Is.True);
        });
    }

    [Test]
    public void FirstPeerFrameMustBeNonAcknowledgementSettings()
    {
        byte[][] invalidFrames =
        [
            BuildWire(0x06, 0x00, 0, new byte[8]),
            BuildSettings(true)
        ];

        Assert.Multiple(() =>
        {
            foreach (byte[] frame in invalidFrames)
            {
                Http2Connection connection = CreateConnection(
                    Http2EndpointRole.Client);
                Http2ConnectionInput input = new Http2ConnectionInput();
                input.HasBytes = true;
                input.Bytes = frame;

                Http2ConnectionResult result =
                    connection.ProcessHttp2Input(input);

                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.Rejected));
                Assert.That(result.IsTerminal, Is.True);
                Assert.That(result.ErrorScope,
                    Is.EqualTo(Http2ErrorScope.Connection));
                Assert.That(result.ErrorCode,
                    Is.EqualTo(Http2ErrorCode.ProtocolError));
                Assert.That(result.ErrorCodeName,
                    Is.EqualTo("http2-initial-settings-required"));
                Assert.That(result.Actions.Single().ActionKind,
                    Is.EqualTo(Http2ActionKind.SendGoAway));
                Assert.That(result.Actions[0].Frame!.LastStreamId,
                    Is.Zero);
                Assert.That(result.Actions[0].ErrorCode,
                    Is.EqualTo(Http2ErrorCode.ProtocolError));
                Assert.That(result.Events.Single().EventKind,
                    Is.EqualTo(Http2EventKind.Error));
            }
        });
    }

    [TestCase("GET / HTTP/1.1\r\nHost: example.test\r\n\r\n",
        Http2EndpointRole.Server, HttpProtocolVersion.Http11)]
    [TestCase("HTTP/1.0 400 Bad Request\r\n\r\n",
        Http2EndpointRole.Client, HttpProtocolVersion.Http10)]
    public void UnexpectedHttp1IsReportedAndRetained(
        string text,
        Http2EndpointRole role,
        HttpProtocolVersion version)
    {
        Http2Connection connection = CreateConnection(role);
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = System.Text.Encoding.ASCII.GetBytes(text);

        Http2ConnectionResult result = connection.ProcessHttp2Input(input);
        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(result.UnexpectedHttp1, Is.True);
            Assert.That(result.HasUnexpectedHttpVersion, Is.True);
            Assert.That(result.UnexpectedHttpVersion, Is.EqualTo(version));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(snapshot.UnexpectedHttp1, Is.True);
            Assert.That(snapshot.HasUnexpectedHttpVersion, Is.True);
            Assert.That(snapshot.UnexpectedHttpVersion, Is.EqualTo(version));
            Assert.That(snapshot.IsTerminal, Is.True);
        });
    }

    [Test]
    public void FragmentedHttp1RequestIsClassifiedAfterItsStartLine()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Server);
        byte[] request = "GET / HTTP/1.1\r\n"u8.ToArray();
        Http2ConnectionResult result = new Http2ConnectionResult();

        int index = 0;
        while (index < request.Length)
        {
            result = ProcessBytes(connection, new[] { request[index] });
            Assert.That(result.Consumed, Is.EqualTo(1));
            if (index < request.Length - 1)
            {
                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.NeedInput));
                Assert.That(result.IsTerminal, Is.False);
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.UnexpectedHttp1, Is.True);
            Assert.That(result.UnexpectedHttpVersion,
                Is.EqualTo(HttpProtocolVersion.Http11));
        });
    }

    [Test]
    public void IncompletePrefaceAtEndOfInputIsTerminal()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Server);
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = "PRI * HTTP/2.0\r\n"u8.ToArray();
        input.EndOfInput = true;

        Http2ConnectionResult result = connection.ProcessHttp2Input(input);
        Http2ConnectionResult repeated = connection.ProcessHttp2Input(
            new Http2ConnectionInput());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("incomplete-http2-connection-preface"));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.UnexpectedHttp1, Is.False);
            Assert.That(repeated.Status,
                Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(repeated.IsTerminal, Is.True);
        });
    }

    [Test]
    public void SettingsAcknowledgementCompletesInitialExchange()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Client);
        Http2ConnectionResult ready = ProcessBytes(
            connection, BuildSettings(false));
        Http2ConnectionResult acknowledged = ProcessBytes(
            connection, BuildSettings(true));

        Assert.Multiple(() =>
        {
            Assert.That(ready.IsReady, Is.True);
            Assert.That(ready.Snapshot!.InitialSettingsAcknowledged,
                Is.False);
            Assert.That(acknowledged.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(acknowledged.IsReady, Is.True);
            Assert.That(acknowledged.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(acknowledged.Snapshot!.InitialSettingsAcknowledged,
                Is.True);
        });
    }

    [Test]
    public void InvalidOptionsAreRejectedWithoutReplacingExistingState()
    {
        Http2Connection connection = CreateConnection(
            Http2EndpointRole.Server);
        Http2ConnectionOptions invalid = new Http2ConnectionOptions();
        invalid.EndpointRole = Http2EndpointRole.Client;
        invalid.Limits.MaximumFrameSize = 100;

        Http2ConnectionResult result =
            connection.ResetHttp2Connection(invalid);
        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("invalid-http2-connection-options"));
            Assert.That(snapshot.EndpointRole,
                Is.EqualTo(Http2EndpointRole.Server));
            Assert.That(snapshot.SentPreface, Is.True);
            Assert.That(snapshot.IsTerminal, Is.False);
        });
    }

    static Http2Connection CreateConnection(Http2EndpointRole role)
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = role;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
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

    static byte[] BuildSettings(bool acknowledgement)
    {
        return BuildWire(
            0x04,
            acknowledgement ? (byte)0x01 : (byte)0x00,
            0,
            Array.Empty<byte>());
    }

    static byte[] BuildWire(
        byte type,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] wire = new byte[
            Http2Constants.FrameHeaderSize + payload.Length];
        wire[0] = (byte)(payload.Length >> 16);
        wire[1] = (byte)(payload.Length >> 8);
        wire[2] = (byte)payload.Length;
        wire[3] = type;
        wire[4] = flags;
        wire[5] = (byte)(streamId >> 24);
        wire[6] = (byte)(streamId >> 16);
        wire[7] = (byte)(streamId >> 8);
        wire[8] = (byte)streamId;
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
