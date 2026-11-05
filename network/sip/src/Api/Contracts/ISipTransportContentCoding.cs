namespace sip;

/// <summary>Encodes and decodes one application-provided transport content coding.</summary>
public interface ISipTransportContentCoding
{
    /// <summary>Returns the case-insensitive content-coding name.</summary>
    /// <returns>The registered coding name.</returns>
    string GetTransportContentCodingName();
    /// <summary>Encodes one complete SIP application message.</summary>
    /// <param name="request">The detached input and compartment context.</param>
    /// <returns>The encoded bytes and updated metadata.</returns>
    SipContentCodingResult EncodeTransportContent(SipContentCodingRequest request);
    /// <summary>Decodes one complete transport content-coding message.</summary>
    /// <param name="request">The detached input and compartment context.</param>
    /// <returns>The decoded SIP bytes and updated metadata.</returns>
    SipContentCodingResult DecodeTransportContent(SipContentCodingRequest request);
    /// <summary>Releases state for one SigComp compartment.</summary>
    /// <param name="compartment">The detached compartment metadata.</param>
    void CloseTransportContentCodingCompartment(SipSigCompCompartment compartment);
}
