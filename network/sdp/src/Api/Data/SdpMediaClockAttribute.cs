namespace sdp;

/// <summary>Represents a media-clock attribute and optional rational rate.</summary>
public class SdpMediaClockAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the optional media-clock identifier.</summary>
    public string Id = string.Empty;
    /// <summary>Contains the registered or extension media-clock name.</summary>
    public string ClockName = string.Empty;
    /// <summary>Contains the media-clock value.</summary>
    public string ClockValue = string.Empty;
    /// <summary>Indicates whether a rational rate was present.</summary>
    public bool HasRate = false;
    /// <summary>Contains the rational-rate numerator.</summary>
    public long RateNumerator = 0;
    /// <summary>Contains the rational-rate denominator.</summary>
    public long RateDenominator = 1;
}
