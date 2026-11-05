namespace uri;

/// <summary>
/// Stores the lossless subcomponents of a URI authority.
/// </summary>
public class UriAuthority
{
    /// <summary>Whether the authority contains a user-information delimiter.</summary>
    public bool HasUserInfo;

    /// <summary>The user-information text without its trailing delimiter.</summary>
    public string UserInfo;

    /// <summary>The parsed host grammar.</summary>
    public UriHostKind HostKind;

    /// <summary>The original host text without IP-literal brackets.</summary>
    public string Host;

    /// <summary>The parsed address octets for IPv4 or IPv6, or an empty array for other host kinds.</summary>
    public byte[] AddressBytes;

    /// <summary>Whether the authority contains a port delimiter.</summary>
    public bool HasPort;

    /// <summary>The port text without its leading delimiter.</summary>
    public string Port;

    /// <summary>Initializes an authority with all optional subcomponents absent.</summary>
    public UriAuthority()
    {
        HasUserInfo = false;
        UserInfo = "";
        HostKind = UriHostKind.Absent;
        Host = "";
        AddressBytes = new byte[0];
        HasPort = false;
        Port = "";
    }
}
