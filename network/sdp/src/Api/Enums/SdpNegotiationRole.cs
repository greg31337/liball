namespace sdp;

/// <summary>
/// Identifies an endpoint's role in SDP offer and answer negotiation.
/// </summary>
public enum SdpNegotiationRole
{
    /// <summary>The negotiation role is unknown or was not specified.</summary>
    Unknown = 0,

    /// <summary>The endpoint creates the offer.</summary>
    Offerer = 1,

    /// <summary>The endpoint creates the answer.</summary>
    Answerer = 2
}
