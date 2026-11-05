namespace ftp;

/// <summary>
/// Defines the registered default TCP ports used by FTP and implicit FTPS.
/// </summary>
public static class FtpPorts
{
    /// <summary>
    /// The registered FTP control port.
    /// </summary>
    public const int Control = 21;

    /// <summary>
    /// The registered implicit FTPS control port.
    /// </summary>
    public const int ImplicitTls = 990;
}

