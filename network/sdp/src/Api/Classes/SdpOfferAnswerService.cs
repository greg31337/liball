using System;

namespace sdp;

/// <summary>Creates and negotiates SDP offers and answers from explicit endpoint capabilities.</summary>
public class SdpOfferAnswerService : ISdpOfferAnswerService
{
    /// <summary>Initializes an SDP offer-and-answer service.</summary>
    public SdpOfferAnswerService()
    {
    }

    /// <summary>Creates an initial offer without mutating the supplied session template or options.</summary>
    /// <param name="sessionTemplate">The session-level description containing the origin, session name, connection, and timing fields.</param>
    /// <param name="options">The negotiation policies and local endpoint capabilities.</param>
    /// <returns>The generated offer and its deterministic per-media mappings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session template or options are null.</exception>
    /// <exception cref="ArgumentException">Thrown when the template, options, or capability graph cannot describe an initial offer.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option or numeric capability is outside its supported range.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when payload, header-extension, or BUNDLE assignments cannot be allocated.</exception>
    public SdpNegotiationResult CreateInitialOffer(SdpSessionDescription sessionTemplate, SdpNegotiationOptions options)
    {
        if (sessionTemplate == null)
        {
            throw new ArgumentNullException("sessionTemplate");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        SdpOfferAnswerCore.ValidateInitialOfferArguments(sessionTemplate, options);
        return SdpOfferAnswerCore.CreateInitialOffer(sessionTemplate, options);
    }

    /// <summary>Creates a subsequent offer while retaining stable negotiation identities from the previous local offer.</summary>
    /// <param name="previousOffer">The previous local offer whose negotiation state should be continued.</param>
    /// <param name="options">The negotiation policies and current local endpoint capabilities.</param>
    /// <returns>The generated subsequent offer and its deterministic per-media mappings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the previous offer or options are null.</exception>
    /// <exception cref="ArgumentException">Thrown when the previous offer, options, capabilities, or requested ICE restart is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option or numeric capability is outside its supported range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a changed offer requires an origin version that cannot be incremented.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when a stable payload, extension, ICE, or BUNDLE assignment cannot be retained.</exception>
    public SdpNegotiationResult CreateSubsequentOffer(SdpSessionDescription previousOffer, SdpNegotiationOptions options)
    {
        if (previousOffer == null)
        {
            throw new ArgumentNullException("previousOffer");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        SdpSubsequentOfferCore.ValidateArguments(previousOffer, options);
        return SdpSubsequentOfferCore.CreateOffer(previousOffer, options);
    }

    /// <summary>Creates an answer whose media descriptions correspond positionally to the supplied offer.</summary>
    /// <param name="offer">The remote offer to answer.</param>
    /// <param name="sessionTemplate">The local session-level description containing the origin, session name, connection, and timing fields.</param>
    /// <param name="options">The negotiation policies and local endpoint capabilities.</param>
    /// <returns>The generated answer and the outcome for every offered media description.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the offer, session template, or options are null.</exception>
    /// <exception cref="ArgumentException">Thrown when the offer, template, options, or capability graph cannot be negotiated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option or numeric capability is outside its supported range.</exception>
    public SdpNegotiationResult CreateAnswer(SdpSessionDescription offer, SdpSessionDescription sessionTemplate, SdpNegotiationOptions options)
    {
        if (offer == null)
        {
            throw new ArgumentNullException("offer");
        }
        if (sessionTemplate == null)
        {
            throw new ArgumentNullException("sessionTemplate");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        SdpOfferAnswerCore.ValidateAnswerArguments(offer, sessionTemplate, options);
        return SdpOfferAnswerCore.CreateAnswer(offer, sessionTemplate, options);
    }
}
