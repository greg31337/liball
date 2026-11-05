namespace whois;

/// <summary>
/// Contains the status-bearing result of resolving a WHOIS endpoint host.
/// </summary>
public class WhoisResolution
{
    /// <summary>
    /// Contains resolved addresses in deterministic attempt order.
    /// </summary>
    public System.Net.IPAddress[] Addresses = System.Array.Empty<System.Net.IPAddress>();

    /// <summary>
    /// Describes how host resolution completed.
    /// </summary>
    public WhoisExchangeStatus Status = WhoisExchangeStatus.ResolveFailed;

    /// <summary>
    /// Contains the elapsed resolution duration.
    /// </summary>
    public System.TimeSpan Duration = System.TimeSpan.Zero;

    /// <summary>
    /// Contains the underlying resolution error, or <c>null</c> when none occurred.
    /// </summary>
    public System.Exception? Error = null;
}
