namespace whois;

/// <summary>
/// Provides conservative defaults for WHOIS requests and response processing.
/// </summary>
public static class WhoisDefaults
{
    /// <summary>
    /// The standard TCP port for RFC 3912 WHOIS.
    /// </summary>
    public const int WhoisPort = 43;

    /// <summary>
    /// The IANA-assigned TCP port normally used by RFC 2167 RWhois.
    /// </summary>
    public const int RWhoisPort = 4321;

    /// <summary>
    /// The IANA WHOIS bootstrap server host name.
    /// </summary>
    public const string IanaHost = "whois.iana.org";

    /// <summary>
    /// The default maximum number of encoded query bytes before the terminating CRLF.
    /// </summary>
    public const int MaximumEncodedQueryBytes = 4096;

    /// <summary>
    /// The default maximum number of response bytes retained for one exchange.
    /// </summary>
    public const int MaximumResponseBytes = 4194304;

    /// <summary>
    /// The default maximum number of automatic referral hops.
    /// </summary>
    public const int MaximumReferralHops = 5;

    /// <summary>
    /// The default maximum number of derived response lines.
    /// </summary>
    public const int MaximumLines = 65536;

    /// <summary>
    /// The default maximum number of parsed response fields.
    /// </summary>
    public const int MaximumFields = 32768;

    /// <summary>
    /// The default time allowed for host resolution.
    /// </summary>
    public static readonly System.TimeSpan ResolveTimeout = System.TimeSpan.FromSeconds(10);

    /// <summary>
    /// The default time allowed for each connection attempt.
    /// </summary>
    public static readonly System.TimeSpan ConnectTimeout = System.TimeSpan.FromSeconds(10);

    /// <summary>
    /// The default time allowed to transmit a framed query.
    /// </summary>
    public static readonly System.TimeSpan WriteTimeout = System.TimeSpan.FromSeconds(10);

    /// <summary>
    /// The default maximum idle time between response reads.
    /// </summary>
    public static readonly System.TimeSpan ReadIdleTimeout = System.TimeSpan.FromSeconds(30);

    /// <summary>
    /// The default maximum duration of one complete exchange.
    /// </summary>
    public static readonly System.TimeSpan OverallTimeout = System.TimeSpan.FromSeconds(60);
}
