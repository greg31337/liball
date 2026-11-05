namespace whois;

/// <summary>
/// Contains the status-bearing result of opening an approved WHOIS connection.
/// </summary>
public class WhoisConnection
{
    /// <summary>
    /// Contains the approved IP address used by the connection.
    /// </summary>
    public System.Net.IPAddress? ApprovedAddress = null;

    /// <summary>
    /// Contains the owned connected stream, or <c>null</c> when connection failed.
    /// </summary>
    public System.IO.Stream? Stream = null;

    /// <summary>
    /// Describes how connection establishment completed.
    /// </summary>
    public WhoisExchangeStatus Status = WhoisExchangeStatus.ConnectFailed;

    /// <summary>
    /// Contains the elapsed connection duration.
    /// </summary>
    public System.TimeSpan Duration = System.TimeSpan.Zero;

    /// <summary>
    /// Contains the underlying connection error, or <c>null</c> when none occurred.
    /// </summary>
    public System.Exception? Error = null;
}
