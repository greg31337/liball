using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpDataChannelServiceCore
{
    internal static SdpSctpDescription ResolveMediaSctp(SdpMediaDescription media)
    {
        return SdpSctpAttributeCodecCore.ResolveMediaSctp(media);
    }

    internal static List<SdpDataChannelAttribute> ResolveDataChannels(SdpMediaDescription media)
    {
        List<SdpDataChannelAttribute> channels = new List<SdpDataChannelAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.DataChannelMap, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.DataChannelAttribute != null)
                {
                    channels.Add(CopyDataChannel(decoded.DataChannelAttribute));
                }
            }
            index = index + 1;
        }

        index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.DataChannelSubprotocol, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.DataChannelSubprotocolAttribute != null)
                {
                    int channelIndex = FindChannelIndex(channels, decoded.DataChannelSubprotocolAttribute.ChannelId);
                    if (channelIndex >= 0)
                    {
                        channels[channelIndex].SubprotocolAttributes.Add(CopyAttribute(decoded.DataChannelSubprotocolAttribute.EmbeddedAttribute));
                    }
                }
            }
            index = index + 1;
        }
        return channels;
    }

    internal static SdpBfcpDescription? ResolveMediaBfcp(SdpSessionDescription description, int mediaIndex)
    {
        return SdpBfcpAttributeCodecCore.ResolveMediaBfcp(description, mediaIndex);
    }

    internal static SdpSctpDescription? IntersectSctpCapabilities(SdpSctpDescription remoteCapability, SdpSctpDescription localCapability)
    {
        if (remoteCapability.Format.Length == 0 || localCapability.Format.Length == 0 || !string.Equals(remoteCapability.Format, localCapability.Format, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        SdpSctpDescription result = CopySctpDescription(remoteCapability);
        long remoteSize = ReadEffectiveMessageSize(remoteCapability);
        long localSize = ReadEffectiveMessageSize(localCapability);
        result.HasMaximumMessageSize = remoteCapability.HasMaximumMessageSize || localCapability.HasMaximumMessageSize;
        result.MaximumMessageSize = IntersectMessageSizes(remoteSize, localSize);
        result.MaximumMessageSizeAttribute = null;
        return result;
    }

    internal static List<SdpDataChannelAttribute> IntersectDataChannelCapabilities(List<SdpDataChannelAttribute> offeredChannels, List<string> supportedSubprotocols)
    {
        List<SdpDataChannelAttribute> result = new List<SdpDataChannelAttribute>();
        int channelIndex = 0;
        while (channelIndex < offeredChannels.Count)
        {
            SdpDataChannelAttribute channel = offeredChannels[channelIndex];
            string subprotocol = ReadSubprotocol(channel);
            if (ContainsSubprotocol(supportedSubprotocols, subprotocol))
            {
                result.Add(CopyDataChannel(channel));
            }
            channelIndex = channelIndex + 1;
        }
        return result;
    }

    internal static long ReadEffectiveMessageSize(SdpSctpDescription capability)
    {
        if (!capability.HasMaximumMessageSize)
        {
            return SdpDataChannelTokens.DefaultMaximumMessageSize;
        }
        return capability.MaximumMessageSize;
    }

    internal static long IntersectMessageSizes(long first, long second)
    {
        if (first == 0)
        {
            return second;
        }
        if (second == 0)
        {
            return first;
        }
        return first < second ? first : second;
    }

    internal static string ReadSubprotocol(SdpDataChannelAttribute channel)
    {
        int index = 0;
        while (index < channel.Parameters.Count)
        {
            SdpDataChannelParameter parameter = channel.Parameters[index];
            if (parameter != null && parameter.HasValue && string.Equals(parameter.Name, SdpDataChannelTokens.Subprotocol, StringComparison.OrdinalIgnoreCase))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static bool ContainsSubprotocol(List<string> supportedSubprotocols, string subprotocol)
    {
        int index = 0;
        while (index < supportedSubprotocols.Count)
        {
            if (string.Equals(supportedSubprotocols[index], subprotocol, StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static int FindChannelIndex(List<SdpDataChannelAttribute> channels, int channelId)
    {
        int index = 0;
        while (index < channels.Count)
        {
            if (channels[index].ChannelId == channelId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static SdpSctpDescription CopySctpDescription(SdpSctpDescription source)
    {
        SdpSctpDescription result = new SdpSctpDescription();
        if (source.PortAttribute != null)
        {
            result.PortAttribute = SdpSctpAttributeCodecCore.CopySctpPort(source.PortAttribute);
        }
        if (source.MaximumMessageSizeAttribute != null)
        {
            result.MaximumMessageSizeAttribute = SdpSctpAttributeCodecCore.CopyMaximumMessageSize(source.MaximumMessageSizeAttribute);
        }
        if (source.SctpMapAttribute != null)
        {
            result.SctpMapAttribute = SdpSctpAttributeCodecCore.CopySctpMap(source.SctpMapAttribute);
        }
        result.Port = source.Port;
        result.HasMaximumMessageSize = source.HasMaximumMessageSize;
        result.MaximumMessageSize = source.MaximumMessageSize;
        result.Format = source.Format;
        result.LegacySctpMap = source.LegacySctpMap;
        result.HasMaximumStreams = source.HasMaximumStreams;
        result.MaximumStreams = source.MaximumStreams;
        return result;
    }

    internal static SdpDataChannelAttribute CopyDataChannel(SdpDataChannelAttribute source)
    {
        SdpDataChannelAttribute result = new SdpDataChannelAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.ChannelId = source.ChannelId;
        int index = 0;
        while (index < source.Parameters.Count)
        {
            if (source.Parameters[index] != null)
            {
                result.Parameters.Add(CopyParameter(source.Parameters[index]));
            }
            index = index + 1;
        }
        index = 0;
        while (index < source.SubprotocolAttributes.Count)
        {
            if (source.SubprotocolAttributes[index] != null)
            {
                result.SubprotocolAttributes.Add(CopyAttribute(source.SubprotocolAttributes[index]));
            }
            index = index + 1;
        }
        return result;
    }

    internal static SdpDataChannelParameter CopyParameter(SdpDataChannelParameter source)
    {
        SdpDataChannelParameter result = new SdpDataChannelParameter();
        result.Name = source.Name;
        result.HasValue = source.HasValue;
        result.Value = source.Value;
        result.RawText = source.RawText;
        return result;
    }

    internal static SdpAttribute CopyAttribute(SdpAttribute source)
    {
        SdpAttribute result = new SdpAttribute();
        result.Kind = source.Kind;
        result.Name = source.Name;
        result.Value = source.Value;
        result.RawText = source.RawText;
        result.Location = SdpDiagnosticBuilder.CopySourceLocation(source.Location);
        return result;
    }
}
