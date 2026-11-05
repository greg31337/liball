namespace ftp;

/// <summary>
/// Specifies protection for FTP data connections.
/// </summary>
public enum FtpDataProtectionLevel
{
    /// <summary>
    /// Transfers data without TLS protection.
    /// </summary>
    Clear = 0,
    /// <summary>
    /// Transfers data through a TLS-protected connection.
    /// </summary>
    Private = 1
}
