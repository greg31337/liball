namespace websocket;

/// <summary>
/// Identifies every four-bit WebSocket frame opcode.
/// </summary>
public enum WebSocketOpcode
{
    /// <summary>
    /// Continues a fragmented data message.
    /// </summary>
    Continuation = 0,

    /// <summary>
    /// Starts or contains a text message.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Starts or contains a binary message.
    /// </summary>
    Binary = 2,

    /// <summary>
    /// Represents the reserved data opcode three.
    /// </summary>
    ReservedData3 = 3,

    /// <summary>
    /// Represents the reserved data opcode four.
    /// </summary>
    ReservedData4 = 4,

    /// <summary>
    /// Represents the reserved data opcode five.
    /// </summary>
    ReservedData5 = 5,

    /// <summary>
    /// Represents the reserved data opcode six.
    /// </summary>
    ReservedData6 = 6,

    /// <summary>
    /// Represents the reserved data opcode seven.
    /// </summary>
    ReservedData7 = 7,

    /// <summary>
    /// Carries a connection close control message.
    /// </summary>
    Close = 8,

    /// <summary>
    /// Carries a Ping control message.
    /// </summary>
    Ping = 9,

    /// <summary>
    /// Carries a Pong control message.
    /// </summary>
    Pong = 10,

    /// <summary>
    /// Represents the reserved control opcode eleven.
    /// </summary>
    ReservedControl11 = 11,

    /// <summary>
    /// Represents the reserved control opcode twelve.
    /// </summary>
    ReservedControl12 = 12,

    /// <summary>
    /// Represents the reserved control opcode thirteen.
    /// </summary>
    ReservedControl13 = 13,

    /// <summary>
    /// Represents the reserved control opcode fourteen.
    /// </summary>
    ReservedControl14 = 14,

    /// <summary>
    /// Represents the reserved control opcode fifteen.
    /// </summary>
    ReservedControl15 = 15
}
