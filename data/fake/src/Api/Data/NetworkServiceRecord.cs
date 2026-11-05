namespace fake;

/// <summary>
/// Represents one assigned IANA service-name, port-number, and transport combination.
/// </summary>
/// <remarks>
/// Registry presence is not evidence that a service is running, reachable, safe,
/// authenticated, or owned by any party.
/// </remarks>
public class NetworkServiceRecord
{
    /// <summary>The registered service name.</summary>
    public string Service = string.Empty;

    /// <summary>The assigned transport port number.</summary>
    public int Port;

    /// <summary>The lowercase transport protocol name.</summary>
    public string Transport = string.Empty;

    /// <summary>The IANA port-number range containing the port.</summary>
    public PortKind PortKind;

    /// <summary>The source-version text of the bundled IANA snapshot.</summary>
    public string RegistryVersion = string.Empty;

    internal NetworkServiceRecord()
    {
    }
}
