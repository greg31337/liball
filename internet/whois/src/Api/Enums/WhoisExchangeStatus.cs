namespace whois;

/// <summary>
/// Describes the completion status of a WHOIS wire exchange.
/// </summary>
public enum WhoisExchangeStatus
{
    /// <summary>
    /// The server closed the connection after the complete response was read.
    /// </summary>
    Complete = 0,

    /// <summary>
    /// The caller cancelled the exchange.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// A configured stage or overall timeout expired.
    /// </summary>
    TimedOut = 2,

    /// <summary>
    /// The configured response byte limit was reached.
    /// </summary>
    ResponseLimitReached = 3,

    /// <summary>
    /// Host resolution failed.
    /// </summary>
    ResolveFailed = 4,

    /// <summary>
    /// No eligible resolved address could be connected.
    /// </summary>
    ConnectFailed = 5,

    /// <summary>
    /// Writing the framed query failed.
    /// </summary>
    WriteFailed = 6,

    /// <summary>
    /// Reading the response failed before end of stream.
    /// </summary>
    ReadFailed = 7
}
