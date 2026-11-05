using websocket;

namespace websocket.Tests;

public class EnumValuesTests
{
    [Test]
    public void RoleValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketRole>(),
            Is.EqualTo(new[]
            {
                WebSocketRole.Client,
                WebSocketRole.Server
            }));

        Assert.That(
            Enum.GetValues<WebSocketRole>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void OpcodeValuesCoverEveryFourBitValue()
    {
        Assert.That(
            Enum.GetValues<WebSocketOpcode>(),
            Is.EqualTo(new[]
            {
                WebSocketOpcode.Continuation,
                WebSocketOpcode.Text,
                WebSocketOpcode.Binary,
                WebSocketOpcode.ReservedData3,
                WebSocketOpcode.ReservedData4,
                WebSocketOpcode.ReservedData5,
                WebSocketOpcode.ReservedData6,
                WebSocketOpcode.ReservedData7,
                WebSocketOpcode.Close,
                WebSocketOpcode.Ping,
                WebSocketOpcode.Pong,
                WebSocketOpcode.ReservedControl11,
                WebSocketOpcode.ReservedControl12,
                WebSocketOpcode.ReservedControl13,
                WebSocketOpcode.ReservedControl14,
                WebSocketOpcode.ReservedControl15
            }));

        Assert.That(
            Enum.GetValues<WebSocketOpcode>().Select(value => (int)value),
            Is.EqualTo(Enumerable.Range(0, 16)));
    }

    [Test]
    public void MessageTypeValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)WebSocketMessageType.Text, Is.EqualTo(0));
            Assert.That((int)WebSocketMessageType.Binary, Is.EqualTo(1));
        });
    }

    [Test]
    public void DeliveryModeValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)WebSocketMessageDeliveryMode.Reassembled, Is.EqualTo(0));
            Assert.That((int)WebSocketMessageDeliveryMode.Streaming, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConnectionStateValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketConnectionState>(),
            Is.EqualTo(new[]
            {
                WebSocketConnectionState.Connecting,
                WebSocketConnectionState.Open,
                WebSocketConnectionState.Closing,
                WebSocketConnectionState.Closed,
                WebSocketConnectionState.Failed
            }));

        Assert.That(
            Enum.GetValues<WebSocketConnectionState>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void ParseStatusValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketParseStatus>(),
            Is.EqualTo(new[]
            {
                WebSocketParseStatus.Complete,
                WebSocketParseStatus.NeedInput,
                WebSocketParseStatus.NeedOutputCapacity,
                WebSocketParseStatus.NeedEventCapacity,
                WebSocketParseStatus.End,
                WebSocketParseStatus.LimitRejected,
                WebSocketParseStatus.ProtocolFailure
            }));

        Assert.That(
            Enum.GetValues<WebSocketParseStatus>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void EventTypeValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketEventType>(),
            Is.EqualTo(new[]
            {
                WebSocketEventType.MessageStart,
                WebSocketEventType.MessageData,
                WebSocketEventType.MessageEnd,
                WebSocketEventType.Ping,
                WebSocketEventType.Pong,
                WebSocketEventType.PingAcknowledged,
                WebSocketEventType.Close,
                WebSocketEventType.Error,
                WebSocketEventType.TransportEnd
            }));

        Assert.That(
            Enum.GetValues<WebSocketEventType>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public void HandshakeTransportValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)WebSocketHandshakeTransport.Http1Upgrade, Is.EqualTo(0));
            Assert.That((int)WebSocketHandshakeTransport.Http2ExtendedConnect, Is.EqualTo(1));
            Assert.That((int)WebSocketHandshakeTransport.Http3ExtendedConnect, Is.EqualTo(2));
        });
    }

    [Test]
    public void HandshakeOutcomeValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketHandshakeOutcome>(),
            Is.EqualTo(new[]
            {
                WebSocketHandshakeOutcome.Accepted,
                WebSocketHandshakeOutcome.Rejected,
                WebSocketHandshakeOutcome.Incomplete,
                WebSocketHandshakeOutcome.Failed
            }));

        Assert.That(
            Enum.GetValues<WebSocketHandshakeOutcome>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void ExtensionStatusValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketExtensionStatus>(),
            Is.EqualTo(new[]
            {
                WebSocketExtensionStatus.Offered,
                WebSocketExtensionStatus.Negotiated,
                WebSocketExtensionStatus.Declined,
                WebSocketExtensionStatus.Invalid
            }));

        Assert.That(
            Enum.GetValues<WebSocketExtensionStatus>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void ErrorKindValuesAreStable()
    {
        Assert.That(
            Enum.GetValues<WebSocketErrorKind>(),
            Is.EqualTo(new[]
            {
                WebSocketErrorKind.Handshake,
                WebSocketErrorKind.Protocol,
                WebSocketErrorKind.Utf8,
                WebSocketErrorKind.Compression,
                WebSocketErrorKind.Limit,
                WebSocketErrorKind.State,
                WebSocketErrorKind.Transport
            }));

        Assert.That(
            Enum.GetValues<WebSocketErrorKind>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void CloseCodeValuesMatchRfcAndIanaAssignments()
    {
        var expectedCodes = new[]
        {
            WebSocketCloseCode.NormalClosure,
            WebSocketCloseCode.GoingAway,
            WebSocketCloseCode.ProtocolError,
            WebSocketCloseCode.UnsupportedData,
            WebSocketCloseCode.Reserved,
            WebSocketCloseCode.NoStatusReceived,
            WebSocketCloseCode.AbnormalClosure,
            WebSocketCloseCode.InvalidFramePayloadData,
            WebSocketCloseCode.PolicyViolation,
            WebSocketCloseCode.MessageTooBig,
            WebSocketCloseCode.MandatoryExtension,
            WebSocketCloseCode.InternalError,
            WebSocketCloseCode.ServiceRestart,
            WebSocketCloseCode.TryAgainLater,
            WebSocketCloseCode.BadGateway,
            WebSocketCloseCode.TlsHandshake,
            WebSocketCloseCode.Unauthorized,
            WebSocketCloseCode.Forbidden,
            WebSocketCloseCode.Timeout
        };

        Assert.That(Enum.GetValues<WebSocketCloseCode>(), Is.EqualTo(expectedCodes));
        Assert.That(
            expectedCodes.Select(value => (int)value),
            Is.EqualTo(new[]
            {
                1000,
                1001,
                1002,
                1003,
                1004,
                1005,
                1006,
                1007,
                1008,
                1009,
                1010,
                1011,
                1012,
                1013,
                1014,
                1015,
                3000,
                3003,
                3008
            }));
    }
}
