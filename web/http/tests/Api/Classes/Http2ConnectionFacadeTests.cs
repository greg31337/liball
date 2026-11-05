using System.Text;

namespace http.Tests;

public class Http2ConnectionFacadeTests
{
    [Test]
    public void FragmentedBytesProduceOrderedMessageEventsAndActions()
    {
        Http2Connection connection = CreateConnection();
        byte[] settings = SerializeFrame(CreateSettingsFrame());
        byte[] headers = SerializeFrame(CreateRequestHeadersFrame(false));
        byte[] data = SerializeFrame(CreateDataFrame());
        byte[] wire = Concat(
            Http2Constants.ClientConnectionPreface,
            settings,
            headers,
            data);
        int secondEnd = Http2Constants.ClientConnectionPreface.Length +
            settings.Length + headers.Length + 5;

        Http2ConnectionResult first = ProcessBytes(
            connection, Slice(wire, 0, 7));
        Http2ConnectionResult second = ProcessBytes(
            connection, Slice(wire, 7, secondEnd - 7));
        Http2ConnectionResult third = ProcessBytes(
            connection,
            Slice(wire, secondEnd, wire.Length - secondEnd));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Consumed, Is.EqualTo(7));
            Assert.That(first.Events, Is.Empty);
            Assert.That(second.Status,
                Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(second.Consumed, Is.EqualTo(secondEnd - 7));
            Assert.That(second.IsReady, Is.True);
            Assert.That(second.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.SettingsChanged,
                    Http2EventKind.ConnectionReady,
                    Http2EventKind.MessageHead
                }));
            Assert.That(second.Actions.Select(item => item.ActionKind),
                Is.EqualTo(new[] { Http2ActionKind.AcknowledgeSettings }));
            Assert.That(second.Events[2].Request!.Method,
                Is.EqualTo(HttpMethods.Get));
            Assert.That(second.Events[2].Request!.Target,
                Is.EqualTo("/resource"));
            Assert.That(third.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(third.Consumed,
                Is.EqualTo(wire.Length - secondEnd));
            Assert.That(third.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.Content,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(third.Events[0].Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("body")));
            Assert.That(third.Events[1].Request!.Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("body")));
            Assert.That(third.Events[1].Request!.IsComplete, Is.True);
        });
    }

    [Test]
    public void DecodedFrameRunsHpackAndMessageValidation()
    {
        Http2Connection acceptedConnection = CreateReadyConnection();
        Http2ConnectionInput acceptedInput = new Http2ConnectionInput();
        acceptedInput.Frame = CreateRequestHeadersFrame(true);

        Http2ConnectionResult accepted =
            acceptedConnection.ProcessHttp2Input(acceptedInput);

        Http2Connection malformedConnection = CreateReadyConnection();
        Http2Frame malformedFrame = new Http2Frame();
        malformedFrame.FrameType = Http2FrameType.Headers;
        malformedFrame.StreamId = 1;
        malformedFrame.EndHeaders = true;
        malformedFrame.EndStream = true;
        malformedFrame.HeaderBlockFragment =
            EncodeFields(CreateIncompleteRequestFields());
        Http2ConnectionInput malformedInput = new Http2ConnectionInput();
        malformedInput.Frame = malformedFrame;

        Http2ConnectionResult malformed =
            malformedConnection.ProcessHttp2Input(malformedInput);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(accepted.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.MessageHead,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(accepted.Events[0].Message!.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(accepted.Events[0].Request!.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http2));
            Assert.That(malformed.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(malformed.ErrorCodeName,
                Is.EqualTo("invalid-http2-request-scheme"));
            Assert.That(malformed.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(malformed.IsTerminal, Is.False);
        });
    }

    [Test]
    public void DecodedFramesDeliverResponsePhasesInOrder()
    {
        Http2Connection connection = CreateReadyClientConnection();
        Http2Frame requestFrame = new Http2Frame();
        requestFrame.FrameType = Http2FrameType.Headers;
        requestFrame.EndHeaders = true;
        requestFrame.EndStream = true;
        requestFrame.HeaderBlockFragment = Array.Empty<byte>();
        Http2Action requestAction = new Http2Action();
        requestAction.ActionKind = Http2ActionKind.SendFrame;
        requestAction.Frame = requestFrame;
        Http2ConnectionResult queued =
            connection.QueueHttp2Action(requestAction);

        Http2ConnectionResult informational = ProcessFrame(
            connection,
            CreateResponseHeadersFrame(1, "103", false));
        Http2ConnectionResult final = ProcessFrame(
            connection,
            CreateResponseHeadersFrame(1, "200", false));
        Http2Frame contentFrame = new Http2Frame();
        contentFrame.FrameType = Http2FrameType.Data;
        contentFrame.StreamId = 1;
        contentFrame.Data = Encoding.ASCII.GetBytes("body");
        Http2ConnectionResult content = ProcessFrame(
            connection, contentFrame);
        Http2Frame trailersFrame = new Http2Frame();
        trailersFrame.FrameType = Http2FrameType.Headers;
        trailersFrame.StreamId = 1;
        trailersFrame.EndHeaders = true;
        trailersFrame.EndStream = true;
        HttpHeaders trailerFields = new HttpHeaders();
        trailerFields.Fields.Add(CreateField("x-check", "ok"));
        trailersFrame.HeaderBlockFragment = EncodeFields(trailerFields);
        Http2ConnectionResult trailers = ProcessFrame(
            connection, trailersFrame);

        Assert.Multiple(() =>
        {
            Assert.That(queued.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(informational.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.MessageHead));
            Assert.That(informational.Events[0].MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(final.Events.Single().MessageKind,
                Is.EqualTo(HttpMessageKind.Response));
            Assert.That(final.Events[0].Response!.StatusCode,
                Is.EqualTo(200));
            Assert.That(content.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.Content));
            Assert.That(content.Events[0].Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("body")));
            Assert.That(trailers.Events.Select(item => item.EventKind),
                Is.EqualTo(new[]
                {
                    Http2EventKind.Trailers,
                    Http2EventKind.StreamComplete
                }));
            Assert.That(trailers.Events[0].Trailers!.Fields[0].Name,
                Is.EqualTo("x-check"));
            Assert.That(trailers.Events[1].Response!.IsComplete, Is.True);
        });
    }

    [Test]
    public void UnknownFrameDeliveryHonorsConnectionOptions()
    {
        Http2Connection preservedConnection = CreateReadyConnection();
        Http2Frame unknown = new Http2Frame();
        unknown.FrameType = Http2FrameType.Unknown;
        unknown.TypeCode = 0xfe;
        unknown.Payload = new byte[] { 1, 2, 3 };

        Http2ConnectionResult preserved = ProcessFrame(
            preservedConnection, unknown);

        Http2ConnectionOptions discardedOptions =
            new Http2ConnectionOptions();
        discardedOptions.EndpointRole = Http2EndpointRole.Server;
        discardedOptions.PreserveUnknownFrames = false;
        Http2Connection discardedConnection = new Http2Connection();
        discardedConnection.ResetHttp2Connection(discardedOptions);
        ProcessBytes(
            discardedConnection,
            Concat(
                Http2Constants.ClientConnectionPreface,
                SerializeFrame(CreateSettingsFrame())));
        Http2ConnectionResult discarded = ProcessFrame(
            discardedConnection, unknown);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(preserved.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(discarded.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(discarded.Events, Is.Empty);
        });
    }

    [Test]
    public void EndOfInputRejectsAPartialFrame()
    {
        Http2Connection connection = CreateReadyConnection();
        byte[] settings = SerializeFrame(CreateSettingsFrame());
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = Slice(settings, 0, 8);
        input.EndOfInput = true;

        Http2ConnectionResult result =
            connection.ProcessHttp2Input(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("incomplete-http2-frame"));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    static Http2Connection CreateConnection()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Server;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        return connection;
    }

    static Http2Connection CreateReadyConnection()
    {
        Http2Connection connection = CreateConnection();
        Http2ConnectionResult ready = ProcessBytes(
            connection,
            Concat(
                Http2Constants.ClientConnectionPreface,
                SerializeFrame(CreateSettingsFrame())));
        Assert.That(ready.IsReady, Is.True);
        return connection;
    }

    static Http2Connection CreateReadyClientConnection()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
        Http2ConnectionResult ready = ProcessBytes(
            connection, SerializeFrame(CreateSettingsFrame()));
        Assert.That(ready.IsReady, Is.True);
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

    static Http2Frame CreateSettingsFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        return frame;
    }

    static Http2Frame CreateRequestHeadersFrame(bool endStream)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = 1;
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = EncodeFields(CreateRequestFields());
        return frame;
    }

    static Http2Frame CreateDataFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = 1;
        frame.EndStream = true;
        frame.Data = Encoding.ASCII.GetBytes("body");
        return frame;
    }

    static Http2Frame CreateResponseHeadersFrame(
        int streamId,
        string status,
        bool endStream)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField(":status", status));
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.EndStream = endStream;
        frame.HeaderBlockFragment = EncodeFields(headers);
        return frame;
    }

    static HttpHeaders CreateRequestFields()
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField(":method", "GET"));
        headers.Fields.Add(CreateField(":scheme", "https"));
        headers.Fields.Add(CreateField(":authority", "example.test"));
        headers.Fields.Add(CreateField(":path", "/resource"));
        return headers;
    }

    static HttpHeaders CreateIncompleteRequestFields()
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(CreateField(":method", "GET"));
        headers.Fields.Add(CreateField(":path", "/"));
        return headers;
    }

    static HttpHeaderField CreateField(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }

    static byte[] EncodeFields(HttpHeaders fields)
    {
        HpackResult result = new HpackEncoder().EncodeHeaderBlock(
            fields, new HpackOptions());
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return result.Bytes;
    }

    static byte[] SerializeFrame(Http2Frame frame)
    {
        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return result.Bytes;
    }

    static byte[] Slice(byte[] source, int offset, int count)
    {
        byte[] result = new byte[count];
        Array.Copy(source, offset, result, 0, count);
        return result;
    }

    static byte[] Concat(params byte[][] values)
    {
        int length = values.Sum(item => item.Length);
        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] value in values)
        {
            Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }

        return result;
    }
}
