namespace sip.Tests;

public class SipAuthenticationServerTests
{
    [Test]
    public void CreatesOriginChallengesInStrongestFirstOrder()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationRequest request = CreateRequest(
            CreateMessage("payload"), SipCredentialScope.Origin);

        SipAuthenticationResult result = server.CreateAuthenticationChallenge(
            request, CreateNonceOptions("example.com", 1));
        SipDigestChallenge[] challenges = ReadChallenges(
            result.Message!, SipHeaderNames.WwwAuthenticate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(result.RetryAllowed, Is.True);
            Assert.That(result.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(challenges, Has.Length.EqualTo(3));
            Assert.That(challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(challenges[1].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(challenges[2].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Md5));
            Assert.That(challenges[0].Nonce, Does.Match("^[A-Za-z0-9_-]{76}$"));
            Assert.That(challenges[0].Opaque, Does.Match("^[A-Za-z0-9_-]{43}$"));
            Assert.That(challenges[0].Nonce, Is.EqualTo(challenges[1].Nonce));
            Assert.That(challenges[0].Opaque, Is.EqualTo(challenges[2].Opaque));
            Assert.That(challenges[0].Qualities,
                Is.EqualTo(new SipDigestQuality[]
                    { SipDigestQuality.Auth, SipDigestQuality.AuthInt }));
            Assert.That(challenges[0].Utf8, Is.True);
        });
    }

    [Test]
    public void CreatesProxyChallengesUsingConfiguredAlgorithmPolicy()
    {
        SipAuthenticationServer server = new SipAuthenticationServer(
            new ServerCredentialProvider());
        SipAuthenticationRequest request = CreateRequest(
            CreateMessage(""), SipCredentialScope.Proxy);
        request.Challenges = new SipDigestChallenge[]
        {
            CreatePolicy(SipDigestAlgorithm.Md5, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha256Sess, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha512256, false, false)
        };

        SipAuthenticationResult result = server.CreateAuthenticationChallenge(
            request, CreateNonceOptions("proxy.example.com", 2));
        SipDigestChallenge[] challenges = ReadChallenges(
            result.Message!, SipHeaderNames.ProxyAuthenticate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.ProxyAuthenticationRequired));
            Assert.That(challenges, Has.Length.EqualTo(3));
            Assert.That(challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(challenges[1].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256Sess));
            Assert.That(challenges[2].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Md5));
            Assert.That(FindHeader(result.Message,
                SipHeaderNames.WwwAuthenticate), Is.Null);
        });
    }

    [Test]
    public void VerifiesDigestAndProducesAuthenticationInfo()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipMessage message = CreateMessage("payload");
        SipAuthenticationRequest challengeRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        challengeRequest.Challenges = new SipDigestChallenge[]
            { CreatePolicy(SipDigestAlgorithm.Sha256, false, false) };
        SipAuthenticationResult challenged = server.CreateAuthenticationChallenge(
            challengeRequest, CreateNonceOptions("example.com", 3));
        SipDigestChallenge[] challenges = ReadChallenges(
            challenged.Message!, SipHeaderNames.WwwAuthenticate);
        SipAuthenticationRequest authorizationRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        authorizationRequest.Challenges = challenges;
        SipAuthenticationResult authorized = client.AuthorizeRequest(
            authorizationRequest);
        SipAuthenticationRequest verification = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);

        SipAuthenticationResult verified = server.VerifyAuthentication(verification);
        string? informationValue = SipAuthenticationHeaderCodec
            .FormatAuthenticationInfoValue(verified.DigestAuthorization);
        SipAuthenticationRequest informationRequest =
            CreateInformationVerificationRequest(authorizationRequest,
                informationValue == null ? "" : informationValue);
        SipAuthenticationResult informationVerified =
            client.VerifyAuthenticationInfo(informationRequest);

        Assert.Multiple(() =>
        {
            Assert.That(authorized.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(verified.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(verified.Identity, Is.EqualTo("alice"));
            Assert.That(verified.DigestAuthorization!.Response,
                Does.Match("^[0-9a-f]{64}$"));
            Assert.That(verified.DigestAuthorization.NextNonce,
                Does.Match("^[A-Za-z0-9_-]{76}$"));
            Assert.That(informationValue, Does.Contain("rspauth="));
            Assert.That(informationValue, Does.Contain("nextnonce="));
            Assert.That(informationVerified.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(provider.ServerLookups, Is.EqualTo(1));
            Assert.That(provider.LastClaimedIdentity, Is.EqualTo("alice"));
        });
    }

    [TestCase(0, true)]
    [TestCase(2, false)]
    public void ReturnsStaleChallengeForExpiredAuthenticNonce(
        int attempt, bool retryAllowed)
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        ServerUnixTimeSource time = new ServerUnixTimeSource(1000);
        SipAuthenticationServerCore server = new SipAuthenticationServerCore(
            provider, new ServerSequentialRandomSource(), time,
            new SipManualMonotonicClock(0));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipMessage message = CreateMessage("");
        SipAuthenticationRequest challengeRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        challengeRequest.Challenges = new SipDigestChallenge[]
            { CreatePolicy(SipDigestAlgorithm.Sha256, false, false) };
        SipNonceOptions options = CreateNonceOptions("example.com", 4);
        options.LifetimeSeconds = 300;
        options.ClockSkewSeconds = 30;
        SipAuthenticationResult challenged = server.CreateChallenge(
            challengeRequest, options);
        SipAuthenticationRequest authorizationRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        authorizationRequest.Challenges = ReadChallenges(
            challenged.Message!, SipHeaderNames.WwwAuthenticate);
        SipAuthenticationResult authorized = client.AuthorizeRequest(
            authorizationRequest);
        time.UnixTimeSeconds = 1331;

        SipAuthenticationRequest staleRequest = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        staleRequest.Attempt = attempt;
        SipAuthenticationResult stale = server.Verify(staleRequest);
        SipDigestChallenge[] replacements = ReadChallenges(
            stale.Message!, SipHeaderNames.WwwAuthenticate);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Status,
                Is.EqualTo(SipAuthenticationStatus.StaleNonce));
            Assert.That(stale.RetryAllowed, Is.EqualTo(retryAllowed));
            Assert.That(stale.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(replacements[0].Stale, Is.True);
            Assert.That(replacements[0].HasStale, Is.True);
            Assert.That(replacements[0].Nonce,
                Is.Not.EqualTo(authorizationRequest.Challenges[0].Nonce));
            Assert.That(provider.ServerLookups, Is.EqualTo(0));
        });
    }

    [Test]
    public void DetectsARepeatedValidAuthorization()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationRequest verification = CreateAuthorizedRequest(
            server, provider, SipCredentialScope.Origin,
            CreatePolicy(SipDigestAlgorithm.Sha256, false, false), "payload", 5);

        SipAuthenticationResult first = server.VerifyAuthentication(verification);
        SipAuthenticationResult repeated = server.VerifyAuthentication(verification);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(repeated.Status,
                Is.EqualTo(SipAuthenticationStatus.ReplayDetected));
            Assert.That(repeated.Identity, Is.Null);
        });
    }

    [Test]
    public void VerifiesAuthIntAndUserHashAndRejectsChangedBody()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer validServer = new SipAuthenticationServer(provider);
        SipDigestChallenge policy = CreatePolicy(
            SipDigestAlgorithm.Sha256, true, true);
        SipAuthenticationRequest valid = CreateAuthorizedRequest(
            validServer, provider, SipCredentialScope.Origin,
            policy, "original body", 6);

        SipAuthenticationResult success =
            validServer.VerifyAuthentication(valid);

        SipAuthenticationServer changedServer = new SipAuthenticationServer(provider);
        SipAuthenticationRequest changed = CreateAuthorizedRequest(
            changedServer, provider, SipCredentialScope.Origin,
            policy, "original body", 7);
        changed.Message!.Body = System.Text.Encoding.ASCII.GetBytes("changed body");
        changed.Body = (byte[])changed.Message.Body.Clone();
        SipAuthenticationResult failure =
            changedServer.VerifyAuthentication(changed);

        Assert.Multiple(() =>
        {
            Assert.That(success.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(success.Identity, Is.EqualTo("alice"));
            Assert.That(failure.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(failure.Identity, Is.Null);
        });
    }

    [Test]
    public void KeepsOriginAndProxyProtectionSpacesSeparate()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "origin-secret", "origin.example", SipCredentialScope.Origin));
        provider.Credentials.Add(CreateCredential(
            "alice", "proxy-secret", "proxy.example", SipCredentialScope.Proxy));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipMessage message = CreateMessage("");
        SipAuthenticationRequest originChallenge = CreateRequest(
            message, SipCredentialScope.Origin);
        originChallenge.Challenges = new SipDigestChallenge[]
            { CreatePolicy(SipDigestAlgorithm.Sha256, false, false) };
        server.CreateAuthenticationChallenge(originChallenge,
            CreateNonceOptions("origin.example", 8));
        SipAuthenticationRequest proxyChallenge = CreateRequest(
            message, SipCredentialScope.Proxy);
        proxyChallenge.Challenges = new SipDigestChallenge[]
            { CreatePolicy(SipDigestAlgorithm.Sha256, false, false) };
        SipAuthenticationResult challenged = server.CreateAuthenticationChallenge(
            proxyChallenge, CreateNonceOptions("proxy.example", 9));
        SipAuthenticationRequest authorizeProxy = CreateRequest(
            message, SipCredentialScope.Proxy);
        authorizeProxy.Challenges = ReadChallenges(
            challenged.Message!, SipHeaderNames.ProxyAuthenticate);
        SipAuthenticationResult authorized = client.AuthorizeRequest(authorizeProxy);
        SipAuthenticationRequest originVerification = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        SipAuthenticationRequest proxyVerification = CreateRequest(
            authorized.Message!, SipCredentialScope.Proxy);

        SipAuthenticationResult origin =
            server.VerifyAuthentication(originVerification);
        SipAuthenticationResult proxy =
            server.VerifyAuthentication(proxyVerification);

        Assert.Multiple(() =>
        {
            Assert.That(origin.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(origin.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(proxy.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(FindHeader(authorized.Message!,
                SipHeaderNames.Authorization), Is.Null);
            Assert.That(FindHeader(authorized.Message!,
                SipHeaderNames.ProxyAuthorization), Is.Not.Null);
        });
    }

    [Test]
    public void RejectsTamperedOpaqueBeforeCredentialLookup()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationRequest verification = CreateAuthorizedRequest(
            server, provider, SipCredentialScope.Origin,
            CreatePolicy(SipDigestAlgorithm.Sha256, false, false), "", 11);
        SipHeader authorizationHeader = FindHeader(
            verification.Message!, SipHeaderNames.Authorization)!;
        SipAuthenticationHeaderResult parsed = SipAuthenticationHeaderCodec
            .ParseAuthenticationHeaderValue(SipHeaderNames.Authorization,
                authorizationHeader.Value, new SipParseOptions());
        parsed.Authorization!.Opaque = parsed.Authorization.Opaque + "A";
        authorizationHeader.Value = SipAuthenticationHeaderCodec
            .FormatDigestAuthorizationValue(parsed.Authorization);
        provider.ServerLookups = 0;

        SipAuthenticationResult result = server.VerifyAuthentication(verification);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.Identity, Is.Null);
            Assert.That(provider.ServerLookups, Is.EqualTo(0));
        });
    }

    [Test]
    public void CredentialProviderDenialReturnsRedactedFailure()
    {
        ServerCredentialProvider clientProvider = new ServerCredentialProvider();
        clientProvider.Credentials.Add(CreateCredential(
            "alice", "secret-value", "example.com", SipCredentialScope.Origin));
        ServerCredentialProvider serverProvider = new ServerCredentialProvider();
        SipAuthenticationServer server = new SipAuthenticationServer(serverProvider);
        SipAuthenticationRequest verification = CreateAuthorizedRequest(
            server, clientProvider, SipCredentialScope.Origin,
            CreatePolicy(SipDigestAlgorithm.Sha256, false, false), "", 10);

        SipAuthenticationResult result = server.VerifyAuthentication(verification);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.Identity, Is.Null);
            Assert.That(result.ErrorMessage, Does.Not.Contain("alice"));
            Assert.That(result.ErrorMessage, Does.Not.Contain("secret-value"));
            Assert.That(serverProvider.ServerLookups, Is.EqualTo(1));
        });
    }

    [Test]
    public void OrdersEveryConfiguredAlgorithmAndRemovesDuplicates()
    {
        SipAuthenticationServer server = new SipAuthenticationServer(
            new ServerCredentialProvider());
        SipAuthenticationRequest request = CreateRequest(
            CreateMessage(""), SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreatePolicy(SipDigestAlgorithm.Md5Sess, false, false),
            CreatePolicy(SipDigestAlgorithm.Md5, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha256Sess, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha256, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha512256Sess, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha512256, false, false),
            CreatePolicy(SipDigestAlgorithm.Sha256, false, false)
        };

        SipAuthenticationResult result = server.CreateAuthenticationChallenge(
            request, CreateNonceOptions("example.com", 12));
        SipDigestChallenge[] challenges = ReadChallenges(
            result.Message!, SipHeaderNames.WwwAuthenticate);

        Assert.Multiple(() =>
        {
            Assert.That(challenges, Has.Length.EqualTo(6));
            Assert.That(challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(challenges[1].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256Sess));
            Assert.That(challenges[2].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(challenges[3].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256Sess));
            Assert.That(challenges[4].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Md5));
            Assert.That(challenges[5].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Md5Sess));
        });
    }

    [TestCase(SipDigestAlgorithm.Md5, 32)]
    [TestCase(SipDigestAlgorithm.Md5Sess, 32)]
    [TestCase(SipDigestAlgorithm.Sha256, 64)]
    [TestCase(SipDigestAlgorithm.Sha256Sess, 64)]
    [TestCase(SipDigestAlgorithm.Sha512256, 64)]
    [TestCase(SipDigestAlgorithm.Sha512256Sess, 64)]
    public void VerifiesEverySupportedAlgorithm(
        SipDigestAlgorithm algorithm, int digestLength)
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationRequest verification = CreateAuthorizedRequest(
            server, provider, SipCredentialScope.Origin,
            CreatePolicy(algorithm, false, false), "payload", 13);

        SipAuthenticationResult result =
            server.VerifyAuthentication(verification);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.Identity, Is.EqualTo("alice"));
            Assert.That(result.DigestAuthorization!.Response,
                Has.Length.EqualTo(digestLength));
            Assert.That(result.DigestAuthorization.NextNonce,
                Does.Match("^[A-Za-z0-9_-]{76}$"));
        });
    }

    [Test]
    public void ReportsUnsupportedAndFailedVerificationStatuses()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationRequest unsupported = CreateAuthorizedRequest(
            server, provider, SipCredentialScope.Origin,
            CreatePolicy(SipDigestAlgorithm.Sha256, false, false), "", 14);
        SipHeader authorization = FindHeader(
            unsupported.Message!, SipHeaderNames.Authorization)!;
        SipAuthenticationHeaderResult parsed = SipAuthenticationHeaderCodec
            .ParseAuthenticationHeaderValue(SipHeaderNames.Authorization,
                authorization.Value, new SipParseOptions());
        parsed.Authorization!.Algorithm = SipDigestAlgorithm.Sha512256;
        parsed.Authorization.AlgorithmToken = "SHA-512-256";
        authorization.Value = SipAuthenticationHeaderCodec
            .FormatDigestAuthorizationValue(parsed.Authorization);
        SipAuthenticationRequest failed = CreateRequest(
            CreateMessage(""), SipCredentialScope.None);

        SipAuthenticationResult unsupportedResult =
            server.VerifyAuthentication(unsupported);
        SipAuthenticationResult failedResult =
            server.VerifyAuthentication(failed);

        Assert.Multiple(() =>
        {
            Assert.That(unsupportedResult.Status,
                Is.EqualTo(SipAuthenticationStatus.Unsupported));
            Assert.That(unsupportedResult.Identity, Is.Null);
            Assert.That(failedResult.Status,
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(failedResult.Identity, Is.Null);
        });
    }

    [Test]
    public void RejectsUnsupportedChallengePolicyWithoutRememberingIt()
    {
        SipAuthenticationServer server = new SipAuthenticationServer(
            new ServerCredentialProvider());
        SipAuthenticationRequest request = CreateRequest(
            CreateMessage(""), SipCredentialScope.Proxy);
        SipDigestChallenge unknown = CreatePolicy(
            SipDigestAlgorithm.Unknown, false, false);
        SipDigestChallenge invalidQuality = CreatePolicy(
            SipDigestAlgorithm.Sha256, false, false);
        invalidQuality.Qualities = new SipDigestQuality[]
            { (SipDigestQuality)99 };
        request.Challenges = new SipDigestChallenge[]
            { unknown, invalidQuality };

        SipAuthenticationResult rejected =
            server.CreateAuthenticationChallenge(request,
                CreateNonceOptions("proxy.example.com", 15));
        SipAuthenticationResult verification = server.VerifyAuthentication(
            CreateRequest(CreateMessage(""), SipCredentialScope.Proxy));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(SipAuthenticationStatus.Unsupported));
            Assert.That(rejected.Message, Is.Null);
            Assert.That(verification.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(verification.Message, Is.Null);
        });
    }

    [Test]
    public void ServerUsesExplicitAuthenticationBodyForAuthInt()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipMessage message = CreateMessage("wire body is unavailable");
        SipAuthenticationRequest challengeRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        challengeRequest.Challenges = new SipDigestChallenge[]
        {
            CreatePolicy(SipDigestAlgorithm.Sha256, true, false)
        };
        SipAuthenticationResult challenged = server.CreateAuthenticationChallenge(
            challengeRequest, CreateNonceOptions("example.com", 16));
        SipAuthenticationRequest authorizationRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        authorizationRequest.Body = new byte[0];
        authorizationRequest.Challenges = ReadChallenges(
            challenged.Message!, SipHeaderNames.WwwAuthenticate);
        SipAuthenticationResult authorized =
            client.AuthorizeRequest(authorizationRequest);
        SipAuthenticationRequest verification = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        verification.Body = new byte[0];

        SipAuthenticationResult result =
            server.VerifyAuthentication(verification);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.Identity, Is.EqualTo("alice"));
            Assert.That(result.DigestAuthorization!.Quality,
                Is.EqualTo(SipDigestQuality.AuthInt));
        });
    }

    [Test]
    public void UserHashLookupReceivesHashAndReturnsEstablishedIdentity()
    {
        ServerCredentialProvider provider = new ServerCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "password", "example.com", SipCredentialScope.Origin));
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationRequest verification = CreateAuthorizedRequest(
            server, provider, SipCredentialScope.Origin,
            CreatePolicy(SipDigestAlgorithm.Sha512256, false, true), "", 17);

        SipAuthenticationResult result =
            server.VerifyAuthentication(verification);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.Identity, Is.EqualTo("alice"));
            Assert.That(provider.LastClaimedIdentity,
                Does.Match("^[0-9a-f]{64}$"));
            Assert.That(provider.LastClaimedIdentity,
                Is.Not.EqualTo("alice"));
        });
    }

    static SipAuthenticationRequest CreateAuthorizedRequest(
        SipAuthenticationServer server, ServerCredentialProvider clientProvider,
        SipCredentialScope scope, SipDigestChallenge policy, string body,
        byte keySeed)
    {
        SipMessage message = CreateMessage(body);
        SipAuthenticationRequest challengeRequest = CreateRequest(message, scope);
        challengeRequest.Challenges = new SipDigestChallenge[] { policy };
        SipAuthenticationResult challenged = server.CreateAuthenticationChallenge(
            challengeRequest, CreateNonceOptions("example.com", keySeed));
        SipAuthenticationRequest authorizationRequest = CreateRequest(message, scope);
        authorizationRequest.Challenges = ReadChallenges(challenged.Message!,
            scope == SipCredentialScope.Proxy
                ? SipHeaderNames.ProxyAuthenticate
                : SipHeaderNames.WwwAuthenticate);
        SipAuthenticationResult authorized = new SipAuthenticationClient(clientProvider)
            .AuthorizeRequest(authorizationRequest);
        return CreateRequest(authorized.Message!, scope);
    }

    static SipAuthenticationRequest CreateInformationVerificationRequest(
        SipAuthenticationRequest authorized, string informationValue)
    {
        SipHeader information = new SipHeader();
        information.Name = SipHeaderNames.AuthenticationInfo;
        information.Value = informationValue;
        SipMessage response = new SipMessage();
        response.Kind = SipMessageKind.Response;
        response.StatusCode = SipStatusCodes.Ok;
        response.StatusClass = SipStatusClass.Successful;
        response.Headers = new SipHeader[] { information };
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = response;
        request.Target = authorized.Target;
        request.Scope = authorized.Scope;
        request.CorrelationId = authorized.CorrelationId;
        return request;
    }

    static SipAuthenticationRequest CreateRequest(
        SipMessage message, SipCredentialScope scope)
    {
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = message;
        request.Target = message.RequestUri;
        request.Body = message.Body == null
            ? new byte[0] : (byte[])message.Body.Clone();
        request.Scope = scope;
        request.CorrelationId = "server-authentication-test";
        return request;
    }

    static SipMessage CreateMessage(string body)
    {
        byte[] bodyBytes = System.Text.Encoding.ASCII.GetBytes(body);
        string text = "MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bKserver\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: server-auth@example.com\r\n"
            + "CSeq: 1 MESSAGE\r\n"
            + "Content-Type: text/plain\r\n"
            + "Content-Length: " + bodyBytes.Length + "\r\n\r\n"
            + body;
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.Strict = false;
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), parseOptions);
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipDigestChallenge[] ReadChallenges(
        SipMessage message, string headerName)
    {
        System.Collections.Generic.List<SipDigestChallenge> challenges =
            new System.Collections.Generic.List<SipDigestChallenge>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (string.Equals(header.Name, headerName,
                StringComparison.OrdinalIgnoreCase))
            {
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        headerName, header.Value, new SipParseOptions());
                Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
                challenges.Add(parsed.Challenge!);
            }
            index = index + 1;
        }
        return challenges.ToArray();
    }

    static SipDigestChallenge CreatePolicy(SipDigestAlgorithm algorithm,
        bool authInt, bool userHash)
    {
        SipDigestChallenge policy = new SipDigestChallenge();
        policy.Algorithm = algorithm;
        policy.Qualities = new SipDigestQuality[]
            { authInt ? SipDigestQuality.AuthInt : SipDigestQuality.Auth };
        policy.Utf8 = true;
        policy.UserHash = userHash;
        policy.HasUserHash = userHash;
        return policy;
    }

    static SipNonceOptions CreateNonceOptions(string realm, byte keySeed)
    {
        SipNonceOptions options = new SipNonceOptions();
        options.Realm = realm;
        options.CurrentKey = new byte[32];
        int index = 0;
        while (index < options.CurrentKey.Length)
        {
            options.CurrentKey[index] = (byte)(keySeed + index);
            index = index + 1;
        }
        return options;
    }

    static SipCredential CreateCredential(string userName, string password,
        string realm, SipCredentialScope scope)
    {
        SipCredential credential = new SipCredential();
        credential.UserName = userName;
        credential.Secret = System.Text.Encoding.UTF8.GetBytes(password);
        credential.Realm = realm;
        credential.Scope = scope;
        return credential;
    }

    static SipHeader? FindHeader(SipMessage message, string name)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            if (string.Equals(message.Headers[index].Name, name,
                StringComparison.OrdinalIgnoreCase))
            {
                return message.Headers[index];
            }
            index = index + 1;
        }
        return null;
    }
}

class ServerCredentialProvider : ISipCredentialProvider
{
    internal System.Collections.Generic.List<SipCredential> Credentials =
        new System.Collections.Generic.List<SipCredential>();
    internal int ServerLookups;
    internal string? LastClaimedIdentity;

    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        string? realm = request.Challenges.Length == 0
            ? null : request.Challenges[0].Realm;
        string? claimed = request.Credential == null
            ? null : request.Credential.UserName;
        if (claimed != null)
        {
            ServerLookups = ServerLookups + 1;
            LastClaimedIdentity = claimed;
        }
        int index = 0;
        while (index < Credentials.Count)
        {
            SipCredential credential = Credentials[index];
            if (credential.Scope == request.Scope && credential.Realm == realm)
            {
                return credential;
            }
            index = index + 1;
        }
        return new SipCredential();
    }
}

class ServerUnixTimeSource : ISipUnixTimeSource
{
    internal long UnixTimeSeconds;

    internal ServerUnixTimeSource(long unixTimeSeconds)
    {
        UnixTimeSeconds = unixTimeSeconds;
    }

    public long GetUnixTimeSeconds()
    {
        return UnixTimeSeconds;
    }
}

class ServerSequentialRandomSource : ISipRandomSource
{
    int next;

    public void FillBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)next;
            next = (next + 1) % 256;
            index = index + 1;
        }
    }
}
