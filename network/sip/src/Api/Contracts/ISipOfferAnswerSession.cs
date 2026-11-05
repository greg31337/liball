namespace sip;

/// <summary>Coordinates opaque or SDP offer/answer state.</summary>
public interface ISipOfferAnswerSession
{
    /// <summary>Gets the current detached offer/answer state.</summary>
    /// <returns>The current exchange.</returns>
    SipOfferAnswer GetOfferAnswer();
    /// <summary>Applies a local offer.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The updated exchange.</returns>
    SipOfferAnswer ApplyLocalOffer(SipOfferAnswer exchange);
    /// <summary>Applies a remote offer.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The updated exchange.</returns>
    SipOfferAnswer ApplyRemoteOffer(SipOfferAnswer exchange);
    /// <summary>Applies an answer.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The updated exchange.</returns>
    SipOfferAnswer ApplyOfferAnswer(SipOfferAnswer exchange);
    /// <summary>Rolls back an outstanding exchange.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The rolled-back exchange.</returns>
    SipOfferAnswer RollbackOfferAnswer(SipOfferAnswer exchange);
    /// <summary>Rejects an outstanding remote offer and restores negotiated state.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The rejected exchange.</returns>
    SipOfferAnswer RejectOfferAnswer(SipOfferAnswer exchange);
}
