using websocket;

namespace websocket.Tests;

public class EnumDataWiringTests
{
    [Test]
    public void FrameResultCarriesOpcodeParseStatusAndErrorVocabulary()
    {
        var result = new WebSocketFrameReadResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.HasHeader = true;
        result.Header.Opcode = WebSocketOpcode.ReservedControl15;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Protocol;
        result.Error.HasCloseCode = true;
        result.Error.CloseCode = 1002;
        result.Error.HasKnownCloseCode = true;
        result.Error.KnownCloseCode = WebSocketCloseCode.ProtocolError;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.HasHeader, Is.True);
            Assert.That(result.Header.Opcode, Is.EqualTo(WebSocketOpcode.ReservedControl15));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(result.Error.HasCloseCode, Is.True);
            Assert.That(result.Error.CloseCode, Is.EqualTo(1002));
            Assert.That(result.Error.HasKnownCloseCode, Is.True);
            Assert.That(result.Error.KnownCloseCode, Is.EqualTo(WebSocketCloseCode.ProtocolError));
        });
    }

    [Test]
    public void HandshakeResultCarriesOutcomeTransportAndExtensionVocabulary()
    {
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Status = WebSocketExtensionStatus.Negotiated;

        var result = new WebSocketHandshakeResult();
        result.Outcome = WebSocketHandshakeOutcome.Accepted;
        result.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        result.Extensions = new[] { negotiation };

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].Status, Is.EqualTo(WebSocketExtensionStatus.Negotiated));
        });
    }

    [Test]
    public void ConnectionInfoCarriesRoleStateAndDeliveryVocabulary()
    {
        var info = new WebSocketConnectionInfo();
        info.Role = WebSocketRole.Server;
        info.State = WebSocketConnectionState.Closing;
        info.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        info.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;

        Assert.Multiple(() =>
        {
            Assert.That(info.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(info.State, Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(info.DeliveryMode, Is.EqualTo(WebSocketMessageDeliveryMode.Streaming));
            Assert.That(info.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
        });
    }

    [Test]
    public void EventCarriesEventAndMessageVocabulary()
    {
        var connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.MessageData;
        connectionEvent.HasMessageType = true;
        connectionEvent.MessageType = WebSocketMessageType.Binary;

        Assert.Multiple(() =>
        {
            Assert.That(connectionEvent.Type, Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(connectionEvent.HasMessageType, Is.True);
            Assert.That(connectionEvent.MessageType, Is.EqualTo(WebSocketMessageType.Binary));
        });
    }

    [Test]
    public void ErrorRetainsFutureAndPrivateRawCloseCodes()
    {
        var error = new WebSocketError();
        error.HasCloseCode = true;
        error.CloseCode = 4321;

        Assert.Multiple(() =>
        {
            Assert.That(error.HasCloseCode, Is.True);
            Assert.That(error.CloseCode, Is.EqualTo(4321));
        });
    }
}
