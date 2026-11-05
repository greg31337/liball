using websocket;

namespace websocket.Tests;

public class WebSocketConnectionProtocolFailureTests
{
    [TestCase("fragment-overlap")]
    [TestCase("orphan-continuation")]
    [TestCase("rsv1")]
    [TestCase("rsv2")]
    [TestCase("rsv3")]
    [TestCase("reserved-data-opcode")]
    [TestCase("reserved-control-opcode")]
    [TestCase("masked-server-frame")]
    [TestCase("unmasked-client-frame")]
    [TestCase("non-minimal-16-bit-length")]
    [TestCase("non-minimal-64-bit-length")]
    public void ProtocolDefectsFailTheConnectionAndSendClose1002(
        string mutation)
    {
        WebSocketRole role = mutation == "unmasked-client-frame"
            ? WebSocketRole.Server
            : WebSocketRole.Client;
        WebSocketConnection connection = Connection(role, 32, 64);
        byte[] wire = ProtocolWire(mutation);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);

        AssertFailure(
            connection,
            role,
            result,
            WebSocketParseStatus.ProtocolFailure,
            WebSocketErrorKind.Protocol,
            WebSocketCloseCodes.ProtocolError,
            WebSocketCloseCode.ProtocolError);
    }

    [TestCase(new byte[] { 0xc0 }, "stream-text-utf8-invalid")]
    [TestCase(new byte[] { 0xe2 }, "stream-text-utf8-incomplete")]
    public void InvalidTextFailsTheConnectionAndSendsClose1007(
        byte[] payload,
        string identifier)
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            32,
            64);
        byte[] wire = Frame(WebSocketOpcode.Text, true, false, payload);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);

        Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        AssertFailure(
            connection,
            WebSocketRole.Client,
            result,
            WebSocketParseStatus.ProtocolFailure,
            WebSocketErrorKind.Utf8,
            WebSocketCloseCodes.InvalidFramePayloadData,
            WebSocketCloseCode.InvalidFramePayloadData);
    }

    [Test]
    public void OversizedFrameFailsTheConnectionAndSendsClose1009()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            4,
            8);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes([0x82, 0x05]);

        Assert.That(result.Error.Identifier,
            Is.EqualTo("frame-payload-too-large"));
        AssertFailure(
            connection,
            WebSocketRole.Client,
            result,
            WebSocketParseStatus.LimitRejected,
            WebSocketErrorKind.Limit,
            WebSocketCloseCodes.MessageTooBig,
            WebSocketCloseCode.MessageTooBig);
    }

    [Test]
    public void InvalidCompressedPayloadFailsTheConnectionAndSendsClose1002()
    {
        WebSocketConnection connection = CompressedClientConnection();
        byte[] malformed = [0xc1, 0x01, 0x07];

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(malformed);

        Assert.That(result.Error.Identifier,
            Is.EqualTo("deflate-block-type-invalid"));
        AssertFailure(
            connection,
            WebSocketRole.Client,
            result,
            WebSocketParseStatus.ProtocolFailure,
            WebSocketErrorKind.Compression,
            WebSocketCloseCodes.ProtocolError,
            WebSocketCloseCode.ProtocolError);
    }

    static WebSocketConnection Connection(
        WebSocketRole role,
        int maximumFramePayloadLength,
        int maximumMessagePayloadLength)
    {
        var options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            MaximumFramePayloadLength = maximumFramePayloadLength,
            MaximumMessagePayloadLength = maximumMessagePayloadLength,
            MaximumDecompressedMessagePayloadLength = maximumMessagePayloadLength,
            MaximumFragmentCount = 16,
            MaximumBufferedInputBytes = 128,
            MaximumBufferedOutputBytes = 128,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = maximumFramePayloadLength,
            CloseTimeoutMilliseconds = 1000
        };
        if (role == WebSocketRole.Client)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = new ProtocolFailureMaskKeySource();
        }

        return new WebSocketConnection(options, Handshake([]));
    }

    static WebSocketConnection CompressedClientConnection()
    {
        var compressionOptions = new PerMessageDeflateOptions
        {
            CompressionThreshold = 0,
            MaximumDecompressedMessagePayloadLength = 128
        };
        var clientExtension = new PerMessageDeflateExtension(compressionOptions);
        var serverExtension = new PerMessageDeflateExtension(compressionOptions);
        WebSocketExtensionOffer offer =
            clientExtension.CreateClientExtensionOffer();
        WebSocketExtensionNegotiation accepted =
            serverExtension.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            clientExtension.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        var options = new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            Extensions = [clientExtension],
            HasPerMessageDeflate = true,
            PerMessageDeflate = compressionOptions,
            MaximumFramePayloadLength = 64,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            MaximumFragmentCount = 16,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = 64,
            CloseTimeoutMilliseconds = 1000,
            HasMaskKeySource = true,
            MaskKeySource = new ProtocolFailureMaskKeySource()
        };
        return new WebSocketConnection(options, Handshake([validated]));
    }

    static WebSocketHandshakeResult Handshake(
        WebSocketExtensionNegotiation[] extensions)
    {
        return new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Extensions = extensions
        };
    }

    static byte[] ProtocolWire(string mutation)
    {
        if (mutation == "fragment-overlap")
        {
            return Combine(
                Frame(WebSocketOpcode.Text, false, false, []),
                Frame(WebSocketOpcode.Binary, true, false, []));
        }

        if (mutation == "orphan-continuation")
        {
            return Frame(WebSocketOpcode.Continuation, true, false, []);
        }

        if (mutation == "rsv1")
        {
            return [0xc2, 0x00];
        }

        if (mutation == "rsv2")
        {
            return [0xa2, 0x00];
        }

        if (mutation == "rsv3")
        {
            return [0x92, 0x00];
        }

        if (mutation == "reserved-data-opcode")
        {
            return [0x83, 0x00];
        }

        if (mutation == "reserved-control-opcode")
        {
            return [0x8b, 0x00];
        }

        if (mutation == "masked-server-frame")
        {
            return Frame(WebSocketOpcode.Binary, true, true, []);
        }

        if (mutation == "unmasked-client-frame")
        {
            return Frame(WebSocketOpcode.Binary, true, false, []);
        }

        if (mutation == "non-minimal-16-bit-length")
        {
            return [0x82, 0x7e, 0x00, 0x7d];
        }

        return
        [
            0x82, 0x7f,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x7d
        ];
    }

    static byte[] Frame(
        WebSocketOpcode opcode,
        bool final,
        bool masked,
        byte[] payload)
    {
        int headerLength = masked ? 6 : 2;
        byte[] frame = new byte[headerLength + payload.Length];
        frame[0] = (byte)((final ? 0x80 : 0) | (int)opcode);
        frame[1] = (byte)((masked ? 0x80 : 0) | payload.Length);
        if (masked)
        {
            frame[2] = 1;
            frame[3] = 2;
            frame[4] = 3;
            frame[5] = 4;
            int index = 0;
            while (index < payload.Length)
            {
                frame[6 + index] = (byte)(payload[index] ^ frame[2 + index % 4]);
                index++;
            }
        }
        else
        {
            payload.CopyTo(frame, 2);
        }

        return frame;
    }

    static byte[] Combine(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }

        return result;
    }

    static void AssertFailure(
        WebSocketConnection connection,
        WebSocketRole role,
        WebSocketInboundFramePumpResult result,
        WebSocketParseStatus expectedStatus,
        WebSocketErrorKind expectedKind,
        int expectedCloseCode,
        WebSocketCloseCode expectedKnownCloseCode)
    {
        WebSocketConnectionInfo information = connection.GetConnectionInfo();
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketEvent errorEvent = events.Single(item =>
            item.Type == WebSocketEventType.Error);
        byte[] closeWire = DrainAll(connection);
        DecodedFailureClose close = DecodeClose(role, closeWire);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Error.HasCloseCode, Is.True);
            Assert.That(result.Error.CloseCode, Is.EqualTo(expectedCloseCode));
            Assert.That(result.Error.HasKnownCloseCode, Is.True);
            Assert.That(result.Error.KnownCloseCode,
                Is.EqualTo(expectedKnownCloseCode));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(information.HasError, Is.True);
            Assert.That(information.Error.Kind, Is.EqualTo(expectedKind));
            Assert.That(information.Error.CloseCode,
                Is.EqualTo(expectedCloseCode));
            Assert.That(errorEvent.Error.Kind, Is.EqualTo(expectedKind));
            Assert.That(errorEvent.Error.CloseCode,
                Is.EqualTo(expectedCloseCode));
            Assert.That(close.Opcode, Is.EqualTo(WebSocketOpcode.Close));
            Assert.That(close.Masked,
                Is.EqualTo(role == WebSocketRole.Client));
            Assert.That(close.CloseCode, Is.EqualTo(expectedCloseCode));
        });
    }

    static List<WebSocketEvent> DrainEvents(WebSocketConnection connection)
    {
        var events = new List<WebSocketEvent>();
        WebSocketEvent? item = connection.ReadNextEvent();
        while (item != null)
        {
            events.Add(item);
            item = connection.ReadNextEvent();
        }

        return events;
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static DecodedFailureClose DecodeClose(
        WebSocketRole senderRole,
        byte[] wire)
    {
        WebSocketRole receiverRole = senderRole == WebSocketRole.Client
            ? WebSocketRole.Server
            : WebSocketRole.Client;
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            receiverRole,
            125,
            []);
        WebSocketFrameReadResult result = codec.ReadFrame(wire);
        byte[] payload = result.Payload
            .AsSpan(result.PayloadOffset, result.PayloadCount)
            .ToArray();
        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        Assert.That(payload, Has.Length.GreaterThanOrEqualTo(2));
        return new DecodedFailureClose
        {
            Opcode = result.Header.Opcode,
            Masked = result.Header.Masked,
            CloseCode = (payload[0] << 8) | payload[1]
        };
    }
}

internal class DecodedFailureClose
{
    internal WebSocketOpcode Opcode;
    internal bool Masked;
    internal int CloseCode;
}

internal class ProtocolFailureMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x11111111U;
        return key;
    }
}
