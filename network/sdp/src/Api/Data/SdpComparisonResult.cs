using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the path-addressed result of comparing two SDP descriptions.
/// </summary>
public class SdpComparisonResult
{
    /// <summary>Indicates whether the descriptions are equal under the applied options.</summary>
    public bool Equal = false;

    /// <summary>Contains the options applied to the comparison.</summary>
    public SdpComparisonOptions AppliedOptions = new SdpComparisonOptions();

    /// <summary>Contains differences in deterministic path order.</summary>
    public List<SdpDifference> Differences = new List<SdpDifference>();
}
