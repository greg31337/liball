using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves and queries effective RTP header-extension mappings for SDP media.</summary>
public interface ISdpExtMapService
{
    /// <summary>Resolves effective session- or media-level RTP header-extension mappings in source order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent typed mappings with implicit directions resolved for the selected media.</returns>
    List<SdpExtMapAttribute> ResolveMediaExtMaps(SdpSessionDescription description, int mediaIndex);

    /// <summary>Determines whether mixed one-byte and two-byte RTP header extensions are enabled for a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns><c>true</c> when a valid session- or media-level marker enables mixed header forms.</returns>
    bool ResolveMediaExtMapAllowMixed(SdpSessionDescription description, int mediaIndex);

    /// <summary>Finds the extension URI assigned to an identifier in the effective media mappings.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="id">The extension identifier to find.</param>
    /// <returns>The first matching URI in source order, or <c>null</c> when no mapping matches.</returns>
    string? FindUriById(SdpSessionDescription description, int mediaIndex, int id);

    /// <summary>Finds the identifier assigned to an extension URI in the effective media mappings.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="uri">The exact extension URI to find.</param>
    /// <returns>The first matching identifier in source order, or <c>-1</c> when no mapping matches.</returns>
    int FindIdByUri(SdpSessionDescription description, int mediaIndex, string uri);

    /// <summary>Intersects offered mappings with local header-extension capabilities in offer order.</summary>
    /// <param name="offeredMappings">The effective mappings from the remote offer.</param>
    /// <param name="localCapabilities">The locally supported header extensions.</param>
    /// <returns>Independent answer mappings with compatible answer directions.</returns>
    List<SdpExtMapAttribute> IntersectExtMaps(List<SdpExtMapAttribute> offeredMappings, List<SdpHeaderExtension> localCapabilities);

    /// <summary>Selects the answer direction supported by an offered direction and a local capability direction.</summary>
    /// <param name="offeredDirection">The mapping direction relative to the offerer.</param>
    /// <param name="localDirection">The supported direction relative to the answerer.</param>
    /// <returns>The compatible direction relative to the answerer.</returns>
    SdpDirection SelectAnswerDirection(SdpDirection offeredDirection, SdpDirection localDirection);

    /// <summary>Allocates usable identifiers while preserving offered usable assignments.</summary>
    /// <param name="mappings">The selected answer mappings in deterministic order.</param>
    /// <returns>One-to-one offered and negotiated identifier mappings.</returns>
    List<SdpHeaderExtensionMapping> AllocateExtMapIds(List<SdpExtMapAttribute> mappings);

    /// <summary>Applies one identifier assignment consistently to every RTP header extension in a BUNDLE group.</summary>
    /// <param name="description">The session description containing the bundled media sections.</param>
    /// <param name="bundleGroup">The BUNDLE group whose media sections will be updated.</param>
    /// <param name="mappings">The extension-configuration-to-identifier mappings to apply.</param>
    void RemapBundleExtMapIds(SdpSessionDescription description, SdpGroupAttribute bundleGroup, List<SdpHeaderExtensionMapping> mappings);

    /// <summary>Applies identifier mappings to an effective-media header-extension graph.</summary>
    /// <param name="media">The effective media graph to update.</param>
    /// <param name="mappings">The one-to-one identifier mappings to apply.</param>
    void RemapExtMapIds(SdpEffectiveMedia media, List<SdpHeaderExtensionMapping> mappings);
}
