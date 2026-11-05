namespace sip;

/// <summary>Authorizes initial, refreshed, and terminated subscriptions.</summary>
public interface ISipSubscriptionAuthorizationPolicy
{
    /// <summary>Authorizes one validated subscription operation.</summary>
    /// <param name="context">The subscription authorization context.</param>
    /// <returns>The application authorization decision.</returns>
    SipAuthorizationDecision AuthorizeSubscription(
        SipSubscriptionAuthorizationContext context);
}
