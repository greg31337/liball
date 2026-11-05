namespace pop3;

/// <summary>Describes one caller-supplied accepted connection for the POP3 server.</summary>
public class Pop3ServerConnection
{
    /// <summary>The connected transport stream, or <c>null</c> until one is supplied.</summary>
    public System.IO.Stream? Stream;

    /// <summary>Indicates whether the supplied transport is already protected.</summary>
    public bool IsSecure;

    /// <summary>The local endpoint description, or <c>null</c> when unavailable.</summary>
    public string? LocalEndPoint;

    /// <summary>The remote endpoint description, or <c>null</c> when unavailable.</summary>
    public string? RemoteEndPoint;

    /// <summary>Indicates whether the caller retains ownership of the supplied stream.</summary>
    public bool LeaveOpen;
}
