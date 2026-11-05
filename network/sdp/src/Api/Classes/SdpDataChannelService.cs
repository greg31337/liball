using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Projects SCTP, data-channel, and BFCP application signaling.</summary>
public class SdpDataChannelService : ISdpDataChannelService
{
    /// <summary>Initializes an SDP data-channel service.</summary>
    public SdpDataChannelService()
    {
    }

    /// <summary>Resolves SCTP signaling for one media description.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent SCTP projection with effective maximum-message-size semantics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpSctpDescription ResolveMediaSctp(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpDataChannelServiceCore.ResolveMediaSctp(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Resolves data-channel mappings and their referenced subprotocol attributes.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent data-channel projections in mapping source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpDataChannelAttribute> ResolveDataChannels(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpDataChannelServiceCore.ResolveDataChannels(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Resolves BFCP signaling for one media description using current BFCP transports.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent BFCP projection, or <c>null</c> when the media transport is not a current BFCP transport.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpBfcpDescription? ResolveMediaBfcp(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpDataChannelServiceCore.ResolveMediaBfcp(description, mediaIndex);
    }

    /// <summary>Intersects two SCTP signaling capabilities.</summary>
    /// <param name="remoteCapability">The remote SCTP signaling capability.</param>
    /// <param name="localCapability">The local SCTP signaling capability.</param>
    /// <returns>An independent compatible capability, or <c>null</c> when the application formats differ.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either capability is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a capability contains a negative message size.</exception>
    public SdpSctpDescription? IntersectSctpCapabilities(SdpSctpDescription remoteCapability, SdpSctpDescription localCapability)
    {
        ValidateSctpCapability(remoteCapability, "remoteCapability");
        ValidateSctpCapability(localCapability, "localCapability");
        return SdpDataChannelServiceCore.IntersectSctpCapabilities(remoteCapability, localCapability);
    }

    /// <summary>Intersects offered data channels with supported subprotocol names.</summary>
    /// <param name="offeredChannels">The offered channels in signaling order.</param>
    /// <param name="supportedSubprotocols">The exact supported subprotocol names, including an empty string for channels without a subprotocol.</param>
    /// <returns>Independent matching channels in offered order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either collection contains a null entry.</exception>
    public List<SdpDataChannelAttribute> IntersectDataChannelCapabilities(List<SdpDataChannelAttribute> offeredChannels, List<string> supportedSubprotocols)
    {
        ValidateChannelCollection(offeredChannels);
        ValidateSubprotocolCollection(supportedSubprotocols);
        return SdpDataChannelServiceCore.IntersectDataChannelCapabilities(offeredChannels, supportedSubprotocols);
    }

    internal void ValidateMediaSelection(SdpSessionDescription description, int mediaIndex)
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

    internal void ValidateSctpCapability(SdpSctpDescription capability, string parameterName)
    {
        if (capability == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        if (capability.MaximumMessageSize < 0)
        {
            throw new ArgumentException("SCTP capability message sizes must be nonnegative.", parameterName);
        }
        if (capability.Format == null)
        {
            throw new ArgumentException("SCTP capability formats must not be null.", parameterName);
        }
    }

    internal void ValidateChannelCollection(List<SdpDataChannelAttribute> channels)
    {
        if (channels == null)
        {
            throw new ArgumentNullException("offeredChannels");
        }
        int index = 0;
        while (index < channels.Count)
        {
            SdpDataChannelAttribute channel = channels[index];
            if (channel == null)
            {
                throw new ArgumentException("The offered channel collection must not contain null entries.", "offeredChannels");
            }
            if (channel.Parameters == null || channel.SubprotocolAttributes == null)
            {
                throw new ArgumentException("Offered channel collections must be initialized.", "offeredChannels");
            }
            int parameterIndex = 0;
            while (parameterIndex < channel.Parameters.Count)
            {
                SdpDataChannelParameter parameter = channel.Parameters[parameterIndex];
                if (parameter == null || parameter.Name == null || parameter.Value == null || parameter.RawText == null)
                {
                    throw new ArgumentException("Offered channel parameters must be complete.", "offeredChannels");
                }
                parameterIndex = parameterIndex + 1;
            }
            int attributeIndex = 0;
            while (attributeIndex < channel.SubprotocolAttributes.Count)
            {
                SdpAttribute attribute = channel.SubprotocolAttributes[attributeIndex];
                if (attribute == null || attribute.Name == null || attribute.Value == null || attribute.RawText == null || attribute.Location == null)
                {
                    throw new ArgumentException("Offered channel subprotocol attributes must be complete.", "offeredChannels");
                }
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
    }

    internal void ValidateSubprotocolCollection(List<string> subprotocols)
    {
        if (subprotocols == null)
        {
            throw new ArgumentNullException("supportedSubprotocols");
        }
        int index = 0;
        while (index < subprotocols.Count)
        {
            if (subprotocols[index] == null)
            {
                throw new ArgumentException("The supported subprotocol collection must not contain null entries.", "supportedSubprotocols");
            }
            index = index + 1;
        }
    }
}
