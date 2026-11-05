namespace http;

/// <summary>
/// Provides HTTP/2 wire constants, protocol defaults, bounds, and frame flags.
/// </summary>
public static class Http2Constants
{
    /// <summary>
    /// The client connection preface sent before the first frame.
    /// </summary>
    public static readonly byte[] ClientConnectionPreface =
    [
        0x50, 0x52, 0x49, 0x20, 0x2a, 0x20, 0x48, 0x54,
        0x54, 0x50, 0x2f, 0x32, 0x2e, 0x30, 0x0d, 0x0a,
        0x0d, 0x0a, 0x53, 0x4d, 0x0d, 0x0a, 0x0d, 0x0a
    ];

    /// <summary>
    /// The number of octets in an HTTP/2 frame header.
    /// </summary>
    public const int FrameHeaderSize = 9;

    /// <summary>
    /// The number of octets in one SETTINGS parameter.
    /// </summary>
    public const int SettingSize = 6;

    /// <summary>
    /// The required PRIORITY frame payload size.
    /// </summary>
    public const int PriorityPayloadSize = 5;

    /// <summary>
    /// The required RST_STREAM frame payload size.
    /// </summary>
    public const int ResetStreamPayloadSize = 4;

    /// <summary>
    /// The required PING frame payload size.
    /// </summary>
    public const int PingPayloadSize = 8;

    /// <summary>
    /// The minimum GOAWAY frame payload size.
    /// </summary>
    public const int GoAwayMinimumPayloadSize = 8;

    /// <summary>
    /// The required WINDOW_UPDATE frame payload size.
    /// </summary>
    public const int WindowUpdatePayloadSize = 4;

    /// <summary>
    /// The default SETTINGS_HEADER_TABLE_SIZE value.
    /// </summary>
    public const int DefaultHeaderTableSize = 4096;

    /// <summary>
    /// The default SETTINGS_ENABLE_PUSH value for a client.
    /// </summary>
    public const int DefaultEnablePush = 1;

    /// <summary>
    /// The default stream flow-control window size.
    /// </summary>
    public const int DefaultInitialWindowSize = 65535;

    /// <summary>
    /// The maximum connection or stream flow-control window size.
    /// </summary>
    public const int MaximumWindowSize = 0x7fffffff;

    /// <summary>
    /// The default maximum frame payload size.
    /// </summary>
    public const int DefaultMaximumFrameSize = 16384;

    /// <summary>
    /// The largest permitted frame payload size.
    /// </summary>
    public const int MaximumFrameSize = 0xffffff;

    /// <summary>
    /// The largest permitted stream identifier.
    /// </summary>
    public const int MaximumStreamIdentifier = 0x7fffffff;

    /// <summary>
    /// The mask for the reserved high bit in stream-related wire values.
    /// </summary>
    public const uint ReservedBitMask = 0x80000000u;

    /// <summary>
    /// The mask for the 31-bit stream identifier or window increment.
    /// </summary>
    public const uint StreamIdentifierMask = 0x7fffffffu;

    /// <summary>
    /// The END_STREAM frame flag.
    /// </summary>
    public const int EndStreamFlag = 0x01;

    /// <summary>
    /// The ACK frame flag used by SETTINGS and PING.
    /// </summary>
    public const int AckFlag = 0x01;

    /// <summary>
    /// The END_HEADERS frame flag.
    /// </summary>
    public const int EndHeadersFlag = 0x04;

    /// <summary>
    /// The PADDED frame flag.
    /// </summary>
    public const int PaddedFlag = 0x08;

    /// <summary>
    /// The PRIORITY flag on a HEADERS frame.
    /// </summary>
    public const int PriorityFlag = 0x20;
}
