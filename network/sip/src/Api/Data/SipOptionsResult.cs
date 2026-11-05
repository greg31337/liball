namespace sip;

/// <summary>Holds an OPTIONS capability query or response result.</summary>
public class SipOptionsResult
{
    /// <summary>Indicates successful capability discovery.</summary>
    public bool Success;
    /// <summary>The generated OPTIONS request.</summary>
    public SipMessage? Request;
    /// <summary>The discovered capabilities.</summary>
    public SipCapabilitySet? Capabilities;
    /// <summary>The OPTIONS response.</summary>
    public SipMessage? Response;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
}
