using System.Text;

namespace http.Tests;

public class Http2ServerPushTests
{
    [Test]
    public void OutboundPushHonorsPolicyPeerSettingAndRequestMethod()
    {
        Http2ConnectionOptions disabledOptions = new Http2ConnectionOptions();
        disabledOptions.AllowServerPush = false;
        Http2Connection disabled = CreateReadyServer(
            disabledOptions, Array.Empty<Http2Setting>());
        ProcessFrame(disabled, CreateHeaders(1, true));
        Http2ConnectionResult policyRejected = QueuePush(
            disabled, 1, 2, CreatePushMessage(HttpMethods.Get));

        Http2Connection peerDisabled = CreateReadyServer(
            new Http2ConnectionOptions(),
            new[] { Setting(Http2SettingIdentifiers.EnablePush, 0u) });
        ProcessFrame(peerDisabled, CreateHeaders(1, true));
        Http2ConnectionResult peerRejected = QueuePush(
            peerDisabled, 1, 2, CreatePushMessage(HttpMethods.Get));

        Http2Connection invalidMethod = CreateReadyServer(
            new Http2ConnectionOptions(), Array.Empty<Http2Setting>());
        ProcessFrame(invalidMethod, CreateHeaders(1, true));
        Http2ConnectionResult unsafeRejected = QueuePush(
            invalidMethod, 1, 2, CreatePushMessage(HttpMethods.Post));
        Http2ConnectionResult uncacheableRejected = QueuePush(
            invalidMethod, 1, 2, CreatePushMessage(HttpMethods.Options));
        Http2MessageResult validMessage = CreatePushMessage(HttpMethods.Get);
        Http2ConnectionResult accepted = QueuePush(
            invalidMethod, 1, 2, validMessage);

        Assert.Multiple(() =>
        {
            Assert.That(policyRejected.ErrorCodeName,
                Is.EqualTo("http2-server-push-disabled"));
            Assert.That(peerRejected.ErrorCodeName,
                Is.EqualTo("http2-peer-disabled-push"));
            Assert.That(unsafeRejected.ErrorCodeName,
                Is.EqualTo("http2-unsafe-push-method"));
            Assert.That(uncacheableRejected.ErrorCodeName,
                Is.EqualTo("http2-uncacheable-push-method"));
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(validMessage.Request!.Method, Is.EqualTo(HttpMethods.Get));
            Assert.That(validMessage.IsPushCacheable, Is.True);
            Assert.That(validMessage.AssociatedStreamId, Is.EqualTo(1));
            Assert.That(FindStream(accepted.Snapshot!, 2).PushRequest!.Target,
                Is.EqualTo("/asset.css?v=1"));
            Assert.That(FindStream(accepted.Snapshot!, 2).IsPushCacheable,
                Is.True);
        });
    }

    [Test]
    public void AcknowledgedPushDisablementMakesPushPromiseAConnectionError()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.EnablePush, 0u));
        Http2Connection connection = CreateReadyClient(options);
        QueueFrame(connection, CreateHeaders(0, true));

        Http2ConnectionResult beforeAcknowledgement = ProcessFrame(
            connection, CreatePushPromise(1, 2, HttpMethods.Get));
        ProcessFrame(connection, CreateSettingsAcknowledgement());
        Http2ConnectionResult afterAcknowledgement = ProcessFrame(
            connection, CreatePushPromise(1, 4, HttpMethods.Get));

        Assert.Multiple(() =>
        {
            Assert.That(beforeAcknowledgement.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(afterAcknowledgement.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(afterAcknowledgement.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(afterAcknowledgement.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(afterAcknowledgement.ErrorCodeName,
                Is.EqualTo("http2-push-disabled"));
            Assert.That(afterAcknowledgement.IsTerminal, Is.True);
        });
    }

    [Test]
    public void PushRequestValidationUsesPromisedStreamErrors()
    {
        Http2Connection unsafeConnection = CreateReadyClient(
            new Http2ConnectionOptions());
        QueueFrame(unsafeConnection, CreateHeaders(0, true));
        Http2ConnectionResult unsafeResult = ProcessFrame(
            unsafeConnection, CreatePushPromise(1, 2, HttpMethods.Post));

        Http2Connection incompleteConnection = CreateReadyClient(
            new Http2ConnectionOptions());
        QueueFrame(incompleteConnection, CreateHeaders(0, true));
        Http2Frame incomplete = CreatePushPromise(
            1, 2, HttpMethods.Get);
        incomplete.HeaderBlockFragment = new byte[] { 0x82, 0x84 };
        Http2ConnectionResult incompleteResult = ProcessFrame(
            incompleteConnection, incomplete);

        Assert.Multiple(() =>
        {
            Assert.That(unsafeResult.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(unsafeResult.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(unsafeResult.ErrorCodeName,
                Is.EqualTo("http2-unsafe-push-method"));
            Assert.That(unsafeResult.Actions.Single().StreamId, Is.EqualTo(2));
            Assert.That(unsafeResult.IsTerminal, Is.False);
            Assert.That(FindStream(unsafeResult.Snapshot!, 2).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(incompleteResult.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(incompleteResult.ErrorCodeName,
                Is.EqualTo("http2-incomplete-push-request"));
            Assert.That(incompleteResult.Actions.Single().StreamId,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void AcceptedPushCarriesCacheMetadataAndCanCompleteOrCancel()
    {
        Http2Connection connection = CreateReadyClient(
            new Http2ConnectionOptions());
        QueueFrame(connection, CreateHeaders(0, true));

        Http2ConnectionResult promised = ProcessFrame(
            connection, CreatePushPromise(1, 2, HttpMethods.Get));
        Http2Event pushEvent = promised.Events.Single();
        Http2ConnectionResult response = ProcessFrame(
            connection, CreateResponseHeaders(2, false));
        Http2MessageResult responseMessage = response.Events.Single().Message!;
        Http2ConnectionResult completed = ProcessFrame(
            connection, CreateData(2, true));

        Http2Connection secondConnection = CreateReadyClient(
            new Http2ConnectionOptions());
        QueueFrame(secondConnection, CreateHeaders(0, true));
        ProcessFrame(
            secondConnection, CreatePushPromise(1, 2, HttpMethods.Get));
        Http2ConnectionResult cancelled = QueueFrame(
            secondConnection, CreateReset(2));

        Assert.Multiple(() =>
        {
            Assert.That(pushEvent.EventKind,
                Is.EqualTo(Http2EventKind.PushPromised));
            Assert.That(pushEvent.StreamId, Is.EqualTo(2));
            Assert.That(pushEvent.Request!.Method, Is.EqualTo(HttpMethods.Get));
            Assert.That(pushEvent.Message!.AssociatedStreamId, Is.EqualTo(1));
            Assert.That(pushEvent.Message.IsPushCacheable, Is.True);
            Assert.That(responseMessage.IsPushed, Is.True);
            Assert.That(responseMessage.PushRequest!.Target,
                Is.EqualTo("/asset.css?v=1"));
            Assert.That(responseMessage.IsPushCacheable, Is.True);
            Assert.That(FindStream(completed.Snapshot!, 2).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(FindStream(completed.Snapshot!, 2).RemoteEndStream,
                Is.True);
            Assert.That(cancelled.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FindStream(cancelled.Snapshot!, 2).IsReset, Is.True);
            Assert.That(FindStream(cancelled.Snapshot!, 2).ResetByLocal, Is.True);
            Assert.That(FindStream(cancelled.Snapshot!, 2).ErrorCode,
                Is.EqualTo(Http2ErrorCode.Cancel));
        });
    }

    [Test]
    public void PushPromiseRequiresAClientInitiatedLiveAssociation()
    {
        Http2Connection connection = CreateReadyClient(
            new Http2ConnectionOptions());
        QueueFrame(connection, CreateHeaders(0, true));
        ProcessFrame(connection, CreatePushPromise(1, 2, HttpMethods.Get));

        Http2ConnectionResult invalidAssociation = ProcessFrame(
            connection, CreatePushPromise(2, 4, HttpMethods.Get));

        Assert.Multiple(() =>
        {
            Assert.That(invalidAssociation.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalidAssociation.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(invalidAssociation.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(invalidAssociation.ErrorCodeName,
                Is.EqualTo("http2-invalid-push-associated-stream"));
            Assert.That(invalidAssociation.IsTerminal, Is.True);
        });
    }

    static Http2Connection CreateReadyClient(Http2ConnectionOptions options)
    {
        options.EndpointRole = Http2EndpointRole.Client;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        ProcessFrame(connection, CreateSettingsFrame(Array.Empty<Http2Setting>()));
        return connection;
    }

    static Http2Connection CreateReadyServer(
        Http2ConnectionOptions options,
        Http2Setting[] settings)
    {
        options.EndpointRole = Http2EndpointRole.Server;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = Concat(
            Http2Constants.ClientConnectionPreface,
            BuildSettingsWire(settings));
        Assert.That(connection.ProcessHttp2Input(input).Status,
            Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2ConnectionResult QueuePush(
        Http2Connection connection,
        int associatedStreamId,
        int promisedStreamId,
        Http2MessageResult message)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = associatedStreamId;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.EndHeaders = true;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        action.Message = message;
        return connection.QueueHttp2Action(action);
    }

    static Http2MessageResult CreatePushMessage(string method)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.Headers = CreatePushHeaders(method);
        return message;
    }

    static HttpHeaders CreatePushHeaders(string method)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField(":method", method));
        headers.Fields.Add(CreateField(":scheme", "https"));
        headers.Fields.Add(CreateField(":authority", "example.com"));
        headers.Fields.Add(CreateField(":path", "/asset.css?v=1"));
        headers.Fields.Add(CreateField("accept", "text/css"));
        return headers;
    }

    static Http2Frame CreatePushPromise(
        int streamId,
        int promisedStreamId,
        string method)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = streamId;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.EndHeaders = true;
        frame.HeaderBlockFragment = EncodeHeaders(CreatePushHeaders(method));
        return frame;
    }

    static byte[] EncodeHeaders(HttpHeaders headers)
    {
        HpackOptions options = new HpackOptions();
        options.IndexingMode = HpackIndexingMode.WithoutIndexing;
        options.UseHuffman = false;
        HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
            headers, options);
        Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
        return encoded.Bytes;
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

    static Http2Frame CreateResponseHeaders(int streamId, bool endStream)
    {
        Http2Frame frame = CreateHeaders(streamId, endStream);
        frame.HeaderBlockFragment = new byte[] { 0x88 };
        return frame;
    }

    static Http2Frame CreateData(int streamId, bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.EndStream = endStream;
        frame.Data = new byte[] { 1, 2, 3 };
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

    static Http2Frame CreateSettingsFrame(Http2Setting[] settings)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        frame.Settings.Values.AddRange(settings);
        return frame;
    }

    static Http2Frame CreateSettingsAcknowledgement()
    {
        Http2Frame frame = CreateSettingsFrame(Array.Empty<Http2Setting>());
        frame.Acknowledgement = true;
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
