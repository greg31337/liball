using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpQueryServiceCore
{
    internal static SdpMediaDescription? FindMediaByIndex(SdpSessionDescription description, int mediaIndex)
    {
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            return null;
        }
        return description.MediaDescriptions[mediaIndex];
    }

    internal static int FindMediaIndexByMid(SdpSessionDescription description, string mediaId)
    {
        return SdpGroupServiceCore.FindMediaIndex(description, mediaId);
    }

    internal static SdpConnectionData? ResolveEffectiveConnection(SdpSessionDescription description, int mediaIndex)
    {
        int transportMediaIndex = SdpBundleServiceCore.ResolveBundleTaggedMediaIndex(description, mediaIndex);
        SdpMediaDescription transportMedia = description.MediaDescriptions[transportMediaIndex];
        SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(description, transportMedia);
        if (connection == null)
        {
            return null;
        }
        return SdpBundleServiceCore.CopyConnection(connection);
    }

    internal static List<SdpBandwidth> ResolveEffectiveBandwidths(SdpSessionDescription description, int mediaIndex)
    {
        List<SdpBandwidth> mediaBandwidths = description.MediaDescriptions[mediaIndex].Bandwidths;
        SdpBundleTransport? bundleTransport = SdpBundleServiceCore.ResolveBundleTransport(description, mediaIndex);
        if (bundleTransport != null)
        {
            mediaBandwidths = bundleTransport.Bandwidths;
        }

        List<SdpBandwidth> result = new List<SdpBandwidth>();
        int index = 0;
        while (index < description.Bandwidths.Count)
        {
            SdpBandwidth bandwidth = description.Bandwidths[index];
            if (!ContainsBandwidthType(mediaBandwidths, bandwidth.Type))
            {
                result.Add(SdpBundleServiceCore.CopyBundleBandwidth(bandwidth));
            }
            index = index + 1;
        }
        index = 0;
        while (index < mediaBandwidths.Count)
        {
            result.Add(SdpBundleServiceCore.CopyBundleBandwidth(mediaBandwidths[index]));
            index = index + 1;
        }
        return result;
    }

    internal static bool ContainsBandwidthType(List<SdpBandwidth> bandwidths, string type)
    {
        int index = 0;
        while (index < bandwidths.Count)
        {
            if (string.Equals(bandwidths[index].Type, type, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpDirection ResolveEffectiveDirection(SdpSessionDescription description, int mediaIndex)
    {
        return SdpExtMapValidationCore.ReadEffectiveMediaDirection(description, description.MediaDescriptions[mediaIndex]);
    }

    internal static List<string> ResolveEffectiveLanguages(SdpSessionDescription description, int mediaIndex)
    {
        return ResolveEffectiveAttributeValues(description, mediaIndex, SdpAttributeNames.Language);
    }

    internal static List<string> ResolveEffectiveDescriptionLanguages(SdpSessionDescription description, int mediaIndex)
    {
        return ResolveEffectiveAttributeValues(description, mediaIndex, SdpAttributeNames.SessionDescriptionLanguage);
    }

    internal static List<string> ResolveEffectiveAttributeValues(SdpSessionDescription description, int mediaIndex, string attributeName)
    {
        List<string> mediaValues = ReadValidAttributeValues(description.MediaDescriptions[mediaIndex].Attributes, attributeName);
        if (mediaValues.Count > 0)
        {
            return mediaValues;
        }
        return ReadValidAttributeValues(description.Attributes, attributeName);
    }

    internal static List<string> ReadValidAttributeValues(List<SdpAttribute> attributes, string attributeName)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.CommonAttribute != null && decoded.CommonAttribute.Name == attributeName)
                {
                    result.Add(decoded.CommonAttribute.Value);
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static SdpIceDescription ResolveEffectiveIce(SdpSessionDescription description, int mediaIndex)
    {
        int transportMediaIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpIceServiceCore.ResolveMediaIce(description, transportMediaIndex);
    }

    internal static SdpDtlsDescription ResolveEffectiveDtls(SdpSessionDescription description, int mediaIndex)
    {
        int transportMediaIndex = SdpDtlsServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpDtlsServiceCore.ResolveMediaDtls(description, transportMediaIndex);
    }

    internal static List<SdpExtMapAttribute> ResolveEffectiveExtMaps(SdpSessionDescription description, int mediaIndex)
    {
        return SdpExtMapServiceCore.ResolveMediaExtMaps(description, mediaIndex);
    }

    internal static SdpRtcpAttribute? ResolveRtcp(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Rtcp, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtcpAttribute != null)
                {
                    return decoded.RtcpAttribute;
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal static List<SdpRtcpExtendedReportAttribute> ResolveRtcpExtendedReports(SdpSessionDescription description, SdpMediaDescription media)
    {
        List<SdpAttribute> attributes = description.Attributes;
        if (SdpExtMapServiceCore.ContainsAttribute(media.Attributes, SdpAttributeNames.RtcpExtendedReports))
        {
            attributes = media.Attributes;
        }
        List<SdpRtcpExtendedReportAttribute> result = new List<SdpRtcpExtendedReportAttribute>();
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.RtcpExtendedReports, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtcpExtendedReportAttribute != null)
                {
                    result.Add(decoded.RtcpExtendedReportAttribute);
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static SdpEffectiveMedia ResolveEffectiveMedia(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        int transportMediaIndex = SdpBundleServiceCore.ResolveBundleTaggedMediaIndex(description, mediaIndex);
        SdpMediaDescription transportMedia = description.MediaDescriptions[transportMediaIndex];
        SdpEffectiveMedia result = new SdpEffectiveMedia();
        result.MediaIndex = mediaIndex;
        result.TransportMediaIndex = transportMediaIndex;
        SdpMediaIdAttribute? mediaId = SdpGroupServiceCore.ResolveMediaId(media);
        if (mediaId != null)
        {
            result.MediaId = mediaId.Id;
        }
        result.Port = transportMedia.Port;
        result.HasPortCount = transportMedia.HasPortCount;
        result.PortCount = transportMedia.PortCount;
        result.Direction = ResolveEffectiveDirection(description, mediaIndex);
        result.Connection = ResolveEffectiveConnection(description, mediaIndex);
        result.Bandwidths = ResolveEffectiveBandwidths(description, mediaIndex);
        result.Languages = ResolveEffectiveLanguages(description, mediaIndex);
        result.SessionDescriptionLanguages = ResolveEffectiveDescriptionLanguages(description, mediaIndex);
        result.Codecs = SdpCodecServiceCore.ResolveMediaCodecs(media);
        result.Rtcp = ResolveRtcp(transportMedia);
        result.RtcpExtendedReports = ResolveRtcpExtendedReports(description, media);
        result.SourceAttributes = SdpStreamServiceCore.ResolveSourceAttributes(media);
        result.SourceGroups = SdpStreamServiceCore.ResolveSourceGroups(media);
        result.MediaStreamIds = SdpStreamServiceCore.ResolveMediaStreamIds(media);
        if (SdpBundleServiceCore.HasIceSignaling(description, transportMedia))
        {
            result.Ice = ResolveEffectiveIce(description, mediaIndex);
        }
        if (SdpBundleServiceCore.HasDtlsSignaling(description, transportMedia))
        {
            result.Dtls = ResolveEffectiveDtls(description, mediaIndex);
        }
        result.HeaderExtensions = ResolveEffectiveExtMaps(description, mediaIndex);
        result.Rids = SdpStreamServiceCore.ResolveRids(media);
        result.Simulcast = SdpStreamServiceCore.ResolveSimulcast(media);
        result.ImageAttributes = SdpStreamServiceCore.ResolveImageAttributes(media);
        result.SourceFilters = SdpEffectiveValueCore.ResolveSourceFilters(description, mediaIndex);
        result.ClockReferences = SdpEffectiveValueCore.ResolveClockReferences(description, mediaIndex);
        result.MediaClocks = SdpEffectiveValueCore.ResolveMediaClocks(description, mediaIndex);
        if (SdpDataChannelValidationCore.IsModernSctpProtocol(media.Protocol) || SdpDataChannelValidationCore.IsLegacySctpProtocol(media.Protocol))
        {
            result.Sctp = SdpDataChannelServiceCore.ResolveMediaSctp(media);
        }
        result.DataChannels = SdpDataChannelServiceCore.ResolveDataChannels(media);
        result.Bfcp = SdpDataChannelServiceCore.ResolveMediaBfcp(description, mediaIndex);
        result.BundleTransport = SdpBundleServiceCore.ResolveBundleTransport(description, mediaIndex);
        return result;
    }
}
