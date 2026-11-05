namespace websocket;

/// <summary>
/// Provides fixed values defined by the WebSocket and Extended CONNECT protocols.
/// </summary>
public static class WebSocketProtocolConstants
{
    /// <summary>
    /// The RFC 6455 GUID appended when computing <c>Sec-WebSocket-Accept</c>.
    /// </summary>
    public const string AcceptGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    /// <summary>
    /// The supported WebSocket protocol version.
    /// </summary>
    public const int Version = 13;

    /// <summary>
    /// The supported WebSocket protocol version formatted for an HTTP field.
    /// </summary>
    public const string VersionText = "13";

    /// <summary>
    /// The HTTP/2 and HTTP/3 setting identifier for Extended CONNECT protocol support.
    /// </summary>
    public const int EnableConnectProtocolSettingIdentifier = 8;

    /// <summary>
    /// The HTTP/1 request method used by an Upgrade handshake.
    /// </summary>
    public const string Http1RequestMethod = "GET";

    /// <summary>
    /// The HTTP/1 success status used by an Upgrade handshake.
    /// </summary>
    public const int Http1SwitchingProtocolsStatus = 101;

    /// <summary>
    /// The method used by an Extended CONNECT handshake.
    /// </summary>
    public const string ExtendedConnectMethod = "CONNECT";

    /// <summary>
    /// The Extended CONNECT protocol value for WebSocket.
    /// </summary>
    public const string ExtendedConnectProtocol = "websocket";

    /// <summary>
    /// The cleartext WebSocket URI scheme.
    /// </summary>
    public const string WebSocketScheme = "ws";

    /// <summary>
    /// The TLS-protected WebSocket URI scheme.
    /// </summary>
    public const string SecureWebSocketScheme = "wss";

    /// <summary>
    /// The HTTP <c>Host</c> field name.
    /// </summary>
    public const string HostHeaderName = "Host";

    /// <summary>
    /// The HTTP <c>Upgrade</c> field name.
    /// </summary>
    public const string UpgradeHeaderName = "Upgrade";

    /// <summary>
    /// The HTTP <c>Connection</c> field name.
    /// </summary>
    public const string ConnectionHeaderName = "Connection";

    /// <summary>
    /// The HTTP <c>Origin</c> field name.
    /// </summary>
    public const string OriginHeaderName = "Origin";

    /// <summary>
    /// The HTTP <c>Sec-WebSocket-Key</c> field name.
    /// </summary>
    public const string KeyHeaderName = "Sec-WebSocket-Key";

    /// <summary>
    /// The HTTP <c>Sec-WebSocket-Accept</c> field name.
    /// </summary>
    public const string AcceptHeaderName = "Sec-WebSocket-Accept";

    /// <summary>
    /// The HTTP <c>Sec-WebSocket-Version</c> field name.
    /// </summary>
    public const string VersionHeaderName = "Sec-WebSocket-Version";

    /// <summary>
    /// The HTTP <c>Sec-WebSocket-Protocol</c> field name.
    /// </summary>
    public const string ProtocolHeaderName = "Sec-WebSocket-Protocol";

    /// <summary>
    /// The HTTP <c>Sec-WebSocket-Extensions</c> field name.
    /// </summary>
    public const string ExtensionsHeaderName = "Sec-WebSocket-Extensions";

    /// <summary>
    /// The HTTP <c>:method</c> pseudo-field name.
    /// </summary>
    public const string MethodPseudoFieldName = ":method";

    /// <summary>
    /// The HTTP <c>:protocol</c> pseudo-field name.
    /// </summary>
    public const string ProtocolPseudoFieldName = ":protocol";

    /// <summary>
    /// The HTTP <c>:scheme</c> pseudo-field name.
    /// </summary>
    public const string SchemePseudoFieldName = ":scheme";

    /// <summary>
    /// The HTTP <c>:path</c> pseudo-field name.
    /// </summary>
    public const string PathPseudoFieldName = ":path";

    /// <summary>
    /// The HTTP <c>:authority</c> pseudo-field name.
    /// </summary>
    public const string AuthorityPseudoFieldName = ":authority";

    /// <summary>
    /// The HTTP <c>:status</c> pseudo-field name.
    /// </summary>
    public const string StatusPseudoFieldName = ":status";

    /// <summary>
    /// The number of random bytes in a client handshake nonce.
    /// </summary>
    public const int ClientNonceLength = 16;

    /// <summary>
    /// The number of SHA-1 digest bytes encoded in an accept value.
    /// </summary>
    public const int AcceptDigestLength = 20;

    /// <summary>
    /// The number of bytes in a frame masking key.
    /// </summary>
    public const int MaskingKeyLength = 4;

    /// <summary>
    /// The smallest possible encoded frame header size.
    /// </summary>
    public const int MinimumFrameHeaderLength = 2;

    /// <summary>
    /// The largest possible encoded frame header size.
    /// </summary>
    public const int MaximumFrameHeaderLength = 14;

    /// <summary>
    /// The smallest four-bit frame opcode value.
    /// </summary>
    public const int MinimumOpcode = 0;

    /// <summary>
    /// The largest four-bit frame opcode value.
    /// </summary>
    public const int MaximumOpcode = 15;

    /// <summary>
    /// The largest payload length encoded directly in the base frame header.
    /// </summary>
    public const int BasicPayloadLengthMaximum = 125;

    /// <summary>
    /// The payload-length marker for a following unsigned 16-bit length.
    /// </summary>
    public const int Extended16PayloadLengthMarker = 126;

    /// <summary>
    /// The payload-length marker for a following unsigned 63-bit length.
    /// </summary>
    public const int Extended63PayloadLengthMarker = 127;

    /// <summary>
    /// The largest payload represented by the unsigned 16-bit length form.
    /// </summary>
    public const int Extended16PayloadLengthMaximum = 65535;

    /// <summary>
    /// The largest legal WebSocket payload length.
    /// </summary>
    public const long PayloadLengthMaximum = long.MaxValue;

    /// <summary>
    /// The largest control-frame payload length.
    /// </summary>
    public const int ControlPayloadLengthMaximum = 125;

    /// <summary>
    /// The number of bytes used by a close status code.
    /// </summary>
    public const int CloseCodeLength = 2;

    /// <summary>
    /// The largest UTF-8 close reason that fits in a control frame.
    /// </summary>
    public const int CloseReasonLengthMaximum = 123;
}
