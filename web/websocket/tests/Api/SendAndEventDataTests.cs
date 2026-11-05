using websocket;

namespace websocket.Tests;

public class SendAndEventDataTests
{
    [Test]
    public void WholeMessageSendUsesCallerOwnedRangeAndExplicitFrameOverride()
    {
        var payload = new byte[] { 0, 1, 2, 3, 0 };
        var request = new WebSocketSendRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.MessageType, Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(request.Payload, Is.Empty);
            Assert.That(request.PayloadOffset, Is.Zero);
            Assert.That(request.PayloadCount, Is.Zero);
            Assert.That(request.Compress, Is.False);
            Assert.That(request.HasFramePayloadLength, Is.False);
            Assert.That(request.FramePayloadLength, Is.Zero);
        });

        request.MessageType = WebSocketMessageType.Binary;
        request.Payload = payload;
        request.PayloadOffset = 1;
        request.PayloadCount = 3;
        request.Compress = true;
        request.HasFramePayloadLength = true;
        request.FramePayloadLength = 2;

        Assert.Multiple(() =>
        {
            Assert.That(request.MessageType, Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(request.Payload, Is.SameAs(payload));
            Assert.That(request.Payload.Skip(request.PayloadOffset).Take(request.PayloadCount), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(request.Compress, Is.True);
            Assert.That(request.HasFramePayloadLength, Is.True);
            Assert.That(request.FramePayloadLength, Is.EqualTo(2));
        });
    }

    [Test]
    public void StreamingSendChunkCarriesBoundaryAndCompressionChoices()
    {
        var payload = new byte[] { 10, 20 };
        var chunk = new WebSocketSendChunk();

        Assert.Multiple(() =>
        {
            Assert.That(chunk.First, Is.False);
            Assert.That(chunk.Final, Is.False);
            Assert.That(chunk.MessageType, Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(chunk.Payload, Is.Empty);
            Assert.That(chunk.PayloadOffset, Is.Zero);
            Assert.That(chunk.PayloadCount, Is.Zero);
            Assert.That(chunk.Compress, Is.False);
        });

        chunk.First = true;
        chunk.Final = false;
        chunk.MessageType = WebSocketMessageType.Text;
        chunk.Payload = payload;
        chunk.PayloadOffset = 0;
        chunk.PayloadCount = 2;
        chunk.Compress = true;

        Assert.Multiple(() =>
        {
            Assert.That(chunk.First, Is.True);
            Assert.That(chunk.Final, Is.False);
            Assert.That(chunk.MessageType, Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(chunk.Payload, Is.SameAs(payload));
            Assert.That(chunk.PayloadCount, Is.EqualTo(2));
            Assert.That(chunk.Compress, Is.True);
        });
    }

    [Test]
    public void EventDefaultsUseExplicitOptionalValueFlags()
    {
        var first = new WebSocketEvent();
        var second = new WebSocketEvent();

        Assert.Multiple(() =>
        {
            Assert.That(first.Type, Is.EqualTo(WebSocketEventType.MessageStart));
            Assert.That(first.HasMessageType, Is.False);
            Assert.That(first.MessageType, Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(first.Payload, Is.Empty);
            Assert.That(first.PayloadOffset, Is.Zero);
            Assert.That(first.PayloadCount, Is.Zero);
            Assert.That(first.First, Is.False);
            Assert.That(first.Final, Is.False);
            Assert.That(first.HasPingElapsedMilliseconds, Is.False);
            Assert.That(first.PingElapsedMilliseconds, Is.Zero);
            Assert.That(first.HasClose, Is.False);
            Assert.That(first.HasError, Is.False);
            Assert.That(first.Close, Is.Not.SameAs(second.Close));
            Assert.That(first.Error, Is.Not.SameAs(second.Error));
        });
    }

    [Test]
    public void EventCarriesMessagePayloadBoundaries()
    {
        var payload = new byte[] { 99, 65, 66, 67 };
        var connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.MessageData;
        connectionEvent.HasMessageType = true;
        connectionEvent.MessageType = WebSocketMessageType.Text;
        connectionEvent.Payload = payload;
        connectionEvent.PayloadOffset = 1;
        connectionEvent.PayloadCount = 3;
        connectionEvent.First = true;
        connectionEvent.Final = true;

        Assert.Multiple(() =>
        {
            Assert.That(connectionEvent.Type, Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(connectionEvent.HasMessageType, Is.True);
            Assert.That(connectionEvent.MessageType, Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(connectionEvent.Payload, Is.SameAs(payload));
            Assert.That(connectionEvent.Payload.Skip(connectionEvent.PayloadOffset).Take(connectionEvent.PayloadCount), Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(connectionEvent.First, Is.True);
            Assert.That(connectionEvent.Final, Is.True);
        });
    }

    [Test]
    public void EventCarriesPingCloseAndErrorDetailsWhenPresent()
    {
        var connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.Error;
        connectionEvent.HasPingElapsedMilliseconds = true;
        connectionEvent.PingElapsedMilliseconds = 42;
        connectionEvent.HasClose = true;
        connectionEvent.Close.HasCode = true;
        connectionEvent.Close.Code = 1006;
        connectionEvent.HasError = true;
        connectionEvent.Error.Kind = WebSocketErrorKind.Transport;

        Assert.Multiple(() =>
        {
            Assert.That(connectionEvent.HasPingElapsedMilliseconds, Is.True);
            Assert.That(connectionEvent.PingElapsedMilliseconds, Is.EqualTo(42));
            Assert.That(connectionEvent.HasClose, Is.True);
            Assert.That(connectionEvent.Close.Code, Is.EqualTo(1006));
            Assert.That(connectionEvent.HasError, Is.True);
            Assert.That(connectionEvent.Error.Kind, Is.EqualTo(WebSocketErrorKind.Transport));
        });
    }
}
