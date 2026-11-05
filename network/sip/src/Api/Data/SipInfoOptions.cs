namespace sip;

/// <summary>Holds INFO body limits and legacy usage policy.</summary>
public class SipInfoOptions
{
    /// <summary>The maximum accepted INFO body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>Allows INFO requests without an Info-Package header.</summary>
    public bool AllowLegacyInfo;
    /// <summary>The media types allowed for legacy INFO, or an empty array to allow any valid type.</summary>
    public SipMediaType[] LegacyContentTypes = new SipMediaType[0];
}
