namespace sip;

/// <summary>Authorizes initial and conditional publication operations.</summary>
public interface ISipPublicationAuthorizationPolicy
{
    /// <summary>Authorizes one validated publication operation.</summary>
    /// <param name="context">The publication authorization context.</param>
    /// <returns>The application authorization decision.</returns>
    SipAuthorizationDecision AuthorizePublication(
        SipPublicationAuthorizationContext context);
}
