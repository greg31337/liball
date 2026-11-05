using websocket;

namespace websocket.Tests;

public class ProtocolConstantsTests
{
    [Test]
    public void HandshakeConstantsMatchTheProtocols()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketProtocolConstants.AcceptGuid, Is.EqualTo("258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            Assert.That(WebSocketProtocolConstants.Version, Is.EqualTo(13));
            Assert.That(WebSocketProtocolConstants.VersionText, Is.EqualTo("13"));
            Assert.That(WebSocketProtocolConstants.EnableConnectProtocolSettingIdentifier, Is.EqualTo(0x08));
            Assert.That(WebSocketProtocolConstants.Http1RequestMethod, Is.EqualTo("GET"));
            Assert.That(WebSocketProtocolConstants.Http1SwitchingProtocolsStatus, Is.EqualTo(101));
            Assert.That(WebSocketProtocolConstants.ExtendedConnectMethod, Is.EqualTo("CONNECT"));
            Assert.That(WebSocketProtocolConstants.ExtendedConnectProtocol, Is.EqualTo("websocket"));
            Assert.That(WebSocketProtocolConstants.WebSocketScheme, Is.EqualTo("ws"));
            Assert.That(WebSocketProtocolConstants.SecureWebSocketScheme, Is.EqualTo("wss"));
        });
    }

    [Test]
    public void HeaderNamesMatchWebSocketHttpFields()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketProtocolConstants.HostHeaderName, Is.EqualTo("Host"));
            Assert.That(WebSocketProtocolConstants.UpgradeHeaderName, Is.EqualTo("Upgrade"));
            Assert.That(WebSocketProtocolConstants.ConnectionHeaderName, Is.EqualTo("Connection"));
            Assert.That(WebSocketProtocolConstants.OriginHeaderName, Is.EqualTo("Origin"));
            Assert.That(WebSocketProtocolConstants.KeyHeaderName, Is.EqualTo("Sec-WebSocket-Key"));
            Assert.That(WebSocketProtocolConstants.AcceptHeaderName, Is.EqualTo("Sec-WebSocket-Accept"));
            Assert.That(WebSocketProtocolConstants.VersionHeaderName, Is.EqualTo("Sec-WebSocket-Version"));
            Assert.That(WebSocketProtocolConstants.ProtocolHeaderName, Is.EqualTo("Sec-WebSocket-Protocol"));
            Assert.That(WebSocketProtocolConstants.ExtensionsHeaderName, Is.EqualTo("Sec-WebSocket-Extensions"));
        });
    }

    [Test]
    public void PseudoFieldNamesMatchExtendedConnectFields()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketProtocolConstants.MethodPseudoFieldName, Is.EqualTo(":method"));
            Assert.That(WebSocketProtocolConstants.ProtocolPseudoFieldName, Is.EqualTo(":protocol"));
            Assert.That(WebSocketProtocolConstants.SchemePseudoFieldName, Is.EqualTo(":scheme"));
            Assert.That(WebSocketProtocolConstants.PathPseudoFieldName, Is.EqualTo(":path"));
            Assert.That(WebSocketProtocolConstants.AuthorityPseudoFieldName, Is.EqualTo(":authority"));
            Assert.That(WebSocketProtocolConstants.StatusPseudoFieldName, Is.EqualTo(":status"));
        });
    }

    [Test]
    public void FramingConstantsMatchRfc6455Bounds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketProtocolConstants.ClientNonceLength, Is.EqualTo(16));
            Assert.That(WebSocketProtocolConstants.AcceptDigestLength, Is.EqualTo(20));
            Assert.That(WebSocketProtocolConstants.MaskingKeyLength, Is.EqualTo(4));
            Assert.That(WebSocketProtocolConstants.MinimumFrameHeaderLength, Is.EqualTo(2));
            Assert.That(WebSocketProtocolConstants.MaximumFrameHeaderLength, Is.EqualTo(14));
            Assert.That(WebSocketProtocolConstants.MinimumOpcode, Is.EqualTo(0));
            Assert.That(WebSocketProtocolConstants.MaximumOpcode, Is.EqualTo(15));
            Assert.That(WebSocketProtocolConstants.BasicPayloadLengthMaximum, Is.EqualTo(125));
            Assert.That(WebSocketProtocolConstants.Extended16PayloadLengthMarker, Is.EqualTo(126));
            Assert.That(WebSocketProtocolConstants.Extended63PayloadLengthMarker, Is.EqualTo(127));
            Assert.That(WebSocketProtocolConstants.Extended16PayloadLengthMaximum, Is.EqualTo(65535));
            Assert.That(WebSocketProtocolConstants.PayloadLengthMaximum, Is.EqualTo(long.MaxValue));
            Assert.That(WebSocketProtocolConstants.ControlPayloadLengthMaximum, Is.EqualTo(125));
            Assert.That(WebSocketProtocolConstants.CloseCodeLength, Is.EqualTo(2));
            Assert.That(WebSocketProtocolConstants.CloseReasonLengthMaximum, Is.EqualTo(123));
        });
    }
}
