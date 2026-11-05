namespace sip.Tests;

public class SipAuthorizationPolicyTests
{
    [Test]
    public void BearerContextIsRedactedAndIdentityIsPropagated()
    {
        RecordingAuthorizationPolicy policy = new RecordingAuthorizationPolicy();
        policy.Decision.Allowed = true;
        policy.Decision.Scopes = new string[] { "sip:register" };
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin, "sip.example");
        SipAuthenticationResult authentication = CreateBearerAuthentication(
            "alice@example.com", "sip.example");

        SipAuthorizationDecision result = CreateServer()
            .AuthorizeAuthenticatedOperation(request, authentication, policy);

        authentication.BearerToken!.Scopes[0] = "changed";
        policy.Decision.Scopes[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Allowed, Is.True);
            Assert.That(result.Identity, Is.EqualTo("alice@example.com"));
            Assert.That(result.StatusCode, Is.EqualTo(0));
            Assert.That(result.Scopes,
                Is.EqualTo(new string[] { "sip:register" }));
            Assert.That(result, Is.Not.SameAs(policy.Decision));
            Assert.That(policy.CallCount, Is.EqualTo(1));
            Assert.That(policy.Request!.Credential!.UserName,
                Is.EqualTo("alice@example.com"));
            Assert.That(policy.Request.Credential.Secret, Is.Empty);
            Assert.That(policy.Request.Credential.Scope,
                Is.EqualTo(SipCredentialScope.Origin));
            Assert.That(policy.Request.Credential.Realm,
                Is.EqualTo("sip.example"));
            Assert.That(policy.Request.Message!.OriginalBytes, Is.Empty);
            Assert.That(FindCredentialHeader(policy.Request.Message), Is.Null);
            Assert.That(policy.Request.BearerToken!.Token, Is.Null);
            Assert.That(policy.Token!.Token, Is.Null);
            Assert.That(policy.Token.Scopes,
                Is.EqualTo(new string[] { "sip:register" }));
        });
    }

    [Test]
    public void CertificateContextIsCopiedIntoPolicyDenial()
    {
        RecordingAuthorizationPolicy policy = new RecordingAuthorizationPolicy();
        policy.Decision.Allowed = false;
        policy.Decision.Reason = "Registration is not permitted.";
        policy.Decision.StatusCode = SipStatusCodes.Forbidden;
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin, null);
        SipCertificateIdentity certificate = new SipCertificateIdentity();
        certificate.Subject = "CN=peer.example.com";
        certificate.Domains = new string[] { "peer.example.com" };
        certificate.CertificateBytes = new byte[] { 1, 2, 3 };
        certificate.ChainValid = true;
        certificate.MutualTls = true;
        SipAuthenticationResult authentication = new SipAuthenticationResult();
        authentication.Status = SipAuthenticationStatus.Success;
        authentication.Identity = "peer.example.com";
        authentication.CertificateIdentity = certificate;
        authentication.CorrelationId = request.CorrelationId;

        SipAuthorizationDecision result = CreateServer()
            .AuthorizeAuthenticatedOperation(request, authentication, policy);

        certificate.Domains[0] = "changed.example.com";
        policy.Certificate!.CertificateBytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Allowed, Is.False);
            Assert.That(result.Reason,
                Is.EqualTo("Registration is not permitted."));
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(policy.Certificate, Is.Not.SameAs(certificate));
            Assert.That(policy.Certificate!.Domains,
                Is.EqualTo(new string[] { "peer.example.com" }));
            Assert.That(authentication.CertificateIdentity!.CertificateBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(policy.Token, Is.Null);
            Assert.That(policy.Request!.Credential!.UserName,
                Is.EqualTo("peer.example.com"));
        });
    }

    [TestCase(SipAuthenticationStatus.InvalidCredentials, "identity", true)]
    [TestCase(SipAuthenticationStatus.Success, null, true)]
    [TestCase(SipAuthenticationStatus.Success, "identity", false)]
    public void RejectsUnusableOrUnrelatedAuthenticationBeforePolicy(
        SipAuthenticationStatus status, string? identity,
        bool matchingCorrelation)
    {
        RecordingAuthorizationPolicy policy = new RecordingAuthorizationPolicy();
        policy.Decision.Allowed = true;
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Proxy, null);
        SipAuthenticationResult authentication = new SipAuthenticationResult();
        authentication.Status = status;
        authentication.Identity = identity;
        authentication.CorrelationId = matchingCorrelation
            ? request.CorrelationId : "another-operation";

        SipAuthorizationDecision result = CreateServer()
            .AuthorizeAuthenticatedOperation(request, authentication, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Allowed, Is.False);
            Assert.That(result.StatusCode, Is.Not.EqualTo(0));
            Assert.That(policy.CallCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void PolicyCanEnforceCredentialScopeAndBearerRealm()
    {
        RecordingAuthorizationPolicy policy = new RecordingAuthorizationPolicy();
        policy.EnforceContext = true;
        policy.ExpectedScope = SipCredentialScope.Origin;
        policy.ExpectedRealm = "sip.example";
        SipAuthenticationRequest matchingRequest = CreateRequest(
            SipCredentialScope.Origin, "sip.example");
        SipAuthenticationResult matchingAuthentication =
            CreateBearerAuthentication("alice", "sip.example");
        SipAuthenticationRequest wrongScopeRequest = CreateRequest(
            SipCredentialScope.Proxy, "sip.example");
        SipAuthenticationResult wrongScopeAuthentication =
            CreateBearerAuthentication("alice", "sip.example");
        wrongScopeAuthentication.CorrelationId = wrongScopeRequest.CorrelationId;
        SipAuthenticationRequest wrongRealmRequest = CreateRequest(
            SipCredentialScope.Origin, "other.example");
        SipAuthenticationResult wrongRealmAuthentication =
            CreateBearerAuthentication("alice", "other.example");

        SipAuthorizationDecision matching = CreateServer()
            .AuthorizeAuthenticatedOperation(matchingRequest,
                matchingAuthentication, policy);
        SipAuthorizationDecision wrongScope = CreateServer()
            .AuthorizeAuthenticatedOperation(wrongScopeRequest,
                wrongScopeAuthentication, policy);
        SipAuthorizationDecision wrongRealm = CreateServer()
            .AuthorizeAuthenticatedOperation(wrongRealmRequest,
                wrongRealmAuthentication, policy);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Allowed, Is.True);
            Assert.That(matching.Identity, Is.EqualTo("alice"));
            Assert.That(wrongScope.Allowed, Is.False);
            Assert.That(wrongScope.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(wrongRealm.Allowed, Is.False);
            Assert.That(wrongRealm.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
        });
    }

    [Test]
    public void MissingPolicyDecisionFailsClosed()
    {
        RecordingAuthorizationPolicy policy = new RecordingAuthorizationPolicy();
        policy.ReturnNull = true;
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin, null);
        SipAuthenticationResult authentication = new SipAuthenticationResult();
        authentication.Status = SipAuthenticationStatus.Success;
        authentication.Identity = "alice";
        authentication.CorrelationId = request.CorrelationId;

        SipAuthorizationDecision result = CreateServer()
            .AuthorizeAuthenticatedOperation(request, authentication, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Allowed, Is.False);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(result.Reason, Does.Contain("no decision"));
        });
    }

    static SipAuthenticationServer CreateServer()
    {
        return new SipAuthenticationServer(
            new AuthorizationCredentialProvider());
    }

    static SipAuthenticationRequest CreateRequest(
        SipCredentialScope scope, string? realm)
    {
        SipHeader authorization = new SipHeader();
        authorization.Name = scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthorization
            : SipHeaderNames.Authorization;
        authorization.Value = "Bearer top-secret-access-token";
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Register;
        message.Headers = new SipHeader[] { authorization };
        message.OriginalBytes = System.Text.Encoding.ASCII.GetBytes(
            authorization.Name + ": " + authorization.Value);
        SipBearerToken bearer = new SipBearerToken();
        bearer.Token = "top-secret-access-token";
        bearer.Realm = realm;
        bearer.Scopes = new string[] { "sip:register" };
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = message;
        request.BearerToken = bearer;
        request.Scope = scope;
        request.CorrelationId = "authorization-operation";
        return request;
    }

    static SipAuthenticationResult CreateBearerAuthentication(
        string identity, string realm)
    {
        SipBearerToken token = new SipBearerToken();
        token.Token = "top-secret-access-token";
        token.Subject = identity;
        token.Realm = realm;
        token.Scopes = new string[] { "sip:register" };
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.Identity = identity;
        result.BearerToken = token;
        result.CorrelationId = "authorization-operation";
        return result;
    }

    static SipHeader? FindCredentialHeader(SipMessage message)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (string.Equals(header.Name, SipHeaderNames.Authorization,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(header.Name,
                    SipHeaderNames.ProxyAuthorization,
                    StringComparison.OrdinalIgnoreCase))
            {
                return header;
            }
            index = index + 1;
        }
        return null;
    }
}

class AuthorizationCredentialProvider : ISipCredentialProvider
{
    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        return new SipCredential();
    }
}

class RecordingAuthorizationPolicy : ISipAuthorizationPolicy
{
    internal SipAuthorizationDecision Decision = new SipAuthorizationDecision();
    internal SipAuthenticationRequest? Request;
    internal SipCertificateIdentity? Certificate;
    internal SipBearerToken? Token;
    internal int CallCount;
    internal bool ReturnNull;
    internal bool EnforceContext;
    internal SipCredentialScope ExpectedScope;
    internal string? ExpectedRealm;

    public SipAuthorizationDecision AuthorizeOperation(
        SipAuthenticationRequest request, SipCertificateIdentity? certificate,
        SipBearerToken? token)
    {
        CallCount = CallCount + 1;
        Request = request;
        Certificate = certificate;
        Token = token;
        if (ReturnNull)
        {
            return null!;
        }
        if (EnforceContext && (request.Scope != ExpectedScope
            || token == null || token.Realm != ExpectedRealm))
        {
            SipAuthorizationDecision denied = new SipAuthorizationDecision();
            denied.Allowed = false;
            denied.Reason = "The scope or realm is not authorized.";
            denied.StatusCode = SipStatusCodes.Forbidden;
            return denied;
        }
        if (EnforceContext)
        {
            SipAuthorizationDecision allowed = new SipAuthorizationDecision();
            allowed.Allowed = true;
            return allowed;
        }
        return Decision;
    }
}
