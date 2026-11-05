namespace sdp;

/// <summary>Creates and negotiates SDP offers and answers from explicit endpoint capabilities.</summary>
public interface ISdpOfferAnswerService
{
    /// <summary>Creates an initial offer without mutating the supplied session template or options.</summary>
    /// <param name="sessionTemplate">The session-level description containing the origin, session name, connection, and timing fields.</param>
    /// <param name="options">The negotiation policies and local endpoint capabilities.</param>
    /// <returns>The generated offer and its deterministic per-media mappings.</returns>
    SdpNegotiationResult CreateInitialOffer(SdpSessionDescription sessionTemplate, SdpNegotiationOptions options);

    /// <summary>Creates a subsequent offer while retaining stable negotiation identities from the previous local offer.</summary>
    /// <param name="previousOffer">The previous local offer whose negotiation state should be continued.</param>
    /// <param name="options">The negotiation policies and current local endpoint capabilities.</param>
    /// <returns>The generated subsequent offer and its deterministic per-media mappings.</returns>
    SdpNegotiationResult CreateSubsequentOffer(SdpSessionDescription previousOffer, SdpNegotiationOptions options);

    /// <summary>Creates an answer whose media descriptions correspond positionally to the supplied offer.</summary>
    /// <param name="offer">The remote offer to answer.</param>
    /// <param name="sessionTemplate">The local session-level description containing the origin, session name, connection, and timing fields.</param>
    /// <param name="options">The negotiation policies and local endpoint capabilities.</param>
    /// <returns>The generated answer and the outcome for every offered media description.</returns>
    SdpNegotiationResult CreateAnswer(SdpSessionDescription offer, SdpSessionDescription sessionTemplate, SdpNegotiationOptions options);
}
