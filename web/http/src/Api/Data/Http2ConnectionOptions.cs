namespace http;

/// <summary>
/// Represents role metadata used to initialize an HTTP/2 connection.
/// </summary>
public class Http2ConnectionOptions
{
    /// <summary>
    /// The local endpoint role.
    /// </summary>
    public Http2EndpointRole EndpointRole;

    /// <summary>
    /// The local settings advertised to the peer.
    /// </summary>
    public Http2Settings LocalSettings = new Http2Settings();

    /// <summary>
    /// The HPACK encoder options for the connection.
    /// </summary>
    public HpackOptions EncoderOptions = new HpackOptions();

    /// <summary>
    /// The HPACK decoder options for the connection.
    /// </summary>
    public HpackOptions DecoderOptions = new HpackOptions();

    /// <summary>
    /// The shared resource limits applied by frame and connection processing.
    /// </summary>
    public HttpLimits Limits = new HttpLimits();

    /// <summary>
    /// Allows the server endpoint to initiate push when peer settings permit it.
    /// </summary>
    public bool AllowServerPush = true;

    /// <summary>
    /// Preserves unknown extension frames in delivered events.
    /// </summary>
    public bool PreserveUnknownFrames = true;

    /// <summary>
    /// Enables PRIORITY_UPDATE frame processing.
    /// </summary>
    public bool EnablePriorityUpdate = true;

    /// <summary>
    /// Enables ORIGIN frame processing.
    /// </summary>
    /// <remarks>
    /// The transport owner disables this for proxy and non-applicable protocol connections.
    /// This transport-independent component does not identify TLS or negotiated protocols.
    /// </remarks>
    public bool EnableOrigin = true;

    /// <summary>
    /// The canonical initial origin to add when the first applicable ORIGIN frame is processed.
    /// </summary>
    /// <remarks>
    /// The transport owner derives this value from its TLS connection context. A <c>null</c>
    /// value omits the initial origin because this transport-independent component does not
    /// inspect SNI, remote addresses, ports, certificates, or TLS state.
    /// </remarks>
    public string? InitialOrigin;

    /// <summary>
    /// Enables extended CONNECT when local and peer settings permit it.
    /// </summary>
    public bool EnableExtendedConnect = true;

    /// <summary>
    /// The maximum number of closed stream records retained for late-frame classification.
    /// </summary>
    public int MaximumRetainedClosedStreams = 100;
}
