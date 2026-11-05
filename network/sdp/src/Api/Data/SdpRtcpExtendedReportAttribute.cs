using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed RTCP extended-report attribute.</summary>
public class SdpRtcpExtendedReportAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains report block tokens in source order.</summary>
    public List<string> Blocks = new List<string>();
    /// <summary>Contains the original report-block text.</summary>
    public string RawValue = string.Empty;
}
