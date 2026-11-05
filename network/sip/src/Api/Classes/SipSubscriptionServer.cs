namespace sip;

/// <summary>Accepts, rejects, notifies, and expires server subscriptions.</summary>
public class SipSubscriptionServer : ISipSubscriptionServer
{
    SipSubscriptionServerCore core;

    /// <summary>Initializes an empty subscription server.</summary>
    public SipSubscriptionServer()
    {
        core = new SipSubscriptionServerCore(
            new SipCryptographicRandomSource());
    }

    /// <summary>Configures application authorization for subscriptions.</summary>
    /// <param name="authorizationPolicy">The subscription authorization policy.</param>
    public void ConfigureSubscriptionAuthorizationPolicy(
        ISipSubscriptionAuthorizationPolicy authorizationPolicy)
    {
        core.ConfigureAuthorization(authorizationPolicy);
    }

    /// <summary>Processes one SUBSCRIBE request.</summary>
    /// <param name="request">The subscription request.</param>
    /// <returns>The resulting event.</returns>
    public SipSubscriptionEvent ProcessSubscription(
        SipSubscriptionRequest request)
    {
        return core.Process(request);
    }

    /// <summary>Creates a NOTIFY operation.</summary>
    /// <param name="subscriptionEvent">The subscription state and body context.</param>
    /// <returns>The resulting event.</returns>
    public SipSubscriptionEvent SendNotify(
        SipSubscriptionEvent subscriptionEvent)
    {
        return core.SendNotification(subscriptionEvent);
    }

    /// <summary>Processes a response to an outstanding NOTIFY request.</summary>
    /// <param name="message">The NOTIFY response.</param>
    /// <returns>The delivery event.</returns>
    public SipSubscriptionEvent ReceiveNotifyResponse(SipMessage message)
    {
        return core.ReceiveNotificationResponse(message);
    }

    /// <summary>Expires subscriptions at a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void ExpireSubscriptions(long nowMilliseconds)
    {
        core.Expire(nowMilliseconds);
    }

    /// <summary>Polls the next queued server-subscription event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipSubscriptionEvent PollSubscriptionEvent()
    {
        return core.PollEvent();
    }
}
