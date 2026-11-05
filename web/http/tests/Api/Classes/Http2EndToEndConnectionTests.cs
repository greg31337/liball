using System.Text;

namespace http.Tests;

public class Http2EndToEndConnectionTests
{
    [Test]
    public void ConcurrentRequestsAndPhasedResponsesExchangeAsWireBytes()
    {
        Http2Connection client = new Http2Connection();
        Http2ConnectionOptions clientOptions = new Http2ConnectionOptions();
        clientOptions.EndpointRole = Http2EndpointRole.Client;
        Http2ConnectionResult clientReset =
            client.ResetHttp2Connection(clientOptions);

        Http2Connection server = new Http2Connection();
        Http2ConnectionOptions serverOptions = new Http2ConnectionOptions();
        serverOptions.EndpointRole = Http2EndpointRole.Server;
        Http2ConnectionResult serverReset =
            server.ResetHttp2Connection(serverOptions);

        Http2ConnectionResult serverReady = Transmit(clientReset, server);
        Http2ConnectionResult clientReady = Transmit(serverReset, client);
        Http2ConnectionResult serverAcknowledged =
            Transmit(clientReady, server);
        Http2ConnectionResult clientAcknowledged =
            Transmit(serverReady, client);

        Http2ConnectionResult firstRequest = QueueRequest(
            client, Request("/one"), true);
        Http2ConnectionResult secondRequest = QueueRequest(
            client, Request("/two"), true);
        Http2ConnectionResult receivedRequests = ProcessBytes(
            server, Concat(Wire(firstRequest), Wire(secondRequest)));

        Http2ConnectionResult informational = QueueResponse(
            server, 1, Response(103, true), false);
        Http2ConnectionResult final = QueueResponse(
            server, 1, Response(200, false), false);
        Http2ConnectionResult firstContent = QueueContent(
            server,
            1,
            HttpMessageKind.Response,
            Encoding.ASCII.GetBytes("he"),
            false);
        Http2ConnectionResult secondContent = QueueContent(
            server,
            1,
            HttpMessageKind.Response,
            Encoding.ASCII.GetBytes("llo"),
            false);
        Http2ConnectionResult trailers = QueueTrailers(
            server, 1, Headers(Field("x-check", "ok")));
        Http2ConnectionResult receivedResponse = ProcessBytes(
            client,
            Concat(
                Wire(informational),
                Wire(final),
                Wire(firstContent),
                Wire(secondContent),
                Wire(trailers)));

        Http2ConnectionResult secondFinal = QueueResponse(
            server, 3, Response(204, false), true);
        Http2ConnectionResult receivedSecondFinal =
            Transmit(secondFinal, client);

        Assert.Multiple(() =>
        {
            Assert.That(clientReset.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.SendConnectionPreface,
                    Http2ActionKind.SendFrame,
                    Http2ActionKind.StartSettingsTimeout
                }));
            Assert.That(serverReset.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[]
                {
                    Http2ActionKind.SendFrame,
                    Http2ActionKind.StartSettingsTimeout
                }));
            Assert.That(serverReady.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.ConnectionReady
                }));
            Assert.That(clientReady.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.ConnectionReady
                }));
            Assert.That(serverReady.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[] { Http2ActionKind.AcknowledgeSettings }));
            Assert.That(clientReady.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[] { Http2ActionKind.AcknowledgeSettings }));
            Assert.That(serverAcknowledged.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(serverAcknowledged.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.CancelSettingsTimeout));
            Assert.That(clientAcknowledged.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(clientAcknowledged.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.CancelSettingsTimeout));

            Assert.That(firstRequest.Actions.Single().StreamId, Is.EqualTo(1));
            Assert.That(secondRequest.Actions.Single().StreamId, Is.EqualTo(3));
            Assert.That(receivedRequests.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.MessageHead,
                    Http2EventKind.StreamComplete,
                    Http2EventKind.MessageHead,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(receivedRequests.Events[0].Request!.Target,
                Is.EqualTo("/one"));
            Assert.That(receivedRequests.Events[2].Request!.Target,
                Is.EqualTo("/two"));
            Assert.That(receivedRequests.Snapshot!.ActiveRemoteStreams,
                Is.EqualTo(2));

            Assert.That(receivedResponse.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.MessageHead,
                    Http2EventKind.MessageHead,
                    Http2EventKind.Content,
                    Http2EventKind.Content,
                    Http2EventKind.Trailers,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(receivedResponse.Events[0].MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(receivedResponse.Events[1].Response!.StatusCode,
                Is.EqualTo(200));
            Assert.That(Text(receivedResponse.Events[2].Content),
                Is.EqualTo("he"));
            Assert.That(Text(receivedResponse.Events[3].Content),
                Is.EqualTo("llo"));
            Assert.That(Text(receivedResponse.Events[4].Trailers!
                .Fields.Single().Value), Is.EqualTo("ok"));
            Assert.That(Text(receivedResponse.Events[5].Response!.Content),
                Is.EqualTo("hello"));
            Assert.That(receivedResponse.Events[5].Response!.IsComplete,
                Is.True);
            Assert.That(receivedSecondFinal.Events.Select(
                item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.MessageHead,
                    Http2EventKind.StreamComplete
                }));
        });
    }

    [Test]
    public void FlowControlBlocksThenResumesThroughPeerWindowUpdateBytes()
    {
        Http2ConnectionOptions serverOptions = new Http2ConnectionOptions();
        serverOptions.LocalSettings.Values.Add(Setting(
            Http2SettingIdentifiers.InitialWindowSize, 1u));
        CreateReadyPair(
            new Http2ConnectionOptions(),
            serverOptions,
            out Http2Connection client,
            out Http2Connection server);

        Http2ConnectionResult request = QueueRequest(
            client, Request("/upload"), false);
        Http2ConnectionResult receivedHead = Transmit(request, server);
        Http2ConnectionResult first = QueueContent(
            client,
            1,
            HttpMessageKind.Request,
            new byte[] { 0x01 },
            false);
        Http2ConnectionResult receivedFirst = Transmit(first, server);
        Http2ConnectionResult blocked = QueueContent(
            client,
            1,
            HttpMessageKind.Request,
            new byte[] { 0x02 },
            true);

        Http2ConnectionResult released = server.ReleaseHttp2Capacity(1, 1);
        Http2ConnectionResult receivedUpdate = Transmit(released, client);
        Http2ConnectionResult retried = QueueContent(
            client,
            1,
            HttpMessageKind.Request,
            new byte[] { 0x02 },
            true);
        Http2ConnectionResult receivedLast = Transmit(retried, server);

        Assert.Multiple(() =>
        {
            Assert.That(receivedHead.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.MessageHead));
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(receivedFirst.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.Content));
            Assert.That(blocked.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(blocked.ErrorCodeName,
                Is.EqualTo("http2-send-flow-control-blocked"));
            Assert.That(blocked.Actions, Is.Empty);
            Assert.That(blocked.Snapshot!.IsFlowControlBlocked, Is.False);
            Assert.That(FindStream(blocked.Snapshot, 1).IsFlowControlBlocked,
                Is.True);
            Assert.That(released.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[] { Http2ActionKind.ReleaseCapacity }));
            Assert.That(released.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.WindowUpdate));
            Assert.That(released.Actions[0].Frame!.WindowIncrement,
                Is.EqualTo(1));
            Assert.That(receivedUpdate.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(retried.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(receivedLast.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.Content,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(Text(receivedLast.Events[1].Request!.Content),
                Is.EqualTo("\u0001\u0002"));
        });
    }

    [Test]
    public void PushAndResetTraverseTheValidatedWirePipeline()
    {
        CreateReadyPair(
            new Http2ConnectionOptions(),
            new Http2ConnectionOptions(),
            out Http2Connection client,
            out Http2Connection server);
        Http2ConnectionResult request = QueueRequest(
            client, Request("/index"), true);
        Transmit(request, server);

        Http2ConnectionResult pushed = QueuePush(server, 1);
        Http2ConnectionResult receivedPush = Transmit(pushed, client);
        Http2ConnectionResult pushedResponse = QueueResponse(
            server, 2, Response(200, false), false);
        Http2ConnectionResult receivedPushedResponse =
            Transmit(pushedResponse, client);
        Http2ConnectionResult pushedContent = QueueContent(
            server,
            2,
            HttpMessageKind.Response,
            Encoding.ASCII.GetBytes("asset"),
            true);
        Http2ConnectionResult receivedPushedContent =
            Transmit(pushedContent, client);

        Http2ConnectionResult reset = QueueReset(
            server, 1, Http2ErrorCode.Cancel);
        Http2ConnectionResult receivedReset = Transmit(reset, client);

        Assert.Multiple(() =>
        {
            Assert.That(pushed.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.InitiatePush));
            Assert.That(pushed.Actions[0].Frame!.FrameType,
                Is.EqualTo(Http2FrameType.PushPromise));
            Assert.That(receivedPush.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.PushPromised));
            Assert.That(receivedPush.Events[0].StreamId, Is.EqualTo(2));
            Assert.That(receivedPush.Events[0].Request!.Target,
                Is.EqualTo("/asset"));
            Assert.That(receivedPushedResponse.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.MessageHead));
            Assert.That(receivedPushedResponse.Events[0].Message!.IsPushed,
                Is.True);
            Assert.That(receivedPushedContent.Events.Select(
                item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.Content,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(Text(receivedPushedContent.Events[1]
                .Response!.Content), Is.EqualTo("asset"));
            Assert.That(reset.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(receivedReset.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.StreamReset));
            Assert.That(receivedReset.Events[0].ErrorCode,
                Is.EqualTo(Http2ErrorCode.Cancel));
            Assert.That(FindStream(receivedReset.Snapshot!, 1).IsReset,
                Is.True);
        });
    }

    [Test]
    public void GracefulShutdownMarksUnprocessedConcurrentRequestRetryable()
    {
        CreateReadyPair(
            new Http2ConnectionOptions(),
            new Http2ConnectionOptions(),
            out Http2Connection client,
            out Http2Connection server);
        Http2ConnectionResult firstRequest = QueueRequest(
            client, Request("/one"), true);
        Http2ConnectionResult secondRequest = QueueRequest(
            client, Request("/two"), true);
        ProcessBytes(
            server, Concat(Wire(firstRequest), Wire(secondRequest)));
        Http2ConnectionResult unprocessed = QueueRequest(
            client, Request("/retry"), true);

        Http2ConnectionResult firstGoAway = QueueGracefulGoAway(server);
        Http2ConnectionResult receivedFirst = Transmit(firstGoAway, client);
        Http2ConnectionResult secondGoAway = QueueGracefulGoAway(server);
        Http2ConnectionResult receivedSecond = Transmit(secondGoAway, client);

        Http2ConnectionResult firstResponse = QueueResponse(
            server, 1, Response(204, false), true);
        Http2ConnectionResult secondResponse = QueueResponse(
            server, 3, Response(204, false), true);
        Http2ConnectionResult receivedResponses = ProcessBytes(
            client, Concat(Wire(firstResponse), Wire(secondResponse)));

        Assert.Multiple(() =>
        {
            Assert.That(unprocessed.Actions.Single().StreamId, Is.EqualTo(5));
            Assert.That(firstGoAway.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.SendGoAway));
            Assert.That(firstGoAway.Actions[0].Frame!.LastStreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(receivedFirst.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(FindStream(receivedFirst.Snapshot!, 5)
                .IsRetryEligible, Is.False);
            Assert.That(secondGoAway.Actions.Single().Frame!.LastStreamId,
                Is.EqualTo(3));
            Assert.That(receivedSecond.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(receivedSecond.Snapshot!.ReceivedLastStreamId,
                Is.EqualTo(3));
            Assert.That(FindStream(receivedSecond.Snapshot, 5)
                .IsRetryEligible, Is.True);
            Assert.That(FindStream(receivedSecond.Snapshot, 5).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(firstResponse.IsTerminal, Is.False);
            Assert.That(secondResponse.IsTerminal, Is.True);
            Assert.That(secondResponse.IsReady, Is.False);
            Assert.That(receivedResponses.Events.Select(
                item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.MessageHead,
                    Http2EventKind.StreamComplete,
                    Http2EventKind.MessageHead,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(receivedResponses.Events[0].StreamId, Is.EqualTo(1));
            Assert.That(receivedResponses.Events[2].StreamId, Is.EqualTo(3));
        });
    }

    static void CreateReadyPair(
        Http2ConnectionOptions clientOptions,
        Http2ConnectionOptions serverOptions,
        out Http2Connection client,
        out Http2Connection server)
    {
        clientOptions.EndpointRole = Http2EndpointRole.Client;
        serverOptions.EndpointRole = Http2EndpointRole.Server;
        client = new Http2Connection();
        server = new Http2Connection();
        Http2ConnectionResult clientReset =
            client.ResetHttp2Connection(clientOptions);
        Http2ConnectionResult serverReset =
            server.ResetHttp2Connection(serverOptions);
        Http2ConnectionResult serverReady = Transmit(clientReset, server);
        Http2ConnectionResult clientReady = Transmit(serverReset, client);
        Transmit(clientReady, server);
        Transmit(serverReady, client);
        Assert.That(client.InspectHttp2Connection().IsReady, Is.True);
        Assert.That(server.InspectHttp2Connection().IsReady, Is.True);
    }

    static Http2ConnectionResult QueueRequest(
        Http2Connection connection,
        HttpRequest request,
        bool endStream)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Request = request;
        input.EndStream = endStream;
        return connection.ProcessHttp2Input(input);
    }

    static Http2ConnectionResult QueueResponse(
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

    static Http2ConnectionResult QueueContent(
        Http2Connection connection,
        int streamId,
        HttpMessageKind messageKind,
        byte[] content,
        bool endStream)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.StreamId = streamId;
        message.MessageKind = messageKind;
        message.Content = content;
        message.EndStream = endStream;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendMessage;
        action.Message = message;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult QueueTrailers(
        Http2Connection connection,
        int streamId,
        HttpHeaders trailers)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.StreamId = streamId;
        message.MessageKind = HttpMessageKind.Trailers;
        message.Trailers = trailers;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendMessage;
        action.Message = message;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult QueuePush(
        Http2Connection connection,
        int associatedStreamId)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.Headers = Headers(
            Field(":method", "GET"),
            Field(":scheme", "https"),
            Field(":authority", "example.test"),
            Field(":path", "/asset"));
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.InitiatePush;
        action.Message = message;
        action.HasStreamId = true;
        action.StreamId = associatedStreamId;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult QueueReset(
        Http2Connection connection,
        int streamId,
        Http2ErrorCode errorCode)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.ResetStream;
        action.HasStreamId = true;
        action.StreamId = streamId;
        action.HasErrorCode = true;
        action.ErrorCode = errorCode;
        action.ErrorCodeValue = (uint)errorCode;
        action.IsKnownErrorCode = true;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult QueueGracefulGoAway(
        Http2Connection connection)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendGoAway;
        return connection.QueueHttp2Action(action);
    }

    static Http2ConnectionResult Transmit(
        Http2ConnectionResult source,
        Http2Connection target)
    {
        return ProcessBytes(target, Wire(source));
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

    static byte[] Wire(Http2ConnectionResult result)
    {
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < result.Actions.Count)
        {
            Http2Action action = result.Actions[index];
            if (action.Bytes.Length != 0)
            {
                bytes.AddRange(action.Bytes);
            }

            if (action.Frame != null)
            {
                Http2FrameResult serialized =
                    new Http2FrameCodec().SerializeHttp2Frame(action.Frame);
                Assert.That(serialized.Status,
                    Is.EqualTo(HttpOperationStatus.Done),
                    serialized.ErrorCode);
                bytes.AddRange(serialized.Bytes);
            }

            index = index + 1;
        }

        return bytes.ToArray();
    }

    static HttpRequest Request(string target)
    {
        HttpRequest request = new HttpRequest();
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Authority = "example.test";
        request.Target = target;
        request.TargetForm = HttpRequestTargetForm.Origin;
        return request;
    }

    static HttpResponse Response(int statusCode, bool informational)
    {
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
        response.IsInformational = informational;
        return response;
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

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
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

    static byte[] Concat(params byte[][] values)
    {
        int length = 0;
        int index = 0;
        while (index < values.Length)
        {
            length = length + values[index].Length;
            index = index + 1;
        }

        byte[] result = new byte[length];
        int offset = 0;
        index = 0;
        while (index < values.Length)
        {
            values[index].CopyTo(result, offset);
            offset = offset + values[index].Length;
            index = index + 1;
        }

        return result;
    }
}
