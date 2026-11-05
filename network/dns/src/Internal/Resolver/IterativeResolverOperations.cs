using System.Collections.Generic;

namespace dns;

internal class DnsIterativeAuthorityCandidate
{
    internal DnsName Zone = new DnsName();
    internal DnsName NameServer = new DnsName();
    internal DnsEndpoint Endpoint = new DnsEndpoint();
}

internal class DnsIterativeReferralResult
{
    internal bool Found;
    internal bool Invalid;
    internal bool Lame;
    internal string Reason = string.Empty;
    internal DnsDelegation? Delegation;
}

internal class DnsIterativeQueryAttempt
{
    internal bool Usable;
    internal bool Stopped;
    internal DnsResolveStatus FailureStatus = DnsResolveStatus.ServerFailure;
    internal DnsMessage? Response;
}

internal class DnsIterativeQuestionPlan
{
    internal DnsName Name = new DnsName();
    internal int RecordTypeValue;
    internal bool Minimized;
}

internal class DnsIterativeResolverState
{
    internal DnsResolveRequest Request = new DnsResolveRequest();
    internal DnsName CurrentName = new DnsName();
    internal List<DnsName> VisitedNames = new List<DnsName>();
    internal List<DnsName> FullQueryZones = new List<DnsName>();
    internal DnsRecursiveOptions Options = new DnsRecursiveOptions();
    internal DnsRootHints RootHints = new DnsRootHints();
    internal DnsDelegation CurrentDelegation = new DnsDelegation();
    internal DnsSecurityStatus CurrentDelegationSecurityStatus =
        DnsSecurityStatus.Insecure;
    internal bool HasAliasSecurityStatus;
    internal DnsSecurityStatus AliasSecurityStatus =
        DnsSecurityStatus.Secure;
    internal DnsResolveResult Result = new DnsResolveResult();
    internal DnsClockValue Deadline = new DnsClockValue();
    internal IDnsIterativeClock ResolverClock = new DnsIterativeClock();
    internal DnsResolveResult? CacheFallback;
    internal IDnsIterativeEntropy ResolverEntropy =
        new DnsIterativeEntropy();
    internal List<DnsServerState> ServerStates =
        new List<DnsServerState>();
    internal List<DnsIterativeReachabilityState> ReachabilityStates =
        new List<DnsIterativeReachabilityState>();
    internal int QueryCount;
    internal int WorkCount;
    internal int ReferralCount;
    internal bool WorkLimitExceeded;
}

internal static class IterativeResolverOperations
{
    internal static DnsResolveResult ResolveIteratively(
        DnsResolveRequest request,
        DnsRootHints rootHints,
        DnsRecursiveOptions options,
        DnsRecursiveResolver resolver)
    {
        DnsName name = ValidateInputs(request, rootHints, options, resolver);
        DnsResolveResult initial = new DnsResolveResult();
        initial.CanonicalName = RecordSetOperations.CopyName(name, false);
        if (request.CancellationRequested)
        {
            initial.Status = DnsResolveStatus.Cancelled;
            AddDiagnostic(
                initial,
                DnsDiagnosticCode.None,
                "resolution",
                "The iterative resolution was cancelled before it started.");
            return initial;
        }

        DnsRootHints validatedHints = RootHintOperations.ValidateRootHints(
            rootHints);
        RootHintOperations.SeedCache(validatedHints, resolver.Cache);
        DnsIterativeCacheSelection cacheSelection =
            IterativeCacheOperations.ReadSelection(
                request,
                resolver.Cache);
        if (cacheSelection.ReturnImmediately &&
            cacheSelection.Result != null)
        {
            return cacheSelection.Result;
        }

        DnsResolveResult? aggressive =
            IterativeDnssecOperations.ReadAggressiveDenial(
                request,
                options,
                resolver);
        if (aggressive != null)
        {
            return aggressive;
        }

        DnsDelegation selected =
            DelegationSelectionOperations.SelectClosestDelegation(
                name,
                validatedHints,
                resolver.Cache);
        DnsIterativeResolverState state = CreateState(
            request,
            options,
            validatedHints,
            selected,
            initial,
            resolver.Clock);
        state.ResolverEntropy = resolver.Entropy;
        state.ServerStates = resolver.ServerStates;
        state.ReachabilityStates = resolver.ReachabilityStates;
        state.CacheFallback = cacheSelection.Result;
        if (!ConsumeWork(state, 1))
        {
            return ResourceLimitFailure(
                state,
                "work",
                "The iterative resolver work budget was exhausted.");
        }

        if (selected.Depth > options.MaximumDepth)
        {
            return ResourceLimitFailure(
                state,
                "depth",
                "The closest known delegation exceeds the depth budget.");
        }

        while (true)
        {
            if (DeadlineExpired(state))
            {
                SetLifetimeTimeout(state);
                IterativeCacheOperations.StoreFailure(
                    state,
                    resolver.Cache);
                return CompleteResolution(state);
            }

            DnsIterativeQuestionPlan questionPlan = CreateQuestionPlan(
                state,
                state.CurrentName,
                EffectiveValue(
                    state.Request.RecordTypeValue,
                    (int)state.Request.RecordType),
                state.CurrentDelegation.Zone);
            List<DnsIterativeAuthorityCandidate> candidates =
                CreateAuthorityCandidates(state, state.CurrentDelegation);
            if (state.WorkLimitExceeded)
            {
                return ResourceLimitFailure(
                    state,
                    "work",
                    "The iterative resolver work budget was exhausted while selecting an authority.");
            }

            List<DnsIterativeAuthorityCandidate> attempted =
                new List<DnsIterativeAuthorityCandidate>();
            bool resolvedMissingAddresses = false;
            bool advanced = false;
            bool sawLameReferral = false;
            DnsResolveStatus lastFailure = DnsResolveStatus.ServerFailure;
            while (!advanced)
            {
                DnsIterativeAuthorityCandidate? candidate =
                    FindUntriedCandidate(state, candidates, attempted);
                if (candidate == null)
                {
                    if (!resolvedMissingAddresses)
                    {
                        DnsNameServerAddressResult addressResult =
                            NameServerAddressOperations.ResolveMissingAddresses(
                                state.CurrentDelegation,
                                state,
                                resolver,
                                new List<DnsName>());
                        if (addressResult.Stopped)
                        {
                            return CompleteResolution(state);
                        }

                        if (addressResult.Loop && !addressResult.Added)
                        {
                            return LoopFailure(
                                state,
                                "A cyclic name-server address dependency prevented progress.");
                        }

                        resolvedMissingAddresses = true;
                        candidates = CreateAuthorityCandidates(
                            state,
                            state.CurrentDelegation);
                        if (state.WorkLimitExceeded)
                        {
                            return ResourceLimitFailure(
                                state,
                                "work",
                                "The iterative resolver work budget was exhausted while selecting an authority.");
                        }

                        candidate = FindUntriedCandidate(
                            state,
                            candidates,
                            attempted);
                    }

                    if (candidate == null)
                    {
                        state.CurrentDelegation.Lame = sawLameReferral;
                        state.Result.Status = lastFailure;
                        AddDiagnostic(
                            state.Result,
                            DnsDiagnosticCode.None,
                            "authority",
                            "No authority candidate made progress.");
                        IterativeCacheOperations.StoreFailure(
                            state,
                            resolver.Cache);
                        return CompleteResolution(state);
                    }
                }

                attempted.Add(candidate);
                if (IterativeDnssecOperations
                    .NeedsDelegationKeyValidation(state, resolver))
                {
                    DnsIterativeQueryAttempt keyAttempt = QueryCandidate(
                        state,
                        state.CurrentDelegation.Zone,
                        (int)DnsRecordType.Dnskey,
                        EffectiveValue(
                            state.Request.ClassValue,
                            (int)state.Request.Class),
                        candidate,
                        resolver);
                    if (keyAttempt.Stopped)
                    {
                        return CompleteResolution(state);
                    }

                    if (!keyAttempt.Usable || keyAttempt.Response == null)
                    {
                        lastFailure = keyAttempt.FailureStatus;
                        continue;
                    }

                    DnsSecurityResult keySecurity =
                        IterativeDnssecOperations.ValidateDelegationKeys(
                            state,
                            keyAttempt.Response,
                            resolver);
                    AppendDiagnostics(
                        state.Result.Diagnostics,
                        keySecurity.Diagnostics);
                    if (keySecurity.Status != DnsSecurityStatus.Secure)
                    {
                        lastFailure = keySecurity.Status ==
                            DnsSecurityStatus.UnsupportedAlgorithm ?
                            DnsResolveStatus.Unsupported :
                            DnsResolveStatus.ServerFailure;
                        continue;
                    }

                    state.CurrentDelegationSecurityStatus =
                        DnsSecurityStatus.Secure;
                    state.CurrentDelegation.Secure = true;
                    ReferralGlueOperations.StoreAuthority(
                        state.CurrentDelegation,
                        resolver.Cache,
                        state.ResolverClock.ReadClock());
                }

                DnsIterativeQueryAttempt attempt = QueryCandidate(
                    state,
                    questionPlan.Name,
                    questionPlan.RecordTypeValue,
                    EffectiveValue(
                        state.Request.ClassValue,
                        (int)state.Request.Class),
                    candidate,
                    resolver);
                if (attempt.Stopped)
                {
                    return CompleteResolution(state);
                }

                if (!attempt.Usable || attempt.Response == null)
                {
                    lastFailure = attempt.FailureStatus;
                    continue;
                }

                DnsIterativeReferralResult referral = ReadReferral(
                    state,
                    attempt.Response,
                    questionPlan.Name,
                    state.CurrentDelegation);
                if (state.WorkLimitExceeded)
                {
                    return ResourceLimitFailure(
                        state,
                        "work",
                        "The iterative resolver work budget was exhausted while reading a referral.");
                }

                if (referral.Invalid)
                {
                    IterativeServerStateOperations.MarkProtocolFailure(
                        state,
                        candidate);
                    lastFailure = DnsResolveStatus.ProtocolError;
                    AddDiagnostic(
                        state.Result,
                        DnsDiagnosticCode.ResponseMismatch,
                        "response.authorities",
                        referral.Reason);
                    continue;
                }

                if (referral.Lame)
                {
                    IterativeServerStateOperations.MarkLame(
                        state,
                        candidate);
                    sawLameReferral = true;
                    lastFailure = DnsResolveStatus.ServerFailure;
                    AddDiagnostic(
                        state.Result,
                        DnsDiagnosticCode.None,
                        "referral",
                        referral.Reason);
                    continue;
                }

                if (!referral.Found || referral.Delegation == null)
                {
                    if (questionPlan.Minimized &&
                        MinimizationNeedsFallback(attempt.Response))
                    {
                        if (options.AllowMinimizationFallback)
                        {
                            EnableFullQuestionForZone(
                                state,
                                state.CurrentDelegation.Zone);
                            AddDiagnostic(
                                state.Result,
                                DnsDiagnosticCode.None,
                                "minimization",
                                "The authority required a full query name for this zone.");
                            advanced = true;
                            continue;
                        }

                        state.Result.Status =
                            DnsResolveStatus.ServerFailure;
                        AddDiagnostic(
                            state.Result,
                            DnsDiagnosticCode.None,
                            "minimization",
                            "A minimized query was incompatible and fallback is disabled.");
                        return CompleteResolution(state);
                    }

                    if (ResponseAllowsAlternate(attempt.Response))
                    {
                        IterativeServerStateOperations.MarkFailure(
                            state,
                            candidate);
                        lastFailure = MapExtendedErrorStatus(
                            attempt.Response,
                            MapResponseStatus(attempt.Response));
                        continue;
                    }

                    DnsIterativeAnswerResult answer =
                        IterativeAnswerOperations.ProcessAnswer(
                            state,
                            attempt.Response,
                            resolver);
                    if (answer.Invalid)
                    {
                        IterativeServerStateOperations.MarkProtocolFailure(
                            state,
                            candidate);
                        lastFailure = DnsResolveStatus.ProtocolError;
                        AddDiagnostic(
                            state.Result,
                            DnsDiagnosticCode.ResponseMismatch,
                            "response.answer",
                            answer.Reason);
                        continue;
                    }

                    if (answer.Lame)
                    {
                        IterativeServerStateOperations.MarkLame(
                            state,
                            candidate);
                        sawLameReferral = true;
                        lastFailure = DnsResolveStatus.ServerFailure;
                        AddDiagnostic(
                            state.Result,
                            DnsDiagnosticCode.None,
                            "answer",
                            answer.Reason);
                        continue;
                    }

                    if (answer.Terminal)
                    {
                        return CompleteResolution(state);
                    }

                    if (answer.Continue)
                    {
                        advanced = true;
                        continue;
                    }

                    lastFailure = DnsResolveStatus.ProtocolError;
                    IterativeServerStateOperations.MarkProtocolFailure(
                        state,
                        candidate);
                    continue;
                }

                if (state.ReferralCount >= options.MaximumReferralDepth ||
                    referral.Delegation.Depth > options.MaximumDepth)
                {
                    return ResourceLimitFailure(
                        state,
                        "referrals",
                        "The iterative referral or depth budget was exhausted.");
                }

                DnsSecurityResult delegationSecurity =
                    IterativeDnssecOperations.ValidateReferral(
                        state,
                        attempt.Response,
                        referral.Delegation,
                        resolver);
                AppendDiagnostics(
                    state.Result.Diagnostics,
                    delegationSecurity.Diagnostics);
                referral.Delegation.Secure = delegationSecurity.Status ==
                    DnsSecurityStatus.Secure;
                if (state.Options.ValidateDnssec &&
                    state.CurrentDelegationSecurityStatus ==
                        DnsSecurityStatus.Secure &&
                    (delegationSecurity.Status ==
                        DnsSecurityStatus.Bogus ||
                     delegationSecurity.Status ==
                        DnsSecurityStatus.UnsupportedAlgorithm))
                {
                    lastFailure = delegationSecurity.Status ==
                        DnsSecurityStatus.UnsupportedAlgorithm ?
                        DnsResolveStatus.Unsupported :
                        DnsResolveStatus.ServerFailure;
                    continue;
                }

                DnsReferralGlueResult glue =
                    ReferralGlueOperations.AcceptGlue(
                        referral.Delegation,
                        attempt.Response,
                        resolver.Cache,
                        state);
                if (state.WorkLimitExceeded)
                {
                    return ResourceLimitFailure(
                        state,
                        "work",
                        "The iterative resolver work budget was exhausted while reading referral glue.");
                }

                if (glue.Invalid)
                {
                    IterativeServerStateOperations.MarkProtocolFailure(
                        state,
                        candidate);
                    lastFailure = DnsResolveStatus.ProtocolError;
                    AddDiagnostic(
                        state.Result,
                        DnsDiagnosticCode.ResponseMismatch,
                        "response.additionals",
                        glue.Reason);
                    continue;
                }

                state.ReferralCount = state.ReferralCount + 1;
                state.Result.Delegations.Add(referral.Delegation);
                state.CurrentDelegation = referral.Delegation;
                state.CurrentDelegationSecurityStatus =
                    delegationSecurity.Status;
                advanced = true;
            }
        }
    }

    static DnsName ValidateInputs(
        DnsResolveRequest request,
        DnsRootHints rootHints,
        DnsRecursiveOptions options,
        DnsRecursiveResolver resolver)
    {
        if (request == null || request.Name == null ||
            request.Options == null || rootHints == null ||
            options == null || resolver == null ||
            resolver.Cache == null || resolver.QueryClient == null ||
            resolver.Entropy == null || resolver.Clock == null ||
            options.Security == null)
        {
            throw new DnsResolutionException(
                "A recursive request, root hints, options, cache, and query client are required.");
        }

        RawDnsNameResult name = RawDnsName.ValidateName(request.Name);
        if (!name.Success || name.Name == null || !name.Name.IsAbsolute)
        {
            throw new DnsResolutionException(
                "Iterative resolution requires an absolute DNS name.");
        }

        int typeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        int classValue = EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        if (typeValue < 1 || typeValue > 65535 ||
            classValue < 1 || classValue > 65535)
        {
            throw new DnsResolutionException(
                "Iterative resolution requires valid DNS type and class values.");
        }

        if (options.MaximumQueries < 0 || options.MaximumWork < 0 ||
            options.MaximumDepth < 0 || options.MaximumAliasDepth < 0 ||
            options.MaximumReferralDepth < 0 ||
            options.TotalTimeoutMilliseconds < 1 ||
            options.EdnsUdpPayloadSize < DnsProtocolLimits.ClassicUdpPayloadSize ||
            options.EdnsUdpPayloadSize > DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            throw new DnsResolutionException(
                "Recursive resolver limits must be positive and protocol-bounded.");
        }

        return name.Name;
    }

    static DnsIterativeResolverState CreateState(
        DnsResolveRequest request,
        DnsRecursiveOptions options,
        DnsRootHints rootHints,
        DnsDelegation selected,
        DnsResolveResult result,
        IDnsIterativeClock clock)
    {
        DnsIterativeResolverState state = new DnsIterativeResolverState();
        state.Request = request;
        state.CurrentName = RecordSetOperations.CopyName(
            request.Name,
            false);
        state.VisitedNames.Add(RecordSetOperations.CopyName(
            request.Name,
            true));
        state.Options = options;
        state.RootHints = rootHints;
        state.CurrentDelegation = selected;
        state.CurrentDelegationSecurityStatus = selected.Secure ?
            DnsSecurityStatus.Secure : DnsSecurityStatus.Insecure;
        state.Result = result;
        state.ResolverClock = clock;
        state.Result.Delegations.Add(selected);
        DnsClockValue startedAt = clock.ReadClock();
        state.Deadline = TransportTime.CreateDeadlineAt(
            startedAt,
            options.TotalTimeoutMilliseconds,
            false,
            new DnsClockValue());
        return state;
    }

    internal static List<DnsIterativeAuthorityCandidate>
        CreateAuthorityCandidates(
        DnsIterativeResolverState state,
        DnsDelegation delegation)
    {
        List<DnsIterativeAuthorityCandidate> result =
            new List<DnsIterativeAuthorityCandidate>();
        int nameIndex = 0;
        while (nameIndex < delegation.NameServers.Count)
        {
            DnsName nameServer = delegation.NameServers[nameIndex];
            int glueIndex = 0;
            while (glueIndex < delegation.GlueAddresses.Count)
            {
                DnsAddressResult glue = delegation.GlueAddresses[glueIndex];
                if (glue != null && glue.CanonicalName != null &&
                    RecordSetOperations.NamesEqual(
                        glue.CanonicalName,
                        nameServer))
                {
                    AddGlueCandidates(
                        state,
                        result,
                        delegation.Zone,
                        nameServer,
                        glue);
                }

                glueIndex = glueIndex + 1;
            }

            nameIndex = nameIndex + 1;
        }

        return result;
    }

    static void AddGlueCandidates(
        DnsIterativeResolverState state,
        List<DnsIterativeAuthorityCandidate> destination,
        DnsName zone,
        DnsName nameServer,
        DnsAddressResult glue)
    {
        if (glue.Addresses == null || glue.AddressFamilies == null ||
            glue.Addresses.Count != glue.AddressFamilies.Count)
        {
            return;
        }

        int index = 0;
        while (index < glue.Addresses.Count)
        {
            if (!ConsumeWork(state, 1))
            {
                return;
            }

            byte[] address = glue.Addresses[index];
            int family = glue.AddressFamilies[index];
            if (address != null &&
                ((family == 4 && address.Length == 4) ||
                    (family == 6 && address.Length == 16)) &&
                !ContainsEndpoint(destination, address, family))
            {
                DnsIterativeAuthorityCandidate candidate =
                    new DnsIterativeAuthorityCandidate();
                candidate.Zone = RecordSetOperations.CopyName(zone, true);
                candidate.NameServer = RecordSetOperations.CopyName(
                    nameServer,
                    false);
                candidate.Endpoint.HasAddress = true;
                candidate.Endpoint.Address = RecordSetOperations.CopyBytes(
                    address);
                candidate.Endpoint.AddressFamily = family;
                candidate.Endpoint.Port = DnsPorts.Dns;
                destination.Add(candidate);
            }

            index = index + 1;
        }
    }

    static bool ContainsEndpoint(
        List<DnsIterativeAuthorityCandidate> candidates,
        byte[] address,
        int family)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (candidates[index].Endpoint.AddressFamily == family &&
                BytesEqual(candidates[index].Endpoint.Address, address))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsQueryRequest CreateIterativeQuery(
        DnsIterativeResolverState state,
        DnsName questionName,
        int typeValue,
        int classValue,
        DnsIterativeAuthorityCandidate candidate,
        int remainingMilliseconds,
        int maximumAttempts)
    {
        DnsQueryRequest result = new DnsQueryRequest();
        bool useEdns = IterativeServerStateOperations.ShouldUseEdns(
            state,
            candidate.Endpoint);
        int udpPayloadSize =
            IterativeServerStateOperations.SelectUdpPayloadSize(
                state,
                candidate.Endpoint);
        result.Question.Name = CreateQueryName(
            questionName,
            state.Options.RandomizeQueryCase,
            state);
        result.Question.RecordType = (DnsRecordType)typeValue;
        result.Question.RecordTypeValue = typeValue;
        result.Question.Class = (DnsClass)classValue;
        result.Question.ClassValue = classValue;
        result.Servers.Add(CopyEndpoint(candidate.Endpoint));
        result.TransportOptions.Kind = DnsTransportKind.Udp;
        result.TransportOptions.AttemptTimeoutMilliseconds =
            remainingMilliseconds;
        result.TransportOptions.TotalTimeoutMilliseconds =
            remainingMilliseconds;
        result.TransportOptions.MaximumAttempts = maximumAttempts;
        result.RecursionDesired = false;
        result.UseEdns = useEdns;
        result.RequestDnssec = state.Options.ValidateDnssec;
        result.CheckingDisabled = false;
        result.AllowTcpFallback = state.Options.AllowTcpFallback;
        result.AllowEdnsDowngrade =
            state.Options.AllowEdnsDowngrade && useEdns;
        result.CancellationRequested = state.Request.CancellationRequested;
        result.Message = CreateIterativeMessage(
            result,
            state,
            udpPayloadSize);
        return result;
    }

    static DnsName CreateQueryName(
        DnsName source,
        bool randomizeCase,
        DnsIterativeResolverState state)
    {
        DnsName result = RecordSetOperations.CopyName(source, false);
        if (!randomizeCase)
        {
            return result;
        }

        int labelIndex = 0;
        while (labelIndex < result.Labels.Count)
        {
            byte[] label = result.Labels[labelIndex];
            int byteIndex = 0;
            while (byteIndex < label.Length)
            {
                byte value = label[byteIndex];
                if ((value >= 65 && value <= 90) ||
                    (value >= 97 && value <= 122))
                {
                    byte lower = value;
                    if (lower >= 65 && lower <= 90)
                    {
                        lower = (byte)(lower + 32);
                    }

                    if (state.ResolverEntropy.NextCaseBit() == 1)
                    {
                        label[byteIndex] = (byte)(lower - 32);
                    }
                    else
                    {
                        label[byteIndex] = lower;
                    }
                }

                byteIndex = byteIndex + 1;
            }

            labelIndex = labelIndex + 1;
        }

        return result;
    }

    static DnsMessage CreateIterativeMessage(
        DnsQueryRequest request,
        DnsIterativeResolverState state,
        int udpPayloadSize)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = state.ResolverEntropy.NextIdentifier();
        message.Header.Flags = DnsHeaderFlag.None;
        message.Header.FlagsValue = 0;
        message.Questions.Add(CopyQuestion(request.Question));
        if (request.UseEdns)
        {
            return EdnsOperations.PrepareRequest(
                message,
                udpPayloadSize,
                request.RequestDnssec);
        }

        return message;
    }

    static DnsQuestion CopyQuestion(DnsQuestion source)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        return result;
    }

    internal static DnsIterativeQueryAttempt QueryCandidate(
        DnsIterativeResolverState state,
        DnsName questionName,
        int typeValue,
        int classValue,
        DnsIterativeAuthorityCandidate candidate,
        DnsRecursiveResolver resolver)
    {
        DnsIterativeQueryAttempt result = new DnsIterativeQueryAttempt();
        if (state.QueryCount >= state.Options.MaximumQueries)
        {
            ResourceLimitFailure(
                state,
                "queries",
                "The iterative resolver query budget was exhausted.");
            result.Stopped = true;
            return result;
        }

        if (!ConsumeWork(state, 1))
        {
            ResourceLimitFailure(
                state,
                "work",
                "The iterative resolver work budget was exhausted before querying.");
            result.Stopped = true;
            return result;
        }

        DnsClockValue currentClock = state.ResolverClock.ReadClock();
        int remaining = TransportTime.RemainingMillisecondsAt(
            state.Deadline,
            currentClock.MonotonicMilliseconds);
        if (remaining == 0)
        {
            SetLifetimeTimeout(state);
            result.Stopped = true;
            return result;
        }

        int maximumAttempts = state.Options.MaximumQueries -
            state.QueryCount;
        if (maximumAttempts > 2)
        {
            maximumAttempts = 2;
        }

        DnsQueryRequest queryRequest = CreateIterativeQuery(
            state,
            questionName,
            typeValue,
            classValue,
            candidate,
            remaining,
            maximumAttempts);
        DnsQueryResult queryResult = resolver.QueryClient.Query(queryRequest);
        if (queryResult == null)
        {
            state.QueryCount = state.QueryCount + 1;
            IterativeServerStateOperations.MarkProtocolFailure(
                state,
                candidate);
            result.FailureStatus = DnsResolveStatus.ProtocolError;
            AddDiagnostic(
                state.Result,
                DnsDiagnosticCode.ResponseMismatch,
                "queryResult",
                "The iterative query client returned no result.");
            return result;
        }

        int usedQueries = queryResult.Attempts.Count;
        if (usedQueries < 1)
        {
            usedQueries = 1;
        }

        state.QueryCount = state.QueryCount + usedQueries;
        IterativeServerStateOperations.ObserveResult(
            state,
            candidate,
            queryResult);

        state.Result.Queries.Add(queryResult);
        AppendDiagnostics(state.Result.Diagnostics, queryResult.Diagnostics);
        if (DeadlineExpired(state))
        {
            SetLifetimeTimeout(state);
            IterativeCacheOperations.StoreFailure(
                state,
                resolver.Cache);
            result.FailureStatus = DnsResolveStatus.TimedOut;
            result.Stopped = true;
            return result;
        }

        if (!queryResult.Success || queryResult.Response == null)
        {
            result.FailureStatus = MapTransportStatus(queryResult.Status);
            if (result.FailureStatus == DnsResolveStatus.Cancelled)
            {
                state.Result.Status = result.FailureStatus;
                result.Stopped = true;
            }

            return result;
        }

        state.Result.Response = queryResult.Response;
        string? responseFailure = ValidateResponse(
            queryRequest,
            queryResult,
            candidate);
        if (responseFailure != null)
        {
            IterativeServerStateOperations.MarkProtocolFailure(
                state,
                candidate);
            result.FailureStatus = DnsResolveStatus.ProtocolError;
            AddDiagnostic(
                state.Result,
                DnsDiagnosticCode.ResponseMismatch,
                "response",
                responseFailure);
            return result;
        }

        ApplyExtendedErrorMetadata(state.Result, queryResult.Response);
        result.Usable = true;
        result.Response = queryResult.Response;
        return result;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint source)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.Host = source.Host;
        result.HasAddress = source.HasAddress;
        result.Address = RecordSetOperations.CopyBytes(source.Address);
        result.AddressFamily = source.AddressFamily;
        result.Port = source.Port;
        return result;
    }

    internal static string? ValidateResponse(
        DnsQueryRequest request,
        DnsQueryResult queryResult,
        DnsIterativeAuthorityCandidate candidate)
    {
        DnsMessage? response = queryResult.Response;
        if (response == null)
        {
            return "The iterative response is missing.";
        }

        if (response.Header == null || response.Questions == null ||
            response.Answers == null || response.Authorities == null ||
            response.Additionals == null)
        {
            return "The iterative response is incomplete.";
        }

        int flags = EffectiveValue(
            response.Header.FlagsValue,
            (int)response.Header.Flags);
        if ((flags & (int)DnsHeaderFlag.Response) == 0)
        {
            return "The iterative result is not marked as a response.";
        }

        int opcode = EffectiveValue(
            response.Header.OpcodeValue,
            (int)response.Header.Opcode);
        if (opcode != (int)DnsOpcode.Query)
        {
            return "The iterative response has the wrong operation code.";
        }

        if (request.Message == null ||
            response.Header.Identifier != request.Message.Header.Identifier)
        {
            return "The iterative response identifier does not match its request.";
        }

        if (request.Message.Questions.Count != 1 ||
            !QuestionMatchesExactly(
                request.Message.Questions[0],
                response.Questions))
        {
            return "The iterative response question or randomized case does not match its request.";
        }

        if (!ResponseSourceMatches(queryResult, candidate.Endpoint))
        {
            return "The iterative response source does not match its authority endpoint.";
        }

        return null;
    }

    static bool QuestionMatchesExactly(
        DnsQuestion expected,
        List<DnsQuestion> actual)
    {
        if (actual.Count != 1 || actual[0] == null)
        {
            return false;
        }

        DnsQuestion candidate = actual[0];
        if (!NamesEqualExactly(expected.Name, candidate.Name))
        {
            return false;
        }

        int expectedType = EffectiveValue(
            expected.RecordTypeValue,
            (int)expected.RecordType);
        int actualType = EffectiveValue(
            candidate.RecordTypeValue,
            (int)candidate.RecordType);
        int expectedClass = EffectiveValue(
            expected.ClassValue,
            (int)expected.Class);
        int actualClass = EffectiveValue(
            candidate.ClassValue,
            (int)candidate.Class);
        return expectedType == actualType && expectedClass == actualClass;
    }

    static bool NamesEqualExactly(DnsName left, DnsName right)
    {
        if (left == null || right == null ||
            left.IsAbsolute != right.IsAbsolute ||
            left.Labels.Count != right.Labels.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Labels.Count)
        {
            if (!BytesEqual(left.Labels[index], right.Labels[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ResponseSourceMatches(
        DnsQueryResult result,
        DnsEndpoint expected)
    {
        if (result.Attempts == null || result.Attempts.Count == 0)
        {
            return false;
        }

        DnsTransportResult attempt =
            result.Attempts[result.Attempts.Count - 1];
        if (attempt == null || attempt.Status != DnsTransportStatus.Success)
        {
            return false;
        }

        return EndpointsMatch(expected, attempt.Endpoint);
    }

    static bool EndpointsMatch(DnsEndpoint expected, DnsEndpoint actual)
    {
        if (expected == null || actual == null ||
            expected.Port != actual.Port ||
            expected.HasAddress != actual.HasAddress)
        {
            return false;
        }

        if (expected.HasAddress)
        {
            return expected.AddressFamily == actual.AddressFamily &&
                BytesEqual(expected.Address, actual.Address);
        }

        if (expected.Host == null || actual.Host == null)
        {
            return expected.Host == null && actual.Host == null;
        }

        return string.Equals(
            expected.Host,
            actual.Host,
            System.StringComparison.OrdinalIgnoreCase);
    }

    internal static DnsIterativeReferralResult ReadReferral(
        DnsIterativeResolverState state,
        DnsMessage response,
        DnsName queryName,
        DnsDelegation currentDelegation)
    {
        DnsIterativeReferralResult result = new DnsIterativeReferralResult();
        int flags = EffectiveValue(
            response.Header.FlagsValue,
            (int)response.Header.Flags);
        int responseCode = EffectiveValue(
            response.Header.ResponseCodeValue,
            (int)response.Header.ResponseCode);
        if (responseCode != (int)DnsResponseCode.NoError ||
            (flags & (int)DnsHeaderFlag.AuthoritativeAnswer) != 0 ||
            response.Answers.Count != 0)
        {
            return result;
        }

        DnsName? referralZone = null;
        List<DnsResourceRecord> nameServerRecords =
            new List<DnsResourceRecord>();
        int index = 0;
        while (index < response.Authorities.Count)
        {
            if (!ConsumeWork(state, 1))
            {
                result.Invalid = true;
                result.Reason =
                    "The iterative resolver work budget was exhausted while reading a referral.";
                return result;
            }

            DnsResourceRecord record = response.Authorities[index];
            if (record == null)
            {
                result.Invalid = true;
                result.Reason =
                    "A referral authority section contains a null record.";
                return result;
            }

            int typeValue = EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue == 2)
            {
                DnsName? target = ReadNameServerTarget(record);
                RawDnsNameResult owner = RawDnsName.ValidateName(record.Name);
                if (target == null || !owner.Success ||
                    owner.Name == null || !owner.Name.IsAbsolute)
                {
                    result.Invalid = true;
                    result.Reason =
                        "A referral NS record has an invalid owner or target.";
                    return result;
                }

                if (!ReferralOwnerIsValid(
                        owner.Name,
                        queryName,
                        currentDelegation.Zone) ||
                    (referralZone != null &&
                        !RecordSetOperations.NamesEqual(
                            referralZone,
                            owner.Name)))
                {
                    result.Lame = true;
                    result.Reason =
                        "An authority returned a referral that does not advance the delegation.";
                    return result;
                }

                if (referralZone == null)
                {
                    referralZone = RecordSetOperations.CopyName(
                        record.Name,
                        false);
                }

                nameServerRecords.Add(record);
            }

            index = index + 1;
        }

        if (referralZone == null || nameServerRecords.Count == 0)
        {
            result.Lame = true;
            result.Reason =
                "An authority returned no answer and no usable referral.";
            return result;
        }

        DnsDelegation delegation = CreateReferralDelegation(
            referralZone,
            nameServerRecords);
        if (delegation.NameServers.Count == 0)
        {
            result.Invalid = true;
            result.Reason = "A referral contains no distinct name servers.";
            return result;
        }

        result.Found = true;
        result.Delegation = delegation;
        return result;
    }

    static DnsName? ReadNameServerTarget(DnsResourceRecord record)
    {
        if (record == null || record.Data == null ||
            record.Data.Name == null || record.Data.Name.Name == null ||
            EffectiveValue(record.ClassValue, (int)record.Class) != 1)
        {
            return null;
        }

        RawDnsNameResult target = RawDnsName.ValidateName(
            record.Data.Name.Name);
        if (!target.Success || target.Name == null ||
            !target.Name.IsAbsolute || target.Name.Labels.Count == 0)
        {
            return null;
        }

        return target.Name;
    }

    static bool ReferralOwnerIsValid(
        DnsName owner,
        DnsName queryName,
        DnsName currentZone)
    {
        RawDnsNameResult validated = RawDnsName.ValidateName(owner);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            return false;
        }

        DnsNameOperationResult querySuffix = DnsNameOperations.HasNameSuffix(
            queryName,
            validated.Name);
        DnsNameOperationResult currentSuffix =
            DnsNameOperations.HasNameSuffix(
                validated.Name,
                currentZone);
        return querySuffix.Success && querySuffix.BooleanValue &&
            currentSuffix.Success && currentSuffix.BooleanValue &&
            validated.Name.Labels.Count > currentZone.Labels.Count;
    }

    static DnsDelegation CreateReferralDelegation(
        DnsName zone,
        List<DnsResourceRecord> records)
    {
        DnsDelegation result = new DnsDelegation();
        result.Zone = RecordSetOperations.CopyName(zone, false);
        result.Depth = zone.Labels.Count;
        result.InBailiwick = false;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = RecordSetOperations.CopyName(zone, false);
        set.RecordType = DnsRecordType.Ns;
        set.RecordTypeValue = 2;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord source = records[index];
            DnsName? target = ReadNameServerTarget(source);
            if (target != null && !ContainsName(result.NameServers, target))
            {
                result.NameServers.Add(RecordSetOperations.CopyName(
                    target,
                    false));
            }

            DnsResourceRecord copy = RecordSetOperations.CopyRecord(
                source,
                set.Name);
            copy.RecordType = DnsRecordType.Ns;
            copy.RecordTypeValue = 2;
            copy.Class = DnsClass.Internet;
            copy.ClassValue = 1;
            set.Records.Add(copy);
            if (index == 0 || source.TimeToLive < set.TimeToLive)
            {
                set.TimeToLive = source.TimeToLive;
            }

            index = index + 1;
        }

        result.AuthorityRecordSets.Add(set);
        return result;
    }

    internal static DnsIterativeQuestionPlan CreateQuestionPlan(
        DnsIterativeResolverState state,
        DnsName targetName,
        int recordTypeValue,
        DnsName currentZone)
    {
        DnsIterativeQuestionPlan result = new DnsIterativeQuestionPlan();
        result.Name = RecordSetOperations.CopyName(targetName, false);
        result.RecordTypeValue = recordTypeValue;
        if (!state.Options.MinimizeQueryName ||
            ContainsName(state.FullQueryZones, currentZone) ||
            targetName.Labels.Count <= currentZone.Labels.Count + 1)
        {
            return result;
        }

        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            targetName,
            currentZone);
        if (!suffix.Success || !suffix.BooleanValue)
        {
            return result;
        }

        DnsName minimized = new DnsName();
        minimized.IsAbsolute = targetName.IsAbsolute;
        int firstLabel = targetName.Labels.Count -
            currentZone.Labels.Count - 1;
        int index = firstLabel;
        while (index < targetName.Labels.Count)
        {
            minimized.Labels.Add(RecordSetOperations.CopyBytes(
                targetName.Labels[index]));
            index = index + 1;
        }

        result.Name = minimized;
        result.RecordTypeValue = 2;
        result.Minimized = true;
        return result;
    }

    internal static void EnableFullQuestionForZone(
        DnsIterativeResolverState state,
        DnsName zone)
    {
        if (!ContainsName(state.FullQueryZones, zone))
        {
            state.FullQueryZones.Add(RecordSetOperations.CopyName(
                zone,
                true));
        }
    }

    internal static bool MinimizationNeedsFallback(DnsMessage response)
    {
        int responseCode = EffectiveValue(
            response.Header.ResponseCodeValue,
            (int)response.Header.ResponseCode);
        return responseCode != (int)DnsResponseCode.NameError;
    }

    internal static DnsIterativeAuthorityCandidate? FindUntriedCandidate(
        DnsIterativeResolverState state,
        List<DnsIterativeAuthorityCandidate> candidates,
        List<DnsIterativeAuthorityCandidate> attempted)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (!CandidateWasTried(candidates[index], attempted) &&
                IterativeServerStateOperations.IsPreferred(
                    state,
                    candidates[index]))
            {
                return candidates[index];
            }

            index = index + 1;
        }

        index = 0;
        while (index < candidates.Count)
        {
            if (!CandidateWasTried(candidates[index], attempted))
            {
                return candidates[index];
            }

            index = index + 1;
        }

        return null;
    }

    static bool CandidateWasTried(
        DnsIterativeAuthorityCandidate candidate,
        List<DnsIterativeAuthorityCandidate> attempted)
    {
        int index = 0;
        while (index < attempted.Count)
        {
            if (candidate.Endpoint.AddressFamily ==
                    attempted[index].Endpoint.AddressFamily &&
                BytesEqual(
                    candidate.Endpoint.Address,
                    attempted[index].Endpoint.Address))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ResponseAllowsAlternate(DnsMessage response)
    {
        int responseCode = EffectiveValue(
            response.Header.ResponseCodeValue,
            (int)response.Header.ResponseCode);
        return responseCode == (int)DnsResponseCode.ServerFailure ||
            responseCode == (int)DnsResponseCode.Refused ||
            responseCode == (int)DnsResponseCode.NotImplemented ||
            responseCode == (int)DnsResponseCode.NotAuthoritative;
    }

    internal static DnsResolveStatus MapResponseStatus(DnsMessage response)
    {
        int responseCode = EffectiveValue(
            response.Header.ResponseCodeValue,
            (int)response.Header.ResponseCode);
        if (responseCode == (int)DnsResponseCode.Refused)
        {
            return DnsResolveStatus.Refused;
        }

        return DnsResolveStatus.ServerFailure;
    }

    internal static DnsResolveStatus MapExtendedErrorStatus(
        DnsMessage response,
        DnsResolveStatus fallback)
    {
        DnsEdnsExtendedError? extendedError = FindExtendedError(response);
        if (extendedError == null)
        {
            return fallback;
        }

        int code = EffectiveValue(
            extendedError.CodeValue,
            (int)extendedError.Code);
        if (code == (int)DnsExtendedErrorCode.Blocked ||
            code == (int)DnsExtendedErrorCode.Censored ||
            code == (int)DnsExtendedErrorCode.Filtered ||
            code == (int)DnsExtendedErrorCode.Prohibited ||
            code == (int)DnsExtendedErrorCode.UnableToConformToPolicy ||
            code == (int)DnsExtendedErrorCode.BlockedByUpstream)
        {
            return DnsResolveStatus.Refused;
        }

        if (code == (int)DnsExtendedErrorCode.UnsupportedDnskeyAlgorithm ||
            code == (int)DnsExtendedErrorCode.UnsupportedDsDigestType ||
            code == (int)DnsExtendedErrorCode.NotSupported ||
            code == (int)DnsExtendedErrorCode.UnsupportedNsec3Iterations ||
            code == (int)DnsExtendedErrorCode.InvalidQueryType)
        {
            return DnsResolveStatus.Unsupported;
        }

        if ((code >= (int)DnsExtendedErrorCode.ForgedAnswer &&
                code <= (int)DnsExtendedErrorCode.NsecMissing) ||
            code == (int)DnsExtendedErrorCode.InvalidData ||
            code == (int)DnsExtendedErrorCode.SignatureExpiredBeforeValid)
        {
            return DnsResolveStatus.ProtocolError;
        }

        return fallback;
    }

    static void ApplyExtendedErrorMetadata(
        DnsResolveResult result,
        DnsMessage response)
    {
        DnsEdnsExtendedError? extendedError = FindExtendedError(response);
        if (extendedError == null)
        {
            return;
        }

        int code = EffectiveValue(
            extendedError.CodeValue,
            (int)extendedError.Code);
        if (code == (int)DnsExtendedErrorCode.StaleAnswer ||
            code == (int)DnsExtendedErrorCode.StaleNameErrorAnswer)
        {
            result.Stale = true;
        }

        string reason = "The authority reported Extended DNS Error " +
            code + ".";
        if (extendedError.ExtraText != null &&
            extendedError.ExtraText.Length > 0)
        {
            reason = reason + " " + extendedError.ExtraText;
        }

        AddDiagnostic(
            result,
            DnsDiagnosticCode.None,
            "response.ede",
            reason);
    }

    static DnsEdnsExtendedError? FindExtendedError(DnsMessage response)
    {
        int recordIndex = 0;
        while (recordIndex < response.Additionals.Count)
        {
            DnsResourceRecord record = response.Additionals[recordIndex];
            if (record.Data != null && record.Data.Edns != null)
            {
                int optionIndex = 0;
                while (optionIndex < record.Data.Edns.Options.Count)
                {
                    DnsEdnsOption option =
                        record.Data.Edns.Options[optionIndex];
                    int optionCode = EffectiveValue(
                        option.CodeValue,
                        (int)option.Code);
                    if (optionCode ==
                            (int)DnsEdnsOptionCode.ExtendedDnsError &&
                        option.ExtendedError != null)
                    {
                        return option.ExtendedError;
                    }

                    optionIndex = optionIndex + 1;
                }
            }

            recordIndex = recordIndex + 1;
        }

        return null;
    }

    internal static bool ConsumeWork(
        DnsIterativeResolverState state,
        int amount)
    {
        if (amount < 0 ||
            state.WorkCount > state.Options.MaximumWork - amount)
        {
            state.WorkLimitExceeded = true;
            return false;
        }

        state.WorkCount = state.WorkCount + amount;
        return true;
    }

    static DnsResolveResult UnsupportedResult(
        DnsIterativeResolverState state,
        DnsMessage? response,
        string reason)
    {
        state.Result.Status = DnsResolveStatus.Unsupported;
        state.Result.Response = response;
        AddDiagnostic(
            state.Result,
            DnsDiagnosticCode.None,
            "iterative",
            reason);
        return state.Result;
    }

    internal static DnsResolveResult ResourceLimitFailure(
        DnsIterativeResolverState state,
        string field,
        string reason)
    {
        state.Result.Status = DnsResolveStatus.DepthLimitExceeded;
        AddDiagnostic(
            state.Result,
            DnsDiagnosticCode.ResourceLimitExceeded,
            field,
            reason);
        return state.Result;
    }

    internal static DnsResolveResult LoopFailure(
        DnsIterativeResolverState state,
        string reason)
    {
        state.Result.Status = DnsResolveStatus.Loop;
        AddDiagnostic(
            state.Result,
            DnsDiagnosticCode.ResourceLimitExceeded,
            "dependency",
            reason);
        return state.Result;
    }

    static DnsResolveResult ProtocolFailure(
        DnsIterativeResolverState state,
        DnsMessage? response,
        string field,
        string reason)
    {
        state.Result.Status = DnsResolveStatus.ProtocolError;
        state.Result.Response = response;
        AddDiagnostic(
            state.Result,
            DnsDiagnosticCode.ResponseMismatch,
            field,
            reason);
        return state.Result;
    }

    internal static void AddDiagnostic(
        DnsResolveResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static bool DeadlineExpired(DnsIterativeResolverState state)
    {
        DnsClockValue now = state.ResolverClock.ReadClock();
        return now.MonotonicMilliseconds >=
            state.Deadline.MonotonicMilliseconds;
    }

    static void SetLifetimeTimeout(DnsIterativeResolverState state)
    {
        state.Result.Status = DnsResolveStatus.TimedOut;
        if (!HasDiagnosticField(state.Result, "lifetime"))
        {
            AddDiagnostic(
                state.Result,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "lifetime",
                "The iterative resolution exhausted its total lifetime.");
        }
    }

    static DnsResolveResult CompleteResolution(
        DnsIterativeResolverState state)
    {
        if (CanUseCacheFallback(state.Result.Status) &&
            state.CacheFallback != null)
        {
            DnsResolveResult fallback = state.CacheFallback;
            fallback.Queries = state.Result.Queries;
            fallback.Delegations = state.Result.Delegations;
            AppendDiagnostics(
                fallback.Diagnostics,
                state.Result.Diagnostics);
            if (fallback.Stale)
            {
                AddDiagnostic(
                    fallback,
                    DnsDiagnosticCode.None,
                    "cache.stale",
                    "A stale cached answer was used after iterative refresh failed.");
            }
            else
            {
                AddDiagnostic(
                    fallback,
                    DnsDiagnosticCode.None,
                    "cache.prefetch",
                    "A fresh cached answer was retained after prefetch failed.");
            }

            state.CacheFallback = null;
            return fallback;
        }

        if (state.Result.Status != DnsResolveStatus.Success &&
            state.Result.Status != DnsResolveStatus.NameError &&
            state.Result.Status != DnsResolveStatus.NoData &&
            !HasDiagnosticField(state.Result, "resolution"))
        {
            AddDiagnostic(
                state.Result,
                DnsDiagnosticCode.None,
                "resolution",
                FinalFailureReason(state.Result.Status));
        }

        return state.Result;
    }

    static bool CanUseCacheFallback(DnsResolveStatus status)
    {
        return status == DnsResolveStatus.TimedOut ||
            status == DnsResolveStatus.ServerFailure ||
            status == DnsResolveStatus.ProtocolError ||
            status == DnsResolveStatus.Refused ||
            status == DnsResolveStatus.Unsupported;
    }

    static bool HasDiagnosticField(
        DnsResolveResult result,
        string field)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (result.Diagnostics[index].Field == field)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string FinalFailureReason(DnsResolveStatus status)
    {
        if (status == DnsResolveStatus.TimedOut)
        {
            return "The iterative resolution timed out.";
        }

        if (status == DnsResolveStatus.Cancelled)
        {
            return "The iterative resolution was cancelled.";
        }

        if (status == DnsResolveStatus.Refused)
        {
            return "Every usable authority refused the iterative request.";
        }

        if (status == DnsResolveStatus.Unsupported)
        {
            return "Every usable authority reported an unsupported request.";
        }

        if (status == DnsResolveStatus.ProtocolError)
        {
            return "Every usable authority returned invalid protocol data.";
        }

        return "No authority completed the iterative resolution.";
    }

    static void AppendDiagnostics(
        List<DnsDiagnostic> destination,
        List<DnsDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static DnsResolveStatus MapTransportStatus(DnsTransportStatus status)
    {
        if (status == DnsTransportStatus.TimedOut)
        {
            return DnsResolveStatus.TimedOut;
        }

        if (status == DnsTransportStatus.Cancelled)
        {
            return DnsResolveStatus.Cancelled;
        }

        if (status == DnsTransportStatus.ProtocolError)
        {
            return DnsResolveStatus.ProtocolError;
        }

        if (status == DnsTransportStatus.Unsupported)
        {
            return DnsResolveStatus.Unsupported;
        }

        return DnsResolveStatus.ServerFailure;
    }

    static bool ContainsName(List<DnsName> names, DnsName candidate)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (RecordSetOperations.NamesEqual(names[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int EffectiveValue(int rawValue, int recognizedValue)
    {
        if (rawValue == 0 && recognizedValue != 0)
        {
            return recognizedValue;
        }

        return rawValue;
    }
}
