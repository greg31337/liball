namespace http;

/// <summary>
/// Provides setting identifiers from the IANA HTTP/2 Settings Registry snapshot.
/// </summary>
public static class Http2SettingIdentifiers
{
    /// <summary>
    /// The registered <c>Reserved</c> setting identifier.
    /// </summary>
    public const int Reserved = 0x0;

    /// <summary>
    /// The registered <c>HEADER_TABLE_SIZE</c> setting identifier.
    /// </summary>
    public const int HeaderTableSize = 0x1;

    /// <summary>
    /// The registered <c>ENABLE_PUSH</c> setting identifier.
    /// </summary>
    public const int EnablePush = 0x2;

    /// <summary>
    /// The registered <c>MAX_CONCURRENT_STREAMS</c> setting identifier.
    /// </summary>
    public const int MaxConcurrentStreams = 0x3;

    /// <summary>
    /// The registered <c>INITIAL_WINDOW_SIZE</c> setting identifier.
    /// </summary>
    public const int InitialWindowSize = 0x4;

    /// <summary>
    /// The registered <c>MAX_FRAME_SIZE</c> setting identifier.
    /// </summary>
    public const int MaxFrameSize = 0x5;

    /// <summary>
    /// The registered <c>MAX_HEADER_LIST_SIZE</c> setting identifier.
    /// </summary>
    public const int MaxHeaderListSize = 0x6;

    /// <summary>
    /// The registered <c>SETTINGS_ENABLE_CONNECT_PROTOCOL</c> setting identifier.
    /// </summary>
    public const int EnableConnectProtocol = 0x8;

    /// <summary>
    /// The registered <c>SETTINGS_NO_RFC7540_PRIORITIES</c> setting identifier.
    /// </summary>
    public const int NoRfc7540Priorities = 0x9;

    /// <summary>
    /// The registered <c>TLS_RENEG_PERMITTED</c> setting identifier.
    /// </summary>
    public const int TlsRenegPermitted = 0x10;

    /// <summary>
    /// The registered <c>SETTINGS_ENABLE_METADATA</c> setting identifier.
    /// </summary>
    public const int EnableMetadata = 0x4d44;
}
