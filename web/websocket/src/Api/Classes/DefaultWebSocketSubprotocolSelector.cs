namespace websocket;

/// <summary>
/// Selects the first mutually supported WebSocket subprotocol in configured preference order.
/// </summary>
public class DefaultWebSocketSubprotocolSelector : IWebSocketSubprotocolSelector
{
    internal bool PreferServerOrder;

    /// <summary>
    /// Initializes a selector with client or server preference ordering.
    /// </summary>
    /// <param name="preferServerOrder">True to use server preference order; false to use client offer order.</param>
    public DefaultWebSocketSubprotocolSelector(bool preferServerOrder)
    {
        PreferServerOrder = preferServerOrder;
    }

    /// <summary>
    /// Selects the first case-sensitive match in the configured preference order.
    /// </summary>
    /// <param name="clientProtocols">Client protocols in offer order.</param>
    /// <param name="serverProtocols">Server protocols in preference order.</param>
    /// <returns>The selected offered protocol, or an empty string when none is selected.</returns>
    public string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols)
    {
        if (PreferServerOrder)
        {
            return WebSocketSubprotocolNegotiator.SelectFirstMatch(serverProtocols, clientProtocols);
        }

        return WebSocketSubprotocolNegotiator.SelectFirstMatch(clientProtocols, serverProtocols);
    }
}
