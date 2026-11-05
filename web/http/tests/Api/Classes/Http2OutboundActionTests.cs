using System.Text;

namespace http.Tests;

public class Http2OutboundActionTests
{
    [Test]
    public void RequestInputOpensAStreamThenContentEndsIt()
    {
        Http2Connection client = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2ConnectionInput requestInput = new Http2ConnectionInput();
        requestInput.Request = CreateRequest("/items");
        Http2ConnectionResult head = client.ProcessHttp2Input(requestInput);
        Http2MessageResult contentMessage = new Http2MessageResult();
        contentMessage.MessageKind = HttpMessageKind.Request;
        contentMessage.StreamId = 1;
        contentMessage.Content = Encoding.ASCII.GetBytes("abc");
        contentMessage.EndStream = true;
        Http2Action contentAction = new Http2Action();
        contentAction.ActionKind = Http2ActionKind.SendMessage;
        contentAction.Message = contentMessage;

        Http2ConnectionResult content =
            client.QueueHttp2Action(contentAction);
        HpackResult decoded = new HpackDecoder().DecodeHeaderBlock(
            head.Actions.Single().Frame!.HeaderBlockFragment,
            new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(head.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(head.Input, Is.SameAs(requestInput));
            Assert.That(head.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendMessage));
            Assert.That(head.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Headers));
            Assert.That(head.Actions[0].StreamId, Is.EqualTo(1));
            Assert.That(decoded.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":authority", ":path"
                }));
            Assert.That(content.Status,
                Is.EqualTo(HttpOperationStatus.Done),
                content.ErrorCodeName);
            Assert.That(content.Actions.Single().Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Data));
            Assert.That(content.Actions[0].Frame!.Data,
                Is.EqualTo(Encoding.ASCII.GetBytes("abc")));
            Assert.That(content.Actions[0].Frame!.EndStream, Is.True);
            Assert.That(FindStream(content.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.HalfClosedLocal));
        });
    }

    [Test]
    public void ServerSendsInformationalFinalContentAndTrailers()
    {
        Http2Connection server = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(server, CreateInboundRequestFrame(false));

        Http2ConnectionResult informational = ProcessResponse(
            server, 1, CreateResponse(103, true), false);
        Http2ConnectionResult final = ProcessResponse(
            server, 1, CreateResponse(200, false), false);
        Http2MessageResult contentMessage = new Http2MessageResult();
        contentMessage.MessageKind = HttpMessageKind.Response;
        contentMessage.StreamId = 1;
        contentMessage.Content = Encoding.ASCII.GetBytes("body");
        Http2Action contentAction = new Http2Action();
        contentAction.ActionKind = Http2ActionKind.SendMessage;
        contentAction.Message = contentMessage;
        Http2ConnectionResult content =
            server.QueueHttp2Action(contentAction);
        HttpHeaders fields = new HttpHeaders();
        fields.Fields.Add(CreateField("x-check", "ok"));
        Http2MessageResult trailerMessage = new Http2MessageResult();
        trailerMessage.MessageKind = HttpMessageKind.Trailers;
        trailerMessage.StreamId = 1;
        trailerMessage.Trailers = fields;
        Http2Action trailerAction = new Http2Action();
        trailerAction.ActionKind = Http2ActionKind.SendMessage;
        trailerAction.Message = trailerMessage;
        Http2ConnectionResult trailers =
            server.QueueHttp2Action(trailerAction);
        Http2ConnectionResult duplicate = ProcessResponse(
            server, 1, CreateResponse(204, false), true);

        Assert.Multiple(() =>
        {
            Assert.That(informational.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(informational.Actions[0].Message!.MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(informational.Actions[0].Frame!.EndStream, Is.False);
            Assert.That(final.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(final.Actions[0].Message!.MessageKind,
                Is.EqualTo(HttpMessageKind.Response));
            Assert.That(content.Status,
                Is.EqualTo(HttpOperationStatus.Done),
                content.ErrorCodeName);
            Assert.That(content.Actions.Single().Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Data));
            Assert.That(trailers.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(trailers.Actions.Single().Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Headers));
            Assert.That(trailers.Actions[0].Frame!.EndStream, Is.True);
            Assert.That(trailers.Actions[0].Message!.Trailers!.Fields[0].Name,
                Is.EqualTo("x-check"));
            Assert.That(duplicate.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(duplicate.ErrorCodeName,
                Is.EqualTo("http2-final-response-already-sent"));
        });
    }

    [Test]
    public void PushResetAndCapacityActionsUseTheValidatedFramePipeline()
    {
        Http2Connection server = CreateReadyConnection(
            Http2EndpointRole.Server);
        ProcessFrame(server, CreateInboundRequestFrame(false));
        ProcessFrame(server, CreateInboundDataFrame(16384));
        ProcessFrame(server, CreateInboundDataFrame(16384));

        Http2Action releaseAction = new Http2Action();
        releaseAction.ActionKind = Http2ActionKind.ReleaseCapacity;
        releaseAction.HasStreamId = true;
        releaseAction.StreamId = 1;
        releaseAction.Capacity = 32768;
        Http2ConnectionResult released =
            server.QueueHttp2Action(releaseAction);

        Http2MessageResult pushMessage = new Http2MessageResult();
        pushMessage.Request = CreateRequest("/asset.css");
        Http2Action pushAction = new Http2Action();
        pushAction.ActionKind = Http2ActionKind.InitiatePush;
        pushAction.HasStreamId = true;
        pushAction.StreamId = 1;
        pushAction.Message = pushMessage;
        Http2ConnectionResult pushed = server.QueueHttp2Action(pushAction);

        Http2Action resetAction = new Http2Action();
        resetAction.ActionKind = Http2ActionKind.ResetStream;
        resetAction.HasStreamId = true;
        resetAction.StreamId = 2;
        resetAction.HasErrorCode = true;
        resetAction.ErrorCode = Http2ErrorCode.Cancel;
        resetAction.ErrorCodeValue = (uint)Http2ErrorCode.Cancel;
        resetAction.IsKnownErrorCode = true;
        Http2ConnectionInput resetInput = new Http2ConnectionInput();
        resetInput.Action = resetAction;
        resetInput.HasStreamId = true;
        resetInput.StreamId = 2;
        Http2ConnectionResult reset =
            server.ProcessHttp2Input(resetInput);

        Assert.Multiple(() =>
        {
            Assert.That(released.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(released.Actions.Select(item => item.Frame!.StreamId),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(pushed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(pushed.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.InitiatePush));
            Assert.That(pushed.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.PushPromise));
            Assert.That(pushed.Actions[0].Frame!.PromisedStreamId,
                Is.EqualTo(2));
            Assert.That(pushed.Actions[0].Message!.StreamId, Is.EqualTo(2));
            Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reset.Input, Is.SameAs(resetInput));
            Assert.That(reset.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(reset.Actions[0].Frame!.ErrorCode,
                Is.EqualTo(Http2ErrorCode.Cancel));
            Assert.That(FindStream(reset.Snapshot!, 2).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
        });
    }

    [Test]
    public void MetadataAndRoleValidationRejectInvalidHighLevelActions()
    {
        Http2Connection client = CreateReadyConnection(
            Http2EndpointRole.Client);
        Http2Action settingsAction = new Http2Action();
        settingsAction.ActionKind = Http2ActionKind.SendFrame;
        settingsAction.Settings = new Http2Settings();
        Http2ConnectionResult settings =
            client.QueueHttp2Action(settingsAction);

        Http2Action pushAction = new Http2Action();
        pushAction.ActionKind = Http2ActionKind.InitiatePush;
        pushAction.HasStreamId = true;
        pushAction.StreamId = 1;
        pushAction.Message = new Http2MessageResult();
        Http2ConnectionResult push = client.QueueHttp2Action(pushAction);

        Http2Connection server = CreateReadyConnection(
            Http2EndpointRole.Server);
        Http2ConnectionResult response = ProcessResponse(
            server, 1, CreateResponse(200, false), true);
        Http2Action resetAction = new Http2Action();
        resetAction.ActionKind = Http2ActionKind.ResetStream;
        resetAction.HasStreamId = true;
        resetAction.StreamId = 1;
        Http2ConnectionResult reset = server.QueueHttp2Action(resetAction);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(settings.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Settings));
            Assert.That(settings.Actions[0].Settings,
                Is.SameAs(settingsAction.Settings));
            Assert.That(push.ErrorCodeName,
                Is.EqualTo("http2-push-role-or-state"));
            Assert.That(response.ErrorCodeName,
                Is.EqualTo("http2-response-stream-missing"));
            Assert.That(reset.ErrorCodeName,
                Is.EqualTo("invalid-http2-reset-action"));
        });
    }

    [Test]
    public void FlowBlockedContentActionCanBeRetriedAfterWindowUpdate()
    {
        Http2Connection client = CreateReadyClientWithZeroStreamWindow();
        Http2ConnectionInput requestInput = new Http2ConnectionInput();
        requestInput.Request = CreateRequest("/");
        client.ProcessHttp2Input(requestInput);
        Http2MessageResult message = new Http2MessageResult();
        message.MessageKind = HttpMessageKind.Request;
        message.StreamId = 1;
        message.Content = new byte[] { 1 };
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendMessage;
        action.Message = message;

        Http2ConnectionResult blocked = client.QueueHttp2Action(action);
        Http2Frame update = new Http2Frame();
        update.FrameType = Http2FrameType.WindowUpdate;
        update.StreamId = 1;
        update.HasWindowIncrement = true;
        update.WindowIncrement = 1;
        ProcessFrame(client, update);
        Http2ConnectionResult retried = client.QueueHttp2Action(action);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(blocked.ErrorCodeName,
                Is.EqualTo("http2-send-flow-control-blocked"));
            Assert.That(retried.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(retried.Actions.Single().Frame!.Data,
                Is.EqualTo(new byte[] { 1 }));
        });
    }

    static Http2ConnectionResult ProcessResponse(
        Http2Connection connection,
        int streamId,
        HttpResponse response,
        bool endStream)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Response = response;
        input.HasStreamId = true;
        input.StreamId = streamId;
        input.EndStream = endStream;
        return connection.ProcessHttp2Input(input);
    }

    static Http2Connection CreateReadyConnection(Http2EndpointRole role)
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = role;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        Http2Frame settings = new Http2Frame();
        settings.FrameType = Http2FrameType.Settings;
        settings.Settings = new Http2Settings();
        if (role == Http2EndpointRole.Server)
        {
            Http2ConnectionInput magic = new Http2ConnectionInput();
            magic.HasBytes = true;
            magic.Bytes = Http2Constants.ClientConnectionPreface;
            connection.ProcessHttp2Input(magic);
        }

        Http2ConnectionResult ready = ProcessFrame(connection, settings);
        Assert.That(ready.IsReady, Is.True);
        return connection;
    }

    static Http2Connection CreateReadyClientWithZeroStreamWindow()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        connection.ResetHttp2Connection(options);
        Http2Setting initialWindow = new Http2Setting();
        initialWindow.Identifier =
            Http2SettingIdentifiers.InitialWindowSize;
        initialWindow.Value = 0u;
        initialWindow.IsKnown = true;
        Http2Settings values = new Http2Settings();
        values.Values.Add(initialWindow);
        Http2Frame settings = new Http2Frame();
        settings.FrameType = Http2FrameType.Settings;
        settings.Settings = values;
        Http2ConnectionResult ready = ProcessFrame(connection, settings);
        Assert.That(ready.IsReady, Is.True);
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

    static Http2Frame CreateInboundRequestFrame(bool endStream)
    {
        HttpHeaders fields = new HttpHeaders();
        fields.Fields.Add(CreateField(":method", "GET"));
        fields.Fields.Add(CreateField(":scheme", "https"));
        fields.Fields.Add(CreateField(":authority", "example.test"));
        fields.Fields.Add(CreateField(":path", "/"));
        HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
            fields, new HpackOptions());
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = 1;
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = encoded.Bytes;
        return frame;
    }

    static Http2Frame CreateInboundDataFrame(int length)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = 1;
        frame.Data = new byte[length];
        return frame;
    }

    static HttpRequest CreateRequest(string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.Method = HttpMethods.Get;
        request.Target = target;
        request.TargetForm = HttpRequestTargetForm.Origin;
        request.Scheme = "https";
        request.Authority = "example.test";
        return request;
    }

    static HttpResponse CreateResponse(int statusCode, bool informational)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http2;
        response.StatusCode = statusCode;
        response.IsInformational = informational;
        return response;
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
}
