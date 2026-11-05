using websocket;

namespace websocket.Tests;

public class ConnectionWireFixture
{
    internal string Name;
    internal string Provenance;
    internal WebSocketRole LocalRole;
    internal byte[] Wire;
    internal bool HasMessage;
    internal WebSocketMessageType MessageType;
    internal byte[] MessagePayload;
    internal bool HasPing;
    internal byte[] PingPayload;

    internal ConnectionWireFixture(
        string name,
        string provenance,
        WebSocketRole localRole,
        byte[] wire,
        bool hasMessage,
        WebSocketMessageType messageType,
        byte[] messagePayload,
        bool hasPing,
        byte[] pingPayload)
    {
        Name = name;
        Provenance = provenance;
        LocalRole = localRole;
        Wire = wire;
        HasMessage = hasMessage;
        MessageType = messageType;
        MessagePayload = messagePayload;
        HasPing = hasPing;
        PingPayload = pingPayload;
    }

    public override string ToString()
    {
        return Name + " (" + Provenance + ")";
    }
}

public class MalformedWireFixture
{
    internal string Name;
    internal string Provenance;
    internal WebSocketRole LocalRole;
    internal byte[] Wire;
    internal string ErrorIdentifier;

    internal MalformedWireFixture(
        string name,
        string provenance,
        WebSocketRole localRole,
        byte[] wire,
        string errorIdentifier)
    {
        Name = name;
        Provenance = provenance;
        LocalRole = localRole;
        Wire = wire;
        ErrorIdentifier = errorIdentifier;
    }

    public override string ToString()
    {
        return Name + " (" + Provenance + ")";
    }
}

public class IanaCloseCodeFixture
{
    internal string Name;
    internal string Provenance;
    internal int Code;
    internal WebSocketCloseCode KnownCode;

    internal IanaCloseCodeFixture(
        string name,
        int code,
        WebSocketCloseCode knownCode)
    {
        Name = name;
        Provenance =
            "specs/registries/iana-websocket.xml WebSocket Close Code Number Registry";
        Code = code;
        KnownCode = knownCode;
    }

    public override string ToString()
    {
        return Name + " (" + Provenance + ")";
    }
}

internal static class WebSocketProtocolFixtures
{
    internal const string CorrectedErrataKey = "AQIDBAUGBwgJCgsMDQ4PEA==";
    internal const string NonCanonicalErrataKey = "AQIDBAUGBwgJCgsMDQ4PEC==";
    internal const string KeyErrataProvenance =
        "specs/rfcs/rfc6455-inline-errata.html EID 3150";
    internal const string RepeatedExtensionsErrataProvenance =
        "specs/rfcs/rfc6455-inline-errata.html EID 3433";

    internal static readonly ConnectionWireFixture[] RfcExamples =
    [
        new ConnectionWireFixture(
            "RFC 6455 unmasked Hello",
            "specs/rfcs/rfc6455.txt section 5.7",
            WebSocketRole.Client,
            [0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f],
            true,
            WebSocketMessageType.Text,
            [0x48, 0x65, 0x6c, 0x6c, 0x6f],
            false,
            []),
        new ConnectionWireFixture(
            "RFC 6455 masked Hello",
            "specs/rfcs/rfc6455.txt section 5.7",
            WebSocketRole.Server,
            [0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58],
            true,
            WebSocketMessageType.Text,
            [0x48, 0x65, 0x6c, 0x6c, 0x6f],
            false,
            []),
        new ConnectionWireFixture(
            "RFC 6455 fragmented Hello",
            "specs/rfcs/rfc6455.txt section 5.7",
            WebSocketRole.Client,
            [0x01, 0x03, 0x48, 0x65, 0x6c, 0x80, 0x02, 0x6c, 0x6f],
            true,
            WebSocketMessageType.Text,
            [0x48, 0x65, 0x6c, 0x6c, 0x6f],
            false,
            []),
        new ConnectionWireFixture(
            "RFC 6455 unmasked Ping",
            "specs/rfcs/rfc6455.txt section 5.7",
            WebSocketRole.Client,
            [0x89, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f],
            false,
            WebSocketMessageType.Binary,
            [],
            true,
            [0x48, 0x65, 0x6c, 0x6c, 0x6f])
    ];

    internal static readonly ConnectionWireFixture[] LibraryInterop =
    [
        new ConnectionWireFixture(
            "Boost.Beast fragmented message with interleaved control frame",
            "specs/libraries/boost-beast-messages.html Messages and control-frame interleaving",
            WebSocketRole.Client,
            [
                0x01, 0x03, 0x48, 0x65, 0x6c,
                0x89, 0x01, 0x3f,
                0x80, 0x02, 0x6c, 0x6f
            ],
            true,
            WebSocketMessageType.Text,
            [0x48, 0x65, 0x6c, 0x6c, 0x6f],
            true,
            [0x3f]),
        new ConnectionWireFixture(
            "Gorilla masked client message and Ping",
            "specs/libraries/gorilla-websocket-doc.go Control Messages",
            WebSocketRole.Server,
            [
                0x89, 0x81, 0x05, 0x06, 0x07, 0x08, 0x3a,
                0x81, 0x82, 0x01, 0x02, 0x03, 0x04, 0x66, 0x6d
            ],
            true,
            WebSocketMessageType.Text,
            [0x67, 0x6f],
            true,
            [0x3f]),
        new ConnectionWireFixture(
            "ws Ping followed by binary message",
            "specs/libraries/ws-README.md How to detect and close broken connections",
            WebSocketRole.Client,
            [0x89, 0x02, 0x77, 0x73, 0x82, 0x02, 0x6f, 0x6b],
            true,
            WebSocketMessageType.Binary,
            [0x6f, 0x6b],
            true,
            [0x77, 0x73]),
        new ConnectionWireFixture(
            "Python websockets fragmented text message",
            "specs/libraries/python-websockets-features.html Messages feature matrix",
            WebSocketRole.Client,
            [0x01, 0x02, 0x70, 0x79, 0x80, 0x04, 0x74, 0x68, 0x6f, 0x6e],
            true,
            WebSocketMessageType.Text,
            [0x70, 0x79, 0x74, 0x68, 0x6f, 0x6e],
            false,
            []),
        new ConnectionWireFixture(
            "WebSocket++ RFC 6455 text message",
            "specs/libraries/websocketpp-README.md Major Features",
            WebSocketRole.Client,
            [0x81, 0x03, 0x63, 0x70, 0x70],
            true,
            WebSocketMessageType.Text,
            [0x63, 0x70, 0x70],
            false,
            [])
    ];

    internal static readonly MalformedWireFixture[] MalformedInputs =
    [
        new MalformedWireFixture(
            "Non-minimal 16-bit payload length",
            "specs/rfcs/rfc6455.txt section 5.2 minimal length encoding",
            WebSocketRole.Client,
            [0x82, 0x7e, 0x00, 0x7d],
            "frame-header-invalid"),
        new MalformedWireFixture(
            "Signed 64-bit payload length",
            "specs/rfcs/rfc6455.txt section 5.2 63-bit payload length",
            WebSocketRole.Client,
            [0x82, 0x7f, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            "frame-header-invalid"),
        new MalformedWireFixture(
            "Masked server-to-client frame",
            "specs/rfcs/rfc6455.txt section 5.1 masking",
            WebSocketRole.Client,
            [0x81, 0x80, 0x00, 0x00, 0x00, 0x00],
            "frame-mask-forbidden"),
        new MalformedWireFixture(
            "Unmasked client-to-server frame",
            "specs/rfcs/rfc6455.txt section 5.1 masking",
            WebSocketRole.Server,
            [0x81, 0x00],
            "frame-mask-required"),
        new MalformedWireFixture(
            "Fragmented Ping",
            "specs/rfcs/rfc6455.txt section 5.5 control frames",
            WebSocketRole.Client,
            [0x09, 0x00],
            "frame-control-fragmented"),
        new MalformedWireFixture(
            "Oversized Ping",
            "specs/rfcs/rfc6455.txt section 5.5 control frames",
            WebSocketRole.Client,
            [0x89, 0x7e, 0x00, 0x7e],
            "frame-control-too-large"),
        new MalformedWireFixture(
            "RSV1 without negotiated extension",
            "specs/rfcs/rfc6455.txt section 5.2 reserved bits",
            WebSocketRole.Client,
            [0xc1, 0x00],
            "frame-rsv1-unclaimed"),
        new MalformedWireFixture(
            "Unassigned data opcode",
            "specs/registries/iana-websocket.xml WebSocket Opcode Registry",
            WebSocketRole.Client,
            [0x83, 0x00],
            "frame-opcode-unclaimed")
    ];

    internal static readonly IanaCloseCodeFixture[] IanaCloseCodes =
    [
        new IanaCloseCodeFixture("Normal Closure", 1000, WebSocketCloseCode.NormalClosure),
        new IanaCloseCodeFixture("Going Away", 1001, WebSocketCloseCode.GoingAway),
        new IanaCloseCodeFixture("Protocol Error", 1002, WebSocketCloseCode.ProtocolError),
        new IanaCloseCodeFixture("Unsupported Data", 1003, WebSocketCloseCode.UnsupportedData),
        new IanaCloseCodeFixture("Invalid Frame Payload Data", 1007, WebSocketCloseCode.InvalidFramePayloadData),
        new IanaCloseCodeFixture("Policy Violation", 1008, WebSocketCloseCode.PolicyViolation),
        new IanaCloseCodeFixture("Message Too Big", 1009, WebSocketCloseCode.MessageTooBig),
        new IanaCloseCodeFixture("Mandatory Extension", 1010, WebSocketCloseCode.MandatoryExtension),
        new IanaCloseCodeFixture("Internal Error", 1011, WebSocketCloseCode.InternalError),
        new IanaCloseCodeFixture("Service Restart", 1012, WebSocketCloseCode.ServiceRestart),
        new IanaCloseCodeFixture("Try Again Later", 1013, WebSocketCloseCode.TryAgainLater),
        new IanaCloseCodeFixture("Bad Gateway", 1014, WebSocketCloseCode.BadGateway),
        new IanaCloseCodeFixture("Unauthorized", 3000, WebSocketCloseCode.Unauthorized),
        new IanaCloseCodeFixture("Forbidden", 3003, WebSocketCloseCode.Forbidden),
        new IanaCloseCodeFixture("Timeout", 3008, WebSocketCloseCode.Timeout)
    ];
}
