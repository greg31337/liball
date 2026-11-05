namespace sdp;

/// <summary>
/// Provides queries over the built-in IANA SDP and RTP registry snapshot.
/// </summary>
public interface ISdpRegistry
{
    /// <summary>Determines whether a media token is registered.</summary>
    /// <param name="token">The media token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredMediaType(string token);

    /// <summary>Determines whether an SDP transport-protocol token is registered.</summary>
    /// <param name="token">The transport-protocol token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredTransportProtocol(string token);

    /// <summary>Determines whether an SDP bandwidth-type token is registered.</summary>
    /// <param name="token">The bandwidth-type token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredBandwidthType(string token);

    /// <summary>Determines whether an SDP network-type token is registered.</summary>
    /// <param name="token">The network-type token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredNetworkType(string token);

    /// <summary>Determines whether an SDP address-type token is registered.</summary>
    /// <param name="token">The address-type token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredAddressType(string token);

    /// <summary>Determines whether an obsolete SDP encryption-key method is registered.</summary>
    /// <param name="token">The encryption-key method token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredEncryptionMethod(string token);

    /// <summary>Determines whether an SDP attribute name is registered.</summary>
    /// <param name="name">The attribute name to query.</param>
    /// <returns><c>true</c> when the name is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredAttributeName(string name);

    /// <summary>Determines whether an SDP group semantic is registered.</summary>
    /// <param name="token">The group semantic token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredGroupSemantic(string token);

    /// <summary>Determines whether a static RTP payload type has an assigned encoding.</summary>
    /// <param name="payloadType">The payload type to query.</param>
    /// <returns><c>true</c> when the payload type has a static assignment; otherwise, <c>false</c>.</returns>
    bool IsAssignedStaticPayloadType(int payloadType);

    /// <summary>Determines whether an RTP encoding name is registered for a media type.</summary>
    /// <param name="mediaType">The RTP payload format media type.</param>
    /// <param name="encodingName">The RTP encoding name.</param>
    /// <returns><c>true</c> when the media and encoding pair is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredRtpEncoding(string mediaType, string encodingName);

    /// <summary>Determines whether an RTP compact header-extension URI is registered.</summary>
    /// <param name="uri">The extension URI to query.</param>
    /// <returns><c>true</c> when the URI is registered; otherwise, <c>false</c>.</returns>
    bool IsRegisteredHeaderExtensionUri(string uri);

    /// <summary>Gets registry metadata for a media token.</summary>
    /// <param name="token">The media token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetMediaTypeRegistration(string token);

    /// <summary>Gets registry metadata for a transport-protocol token.</summary>
    /// <param name="token">The transport-protocol token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetTransportProtocolRegistration(string token);

    /// <summary>Gets registry metadata for a bandwidth-type token.</summary>
    /// <param name="token">The bandwidth-type token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetBandwidthTypeRegistration(string token);

    /// <summary>Gets registry metadata for a network-type token.</summary>
    /// <param name="token">The network-type token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetNetworkTypeRegistration(string token);

    /// <summary>Gets registry metadata for an address-type token.</summary>
    /// <param name="token">The address-type token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetAddressTypeRegistration(string token);

    /// <summary>Gets registry metadata for an obsolete encryption-key method.</summary>
    /// <param name="token">The encryption-key method token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetEncryptionMethodRegistration(string token);

    /// <summary>Gets registry metadata for an SDP attribute name.</summary>
    /// <param name="name">The attribute name to query.</param>
    /// <returns>An attribute registration projection that explicitly indicates whether the name was found.</returns>
    SdpAttributeRegistration GetAttributeRegistration(string name);

    /// <summary>Gets registry metadata for a media-group semantic.</summary>
    /// <param name="token">The group semantic token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    SdpRegisteredToken GetGroupSemanticRegistration(string token);

    /// <summary>Gets registry metadata for an RTP payload type or registered range.</summary>
    /// <param name="payloadType">The RTP payload type to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the identifier was found.</returns>
    SdpRegisteredToken GetRtpPayloadTypeRegistration(int payloadType);

    /// <summary>Gets registry metadata for an RTP encoding name and media type.</summary>
    /// <param name="mediaType">The RTP payload format media type.</param>
    /// <param name="encodingName">The RTP encoding name.</param>
    /// <returns>A registration projection that explicitly indicates whether the encoding was found.</returns>
    SdpRegisteredToken GetRtpEncodingRegistration(string mediaType, string encodingName);

    /// <summary>Gets registry metadata for an RTP compact header-extension URI.</summary>
    /// <param name="uri">The extension URI to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the URI was found.</returns>
    SdpRegisteredToken GetHeaderExtensionRegistration(string uri);
}
