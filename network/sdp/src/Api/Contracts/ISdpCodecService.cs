using System.Collections.Generic;

namespace sdp;

/// <summary>Projects RTP codec signaling from SDP media descriptions.</summary>
public interface ISdpCodecService
{
    /// <summary>Projects numeric RTP formats and their related media attributes in format order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The projected RTP codecs in media-format order.</returns>
    List<SdpCodec> ResolveMediaCodecs(SdpSessionDescription description, int mediaIndex);

    /// <summary>Determines whether a projected codec satisfies a local codec capability under a selected matching policy.</summary>
    /// <param name="codec">The projected codec to compare.</param>
    /// <param name="capability">The local codec capability containing required constraints.</param>
    /// <param name="matchMode">The strict or compatible matching policy.</param>
    /// <returns><c>true</c> when the codec satisfies the capability; otherwise, <c>false</c>.</returns>
    bool IsCodecMatch(SdpCodec codec, SdpCodecCapability capability, SdpCodecMatchMode matchMode);

    /// <summary>Orders codecs by matching local preference while retaining unmatched codecs in their original relative order.</summary>
    /// <param name="codecs">The codecs to order without mutating the supplied list.</param>
    /// <param name="capabilities">The local codec capabilities and their preference values.</param>
    /// <param name="matchMode">The strict or compatible matching policy.</param>
    /// <returns>A newly allocated list containing the same codec objects in preference order.</returns>
    List<SdpCodec> OrderCodecsByPreference(List<SdpCodec> codecs, List<SdpCodecCapability> capabilities, SdpCodecMatchMode matchMode);

    /// <summary>Intersects remote codecs with local capabilities while retaining remote format order.</summary>
    /// <param name="remoteCodecs">The remote codecs in media-format order.</param>
    /// <param name="localCapabilities">The locally supported codec capabilities.</param>
    /// <param name="matchMode">The strict or compatible matching policy.</param>
    /// <returns>A newly allocated list containing matching remote codec objects in remote format order.</returns>
    List<SdpCodec> IntersectCodecs(List<SdpCodec> remoteCodecs, List<SdpCodecCapability> localCapabilities, SdpCodecMatchMode matchMode);

    /// <summary>Creates deterministic payload mappings for codecs in format order.</summary>
    /// <param name="codecs">The codecs whose current payload types identify the offered assignments.</param>
    /// <param name="mediaIndex">The zero-based media index recorded in each mapping.</param>
    /// <param name="preserveOfferedPayloadTypes">Indicates that symmetric answer mappings must retain offered assignments.</param>
    /// <returns>Payload mappings in codec format order.</returns>
    List<SdpPayloadMapping> AllocatePayloadTypes(List<SdpCodec> codecs, int mediaIndex, bool preserveOfferedPayloadTypes);

    /// <summary>Applies payload mappings to a complete effective-media codec and stream graph.</summary>
    /// <param name="media">The effective media graph to update.</param>
    /// <param name="mappings">The one-to-one payload mappings to apply.</param>
    void RemapPayloadTypes(SdpEffectiveMedia media, List<SdpPayloadMapping> mappings);
}
