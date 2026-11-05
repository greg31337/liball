namespace fix;

/// <summary>Describes one ordered session connection endpoint.</summary>
public class SessionEndpoint
{
    /// <summary>The DNS name or numeric address.</summary>
    public string Host = "";
    /// <summary>The TCP port.</summary>
    public int Port;
    /// <summary>The failover priority, where lower values are attempted first.</summary>
    public int Priority;
    /// <summary>Whether TLS is required for this endpoint.</summary>
    public bool UseTls;
}
