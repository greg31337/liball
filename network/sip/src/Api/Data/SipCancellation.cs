namespace sip;

/// <summary>Holds a cooperative cancellation request.</summary>
public class SipCancellation
{
    /// <summary>Indicates that cancellation was requested.</summary>
    public bool Requested;
    /// <summary>The cancellation identifier.</summary>
    public string? Id;
    /// <summary>The cancellation reason.</summary>
    public string? Reason;
}
