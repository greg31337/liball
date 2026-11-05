namespace fake;

/// <summary>
/// Represents correlated synthetic user-agent metadata and text.
/// </summary>
/// <remarks>
/// Version and device values are fixed fixtures. They do not report current product
/// releases, identify a real installation, or make compatibility claims.
/// </remarks>
public class UserAgentRecord
{
    /// <summary>The browser or synthetic crawler name represented in the user-agent text.</summary>
    public string Browser = string.Empty;

    /// <summary>The fixed fixture version represented in the user-agent text.</summary>
    public string BrowserVersion = string.Empty;

    /// <summary>The operating-system token represented in the user-agent text.</summary>
    public string OperatingSystem = string.Empty;

    /// <summary>The device token represented in the user-agent text.</summary>
    public string Device = string.Empty;

    /// <summary>The complete synthetic user-agent value.</summary>
    public string UserAgent = string.Empty;

    /// <summary>Whether the value represents an explicitly synthetic crawler.</summary>
    public bool IsBot;

    internal UserAgentRecord()
    {
    }
}
