namespace websocket;

/// <summary>
/// Defines application subprotocol selection for server handshakes.
/// </summary>
public interface IWebSocketSubprotocolSelector
{
    /// <summary>
    /// Selects one protocol offered by the client and supported by the server.
    /// </summary>
    /// <param name="clientProtocols">Client protocols in offer order.</param>
    /// <param name="serverProtocols">Server protocols in preference order.</param>
    /// <returns>The selected protocol, or an empty string when none is selected.</returns>
    string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols);
}
