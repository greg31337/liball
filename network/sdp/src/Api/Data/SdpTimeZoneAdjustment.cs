namespace sdp;

/// <summary>
/// Represents one SDP time-zone adjustment pair.
/// </summary>
public class SdpTimeZoneAdjustment
{
    /// <summary>Contains the lossless decimal NTP time at which the adjustment applies.</summary>
    public string AdjustmentTime = string.Empty;

    /// <summary>Contains the signed offset from the session's base time.</summary>
    public SdpTypedTime Offset = new SdpTypedTime();

    /// <summary>Identifies the adjustment pair's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
