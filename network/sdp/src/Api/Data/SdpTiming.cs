using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Represents an SDP active-time period and its attached recurrences.
/// </summary>
public class SdpTiming
{
    /// <summary>Contains the lossless decimal NTP start time.</summary>
    public string StartTime = string.Empty;

    /// <summary>Contains the lossless decimal NTP stop time.</summary>
    public string StopTime = string.Empty;

    /// <summary>Contains recurrence fields attached to this timing period in source order.</summary>
    public List<SdpRepeatTime> RepeatTimes = new List<SdpRepeatTime>();

    /// <summary>Contains the original text following <c>t=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the timing field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
