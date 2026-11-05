using System.Text;

namespace http.Tests;

public class Http2HeaderBlockConnectionTests
{
    [Test]
    public void ContinuationCompletesOneDecodedHeaderBlock()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2Frame headers = CreateHeaders(
            1, false, false, new byte[] { 0x82, 0x87 });

        Http2ConnectionResult first = ProcessFrame(connection, headers);
        Http2ConnectionResult completed = ProcessFrame(
            connection,
            CreateContinuation(
                1,
                true,
                new byte[] { 0x01, 0x01, (byte)'x', 0x84 }));
        Http2MessageResult message = completed.Events.Single().Message!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Events.Single().Message, Is.Null);
            Assert.That(completed.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(message.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(message.StreamId, Is.EqualTo(1));
            Assert.That(message.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(message.EndStream, Is.False);
            Assert.That(message.Headers.Fields.Select(field => field.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":authority", ":path"
                }));
            Assert.That(message.Headers.Fields[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("GET")));
        });
    }

    [Test]
    public void PushPromiseContinuationBelongsToPromisedStream()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        QueueFrame(connection, CreateHeaders(0, true, true, Array.Empty<byte>()));
        Http2Frame push = new Http2Frame();
        push.FrameType = Http2FrameType.PushPromise;
        push.StreamId = 1;
        push.HasPromisedStreamId = true;
        push.PromisedStreamId = 2;
        push.HeaderBlockFragment = new byte[] { 0x82 };

        Http2ConnectionResult first = ProcessFrame(connection, push);
        Http2ConnectionResult completed = ProcessFrame(
            connection,
            CreateContinuation(1, true, new byte[] { 0x87, 0x84 }));
        Http2MessageResult message = completed.Events.Single().Message!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(message.StreamId, Is.EqualTo(2));
            Assert.That(message.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(message.Headers.Fields, Has.Count.EqualTo(3));
            Assert.That(FindStream(completed.Snapshot!, 2).StreamState,
                Is.EqualTo(Http2StreamState.ReservedRemote));
        });
    }

    [Test]
    public void InterleavingWrongStreamAndUnexpectedContinuationAreErrors()
    {
        Http2Connection interleavedConnection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(
            interleavedConnection,
            CreateHeaders(1, false, false, new byte[] { 0x82 }));
        Http2Frame ping = new Http2Frame();
        ping.FrameType = Http2FrameType.Ping;
        ping.OpaqueData = new byte[Http2Constants.PingPayloadSize];
        Http2ConnectionResult interleaved = ProcessFrame(
            interleavedConnection, ping);

        Http2Connection wrongStreamConnection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        ProcessFrame(
            wrongStreamConnection,
            CreateHeaders(1, false, false, new byte[] { 0x82 }));
        Http2ConnectionResult wrongStream = ProcessFrame(
            wrongStreamConnection,
            CreateContinuation(3, true, new byte[] { 0x84 }));

        Http2Connection unexpectedConnection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2ConnectionResult unexpected = ProcessFrame(
            unexpectedConnection,
            CreateContinuation(1, true, new byte[] { 0x82 }));

        Assert.Multiple(() =>
        {
            Assert.That(interleaved.ErrorCodeName,
                Is.EqualTo("http2-inbound-header-block-interleaved"));
            Assert.That(interleaved.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(interleaved.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(interleaved.IsTerminal, Is.True);
            Assert.That(wrongStream.ErrorCodeName,
                Is.EqualTo("http2-inbound-header-block-interleaved"));
            Assert.That(wrongStream.IsTerminal, Is.True);
            Assert.That(unexpected.ErrorCodeName,
                Is.EqualTo("http2-inbound-unexpected-continuation"));
            Assert.That(unexpected.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(unexpected.IsTerminal, Is.True);
        });
    }

    [Test]
    public void InboundDecoderContextIsSharedAcrossHeaderBlocks()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        byte[] literal = new byte[]
        {
            0x40, 0x01, (byte)'a', 0x01, (byte)'1'
        };

        Http2ConnectionResult inserted = ProcessFrame(
            connection, CreateHeaders(1, false, true, literal));
        Http2ConnectionResult indexed = ProcessFrame(
            connection,
            CreateHeaders(3, false, true, new byte[] { 0xbe }));

        Assert.Multiple(() =>
        {
            Assert.That(inserted.Events.Single().Message!.Headers.Fields[0].Name,
                Is.EqualTo("a"));
            Assert.That(indexed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(indexed.Events.Single().Message!.Headers.Fields[0].Name,
                Is.EqualTo("a"));
            Assert.That(indexed.Events[0].Message!.Headers.Fields[0].Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("1")));
        });
    }

    [Test]
    public void OutboundEncoderIsSharedAndIndependentFromInboundDecoder()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EncoderOptions.IndexingMode = HpackIndexingMode.Incremental;
        options.EncoderOptions.UseHuffman = false;
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, options);
        HttpHeaders fields = new HttpHeaders();
        fields.Fields.Add(CreateField("a", "1"));

        Http2ConnectionResult first = QueueMessageHeaders(
            connection, fields);
        Http2ConnectionResult second = QueueMessageHeaders(
            connection, fields);
        Http2ConnectionResult inbound = ProcessFrame(
            connection,
            CreateHeaders(1, false, true, new byte[] { 0xbe }));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Actions.Single().Frame!.HeaderBlockFragment,
                Is.EqualTo(new byte[]
                {
                    0x40, 0x01, (byte)'a', 0x01, (byte)'1'
                }));
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(second.Actions.Single().Frame!.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0xbe }));
            Assert.That(inbound.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(inbound.ErrorCode,
                Is.EqualTo(Http2ErrorCode.CompressionError));
            Assert.That(inbound.IsTerminal, Is.True);
        });
    }

    [Test]
    public void OutboundContinuationMustRemainContiguousAndOnOneStream()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Client, new Http2ConnectionOptions());
        Http2ConnectionResult first = QueueFrame(
            connection,
            CreateHeaders(0, false, false, new byte[] { 0x82 }));
        Http2ConnectionResult interleaved = QueueFrame(
            connection, CreateData(1));
        Http2ConnectionResult wrongStream = QueueFrame(
            connection,
            CreateContinuation(3, true, new byte[] { 0x84 }));
        Http2ConnectionResult completed = QueueFrame(
            connection,
            CreateContinuation(1, true, new byte[] { 0x84 }));
        Http2ConnectionResult unexpected = QueueFrame(
            connection,
            CreateContinuation(1, true, new byte[] { 0x82 }));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(interleaved.ErrorCodeName,
                Is.EqualTo("http2-outbound-header-block-interleaved"));
            Assert.That(interleaved.IsTerminal, Is.False);
            Assert.That(wrongStream.ErrorCodeName,
                Is.EqualTo("http2-outbound-header-block-interleaved"));
            Assert.That(completed.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(unexpected.ErrorCodeName,
                Is.EqualTo("http2-outbound-unexpected-continuation"));
            Assert.That(unexpected.IsTerminal, Is.False);
        });
    }

    [Test]
    public void AggregateCompressedLimitIsCheckedBeforeAppendingFragment()
    {
        Http2ConnectionOptions inboundOptions = new Http2ConnectionOptions();
        inboundOptions.DecoderOptions.MaximumCompressedBlockSize = 2;
        Http2Connection inboundConnection = CreateReadyConnection(
            Http2EndpointRole.Server, inboundOptions);
        ProcessFrame(
            inboundConnection,
            CreateHeaders(1, false, false, new byte[] { 0x82, 0x84 }));
        Http2ConnectionResult inbound = ProcessFrame(
            inboundConnection,
            CreateContinuation(1, true, new byte[] { 0x86 }));

        Http2ConnectionOptions outboundOptions = new Http2ConnectionOptions();
        outboundOptions.EncoderOptions.MaximumCompressedBlockSize = 1;
        Http2Connection outboundConnection = CreateReadyConnection(
            Http2EndpointRole.Client, outboundOptions);
        HttpHeaders fields = new HttpHeaders();
        fields.Fields.Add(CreateField("a", "1"));
        Http2ConnectionResult outbound = QueueMessageHeaders(
            outboundConnection, fields);

        Assert.Multiple(() =>
        {
            Assert.That(inbound.ErrorCodeName,
                Is.EqualTo("http2-inbound-compressed-header-block-limit"));
            Assert.That(inbound.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(inbound.ErrorCode,
                Is.EqualTo(Http2ErrorCode.EnhanceYourCalm));
            Assert.That(inbound.IsTerminal, Is.True);
            Assert.That(outbound.ErrorCodeName,
                Is.EqualTo("hpack-compressed-block-limit-exceeded"));
            Assert.That(outbound.IsTerminal, Is.False);
            Assert.That(outbound.Actions, Is.Empty);
        });
    }

    [Test]
    public void DecodedListLimitIsStreamErrorButMalformedHpackIsConnectionError()
    {
        Http2ConnectionOptions limitedOptions = new Http2ConnectionOptions();
        limitedOptions.DecoderOptions.MaximumHeaderListSize = 50;
        Http2Connection limitedConnection = CreateReadyConnection(
            Http2EndpointRole.Server, limitedOptions);
        Http2ConnectionResult limited = ProcessFrame(
            limitedConnection,
            CreateHeaders(
                1, false, true, new byte[] { 0x82, 0x84 }));

        Http2Connection malformedConnection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2ConnectionResult malformed = ProcessFrame(
            malformedConnection,
            CreateHeaders(1, false, true, new byte[] { 0x80 }));

        Assert.Multiple(() =>
        {
            Assert.That(limited.ErrorCodeName,
                Is.EqualTo("hpack-header-list-size-limit-exceeded"));
            Assert.That(limited.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(limited.ErrorCode,
                Is.EqualTo(Http2ErrorCode.EnhanceYourCalm));
            Assert.That(limited.IsTerminal, Is.False);
            Assert.That(limited.Actions.Single().ActionKind,
                Is.EqualTo(Http2ActionKind.ResetStream));
            Assert.That(FindStream(limited.Snapshot!, 1).StreamState,
                Is.EqualTo(Http2StreamState.Closed));
            Assert.That(malformed.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(malformed.ErrorCode,
                Is.EqualTo(Http2ErrorCode.CompressionError));
            Assert.That(malformed.IsTerminal, Is.True);
        });
    }

    [Test]
    public void EndOfInputRejectsAnIncompleteHeaderBlock()
    {
        Http2Connection connection = CreateReadyConnection(
            Http2EndpointRole.Server, new Http2ConnectionOptions());
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Frame = CreateHeaders(
            1, false, false, new byte[] { 0x82 });
        input.EndOfInput = true;

        Http2ConnectionResult result = connection.ProcessHttp2Input(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("incomplete-http2-header-block"));
            Assert.That(result.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode,
                Is.EqualTo(Http2ErrorCode.ProtocolError));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    static Http2Connection CreateReadyConnection(
        Http2EndpointRole role,
        Http2ConnectionOptions options)
    {
        Http2Connection connection = new Http2Connection();
        options.EndpointRole = role;
        Http2ConnectionResult reset =
            connection.ResetHttp2Connection(options);
        Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));

        byte[] settings = BuildWire(0x04, 0, 0, Array.Empty<byte>());
        byte[] inputBytes = settings;
        if (role == Http2EndpointRole.Server)
        {
            inputBytes = Concat(
                Http2Constants.ClientConnectionPreface, settings);
        }

        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = inputBytes;
        Http2ConnectionResult ready = connection.ProcessHttp2Input(input);
        Assert.That(ready.IsReady, Is.True);
        return connection;
    }

    static Http2ConnectionResult QueueMessageHeaders(
        Http2Connection connection,
        HttpHeaders headers)
    {
        Http2Frame frame = CreateHeaders(
            0, false, true, Array.Empty<byte>());
        Http2MessageResult message = new Http2MessageResult();
        message.Headers = headers;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
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

    static Http2Frame CreateHeaders(
        int streamId,
        bool endStream,
        bool endHeaders,
        byte[] fragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndStream = endStream;
        frame.EndHeaders = endHeaders;
        frame.HeaderBlockFragment = fragment;
        return frame;
    }

    static Http2Frame CreateContinuation(
        int streamId,
        bool endHeaders,
        byte[] fragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Continuation;
        frame.StreamId = streamId;
        frame.EndHeaders = endHeaders;
        frame.HeaderBlockFragment = fragment;
        return frame;
    }

    static Http2Frame CreateData(int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.Data = new byte[] { 0x01 };
        return frame;
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

    static byte[] BuildWire(
        byte type,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] wire = new byte[9 + payload.Length];
        wire[0] = (byte)(payload.Length >> 16);
        wire[1] = (byte)(payload.Length >> 8);
        wire[2] = (byte)payload.Length;
        wire[3] = type;
        wire[4] = flags;
        wire[5] = (byte)(streamId >> 24);
        wire[6] = (byte)(streamId >> 16);
        wire[7] = (byte)(streamId >> 8);
        wire[8] = (byte)streamId;
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
