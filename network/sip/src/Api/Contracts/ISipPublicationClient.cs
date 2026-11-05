namespace sip;

/// <summary>Creates, modifies, refreshes, and removes client publications.</summary>
public interface ISipPublicationClient
{
    /// <summary>Configures authentication used for publication challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    void ConfigurePublicationAuthenticationClient(
        ISipAuthenticationClient authenticationClient);
    /// <summary>Creates an initial publication.</summary>
    /// <param name="request">The publication request.</param>
    /// <returns>The resulting event.</returns>
    SipPublicationEvent CreatePublication(SipPublicationRequest request);
    /// <summary>Modifies an active publication.</summary>
    /// <param name="request">The modification request.</param>
    /// <returns>The resulting event.</returns>
    SipPublicationEvent ModifyPublication(SipPublicationRequest request);
    /// <summary>Refreshes an active publication.</summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>The resulting event.</returns>
    SipPublicationEvent RefreshPublication(SipPublicationRequest request);
    /// <summary>Removes an active publication.</summary>
    /// <param name="request">The removal request.</param>
    /// <returns>The resulting event.</returns>
    SipPublicationEvent RemovePublication(SipPublicationRequest request);
    /// <summary>Consumes a response to an outstanding PUBLISH request.</summary>
    /// <param name="message">The PUBLISH response.</param>
    /// <returns>The resulting event.</returns>
    SipPublicationEvent ReceivePublicationResponse(SipMessage message);
    /// <summary>Advances automatic refresh and expiration processing.</summary>
    /// <param name="nowMilliseconds">The current monotonic timestamp.</param>
    void AdvancePublicationTime(long nowMilliseconds);
    /// <summary>Polls the next queued publication event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipPublicationEvent PollPublicationEvent();
}
