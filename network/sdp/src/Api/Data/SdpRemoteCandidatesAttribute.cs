using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed SDP remote-candidates attribute.</summary>
public class SdpRemoteCandidatesAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains remote candidate tuples in source order.</summary>
    public List<SdpRemoteCandidate> Candidates = new List<SdpRemoteCandidate>();
}
