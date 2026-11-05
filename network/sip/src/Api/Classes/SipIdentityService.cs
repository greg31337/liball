namespace sip;

/// <summary>Inspects, preserves, verifies, and privacy-filters SIP identity metadata.</summary>
public class SipIdentityService : ISipIdentityService
{
    /// <summary>Initializes an identity metadata service.</summary>
    public SipIdentityService()
    {
    }

    /// <summary>Inspects identity-related fields while retaining extension parameters.</summary>
    /// <param name="message">The message to inspect.</param>
    /// <returns>The detached identity metadata.</returns>
    public SipIdentityResult InspectIdentityMessage(SipMessage message)
    {
        return SipIdentityServiceCore.Inspect(message);
    }

    /// <summary>Adds identity-related metadata to a detached message.</summary>
    /// <param name="message">The message to copy and augment.</param>
    /// <param name="metadata">The identity metadata to add.</param>
    /// <returns>The augmented message or a validation failure.</returns>
    public SipIdentityResult AddIdentityHeaders(
        SipMessage message, SipIdentityMetadata metadata)
    {
        return SipIdentityServiceCore.AddHeaders(message, metadata);
    }

    /// <summary>Filters identity fields when forwarding across a trust boundary.</summary>
    /// <param name="message">The message to copy and filter.</param>
    /// <param name="trustedDestination">Whether the next recipient belongs to the trust domain.</param>
    /// <returns>The filtered message and retained metadata.</returns>
    public SipIdentityResult ApplyIdentityPrivacy(
        SipMessage message, bool trustedDestination)
    {
        return SipIdentityServiceCore.ApplyPrivacy(
            message, trustedDestination);
    }

    /// <summary>Updates the RFC 4916 connected identity while retaining the dialog tag.</summary>
    /// <param name="message">The request or response to copy and update.</param>
    /// <param name="identity">The connected identity to present.</param>
    /// <returns>The updated message or a validation failure.</returns>
    public SipIdentityResult UpdateConnectedIdentity(
        SipMessage message, SipNameAddress identity)
    {
        return SipIdentityServiceCore.UpdateConnectedIdentity(
            message, identity);
    }

    /// <summary>Passes opaque Identity fields and PASSporTs to application verification policy.</summary>
    /// <param name="message">The SIP request to verify.</param>
    /// <param name="policy">The application verification and trust policy.</param>
    /// <returns>The detached application decision.</returns>
    public SipIdentityVerificationResult VerifyMessageIdentity(
        SipMessage message, ISipIdentityVerificationPolicy policy)
    {
        return SipIdentityServiceCore.Verify(message, policy);
    }
}
