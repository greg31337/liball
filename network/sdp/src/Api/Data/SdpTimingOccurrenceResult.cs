using System.Collections.Generic;

namespace sdp;

/// <summary>Contains a bounded chronological set of active SDP timing intervals.</summary>
public class SdpTimingOccurrenceResult
{
    /// <summary>Indicates whether occurrence calculation completed successfully.</summary>
    public bool Success = false;

    /// <summary>Contains intersected active intervals ordered by start time and source position.</summary>
    public List<SdpTimingOccurrence> Occurrences = new List<SdpTimingOccurrence>();

    /// <summary>Indicates that additional matching intervals were omitted because of the result limit.</summary>
    public bool ResultLimitReached = false;

    /// <summary>Contains a stable diagnostic code when calculation fails.</summary>
    public string DiagnosticCode = string.Empty;

    /// <summary>Contains a human-readable explanation when calculation fails.</summary>
    public string Detail = string.Empty;
}
