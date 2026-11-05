namespace sdp;

/// <summary>
/// Describes one path-addressed difference between SDP descriptions.
/// </summary>
public class SdpDifference
{
    /// <summary>Identifies how the compared value differs.</summary>
    public SdpDifferenceKind Kind = SdpDifferenceKind.Unknown;

    /// <summary>Identifies the affected model value.</summary>
    public SdpSourcePath Path = new SdpSourcePath();

    /// <summary>Contains the value from the original description when safe to expose.</summary>
    public string OriginalValue = string.Empty;

    /// <summary>Contains the value from the compared description when safe to expose.</summary>
    public string ComparedValue = string.Empty;

    /// <summary>Indicates that compared values were omitted because they contain sensitive data.</summary>
    public bool ValuesRedacted = false;

    /// <summary>Contains a human-readable description of the difference.</summary>
    public string Detail = string.Empty;
}
