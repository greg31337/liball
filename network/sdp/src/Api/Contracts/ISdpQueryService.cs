using System.Collections.Generic;

namespace sdp;

/// <summary>Looks up SDP media and projects effective inherited and transport values.</summary>
public interface ISdpQueryService
{
    /// <summary>Finds a media description by zero-based index.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>The stored media description, or <c>null</c> when the index is outside the collection.</returns>
    SdpMediaDescription? FindMediaByIndex(SdpSessionDescription description, int mediaIndex);

    /// <summary>Finds the first media description with an exact valid MID.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaId">The exact media identifier.</param>
    /// <returns>The stored media description, or <c>null</c> when no media section matches.</returns>
    SdpMediaDescription? FindMediaByMid(SdpSessionDescription description, string mediaId);

    /// <summary>Finds the zero-based index of the first media description with an exact valid MID.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaId">The exact media identifier.</param>
    /// <returns>The matching zero-based index, or <c>-1</c> when no media section matches.</returns>
    int FindMediaIndexByMid(SdpSessionDescription description, string mediaId);

    /// <summary>Resolves connection data for the effective media transport.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective connection, or <c>null</c> when none is available.</returns>
    SdpConnectionData? ResolveEffectiveConnection(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective bandwidth values with media types overriding equivalent session types.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective bandwidth values in deterministic scope order.</returns>
    List<SdpBandwidth> ResolveEffectiveBandwidths(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves media direction using media override, session default, and send-receive fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>The effective direction, including inactive for a rejected media section.</returns>
    SdpDirection ResolveEffectiveDirection(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves media-language capabilities using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Effective valid language tags in preference order.</returns>
    List<string> ResolveEffectiveLanguages(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves SDP-description languages using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Effective valid SDP-description language tags in source order.</returns>
    List<string> ResolveEffectiveDescriptionLanguages(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective ICE signaling from the BUNDLE transport owner when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective ICE projection.</returns>
    SdpIceDescription ResolveEffectiveIce(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective TLS and DTLS signaling from the BUNDLE transport owner when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective TLS and DTLS projection.</returns>
    SdpDtlsDescription ResolveEffectiveDtls(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective session- or media-level RTP header-extension mappings.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective mappings in source order.</returns>
    List<SdpExtMapAttribute> ResolveEffectiveExtMaps(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves source filters that apply to the effective media connection.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective source filters in source order.</returns>
    List<SdpSourceFilterAttribute> ResolveEffectiveSourceFilters(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective timestamp reference clocks using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective timestamp reference clocks in source order.</returns>
    List<SdpClockReferenceAttribute> ResolveEffectiveClockReferences(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective media clocks using media override and session fallback semantics.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>Independent effective media clocks in source order.</returns>
    List<SdpMediaClockAttribute> ResolveEffectiveMediaClocks(SdpSessionDescription description, int mediaIndex);

    /// <summary>Projects effective inherited values and the resolved transport endpoint for a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>An independent effective-media projection.</returns>
    SdpEffectiveMedia ResolveEffectiveMedia(SdpSessionDescription description, int mediaIndex);

    /// <summary>Projects effective inherited values for the first media section with an exact valid MID.</summary>
    /// <param name="description">The session description to query.</param>
    /// <param name="mediaId">The exact media identifier.</param>
    /// <returns>An independent effective-media projection, or <c>null</c> when no media section matches.</returns>
    SdpEffectiveMedia? ResolveEffectiveMediaByMid(SdpSessionDescription description, string mediaId);
}
