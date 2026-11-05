using websocket;

namespace websocket.Tests;

public class ExceptionBoundaryTests
{
    [Test]
    public void ProtocolExceptionHasStableMessageWhenThrown()
    {
        var exception = new WebSocketProtocolException();

        Assert.That(
            () => throw exception,
            Throws.TypeOf<WebSocketProtocolException>()
                .With.Message.EqualTo("The WebSocket wire protocol is invalid."));
    }

    [Test]
    public void HandshakeExceptionHasStableMessageWhenThrown()
    {
        var exception = new WebSocketHandshakeException();

        Assert.That(
            () => throw exception,
            Throws.TypeOf<WebSocketHandshakeException>()
                .With.Message.EqualTo("The WebSocket opening handshake is invalid."));
    }

    [Test]
    public void ExtensionExceptionHasStableMessageWhenThrown()
    {
        var exception = new WebSocketExtensionException();

        Assert.That(
            () => throw exception,
            Throws.TypeOf<WebSocketExtensionException>()
                .With.Message.EqualTo("The WebSocket extension operation failed."));
    }

    [Test]
    public void LimitExceptionHasStableMessageWhenThrown()
    {
        var exception = new WebSocketLimitException();

        Assert.That(
            () => throw exception,
            Throws.TypeOf<WebSocketLimitException>()
                .With.Message.EqualTo("The WebSocket operation exceeds a configured limit."));
    }

    [Test]
    public void StateExceptionHasStableMessageWhenThrown()
    {
        var exception = new WebSocketStateException();

        Assert.That(
            () => throw exception,
            Throws.TypeOf<WebSocketStateException>()
                .With.Message.EqualTo("The WebSocket operation is invalid in the current state."));
    }
}
