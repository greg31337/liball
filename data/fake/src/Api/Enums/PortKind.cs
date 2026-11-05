namespace fake;

/// <summary>
/// Selects an IANA transport port-number range.
/// </summary>
public enum PortKind
{
    /// <summary>
    /// Uses the system or well-known range from 0 through 1023.
    /// </summary>
    System = 0,

    /// <summary>
    /// Uses the user or registered range from 1024 through 49151.
    /// </summary>
    Registered = 1,

    /// <summary>
    /// Uses the dynamic or private range from 49152 through 65535.
    /// </summary>
    Dynamic = 2
}
