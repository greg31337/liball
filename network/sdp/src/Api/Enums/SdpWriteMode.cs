namespace sdp;

/// <summary>
/// Specifies how an SDP description is written.
/// </summary>
public enum SdpWriteMode
{
    /// <summary>The mode is unknown or was not specified.</summary>
    Unknown = 0,

    /// <summary>Write normalized SDP using deterministic ordering and formatting.</summary>
    Canonical = 1,

    /// <summary>Re-emit preserved source details when they are available.</summary>
    Lossless = 2
}
