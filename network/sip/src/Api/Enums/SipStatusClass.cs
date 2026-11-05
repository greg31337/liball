namespace sip;

/// <summary>
/// Identifies the class of a SIP response status code.
/// </summary>
public enum SipStatusClass
{
    /// <summary>
    /// The status code is outside the defined SIP response classes.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A provisional 1xx response.
    /// </summary>
    Provisional = 1,

    /// <summary>
    /// A successful 2xx response.
    /// </summary>
    Successful = 2,

    /// <summary>
    /// A redirection 3xx response.
    /// </summary>
    Redirection = 3,

    /// <summary>
    /// A client-error 4xx response.
    /// </summary>
    ClientError = 4,

    /// <summary>
    /// A server-error 5xx response.
    /// </summary>
    ServerError = 5,

    /// <summary>
    /// A global-failure 6xx response.
    /// </summary>
    GlobalFailure = 6
}
