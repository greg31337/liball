namespace sip;

internal class SipAuthenticationClientCore
{
    const int MaximumRetryAttempt = 2;
    const int MaximumCacheEntries = 64;
    object gate = new object();
    ISipCredentialProvider credentialProvider;
    SipProtocolTokenGenerator tokens;
    System.Collections.Generic.List<SipAuthenticationClientCacheEntry> cache =
        new System.Collections.Generic.List<SipAuthenticationClientCacheEntry>();

    internal SipAuthenticationClientCore(
        ISipCredentialProvider provider, ISipRandomSource random)
    {
        if (provider == null)
        {
            throw new System.ArgumentNullException("provider");
        }
        if (random == null)
        {
            throw new System.ArgumentNullException("random");
        }
        credentialProvider = provider;
        tokens = new SipProtocolTokenGenerator(random);
    }

    internal SipAuthenticationResult Authorize(SipAuthenticationRequest? request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.BearerToken != null)
        {
            return SipBearerAuthenticationCore.Authorize(request);
        }
        if (request.Message == null
            || request.Message.Kind != SipMessageKind.Request)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Digest authorization requires a SIP request message.", request);
        }
        if (request.Scope != SipCredentialScope.Origin
            && request.Scope != SipCredentialScope.Proxy)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }
        if (request.Attempt < 0 || request.Attempt > MaximumRetryAttempt)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The bounded authentication retry limit has been reached.", request);
        }

        SipUri? target = GetTarget(request);
        string? targetKey = FormatTargetKey(target);
        string? digestUri = FormatDigestUri(request.Message.RequestUri);
        string? method = GetMethodToken(request.Message);
        if (target == null || targetKey == null || digestUri == null
            || method == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The authentication target or request line is invalid.", request);
        }

        SipAuthenticationChallengeSet source = CollectChallenges(request);
        bool suppliedChallenges = source.Challenges.Length > 0
            || source.Malformed;
        SipDigestChallenge[] candidates = GetPreferredChallenges(
            source.Challenges);
        if (candidates.Length == 0 && !suppliedChallenges)
        {
            candidates = GetPreferredChallenges(FindCachedChallenges(
                request.Scope, targetKey));
        }
        if (candidates.Length == 0)
        {
            SipAuthenticationStatus status = suppliedChallenges
                ? SipAuthenticationStatus.Unsupported
                : SipAuthenticationStatus.ChallengeRequired;
            string message = source.Malformed
                ? "The authentication challenge is malformed."
                : "No supported Digest challenge is available.";
            return CreateFailure(status, message, request);
        }

        SipDigestChallenge? selected = null;
        SipCredential? selectedCredential = null;
        System.Collections.Generic.List<string> attemptedRealms =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < candidates.Length && selected == null)
        {
            SipDigestChallenge candidate = candidates[index];
            string realm = candidate.Realm == null ? "" : candidate.Realm;
            if (!ContainsRealm(attemptedRealms, realm))
            {
                attemptedRealms.Add(realm);
                SipAuthenticationRequest lookup = CreateCredentialLookup(
                    request, target, candidate);
                SipCredential? credential = credentialProvider.GetCredential(lookup);
                if (CredentialMatches(credential, candidate, request.Scope))
                {
                    selected = candidate;
                    selectedCredential = credential;
                }
            }
            index = index + 1;
        }
        if (selected == null || selectedCredential == null)
        {
            return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                "No credential matches the challenged realm, target, and scope.",
                request);
        }

        return CreateAuthorization(request, targetKey, digestUri, method,
            selected, selectedCredential);
    }

    internal SipAuthenticationResult Verify(SipAuthenticationRequest? request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Message == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Authentication-Info verification requires a SIP message.", request);
        }
        if (request.Scope != SipCredentialScope.Origin
            && request.Scope != SipCredentialScope.Proxy)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }

        SipAuthenticationInfoSet parsed = ParseAuthenticationInformation(
            request.Message, request.Scope);
        if (!parsed.Found)
        {
            ClearVerificationContext(request);
            return CreateVerificationSuccess(request, null, null);
        }
        if (parsed.Malformed || parsed.Multiple || parsed.Information == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The Authentication-Info value is malformed or ambiguous.", request);
        }

        SipUri? target = GetTarget(request);
        string? targetKey = FormatTargetKey(target);
        lock (gate)
        {
            int entryIndex = FindVerificationEntryIndex(
                request.Scope, targetKey, request.CorrelationId,
                parsed.Information);
            if (entryIndex < 0)
            {
                return CreateFailure(SipAuthenticationStatus.Failed,
                    "No cached Digest exchange matches Authentication-Info.",
                    request);
            }
            SipAuthenticationClientCacheEntry entry = cache[entryIndex];
            SipDigestAuthorization information = parsed.Information;
            if (!InformationMatchesExchange(information, entry))
            {
                RemoveCacheEntry(entryIndex);
                return CreateFailure(SipAuthenticationStatus.InvalidCredentials,
                    "Authentication-Info does not match the authorized request.",
                    request);
            }

            SipDigestCalculation? stored = entry.LastCalculation;
            string? identity = stored == null ? null : stored.UserName;
            if (information.Response != null)
            {
                if (stored == null)
                {
                    RemoveCacheEntry(entryIndex);
                    return CreateFailure(SipAuthenticationStatus.Failed,
                        "The Digest exchange has no verification context.", request);
                }
                SipDigestCalculation verification = CopyCalculation(stored);
                verification.ResponseBody = SelectBody(request);
                SipDigestCalculationResult expected =
                    SipDigestCalculator.Calculate(verification);
                ClearCalculation(verification);
                if (!SipDigestHash.FixedTimeEqualsHex(
                    verification.Algorithm, expected.ResponseAuthentication,
                    information.Response))
                {
                    RemoveCacheEntry(entryIndex);
                    return CreateFailure(
                        SipAuthenticationStatus.InvalidCredentials,
                        "The server response digest is invalid.", request);
                }
            }

            if (information.NextNonce != null)
            {
                if (information.NextNonce.Length == 0
                    || !IsAscii(information.NextNonce))
                {
                    RemoveCacheEntry(entryIndex);
                    return CreateFailure(SipAuthenticationStatus.Failed,
                        "The nextnonce value is invalid.", request);
                }
                entry.Challenge.Nonce = information.NextNonce;
                entry.Challenge.Stale = false;
                entry.Challenge.HasStale = false;
                entry.NonceCount = 0;
                entry.ClientNonce = null;
            }
            ClearCalculation(entry.LastCalculation);
            entry.LastCalculation = null;
            MoveCacheEntryToEnd(entryIndex);
            return CreateVerificationSuccess(request, information, identity);
        }
    }

    SipAuthenticationResult CreateAuthorization(
        SipAuthenticationRequest request, string targetKey, string digestUri,
        string method, SipDigestChallenge challenge, SipCredential credential)
    {
        SipDigestQuality quality = SelectQuality(challenge);
        string realm = challenge.Realm == null ? "" : challenge.Realm;
        string nonce = challenge.Nonce == null ? "" : challenge.Nonce;
        lock (gate)
        {
            int entryIndex = FindCacheEntryIndex(request.Scope, targetKey, realm);
            SipAuthenticationClientCacheEntry? existing = entryIndex < 0
                ? null : cache[entryIndex];
            bool reuse = existing != null && !challenge.Stale
                && existing.Challenge.Nonce == nonce
                && existing.Challenge.Algorithm == challenge.Algorithm
                && existing.Quality == quality;
            long nonceCount = -1;
            if (quality != SipDigestQuality.None)
            {
                if (reuse && existing != null
                    && existing.NonceCount >= 4294967295L)
                {
                    return CreateFailure(SipAuthenticationStatus.Failed,
                        "The Digest nonce count is exhausted.", request);
                }
                nonceCount = reuse && existing != null
                    ? existing.NonceCount + 1 : 1;
            }
            bool needsClientNonce = quality != SipDigestQuality.None
                || SipDigestHash.UsesSessionKey(challenge.Algorithm);
            string? clientNonce = null;
            if (needsClientNonce)
            {
                clientNonce = reuse && existing != null
                    ? existing.ClientNonce : null;
                if (clientNonce == null || clientNonce.Length == 0)
                {
                    clientNonce = tokens.CreateClientNonce();
                }
            }

            SipDigestCalculation calculation = new SipDigestCalculation();
            calculation.Algorithm = challenge.Algorithm;
            calculation.Quality = quality;
            calculation.UserName = credential.UserName;
            calculation.Realm = realm;
            calculation.Secret = CopyBytes(credential.Secret);
            calculation.Nonce = nonce;
            calculation.NonceCount = nonceCount;
            calculation.ClientNonce = clientNonce;
            calculation.Method = method;
            calculation.DigestUri = digestUri;
            calculation.RequestBody = SelectBody(request);
            calculation.ResponseBody = new byte[0];
            calculation.Utf8 = challenge.Utf8;
            SipDigestCalculationResult calculated =
                SipDigestCalculator.Calculate(calculation);

            SipDigestAuthorization authorization = CreateAuthorizationModel(
                challenge, credential, quality, digestUri, nonceCount,
                clientNonce, calculated);
            string? headerValue = SipAuthenticationHeaderCodec
                .FormatDigestAuthorizationValue(authorization);
            if (headerValue == null)
            {
                ClearCalculation(calculation);
                return CreateFailure(SipAuthenticationStatus.Failed,
                    "The Digest authorization could not be formatted.", request);
            }
            SipMessage? copied = SipSnapshotCopy.CopyMessageModel(request.Message);
            if (copied == null)
            {
                ClearCalculation(calculation);
                return CreateFailure(SipAuthenticationStatus.Failed,
                    "The SIP request could not be copied.", request);
            }
            AddAuthorizationHeader(copied, request.Scope, headerValue);

            SipAuthenticationClientCacheEntry entry;
            if (existing == null)
            {
                entry = new SipAuthenticationClientCacheEntry();
                entry.Scope = request.Scope;
                entry.TargetKey = targetKey;
                entry.Realm = realm;
                cache.Add(entry);
            }
            else
            {
                entry = existing;
                ClearCalculation(entry.LastCalculation);
                cache.RemoveAt(entryIndex);
                cache.Add(entry);
            }
            entry.Challenge = CopyChallenge(challenge);
            entry.Challenge.Stale = false;
            entry.Challenge.HasStale = false;
            entry.Quality = quality;
            entry.NonceCount = nonceCount;
            entry.ClientNonce = clientNonce;
            entry.LastCalculation = calculation;
            entry.CorrelationId = request.CorrelationId;
            TrimCache();

            SipAuthenticationResult result = new SipAuthenticationResult();
            result.Status = SipAuthenticationStatus.Success;
            result.Message = copied;
            result.DigestAuthorization = authorization;
            result.Identity = credential.UserName;
            result.RetryAllowed = request.Attempt < MaximumRetryAttempt;
            result.CorrelationId = request.CorrelationId;
            return result;
        }
    }

    static SipDigestAuthorization CreateAuthorizationModel(
        SipDigestChallenge challenge, SipCredential credential,
        SipDigestQuality quality, string digestUri, long nonceCount,
        string? clientNonce, SipDigestCalculationResult calculated)
    {
        SipDigestAuthorization authorization = new SipDigestAuthorization();
        authorization.Scheme = "Digest";
        if (challenge.UserHash)
        {
            authorization.UserName = calculated.UserHash;
        }
        else if (challenge.Utf8 && ContainsNonAscii(credential.UserName))
        {
            authorization.UserNameExtended = EncodeExtendedUserName(
                credential.UserName);
        }
        else
        {
            authorization.UserName = credential.UserName;
        }
        authorization.Realm = challenge.Realm;
        authorization.Nonce = challenge.Nonce;
        authorization.DigestUri = digestUri;
        authorization.Response = calculated.Response;
        authorization.Algorithm = challenge.Algorithm;
        if (challenge.AlgorithmToken != null
            || challenge.Algorithm != SipDigestAlgorithm.Md5)
        {
            authorization.AlgorithmToken = GetCanonicalAlgorithmToken(
                challenge.Algorithm);
        }
        authorization.Quality = quality;
        if (quality == SipDigestQuality.Auth)
        {
            authorization.QualityToken = "auth";
        }
        else if (quality == SipDigestQuality.AuthInt)
        {
            authorization.QualityToken = "auth-int";
        }
        authorization.NonceCount = nonceCount;
        authorization.ClientNonce = clientNonce;
        authorization.Opaque = challenge.Opaque;
        authorization.HasUserHash = challenge.HasUserHash
            || challenge.UserHash;
        authorization.UserHash = challenge.UserHash;
        return authorization;
    }

    static SipAuthenticationChallengeSet CollectChallenges(
        SipAuthenticationRequest request)
    {
        SipAuthenticationChallengeSet result =
            new SipAuthenticationChallengeSet();
        if (request.Challenges == null)
        {
            result.Malformed = true;
            return result;
        }
        if (request.Challenges.Length > 0)
        {
            result.Challenges = request.Challenges;
            return result;
        }
        if (request.Message == null || request.Message.Headers == null)
        {
            return result;
        }
        string headerName = request.Scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthenticate : SipHeaderNames.WwwAuthenticate;
        System.Collections.Generic.List<SipDigestChallenge> values =
            new System.Collections.Generic.List<SipDigestChallenge>();
        SipParseOptions options = new SipParseOptions();
        int index = 0;
        while (index < request.Message.Headers.Length)
        {
            SipHeader? header = request.Message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                header.Name, headerName))
            {
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        header.Name, header.Value, options);
                if (!parsed.Success || parsed.Challenge == null)
                {
                    result.Malformed = true;
                }
                else
                {
                    values.Add(parsed.Challenge);
                }
            }
            index = index + 1;
        }
        result.Challenges = values.ToArray();
        return result;
    }

    static SipDigestChallenge[] GetPreferredChallenges(
        SipDigestChallenge[] challenges)
    {
        System.Collections.Generic.List<SipDigestChallenge> ordered =
            new System.Collections.Generic.List<SipDigestChallenge>();
        int index = 0;
        while (index < challenges.Length)
        {
            SipDigestChallenge? challenge = challenges[index];
            if (IsSupportedChallenge(challenge))
            {
                SipDigestChallenge value = challenge == null
                    ? new SipDigestChallenge() : challenge;
                int rank = GetAlgorithmRank(value.Algorithm);
                int insertion = 0;
                while (insertion < ordered.Count
                    && GetAlgorithmRank(ordered[insertion].Algorithm) >= rank)
                {
                    insertion = insertion + 1;
                }
                ordered.Insert(insertion, value);
            }
            index = index + 1;
        }
        return ordered.ToArray();
    }

    static bool IsSupportedChallenge(SipDigestChallenge? challenge)
    {
        if (challenge == null || challenge.Realm == null
            || challenge.Nonce == null || challenge.Nonce.Length == 0)
        {
            return false;
        }
        if (challenge.Scheme != null
            && !SipAscii.EqualsIgnoreCase(challenge.Scheme, "Digest"))
        {
            return false;
        }
        if (GetAlgorithmRank(challenge.Algorithm) == 0
            || !AlgorithmTokenMatches(challenge))
        {
            return false;
        }
        return SelectQuality(challenge) != (SipDigestQuality)(-1);
    }

    static bool AlgorithmTokenMatches(SipDigestChallenge challenge)
    {
        if (challenge.AlgorithmToken == null)
        {
            return true;
        }
        SipDigestAlgorithm parsed = GetAlgorithmFromToken(
            challenge.AlgorithmToken);
        return parsed == challenge.Algorithm;
    }

    static SipDigestAlgorithm GetAlgorithmFromToken(string token)
    {
        if (SipAscii.EqualsIgnoreCase(token, "MD5"))
        {
            return SipDigestAlgorithm.Md5;
        }
        if (SipAscii.EqualsIgnoreCase(token, "MD5-sess"))
        {
            return SipDigestAlgorithm.Md5Sess;
        }
        if (SipAscii.EqualsIgnoreCase(token, "SHA-256"))
        {
            return SipDigestAlgorithm.Sha256;
        }
        if (SipAscii.EqualsIgnoreCase(token, "SHA-256-sess"))
        {
            return SipDigestAlgorithm.Sha256Sess;
        }
        if (SipAscii.EqualsIgnoreCase(token, "SHA-512-256"))
        {
            return SipDigestAlgorithm.Sha512256;
        }
        if (SipAscii.EqualsIgnoreCase(token, "SHA-512-256-sess"))
        {
            return SipDigestAlgorithm.Sha512256Sess;
        }
        return SipDigestAlgorithm.Unknown;
    }

    static int GetAlgorithmRank(SipDigestAlgorithm algorithm)
    {
        if (algorithm == SipDigestAlgorithm.Sha512256
            || algorithm == SipDigestAlgorithm.Sha512256Sess)
        {
            return 3;
        }
        if (algorithm == SipDigestAlgorithm.Sha256
            || algorithm == SipDigestAlgorithm.Sha256Sess)
        {
            return 2;
        }
        if (algorithm == SipDigestAlgorithm.Md5
            || algorithm == SipDigestAlgorithm.Md5Sess)
        {
            return 1;
        }
        return 0;
    }

    static SipDigestQuality SelectQuality(SipDigestChallenge challenge)
    {
        SipDigestQuality[] qualities = challenge.Qualities == null
            ? new SipDigestQuality[0] : challenge.Qualities;
        string[] tokens = challenge.QualityTokens == null
            ? new string[0] : challenge.QualityTokens;
        bool offered = qualities.Length > 0 || tokens.Length > 0;
        int index = 0;
        while (index < qualities.Length)
        {
            if (qualities[index] == SipDigestQuality.AuthInt)
            {
                return SipDigestQuality.AuthInt;
            }
            index = index + 1;
        }
        index = 0;
        while (index < qualities.Length)
        {
            if (qualities[index] == SipDigestQuality.Auth)
            {
                return SipDigestQuality.Auth;
            }
            index = index + 1;
        }
        return offered ? (SipDigestQuality)(-1) : SipDigestQuality.None;
    }

    static bool CredentialMatches(SipCredential? credential,
        SipDigestChallenge challenge, SipCredentialScope scope)
    {
        if (credential == null || credential.UserName == null
            || credential.UserName.Length == 0 || credential.Secret == null
            || credential.Realm == null || challenge.Realm == null)
        {
            return false;
        }
        return credential.Scope == scope
            && credential.Realm == challenge.Realm;
    }

    static SipAuthenticationRequest CreateCredentialLookup(
        SipAuthenticationRequest source, SipUri target,
        SipDigestChallenge challenge)
    {
        SipAuthenticationRequest lookup = new SipAuthenticationRequest();
        lookup.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        lookup.Challenges = new SipDigestChallenge[] { CopyChallenge(challenge) };
        lookup.Target = SipUriModelCopy.CreateNormalizedCopy(target);
        lookup.Body = CopyBytes(source.Body);
        lookup.Scope = source.Scope;
        lookup.Attempt = source.Attempt;
        lookup.CorrelationId = source.CorrelationId;
        return lookup;
    }

    static SipDigestChallenge CopyChallenge(SipDigestChallenge source)
    {
        SipDigestChallenge copy = new SipDigestChallenge();
        copy.Scheme = source.Scheme;
        copy.SchemeData = source.SchemeData;
        copy.Realm = source.Realm;
        copy.Nonce = source.Nonce;
        copy.Opaque = source.Opaque;
        copy.Algorithm = source.Algorithm;
        copy.AlgorithmToken = source.AlgorithmToken;
        copy.Qualities = CopyQualities(source.Qualities);
        copy.QualityTokens = CopyStrings(source.QualityTokens);
        copy.Domain = source.Domain;
        copy.Stale = source.Stale;
        copy.HasStale = source.HasStale;
        copy.Utf8 = source.Utf8;
        copy.UserHash = source.UserHash;
        copy.HasUserHash = source.HasUserHash;
        copy.Parameters = CopyParameters(source.Parameters);
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipAuthenticationInfoSet ParseAuthenticationInformation(
        SipMessage message, SipCredentialScope scope)
    {
        SipAuthenticationInfoSet result = new SipAuthenticationInfoSet();
        if (message.Headers == null)
        {
            result.Malformed = true;
            return result;
        }
        SipParseOptions options = new SipParseOptions();
        string selectedName = SipHeaderNames.AuthenticationInfo;
        if (scope == SipCredentialScope.Proxy
            && MessageHasHeader(message, "Proxy-Authentication-Info"))
        {
            selectedName = "Proxy-Authentication-Info";
        }
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            bool matches = header != null && SipAscii.EqualsIgnoreCase(
                header.Name, selectedName);
            if (matches && header != null)
            {
                if (result.Found)
                {
                    result.Multiple = true;
                }
                result.Found = true;
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        SipHeaderNames.AuthenticationInfo, header.Value, options);
                if (!parsed.Success || parsed.Authorization == null)
                {
                    result.Malformed = true;
                }
                else
                {
                    result.Information = parsed.Authorization;
                }
            }
            index = index + 1;
        }
        return result;
    }

    static bool MessageHasHeader(SipMessage message, string name)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool InformationMatchesExchange(
        SipDigestAuthorization information,
        SipAuthenticationClientCacheEntry entry)
    {
        SipDigestCalculation? calculation = entry.LastCalculation;
        if (information.Response == null)
        {
            return information.NextNonce != null;
        }
        if (calculation == null)
        {
            return false;
        }
        if (entry.Quality == SipDigestQuality.None)
        {
            return information.Quality == SipDigestQuality.None
                && information.ClientNonce == null
                && information.NonceCount < 0;
        }
        return information.Quality == entry.Quality
            && information.ClientNonce == entry.ClientNonce
            && information.NonceCount == entry.NonceCount;
    }

    static SipDigestCalculation CopyCalculation(SipDigestCalculation source)
    {
        SipDigestCalculation copy = new SipDigestCalculation();
        copy.Algorithm = source.Algorithm;
        copy.Quality = source.Quality;
        copy.UserName = source.UserName;
        copy.Realm = source.Realm;
        copy.Secret = CopyBytes(source.Secret);
        copy.Nonce = source.Nonce;
        copy.NonceCount = source.NonceCount;
        copy.ClientNonce = source.ClientNonce;
        copy.Method = source.Method;
        copy.DigestUri = source.DigestUri;
        copy.RequestBody = CopyBytes(source.RequestBody);
        copy.ResponseBody = CopyBytes(source.ResponseBody);
        copy.Utf8 = source.Utf8;
        return copy;
    }

    SipDigestChallenge[] FindCachedChallenges(
        SipCredentialScope scope, string targetKey)
    {
        System.Collections.Generic.List<SipDigestChallenge> values =
            new System.Collections.Generic.List<SipDigestChallenge>();
        lock (gate)
        {
            int index = cache.Count - 1;
            while (index >= 0)
            {
                SipAuthenticationClientCacheEntry entry = cache[index];
                if (entry.Scope == scope && entry.TargetKey == targetKey)
                {
                    values.Add(CopyChallenge(entry.Challenge));
                }
                index = index - 1;
            }
        }
        return values.ToArray();
    }

    int FindCacheEntryIndex(
        SipCredentialScope scope, string targetKey, string realm)
    {
        int index = cache.Count - 1;
        while (index >= 0)
        {
            SipAuthenticationClientCacheEntry entry = cache[index];
            if (entry.Scope == scope && entry.TargetKey == targetKey
                && entry.Realm == realm)
            {
                return index;
            }
            index = index - 1;
        }
        return -1;
    }

    int FindVerificationEntryIndex(SipCredentialScope scope,
        string? targetKey, string? correlationId,
        SipDigestAuthorization? information)
    {
        int index = cache.Count - 1;
        while (index >= 0)
        {
            SipAuthenticationClientCacheEntry entry = cache[index];
            bool targetMatches = targetKey == null
                || entry.TargetKey == targetKey;
            bool correlationMatches = correlationId == null
                || entry.CorrelationId == correlationId;
            bool informationMatches = information == null
                || information.Response == null
                || entry.Quality == SipDigestQuality.None
                || information.ClientNonce == entry.ClientNonce
                && information.NonceCount == entry.NonceCount;
            if (entry.Scope == scope && targetMatches && correlationMatches
                && informationMatches && (targetKey != null
                    || correlationId != null))
            {
                return index;
            }
            index = index - 1;
        }
        return -1;
    }

    void MoveCacheEntryToEnd(int index)
    {
        SipAuthenticationClientCacheEntry entry = cache[index];
        cache.RemoveAt(index);
        cache.Add(entry);
    }

    void TrimCache()
    {
        while (cache.Count > MaximumCacheEntries)
        {
            RemoveCacheEntry(0);
        }
    }

    void RemoveCacheEntry(int index)
    {
        ClearCalculation(cache[index].LastCalculation);
        cache.RemoveAt(index);
    }

    void ClearVerificationContext(SipAuthenticationRequest request)
    {
        string? targetKey = FormatTargetKey(GetTarget(request));
        lock (gate)
        {
            int index = FindVerificationEntryIndex(
                request.Scope, targetKey, request.CorrelationId, null);
            if (index >= 0)
            {
                ClearCalculation(cache[index].LastCalculation);
                cache[index].LastCalculation = null;
            }
        }
    }

    static void ClearCalculation(SipDigestCalculation? calculation)
    {
        if (calculation != null && calculation.Secret != null)
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(
                calculation.Secret);
        }
    }

    static void AddAuthorizationHeader(
        SipMessage message, SipCredentialScope scope, string value)
    {
        string name = scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthorization : SipHeaderNames.Authorization;
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (!SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                headers.Add(header);
            }
            index = index + 1;
        }
        SipHeader authorization = new SipHeader();
        authorization.Name = name;
        authorization.Value = value;
        headers.Add(authorization);
        message.Headers = headers.ToArray();
        message.OriginalBytes = new byte[0];
    }

    static SipAuthenticationResult CreateFailure(
        SipAuthenticationStatus status, string message,
        SipAuthenticationRequest request)
    {
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = status;
        result.ErrorMessage = message;
        result.RetryAllowed = false;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static SipAuthenticationResult CreateVerificationSuccess(
        SipAuthenticationRequest request, SipDigestAuthorization? information,
        string? identity)
    {
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.Message = SipSnapshotCopy.CopyMessageModel(request.Message);
        result.DigestAuthorization = information;
        result.Identity = identity;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static SipUri? GetTarget(SipAuthenticationRequest request)
    {
        if (request.Target != null)
        {
            return request.Target;
        }
        return request.Message == null ? null : request.Message.RequestUri;
    }

    static string? FormatTargetKey(SipUri? target)
    {
        if (target == null)
        {
            return null;
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.MaxHeaderLineBytes = int.MaxValue;
        SipWriteResult result = SipUriFormatter.FormatUriModel(target, options);
        return result.Success
            ? System.Text.Encoding.ASCII.GetString(result.Bytes) : null;
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

    static byte[] SelectBody(SipAuthenticationRequest request)
    {
        return CopyBytes(request.Body);
    }

    static byte[] CopyBytes(byte[]? source)
    {
        return source == null ? new byte[0] : (byte[])source.Clone();
    }

    static SipDigestQuality[] CopyQualities(SipDigestQuality[]? source)
    {
        return source == null
            ? new SipDigestQuality[0] : (SipDigestQuality[])source.Clone();
    }

    static string[] CopyStrings(string[]? source)
    {
        return source == null ? new string[0] : (string[])source.Clone();
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
            SipUriParameter value = new SipUriParameter();
            value.Name = source[index].Name;
            value.Value = source[index].Value;
            value.HasValue = source[index].HasValue;
            value.OriginalText = source[index].OriginalText;
            value.ComparisonNameSource = source[index].ComparisonNameSource;
            value.ComparisonNameValue = source[index].ComparisonNameValue;
            value.ComparisonValueSource = source[index].ComparisonValueSource;
            value.ComparisonValueValue = source[index].ComparisonValueValue;
            value.Quoted = source[index].Quoted;
            result[index] = value;
            index = index + 1;
        }
        return result;
    }

    static bool ContainsRealm(
        System.Collections.Generic.List<string> realms, string realm)
    {
        int index = 0;
        while (index < realms.Count)
        {
            if (realms[index] == realm)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsNonAscii(string? value)
    {
        if (value == null)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string EncodeExtendedUserName(string? value)
    {
        string normalized = value == null ? "" : value.Normalize(
            System.Text.NormalizationForm.FormC);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(
            "UTF-8''");
        const string digits = "0123456789ABCDEF";
        int index = 0;
        while (index < bytes.Length)
        {
            int octet = bytes[index];
            if (octet >= 'a' && octet <= 'z'
                || octet >= 'A' && octet <= 'Z'
                || octet >= '0' && octet <= '9'
                || octet == '!' || octet == '#' || octet == '$'
                || octet == '&' || octet == '+' || octet == '-'
                || octet == '.' || octet == '^' || octet == '_'
                || octet == '`' || octet == '|' || octet == '~')
            {
                builder.Append((char)octet);
            }
            else
            {
                builder.Append('%');
                builder.Append(digits[octet >> 4]);
                builder.Append(digits[octet & 15]);
            }
            index = index + 1;
        }
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        return builder.ToString();
    }

    static string GetCanonicalAlgorithmToken(SipDigestAlgorithm algorithm)
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

    static bool IsAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
