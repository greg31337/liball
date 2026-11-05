namespace sip;

/// <summary>
/// Holds one parsed Via header field value.
/// </summary>
public class SipViaValue
{
    /// <summary>The sent protocol name.</summary>
    public string? ProtocolName;

    /// <summary>The sent protocol version.</summary>
    public string? ProtocolVersion;

    /// <summary>The transport token with its original spelling.</summary>
    public string? Transport;

    /// <summary>The sent-by host without IPv6 brackets.</summary>
    public string? Host;

    /// <summary>The sent-by port, or negative one when absent.</summary>
    public int Port = -1;

    /// <summary>The ordered Via parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];

    /// <summary>The branch parameter value.</summary>
    public string? Branch;

    /// <summary>The received parameter value.</summary>
    public string? Received;

    /// <summary>The numeric rport value, or negative one when absent or valueless.</summary>
    public int RPort = -1;

    /// <summary>Indicates that a valueless rport parameter was supplied.</summary>
    public bool HasRPort;
}
