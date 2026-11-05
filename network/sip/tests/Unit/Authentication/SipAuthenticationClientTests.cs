namespace sip.Tests;

public class SipAuthenticationClientTests
{
    [Test]
    public void ChoosesStrongestAlgorithmAndAuthIntQuality()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Md5, "realm-a", "md5", false),
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "sha", false),
            CreateChallenge(SipDigestAlgorithm.Sha512256, "realm-a", "sha512", true)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.RetryAllowed, Is.True);
            Assert.That(result.DigestAuthorization!.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(result.DigestAuthorization.Quality,
                Is.EqualTo(SipDigestQuality.AuthInt));
            Assert.That(result.DigestAuthorization.NonceCount, Is.EqualTo(1));
            Assert.That(result.DigestAuthorization.UserHash, Is.True);
            Assert.That(result.DigestAuthorization.UserName,
                Does.Match("^[0-9a-f]{64}$"));
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(provider.Requests[0].Challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(provider.Requests[0].Target!.Host,
                Is.EqualTo("example.com"));
            Assert.That(FindHeader(result.Message!, SipHeaderNames.Authorization),
                Is.Not.Null);
            Assert.That(FindHeader(result.Message!, SipHeaderNames.ProxyAuthorization),
                Is.Null);
            Assert.That(request.Message!.Headers, Is.Empty);
        });
    }

    [Test]
    public void LooksAcrossRealmsWithoutDowngradingWithinARealm()
    {
        AuthenticationCredentialProvider provider = new AuthenticationCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "secret-b", "realm-b", SipCredentialScope.Origin));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Md5, "realm-a", "weak", false),
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-b", "medium", false),
            CreateChallenge(SipDigestAlgorithm.Sha512256, "realm-a", "strong", false)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization!.Realm, Is.EqualTo("realm-b"));
            Assert.That(result.DigestAuthorization.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
            Assert.That(provider.Requests[0].Challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(provider.Requests[1].Challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256));
        });
    }

    [Test]
    public void KeepsOriginAndProxyCachesAndHeadersIndependent()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        provider.Credentials.Add(CreateCredential(
            "proxy-user", "proxy-secret", "realm-a", SipCredentialScope.Proxy));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest origin = CreateRequest(SipCredentialScope.Origin);
        origin.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "origin", false)
        };
        SipAuthenticationRequest proxy = CreateRequest(SipCredentialScope.Proxy);
        proxy.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Md5, "realm-a", "proxy", false)
        };

        SipAuthenticationResult firstOrigin = client.AuthorizeRequest(origin);
        SipAuthenticationResult firstProxy = client.AuthorizeRequest(proxy);
        origin.Challenges = new SipDigestChallenge[0];
        proxy.Challenges = new SipDigestChallenge[0];
        SipAuthenticationResult secondOrigin = client.AuthorizeRequest(origin);
        SipAuthenticationResult secondProxy = client.AuthorizeRequest(proxy);

        Assert.Multiple(() =>
        {
            Assert.That(firstOrigin.DigestAuthorization!.Nonce, Is.EqualTo("origin"));
            Assert.That(firstProxy.DigestAuthorization!.Nonce, Is.EqualTo("proxy"));
            Assert.That(secondOrigin.DigestAuthorization!.NonceCount, Is.EqualTo(2));
            Assert.That(secondOrigin.DigestAuthorization.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(secondProxy.DigestAuthorization!.NonceCount, Is.EqualTo(2));
            Assert.That(secondProxy.DigestAuthorization.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Md5));
            Assert.That(FindHeader(secondOrigin.Message!, SipHeaderNames.Authorization),
                Is.Not.Null);
            Assert.That(FindHeader(secondOrigin.Message!, SipHeaderNames.ProxyAuthorization),
                Is.Null);
            Assert.That(FindHeader(secondProxy.Message!, SipHeaderNames.ProxyAuthorization),
                Is.Not.Null);
            Assert.That(FindHeader(secondProxy.Message!, SipHeaderNames.Authorization),
                Is.Null);
        });
    }

    [Test]
    public void ParsesRawChallengeHeadersWhenModelsAreNotSupplied()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        SipHeader challenge = new SipHeader();
        challenge.Name = "wWw-AuThEnTiCaTe";
        challenge.Value = "Digest realm=\"realm-a\", nonce=\"raw-nonce\", "
            + "algorithm=SHA-256, qop=\"auth\"";
        request.Message!.Headers = new SipHeader[] { challenge };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization!.Nonce,
                Is.EqualTo("raw-nonce"));
            Assert.That(result.DigestAuthorization.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(result.DigestAuthorization.Quality,
                Is.EqualTo(SipDigestQuality.Auth));
        });
    }

    [Test]
    public void VerifiesRspauthThenAppliesNextNonceToCache()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "nonce-1", true)
        };
        SipAuthenticationResult authorized = client.AuthorizeRequest(request);
        byte[] responseBody = System.Text.Encoding.ASCII.GetBytes("response entity");
        string rspauth = CalculateRspauth(
            authorized.DigestAuthorization!, provider.Credentials[0], request,
            responseBody);
        SipAuthenticationRequest verification = CreateVerificationRequest(
            request, authorized.DigestAuthorization!, rspauth, "nonce-2",
            responseBody);

        SipAuthenticationResult verified =
            client.VerifyAuthenticationInfo(verification);
        request.Challenges = new SipDigestChallenge[0];
        request.Attempt = 1;
        SipAuthenticationResult next = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(verified.Identity, Is.EqualTo("alice"));
            Assert.That(verified.DigestAuthorization!.NextNonce,
                Is.EqualTo("nonce-2"));
            Assert.That(next.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(next.DigestAuthorization!.Nonce, Is.EqualTo("nonce-2"));
            Assert.That(next.DigestAuthorization.NonceCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void StaleChallengeResetsNonceCountAndSession()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256Sess,
                "realm-a", "nonce-1", false)
        };
        SipAuthenticationResult first = client.AuthorizeRequest(request);
        SipDigestChallenge stale = CreateChallenge(
            SipDigestAlgorithm.Sha256Sess, "realm-a", "nonce-3", false);
        stale.Stale = true;
        stale.HasStale = true;
        request.Challenges = new SipDigestChallenge[] { stale };
        request.Attempt = 1;

        SipAuthenticationResult retried = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.DigestAuthorization!.NonceCount, Is.EqualTo(1));
            Assert.That(retried.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(retried.DigestAuthorization!.Nonce, Is.EqualTo("nonce-3"));
            Assert.That(retried.DigestAuthorization.NonceCount, Is.EqualTo(1));
            Assert.That(retried.RetryAllowed, Is.True);
        });
    }

    [Test]
    public void InvalidRspauthInvalidatesCachedChallenge()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "nonce-1", false)
        };
        SipAuthenticationResult authorized = client.AuthorizeRequest(request);
        SipAuthenticationRequest verification = CreateVerificationRequest(
            request, authorized.DigestAuthorization!, new string('0', 64),
            "untrusted-next", new byte[0]);

        SipAuthenticationResult failed =
            client.VerifyAuthenticationInfo(verification);
        request.Challenges = new SipDigestChallenge[0];
        request.Attempt = 1;
        SipAuthenticationResult afterFailure = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(afterFailure.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
        });
    }

    [Test]
    public void ProxyVerificationPrefersProxyAuthenticationInfo()
    {
        AuthenticationCredentialProvider provider = new AuthenticationCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "proxy-user", "proxy-secret", "realm-a", SipCredentialScope.Proxy));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Proxy);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "proxy-nonce", false)
        };
        SipAuthenticationResult authorized = client.AuthorizeRequest(request);
        string rspauth = CalculateRspauth(
            authorized.DigestAuthorization!, provider.Credentials[0], request,
            new byte[0]);
        SipAuthenticationRequest verification = CreateVerificationRequest(
            request, authorized.DigestAuthorization!, rspauth, "proxy-next",
            new byte[0]);
        SipHeader proxyInformation = verification.Message!.Headers[0];
        proxyInformation.Name = "Proxy-Authentication-Info";
        SipHeader originInformation = new SipHeader();
        originInformation.Name = SipHeaderNames.AuthenticationInfo;
        originInformation.Value = proxyInformation.Value;
        verification.Message.Headers = new SipHeader[]
            { originInformation, proxyInformation };

        SipAuthenticationResult result =
            client.VerifyAuthenticationInfo(verification);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization!.NextNonce,
                Is.EqualTo("proxy-next"));
            Assert.That(result.Identity, Is.EqualTo("proxy-user"));
        });
    }

    [Test]
    public void ExplicitEmptyAuthIntBodyOverridesMessageBody()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "nonce", true)
        };
        request.Body = new byte[0];

        SipAuthenticationResult result = client.AuthorizeRequest(request);
        SipDigestAuthorization authorization = result.DigestAuthorization!;
        SipDigestCalculation calculation = CreateCalculation(
            authorization, provider.Credentials[0], request, new byte[0]);
        calculation.RequestBody = new byte[0];
        string emptyResponse = SipDigestCalculator.Calculate(calculation).Response;
        calculation.RequestBody = (byte[])request.Message!.Body.Clone();
        string messageBodyResponse =
            SipDigestCalculator.Calculate(calculation).Response;

        Assert.Multiple(() =>
        {
            Assert.That(authorization.Response, Is.EqualTo(emptyResponse));
            Assert.That(authorization.Response, Is.Not.EqualTo(messageBodyResponse));
        });
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void EnforcesRetryBoundBeforeCredentialLookup(int attempt)
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Attempt = attempt;
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "nonce", false)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(result.RetryAllowed, Is.False);
            Assert.That(provider.Requests, Is.Empty);
        });
    }

    [Test]
    public void ReportsMissingUnsupportedAndMalformedChallenges()
    {
        AuthenticationCredentialProvider provider = new AuthenticationCredentialProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);

        SipAuthenticationResult missing = client.AuthorizeRequest(request);
        SipDigestChallenge unsupported = CreateChallenge(
            SipDigestAlgorithm.Unknown, "realm-a", "nonce", false);
        unsupported.AlgorithmToken = "FUTURE";
        request.Challenges = new SipDigestChallenge[] { unsupported };
        SipAuthenticationResult unknown = client.AuthorizeRequest(request);
        request.Challenges = new SipDigestChallenge[0];
        SipHeader malformed = new SipHeader();
        malformed.Name = SipHeaderNames.WwwAuthenticate;
        malformed.Value = "Digest realm=\"realm-a\"";
        request.Message!.Headers = new SipHeader[] { malformed };
        SipAuthenticationResult invalid = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(unknown.Status,
                Is.EqualTo(SipAuthenticationStatus.Unsupported));
            Assert.That(invalid.Status,
                Is.EqualTo(SipAuthenticationStatus.Unsupported));
            Assert.That(invalid.ErrorMessage, Does.Contain("malformed"));
        });
    }

    [Test]
    public void ReportsMissingRealmScopedCredential()
    {
        AuthenticationCredentialProvider provider = new AuthenticationCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "secret", "other-realm", SipCredentialScope.Origin));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256, "realm-a", "nonce", false)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.RetryAllowed, Is.False);
            Assert.That(result.Message, Is.Null);
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void StrongestChallengeWinsAtEveryWirePosition(int strongestIndex)
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin);
        SipDigestChallenge[] challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Md5,
                "realm-a", "md5", false),
            CreateChallenge(SipDigestAlgorithm.Sha256,
                "realm-a", "sha256", false),
            CreateChallenge(SipDigestAlgorithm.Sha512256,
                "realm-a", "sha512", false)
        };
        SipDigestChallenge strongest = challenges[2];
        challenges[2] = challenges[strongestIndex];
        challenges[strongestIndex] = strongest;
        request.Challenges = challenges;

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization!.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(result.DigestAuthorization.Nonce,
                Is.EqualTo("sha512"));
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void FallsBackOnlyWhenStrongerOffersAreActuallyUnsupported()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin);
        SipDigestChallenge unknown = CreateChallenge(
            SipDigestAlgorithm.Unknown, "realm-a", "unknown", false);
        unknown.AlgorithmToken = "FUTURE-DIGEST";
        SipDigestChallenge mismatched = CreateChallenge(
            SipDigestAlgorithm.Sha512256, "realm-a", "mismatch", false);
        mismatched.AlgorithmToken = "MD5";
        SipDigestChallenge unsupportedQuality = CreateChallenge(
            SipDigestAlgorithm.Sha256, "realm-a", "quality", false);
        unsupportedQuality.Qualities = new SipDigestQuality[]
            { (SipDigestQuality)99 };
        unsupportedQuality.QualityTokens = new string[] { "future-qop" };
        request.Challenges = new SipDigestChallenge[]
        {
            unknown,
            mismatched,
            unsupportedQuality,
            CreateChallenge(SipDigestAlgorithm.Md5,
                "realm-a", "supported", false)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization!.Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Md5));
            Assert.That(result.DigestAuthorization.Nonce,
                Is.EqualTo("supported"));
        });
    }

    [Test]
    public void DoesNotDowngradeWithinRealmAfterStrongCredentialLookupFails()
    {
        AlgorithmSelectiveCredentialProvider provider =
            new AlgorithmSelectiveCredentialProvider();
        provider.AcceptedAlgorithm = SipDigestAlgorithm.Md5;
        provider.Credential = CreateCredential(
            "alice", "secret", "realm-a", SipCredentialScope.Origin);
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Md5,
                "realm-a", "weak", false),
            CreateChallenge(SipDigestAlgorithm.Sha512256,
                "realm-a", "strong", false)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.DigestAuthorization, Is.Null);
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(provider.Requests[0].Challenges[0].Algorithm,
                Is.EqualTo(SipDigestAlgorithm.Sha512256));
        });
    }

    [Test]
    public void RawOriginAndProxyChallengesRemainScopeSeparated()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        provider.Credentials.Add(CreateCredential(
            "proxy-user", "proxy-secret", "realm-proxy",
            SipCredentialScope.Proxy));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest origin = CreateRequest(
            SipCredentialScope.Origin);
        SipHeader originChallenge = new SipHeader();
        originChallenge.Name = SipHeaderNames.WwwAuthenticate;
        originChallenge.Value = "Digest realm=\"realm-a\", "
            + "nonce=\"origin-raw\", algorithm=SHA-256, qop=\"auth\"";
        SipHeader proxyChallenge = new SipHeader();
        proxyChallenge.Name = SipHeaderNames.ProxyAuthenticate;
        proxyChallenge.Value = "Digest realm=\"realm-proxy\", "
            + "nonce=\"proxy-raw\", algorithm=MD5, qop=\"auth\"";
        origin.Message!.Headers = new SipHeader[]
            { proxyChallenge, originChallenge };
        SipAuthenticationRequest proxy = CreateRequest(
            SipCredentialScope.Proxy);
        proxy.Message!.Headers = new SipHeader[]
            { originChallenge, proxyChallenge };

        SipAuthenticationResult originResult =
            client.AuthorizeRequest(origin);
        SipAuthenticationResult proxyResult =
            client.AuthorizeRequest(proxy);

        Assert.Multiple(() =>
        {
            Assert.That(originResult.DigestAuthorization!.Nonce,
                Is.EqualTo("origin-raw"));
            Assert.That(originResult.Identity, Is.EqualTo("alice"));
            Assert.That(FindHeader(originResult.Message!,
                SipHeaderNames.Authorization), Is.Not.Null);
            Assert.That(FindHeader(originResult.Message!,
                SipHeaderNames.ProxyAuthorization), Is.Null);
            Assert.That(proxyResult.DigestAuthorization!.Nonce,
                Is.EqualTo("proxy-raw"));
            Assert.That(proxyResult.Identity, Is.EqualTo("proxy-user"));
            Assert.That(FindHeader(proxyResult.Message!,
                SipHeaderNames.ProxyAuthorization), Is.Not.Null);
            Assert.That(FindHeader(proxyResult.Message!,
                SipHeaderNames.Authorization), Is.Null);
        });
    }

    [Test]
    public void CachedChallengesAreTargetScopedAndNeverOverrideSuppliedOffers()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest priming = CreateRequest(
            SipCredentialScope.Origin);
        priming.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256,
                "realm-a", "cached", false)
        };
        SipAuthenticationResult primed = client.AuthorizeRequest(priming);
        SipAuthenticationRequest otherTarget = CreateRequest(
            SipCredentialScope.Origin);
        otherTarget.Target!.Host = "other.example.com";
        otherTarget.Target.OriginalText = "sip:bob@other.example.com";
        otherTarget.Message!.RequestUri = otherTarget.Target;

        SipAuthenticationResult isolated =
            client.AuthorizeRequest(otherTarget);
        SipDigestChallenge unsupported = CreateChallenge(
            SipDigestAlgorithm.Unknown, "realm-a", "future", false);
        unsupported.AlgorithmToken = "FUTURE-DIGEST";
        priming.Challenges = new SipDigestChallenge[] { unsupported };
        SipAuthenticationResult supplied = client.AuthorizeRequest(priming);

        Assert.Multiple(() =>
        {
            Assert.That(primed.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(isolated.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(supplied.Status,
                Is.EqualTo(SipAuthenticationStatus.Unsupported));
            Assert.That(supplied.DigestAuthorization, Is.Null);
        });
    }

    [Test]
    public void RetryAllowanceStopsAtMaximumAttempt()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin);
        request.Attempt = 2;
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256,
                "realm-a", "last-attempt", false)
        };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.RetryAllowed, Is.False);
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AuthenticationInfoExchangeMismatchIgnoresUntrustedNextNonce()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256,
                "realm-a", "nonce-1", false)
        };
        SipAuthenticationResult authorized = client.AuthorizeRequest(request);
        string rspauth = CalculateRspauth(
            authorized.DigestAuthorization!, provider.Credentials[0], request,
            new byte[0]);
        SipAuthenticationRequest verification = CreateVerificationRequest(
            request, authorized.DigestAuthorization!, rspauth, "nonce-2",
            new byte[0]);
        verification.Message!.Headers[0].Value =
            verification.Message.Headers[0].Value!.Replace(
                authorized.DigestAuthorization!.ClientNonce!, "wrong-cnonce");

        SipAuthenticationResult failed =
            client.VerifyAuthenticationInfo(verification);
        request.Challenges = new SipDigestChallenge[0];
        request.Attempt = 1;
        SipAuthenticationResult afterFailure = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(failed.ErrorMessage,
                Does.Contain("No cached Digest exchange"));
            Assert.That(afterFailure.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(afterFailure.DigestAuthorization!.Nonce,
                Is.EqualTo("nonce-1"));
        });
    }

    [Test]
    public void InvalidNextNonceFailsAuthenticationInfoAndClearsCache()
    {
        AuthenticationCredentialProvider provider = CreateProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            SipCredentialScope.Origin);
        request.Challenges = new SipDigestChallenge[]
        {
            CreateChallenge(SipDigestAlgorithm.Sha256,
                "realm-a", "nonce-1", false)
        };
        SipAuthenticationResult authorized = client.AuthorizeRequest(request);
        string rspauth = CalculateRspauth(
            authorized.DigestAuthorization!, provider.Credentials[0], request,
            new byte[0]);
        SipAuthenticationRequest verification = CreateVerificationRequest(
            request, authorized.DigestAuthorization!, rspauth, "nönce-2",
            new byte[0]);

        SipAuthenticationResult failed =
            client.VerifyAuthenticationInfo(verification);
        request.Challenges = new SipDigestChallenge[0];
        request.Attempt = 1;
        SipAuthenticationResult afterFailure = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(failed.ErrorMessage, Does.Contain("nextnonce"));
            Assert.That(afterFailure.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
        });
    }

    [Test]
    public void EmitsUtf8ExtendedUserNameWhenUserhashIsNotRequested()
    {
        AuthenticationCredentialProvider provider = new AuthenticationCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "Ja\u0308søn Doe", "Secret, or not?", "realm-a",
            SipCredentialScope.Origin));
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(SipCredentialScope.Origin);
        SipDigestChallenge challenge = CreateChallenge(
            SipDigestAlgorithm.Sha512256, "realm-a", "nonce", false);
        challenge.Utf8 = true;
        request.Challenges = new SipDigestChallenge[] { challenge };

        SipAuthenticationResult result = client.AuthorizeRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization!.UserName, Is.Null);
            Assert.That(result.DigestAuthorization.UserNameExtended,
                Is.EqualTo("UTF-8''J%C3%A4s%C3%B8n%20Doe"));
            Assert.That(FindHeader(result.Message!, SipHeaderNames.Authorization)!.Value,
                Does.Contain("username*=UTF-8''J%C3%A4s%C3%B8n%20Doe"));
        });
    }

    static AuthenticationCredentialProvider CreateProvider()
    {
        AuthenticationCredentialProvider provider =
            new AuthenticationCredentialProvider();
        provider.Credentials.Add(CreateCredential(
            "alice", "secret", "realm-a", SipCredentialScope.Origin));
        return provider;
    }

    static SipCredential CreateCredential(string userName, string secret,
        string realm, SipCredentialScope scope)
    {
        SipCredential credential = new SipCredential();
        credential.UserName = userName;
        credential.Secret = System.Text.Encoding.UTF8.GetBytes(secret);
        credential.Realm = realm;
        credential.Scope = scope;
        return credential;
    }

    static SipAuthenticationRequest CreateRequest(SipCredentialScope scope)
    {
        SipUri uri = CreateTarget();
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Message;
        message.MethodToken = SipMethods.Message;
        message.RequestUri = uri;
        message.Body = System.Text.Encoding.ASCII.GetBytes("request entity");

        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = message;
        request.Target = uri;
        request.Body = (byte[])message.Body.Clone();
        request.Scope = scope;
        request.CorrelationId = scope == SipCredentialScope.Proxy
            ? "proxy-correlation" : "origin-correlation";
        return request;
    }

    static SipUri CreateTarget()
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sip;
        uri.SchemeToken = "sip";
        uri.User = "bob";
        uri.Host = "example.com";
        uri.OriginalText = "sip:bob@example.com";
        return uri;
    }

    static SipDigestChallenge CreateChallenge(SipDigestAlgorithm algorithm,
        string realm, string nonce, bool authInt)
    {
        SipDigestChallenge challenge = new SipDigestChallenge();
        challenge.Scheme = "Digest";
        challenge.Realm = realm;
        challenge.Nonce = nonce;
        challenge.Algorithm = algorithm;
        challenge.AlgorithmToken = AlgorithmToken(algorithm);
        challenge.Qualities = authInt
            ? new SipDigestQuality[]
                { SipDigestQuality.Auth, SipDigestQuality.AuthInt }
            : new SipDigestQuality[] { SipDigestQuality.Auth };
        challenge.QualityTokens = authInt
            ? new string[] { "auth", "auth-int" }
            : new string[] { "auth" };
        challenge.UserHash = authInt;
        challenge.HasUserHash = authInt;
        return challenge;
    }

    static string AlgorithmToken(SipDigestAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case SipDigestAlgorithm.Md5: return "MD5";
            case SipDigestAlgorithm.Md5Sess: return "MD5-sess";
            case SipDigestAlgorithm.Sha256: return "SHA-256";
            case SipDigestAlgorithm.Sha256Sess: return "SHA-256-sess";
            case SipDigestAlgorithm.Sha512256: return "SHA-512-256";
            case SipDigestAlgorithm.Sha512256Sess: return "SHA-512-256-sess";
            default: return "FUTURE";
        }
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
            index++;
        }
        return null;
    }

    static string CalculateRspauth(SipDigestAuthorization authorization,
        SipCredential credential, SipAuthenticationRequest request,
        byte[] responseBody)
    {
        SipDigestCalculation calculation = CreateCalculation(
            authorization, credential, request, responseBody);
        return SipDigestCalculator.Calculate(calculation).ResponseAuthentication;
    }

    static SipDigestCalculation CreateCalculation(
        SipDigestAuthorization authorization, SipCredential credential,
        SipAuthenticationRequest request, byte[] responseBody)
    {
        SipDigestCalculation calculation = new SipDigestCalculation();
        calculation.Algorithm = authorization.Algorithm;
        calculation.Quality = authorization.Quality;
        calculation.UserName = credential.UserName;
        calculation.Realm = authorization.Realm;
        calculation.Secret = (byte[])credential.Secret.Clone();
        calculation.Nonce = authorization.Nonce;
        calculation.NonceCount = authorization.NonceCount;
        calculation.ClientNonce = authorization.ClientNonce;
        calculation.Method = SipMethods.Message;
        calculation.DigestUri = authorization.DigestUri;
        calculation.RequestBody = (byte[])request.Body.Clone();
        calculation.ResponseBody = (byte[])responseBody.Clone();
        calculation.Utf8 = request.Challenges[0].Utf8;
        return calculation;
    }

    static SipAuthenticationRequest CreateVerificationRequest(
        SipAuthenticationRequest authorizedRequest,
        SipDigestAuthorization authorization, string rspauth,
        string nextNonce, byte[] responseBody)
    {
        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.AuthenticationInfo;
        header.Value = "nextnonce=\"" + nextNonce + "\", qop="
            + authorization.QualityToken + ", rspauth=\"" + rspauth
            + "\", cnonce=\"" + authorization.ClientNonce + "\", nc="
            + authorization.NonceCount.ToString(
                "x8", System.Globalization.CultureInfo.InvariantCulture);
        SipMessage response = new SipMessage();
        response.Kind = SipMessageKind.Response;
        response.StatusCode = 200;
        response.StatusClass = SipStatusClass.Successful;
        response.Headers = new SipHeader[] { header };
        response.Body = (byte[])responseBody.Clone();

        SipAuthenticationRequest verification = new SipAuthenticationRequest();
        verification.Message = response;
        verification.Target = authorizedRequest.Target;
        verification.Body = (byte[])responseBody.Clone();
        verification.Scope = authorizedRequest.Scope;
        verification.CorrelationId = authorizedRequest.CorrelationId;
        return verification;
    }
}

class AuthenticationCredentialProvider : ISipCredentialProvider
{
    internal System.Collections.Generic.List<SipCredential> Credentials =
        new System.Collections.Generic.List<SipCredential>();
    internal System.Collections.Generic.List<SipAuthenticationRequest> Requests =
        new System.Collections.Generic.List<SipAuthenticationRequest>();

    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        Requests.Add(request);
        string? realm = request.Challenges.Length == 0
            ? null : request.Challenges[0].Realm;
        int index = 0;
        while (index < Credentials.Count)
        {
            SipCredential credential = Credentials[index];
            if (credential.Scope == request.Scope && credential.Realm == realm)
            {
                return credential;
            }
            index++;
        }
        return new SipCredential();
    }
}

class AlgorithmSelectiveCredentialProvider : ISipCredentialProvider
{
    internal SipDigestAlgorithm AcceptedAlgorithm;
    internal SipCredential Credential = new SipCredential();
    internal System.Collections.Generic.List<SipAuthenticationRequest> Requests =
        new System.Collections.Generic.List<SipAuthenticationRequest>();

    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        Requests.Add(request);
        if (request.Challenges.Length > 0
            && request.Challenges[0].Algorithm == AcceptedAlgorithm)
        {
            return Credential;
        }
        return new SipCredential();
    }
}
