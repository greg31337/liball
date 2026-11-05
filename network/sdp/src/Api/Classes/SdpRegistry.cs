namespace sdp;

/// <summary>
/// Provides lookup operations over the library's built-in IANA registry snapshot.
/// </summary>
public class SdpRegistry : ISdpRegistry
{
    /// <summary>
    /// Initializes a registry backed by the immutable built-in snapshot.
    /// </summary>
    public SdpRegistry()
    {
    }

    /// <summary>Determines whether a media token is registered.</summary>
    /// <param name="token">The media token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredMediaType(string token)
    {
        return SdpRegistryData.MediaTypes.Contains(token);
    }

    /// <summary>Determines whether an SDP transport-protocol token is registered.</summary>
    /// <param name="token">The transport-protocol token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredTransportProtocol(string token)
    {
        return SdpRegistryData.TransportProtocols.Contains(token);
    }

    /// <summary>Determines whether an SDP bandwidth-type token is registered.</summary>
    /// <param name="token">The bandwidth-type token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredBandwidthType(string token)
    {
        return SdpRegistryData.BandwidthTypes.Contains(token);
    }

    /// <summary>Determines whether an SDP network-type token is registered.</summary>
    /// <param name="token">The network-type token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredNetworkType(string token)
    {
        return SdpRegistryData.NetworkTypes.Contains(token);
    }

    /// <summary>Determines whether an SDP address-type token is registered.</summary>
    /// <param name="token">The address-type token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredAddressType(string token)
    {
        return SdpRegistryData.AddressTypes.Contains(token);
    }

    /// <summary>Determines whether an obsolete SDP encryption-key method is registered.</summary>
    /// <param name="token">The encryption-key method token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredEncryptionMethod(string token)
    {
        return SdpRegistryData.EncryptionMethods.Contains(token);
    }

    /// <summary>Determines whether an SDP attribute name is registered.</summary>
    /// <param name="name">The attribute name to query.</param>
    /// <returns><c>true</c> when the name is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredAttributeName(string name)
    {
        return SdpRegistryData.AttributeNames.Contains(name);
    }

    /// <summary>Determines whether an SDP group semantic is registered.</summary>
    /// <param name="token">The group semantic token to query.</param>
    /// <returns><c>true</c> when the token is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredGroupSemantic(string token)
    {
        return SdpRegistryData.GroupSemantics.Contains(token);
    }

    /// <summary>Determines whether a static RTP payload type has an assigned encoding.</summary>
    /// <param name="payloadType">The payload type to query.</param>
    /// <returns><c>true</c> when the payload type has a static assignment; otherwise, <c>false</c>.</returns>
    public bool IsAssignedStaticPayloadType(int payloadType)
    {
        return SdpRegistryData.AssignedPayloadTypes.Contains(payloadType);
    }

    /// <summary>Determines whether an RTP encoding name is registered for a media type.</summary>
    /// <param name="mediaType">The RTP payload format media type.</param>
    /// <param name="encodingName">The RTP encoding name.</param>
    /// <returns><c>true</c> when the media and encoding pair is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredRtpEncoding(string mediaType, string encodingName)
    {
        string key = SdpRegistryLookup.JoinRtpEncodingKey(mediaType, encodingName);
        return SdpRegistryData.RtpEncodings.Contains(key);
    }

    /// <summary>Determines whether an RTP compact header-extension URI is registered.</summary>
    /// <param name="uri">The extension URI to query.</param>
    /// <returns><c>true</c> when the URI is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegisteredHeaderExtensionUri(string uri)
    {
        return SdpRegistryData.HeaderExtensionUris.Contains(uri);
    }

    /// <summary>Gets registry metadata for a media token.</summary>
    /// <param name="token">The media token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetMediaTypeRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-1", token, true, false);
    }

    /// <summary>Gets registry metadata for a transport-protocol token.</summary>
    /// <param name="token">The transport-protocol token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetTransportProtocolRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-2", token, false, false);
    }

    /// <summary>Gets registry metadata for a bandwidth-type token.</summary>
    /// <param name="token">The bandwidth-type token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetBandwidthTypeRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-3", token, false, false);
    }

    /// <summary>Gets registry metadata for a network-type token.</summary>
    /// <param name="token">The network-type token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetNetworkTypeRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-4", token, false, false);
    }

    /// <summary>Gets registry metadata for an address-type token.</summary>
    /// <param name="token">The address-type token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetAddressTypeRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-5", token, false, false);
    }

    /// <summary>Gets registry metadata for an obsolete encryption-key method.</summary>
    /// <param name="token">The encryption-key method token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetEncryptionMethodRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-6", token, false, true);
    }

    /// <summary>Gets registry metadata for an SDP attribute name.</summary>
    /// <param name="name">The attribute name to query.</param>
    /// <returns>An attribute registration projection that explicitly indicates whether the name was found.</returns>
    public SdpAttributeRegistration GetAttributeRegistration(string name)
    {
        return SdpRegistryLookup.FindAttributeRegistration(name);
    }

    /// <summary>Gets registry metadata for a media-group semantic.</summary>
    /// <param name="token">The group semantic token to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the token was found.</returns>
    public SdpRegisteredToken GetGroupSemanticRegistration(string token)
    {
        return SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-13", token, false, false);
    }

    /// <summary>Gets registry metadata for an RTP payload type or registered range.</summary>
    /// <param name="payloadType">The RTP payload type to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the identifier was found.</returns>
    public SdpRegisteredToken GetRtpPayloadTypeRegistration(int payloadType)
    {
        return SdpRegistryLookup.FindPayloadTypeRegistration(payloadType);
    }

    /// <summary>Gets registry metadata for an RTP encoding name and media type.</summary>
    /// <param name="mediaType">The RTP payload format media type.</param>
    /// <param name="encodingName">The RTP encoding name.</param>
    /// <returns>A registration projection that explicitly indicates whether the encoding was found.</returns>
    public SdpRegisteredToken GetRtpEncodingRegistration(string mediaType, string encodingName)
    {
        return SdpRegistryLookup.FindRtpEncodingRegistration(mediaType, encodingName);
    }

    /// <summary>Gets registry metadata for an RTP compact header-extension URI.</summary>
    /// <param name="uri">The extension URI to query.</param>
    /// <returns>A registration projection that explicitly indicates whether the URI was found.</returns>
    public SdpRegisteredToken GetHeaderExtensionRegistration(string uri)
    {
        return SdpRegistryLookup.FindHeaderExtensionRegistration(uri);
    }
}
