namespace sip.Tests;

public class SipCertificateAuthenticationTests
{
    [Test]
    public void TrustedMutualTlsPeerProducesOwnedAuthenticationResult()
    {
        CertificatePolicy policy = new CertificatePolicy();
        policy.Decision.Allowed = true;
        policy.Decision.Identity = "alice@example.com";
        policy.Decision.Scopes = new string[] { "sip:register" };
        SipCertificateIdentity certificate = CreateCertificate();
        SipConnectionInfo connection = CreateConnection(certificate);
        connection.PeerIdentity = "transport.example.com";
        SipAuthenticationRequest request = CreateRequest();

        SipAuthenticationResult result = CreateServer()
            .VerifyCertificateAuthentication(request, connection, policy);

        certificate.Domains[0] = "changed.example.com";
        certificate.CertificateBytes[0] = 9;
        policy.Decision.Scopes[0] = "changed";
        policy.ReceivedIdentity!.Domains[0] = "policy-change.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.Identity, Is.EqualTo("alice@example.com"));
            Assert.That(result.Message, Is.Not.SameAs(request.Message));
            Assert.That(result.CertificateIdentity, Is.Not.SameAs(certificate));
            Assert.That(result.CertificateIdentity!.Domains,
                Is.EqualTo(new string[] { "peer.example.com" }));
            Assert.That(result.CertificateIdentity.CertificateBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.CertificateDecision,
                Is.Not.SameAs(policy.Decision));
            Assert.That(result.CertificateDecision!.Allowed, Is.True);
            Assert.That(result.CertificateDecision.Scopes,
                Is.EqualTo(new string[] { "sip:register" }));
            Assert.That(result.CorrelationId, Is.EqualTo("certificate-test"));
            Assert.That(policy.CallCount, Is.EqualTo(1));
            Assert.That(policy.ReceivedIdentity, Is.Not.SameAs(certificate));
        });
    }

    [Test]
    public void TrustedPeerFallsBackToTransportOrCertificateIdentity()
    {
        CertificatePolicy transportPolicy = CreateAllowingPolicy();
        SipConnectionInfo transportConnection = CreateConnection(
            CreateCertificate());
        transportConnection.PeerIdentity = "authenticated-transport.example";
        CertificatePolicy certificatePolicy = CreateAllowingPolicy();
        SipConnectionInfo certificateConnection = CreateConnection(
            CreateCertificate());

        SipAuthenticationResult transportResult = CreateServer()
            .VerifyCertificateAuthentication(CreateRequest(),
                transportConnection, transportPolicy);
        SipAuthenticationResult certificateResult = CreateServer()
            .VerifyCertificateAuthentication(CreateRequest(),
                certificateConnection, certificatePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(transportResult.Identity,
                Is.EqualTo("authenticated-transport.example"));
            Assert.That(certificateResult.Identity,
                Is.EqualTo("peer.example.com"));
        });
    }

    [Test]
    public void CertificatePolicyDenialRetainsPolicyResultWithoutTrustingPeer()
    {
        CertificatePolicy policy = new CertificatePolicy();
        policy.Decision.Allowed = false;
        policy.Decision.Identity = "untrusted@example.com";
        policy.Decision.Reason = "Peer is outside the trusted federation.";
        policy.Decision.Scopes = new string[] { "sip:invite" };
        policy.Decision.StatusCode = SipStatusCodes.Forbidden;

        SipAuthenticationResult result = CreateServer()
            .VerifyCertificateAuthentication(CreateRequest(),
                CreateConnection(CreateCertificate()), policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.Identity, Is.Null);
            Assert.That(result.Message, Is.Null);
            Assert.That(result.RetryAllowed, Is.False);
            Assert.That(result.ErrorMessage,
                Is.EqualTo("Peer is outside the trusted federation."));
            Assert.That(result.CertificateIdentity, Is.Not.Null);
            Assert.That(result.CertificateDecision!.Allowed, Is.False);
            Assert.That(result.CertificateDecision.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(result.CertificateDecision.Scopes,
                Is.EqualTo(new string[] { "sip:invite" }));
        });
    }

    [TestCase(false, true, true, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, true, false, true)]
    [TestCase(true, true, true, false)]
    public void RejectsTransportThatDoesNotProveMutualTls(
        bool secure, bool chainValid, bool mutualTls, bool hasCertificateBytes)
    {
        CertificatePolicy policy = CreateAllowingPolicy();
        SipCertificateIdentity certificate = CreateCertificate();
        certificate.ChainValid = chainValid;
        certificate.MutualTls = mutualTls;
        certificate.CertificateBytes = hasCertificateBytes
            ? new byte[] { 1, 2, 3 } : new byte[0];
        SipConnectionInfo connection = CreateConnection(certificate);
        connection.Secure = secure;

        SipAuthenticationResult result = CreateServer()
            .VerifyCertificateAuthentication(CreateRequest(), connection,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.Identity, Is.Null);
            Assert.That(result.CertificateIdentity, Is.Null);
            Assert.That(policy.CallCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void ReportsMissingPolicyDecisionOrTrustedIdentity()
    {
        CertificatePolicy missingDecision = CreateAllowingPolicy();
        missingDecision.ReturnNull = true;
        CertificatePolicy missingIdentity = CreateAllowingPolicy();
        SipCertificateIdentity anonymous = CreateCertificate();
        anonymous.Subject = null;
        anonymous.Domains = new string[0];

        SipAuthenticationResult noDecision = CreateServer()
            .VerifyCertificateAuthentication(CreateRequest(),
                CreateConnection(CreateCertificate()), missingDecision);
        SipAuthenticationResult noIdentity = CreateServer()
            .VerifyCertificateAuthentication(CreateRequest(),
                CreateConnection(anonymous), missingIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(noDecision.Status,
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(noDecision.ErrorMessage,
                Does.Contain("no decision"));
            Assert.That(noIdentity.Status,
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(noIdentity.CertificateDecision!.Allowed, Is.True);
            Assert.That(noIdentity.Identity, Is.Null);
        });
    }

    static SipAuthenticationServer CreateServer()
    {
        return new SipAuthenticationServer(new CertificateCredentialProvider());
    }

    static CertificatePolicy CreateAllowingPolicy()
    {
        CertificatePolicy policy = new CertificatePolicy();
        policy.Decision.Allowed = true;
        return policy;
    }

    static SipAuthenticationRequest CreateRequest()
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Register;
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = message;
        request.Scope = SipCredentialScope.Origin;
        request.CorrelationId = "certificate-test";
        return request;
    }

    static SipCertificateIdentity CreateCertificate()
    {
        SipCertificateIdentity certificate = new SipCertificateIdentity();
        certificate.Subject = "CN=peer.example.com";
        certificate.Domains = new string[] { "peer.example.com" };
        certificate.CertificateBytes = new byte[] { 1, 2, 3 };
        certificate.ChainValid = true;
        certificate.MutualTls = true;
        return certificate;
    }

    static SipConnectionInfo CreateConnection(
        SipCertificateIdentity certificate)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tls;
        connection.Secure = true;
        connection.CertificateIdentity = certificate;
        return connection;
    }
}

class CertificateCredentialProvider : ISipCredentialProvider
{
    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        return new SipCredential();
    }
}

class CertificatePolicy : ISipCertificatePolicy
{
    internal SipAuthorizationDecision Decision = new SipAuthorizationDecision();
    internal SipCertificateIdentity? ReceivedIdentity;
    internal int CallCount;
    internal bool ReturnNull;

    public SipAuthorizationDecision EvaluateCertificate(
        SipCertificateIdentity identity, SipAuthenticationRequest request)
    {
        CallCount = CallCount + 1;
        ReceivedIdentity = identity;
        return ReturnNull ? null! : Decision;
    }
}
