namespace sip;

/// <summary>Creates, refreshes, terminates, and observes client subscriptions.</summary>
public class SipSubscriptionClient : ISipSubscriptionClient
{
    SipSubscriptionClientCore core;

    /// <summary>Initializes an idle subscription client.</summary>
    public SipSubscriptionClient()
    {
        core = new SipSubscriptionClientCore(
            new SipCryptographicRandomSource());
    }

    /// <summary>Configures the dialog layer used for in-dialog subscriptions.</summary>
    /// <param name="dialogLayer">The dialog layer.</param>
    public void ConfigureSubscriptionDialogLayer(ISipDialogLayer dialogLayer)
    {
        core.ConfigureDialogLayer(dialogLayer);
    }

    /// <summary>Configures authentication used for subscription challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    public void ConfigureSubscriptionAuthenticationClient(
        ISipAuthenticationClient authenticationClient)
    {
        core.ConfigureAuthentication(authenticationClient);
    }

    /// <summary>Starts a client subscription.</summary>
    /// <param name="request">The subscription request.</param>
    /// <returns>The resulting event.</returns>
    public SipSubscriptionEvent CreateSubscription(
        SipSubscriptionRequest request)
    {
        return core.Create(request);
    }

    /// <summary>Refreshes a client subscription.</summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>The resulting event.</returns>
    public SipSubscriptionEvent RefreshSubscription(
        SipSubscriptionRequest request)
    {
        return core.Refresh(request);
    }

    /// <summary>Consumes a response to an outstanding SUBSCRIBE request.</summary>
    /// <param name="message">The SUBSCRIBE response.</param>
    /// <returns>The resulting event.</returns>
    public SipSubscriptionEvent ReceiveSubscriptionResponse(
        SipMessage message)
    {
        return core.ReceiveResponse(message);
    }

    /// <summary>Consumes a NOTIFY request.</summary>
    /// <param name="message">The NOTIFY request.</param>
    /// <returns>The resulting event.</returns>
    public SipSubscriptionEvent ConsumeNotify(SipMessage message)
    {
        return core.ConsumeNotification(message);
    }

    /// <summary>Terminates a client subscription.</summary>
    /// <param name="cancellation">The termination request.</param>
    public void TerminateSubscription(SipCancellation cancellation)
    {
        core.Terminate(cancellation);
    }

    /// <summary>Advances refresh, expiry, and retry processing.</summary>
    /// <param name="nowMilliseconds">The current monotonic timestamp.</param>
    public void AdvanceSubscriptionTime(long nowMilliseconds)
    {
        core.AdvanceTime(nowMilliseconds);
    }

    /// <summary>Polls the next queued client-subscription event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipSubscriptionEvent PollSubscriptionEvent()
    {
        return core.PollEvent();
    }
}
