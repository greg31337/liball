namespace ftp;

/// <summary>
/// Specifies how FTP data connections are established.
/// </summary>
public enum FtpDataConnectionMode
{
    /// <summary>
    /// Selects passive or active operation according to capabilities and policy.
    /// </summary>
    Automatic = 0,
    /// <summary>
    /// Uses a server-listening passive data connection.
    /// </summary>
    Passive = 1,
    /// <summary>
    /// Uses a client-listening active data connection.
    /// </summary>
    Active = 2
}
