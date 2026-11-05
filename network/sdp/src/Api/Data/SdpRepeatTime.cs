using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Represents an SDP recurrence attached to a timing period.
/// </summary>
public class SdpRepeatTime
{
    /// <summary>Contains the interval between recurrence starts.</summary>
    public SdpTypedTime RepeatInterval = new SdpTypedTime();

    /// <summary>Contains the duration for which each recurrence remains active.</summary>
    public SdpTypedTime ActiveDuration = new SdpTypedTime();

    /// <summary>Contains the ordered offsets from the recurrence start.</summary>
    public List<SdpTypedTime> Offsets = new List<SdpTypedTime>();

    /// <summary>Contains the original text following <c>r=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the recurrence field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
