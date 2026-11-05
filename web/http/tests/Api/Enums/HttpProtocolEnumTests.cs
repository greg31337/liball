namespace http.Tests;

public class HttpProtocolEnumTests
{
    [Test]
    public void Http1BodyFramingValuesAreStable()
    {
        Assert.That((int)Http1BodyFraming.None, Is.EqualTo(0));
        Assert.That((int)Http1BodyFraming.ContentLength, Is.EqualTo(1));
        Assert.That((int)Http1BodyFraming.Chunked, Is.EqualTo(2));
        Assert.That((int)Http1BodyFraming.CloseDelimited, Is.EqualTo(3));
        Assert.That((int)Http1BodyFraming.Tunnel, Is.EqualTo(4));
        Assert.That((int)Http1BodyFraming.Invalid, Is.EqualTo(5));
    }

    [Test]
    public void Http1ParseEventKindValuesAreStable()
    {
        Assert.That((int)Http1ParseEventKind.MessageHead, Is.EqualTo(0));
        Assert.That((int)Http1ParseEventKind.Content, Is.EqualTo(1));
        Assert.That((int)Http1ParseEventKind.Trailers, Is.EqualTo(2));
        Assert.That((int)Http1ParseEventKind.Complete, Is.EqualTo(3));
        Assert.That((int)Http1ParseEventKind.ProtocolSwitch, Is.EqualTo(4));
        Assert.That((int)Http1ParseEventKind.TunnelBytes, Is.EqualTo(5));
    }

    [Test]
    public void Http1ConnectionModeValuesAreStable()
    {
        Assert.That((int)Http1ConnectionMode.Persistent, Is.EqualTo(0));
        Assert.That((int)Http1ConnectionMode.Close, Is.EqualTo(1));
        Assert.That((int)Http1ConnectionMode.Upgraded, Is.EqualTo(2));
        Assert.That((int)Http1ConnectionMode.Tunneled, Is.EqualTo(3));
        Assert.That((int)Http1ConnectionMode.Unusable, Is.EqualTo(4));
    }

    [Test]
    public void HpackIndexingModeValuesAreStable()
    {
        Assert.That((int)HpackIndexingMode.Incremental, Is.EqualTo(0));
        Assert.That((int)HpackIndexingMode.WithoutIndexing, Is.EqualTo(1));
        Assert.That((int)HpackIndexingMode.NeverIndexed, Is.EqualTo(2));
        Assert.That((int)HpackIndexingMode.Automatic, Is.EqualTo(3));
    }

    [Test]
    public void Http2EndpointRoleValuesAreStable()
    {
        Assert.That((int)Http2EndpointRole.Client, Is.EqualTo(0));
        Assert.That((int)Http2EndpointRole.Server, Is.EqualTo(1));
    }

    [Test]
    public void Http2FrameTypesMatchRegisteredWireCodes()
    {
        Assert.That((int)Http2FrameType.Unknown, Is.EqualTo(-1));
        Assert.That((int)Http2FrameType.Data, Is.EqualTo(0x00));
        Assert.That((int)Http2FrameType.Headers, Is.EqualTo(0x01));
        Assert.That((int)Http2FrameType.Priority, Is.EqualTo(0x02));
        Assert.That((int)Http2FrameType.RstStream, Is.EqualTo(0x03));
        Assert.That((int)Http2FrameType.Settings, Is.EqualTo(0x04));
        Assert.That((int)Http2FrameType.PushPromise, Is.EqualTo(0x05));
        Assert.That((int)Http2FrameType.Ping, Is.EqualTo(0x06));
        Assert.That((int)Http2FrameType.GoAway, Is.EqualTo(0x07));
        Assert.That((int)Http2FrameType.WindowUpdate, Is.EqualTo(0x08));
        Assert.That((int)Http2FrameType.Continuation, Is.EqualTo(0x09));
        Assert.That((int)Http2FrameType.Origin, Is.EqualTo(0x0c));
        Assert.That((int)Http2FrameType.PriorityUpdate, Is.EqualTo(0x10));
    }

    [Test]
    public void Http2ErrorCodesMatchRegisteredWireCodes()
    {
        Assert.That((int)Http2ErrorCode.Unknown, Is.EqualTo(-1));
        Assert.That((int)Http2ErrorCode.NoError, Is.EqualTo(0x00));
        Assert.That((int)Http2ErrorCode.ProtocolError, Is.EqualTo(0x01));
        Assert.That((int)Http2ErrorCode.InternalError, Is.EqualTo(0x02));
        Assert.That((int)Http2ErrorCode.FlowControlError, Is.EqualTo(0x03));
        Assert.That((int)Http2ErrorCode.SettingsTimeout, Is.EqualTo(0x04));
        Assert.That((int)Http2ErrorCode.StreamClosed, Is.EqualTo(0x05));
        Assert.That((int)Http2ErrorCode.FrameSizeError, Is.EqualTo(0x06));
        Assert.That((int)Http2ErrorCode.RefusedStream, Is.EqualTo(0x07));
        Assert.That((int)Http2ErrorCode.Cancel, Is.EqualTo(0x08));
        Assert.That((int)Http2ErrorCode.CompressionError, Is.EqualTo(0x09));
        Assert.That((int)Http2ErrorCode.ConnectError, Is.EqualTo(0x0a));
        Assert.That((int)Http2ErrorCode.EnhanceYourCalm, Is.EqualTo(0x0b));
        Assert.That((int)Http2ErrorCode.InadequateSecurity, Is.EqualTo(0x0c));
        Assert.That((int)Http2ErrorCode.Http11Required, Is.EqualTo(0x0d));
    }

    [Test]
    public void Http2StreamStateValuesAreStable()
    {
        Assert.That((int)Http2StreamState.Idle, Is.EqualTo(0));
        Assert.That((int)Http2StreamState.ReservedLocal, Is.EqualTo(1));
        Assert.That((int)Http2StreamState.ReservedRemote, Is.EqualTo(2));
        Assert.That((int)Http2StreamState.Open, Is.EqualTo(3));
        Assert.That((int)Http2StreamState.HalfClosedLocal, Is.EqualTo(4));
        Assert.That((int)Http2StreamState.HalfClosedRemote, Is.EqualTo(5));
        Assert.That((int)Http2StreamState.Closed, Is.EqualTo(6));
    }

    [Test]
    public void Http2EventKindValuesAreStable()
    {
        Assert.That((int)Http2EventKind.ConnectionReady, Is.EqualTo(0));
        Assert.That((int)Http2EventKind.FrameReceived, Is.EqualTo(1));
        Assert.That((int)Http2EventKind.MessageHead, Is.EqualTo(2));
        Assert.That((int)Http2EventKind.Content, Is.EqualTo(3));
        Assert.That((int)Http2EventKind.Trailers, Is.EqualTo(4));
        Assert.That((int)Http2EventKind.StreamComplete, Is.EqualTo(5));
        Assert.That((int)Http2EventKind.StreamReset, Is.EqualTo(6));
        Assert.That((int)Http2EventKind.SettingsChanged, Is.EqualTo(7));
        Assert.That((int)Http2EventKind.PingAcknowledged, Is.EqualTo(8));
        Assert.That((int)Http2EventKind.PushPromised, Is.EqualTo(9));
        Assert.That((int)Http2EventKind.Error, Is.EqualTo(10));
    }

    [Test]
    public void Http2ActionKindValuesAreStable()
    {
        Assert.That((int)Http2ActionKind.SendFrame, Is.EqualTo(0));
        Assert.That((int)Http2ActionKind.AcknowledgeSettings, Is.EqualTo(1));
        Assert.That((int)Http2ActionKind.AcknowledgePing, Is.EqualTo(2));
        Assert.That((int)Http2ActionKind.ResetStream, Is.EqualTo(3));
        Assert.That((int)Http2ActionKind.SendGoAway, Is.EqualTo(4));
        Assert.That((int)Http2ActionKind.ReleaseCapacity, Is.EqualTo(5));
        Assert.That((int)Http2ActionKind.InitiatePush, Is.EqualTo(6));
        Assert.That((int)Http2ActionKind.SendMessage, Is.EqualTo(7));
        Assert.That((int)Http2ActionKind.SendConnectionPreface,
            Is.EqualTo(8));
        Assert.That((int)Http2ActionKind.StartSettingsTimeout,
            Is.EqualTo(9));
        Assert.That((int)Http2ActionKind.CancelSettingsTimeout,
            Is.EqualTo(10));
        Assert.That((int)Http2ActionKind.StartPingTimeout,
            Is.EqualTo(11));
        Assert.That((int)Http2ActionKind.CancelPingTimeout,
            Is.EqualTo(12));
    }

    [Test]
    public void Http2ErrorScopeValuesAreStable()
    {
        Assert.That((int)Http2ErrorScope.None, Is.EqualTo(0));
        Assert.That((int)Http2ErrorScope.Stream, Is.EqualTo(1));
        Assert.That((int)Http2ErrorScope.Connection, Is.EqualTo(2));
    }

    [Test]
    public void Http2OriginOutcomeValuesAreStable()
    {
        Assert.That((int)Http2OriginOutcome.DefaultAuthorityRules,
            Is.EqualTo(0));
        Assert.That((int)Http2OriginOutcome.OriginSetAllows, Is.EqualTo(1));
        Assert.That((int)Http2OriginOutcome.OriginSetRejects, Is.EqualTo(2));
        Assert.That((int)Http2OriginOutcome.NotApplicable, Is.EqualTo(3));
    }
}
