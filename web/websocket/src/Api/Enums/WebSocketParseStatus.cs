namespace websocket;

/// <summary>
/// Identifies the result of an incremental parsing or pumping operation.
/// </summary>
public enum WebSocketParseStatus
{
    /// <summary>
    /// The requested protocol item completed.
    /// </summary>
    Complete = 0,

    /// <summary>
    /// More source bytes are required.
    /// </summary>
    NeedInput = 1,

    /// <summary>
    /// Output must be drained before processing can continue.
    /// </summary>
    NeedOutputCapacity = 2,

    /// <summary>
    /// Events must be drained before processing can continue.
    /// </summary>
    NeedEventCapacity = 3,

    /// <summary>
    /// The input stream has ended.
    /// </summary>
    End = 4,

    /// <summary>
    /// A configured limit rejected the input.
    /// </summary>
    LimitRejected = 5,

    /// <summary>
    /// A protocol rule rejected the input.
    /// </summary>
    ProtocolFailure = 6
}
