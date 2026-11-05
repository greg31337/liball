namespace sdp;

/// <summary>Resolves effective ICE signaling for SDP media and transport scopes.</summary>
public interface ISdpIceService
{
    /// <summary>Resolves session defaults and media-level ICE signaling without applying BUNDLE transport ownership.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective ICE signaling for the selected media section.</returns>
    SdpIceDescription ResolveMediaIce(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective ICE signaling using the BUNDLE-tagged media section when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective ICE signaling for the selected media transport.</returns>
    SdpIceDescription ResolveEffectiveIce(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves the media section that owns ICE transport signaling for a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The zero-based media-section index that owns the ICE transport.</returns>
    int ResolveIceTransportMediaIndex(SdpSessionDescription description, int mediaIndex);

    /// <summary>Adds a candidate to the effective ICE transport unless an equivalent candidate already exists.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="candidate">The candidate to add.</param>
    /// <returns><c>true</c> when the candidate was added; otherwise, <c>false</c>.</returns>
    bool AddCandidate(SdpSessionDescription description, int mediaIndex, SdpIceCandidate candidate);

    /// <summary>Removes every candidate equivalent to a supplied candidate from the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="candidate">The candidate to remove.</param>
    /// <returns>The number of candidate attributes removed.</returns>
    int RemoveCandidate(SdpSessionDescription description, int mediaIndex, SdpIceCandidate candidate);

    /// <summary>Removes candidates matching optional component, transport, and candidate-type filters.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="component">The component to match, or zero to match every component.</param>
    /// <param name="transportProtocol">The transport to match, or <see cref="SdpIceTransportProtocol.Unknown"/> to match every transport.</param>
    /// <param name="candidateType">The candidate type to match, or <see cref="SdpIceCandidateType.Unknown"/> to match every type.</param>
    /// <returns>The number of candidate attributes removed.</returns>
    int RemoveCandidatesByFilter(SdpSessionDescription description, int mediaIndex, int component, SdpIceTransportProtocol transportProtocol, SdpIceCandidateType candidateType);

    /// <summary>Orders valid candidate attributes by descending priority while preserving the order of equal priorities.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    void SortCandidatesByPriority(SdpSessionDescription description, int mediaIndex);

    /// <summary>Marks candidate gathering complete on the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    void MarkGatheringComplete(SdpSessionDescription description, int mediaIndex);

    /// <summary>Replaces remote-candidate tuples on the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="candidates">The remote candidates to set, or an empty list to clear the attribute.</param>
    void SetRemoteCandidates(SdpSessionDescription description, int mediaIndex, System.Collections.Generic.List<SdpRemoteCandidate> candidates);

    /// <summary>Determines whether both effective ICE credential values changed between two media transports.</summary>
    /// <param name="previousDescription">The earlier session description.</param>
    /// <param name="previousMediaIndex">The zero-based media-section index in the earlier description.</param>
    /// <param name="currentDescription">The later session description.</param>
    /// <param name="currentMediaIndex">The zero-based media-section index in the later description.</param>
    /// <returns><c>true</c> when both complete effective credential values changed; otherwise, <c>false</c>.</returns>
    bool IsIceRestart(SdpSessionDescription previousDescription, int previousMediaIndex, SdpSessionDescription currentDescription, int currentMediaIndex);

    /// <summary>Replaces ICE credentials and clears stale gathering state on the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="credentials">The new ICE credential pair.</param>
    void RestartIce(SdpSessionDescription description, int mediaIndex, SdpIceCredentials credentials);

    /// <summary>Parses an <c>application/trickle-ice-sdpfrag</c> payload.</summary>
    /// <param name="input">The SDP fragment text.</param>
    /// <param name="options">The parsing options and safety limits.</param>
    /// <returns>A parse result containing the fragment model and bounded diagnostics.</returns>
    SdpParseResult ParseTrickleIceFragment(string input, SdpParseOptions options);

    /// <summary>Generates a canonical <c>application/trickle-ice-sdpfrag</c> payload.</summary>
    /// <param name="fragment">The fragment model to generate.</param>
    /// <param name="options">The writing options and safety limits.</param>
    /// <returns>A write result containing fragment text or bounded diagnostics.</returns>
    SdpWriteResult WriteTrickleIceFragment(SdpSessionDescription fragment, SdpWriteOptions options);
}
