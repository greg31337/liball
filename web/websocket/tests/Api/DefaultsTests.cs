using websocket;

namespace websocket.Tests;

public class DefaultsTests
{
    [Test]
    public void ResourceDefaultsAreFiniteAndPositive()
    {
        var defaults = new[]
        {
            WebSocketDefaults.MaximumHttpHeaderBytes,
            WebSocketDefaults.MaximumHttpHeaderCount,
            WebSocketDefaults.MaximumHttpLineLength,
            WebSocketDefaults.MaximumFramePayloadLength,
            WebSocketDefaults.MaximumMessagePayloadLength,
            WebSocketDefaults.MaximumDecompressedMessagePayloadLength,
            WebSocketDefaults.MaximumFragmentCount,
            WebSocketDefaults.MaximumBufferedInputBytes,
            WebSocketDefaults.MaximumBufferedOutputBytes,
            WebSocketDefaults.MaximumQueuedEvents,
            WebSocketDefaults.AutomaticFramePayloadLength,
            WebSocketDefaults.CloseTimeoutMilliseconds,
            WebSocketDefaults.HeartbeatIdleIntervalMilliseconds,
            WebSocketDefaults.HeartbeatPongTimeoutMilliseconds,
            WebSocketDefaults.HeartbeatTokenLength
        };

        Assert.That(defaults, Has.All.GreaterThan(0));
        Assert.That(defaults, Has.All.LessThan(int.MaxValue));
    }

    [Test]
    public void HeaderDefaultsHaveSafeInternalRelationships()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketDefaults.MaximumHttpHeaderBytes, Is.EqualTo(16384));
            Assert.That(WebSocketDefaults.MaximumHttpHeaderCount, Is.EqualTo(100));
            Assert.That(WebSocketDefaults.MaximumHttpLineLength, Is.EqualTo(8192));
            Assert.That(WebSocketDefaults.MaximumHttpLineLength, Is.LessThanOrEqualTo(WebSocketDefaults.MaximumHttpHeaderBytes));
        });
    }

    [Test]
    public void PayloadAndBufferDefaultsAreBounded()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketDefaults.MaximumFramePayloadLength, Is.EqualTo(16 * 1024 * 1024));
            Assert.That(WebSocketDefaults.MaximumMessagePayloadLength, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(WebSocketDefaults.MaximumDecompressedMessagePayloadLength, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(WebSocketDefaults.MaximumFragmentCount, Is.EqualTo(1024));
            Assert.That(WebSocketDefaults.MaximumBufferedInputBytes, Is.EqualTo(1024 * 1024));
            Assert.That(WebSocketDefaults.MaximumBufferedOutputBytes, Is.EqualTo(1024 * 1024));
            Assert.That(WebSocketDefaults.MaximumQueuedEvents, Is.EqualTo(256));
            Assert.That(WebSocketDefaults.AutomaticFramePayloadLength, Is.EqualTo(64 * 1024));
            Assert.That(WebSocketDefaults.MaximumFramePayloadLength, Is.LessThanOrEqualTo(WebSocketDefaults.MaximumMessagePayloadLength));
            Assert.That(WebSocketDefaults.AutomaticFramePayloadLength, Is.LessThanOrEqualTo(WebSocketDefaults.MaximumFramePayloadLength));
        });
    }

    [Test]
    public void TimingDefaultsAreExplicitMilliseconds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketDefaults.CloseTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(WebSocketDefaults.HeartbeatIdleIntervalMilliseconds, Is.EqualTo(30000));
            Assert.That(WebSocketDefaults.HeartbeatPongTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(WebSocketDefaults.HeartbeatTokenLength, Is.EqualTo(8));
            Assert.That(WebSocketDefaults.HeartbeatTokenLength, Is.LessThanOrEqualTo(WebSocketProtocolConstants.ControlPayloadLengthMaximum));
        });
    }
}
