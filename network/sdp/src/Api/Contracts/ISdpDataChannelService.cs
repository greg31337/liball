using System.Collections.Generic;

namespace sdp;

/// <summary>Projects SCTP, data-channel, and BFCP application signaling.</summary>
public interface ISdpDataChannelService
{
    /// <summary>Resolves SCTP signaling for one media description.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent SCTP projection with effective maximum-message-size semantics.</returns>
    SdpSctpDescription ResolveMediaSctp(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves data-channel mappings and their referenced subprotocol attributes.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent data-channel projections in mapping source order.</returns>
    List<SdpDataChannelAttribute> ResolveDataChannels(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves BFCP signaling for one media description using current BFCP transports.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent BFCP projection, or <c>null</c> when the media transport is not a current BFCP transport.</returns>
    SdpBfcpDescription? ResolveMediaBfcp(SdpSessionDescription description, int mediaIndex);

    /// <summary>Intersects two SCTP signaling capabilities.</summary>
    /// <param name="remoteCapability">The remote SCTP signaling capability.</param>
    /// <param name="localCapability">The local SCTP signaling capability.</param>
    /// <returns>An independent compatible capability, or <c>null</c> when the application formats differ.</returns>
    SdpSctpDescription? IntersectSctpCapabilities(SdpSctpDescription remoteCapability, SdpSctpDescription localCapability);

    /// <summary>Intersects offered data channels with supported subprotocol names.</summary>
    /// <param name="offeredChannels">The offered channels in signaling order.</param>
    /// <param name="supportedSubprotocols">The exact supported subprotocol names, including an empty string for channels without a subprotocol.</param>
    /// <returns>Independent matching channels in offered order.</returns>
    List<SdpDataChannelAttribute> IntersectDataChannelCapabilities(List<SdpDataChannelAttribute> offeredChannels, List<string> supportedSubprotocols);
}
