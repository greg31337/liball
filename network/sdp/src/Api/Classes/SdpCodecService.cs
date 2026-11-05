using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Projects RTP codec signaling from SDP media descriptions.</summary>
public class SdpCodecService : ISdpCodecService
{
    /// <summary>Initializes an SDP codec service.</summary>
    public SdpCodecService()
    {
    }

    /// <summary>Projects numeric RTP formats and their related media attributes in format order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The projected RTP codecs in media-format order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpCodec> ResolveMediaCodecs(SdpSessionDescription description, int mediaIndex)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
        return SdpCodecServiceCore.ResolveMediaCodecs(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Determines whether a projected codec satisfies a local codec capability under a selected matching policy.</summary>
    /// <param name="codec">The projected codec to compare.</param>
    /// <param name="capability">The local codec capability containing required constraints.</param>
    /// <param name="matchMode">The strict or compatible matching policy.</param>
    /// <returns><c>true</c> when the codec satisfies the capability; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the codec or capability argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the match mode is unspecified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the match mode is not a defined value.</exception>
    public bool IsCodecMatch(SdpCodec codec, SdpCodecCapability capability, SdpCodecMatchMode matchMode)
    {
        if (codec == null)
        {
            throw new ArgumentNullException("codec");
        }
        if (capability == null)
        {
            throw new ArgumentNullException("capability");
        }
        ValidateMatchMode(matchMode);
        return SdpCodecServiceCore.IsCodecMatch(codec, capability, matchMode);
    }

    /// <summary>Orders codecs by matching local preference while retaining unmatched codecs in their original relative order.</summary>
    /// <param name="codecs">The codecs to order without mutating the supplied list.</param>
    /// <param name="capabilities">The local codec capabilities and their preference values.</param>
    /// <param name="matchMode">The strict or compatible matching policy.</param>
    /// <returns>A newly allocated list containing the same codec objects in preference order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either list argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a list contains null or the match mode is unspecified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the match mode is not a defined value.</exception>
    public List<SdpCodec> OrderCodecsByPreference(List<SdpCodec> codecs, List<SdpCodecCapability> capabilities, SdpCodecMatchMode matchMode)
    {
        ValidateCodecCollection(codecs, "codecs");
        ValidateCapabilityCollection(capabilities, "capabilities");
        ValidateMatchMode(matchMode);
        return SdpCodecServiceCore.OrderCodecsByPreference(codecs, capabilities, matchMode);
    }

    /// <summary>Intersects remote codecs with local capabilities while retaining remote format order.</summary>
    /// <param name="remoteCodecs">The remote codecs in media-format order.</param>
    /// <param name="localCapabilities">The locally supported codec capabilities.</param>
    /// <param name="matchMode">The strict or compatible matching policy.</param>
    /// <returns>A newly allocated list containing matching remote codec objects in remote format order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either list argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a list contains null or the match mode is unspecified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the match mode is not a defined value.</exception>
    public List<SdpCodec> IntersectCodecs(List<SdpCodec> remoteCodecs, List<SdpCodecCapability> localCapabilities, SdpCodecMatchMode matchMode)
    {
        ValidateCodecCollection(remoteCodecs, "remoteCodecs");
        ValidateCapabilityCollection(localCapabilities, "localCapabilities");
        ValidateMatchMode(matchMode);
        return SdpCodecServiceCore.IntersectCodecs(remoteCodecs, localCapabilities, matchMode);
    }

    /// <summary>Creates deterministic payload mappings for codecs in format order.</summary>
    /// <param name="codecs">The codecs whose current payload types identify the offered assignments.</param>
    /// <param name="mediaIndex">The zero-based media index recorded in each mapping.</param>
    /// <param name="preserveOfferedPayloadTypes">Indicates that symmetric answer mappings must retain offered assignments.</param>
    /// <returns>Payload mappings in codec format order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the codec list is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is negative or a payload type is outside its supported range.</exception>
    /// <exception cref="ArgumentException">Thrown when the codec list contains null.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when offered assignments collide or the dynamic payload range is exhausted.</exception>
    public List<SdpPayloadMapping> AllocatePayloadTypes(List<SdpCodec> codecs, int mediaIndex, bool preserveOfferedPayloadTypes)
    {
        ValidateCodecCollection(codecs, "codecs");
        if (mediaIndex < 0)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
        return SdpCodecServiceCore.AllocatePayloadTypes(codecs, mediaIndex, preserveOfferedPayloadTypes);
    }

    /// <summary>Applies payload mappings to a complete effective-media codec and stream graph.</summary>
    /// <param name="media">The effective media graph to update.</param>
    /// <param name="mappings">The one-to-one payload mappings to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when the media graph or mapping list is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the graph or mappings are incomplete, invalid, or colliding.</exception>
    public void RemapPayloadTypes(SdpEffectiveMedia media, List<SdpPayloadMapping> mappings)
    {
        if (media == null)
        {
            throw new ArgumentNullException("media");
        }
        if (mappings == null)
        {
            throw new ArgumentNullException("mappings");
        }
        SdpCodecServiceCore.ValidatePayloadRemap(media, mappings);
        SdpCodecServiceCore.RemapPayloadTypes(media, mappings);
    }

    internal void ValidateCodecCollection(List<SdpCodec> codecs, string parameterName)
    {
        if (codecs == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        int index = 0;
        while (index < codecs.Count)
        {
            if (codecs[index] == null)
            {
                throw new ArgumentException("A codec collection must not contain null entries.", parameterName);
            }
            index = index + 1;
        }
    }

    internal void ValidateCapabilityCollection(List<SdpCodecCapability> capabilities, string parameterName)
    {
        if (capabilities == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        int index = 0;
        while (index < capabilities.Count)
        {
            if (capabilities[index] == null)
            {
                throw new ArgumentException("A codec capability collection must not contain null entries.", parameterName);
            }
            index = index + 1;
        }
    }

    internal void ValidateMatchMode(SdpCodecMatchMode matchMode)
    {
        if (matchMode == SdpCodecMatchMode.Unknown)
        {
            throw new ArgumentException("A strict or compatible codec match mode is required.", "matchMode");
        }
        if (matchMode != SdpCodecMatchMode.Strict && matchMode != SdpCodecMatchMode.Compatible)
        {
            throw new ArgumentOutOfRangeException("matchMode");
        }
    }
}
