namespace sdp;

/// <summary>
/// Receives a complete SDP session description for application-defined processing.
/// </summary>
public interface ISdpSessionDescriptionVisitor
{
    /// <summary>Visits a session description.</summary>
    /// <param name="description">The session description to visit.</param>
    void VisitSessionDescription(SdpSessionDescription description);
}
