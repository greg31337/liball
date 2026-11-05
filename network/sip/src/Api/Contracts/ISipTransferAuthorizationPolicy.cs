namespace sip;

/// <summary>Authorizes validated transfer, pickup, join, and dialog-association operations.</summary>
public interface ISipTransferAuthorizationPolicy
{
    /// <summary>Authorizes one validated and correlated call-control operation.</summary>
    /// <param name="context">The validated operation and correlated dialog context.</param>
    /// <returns>The application authorization decision.</returns>
    SipAuthorizationDecision AuthorizeTransfer(SipTransferAuthorizationContext context);
}
