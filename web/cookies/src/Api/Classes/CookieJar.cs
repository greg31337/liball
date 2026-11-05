namespace cookies;

/// <summary>
/// Coordinates cookie parsing, policy, storage, selection, management, and snapshots.
/// </summary>
public class CookieJar : ICookieJar
{
    internal CookieJarOptions Options;
    internal ICookieParser Parser;
    internal ICookiePolicy Policy;
    internal ICookieStore Store;
    internal ICookieSerializer Serializer;
    internal ICookieClock Clock;

    /// <summary>
    /// Creates a cookie jar from explicit options and pluggable processing services.
    /// </summary>
    /// <param name="options">The effective cookie-jar options.</param>
    /// <param name="parser">The Set-Cookie and Cookie field parser.</param>
    /// <param name="policy">The cookie acceptance and return policy.</param>
    /// <param name="store">The canonical cookie store.</param>
    /// <param name="serializer">The Cookie field serializer.</param>
    /// <param name="clock">The deterministic UTC clock.</param>
    /// <exception cref="CookiePolicyException">The options or dependency graph is invalid.</exception>
    public CookieJar(
        CookieJarOptions options,
        ICookieParser parser,
        ICookiePolicy policy,
        ICookieStore store,
        ICookieSerializer serializer,
        ICookieClock clock)
    {
        if (!CookieServiceConfigurationValidator.IsValidJarConfiguration(
            options,
            parser,
            policy,
            store,
            serializer,
            clock))
        {
            throw new CookiePolicyException(
                CookieServiceConfigurationValidator.CreateFailureMessage());
        }

        Options = CookieGraphCloner.CloneJarOptions(options);
        Parser = parser;
        Policy = policy;
        Store = store;
        Serializer = serializer;
        Clock = clock;
    }

    /// <summary>
    /// Processes one raw Set-Cookie field value.
    /// </summary>
    /// <param name="fieldValue">The Set-Cookie field value.</param>
    /// <param name="context">The cookie-setting context.</param>
    /// <returns>The storage outcome.</returns>
    public CookieSetResult StoreSetCookieHeader(
        string fieldValue,
        CookieSetContext context)
    {
        if (Parser == null)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)],
                null);
        }

        SetCookieParseResult suppliedParseResult = Parser.TryParseSetCookie(
            fieldValue,
            Options.ParseOptions);
        if (suppliedParseResult == null)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)],
                null);
        }

        SetCookieParseResult parseResult =
            CookieGraphCloner.CloneSetCookieParseResult(suppliedParseResult);

        if (!parseResult.Success || parseResult.Cookie == null)
        {
            CookieErrorCode errorCode = parseResult.ErrorCode;
            if (errorCode == CookieErrorCode.None)
            {
                errorCode = CookieErrorCode.MalformedSyntax;
            }

            if (parseResult.Issues == null || parseResult.Issues.Length == 0)
            {
                parseResult.Issues = [CreateIssue(errorCode)];
            }

            return CreateRejectedResult(errorCode, [], parseResult);
        }

        return StoreAcceptedModel(parseResult.Cookie, context, parseResult);
    }

    /// <summary>
    /// Processes independent raw Set-Cookie field values in order.
    /// </summary>
    /// <param name="fieldValues">The Set-Cookie field values.</param>
    /// <param name="context">The cookie-setting context.</param>
    /// <returns>The ordered batch outcome.</returns>
    public CookieBatchSetResult StoreSetCookieHeaders(
        string[] fieldValues,
        CookieSetContext context)
    {
        if (fieldValues == null)
        {
            CookieSetResult rejected = CreateRejectedResult(
                CookieErrorCode.InvalidAttribute,
                [CreateIssue(CookieErrorCode.InvalidAttribute)],
                null);
            return CreateBatchResult([rejected]);
        }

        string[] ownedFields = CookieGraphCloner.CloneStrings(fieldValues);
        CookieSetResult[] results = new CookieSetResult[ownedFields.Length];
        int index = 0;
        while (index < ownedFields.Length)
        {
            results[index] = StoreSetCookieHeader(ownedFields[index], context);
            index = index + 1;
        }

        return CreateBatchResult(results);
    }

    /// <summary>
    /// Processes a producer-supplied Set-Cookie model.
    /// </summary>
    /// <param name="cookie">The cookie model.</param>
    /// <param name="context">The cookie-setting context.</param>
    /// <returns>The storage outcome.</returns>
    public CookieSetResult StoreSetCookieModel(
        SetCookie cookie,
        CookieSetContext context)
    {
        return StoreAcceptedModel(cookie, context, null);
    }

    /// <summary>
    /// Selects stored cookies for a request.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The ordered selection outcome.</returns>
    public CookieSelectionResult SelectRequestCookies(CookieRequestContext context)
    {
        if (context == null || context.RequestUri == null)
        {
            return CreateSelectionFailure(CookieErrorCode.InvalidHost);
        }

        if (Policy == null || Store == null)
        {
            return CreateSelectionFailure(CookieErrorCode.InvalidConfiguration);
        }

        CookieClockResult accessTime = CookieClockEvaluator.Evaluate(Clock);
        if (!accessTime.Success)
        {
            return CreateSelectionFailure(CookieErrorCode.InvalidConfiguration);
        }

        CookieCanonicalHostResult requestHost =
            CookieMatcher.CanonicalizeUriHost(context.RequestUri);
        if (!requestHost.Success)
        {
            return CreateSelectionFailure(requestHost.ErrorCode);
        }

        CookieRequestContext effectiveContext =
            CookieGraphCloner.CloneRequestContext(context);
        StoredCookie[] candidates = FindRequestCandidates(
            requestHost,
            effectiveContext);
        global::System.Collections.Generic.List<CookieMatchResult> matches =
            new global::System.Collections.Generic.List<CookieMatchResult>();
        global::System.Collections.Generic.List<CookieMatchResult> included =
            new global::System.Collections.Generic.List<CookieMatchResult>();
        CookieIssue[] issues = [];
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            CookieMatchResult suppliedMatch = Policy.EvaluateCookieReturn(
                CookieGraphCloner.CloneStoredCookie(candidates[candidateIndex]),
                CookieGraphCloner.CloneRequestContext(effectiveContext));
            if (suppliedMatch == null)
            {
                return CreateSelectionFailure(CookieErrorCode.InvalidConfiguration);
            }

            CookieMatchResult match =
                CookieGraphCloner.CloneMatchResult(suppliedMatch);
            matches.Add(match);
            issues = CombineIssues(issues, match.Issues);
            if (match.Status == CookieMatchStatus.Included)
            {
                if (match.Cookie == null || match.Pair == null)
                {
                    return CreateSelectionFailure(CookieErrorCode.InvalidConfiguration);
                }

                match.Cookie = CookieGraphCloner.CloneStoredCookie(
                    candidates[candidateIndex]);
                match.Pair = CreateRequestPair(candidates[candidateIndex]);
                included.Add(CookieGraphCloner.CloneMatchResult(match));
            }

            candidateIndex = candidateIndex + 1;
        }

        CookieMatchResult[] orderedIncluded = included.ToArray();
        SortIncludedMatches(orderedIncluded);
        CookieIdentity[] selectedIdentities =
            new CookieIdentity[orderedIncluded.Length];
        int identityIndex = 0;
        while (identityIndex < orderedIncluded.Length)
        {
            StoredCookie selectedCookie =
                orderedIncluded[identityIndex].Cookie ?? new StoredCookie();
            selectedIdentities[identityIndex] = CookieIdentityUtility.CreateFromStoredCookie(
                selectedCookie);
            identityIndex = identityIndex + 1;
        }

        StoredCookie[] updatedCookies = Store.UpdateStoredCookieAccess(
            selectedIdentities,
            accessTime.Value);
        CookiePair[] pairs = new CookiePair[updatedCookies.Length];
        int pairIndex = 0;
        while (pairIndex < updatedCookies.Length)
        {
            pairs[pairIndex] = CreateRequestPair(updatedCookies[pairIndex]);
            pairIndex = pairIndex + 1;
        }

        CookieMatchResult[] matchResults = matches.ToArray();
        ApplyUpdatedMatchCookies(matchResults, updatedCookies);
        CookieSelectionResult result = new CookieSelectionResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Cookies = CookieGraphCloner.CloneStoredCookies(updatedCookies);
        result.Pairs = CookieGraphCloner.ClonePairs(pairs);
        result.Matches = CookieGraphCloner.CloneMatchResults(matchResults);
        result.Issues = CookieGraphCloner.CloneIssues(issues);
        return result;
    }

    /// <summary>
    /// Selects and serializes Cookie request fields.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The ordered selection and field values.</returns>
    public CookieSelectionResult CreateRequestCookieFields(
        CookieRequestContext context)
    {
        if (Serializer == null)
        {
            return CreateSelectionFailure(CookieErrorCode.InvalidConfiguration);
        }

        CookieSelectionResult selection = SelectRequestCookies(context);
        if (!selection.Success)
        {
            return CookieGraphCloner.CloneSelectionResult(selection);
        }

        CookieSelectionResult suppliedSerialization = Serializer.SerializeCookieFields(
            CookieGraphCloner.ClonePairs(selection.Pairs),
            CookieGraphCloner.CloneSerializationOptions(
                Options.SerializationOptions));
        if (suppliedSerialization == null)
        {
            return CreateSelectionSerializationFailure(
                selection,
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)]);
        }

        CookieSelectionResult serialization =
            CookieGraphCloner.CloneSelectionResult(suppliedSerialization);
        if (!serialization.Success)
        {
            CookieErrorCode errorCode = serialization.ErrorCode;
            CookieIssue[] serializationIssues = serialization.Issues;
            if (errorCode == CookieErrorCode.None)
            {
                errorCode = CookieErrorCode.InvalidConfiguration;
                serializationIssues = CombineIssues(
                    serializationIssues,
                    [CreateIssue(CookieErrorCode.InvalidConfiguration)]);
            }

            return CreateSelectionSerializationFailure(
                selection,
                errorCode,
                serializationIssues);
        }

        CookieSelectionResult result =
            CookieGraphCloner.CloneSelectionResult(selection);
        result.FieldValues = CookieGraphCloner.CloneStrings(
            serialization.FieldValues);
        result.Issues = CombineIssues(selection.Issues, serialization.Issues);
        return result;
    }

    /// <summary>
    /// Queries cookies retained by the jar.
    /// </summary>
    /// <param name="query">The management query.</param>
    /// <returns>Independent matching stored cookie models.</returns>
    /// <exception cref="CookieStoreException">The store dependency or query is invalid.</exception>
    public StoredCookie[] QueryJarCookies(CookieStoreQuery query)
    {
        if (Store == null || !CookieStoreQueryEvaluator.IsValidStoreQuery(query))
        {
            throw new CookieStoreException("The cookie store dependency or query is invalid.");
        }

        CookieStoreQuery ownedQuery = CookieGraphCloner.CloneStoreQuery(query);
        StoredCookie[] suppliedCookies = Store.QueryStoredCookies(ownedQuery);
        if (suppliedCookies == null)
        {
            throw new CookieStoreException("The cookie store returned invalid query state.");
        }

        return CookieGraphCloner.CloneStoredCookies(suppliedCookies);
    }

    /// <summary>
    /// Removes cookies selected by management filters.
    /// </summary>
    /// <param name="query">The removal query.</param>
    /// <returns>The number of cookies removed.</returns>
    /// <exception cref="CookieStoreException">The store dependency or query is invalid.</exception>
    public int RemoveJarCookies(CookieRemovalQuery query)
    {
        if (Store == null || !CookieStoreQueryEvaluator.IsValidRemovalQuery(query))
        {
            throw new CookieStoreException("The cookie store dependency or removal query is invalid.");
        }

        CookieRemovalQuery ownedQuery =
            CookieGraphCloner.CloneRemovalQuery(query);
        int removed = Store.RemoveStoredCookies(ownedQuery);
        if (removed < 0)
        {
            throw new CookieStoreException("The cookie store returned an invalid removal count.");
        }

        return removed;
    }

    /// <summary>
    /// Removes cookies expired at the injected clock instant.
    /// </summary>
    /// <returns>The number of cookies removed.</returns>
    /// <exception cref="CookieStoreException">The clock or store dependency fails.</exception>
    public int PurgeExpiredCookies()
    {
        if (Store == null)
        {
            throw new CookieStoreException("The cookie store dependency is invalid.");
        }

        CookieClockResult evaluation = CookieClockEvaluator.Evaluate(Clock);
        if (!evaluation.Success)
        {
            throw new CookieStoreException("The cookie clock dependency is invalid.");
        }

        CookieRemovalQuery query = new CookieRemovalQuery();
        query.ExpiredOnly = true;
        query.HasEvaluationTime = true;
        query.EvaluationTime = evaluation.Value;
        return RemoveJarCookies(query);
    }

    /// <summary>
    /// Captures a lossless cookie-jar snapshot.
    /// </summary>
    /// <returns>The independent snapshot model.</returns>
    /// <exception cref="CookieStoreException">The store dependency fails.</exception>
    public CookieJarSnapshot ExportJarSnapshot()
    {
        if (Store == null)
        {
            throw new CookieStoreException("The cookie store dependency is invalid.");
        }

        return Store.CaptureStoreSnapshot();
    }

    /// <summary>
    /// Imports a validated cookie-jar snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to import.</param>
    /// <param name="mode">The merge or replacement behavior.</param>
    /// <returns>The import outcome.</returns>
    public CookieImportResult ImportJarSnapshot(
        CookieJarSnapshot snapshot,
        CookieImportMode mode)
    {
        if ((mode != CookieImportMode.Merge && mode != CookieImportMode.Replace) ||
            Policy == null || Store == null)
        {
            return CreateImportFailure(mode, CookieErrorCode.InvalidConfiguration);
        }

        CookieErrorCode snapshotError =
            CookieSnapshotValidator.ValidateSnapshotForWriting(snapshot);
        if (snapshotError != CookieErrorCode.None)
        {
            return CreateImportFailure(mode, snapshotError);
        }

        CookieClockResult evaluation = CookieClockEvaluator.Evaluate(Clock);
        if (!evaluation.Success)
        {
            return CreateImportFailure(mode, CookieErrorCode.InvalidConfiguration);
        }

        CookieJarSnapshot before = Store.CaptureStoreSnapshot();
        MemoryCookieStore working = new MemoryCookieStore();
        if (mode == CookieImportMode.Merge)
        {
            working.ReplaceStoreSnapshot(before);
        }

        global::System.Collections.Generic.List<StoredCookie> replacements =
            new global::System.Collections.Generic.List<StoredCookie>();
        CookieSetResult[] results = new CookieSetResult[snapshot.Cookies.Length];
        CookieIssue[] issues = [];
        int index = 0;
        while (index < snapshot.Cookies.Length)
        {
            StoredCookie cookie = CookieGraphCloner.CloneStoredCookie(
                snapshot.Cookies[index]);
            CookieSetResult entry;
            if (CookieExpiryEvaluator.IsExpired(cookie, evaluation.Value))
            {
                CookieRemovalQuery removal = new CookieRemovalQuery();
                removal.HasIdentity = true;
                removal.Identity = CookieIdentityUtility.CreateFromStoredCookie(cookie);
                int removed = mode == CookieImportMode.Merge ?
                    working.RemoveStoredCookies(removal) : 0;
                entry = CreateImportEntry(
                    removed > 0 ? CookieSetStatus.Deleted : CookieSetStatus.Ignored,
                    CookieErrorCode.Expired,
                    null,
                    [CreateIssue(CookieErrorCode.Expired)]);
            }
            else
            {
                CookieMatchResult validation = EvaluateImportedCookie(cookie);
                if (validation.Status != CookieMatchStatus.Included)
                {
                    CookieErrorCode errorCode = validation.ErrorCode;
                    if (errorCode == CookieErrorCode.None)
                    {
                        errorCode = CookieErrorCode.PolicyRejection;
                    }

                    entry = CreateImportEntry(
                        CookieSetStatus.Rejected,
                        errorCode,
                        null,
                        validation.Issues);
                }
                else if (mode == CookieImportMode.Replace)
                {
                    replacements.Add(cookie);
                    entry = CreateImportEntry(
                        CookieSetStatus.Accepted,
                        CookieErrorCode.None,
                        cookie,
                        validation.Issues);
                }
                else
                {
                    CookieSetResult applied = working.ApplyStoredCookie(cookie);
                    entry = CookieGraphCloner.CloneSetResult(applied);
                    entry.Issues = CombineIssues(validation.Issues, applied.Issues);
                }
            }

            results[index] = entry;
            issues = CombineIssues(issues, entry.Issues);
            index = index + 1;
        }

        if (mode == CookieImportMode.Replace)
        {
            CookieJarSnapshot replacement = new CookieJarSnapshot();
            replacement.Cookies = replacements.ToArray();
            replacement.NextSequenceNumber = snapshot.NextSequenceNumber;
            working.ReplaceStoreSnapshot(replacement);
        }

        CookieJarSnapshot candidate = working.CaptureStoreSnapshot();
        CookieEvictionResult eviction = CookieEvictionAlgorithm.Plan(
            candidate.Cookies,
            Options.Limits,
            evaluation.Value,
            Options.EvictNonSecureCookiesFirst);
        if (!eviction.Success)
        {
            return CreateImportFailure(mode, eviction.ErrorCode);
        }

        CookieJarSnapshot finalSnapshot = new CookieJarSnapshot();
        finalSnapshot.Cookies = eviction.RetainedCookies;
        finalSnapshot.NextSequenceNumber = candidate.NextSequenceNumber;
        Store.ReplaceStoreSnapshot(finalSnapshot);
        issues = CombineIssues(issues, eviction.Issues);
        ApplyImportEvictionResults(results, eviction);
        CookieImportResult result = CreateImportSuccess(
            mode,
            results,
            issues);
        if (mode == CookieImportMode.Replace)
        {
            result.RemovedCount = before.Cookies.Length;
        }

        return result;
    }

    internal CookieMatchResult EvaluateImportedCookie(StoredCookie cookie)
    {
        CookieValidationResult validation =
            CookieAcceptanceValidator.ValidateStoredCookie(cookie, Options);
        if (!validation.Success)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                validation.ErrorCode,
                validation.Issues);
        }

        if (cookie.HasPartitionKey && !Options.EnablePartitionedCookies)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                CookieErrorCode.PartitionMismatch,
                [CreateIssue(CookieErrorCode.PartitionMismatch)]);
        }

        if (cookie.HasPartitionKey && !cookie.Secure)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                CookieErrorCode.InsecureSecureCookie,
                [CreateIssue(CookieErrorCode.InsecureSecureCookie)]);
        }

        if (Options.RequireSecureForSameSiteNone && cookie.HasSameSite &&
            cookie.SameSiteMode == CookieSameSiteMode.None && !cookie.Secure)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                CookieErrorCode.SameSiteViolation,
                [CreateIssue(CookieErrorCode.SameSiteViolation)]);
        }

        if (HasInvalidImportedPrefix(cookie))
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                CookieErrorCode.PrefixViolation,
                [CreateIssue(CookieErrorCode.PrefixViolation)]);
        }

        CookieImportUriResult uri = CreateImportValidationUri(cookie);
        if (!uri.Success || uri.Value == null)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                CookieErrorCode.InvalidHost,
                [CreateIssue(CookieErrorCode.InvalidHost)]);
        }

        CookieRequestContext context = new CookieRequestContext();
        context.RequestUri = uri.Value;
        context.RequestMethod = "GET";
        context.SameSiteContext = CookieSameSiteContext.SameSite;
        context.AccessKind = CookieAccessKind.Http;
        context.IsTopLevelNavigation = true;
        context.HasTopLevelSite = cookie.HasPartitionKey;
        context.TopLevelSite = cookie.PartitionKey;
        CookieMatchResult result = Policy.EvaluateCookieReturn(cookie, context);
        if (result == null)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)]);
        }

        return CookieGraphCloner.CloneMatchResult(result);
    }

    internal bool HasInvalidImportedPrefix(StoredCookie cookie)
    {
        if (Options.PrefixPolicy == CookiePrefixPolicy.Disabled)
        {
            return false;
        }

        bool ignoreCase = Options.ComplianceMode == CookieComplianceMode.Rfc6265Bis ||
            Options.UseCaseInsensitivePrefixes;
        bool host = CookiePrefixValidator.StartsWithPrefix(
            cookie.Name,
            CookieAttributeNames.HostPrefix,
            ignoreCase);
        bool secure = !host && CookiePrefixValidator.StartsWithPrefix(
            cookie.Name,
            CookieAttributeNames.SecurePrefix,
            ignoreCase);
        if (!host && !secure)
        {
            return false;
        }

        return !cookie.Secure ||
            (host && (!cookie.HostOnly || cookie.Path != "/"));
    }

    internal static CookieImportUriResult CreateImportValidationUri(
        StoredCookie cookie)
    {
        string host = cookie.Domain;
        if (host.IndexOf(':') >= 0)
        {
            host = $"[{host}]";
        }

        string value = $"https://{host}{cookie.Path}";
        global::System.Threading.Tasks.Task<global::System.Uri> uriTask =
            new global::System.Threading.Tasks.Task<global::System.Uri>(
                () => new global::System.Uri(value));
        uriTask.RunSynchronously(
            global::System.Threading.Tasks.TaskScheduler.Default);
        CookieImportUriResult result = new CookieImportUriResult();
        if (!uriTask.IsCompletedSuccessfully)
        {
            uriTask.Exception?.Handle(exception => true);
            return result;
        }

        result.Success = true;
        result.Value = uriTask.Result;
        return result;
    }

    internal static CookieSetResult CreateImportEntry(
        CookieSetStatus status,
        CookieErrorCode errorCode,
        StoredCookie? cookie,
        CookieIssue[] issues)
    {
        CookieSetResult result = new CookieSetResult();
        result.Status = status;
        result.ErrorCode = errorCode;
        result.Issues = CookieGraphCloner.CloneIssues(issues);
        if (cookie != null)
        {
            result.Cookie = CookieGraphCloner.CloneStoredCookie(cookie);
        }

        return result;
    }

    internal static void ApplyImportEvictionResults(
        CookieSetResult[] results,
        CookieEvictionResult eviction)
    {
        int evictionIndex = 0;
        while (evictionIndex < eviction.Evictions.Length)
        {
            StoredCookie evicted = eviction.Evictions[evictionIndex].Cookie;
            int resultIndex = 0;
            while (resultIndex < results.Length)
            {
                CookieSetResult result = results[resultIndex];
                if (result.Cookie != null &&
                    CookieIdentityUtility.HaveSameStoredIdentity(
                        result.Cookie,
                        evicted))
                {
                    result.Cookie = null;
                    result.Issues = CombineIssues(
                        result.Issues,
                        [CreateIssue(CookieErrorCode.QuotaEviction)]);
                }

                resultIndex = resultIndex + 1;
            }

            evictionIndex = evictionIndex + 1;
        }
    }

    internal static CookieImportResult CreateImportSuccess(
        CookieImportMode mode,
        CookieSetResult[] results,
        CookieIssue[] issues)
    {
        CookieImportResult import = new CookieImportResult();
        import.Success = true;
        import.Mode = mode;
        import.ErrorCode = CookieErrorCode.None;
        import.Results = CookieGraphCloner.CloneSetResults(results);
        import.Issues = CookieGraphCloner.CloneIssues(issues);
        int index = 0;
        while (index < import.Results.Length)
        {
            CookieSetStatus status = import.Results[index].Status;
            if (status == CookieSetStatus.Accepted)
            {
                import.AcceptedCount = import.AcceptedCount + 1;
            }
            else if (status == CookieSetStatus.Replaced)
            {
                import.ReplacedCount = import.ReplacedCount + 1;
            }
            else if (status == CookieSetStatus.Deleted)
            {
                import.DeletedCount = import.DeletedCount + 1;
            }
            else if (status == CookieSetStatus.Ignored)
            {
                import.IgnoredCount = import.IgnoredCount + 1;
            }
            else
            {
                import.RejectedCount = import.RejectedCount + 1;
            }

            index = index + 1;
        }

        return import;
    }

    internal CookieSetResult StoreAcceptedModel(
        SetCookie cookie,
        CookieSetContext context,
        SetCookieParseResult? parseResult)
    {
        if (cookie == null || context == null)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidAttribute,
                [CreateIssue(CookieErrorCode.InvalidAttribute)],
                parseResult);
        }

        if (Policy == null || Store == null)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)],
                parseResult);
        }

        CookieClockResult receiptTime = CookieClockEvaluator.Evaluate(Clock);
        if (!receiptTime.Success)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)],
                parseResult);
        }

        CookieRemovalQuery expired = new CookieRemovalQuery();
        expired.ExpiredOnly = true;
        expired.HasEvaluationTime = true;
        expired.EvaluationTime = receiptTime.Value;
        Store.RemoveStoredCookies(expired);

        CookieSetContext effectiveContext = CookieGraphCloner.CloneSetContext(context);
        effectiveContext.Options = CookieGraphCloner.CloneJarOptions(Options);
        effectiveContext.ExistingCookie = null;
        effectiveContext.ExistingCookies = Store.QueryStoredCookies(new CookieStoreQuery());
        CookieSetDecision decision = Policy.EvaluateCookieAcceptance(
            CookieGraphCloner.CloneSetCookie(cookie),
            effectiveContext);
        if (decision == null)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidConfiguration,
                [CreateIssue(CookieErrorCode.InvalidConfiguration)],
                parseResult);
        }

        if (decision.Status != CookieSetStatus.Accepted &&
            decision.Status != CookieSetStatus.Replaced)
        {
            return CreateDecisionResult(decision, parseResult);
        }

        StoredCookie? normalized = decision.NormalizedCookie;
        if (normalized == null)
        {
            return CreateRejectedResult(
                CookieErrorCode.InvalidConfiguration,
                CombineIssues(
                    decision.Issues,
                    [CreateIssue(CookieErrorCode.InvalidConfiguration)]),
                parseResult);
        }

        CookieJarSnapshot beforeMutation = Store.CaptureStoreSnapshot();
        CookieSetResult stored = Store.ApplyStoredCookie(normalized);
        CookieJarSnapshot afterMutation = Store.CaptureStoreSnapshot();
        CookieEvictionResult eviction = CookieEvictionAlgorithm.Plan(
            afterMutation.Cookies,
            Options.Limits,
            receiptTime.Value,
            Options.EvictNonSecureCookiesFirst);
        if (!eviction.Success)
        {
            Store.ReplaceStoreSnapshot(beforeMutation);
            return CreateRejectedResult(
                eviction.ErrorCode,
                CombineIssues(decision.Issues, eviction.Issues),
                parseResult);
        }

        bool appliedCookieWasEvicted = ExecuteEvictions(eviction, stored.Cookie);
        CookieSetResult result = CookieGraphCloner.CloneSetResult(stored);
        result.ParseResult = parseResult == null ?
            null : CookieGraphCloner.CloneSetCookieParseResult(parseResult);
        CookieIssue[] parseIssues = [];
        if (parseResult != null)
        {
            parseIssues = parseResult.Issues;
        }

        result.Issues = CombineIssues(
            CombineIssues(
                CombineIssues(parseIssues, decision.Issues),
                stored.Issues),
            eviction.Issues);
        if (appliedCookieWasEvicted)
        {
            result.Cookie = null;
        }

        return result;
    }

    internal bool ExecuteEvictions(
        CookieEvictionResult eviction,
        StoredCookie? appliedCookie)
    {
        bool appliedCookieWasEvicted = false;
        int index = 0;
        while (index < eviction.Evictions.Length)
        {
            StoredCookie evicted = eviction.Evictions[index].Cookie;
            CookieRemovalQuery removal = new CookieRemovalQuery();
            removal.HasIdentity = true;
            removal.Identity = CookieIdentityUtility.CreateFromStoredCookie(evicted);
            Store.RemoveStoredCookies(removal);
            if (appliedCookie != null &&
                CookieIdentityUtility.HaveSameStoredIdentity(appliedCookie, evicted))
            {
                appliedCookieWasEvicted = true;
            }

            index = index + 1;
        }

        return appliedCookieWasEvicted;
    }

    internal StoredCookie[] FindRequestCandidates(
        CookieCanonicalHostResult requestHost,
        CookieRequestContext context)
    {
        global::System.Collections.Generic.Dictionary<string, StoredCookie> candidates =
            new global::System.Collections.Generic.Dictionary<string, StoredCookie>();
        string domain = requestHost.Host;
        bool continueDomains = true;
        while (continueDomains)
        {
            AddRequestCandidates(candidates, domain, false, "");
            if (Options.EnablePartitionedCookies && context.HasTopLevelSite &&
                context.TopLevelSite != null && context.TopLevelSite.Length > 0)
            {
                AddRequestCandidates(
                    candidates,
                    domain,
                    true,
                    context.TopLevelSite);
            }

            int dot = domain.IndexOf('.');
            if (!requestHost.IsDnsName || dot < 0)
            {
                continueDomains = false;
            }
            else
            {
                domain = domain.Substring(dot + 1);
            }
        }

        global::System.Collections.Generic.List<StoredCookie> values =
            new global::System.Collections.Generic.List<StoredCookie>();
        foreach (global::System.Collections.Generic.KeyValuePair<string, StoredCookie> entry
            in candidates)
        {
            values.Add(entry.Value);
        }

        StoredCookie[] result = values.ToArray();
        SortCandidateCookies(result);
        return result;
    }

    internal void AddRequestCandidates(
        global::System.Collections.Generic.Dictionary<string, StoredCookie> candidates,
        string domain,
        bool hasPartitionKey,
        string partitionKey)
    {
        CookieStoreQuery query = new CookieStoreQuery();
        query.HasDomain = true;
        query.Domain = domain;
        query.FilterByPartition = true;
        query.HasPartitionKey = hasPartitionKey;
        query.PartitionKey = partitionKey;
        StoredCookie[] found = Store.QueryStoredCookies(query);
        int index = 0;
        while (index < found.Length)
        {
            string key = CookieIdentityUtility.CreateStoredCookieKey(found[index]);
            if (!candidates.ContainsKey(key))
            {
                candidates.Add(key, CookieGraphCloner.CloneStoredCookie(found[index]));
            }

            index = index + 1;
        }
    }

    internal static void SortCandidateCookies(StoredCookie[] cookies)
    {
        int index = 1;
        while (index < cookies.Length)
        {
            StoredCookie value = cookies[index];
            int insertion = index;
            while (insertion > 0 &&
                CompareCandidateCookies(cookies[insertion - 1], value) > 0)
            {
                cookies[insertion] = cookies[insertion - 1];
                insertion = insertion - 1;
            }

            cookies[insertion] = value;
            index = index + 1;
        }
    }

    internal static int CompareCandidateCookies(
        StoredCookie left,
        StoredCookie right)
    {
        if (left.SequenceNumber < right.SequenceNumber)
        {
            return -1;
        }

        if (left.SequenceNumber > right.SequenceNumber)
        {
            return 1;
        }

        return global::System.String.CompareOrdinal(
            CookieIdentityUtility.CreateStoredCookieKey(left),
            CookieIdentityUtility.CreateStoredCookieKey(right));
    }

    internal static void SortIncludedMatches(CookieMatchResult[] matches)
    {
        int index = 1;
        while (index < matches.Length)
        {
            CookieMatchResult value = matches[index];
            int insertion = index;
            while (insertion > 0 &&
                CompareIncludedMatches(matches[insertion - 1], value) > 0)
            {
                matches[insertion] = matches[insertion - 1];
                insertion = insertion - 1;
            }

            matches[insertion] = value;
            index = index + 1;
        }
    }

    internal static int CompareIncludedMatches(
        CookieMatchResult left,
        CookieMatchResult right)
    {
        StoredCookie leftCookie = left.Cookie ?? new StoredCookie();
        StoredCookie rightCookie = right.Cookie ?? new StoredCookie();
        if (leftCookie.Path.Length > rightCookie.Path.Length)
        {
            return -1;
        }

        if (leftCookie.Path.Length < rightCookie.Path.Length)
        {
            return 1;
        }

        int creation = leftCookie.CreationTime.CompareTo(rightCookie.CreationTime);
        if (creation != 0)
        {
            return creation;
        }

        if (leftCookie.SequenceNumber < rightCookie.SequenceNumber)
        {
            return -1;
        }

        if (leftCookie.SequenceNumber > rightCookie.SequenceNumber)
        {
            return 1;
        }

        return global::System.String.CompareOrdinal(
            CookieIdentityUtility.CreateStoredCookieKey(leftCookie),
            CookieIdentityUtility.CreateStoredCookieKey(rightCookie));
    }

    internal static CookiePair CreateRequestPair(StoredCookie cookie)
    {
        CookiePair pair = new CookiePair();
        pair.Name = cookie.Name;
        pair.Value = cookie.Value;
        pair.IsQuoted = cookie.IsValueQuoted;
        return pair;
    }

    internal static void ApplyUpdatedMatchCookies(
        CookieMatchResult[] matches,
        StoredCookie[] updatedCookies)
    {
        int matchIndex = 0;
        while (matchIndex < matches.Length)
        {
            CookieMatchResult match = matches[matchIndex];
            if (match.Status == CookieMatchStatus.Included && match.Cookie != null)
            {
                int cookieIndex = 0;
                while (cookieIndex < updatedCookies.Length)
                {
                    if (CookieIdentityUtility.HaveSameStoredIdentity(
                        match.Cookie,
                        updatedCookies[cookieIndex]))
                    {
                        match.Cookie = CookieGraphCloner.CloneStoredCookie(
                            updatedCookies[cookieIndex]);
                        match.Pair = CreateRequestPair(updatedCookies[cookieIndex]);
                        cookieIndex = updatedCookies.Length;
                    }
                    else
                    {
                        cookieIndex = cookieIndex + 1;
                    }
                }
            }

            matchIndex = matchIndex + 1;
        }
    }

    internal static CookieSetResult CreateRejectedResult(
        CookieErrorCode errorCode,
        CookieIssue[] issues,
        SetCookieParseResult? parseResult)
    {
        CookieSetResult result = new CookieSetResult();
        result.Status = CookieSetStatus.Rejected;
        result.ErrorCode = errorCode;
        result.Issues = CookieGraphCloner.CloneIssues(issues);
        if (parseResult != null)
        {
            result.ParseResult =
                CookieGraphCloner.CloneSetCookieParseResult(parseResult);
            result.Issues = CombineIssues(parseResult.Issues, result.Issues);
        }

        return result;
    }

    internal static CookieSetResult CreateDecisionResult(
        CookieSetDecision decision,
        SetCookieParseResult? parseResult)
    {
        CookieSetResult result = new CookieSetResult();
        result.Status = decision.Status;
        result.ErrorCode = decision.ErrorCode;
        result.Issues = CookieGraphCloner.CloneIssues(decision.Issues);
        if (decision.NormalizedCookie != null)
        {
            result.Cookie = CookieGraphCloner.CloneStoredCookie(
                decision.NormalizedCookie);
        }

        if (parseResult != null)
        {
            result.ParseResult =
                CookieGraphCloner.CloneSetCookieParseResult(parseResult);
            result.Issues = CombineIssues(parseResult.Issues, result.Issues);
        }

        return result;
    }

    internal static CookieBatchSetResult CreateBatchResult(
        CookieSetResult[] results)
    {
        CookieBatchSetResult batch = new CookieBatchSetResult();
        batch.Results = CookieGraphCloner.CloneSetResults(results);
        CookieIssue[] issues = [];
        int index = 0;
        while (index < batch.Results.Length)
        {
            CookieSetResult result = batch.Results[index];
            if (result.Status == CookieSetStatus.Accepted)
            {
                batch.AcceptedCount = batch.AcceptedCount + 1;
            }
            else if (result.Status == CookieSetStatus.Replaced)
            {
                batch.ReplacedCount = batch.ReplacedCount + 1;
            }
            else if (result.Status == CookieSetStatus.Deleted)
            {
                batch.DeletedCount = batch.DeletedCount + 1;
            }
            else if (result.Status == CookieSetStatus.Ignored)
            {
                batch.IgnoredCount = batch.IgnoredCount + 1;
            }
            else
            {
                batch.RejectedCount = batch.RejectedCount + 1;
            }

            issues = CombineIssues(issues, result.Issues);
            index = index + 1;
        }

        batch.Issues = issues;
        return batch;
    }

    internal static CookieIssue[] CombineIssues(
        CookieIssue[] first,
        CookieIssue[] second)
    {
        if (first == null)
        {
            first = [];
        }

        if (second == null)
        {
            second = [];
        }

        CookieIssue[] combined = new CookieIssue[first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            combined[index] = CookieGraphCloner.CloneIssue(first[index]);
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            combined[index] = CookieGraphCloner.CloneIssue(second[secondIndex]);
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return combined;
    }

    internal static CookieIssue CreateIssue(CookieErrorCode errorCode)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    internal static CookieSelectionResult CreateSelectionFailure(
        CookieErrorCode errorCode)
    {
        CookieSelectionResult result = new CookieSelectionResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }

    internal static CookieSelectionResult CreateSelectionSerializationFailure(
        CookieSelectionResult selection,
        CookieErrorCode errorCode,
        CookieIssue[] serializationIssues)
    {
        CookieSelectionResult result =
            CookieGraphCloner.CloneSelectionResult(selection);
        result.Success = false;
        result.ErrorCode = errorCode;
        result.FieldValues = [];
        result.Issues = CombineIssues(selection.Issues, serializationIssues);
        return result;
    }

    internal static CookieImportResult CreateImportFailure(
        CookieImportMode mode,
        CookieErrorCode errorCode)
    {
        CookieImportResult result = new CookieImportResult();
        result.Mode = mode;
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }
}
