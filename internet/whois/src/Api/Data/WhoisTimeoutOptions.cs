namespace whois;

/// <summary>
/// Contains stage and overall timeout limits for one WHOIS exchange.
/// </summary>
public class WhoisTimeoutOptions
{
    /// <summary>
    /// Contains the maximum duration of host resolution.
    /// </summary>
    public System.TimeSpan Resolve = WhoisDefaults.ResolveTimeout;

    /// <summary>
    /// Contains the maximum duration of each connection attempt.
    /// </summary>
    public System.TimeSpan Connect = WhoisDefaults.ConnectTimeout;

    /// <summary>
    /// Contains the maximum duration of query transmission.
    /// </summary>
    public System.TimeSpan Write = WhoisDefaults.WriteTimeout;

    /// <summary>
    /// Contains the maximum idle duration between response reads.
    /// </summary>
    public System.TimeSpan ReadIdle = WhoisDefaults.ReadIdleTimeout;

    /// <summary>
    /// Contains the maximum duration of the complete exchange.
    /// </summary>
    public System.TimeSpan Overall = WhoisDefaults.OverallTimeout;
}
