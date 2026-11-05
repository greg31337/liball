using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed RTP format-parameters attribute.</summary>
public class SdpFormatParametersAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the RTP payload type.</summary>
    public int PayloadType = 0;
    /// <summary>Contains parameters in source order, including duplicates.</summary>
    public List<SdpFormatParameter> Parameters = new List<SdpFormatParameter>();
    /// <summary>Contains the unparsed parameter text for opaque preservation.</summary>
    public string RawParameters = string.Empty;
}
