namespace sip;

/// <summary>Creates, refreshes, terminates, and observes client subscriptions.</summary>
public interface ISipSubscriptionClient
{
    /// <summary>Configures the dialog layer used for in-dialog subscriptions.</summary>
    /// <param name="dialogLayer">The dialog layer.</param>
    void ConfigureSubscriptionDialogLayer(ISipDialogLayer dialogLayer);
    /// <summary>Configures authentication used for subscription challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    void ConfigureSubscriptionAuthenticationClient(
        ISipAuthenticationClient authenticationClient);
    /// <summary>Starts a client subscription.</summary>
    /// <param name="request">The subscription request.</param>
    /// <returns>The resulting event.</returns>
    SipSubscriptionEvent CreateSubscription(SipSubscriptionRequest request);
    /// <summary>Refreshes a client subscription.</summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>The resulting event.</returns>
    SipSubscriptionEvent RefreshSubscription(SipSubscriptionRequest request);
    /// <summary>Consumes a response to an outstanding SUBSCRIBE request.</summary>
    /// <param name="message">The SUBSCRIBE response.</param>
    /// <returns>The resulting event.</returns>
    SipSubscriptionEvent ReceiveSubscriptionResponse(SipMessage message);
    /// <summary>Consumes a NOTIFY request.</summary>
    /// <param name="message">The NOTIFY request.</param>
    /// <returns>The resulting event.</returns>
    SipSubscriptionEvent ConsumeNotify(SipMessage message);
    /// <summary>Terminates a client subscription.</summary>
    /// <param name="cancellation">The termination request.</param>
    void TerminateSubscription(SipCancellation cancellation);
    /// <summary>Advances refresh, expiry, and retry processing.</summary>
    /// <param name="nowMilliseconds">The current monotonic timestamp.</param>
    void AdvanceSubscriptionTime(long nowMilliseconds);
    /// <summary>Polls the next queued client-subscription event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipSubscriptionEvent PollSubscriptionEvent();
}
