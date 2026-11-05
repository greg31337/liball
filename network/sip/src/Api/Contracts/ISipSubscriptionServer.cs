namespace sip;

/// <summary>Accepts, rejects, notifies, and expires server subscriptions.</summary>
public interface ISipSubscriptionServer
{
    /// <summary>Configures application authorization for subscriptions.</summary>
    /// <param name="authorizationPolicy">The subscription authorization policy.</param>
    void ConfigureSubscriptionAuthorizationPolicy(
        ISipSubscriptionAuthorizationPolicy authorizationPolicy);
    /// <summary>Processes one SUBSCRIBE request.</summary>
    /// <param name="request">The subscription request.</param>
    /// <returns>The resulting event.</returns>
    SipSubscriptionEvent ProcessSubscription(SipSubscriptionRequest request);
    /// <summary>Creates a NOTIFY operation.</summary>
    /// <param name="subscriptionEvent">The subscription state and body context.</param>
    /// <returns>The resulting event.</returns>
    SipSubscriptionEvent SendNotify(SipSubscriptionEvent subscriptionEvent);
    /// <summary>Processes a response to an outstanding NOTIFY request.</summary>
    /// <param name="message">The NOTIFY response.</param>
    /// <returns>The delivery event.</returns>
    SipSubscriptionEvent ReceiveNotifyResponse(SipMessage message);
    /// <summary>Expires subscriptions at a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void ExpireSubscriptions(long nowMilliseconds);
    /// <summary>Polls the next queued server-subscription event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipSubscriptionEvent PollSubscriptionEvent();
}
