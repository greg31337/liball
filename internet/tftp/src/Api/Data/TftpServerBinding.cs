using System.Net;

namespace tftp;

/// <summary>
/// Holds an IP address and UDP port on which a TFTP server listens.
/// </summary>
public class TftpServerBinding
{
    /// <summary>
    /// The local IP address to bind.
    /// </summary>
    public IPAddress Address = IPAddress.Any;

    /// <summary>
    /// The local UDP port, or zero to request a dynamic port.
    /// </summary>
    public int Port = TftpProtocolValues.DefaultPort;
}
