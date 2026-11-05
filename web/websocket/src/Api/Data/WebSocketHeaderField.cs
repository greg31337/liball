namespace websocket;

/// <summary>
/// Holds one HTTP field name and value.
/// </summary>
/// <remarks>
/// Arrays of fields preserve order and duplicate field names. The strings are immutable and may be retained by consumers.
/// </remarks>
public class WebSocketHeaderField
{
    /// <summary>
    /// Holds the HTTP field name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Holds the HTTP field value without line terminators.
    /// </summary>
    public string Value = "";
}
