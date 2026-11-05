namespace sip;

internal class SipAuthenticationServerCore
{
    const int MaximumProtectionSpaces = 64;
    const int MaximumRetryAttempt = 2;
    object gate = new object();
    ISipCredentialProvider credentialProvider;
    SipNonceCodec nonceCodec;
    ISipMonotonicClock replayClock;
    SipProtocolTokenGenerator tokens;
    System.Collections.Generic.List<SipAuthenticationServerSpace> spaces =
        new System.Collections.Generic.List<SipAuthenticationServerSpace>();

    internal SipAuthenticationServerCore(ISipCredentialProvider provider,
        ISipRandomSource randomSource, ISipUnixTimeSource timeSource,
        ISipMonotonicClock replayClock)
    {
        if (provider == null)
        {
            throw new System.ArgumentNullException("provider");
        }
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        if (timeSource == null)
        {
            throw new System.ArgumentNullException("timeSource");
        }
        if (replayClock == null)
        {
            throw new System.ArgumentNullException("replayClock");
        }

        credentialProvider = provider;
        nonceCodec = new SipNonceCodec(randomSource, timeSource);
        this.replayClock = replayClock;
        tokens = new SipProtocolTokenGenerator(randomSource);
    }

    internal SipAuthenticationResult CreateChallenge(
        SipAuthenticationRequest? request, SipNonceOptions? options)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (!IsValidScope(request.Scope))
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }
        if (request.BearerToken != null)
        {
            return BuildBearerChallengeResponse(request,
                request.BearerToken, SipAuthenticationStatus.ChallengeRequired);
        }
        SipNonceOptions copiedOptions = CopyNonceOptions(options);
        if (copiedOptions.Realm == null || copiedOptions.Realm.Length == 0)
        {
            throw new System.ArgumentException(
                "A non-empty authentication realm is required.", "options");
        }

        SipDigestChallenge[] policies = CreateChallengePolicies(
            request.Challenges);
        if (policies.Length == 0)
        {
            return CreateFailure(SipAuthenticationStatus.Unsupported,
                "No supported Digest algorithm was configured.", request);
        }

        SipAuthenticationResult result = BuildChallengeResponse(request,
            copiedOptions, policies, false,
            SipAuthenticationStatus.ChallengeRequired);
        if (result.Message != null)
        {
            RememberProtectionSpace(request.Scope, copiedOptions, policies);
        }
        return result;
    }

    internal SipAuthenticationResult Verify(SipAuthenticationRequest? request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Message == null
            || request.Message.Kind != SipMessageKind.Request)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Digest verification requires a SIP request message.", request);
        }
        if (!IsValidScope(request.Scope))
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }

        SipAuthenticationAuthorizationSet parsed =
            ParseAuthorization(request.Message, request.Scope);
        if (!parsed.Found)
        {
            SipAuthenticationServerSpace? latest =
                FindLatestProtectionSpace(request.Scope);
            if (latest == null)
            {
                return CreateFailure(SipAuthenticationStatus.ChallengeRequired,
                    "No Digest authorization or protection space is available.",
                    request);
            }
            return BuildChallengeResponse(request, latest.Options,
                latest.Challenges, false,
                SipAuthenticationStatus.ChallengeRequired);
        }
        if (parsed.Malformed || parsed.Multiple || parsed.Authorization == null)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest authorization is invalid.", request);
        }

        SipDigestAuthorization authorization = parsed.Authorization;
        if (SipAscii.EqualsIgnoreCase(authorization.Scheme, "Bearer"))
        {
            return CreateFailure(SipAuthenticationStatus.Unsupported,
                "Bearer credentials require an external token validator.",
                request);
        }
        if (!SipAscii.EqualsIgnoreCase(authorization.Scheme, "Digest"))
        {
            return CreateFailure(SipAuthenticationStatus.Unsupported,
                "The authentication scheme is unsupported.", request);
        }
        string realm = authorization.Realm == null ? "" : authorization.Realm;
        SipAuthenticationServerSpace? space = FindProtectionSpace(
            request.Scope, realm);
        if (space == null)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest authorization is invalid.", request);
        }
        SipDigestChallenge? policy = FindChallengePolicy(
            space.Challenges, authorization.Algorithm);
        if (policy == null || !AuthorizationMatchesPolicy(
            authorization, policy))
        {
            return CreateFailure(SipAuthenticationStatus.Unsupported,
                "The Digest algorithm or quality is unsupported.", request);
        }

        string? digestUri = FormatDigestUri(request.Message.RequestUri);
        string? method = GetMethodToken(request.Message);
        if (digestUri == null || method == null
            || authorization.DigestUri != digestUri)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest authorization is invalid.", request);
        }

        SipAuthenticationStatus nonceStatus = nonceCodec.ValidateNonce(
            authorization.Nonce, space.Options);
        if (nonceStatus == SipAuthenticationStatus.StaleNonce)
        {
            return BuildChallengeResponse(request, space.Options,
                space.Challenges, true, SipAuthenticationStatus.StaleNonce);
        }
        if (nonceStatus != SipAuthenticationStatus.Success
            || !MatchesOpaqueValue(authorization.Opaque, request.Scope,
                realm, space.Options))
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest authorization is invalid.", request);
        }

        string? suppliedIdentity = ReadSuppliedIdentity(authorization);
        if (suppliedIdentity == null || suppliedIdentity.Length == 0)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest authorization is invalid.", request);
        }
        SipAuthenticationRequest lookup = CreateCredentialLookup(
            request, policy, realm, suppliedIdentity);
        SipCredential? credential = credentialProvider.GetCredential(lookup);
        if (!CredentialMatchesProtectionSpace(credential, request.Scope,
            realm))
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest credentials are invalid.", request);
        }

        SipCredential selected = credential == null
            ? new SipCredential() : credential;
        SipDigestCalculation calculation = CreateCalculation(authorization,
            selected, policy, method, digestUri, request.Body);
        SipDigestCalculationResult calculated =
            SipDigestCalculator.Calculate(calculation);
        bool identityMatches = authorization.UserHash
            ? SipDigestHash.FixedTimeEqualsHex(authorization.Algorithm,
                calculated.UserHash, suppliedIdentity)
            : selected.UserName == suppliedIdentity;
        bool responseMatches = SipDigestHash.FixedTimeEqualsHex(
            authorization.Algorithm, calculated.Response,
            authorization.Response);
        ClearCalculation(calculation);
        if (!identityMatches || !responseMatches)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Digest credentials are invalid.", request);
        }

        SipNonceReplayTracker? tracker = space.ReplayTracker;
        if (tracker == null || selected.UserName == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Digest replay protection is unavailable.", request);
        }
        string replayClientNonce = authorization.ClientNonce == null
            ? "-" : authorization.ClientNonce;
        long replayNonceCount = authorization.Quality == SipDigestQuality.None
            ? 1 : authorization.NonceCount;
        SipAuthenticationStatus replayStatus = tracker.CheckAndRecord(
            realm, selected.UserName, authorization.Nonce == null
                ? "" : authorization.Nonce, replayClientNonce,
            replayNonceCount);
        if (replayStatus != SipAuthenticationStatus.Success)
        {
            return CreateFailure(replayStatus,
                replayStatus == SipAuthenticationStatus.ReplayDetected
                    ? "The Digest authorization was replayed."
                    : "Digest replay protection is at capacity.", request);
        }

        SipDigestAuthorization information = CreateAuthenticationInformation(
            authorization, calculated, space.Options);
        SipAuthenticationResult success = new SipAuthenticationResult();
        success.Status = SipAuthenticationStatus.Success;
        success.Message = SipSnapshotCopy.CopyMessageModel(request.Message);
        success.DigestAuthorization = information;
        success.Identity = selected.UserName;
        success.CorrelationId = request.CorrelationId;
        return success;
    }

    internal SipAuthenticationResult VerifyBearer(
        SipAuthenticationRequest? request, ISipTokenValidator? tokenValidator)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (tokenValidator == null)
        {
            throw new System.ArgumentNullException("tokenValidator");
        }
        if (request.Message == null
            || request.Message.Kind != SipMessageKind.Request)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Bearer verification requires a SIP request message.", request);
        }
        if (!IsValidScope(request.Scope))
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }
        SipAuthenticationAuthorizationSet parsed =
            ParseAuthorization(request.Message, request.Scope);
        if (!parsed.Found)
        {
            if (request.BearerToken != null)
            {
                return BuildBearerChallengeResponse(request,
                    request.BearerToken,
                    SipAuthenticationStatus.ChallengeRequired);
            }
            return CreateFailure(SipAuthenticationStatus.ChallengeRequired,
                "A Bearer authorization is required.", request);
        }
        if (parsed.Malformed || parsed.Multiple
            || parsed.Authorization == null || parsed.BearerToken == null)
        {
            if (request.BearerToken != null
                && request.BearerToken.AuthorizationServer != null)
            {
                SipBearerToken challenge = CopyBearerToken(
                    request.BearerToken);
                challenge.Token = null;
                challenge.Subject = null;
                challenge.Error = "invalid_token";
                return BuildBearerChallengeResponse(request, challenge,
                    SipAuthenticationStatus.InvalidCredentials);
            }
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "The Bearer authorization is invalid.", request);
        }
        if (!SipAscii.EqualsIgnoreCase(
            parsed.Authorization.Scheme, "Bearer"))
        {
            return CreateFailure(SipAuthenticationStatus.Unsupported,
                "The authentication scheme is unsupported.", request);
        }
        return VerifyBearerAuthorization(
            request, parsed.BearerToken, tokenValidator);
    }

    SipAuthenticationResult VerifyBearerAuthorization(
        SipAuthenticationRequest request, SipBearerToken parsedToken,
        ISipTokenValidator tokenValidator)
    {
        SipBearerToken policy = request.BearerToken == null
            ? new SipBearerToken() : request.BearerToken;
        SipBearerToken token = CopyBearerToken(parsedToken);
        token.Realm = policy.Realm;
        token.AuthorizationServer = policy.AuthorizationServer;
        token.Scopes = CopyStrings(policy.Scopes);
        SipAuthenticationResult? validated = tokenValidator
            .ValidateBearerToken(token, request);
        if (validated == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The external Bearer token validator returned no result.",
                request);
        }
        if (validated.Status == SipAuthenticationStatus.Success)
        {
            string? identity = validated.Identity == null
                ? token.Subject : validated.Identity;
            if (identity == null || identity.Length == 0)
            {
                return CreateFailure(SipAuthenticationStatus.Failed,
                    "The external Bearer token has no subject identity.",
                    request);
            }
            SipAuthenticationResult success = new SipAuthenticationResult();
            success.Status = SipAuthenticationStatus.Success;
            success.Message = SipAuthorizationPolicyCore.CopySanitizedMessage(
                request.Message);
            success.Identity = identity;
            token.Token = null;
            success.BearerToken = CopyBearerToken(token);
            success.CorrelationId = request.CorrelationId;
            return success;
        }

        string error = token.Error == "invalid_scope"
            ? "invalid_scope" : "invalid_token";
        string failureMessage = error == "invalid_scope"
            ? "The Bearer access token does not grant the required scope."
            : "The external Bearer token was rejected.";
        if (policy.AuthorizationServer != null)
        {
            SipBearerToken challenge = CopyBearerToken(policy);
            challenge.Token = null;
            challenge.Subject = null;
            challenge.Error = error;
            SipAuthenticationResult challenged = BuildBearerChallengeResponse(
                request, challenge, validated.Status);
            challenged.RetryAllowed = validated.RetryAllowed
                && request.Attempt < MaximumRetryAttempt;
            challenged.ErrorMessage = failureMessage;
            return challenged;
        }
        SipAuthenticationResult failure = CreateFailure(validated.Status,
            failureMessage, request);
        failure.RetryAllowed = validated.RetryAllowed
            && request.Attempt < MaximumRetryAttempt;
        return failure;
    }

    SipAuthenticationResult BuildBearerChallengeResponse(
        SipAuthenticationRequest request, SipBearerToken challenge,
        SipAuthenticationStatus status)
    {
        if (!CanCreateResponse(request.Message))
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "A complete SIP request is required to create a challenge.",
                request);
        }
        string? value = SipAuthenticationHeaderCodec
            .FormatBearerChallengeValue(challenge);
        if (value == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The Bearer challenge policy is invalid.", request);
        }
        SipMessage source = request.Message == null
            ? new SipMessage() : request.Message;
        SipMessage response = SipTransactionMessageFactory.CreateRequestResponse(
            source, request.Scope == SipCredentialScope.Proxy
                ? SipStatusCodes.ProxyAuthenticationRequired
                : SipStatusCodes.Unauthorized,
            request.Scope == SipCredentialScope.Proxy
                ? SipReasonPhrases.ProxyAuthenticationRequired
                : SipReasonPhrases.Unauthorized,
            tokens.CreateTag());
        AppendHeader(response, request.Scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthenticate : SipHeaderNames.WwwAuthenticate,
            value);
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = status;
        result.Message = response;
        result.BearerToken = CopyBearerToken(challenge);
        result.RetryAllowed = request.Attempt < MaximumRetryAttempt;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    SipAuthenticationResult BuildChallengeResponse(
        SipAuthenticationRequest request, SipNonceOptions options,
        SipDigestChallenge[] policies, bool stale,
        SipAuthenticationStatus status)
    {
        if (!CanCreateResponse(request.Message))
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "A complete SIP request is required to create a challenge.",
                request);
        }
        SipMessage source = request.Message == null
            ? new SipMessage() : request.Message;
        string nonce = nonceCodec.CreateNonce(options);
        string realm = options.Realm == null ? "" : options.Realm;
        string opaque = CreateOpaqueValue(request.Scope, realm, options);
        SipDigestChallenge[] challenges = CreateIssuedChallenges(
            policies, realm, nonce, opaque, stale);
        SipMessage response = SipTransactionMessageFactory.CreateRequestResponse(
            source, request.Scope == SipCredentialScope.Proxy
                ? SipStatusCodes.ProxyAuthenticationRequired
                : SipStatusCodes.Unauthorized,
            request.Scope == SipCredentialScope.Proxy
                ? SipReasonPhrases.ProxyAuthenticationRequired
                : SipReasonPhrases.Unauthorized,
            tokens.CreateTag());
        string headerName = request.Scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthenticate : SipHeaderNames.WwwAuthenticate;
        int index = 0;
        while (index < challenges.Length)
        {
            string? value = SipAuthenticationHeaderCodec
                .FormatDigestChallengeValue(challenges[index]);
            if (value == null)
            {
                return CreateFailure(SipAuthenticationStatus.Failed,
                    "A Digest challenge could not be formatted.", request);
            }
            AppendHeader(response, headerName, value);
            index = index + 1;
        }

        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = status;
        result.Message = response;
        result.RetryAllowed = request.Attempt < MaximumRetryAttempt;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    void RememberProtectionSpace(SipCredentialScope scope,
        SipNonceOptions options, SipDigestChallenge[] policies)
    {
        SipNonceReplayTracker tracker = new SipNonceReplayTracker(
            replayClock, options);
        lock (gate)
        {
            int existingIndex = FindProtectionSpaceIndex(scope,
                options.Realm == null ? "" : options.Realm);
            if (existingIndex >= 0)
            {
                SipAuthenticationServerSpace existing = spaces[existingIndex];
                if (HasSameReplayBounds(existing.Options, options)
                    && existing.ReplayTracker != null)
                {
                    tracker = existing.ReplayTracker;
                }
                spaces.RemoveAt(existingIndex);
            }
            SipAuthenticationServerSpace added =
                new SipAuthenticationServerSpace();
            added.Scope = scope;
            added.Realm = options.Realm == null ? "" : options.Realm;
            added.Options = CopyNonceOptions(options);
            added.Challenges = CopyChallenges(policies);
            added.ReplayTracker = tracker;
            spaces.Add(added);
            while (spaces.Count > MaximumProtectionSpaces)
            {
                spaces.RemoveAt(0);
            }
        }
    }

    SipAuthenticationServerSpace? FindProtectionSpace(
        SipCredentialScope scope, string realm)
    {
        lock (gate)
        {
            int index = FindProtectionSpaceIndex(scope, realm);
            return index < 0 ? null : spaces[index];
        }
    }

    SipAuthenticationServerSpace? FindLatestProtectionSpace(
        SipCredentialScope scope)
    {
        lock (gate)
        {
            int index = spaces.Count - 1;
            while (index >= 0)
            {
                if (spaces[index].Scope == scope)
                {
                    return spaces[index];
                }
                index = index - 1;
            }
            return null;
        }
    }

    int FindProtectionSpaceIndex(SipCredentialScope scope, string realm)
    {
        int index = spaces.Count - 1;
        while (index >= 0)
        {
            if (spaces[index].Scope == scope && spaces[index].Realm == realm)
            {
                return index;
            }
            index = index - 1;
        }
        return -1;
    }

    static SipDigestChallenge[] CreateChallengePolicies(
        SipDigestChallenge[]? requested)
    {
        if (requested == null)
        {
            return new SipDigestChallenge[0];
        }
        if (requested.Length == 0)
        {
            return CreateDefaultChallengePolicies();
        }

        System.Collections.Generic.List<SipDigestChallenge> ordered =
            new System.Collections.Generic.List<SipDigestChallenge>();
        int index = 0;
        while (index < requested.Length)
        {
            SipDigestChallenge? candidate = requested[index];
            if (candidate != null && AlgorithmRank(candidate.Algorithm) > 0
                && !ContainsAlgorithm(ordered, candidate.Algorithm)
                && HasValidQualities(candidate.Qualities))
            {
                SipDigestChallenge copy = CopyChallengePolicy(candidate);
                int insertion = 0;
                while (insertion < ordered.Count
                    && AlgorithmRank(ordered[insertion].Algorithm)
                        >= AlgorithmRank(copy.Algorithm))
                {
                    insertion = insertion + 1;
                }
                ordered.Insert(insertion, copy);
            }
            index = index + 1;
        }
        return ordered.ToArray();
    }

    static SipDigestChallenge[] CreateDefaultChallengePolicies()
    {
        SipDigestChallenge sha512 = CreateDefaultChallengePolicy(
            SipDigestAlgorithm.Sha512256);
        SipDigestChallenge sha256 = CreateDefaultChallengePolicy(
            SipDigestAlgorithm.Sha256);
        SipDigestChallenge md5 = CreateDefaultChallengePolicy(
            SipDigestAlgorithm.Md5);
        return new SipDigestChallenge[] { sha512, sha256, md5 };
    }

    static SipDigestChallenge CreateDefaultChallengePolicy(
        SipDigestAlgorithm algorithm)
    {
        SipDigestChallenge challenge = new SipDigestChallenge();
        challenge.Scheme = "Digest";
        challenge.Algorithm = algorithm;
        challenge.AlgorithmToken = AlgorithmToken(algorithm);
        challenge.Qualities = new SipDigestQuality[]
            { SipDigestQuality.Auth, SipDigestQuality.AuthInt };
        challenge.QualityTokens = new string[] { "auth", "auth-int" };
        challenge.Utf8 = true;
        return challenge;
    }

    static SipDigestChallenge[] CreateIssuedChallenges(
        SipDigestChallenge[] policies, string realm, string nonce,
        string opaque, bool stale)
    {
        SipDigestChallenge[] result =
            new SipDigestChallenge[policies.Length];
        int index = 0;
        while (index < policies.Length)
        {
            SipDigestChallenge challenge = CopyChallengePolicy(policies[index]);
            challenge.Realm = realm;
            challenge.Nonce = nonce;
            challenge.Opaque = opaque;
            challenge.Stale = stale;
            challenge.HasStale = stale;
            result[index] = challenge;
            index = index + 1;
        }
        return result;
    }

    static SipDigestChallenge CopyChallengePolicy(SipDigestChallenge source)
    {
        SipDigestChallenge copy = new SipDigestChallenge();
        copy.Scheme = "Digest";
        copy.Algorithm = source.Algorithm;
        copy.AlgorithmToken = AlgorithmToken(source.Algorithm);
        copy.Qualities = CopyQualities(source.Qualities);
        copy.QualityTokens = QualityTokens(copy.Qualities);
        copy.Domain = source.Domain;
        copy.Utf8 = source.Utf8;
        copy.UserHash = source.UserHash;
        copy.HasUserHash = source.HasUserHash || source.UserHash;
        return copy;
    }

    static SipDigestChallenge[] CopyChallenges(SipDigestChallenge[] source)
    {
        SipDigestChallenge[] result =
            new SipDigestChallenge[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyChallengePolicy(source[index]);
            index = index + 1;
        }
        return result;
    }

    static SipDigestChallenge? FindChallengePolicy(
        SipDigestChallenge[] challenges, SipDigestAlgorithm algorithm)
    {
        int index = 0;
        while (index < challenges.Length)
        {
            if (challenges[index].Algorithm == algorithm)
            {
                return challenges[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool AuthorizationMatchesPolicy(
        SipDigestAuthorization authorization, SipDigestChallenge policy)
    {
        if (!QualityIsOffered(policy.Qualities, authorization.Quality)
            || authorization.UserHash != policy.UserHash)
        {
            return false;
        }
        if (authorization.Quality == SipDigestQuality.None)
        {
            return authorization.NonceCount < 0
                && (SipDigestHash.UsesSessionKey(authorization.Algorithm)
                    ? authorization.ClientNonce != null
                        && authorization.ClientNonce.Length > 0
                    : true);
        }
        return authorization.NonceCount >= 1
            && authorization.NonceCount <= 4294967295L
            && authorization.ClientNonce != null
            && authorization.ClientNonce.Length > 0;
    }

    static bool QualityIsOffered(SipDigestQuality[]? qualities,
        SipDigestQuality quality)
    {
        SipDigestQuality[] values = qualities == null
            ? new SipDigestQuality[0] : qualities;
        if (values.Length == 0)
        {
            return quality == SipDigestQuality.None;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == quality)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasValidQualities(SipDigestQuality[]? qualities)
    {
        if (qualities == null)
        {
            return false;
        }
        int index = 0;
        while (index < qualities.Length)
        {
            if (qualities[index] != SipDigestQuality.Auth
                && qualities[index] != SipDigestQuality.AuthInt)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ContainsAlgorithm(
        System.Collections.Generic.List<SipDigestChallenge> challenges,
        SipDigestAlgorithm algorithm)
    {
        int index = 0;
        while (index < challenges.Count)
        {
            if (challenges[index].Algorithm == algorithm)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static int AlgorithmRank(SipDigestAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case SipDigestAlgorithm.Sha512256: return 6;
            case SipDigestAlgorithm.Sha512256Sess: return 5;
            case SipDigestAlgorithm.Sha256: return 4;
            case SipDigestAlgorithm.Sha256Sess: return 3;
            case SipDigestAlgorithm.Md5: return 2;
            case SipDigestAlgorithm.Md5Sess: return 1;
            default: return 0;
        }
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
            case SipDigestAlgorithm.Sha512256Sess:
                return "SHA-512-256-sess";
            default: return "";
        }
    }

    static string[] QualityTokens(SipDigestQuality[] qualities)
    {
        string[] result = new string[qualities.Length];
        int index = 0;
        while (index < qualities.Length)
        {
            result[index] = qualities[index] == SipDigestQuality.AuthInt
                ? "auth-int" : "auth";
            index = index + 1;
        }
        return result;
    }

    static SipAuthenticationAuthorizationSet ParseAuthorization(
        SipMessage message, SipCredentialScope scope)
    {
        SipAuthenticationAuthorizationSet result =
            new SipAuthenticationAuthorizationSet();
        if (message.Headers == null)
        {
            result.Malformed = true;
            return result;
        }
        string headerName = scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthorization : SipHeaderNames.Authorization;
        SipParseOptions options = new SipParseOptions();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                header.Name, headerName))
            {
                if (result.Found)
                {
                    result.Multiple = true;
                }
                result.Found = true;
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        headerName, header.Value, options);
                if (!parsed.Success || parsed.Authorization == null)
                {
                    result.Malformed = true;
                }
                else
                {
                    result.Authorization = parsed.Authorization;
                    result.BearerToken = parsed.BearerToken;
                }
            }
            index = index + 1;
        }
        return result;
    }

    static SipAuthenticationRequest CreateCredentialLookup(
        SipAuthenticationRequest source, SipDigestChallenge policy,
        string realm, string suppliedIdentity)
    {
        SipAuthenticationRequest lookup = new SipAuthenticationRequest();
        lookup.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        SipDigestChallenge challenge = CopyChallengePolicy(policy);
        challenge.Realm = realm;
        lookup.Challenges = new SipDigestChallenge[] { challenge };
        SipCredential claimed = new SipCredential();
        claimed.UserName = suppliedIdentity;
        claimed.Realm = realm;
        claimed.Scope = source.Scope;
        lookup.Credential = claimed;
        lookup.Target = SipUriModelCopy.CreateNormalizedCopy(source.Target == null
            ? source.Message == null ? null : source.Message.RequestUri
            : source.Target);
        lookup.Body = CopyBytes(source.Body);
        lookup.Scope = source.Scope;
        lookup.Attempt = source.Attempt;
        lookup.CorrelationId = source.CorrelationId;
        return lookup;
    }

    static bool CredentialMatchesProtectionSpace(SipCredential? credential,
        SipCredentialScope scope, string realm)
    {
        return credential != null && credential.UserName != null
            && credential.UserName.Length > 0 && credential.Secret != null
            && credential.Realm == realm && credential.Scope == scope;
    }

    static SipDigestCalculation CreateCalculation(
        SipDigestAuthorization authorization, SipCredential credential,
        SipDigestChallenge policy, string method, string digestUri,
        byte[]? requestBody)
    {
        SipDigestCalculation calculation = new SipDigestCalculation();
        calculation.Algorithm = authorization.Algorithm;
        calculation.Quality = authorization.Quality;
        calculation.UserName = credential.UserName;
        calculation.Realm = authorization.Realm;
        calculation.Secret = CopyBytes(credential.Secret);
        calculation.Nonce = authorization.Nonce;
        calculation.NonceCount = authorization.NonceCount;
        calculation.ClientNonce = authorization.ClientNonce;
        calculation.Method = method;
        calculation.DigestUri = digestUri;
        calculation.RequestBody = CopyBytes(requestBody);
        calculation.ResponseBody = new byte[0];
        calculation.Utf8 = policy.Utf8;
        return calculation;
    }

    SipDigestAuthorization CreateAuthenticationInformation(
        SipDigestAuthorization authorization,
        SipDigestCalculationResult calculated, SipNonceOptions options)
    {
        SipDigestAuthorization information = new SipDigestAuthorization();
        information.NextNonce = nonceCodec.CreateNonce(options);
        information.Response = calculated.ResponseAuthentication;
        information.Quality = authorization.Quality;
        information.QualityToken = authorization.QualityToken;
        information.NonceCount = authorization.NonceCount;
        information.ClientNonce = authorization.ClientNonce;
        return information;
    }

    static string? ReadSuppliedIdentity(
        SipDigestAuthorization authorization)
    {
        if (authorization.UserHash)
        {
            return authorization.UserNameExtended == null
                ? authorization.UserName : null;
        }
        if (authorization.UserNameExtended == null)
        {
            return authorization.UserName;
        }
        return DecodeExtendedIdentity(authorization.UserNameExtended);
    }

    static string? DecodeExtendedIdentity(string value)
    {
        if (value.Length < 7
            || !SipAscii.EqualsIgnoreCase(value.Substring(0, 7), "UTF-8''"))
        {
            return null;
        }
        int index = 7;
        while (index < value.Length)
        {
            char current = value[index];
            if (current > 127)
            {
                return null;
            }
            if (current == '%')
            {
                if (index + 2 >= value.Length
                    || HexValue(value[index + 1]) < 0
                    || HexValue(value[index + 2]) < 0)
                {
                    return null;
                }
                index = index + 3;
            }
            else
            {
                index = index + 1;
            }
        }
        return System.Uri.UnescapeDataString(value.Substring(7)).Normalize(
            System.Text.NormalizationForm.FormC);
    }

    static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        return -1;
    }

    static string CreateOpaqueValue(SipCredentialScope scope,
        string realm, SipNonceOptions options)
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(
            "sip-digest-opaque-v1\n" + (int)scope + "\n" + realm);
        byte[] key = CopyBytes(options.CurrentKey);
        byte[] hash = System.Security.Cryptography.HMACSHA256.HashData(key, input);
        string result = System.Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(input);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(hash);
        return result;
    }

    static bool MatchesOpaqueValue(string? supplied, SipCredentialScope scope,
        string realm, SipNonceOptions options)
    {
        string current = CreateOpaqueValue(scope, realm, options);
        SipNonceOptions previousOptions = CopyNonceOptions(options);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            previousOptions.CurrentKey);
        previousOptions.CurrentKey = options.PreviousKey.Length == 0
            ? CopyBytes(options.CurrentKey) : CopyBytes(options.PreviousKey);
        string previous = CreateOpaqueValue(scope, realm, previousOptions);
        byte[] suppliedBytes = System.Text.Encoding.ASCII.GetBytes(
            supplied == null ? "" : supplied);
        byte[] currentBytes = System.Text.Encoding.ASCII.GetBytes(current);
        byte[] previousBytes = System.Text.Encoding.ASCII.GetBytes(previous);
        bool currentMatches = suppliedBytes.Length == currentBytes.Length
            && System.Security.Cryptography.CryptographicOperations
                .FixedTimeEquals(suppliedBytes, currentBytes);
        bool previousMatches = suppliedBytes.Length == previousBytes.Length
            && System.Security.Cryptography.CryptographicOperations
                .FixedTimeEquals(suppliedBytes, previousBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            suppliedBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            currentBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            previousBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            previousOptions.CurrentKey);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            previousOptions.PreviousKey);
        return currentMatches | previousMatches;
    }

    static bool CanCreateResponse(SipMessage? message)
    {
        return message != null && message.Kind == SipMessageKind.Request
            && message.ViaValues != null && message.ViaValues.Length > 0
            && message.CSeq != null && message.From != null
            && message.To != null;
    }

    static void AppendHeader(SipMessage message, string name, string value)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            headers.Add(message.Headers[index]);
            index = index + 1;
        }
        SipHeader added = new SipHeader();
        added.Name = name;
        added.Value = value;
        headers.Add(added);
        message.Headers = headers.ToArray();
        message.OriginalBytes = new byte[0];
    }

    static string? FormatDigestUri(SipUri? target)
    {
        if (target == null)
        {
            return null;
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = false;
        options.PreserveOriginal = true;
        options.MaxHeaderLineBytes = int.MaxValue;
        SipWriteResult result = SipUriFormatter.FormatUriModel(target, options);
        return result.Success
            ? System.Text.Encoding.ASCII.GetString(result.Bytes) : null;
    }

    static string? GetMethodToken(SipMessage message)
    {
        switch (message.Method)
        {
            case SipMethod.Ack: return SipMethods.Ack;
            case SipMethod.Bye: return SipMethods.Bye;
            case SipMethod.Cancel: return SipMethods.Cancel;
            case SipMethod.Info: return SipMethods.Info;
            case SipMethod.Invite: return SipMethods.Invite;
            case SipMethod.Message: return SipMethods.Message;
            case SipMethod.Notify: return SipMethods.Notify;
            case SipMethod.Options: return SipMethods.Options;
            case SipMethod.Prack: return SipMethods.Prack;
            case SipMethod.Publish: return SipMethods.Publish;
            case SipMethod.Refer: return SipMethods.Refer;
            case SipMethod.Register: return SipMethods.Register;
            case SipMethod.Subscribe: return SipMethods.Subscribe;
            case SipMethod.Update: return SipMethods.Update;
            default:
                return SipAscii.IsToken(message.MethodToken)
                    ? message.MethodToken : null;
        }
    }

    static SipNonceOptions CopyNonceOptions(SipNonceOptions? source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("options");
        }
        SipNonceOptions copy = new SipNonceOptions();
        copy.LifetimeSeconds = source.LifetimeSeconds;
        copy.ClockSkewSeconds = source.ClockSkewSeconds;
        copy.Realm = source.Realm;
        copy.CurrentKey = CopyBytes(source.CurrentKey);
        copy.PreviousKey = CopyBytes(source.PreviousKey);
        copy.MaxReplayEntries = source.MaxReplayEntries;
        return copy;
    }

    static bool HasSameReplayBounds(SipNonceOptions first,
        SipNonceOptions second)
    {
        return first.LifetimeSeconds == second.LifetimeSeconds
            && first.ClockSkewSeconds == second.ClockSkewSeconds
            && first.MaxReplayEntries == second.MaxReplayEntries;
    }

    static SipDigestQuality[] CopyQualities(SipDigestQuality[]? source)
    {
        return source == null ? new SipDigestQuality[0]
            : (SipDigestQuality[])source.Clone();
    }

    static byte[] CopyBytes(byte[]? source)
    {
        return source == null ? new byte[0] : (byte[])source.Clone();
    }

    static string[] CopyStrings(string[]? source)
    {
        return source == null ? new string[0] : (string[])source.Clone();
    }

    static SipBearerToken CopyBearerToken(SipBearerToken source)
    {
        SipBearerToken copy = new SipBearerToken();
        copy.Token = source.Token;
        copy.Realm = source.Realm;
        copy.AuthorizationServer = source.AuthorizationServer;
        copy.Scopes = CopyStrings(source.Scopes);
        copy.Subject = source.Subject;
        copy.Error = source.Error;
        copy.Parameters = CopyParameters(source.Parameters);
        return copy;
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }
        SipUriParameter[] result = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = source[index].Name;
            parameter.Value = source[index].Value;
            parameter.HasValue = source[index].HasValue;
            parameter.Quoted = source[index].Quoted;
            result[index] = parameter;
            index = index + 1;
        }
        return result;
    }

    static void ClearCalculation(SipDigestCalculation calculation)
    {
        if (calculation.Secret != null)
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(
                calculation.Secret);
        }
    }

    static bool IsValidScope(SipCredentialScope scope)
    {
        return scope == SipCredentialScope.Origin
            || scope == SipCredentialScope.Proxy;
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
