namespace http;

/// <summary>
/// Identifies the semantic kind of an HTTP field section or message event.
/// </summary>
public enum HttpMessageKind
{
    /// <summary>
    /// A request message.
    /// </summary>
    Request = 0,

    /// <summary>
    /// A final response message.
    /// </summary>
    Response = 1,

    /// <summary>
    /// An informational response message.
    /// </summary>
    InformationalResponse = 2,

    /// <summary>
    /// A trailing field section.
    /// </summary>
    Trailers = 3
}
