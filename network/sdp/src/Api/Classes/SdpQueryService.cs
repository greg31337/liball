using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Looks up SDP media and projects effective inherited and transport values.</summary>
public class SdpQueryService : ISdpQueryService
{
    /// <summary>Initializes an SDP query service.</summary>
    public SdpQueryService()
    {
    }

    /// <summary>Finds a media description by zero-based index.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>The stored media description, or <c>null</c> when the index is outside the collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    public SdpMediaDescription? FindMediaByIndex(SdpSessionDescription description, int mediaIndex)
    {
        ValidateDescription(description);
        return SdpQueryServiceCore.FindMediaByIndex(description, mediaIndex);
    }

    /// <summary>Finds the first media description with an exact valid MID.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaId">The exact media identifier.</param>
    /// <returns>The stored media description, or <c>null</c> when no media section matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description or media identifier is null.</exception>
    public SdpMediaDescription? FindMediaByMid(SdpSessionDescription description, string mediaId)
    {
        ValidateDescriptionAndMid(description, mediaId);
        int mediaIndex = SdpQueryServiceCore.FindMediaIndexByMid(description, mediaId);
        return SdpQueryServiceCore.FindMediaByIndex(description, mediaIndex);
    }

    /// <summary>Finds the zero-based index of the first media description with an exact valid MID.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaId">The exact media identifier.</param>
    /// <returns>The matching zero-based index, or <c>-1</c> when no media section matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description or media identifier is null.</exception>
    public int FindMediaIndexByMid(SdpSessionDescription description, string mediaId)
    {
        ValidateDescriptionAndMid(description, mediaId);
        return SdpQueryServiceCore.FindMediaIndexByMid(description, mediaId);
    }

    /// <summary>Resolves connection data for the effective media transport.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective connection, or <c>null</c> when none is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public SdpConnectionData? ResolveEffectiveConnection(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveConnection(description, mediaIndex);
    }

    /// <summary>Resolves effective bandwidth values with media types overriding equivalent session types.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective bandwidth values in deterministic scope order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<SdpBandwidth> ResolveEffectiveBandwidths(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveBandwidths(description, mediaIndex);
    }

    /// <summary>Resolves media direction using media override, session default, and send-receive fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>The effective direction, including inactive for a rejected media section.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public SdpDirection ResolveEffectiveDirection(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveDirection(description, mediaIndex);
    }

    /// <summary>Resolves media-language capabilities using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Effective valid language tags in preference order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<string> ResolveEffectiveLanguages(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveLanguages(description, mediaIndex);
    }

    /// <summary>Resolves SDP-description languages using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Effective valid SDP-description language tags in source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<string> ResolveEffectiveDescriptionLanguages(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveDescriptionLanguages(description, mediaIndex);
    }

    /// <summary>Resolves effective ICE signaling from the BUNDLE transport owner when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective ICE projection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public SdpIceDescription ResolveEffectiveIce(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveIce(description, mediaIndex);
    }

    /// <summary>Resolves effective TLS and DTLS signaling from the BUNDLE transport owner when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective TLS and DTLS projection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public SdpDtlsDescription ResolveEffectiveDtls(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveDtls(description, mediaIndex);
    }

    /// <summary>Resolves effective session- or media-level RTP header-extension mappings.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective mappings in source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<SdpExtMapAttribute> ResolveEffectiveExtMaps(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveExtMaps(description, mediaIndex);
    }

    /// <summary>Resolves source filters that apply to the effective media connection.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective source filters in source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<SdpSourceFilterAttribute> ResolveEffectiveSourceFilters(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpEffectiveValueCore.ResolveSourceFilters(description, mediaIndex);
    }

    /// <summary>Resolves effective timestamp reference clocks using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective timestamp reference clocks in source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<SdpClockReferenceAttribute> ResolveEffectiveClockReferences(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpEffectiveValueCore.ResolveClockReferences(description, mediaIndex);
    }

    /// <summary>Resolves effective media clocks using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective media clocks in source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public List<SdpMediaClockAttribute> ResolveEffectiveMediaClocks(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpEffectiveValueCore.ResolveMediaClocks(description, mediaIndex);
    }

    /// <summary>Projects effective inherited values and the resolved transport endpoint for a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective-media projection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is outside the collection.</exception>
    public SdpEffectiveMedia ResolveEffectiveMedia(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpQueryServiceCore.ResolveEffectiveMedia(description, mediaIndex);
    }

    /// <summary>Projects effective inherited values for the first media section with an exact valid MID.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaId">The exact media identifier.</param>
    /// <returns>An independent effective-media projection, or <c>null</c> when no media section matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description or media identifier is null.</exception>
    public SdpEffectiveMedia? ResolveEffectiveMediaByMid(SdpSessionDescription description, string mediaId)
    {
        ValidateDescriptionAndMid(description, mediaId);
        int mediaIndex = SdpQueryServiceCore.FindMediaIndexByMid(description, mediaId);
        if (mediaIndex < 0)
        {
            return null;
        }
        return SdpQueryServiceCore.ResolveEffectiveMedia(description, mediaIndex);
    }

    internal void ValidateDescription(SdpSessionDescription description)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
    }

    internal void ValidateDescriptionAndMid(SdpSessionDescription description, string mediaId)
    {
        ValidateDescription(description);
        if (mediaId == null)
        {
            throw new ArgumentNullException("mediaId");
        }
    }

    internal void ValidateMediaSelection(SdpSessionDescription description, int mediaIndex)
    {
        ValidateDescription(description);
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
    }
}
