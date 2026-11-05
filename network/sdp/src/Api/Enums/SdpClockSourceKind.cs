namespace sdp;

/// <summary>
/// Identifies a timestamp reference clock source.
/// </summary>
public enum SdpClockSourceKind
{
    /// <summary>The clock source is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The clock source uses the Network Time Protocol.</summary>
    Ntp = 1,

    /// <summary>The clock source uses the Precision Time Protocol.</summary>
    Ptp = 2,

    /// <summary>The clock source is the Global Positioning System.</summary>
    Gps = 3,

    /// <summary>The clock source is the Galileo navigation system.</summary>
    Galileo = 4,

    /// <summary>The clock source is the GLONASS navigation system.</summary>
    Glonass = 5,

    /// <summary>The clock source is the sender's local clock.</summary>
    Local = 6,

    /// <summary>The clock source is a private reference clock.</summary>
    Private = 7
}
