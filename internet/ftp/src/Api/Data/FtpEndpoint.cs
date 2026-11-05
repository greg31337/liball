namespace ftp;

/// <summary>
/// Describes a network endpoint used by an FTP control or data connection.
/// </summary>
public class FtpEndpoint
{
    /// <summary>
    /// The DNS host name or numeric address.
    /// </summary>
    public string Host;

    /// <summary>
    /// The TCP port number.
    /// </summary>
    public int Port;

    /// <summary>
    /// The required network address family.
    /// </summary>
    public FtpAddressFamily AddressFamily;

    /// <summary>
    /// Creates an endpoint with the registered FTP control port and no host.
    /// </summary>
    public FtpEndpoint()
    {
        Host = "";
        Port = FtpPorts.Control;
        AddressFamily = FtpAddressFamily.Unspecified;
    }
}

