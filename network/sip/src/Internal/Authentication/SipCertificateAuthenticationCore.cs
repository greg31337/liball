namespace sip;

internal static class SipCertificateAuthenticationCore
{
    internal static SipAuthenticationResult Verify(
        SipAuthenticationRequest? request, SipConnectionInfo? connection,
        ISipCertificatePolicy? certificatePolicy)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (connection == null)
        {
            throw new System.ArgumentNullException("connection");
        }
        if (certificatePolicy == null)
        {
            throw new System.ArgumentNullException("certificatePolicy");
        }
        if (request.Message == null
            || request.Message.Kind != SipMessageKind.Request)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Certificate authentication requires a SIP request message.",
                request);
        }
        if (request.Scope != SipCredentialScope.Origin
            && request.Scope != SipCredentialScope.Proxy)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }

        SipCertificateIdentity? certificate = connection.CertificateIdentity;
        if (!connection.Secure || certificate == null
            || !certificate.ChainValid || !certificate.MutualTls
            || certificate.CertificateBytes == null
            || certificate.CertificateBytes.Length == 0)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "A validated mutual-TLS peer certificate is required.",
                request);
        }

        SipCertificateIdentity? policyIdentity =
            SipCertificateIdentityCopy.Copy(certificate);
        if (policyIdentity == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The peer certificate identity could not be copied.", request);
        }
        SipAuthorizationDecision? decision = certificatePolicy
            .EvaluateCertificate(policyIdentity, request);
        if (decision == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The certificate policy returned no decision.", request);
        }

        SipAuthorizationDecision copiedDecision = CopyDecision(decision);
        if (!copiedDecision.Allowed)
        {
            SipAuthenticationResult denied = CreateFailure(
                SipAuthenticationStatus.InvalidCredentials,
                copiedDecision.Reason == null
                    ? "The mutual-TLS peer is not trusted."
                    : copiedDecision.Reason,
                request);
            denied.CertificateIdentity =
                SipCertificateIdentityCopy.Copy(certificate);
            denied.CertificateDecision = copiedDecision;
            return denied;
        }

        string? identity = SelectIdentity(
            copiedDecision, certificate, connection.PeerIdentity);
        if (identity == null)
        {
            SipAuthenticationResult missingIdentity = CreateFailure(
                SipAuthenticationStatus.Failed,
                "The trusted mutual-TLS peer has no identity.", request);
            missingIdentity.CertificateIdentity =
                SipCertificateIdentityCopy.Copy(certificate);
            missingIdentity.CertificateDecision = copiedDecision;
            return missingIdentity;
        }

        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.Message = SipSnapshotCopy.CopyMessageModel(request.Message);
        result.CertificateIdentity =
            SipCertificateIdentityCopy.Copy(certificate);
        result.CertificateDecision = copiedDecision;
        result.Identity = identity;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static string? SelectIdentity(SipAuthorizationDecision decision,
        SipCertificateIdentity certificate, string? peerIdentity)
    {
        if (decision.Identity != null && decision.Identity.Length > 0)
        {
            return decision.Identity;
        }
        if (peerIdentity != null && peerIdentity.Length > 0)
        {
            return peerIdentity;
        }
        string[] domains = certificate.Domains == null
            ? new string[0] : certificate.Domains;
        int index = 0;
        while (index < domains.Length)
        {
            if (domains[index] != null && domains[index].Length > 0)
            {
                return domains[index];
            }
            index = index + 1;
        }
        if (certificate.Subject != null && certificate.Subject.Length > 0)
        {
            return certificate.Subject;
        }
        return null;
    }

    static SipAuthorizationDecision CopyDecision(
        SipAuthorizationDecision source)
    {
        SipAuthorizationDecision result = new SipAuthorizationDecision();
        result.Allowed = source.Allowed;
        result.Identity = source.Identity;
        result.Reason = source.Reason;
        string[] scopes = source.Scopes == null
            ? new string[0] : source.Scopes;
        result.Scopes = new string[scopes.Length];
        int index = 0;
        while (index < scopes.Length)
        {
            result.Scopes[index] = scopes[index];
            index = index + 1;
        }
        result.StatusCode = source.StatusCode;
        return result;
    }

    static SipAuthenticationResult CreateFailure(
        SipAuthenticationStatus status, string message,
        SipAuthenticationRequest request)
    {
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = status;
        result.ErrorMessage = message;
        result.CorrelationId = request.CorrelationId;
        return result;
    }
}
