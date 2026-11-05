using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves and queries effective RTP header-extension mappings for SDP media.</summary>
public class SdpExtMapService : ISdpExtMapService
{
    /// <summary>Initializes an SDP RTP header-extension mapping service.</summary>
    public SdpExtMapService()
    {
    }

    /// <summary>Resolves effective session- or media-level RTP header-extension mappings in source order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent typed mappings with implicit directions resolved for the selected media.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpExtMapAttribute> ResolveMediaExtMaps(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpExtMapServiceCore.ResolveMediaExtMaps(description, mediaIndex);
    }

    /// <summary>Determines whether mixed one-byte and two-byte RTP header extensions are enabled for a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns><c>true</c> when a valid session- or media-level marker enables mixed header forms.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public bool ResolveMediaExtMapAllowMixed(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpExtMapServiceCore.ResolveMediaExtMapAllowMixed(description, mediaIndex);
    }

    /// <summary>Finds the extension URI assigned to an identifier in the effective media mappings.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="id">The extension identifier to find.</param>
    /// <returns>The first matching URI in source order, or <c>null</c> when no mapping matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public string? FindUriById(SdpSessionDescription description, int mediaIndex, int id)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpExtMapServiceCore.FindUriById(description, mediaIndex, id);
    }

    /// <summary>Finds the identifier assigned to an extension URI in the effective media mappings.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="uri">The exact extension URI to find.</param>
    /// <returns>The first matching identifier in source order, or <c>-1</c> when no mapping matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or URI argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public int FindIdByUri(SdpSessionDescription description, int mediaIndex, string uri)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }
        return SdpExtMapServiceCore.FindIdByUri(description, mediaIndex, uri);
    }

    /// <summary>Intersects offered mappings with local header-extension capabilities in offer order.</summary>
    /// <param name="offeredMappings">The effective mappings from the remote offer.</param>
    /// <param name="localCapabilities">The locally supported header extensions.</param>
    /// <returns>Independent answer mappings with compatible answer directions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either collection contains a null entry.</exception>
    public List<SdpExtMapAttribute> IntersectExtMaps(List<SdpExtMapAttribute> offeredMappings, List<SdpHeaderExtension> localCapabilities)
    {
        ValidateMappingCollection(offeredMappings, "offeredMappings");
        ValidateCapabilityCollection(localCapabilities);
        return SdpExtMapServiceCore.IntersectExtMaps(offeredMappings, localCapabilities);
    }

    /// <summary>Selects the answer direction supported by an offered direction and a local capability direction.</summary>
    /// <param name="offeredDirection">The mapping direction relative to the offerer.</param>
    /// <param name="localDirection">The supported direction relative to the answerer.</param>
    /// <returns>The compatible direction relative to the answerer.</returns>
    /// <exception cref="ArgumentException">Thrown when either direction is unspecified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either direction is not a defined value.</exception>
    public SdpDirection SelectAnswerDirection(SdpDirection offeredDirection, SdpDirection localDirection)
    {
        ValidateDirection(offeredDirection, "offeredDirection");
        ValidateDirection(localDirection, "localDirection");
        return SdpExtMapServiceCore.SelectAnswerDirection(offeredDirection, localDirection);
    }

    /// <summary>Allocates usable identifiers while preserving offered usable assignments.</summary>
    /// <param name="mappings">The selected answer mappings in deterministic order.</param>
    /// <returns>One-to-one offered and negotiated identifier mappings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the mapping collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the collection contains null or invalid mapping data.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an offered identifier is outside its permitted ranges.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when offered identifiers collide or the usable identifier range is exhausted.</exception>
    public List<SdpHeaderExtensionMapping> AllocateExtMapIds(List<SdpExtMapAttribute> mappings)
    {
        ValidateMappingCollection(mappings, "mappings");
        return SdpExtMapServiceCore.AllocateExtMapIds(mappings);
    }

    /// <summary>Applies one identifier assignment consistently to every RTP header extension in a BUNDLE group.</summary>
    /// <param name="description">The session description containing the bundled media sections.</param>
    /// <param name="bundleGroup">The BUNDLE group whose media sections will be updated.</param>
    /// <param name="mappings">The extension-configuration-to-identifier mappings to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description, BUNDLE group, or mapping collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the group cannot be resolved or the identifier mappings are incomplete, invalid, or colliding.</exception>
    public void RemapBundleExtMapIds(SdpSessionDescription description, SdpGroupAttribute bundleGroup, List<SdpHeaderExtensionMapping> mappings)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (bundleGroup == null)
        {
            throw new ArgumentNullException("bundleGroup");
        }
        if (mappings == null)
        {
            throw new ArgumentNullException("mappings");
        }
        List<SdpExtMapAttribute> extensions = SdpExtMapServiceCore.PrepareBundleExtMapRemap(description, bundleGroup, mappings);
        SdpExtMapServiceCore.RemapBundleExtMapIds(extensions, mappings);
    }

    /// <summary>Applies identifier mappings to an effective-media header-extension graph.</summary>
    /// <param name="media">The effective media graph to update.</param>
    /// <param name="mappings">The one-to-one identifier mappings to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when the effective media or mapping collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the graph or identifier mappings are incomplete, invalid, or colliding.</exception>
    public void RemapExtMapIds(SdpEffectiveMedia media, List<SdpHeaderExtensionMapping> mappings)
    {
        if (media == null)
        {
            throw new ArgumentNullException("media");
        }
        if (mappings == null)
        {
            throw new ArgumentNullException("mappings");
        }
        SdpExtMapServiceCore.ValidateExtMapRemap(media, mappings);
        SdpExtMapServiceCore.RemapExtMapIds(media, mappings);
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

    internal void ValidateMappingCollection(List<SdpExtMapAttribute> mappings, string parameterName)
    {
        if (mappings == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index] == null)
            {
                throw new ArgumentException("An extmap collection must not contain null entries.", parameterName);
            }
            index = index + 1;
        }
    }

    internal void ValidateCapabilityCollection(List<SdpHeaderExtension> capabilities)
    {
        if (capabilities == null)
        {
            throw new ArgumentNullException("localCapabilities");
        }
        int index = 0;
        while (index < capabilities.Count)
        {
            if (capabilities[index] == null)
            {
                throw new ArgumentException("A header-extension capability collection must not contain null entries.", "localCapabilities");
            }
            index = index + 1;
        }
    }

    internal void ValidateDirection(SdpDirection direction, string parameterName)
    {
        if (direction == SdpDirection.Unknown)
        {
            throw new ArgumentException("A defined SDP direction is required.", parameterName);
        }
        if (direction != SdpDirection.SendReceive && direction != SdpDirection.SendOnly && direction != SdpDirection.ReceiveOnly && direction != SdpDirection.Inactive)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
