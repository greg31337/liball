namespace websocket;

/// <summary>
/// Defines application origin authorization for server handshakes.
/// </summary>
public interface IWebSocketOriginPolicy
{
    /// <summary>
    /// Determines whether the Origin represented by a request is allowed.
    /// </summary>
    /// <param name="request">The validated HTTP request metadata.</param>
    /// <returns><c>true</c> when the request Origin is allowed; otherwise <c>false</c>.</returns>
    bool IsOriginAllowed(WebSocketHttpRequest request);
}
