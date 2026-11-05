namespace http;

/// <summary>
/// Identifies the request-target form carried by an HTTP request.
/// </summary>
public enum HttpRequestTargetForm
{
    /// <summary>
    /// The origin-form request target.
    /// </summary>
    Origin = 0,

    /// <summary>
    /// The absolute-form request target.
    /// </summary>
    Absolute = 1,

    /// <summary>
    /// The authority-form request target.
    /// </summary>
    Authority = 2,

    /// <summary>
    /// The asterisk-form request target.
    /// </summary>
    Asterisk = 3
}
