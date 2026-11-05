namespace sip;

/// <summary>Verifies SIP Identity fields and PASSporT values using application trust policy.</summary>
public interface ISipIdentityVerificationPolicy
{
    /// <summary>Verifies identity assertions without replacing signaling identity.</summary>
    /// <param name="request">The detached signaling message and identity assertions.</param>
    /// <returns>The application verification decision.</returns>
    SipIdentityVerificationResult VerifyIdentityAssertion(
        SipIdentityVerificationRequest request);
}
