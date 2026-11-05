namespace sdp;

/// <summary>Represents a timestamp reference-clock attribute.</summary>
public class SdpClockReferenceAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Identifies a recognized clock-source kind.</summary>
    public SdpClockSourceKind SourceKind = SdpClockSourceKind.Unknown;
    /// <summary>Contains the original clock-source token.</summary>
    public string SourceToken = string.Empty;
    /// <summary>Contains extension data associated with the clock source.</summary>
    public string Value = string.Empty;
}
