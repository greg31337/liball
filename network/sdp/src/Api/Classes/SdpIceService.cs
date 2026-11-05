using System;

namespace sdp;

/// <summary>Resolves effective ICE signaling for SDP media and BUNDLE transports.</summary>
public class SdpIceService : ISdpIceService
{
    /// <summary>Initializes an SDP ICE service.</summary>
    public SdpIceService()
    {
    }

    /// <summary>Resolves session defaults and media-level ICE signaling without applying BUNDLE transport ownership.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective ICE signaling for the selected media section.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpIceDescription ResolveMediaIce(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpIceServiceCore.ResolveMediaIce(description, mediaIndex);
    }

    /// <summary>Resolves effective ICE signaling using the BUNDLE-tagged media section when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective ICE signaling for the selected media transport.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpIceDescription ResolveEffectiveIce(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpIceServiceCore.ResolveMediaIce(description, transportMediaIndex);
    }

    /// <summary>Resolves the media section that owns ICE transport signaling for a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The zero-based media-section index that owns the ICE transport.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public int ResolveIceTransportMediaIndex(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
    }

    /// <summary>Adds a candidate to the effective ICE transport unless an equivalent candidate already exists.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="candidate">The candidate to add.</param>
    /// <returns><c>true</c> when the candidate was added; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or candidate argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when the candidate cannot be represented as an SDP candidate attribute.</exception>
    public bool AddCandidate(SdpSessionDescription description, int mediaIndex, SdpIceCandidate candidate)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (candidate == null)
        {
            throw new ArgumentNullException("candidate");
        }
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpIceServiceCore.AddCandidate(description.MediaDescriptions[transportMediaIndex], candidate);
    }

    /// <summary>Removes every candidate equivalent to a supplied candidate from the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="candidate">The candidate to remove.</param>
    /// <returns>The number of candidate attributes removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or candidate argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when the candidate cannot be represented as an SDP candidate attribute.</exception>
    public int RemoveCandidate(SdpSessionDescription description, int mediaIndex, SdpIceCandidate candidate)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (candidate == null)
        {
            throw new ArgumentNullException("candidate");
        }
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpIceServiceCore.RemoveCandidate(description.MediaDescriptions[transportMediaIndex], candidate);
    }

    /// <summary>Removes candidates matching optional component, transport, and candidate-type filters.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="component">The component to match, or zero to match every component.</param>
    /// <param name="transportProtocol">The transport to match, or <see cref="SdpIceTransportProtocol.Unknown"/> to match every transport.</param>
    /// <param name="candidateType">The candidate type to match, or <see cref="SdpIceCandidateType.Unknown"/> to match every type.</param>
    /// <returns>The number of candidate attributes removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a media index or filter value is outside its supported range.</exception>
    public int RemoveCandidatesByFilter(SdpSessionDescription description, int mediaIndex, int component, SdpIceTransportProtocol transportProtocol, SdpIceCandidateType candidateType)
    {
        ValidateResolutionArguments(description, mediaIndex);
        SdpIceServiceCore.ValidateCandidateFilter(component, transportProtocol, candidateType);
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpIceServiceCore.RemoveCandidatesByFilter(description.MediaDescriptions[transportMediaIndex], component, transportProtocol, candidateType);
    }

    /// <summary>Orders valid candidate attributes by descending priority while preserving the order of equal priorities.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public void SortCandidatesByPriority(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpIceServiceCore.SortCandidatesByPriority(description.MediaDescriptions[transportMediaIndex]);
    }

    /// <summary>Marks candidate gathering complete on the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public void MarkGatheringComplete(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpIceServiceCore.MarkGatheringComplete(description.MediaDescriptions[transportMediaIndex]);
    }

    /// <summary>Replaces remote-candidate tuples on the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="candidates">The remote candidates to set, or an empty list to clear the attribute.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or candidates argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when the remote candidates cannot be represented as an SDP attribute.</exception>
    public void SetRemoteCandidates(SdpSessionDescription description, int mediaIndex, System.Collections.Generic.List<SdpRemoteCandidate> candidates)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (candidates == null)
        {
            throw new ArgumentNullException("candidates");
        }
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpIceServiceCore.SetRemoteCandidates(description.MediaDescriptions[transportMediaIndex], candidates);
    }

    /// <summary>Determines whether both effective ICE credential values changed between two media transports.</summary>
    /// <param name="previousDescription">The earlier session description.</param>
    /// <param name="previousMediaIndex">The zero-based media-section index in the earlier description.</param>
    /// <param name="currentDescription">The later session description.</param>
    /// <param name="currentMediaIndex">The zero-based media-section index in the later description.</param>
    /// <returns><c>true</c> when both complete effective credential values changed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either media index does not identify a media section.</exception>
    public bool IsIceRestart(SdpSessionDescription previousDescription, int previousMediaIndex, SdpSessionDescription currentDescription, int currentMediaIndex)
    {
        ValidateResolutionArguments(previousDescription, previousMediaIndex);
        ValidateResolutionArguments(currentDescription, currentMediaIndex);
        int previousTransportIndex = SdpIceServiceCore.ResolveTransportMediaIndex(previousDescription, previousMediaIndex);
        int currentTransportIndex = SdpIceServiceCore.ResolveTransportMediaIndex(currentDescription, currentMediaIndex);
        SdpIceDescription previousIce = SdpIceServiceCore.ResolveMediaIce(previousDescription, previousTransportIndex);
        SdpIceDescription currentIce = SdpIceServiceCore.ResolveMediaIce(currentDescription, currentTransportIndex);
        return SdpIceServiceCore.CredentialsIndicateRestart(previousIce.Credentials, currentIce.Credentials);
    }

    /// <summary>Replaces ICE credentials and clears stale gathering state on the effective ICE transport.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="credentials">The new ICE credential pair.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or credentials argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when credentials are invalid or fail to change both values of an existing complete pair.</exception>
    public void RestartIce(SdpSessionDescription description, int mediaIndex, SdpIceCredentials credentials)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (credentials == null)
        {
            throw new ArgumentNullException("credentials");
        }
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpIceDescription existing = SdpIceServiceCore.ResolveMediaIce(description, transportMediaIndex);
        SdpIceServiceCore.RestartIce(description.MediaDescriptions[transportMediaIndex], existing.Credentials, credentials);
    }

    /// <summary>Parses an <c>application/trickle-ice-sdpfrag</c> payload.</summary>
    /// <param name="input">The SDP fragment text.</param>
    /// <param name="options">The parsing options and safety limits.</param>
    /// <returns>A parse result containing the fragment model and bounded diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parsing options contain an unsupported value.</exception>
    public SdpParseResult ParseTrickleIceFragment(string input, SdpParseOptions options)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpTrickleIceFragmentCore.ParseFragment(input, options);
    }

    /// <summary>Generates a canonical <c>application/trickle-ice-sdpfrag</c> payload.</summary>
    /// <param name="fragment">The fragment model to generate.</param>
    /// <param name="options">The writing options and safety limits.</param>
    /// <returns>A write result containing fragment text or bounded diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the fragment or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when writing options contain an unsupported value.</exception>
    public SdpWriteResult WriteTrickleIceFragment(SdpSessionDescription fragment, SdpWriteOptions options)
    {
        if (fragment == null)
        {
            throw new ArgumentNullException("fragment");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpTrickleIceFragmentCore.WriteFragment(fragment, options);
    }

    internal void ValidateResolutionArguments(SdpSessionDescription description, int mediaIndex)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
    }
}
