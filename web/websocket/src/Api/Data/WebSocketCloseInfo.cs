namespace websocket;

/// <summary>
/// Holds local, remote, or combined WebSocket close-handshake details.
/// </summary>
public class WebSocketCloseInfo
{
    /// <summary>
    /// Indicates whether <see cref="Code"/> contains a close code.
    /// </summary>
    public bool HasCode;

    /// <summary>
    /// Holds the raw close code when <see cref="HasCode"/> is true.
    /// </summary>
    public int Code;

    /// <summary>
    /// Indicates whether <see cref="KnownCode"/> identifies the registered meaning of <see cref="Code"/>.
    /// </summary>
    public bool HasKnownCode;

    /// <summary>
    /// Identifies a known close code when <see cref="HasKnownCode"/> is true.
    /// </summary>
    public WebSocketCloseCode KnownCode;

    /// <summary>
    /// Indicates whether <see cref="Reason"/> was present after a close code.
    /// </summary>
    public bool HasReason;

    /// <summary>
    /// Holds the decoded UTF-8 reason when <see cref="HasReason"/> is true.
    /// </summary>
    public string Reason = "";

    /// <summary>
    /// Indicates that the local endpoint initiated closing.
    /// </summary>
    public bool InitiatedLocally;

    /// <summary>
    /// Indicates that a Close frame was sent.
    /// </summary>
    public bool Sent;

    /// <summary>
    /// Indicates that a Close frame was received.
    /// </summary>
    public bool Received;

    /// <summary>
    /// Indicates that both Close frames and the required transport termination completed cleanly.
    /// </summary>
    public bool Clean;
}
