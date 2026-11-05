namespace websocket;

/// <summary>
/// Holds one ordered WebSocket extension parameter.
/// </summary>
public class WebSocketExtensionParameter
{
    /// <summary>
    /// Holds the extension parameter name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Indicates whether <see cref="Value"/> is present.
    /// </summary>
    public bool HasValue;

    /// <summary>
    /// Holds the decoded parameter value when <see cref="HasValue"/> is true.
    /// </summary>
    public string Value = "";
}
