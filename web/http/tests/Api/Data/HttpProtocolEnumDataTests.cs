namespace http.Tests;

public class HttpProtocolEnumDataTests
{
    [Test]
    public void Http1DataRetainsFramingConnectionAndEventKinds()
    {
        Http1FramingDecision framing = new Http1FramingDecision();
        Http1ConnectionDecision connection = new Http1ConnectionDecision();
        Http1ParseEvent parseEvent = new Http1ParseEvent();

        framing.BodyFraming = Http1BodyFraming.Chunked;
        connection.ConnectionMode = Http1ConnectionMode.Upgraded;
        parseEvent.EventKind = Http1ParseEventKind.ProtocolSwitch;

        Assert.That(framing.BodyFraming, Is.EqualTo(Http1BodyFraming.Chunked));
        Assert.That(connection.ConnectionMode, Is.EqualTo(Http1ConnectionMode.Upgraded));
        Assert.That(parseEvent.EventKind, Is.EqualTo(Http1ParseEventKind.ProtocolSwitch));
    }

    [Test]
    public void Http2ConnectionDataRetainsRoleActionAndStreamState()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        Http2Action action = new Http2Action();
        Http2StreamSnapshot stream = new Http2StreamSnapshot();

        options.EndpointRole = Http2EndpointRole.Server;
        action.ActionKind = Http2ActionKind.ReleaseCapacity;
        stream.StreamId = 17;
        stream.StreamState = Http2StreamState.HalfClosedRemote;

        Assert.That(options.EndpointRole, Is.EqualTo(Http2EndpointRole.Server));
        Assert.That(action.ActionKind, Is.EqualTo(Http2ActionKind.ReleaseCapacity));
        Assert.That(stream.StreamId, Is.EqualTo(17));
        Assert.That(stream.StreamState, Is.EqualTo(Http2StreamState.HalfClosedRemote));
    }

    [Test]
    public void KnownHttp2FrameRetainsSemanticAndRawType()
    {
        Http2Frame frame = new Http2Frame();

        frame.FrameType = Http2FrameType.Origin;
        frame.TypeCode = 0x0c;
        frame.IsKnownType = true;

        Assert.That(frame.FrameType, Is.EqualTo(Http2FrameType.Origin));
        Assert.That(frame.TypeCode, Is.EqualTo(0x0c));
        Assert.That(frame.IsKnownType, Is.True);
    }

    [Test]
    public void UnknownHttp2FrameRetainsExactRawTypeWithoutNullableValue()
    {
        Http2Frame frame = new Http2Frame();

        frame.FrameType = Http2FrameType.Unknown;
        frame.TypeCode = 0xfe;
        frame.IsKnownType = false;

        Assert.That(frame.FrameType, Is.EqualTo(Http2FrameType.Unknown));
        Assert.That(frame.TypeCode, Is.EqualTo(0xfe));
        Assert.That(frame.IsKnownType, Is.False);
    }

    [Test]
    public void KnownHttp2ErrorRetainsSemanticAndRawCode()
    {
        Http2Event http2Event = new Http2Event();

        http2Event.EventKind = Http2EventKind.Error;
        http2Event.HasErrorCode = true;
        http2Event.ErrorCode = Http2ErrorCode.Cancel;
        http2Event.ErrorCodeValue = 0x08u;
        http2Event.IsKnownErrorCode = true;
        http2Event.ErrorScope = Http2ErrorScope.Stream;

        Assert.That(http2Event.EventKind, Is.EqualTo(Http2EventKind.Error));
        Assert.That(http2Event.HasErrorCode, Is.True);
        Assert.That(http2Event.ErrorCode, Is.EqualTo(Http2ErrorCode.Cancel));
        Assert.That(http2Event.ErrorCodeValue, Is.EqualTo(0x08u));
        Assert.That(http2Event.IsKnownErrorCode, Is.True);
        Assert.That(http2Event.ErrorScope, Is.EqualTo(Http2ErrorScope.Stream));
    }

    [Test]
    public void UnknownHttp2ErrorRetainsExactUnsignedCodeWithoutNullableValue()
    {
        Http2Event http2Event = new Http2Event();

        http2Event.EventKind = Http2EventKind.Error;
        http2Event.HasErrorCode = true;
        http2Event.ErrorCode = Http2ErrorCode.Unknown;
        http2Event.ErrorCodeValue = 0xf0000001u;
        http2Event.IsKnownErrorCode = false;
        http2Event.ErrorScope = Http2ErrorScope.Connection;

        Assert.That(http2Event.ErrorCode, Is.EqualTo(Http2ErrorCode.Unknown));
        Assert.That(http2Event.ErrorCodeValue, Is.EqualTo(0xf0000001u));
        Assert.That(http2Event.IsKnownErrorCode, Is.False);
        Assert.That(http2Event.ErrorScope, Is.EqualTo(Http2ErrorScope.Connection));
    }
}
