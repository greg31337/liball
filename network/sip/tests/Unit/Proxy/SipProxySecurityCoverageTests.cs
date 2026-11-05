namespace sip.Tests;

public class SipProxySecurityCoverageTests
{
    [Test]
    public void PreauthenticatedContextSkipsVerificationAndRedactsNestedSecrets()
    {
        ProxySecurityAuthenticationServer authentication =
            new ProxySecurityAuthenticationServer();
        ProxySecurityAuthorizationPolicy authorization =
            new ProxySecurityAuthorizationPolicy();
        authorization.Decision.Allowed = true;
        authorization.Decision.Identity = "authorized@example.com";
        SipProxy proxy = SecureProxy(authentication, authorization);
        SipProxyRequest request = Request("preauthenticated",
            "Authorization: Bearer origin-secret\r\n"
                + "Proxy-Authorization: Bearer proxy-secret\r\n"
                + "Authentication-Info: nextnonce=\"next-secret\"\r\n"
                + "P-Asserted-Identity: <sip:alice@example.com>\r\n");
        SipAuthenticationResult established = new SipAuthenticationResult();
        established.Status = SipAuthenticationStatus.Success;
        established.Identity = "authenticated@example.com";
        established.DigestAuthorization = new SipDigestAuthorization();
        SipBearerToken bearer = new SipBearerToken();
        bearer.Token = "nested-bearer-secret";
        established.BearerToken = bearer;
        established.Message = request.Message;
        request.Authentication = established;

        SipProxyResult result = proxy.ProcessProxyRequest(request);
        SipProxyEvent accepted = proxy.PollProxyEvent();
        SipProxyEvent branch = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.AuthenticatedIdentity,
                Is.EqualTo("authorized@example.com"));
            Assert.That(authentication.VerifyCount, Is.Zero);
            Assert.That(authentication.ChallengeCount, Is.Zero);
            Assert.That(authentication.AuthorizeCount, Is.EqualTo(1));
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.Authorization), Is.True);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.ProxyAuthorization), Is.False);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.AuthenticationInfo), Is.True);
            Assert.That(accepted.Request!.Authentication, Is.Not.Null);
            Assert.That(accepted.Request.Authentication!.DigestAuthorization,
                Is.Null);
            Assert.That(accepted.Request.Authentication.BearerToken!.Token,
                Is.Null);
            Assert.That(accepted.Request.TransportMessage!.Bytes, Is.Empty);
            AssertEventContainsNoCredentials(accepted);
            AssertEventContainsNoCredentials(branch);
        });
    }

    [Test]
    public void UnusableTrustedConnectionsFallBackToMessageAuthentication()
    {
        ProxySecurityAuthenticationServer disconnectedAuthentication =
            SuccessfulAuthentication("digest-disconnected@example.com");
        ProxySecurityAuthorizationPolicy disconnectedAuthorization =
            AllowedAuthorization("digest-disconnected@example.com");
        SipProxy disconnectedProxy = SecureProxy(
            disconnectedAuthentication, disconnectedAuthorization);
        SipProxyRequest disconnectedRequest = Request("peer-disconnected", "");
        disconnectedRequest.TransportMessage!.Connection = Connection(
            SipConnectionState.Closed, true, "edge.example.com");
        SipProxyResult disconnected = disconnectedProxy.ProcessProxyRequest(
            disconnectedRequest);

        ProxySecurityAuthenticationServer unnamedAuthentication =
            SuccessfulAuthentication("digest-unnamed@example.com");
        ProxySecurityAuthorizationPolicy unnamedAuthorization =
            AllowedAuthorization("digest-unnamed@example.com");
        SipProxy unnamedProxy = SecureProxy(
            unnamedAuthentication, unnamedAuthorization);
        SipProxyRequest unnamedRequest = Request("peer-unnamed", "");
        unnamedRequest.TransportMessage!.Connection = Connection(
            SipConnectionState.Connected, true, "");
        SipProxyResult unnamed = unnamedProxy.ProcessProxyRequest(
            unnamedRequest);

        ProxySecurityAuthenticationServer insecureAuthentication =
            SuccessfulAuthentication("digest-insecure@example.com");
        ProxySecurityAuthorizationPolicy insecureAuthorization =
            AllowedAuthorization("digest-insecure@example.com");
        SipProxy insecureProxy = SecureProxy(
            insecureAuthentication, insecureAuthorization);
        SipProxyRequest insecureRequest = Request("peer-insecure", "");
        insecureRequest.TransportMessage!.Connection = Connection(
            SipConnectionState.Connected, false, "edge.example.com");
        SipProxyResult insecure = insecureProxy.ProcessProxyRequest(
            insecureRequest);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Success, Is.True,
                disconnected.ErrorMessage);
            Assert.That(disconnected.AuthenticatedIdentity,
                Is.EqualTo("digest-disconnected@example.com"));
            Assert.That(disconnectedAuthentication.VerifyCount, Is.EqualTo(1));
            Assert.That(unnamed.Success, Is.True, unnamed.ErrorMessage);
            Assert.That(unnamed.AuthenticatedIdentity,
                Is.EqualTo("digest-unnamed@example.com"));
            Assert.That(unnamedAuthentication.VerifyCount, Is.EqualTo(1));
            Assert.That(insecure.Success, Is.True, insecure.ErrorMessage);
            Assert.That(insecure.AuthenticatedIdentity,
                Is.EqualTo("digest-insecure@example.com"));
            Assert.That(insecureAuthentication.VerifyCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void UsesSuppliedAuthenticationFailureOrSynthesizesChallenge()
    {
        ProxySecurityAuthenticationServer suppliedAuthentication =
            new ProxySecurityAuthenticationServer();
        suppliedAuthentication.Verification.Status =
            SipAuthenticationStatus.InvalidCredentials;
        suppliedAuthentication.Verification.Message = SecurityResponse(
            "supplied-failure", SipStatusCodes.Forbidden, "Forbidden");
        suppliedAuthentication.Verification.ErrorMessage = "Bad credentials.";
        SipProxy suppliedProxy = SecureProxy(suppliedAuthentication,
            AllowedAuthorization("unused"));
        SipProxyResult supplied = suppliedProxy.ProcessProxyRequest(
            Request("supplied-failure", ""));

        ProxySecurityAuthenticationServer synthesizedAuthentication =
            new ProxySecurityAuthenticationServer();
        synthesizedAuthentication.Verification.Status =
            SipAuthenticationStatus.ChallengeRequired;
        synthesizedAuthentication.Challenge.Status =
            SipAuthenticationStatus.ChallengeRequired;
        synthesizedAuthentication.Challenge.ErrorMessage = "Credentials needed.";
        SipProxy synthesizedProxy = SecureProxy(synthesizedAuthentication,
            AllowedAuthorization("unused"));
        SipProxyResult synthesized = synthesizedProxy.ProcessProxyRequest(
            Request("synthesized-challenge", ""));

        Assert.Multiple(() =>
        {
            Assert.That(supplied.Success, Is.True);
            Assert.That(supplied.Completed, Is.True);
            Assert.That(supplied.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(suppliedAuthentication.ChallengeCount, Is.Zero);
            Assert.That(supplied.ErrorMessage, Is.EqualTo("Bad credentials."));
            Assert.That(synthesized.Success, Is.True);
            Assert.That(synthesized.Completed, Is.True);
            Assert.That(synthesized.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ProxyAuthenticationRequired));
            Assert.That(synthesizedAuthentication.ChallengeCount,
                Is.EqualTo(1));
            Assert.That(synthesized.SendRequest, Is.Not.Null);
            Assert.That(synthesized.ErrorMessage,
                Is.EqualTo("Credentials needed."));
        });
    }

    [Test]
    public void NormalizesInvalidAndMissingAuthorizationDecisions()
    {
        ProxySecurityAuthenticationServer invalidAuthentication =
            SuccessfulAuthentication("alice@example.com");
        ProxySecurityAuthorizationPolicy invalidPolicy =
            new ProxySecurityAuthorizationPolicy();
        invalidPolicy.Decision.Allowed = false;
        invalidPolicy.Decision.StatusCode = SipStatusCodes.Ok;
        invalidPolicy.Decision.Reason = "Denied\r\nInjected: value";
        SipProxy invalidProxy = SecureProxy(invalidAuthentication, invalidPolicy);
        SipProxyResult invalid = invalidProxy.ProcessProxyRequest(
            Request("authorization-invalid", ""));

        ProxySecurityAuthenticationServer missingAuthentication =
            SuccessfulAuthentication("alice@example.com");
        NullProxyAuthorizationPolicy missingPolicy =
            new NullProxyAuthorizationPolicy();
        SipProxy missingProxy = SecureProxy(
            missingAuthentication, missingPolicy);
        SipProxyResult missing = missingProxy.ProcessProxyRequest(
            Request("authorization-missing", ""));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(invalid.ErrorMessage, Does.Not.Contain("\r"));
            Assert.That(invalid.ErrorMessage, Does.Not.Contain("\n"));
            Assert.That(invalid.Response.ReasonPhrase,
                Does.Not.Contain("\n"));
            Assert.That(missing.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(missing.Response.ReasonPhrase,
                Is.EqualTo(SipReasonPhrases.Forbidden));
            Assert.That(missingAuthentication.AuthorizeCount, Is.EqualTo(1));
            Assert.That(missingPolicy.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void PrivacyIdMatchingIsTokenBasedAndCaseInsensitive()
    {
        ProxySecurityAuthenticationServer keepAuthentication =
            SuccessfulAuthentication("alice@example.com");
        SipProxy keepProxy = SecureProxy(keepAuthentication,
            AllowedAuthorization("alice@example.com"));
        SipProxyResult kept = keepProxy.ProcessProxyRequest(Request(
            "privacy-word", "Privacy: identity; session\r\n"
                + "P-Asserted-Identity: <sip:alice@example.com>\r\n"
                + "P-Preferred-Identity: <sip:alias@example.com>\r\n"));

        ProxySecurityAuthenticationServer removeAuthentication =
            SuccessfulAuthentication("alice@example.com");
        SipProxy removeProxy = SecureProxy(removeAuthentication,
            AllowedAuthorization("alice@example.com"));
        SipProxyResult removed = removeProxy.ProcessProxyRequest(Request(
            "privacy-id", "Privacy: user; ID\r\n"
                + "P-Asserted-Identity: <sip:alice@example.com>\r\n"
                + "P-Preferred-Identity: <sip:alias@example.com>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(HasHeader(kept.Request!,
                SipHeaderNames.PAssertedIdentity), Is.True);
            Assert.That(HasHeader(kept.Request!,
                SipHeaderNames.PPreferredIdentity), Is.True);
            Assert.That(HasHeader(removed.Request!,
                SipHeaderNames.PAssertedIdentity), Is.False);
            Assert.That(HasHeader(removed.Request!,
                SipHeaderNames.PPreferredIdentity), Is.False);
            Assert.That(HasHeader(removed.Request!, SipHeaderNames.Privacy),
                Is.True);
        });
    }

    [Test]
    public void SafetyPolicyIsValidatedCopiedAndAllowsZeroRetryAfter()
    {
        AssertInvalidSafety(options => options.MaxTransactionCount = 0);
        AssertInvalidSafety(options => options.MaxBranchCount = 0);
        AssertInvalidSafety(options => options.MaxEventCount = 0);
        AssertInvalidSafety(options => options.MaxRequestCount = 0);
        AssertInvalidSafety(options => options.RequestWindowMilliseconds = 0);
        AssertInvalidSafety(options => options.RetryAfterSeconds = -1);

        SipProxySafetyOptions configured = SafetyOptions();
        configured.MaxTransactionCount = 1;
        configured.RetryAfterSeconds = 0;
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxySafety(configured);
        configured.RetryAfterSeconds = 99;
        SipProxyResult admitted = proxy.ProcessProxyRequest(
            Request("overload-first", ""));
        SipProxyResult overloaded = proxy.ProcessProxyRequest(
            Request("overload-second", ""));

        Assert.Multiple(() =>
        {
            Assert.That(admitted.Success, Is.True, admitted.ErrorMessage);
            Assert.That(overloaded.Success, Is.True);
            Assert.That(overloaded.Completed, Is.True);
            Assert.That(overloaded.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(Header(overloaded.Response, SipHeaderNames.RetryAfter),
                Is.EqualTo("0"));
        });
    }

    static void AssertInvalidSafety(
        System.Action<SipProxySafetyOptions> invalidate)
    {
        SipProxySafetyOptions options = SafetyOptions();
        invalidate(options);
        Assert.Throws<System.ArgumentException>(() =>
            new SipProxy().ConfigureProxySafety(options));
    }

    static SipProxySafetyOptions SafetyOptions()
    {
        SipProxySafetyOptions options = new SipProxySafetyOptions();
        options.MaxTransactionCount = 10;
        options.MaxBranchCount = 10;
        options.MaxEventCount = 100;
        options.MaxRequestCount = 10;
        options.RequestWindowMilliseconds = 1000;
        options.RetryAfterSeconds = 1;
        return options;
    }

    static ProxySecurityAuthenticationServer SuccessfulAuthentication(
        string identity)
    {
        ProxySecurityAuthenticationServer result =
            new ProxySecurityAuthenticationServer();
        result.Verification.Status = SipAuthenticationStatus.Success;
        result.Verification.Identity = identity;
        return result;
    }

    static ProxySecurityAuthorizationPolicy AllowedAuthorization(
        string identity)
    {
        ProxySecurityAuthorizationPolicy result =
            new ProxySecurityAuthorizationPolicy();
        result.Decision.Allowed = true;
        result.Decision.Identity = identity;
        return result;
    }

    static SipProxy SecureProxy(
        ProxySecurityAuthenticationServer authentication,
        ISipAuthorizationPolicy authorization)
    {
        SipProxy proxy = new SipProxy();
        SipNonceOptions options = new SipNonceOptions();
        options.Realm = "proxy.example.com";
        options.CurrentKey = new byte[] { 1, 2, 3, 4 };
        proxy.ConfigureProxySecurity(authentication, authorization, options);
        return proxy;
    }

    static SipProxyRequest Request(string callId, string additionalHeaders)
    {
        SipMessage message = Parse(
            "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-"
            + callId + ";rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + additionalHeaders
            + "Content-Length: 0\r\n\r\n");
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000);
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = "192.0.2.100";
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri("sip:bob@target.example.com");
        target.Candidate = candidate;
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.Stateless;
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Targets = new SipProxyTarget[] { target };
        request.Options = options;
        request.CorrelationId = callId;
        return request;
    }

    static SipMessage SecurityResponse(string callId, int status,
        string reason)
    {
        return Parse("SIP/2.0 " + status.ToString() + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-"
            + callId + ";rport\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=proxy-tag\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipConnectionInfo Connection(SipConnectionState state,
        bool secure, string identity)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = "peer-connection";
        connection.State = state;
        connection.Transport = SipTransportKind.Tls;
        connection.Secure = secure;
        connection.PeerIdentity = identity;
        return connection;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        return endpoint;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Uri!;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static void AssertEventContainsNoCredentials(SipProxyEvent value)
    {
        Assert.That(HasSensitiveHeader(value.Request!.Message!), Is.False);
        Assert.That(HasSensitiveHeader(value.Request.TransportMessage!.Message!),
            Is.False);
        Assert.That(HasSensitiveHeader(value.Message!), Is.False);
        Assert.That(HasSensitiveHeader(value.Result!.Request!), Is.False);
        Assert.That(HasSensitiveHeader(value.Result.SendRequest!.Message!),
            Is.False);
    }

    static bool HasSensitiveHeader(SipMessage message)
    {
        return HasHeader(message, SipHeaderNames.Authorization)
            || HasHeader(message, SipHeaderNames.ProxyAuthorization)
            || HasHeader(message, SipHeaderNames.AuthenticationInfo);
    }

    static bool HasHeader(SipMessage message, string name)
    {
        return message.Headers.Any(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    static string Header(SipMessage message, string name)
    {
        return message.Headers.Single(header => string.Equals(
            header.Name, name,
            System.StringComparison.OrdinalIgnoreCase)).Value!;
    }
}

public class NullProxyAuthorizationPolicy : ISipAuthorizationPolicy
{
    public int Calls;

    public SipAuthorizationDecision AuthorizeOperation(
        SipAuthenticationRequest request, SipCertificateIdentity? certificate,
        SipBearerToken? token)
    {
        Calls++;
        return null!;
    }
}
