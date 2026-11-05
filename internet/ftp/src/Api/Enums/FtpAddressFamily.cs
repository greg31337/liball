namespace ftp;

/// <summary>
/// Identifies an Internet Protocol address family without exposing platform-specific values.
/// </summary>
public enum FtpAddressFamily
{
    /// <summary>
    /// Does not require a specific address family.
    /// </summary>
    Unspecified = 0,
    /// <summary>
    /// Uses Internet Protocol version 4.
    /// </summary>
    IPv4 = 1,
    /// <summary>
    /// Uses Internet Protocol version 6.
    /// </summary>
    IPv6 = 2
}
