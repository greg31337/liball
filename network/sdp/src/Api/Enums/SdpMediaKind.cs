namespace sdp;

/// <summary>
/// Identifies a commonly used SDP media kind.
/// </summary>
public enum SdpMediaKind
{
    /// <summary>The media kind is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The media carries audio.</summary>
    Audio = 1,

    /// <summary>The media carries video.</summary>
    Video = 2,

    /// <summary>The media carries text.</summary>
    Text = 3,

    /// <summary>The media carries application data.</summary>
    Application = 4,

    /// <summary>The media carries a message format.</summary>
    Message = 5
}
