using System.Net;
using System.Net.Sockets;

namespace tftp;

/// <summary>
/// Holds the remote and optional local addressing used by a TFTP client.
/// </summary>
public class TftpEndpoint
{
    /// <summary>
    /// The remote host name or textual IP address.
    /// </summary>
    public string Host = "";

    /// <summary>
    /// The remote UDP port.
    /// </summary>
    public int Port = TftpProtocolValues.DefaultPort;

    /// <summary>
    /// The optional local IP address to bind before sending.
    /// </summary>
    public IPAddress? LocalAddress = null;

    /// <summary>
    /// The preferred address family, or <see cref="AddressFamily.Unspecified"/> for no preference.
    /// </summary>
    public AddressFamily AddressFamily = AddressFamily.Unspecified;
}
