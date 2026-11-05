namespace sip;

/// <summary>Creates, modifies, refreshes, removes, and observes client publications.</summary>
public class SipPublicationClient : ISipPublicationClient
{
    SipPublicationClientCore core;

    /// <summary>Initializes an idle publication client.</summary>
    public SipPublicationClient()
    {
        core = new SipPublicationClientCore(
            new SipCryptographicRandomSource());
    }

    /// <summary>Configures authentication used for publication challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    public void ConfigurePublicationAuthenticationClient(
        ISipAuthenticationClient authenticationClient)
    {
        core.ConfigureAuthentication(authenticationClient);
    }

    /// <summary>Creates an initial publication.</summary>
    /// <param name="request">The publication request.</param>
    /// <returns>The resulting event.</returns>
    public SipPublicationEvent CreatePublication(SipPublicationRequest request)
    {
        return core.Create(request);
    }

    /// <summary>Modifies an active publication.</summary>
    /// <param name="request">The modification request.</param>
    /// <returns>The resulting event.</returns>
    public SipPublicationEvent ModifyPublication(SipPublicationRequest request)
    {
        return core.Modify(request);
    }

    /// <summary>Refreshes an active publication.</summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>The resulting event.</returns>
    public SipPublicationEvent RefreshPublication(SipPublicationRequest request)
    {
        return core.Refresh(request);
    }

    /// <summary>Removes an active publication.</summary>
    /// <param name="request">The removal request.</param>
    /// <returns>The resulting event.</returns>
    public SipPublicationEvent RemovePublication(SipPublicationRequest request)
    {
        return core.Remove(request);
    }

    /// <summary>Consumes a response to an outstanding PUBLISH request.</summary>
    /// <param name="message">The PUBLISH response.</param>
    /// <returns>The resulting event.</returns>
    public SipPublicationEvent ReceivePublicationResponse(SipMessage message)
    {
        return core.ReceiveResponse(message);
    }

    /// <summary>Advances automatic refresh and expiration processing.</summary>
    /// <param name="nowMilliseconds">The current monotonic timestamp.</param>
    public void AdvancePublicationTime(long nowMilliseconds)
    {
        core.AdvanceTime(nowMilliseconds);
    }

    /// <summary>Polls the next queued publication event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipPublicationEvent PollPublicationEvent()
    {
        return core.PollEvent();
    }
}
