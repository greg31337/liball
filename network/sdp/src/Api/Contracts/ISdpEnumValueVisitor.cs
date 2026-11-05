namespace sdp;

/// <summary>
/// Receives strongly typed SDP enumeration values without requiring reflection or boxing.
/// </summary>
public interface ISdpEnumValueVisitor
{
    /// <summary>Visits a parsed connection-address kind.</summary>
    /// <param name="value">The address kind to visit.</param>
    void VisitAddressKind(SdpAddressKind value);

    /// <summary>Visits an attribute syntax kind.</summary>
    /// <param name="value">The attribute kind to visit.</param>
    void VisitAttributeKind(SdpAttributeKind value);

    /// <summary>Visits a timestamp reference-clock source kind.</summary>
    /// <param name="value">The clock source kind to visit.</param>
    void VisitClockSourceKind(SdpClockSourceKind value);

    /// <summary>Visits a codec matching mode.</summary>
    /// <param name="value">The codec match mode to visit.</param>
    void VisitCodecMatchMode(SdpCodecMatchMode value);

    /// <summary>Visits a comparison difference kind.</summary>
    /// <param name="value">The difference kind to visit.</param>
    void VisitDifferenceKind(SdpDifferenceKind value);

    /// <summary>Visits a media direction.</summary>
    /// <param name="value">The direction to visit.</param>
    void VisitDirection(SdpDirection value);

    /// <summary>Visits a TLS or DTLS setup role.</summary>
    /// <param name="value">The setup role to visit.</param>
    void VisitDtlsSetupRole(SdpDtlsSetupRole value);

    /// <summary>Visits a media-group semantic.</summary>
    /// <param name="value">The group semantic to visit.</param>
    void VisitGroupSemantic(SdpGroupSemantic value);

    /// <summary>Visits an ICE candidate type.</summary>
    /// <param name="value">The candidate type to visit.</param>
    void VisitIceCandidateType(SdpIceCandidateType value);

    /// <summary>Visits an ICE TCP candidate type.</summary>
    /// <param name="value">The TCP candidate type to visit.</param>
    void VisitIceTcpType(SdpIceTcpType value);

    /// <summary>Visits an ICE candidate transport protocol.</summary>
    /// <param name="value">The transport protocol to visit.</param>
    void VisitIceTransportProtocol(SdpIceTransportProtocol value);

    /// <summary>Visits an SDP line-ending kind.</summary>
    /// <param name="value">The line ending to visit.</param>
    void VisitLineEnding(SdpLineEnding value);

    /// <summary>Visits a media kind.</summary>
    /// <param name="value">The media kind to visit.</param>
    void VisitMediaKind(SdpMediaKind value);

    /// <summary>Visits an attribute multiplexing category.</summary>
    /// <param name="value">The multiplexing category to visit.</param>
    void VisitMuxCategory(SdpMuxCategory value);

    /// <summary>Visits an offer-and-answer negotiation role.</summary>
    /// <param name="value">The negotiation role to visit.</param>
    void VisitNegotiationRole(SdpNegotiationRole value);

    /// <summary>Visits a parser mode.</summary>
    /// <param name="value">The parse mode to visit.</param>
    void VisitParseMode(SdpParseMode value);

    /// <summary>Visits a media rejection reason.</summary>
    /// <param name="value">The rejection reason to visit.</param>
    void VisitRejectionReason(SdpRejectionReason value);

    /// <summary>Visits an SDP scope.</summary>
    /// <param name="value">The scope to visit.</param>
    void VisitScope(SdpScope value);

    /// <summary>Visits a validation profile.</summary>
    /// <param name="value">The validation profile to visit.</param>
    void VisitValidationProfile(SdpValidationProfile value);

    /// <summary>Visits a validation severity.</summary>
    /// <param name="value">The validation severity to visit.</param>
    void VisitValidationSeverity(SdpValidationSeverity value);

    /// <summary>Visits a writer mode.</summary>
    /// <param name="value">The write mode to visit.</param>
    void VisitWriteMode(SdpWriteMode value);
}
