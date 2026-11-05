namespace sdp;

/// <summary>
/// Represents an SDP typed-time value with an optional unit suffix.
/// </summary>
public class SdpTypedTime
{
    /// <summary>Contains the signed numeric component of the typed time.</summary>
    public long Value = 0;

    /// <summary>Contains the unit suffix, or an empty string for seconds.</summary>
    public string Unit = string.Empty;

    /// <summary>Contains the original typed-time token.</summary>
    public string RawValue = string.Empty;
}
