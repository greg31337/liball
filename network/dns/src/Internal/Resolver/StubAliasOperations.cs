using System.Collections.Generic;

namespace dns;

internal class DnsStubAliasState
{
    internal DnsName CurrentName = new DnsName();
    internal List<DnsName> VisitedNames = new List<DnsName>();
    internal List<DnsAliasStep> Steps = new List<DnsAliasStep>();
    internal List<DnsQueryResult> Queries = new List<DnsQueryResult>();
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
    internal bool AllFromCache = true;
    internal bool Stale;
    internal bool HasTimeToLive;
    internal uint TimeToLive;
}

internal class DnsCachedAlias
{
    internal bool Found;
    internal bool Invalid;
    internal DnsAliasStep? Step;
    internal bool Stale;
    internal bool HasTimeToLive;
    internal uint TimeToLive;
    internal DnsMessage? Response;
}

internal static class StubAliasOperations
{
    internal static DnsResolveResult ResolveCandidate(
        DnsResolveRequest request,
        DnsResolverConfiguration configuration,
        DnsClockValue deadline,
        DnsStubResolver resolver)
    {
        DnsStubAliasState state = CreateState(request.Name);
        while (true)
        {
            DnsResolveRequest currentRequest =
                StubResolverOperations.CreateCandidateRequest(
                    request,
                    state.CurrentName);
            DnsResolveResult? special =
                SpecialUseResolverOperations.ResolveCandidateName(
                    currentRequest,
                    state.CurrentName);
            if (special != null)
            {
                return FinishResult(special, state);
            }

            DnsResolveResult? cached =
                StubResolverOperations.LookupCachedResult(
                    currentRequest,
                    resolver.Cache);
            if (cached != null)
            {
                return FinishResult(cached, state);
            }

            DnsCachedAlias cachedAlias = LookupCachedAlias(
                currentRequest,
                resolver.Cache);
            if (cachedAlias.Invalid ||
                (cachedAlias.Found && cachedAlias.Step == null))
            {
                DnsResolveResult invalid = AliasFailure(
                    DnsResolveStatus.ProtocolError,
                    "A cached alias record is incomplete or invalid.");
                invalid.Response = cachedAlias.Response;
                invalid.FromCache = true;
                return FinishResult(invalid, state);
            }

            if (cachedAlias.Found && cachedAlias.Step != null)
            {
                state.Stale = state.Stale || cachedAlias.Stale;
                UpdateTimeToLive(
                    state,
                    cachedAlias.HasTimeToLive,
                    cachedAlias.TimeToLive);
                DnsResolveResult? aliasFailure = ApplyStep(
                    state,
                    cachedAlias.Step,
                    request.Options.MaximumAliasDepth);
                if (aliasFailure != null)
                {
                    aliasFailure.FromCache = true;
                    return FinishResult(aliasFailure, state);
                }

                if (!request.Options.FollowAliases)
                {
                    DnsResolveResult stopped =
                        StubResolverOperations.CreateStatusResult(
                            DnsResolveStatus.NoData);
                    stopped.Response = cachedAlias.Response;
                    stopped.FromCache = true;
                    return FinishResult(stopped, state);
                }

                continue;
            }

            int remainingDepth = request.Options.MaximumAliasDepth -
                state.Steps.Count;
            if (!request.Options.FollowAliases && remainingDepth < 1)
            {
                remainingDepth = 1;
            }

            DnsResolveResult segment =
                StubResolverOperations.QuerySingleName(
                    currentRequest,
                    configuration,
                    deadline,
                    resolver,
                    remainingDepth);
            state.AllFromCache = false;
            AccumulateResult(state, segment);
            if (request.Options.UseCache)
            {
                StoreAliasEntries(segment, resolver.Cache);
            }

            int segmentStepCount = segment.AliasSteps.Count;
            DnsResolveResult? stepFailure = ApplySteps(
                state,
                segment.AliasSteps,
                request.Options.MaximumAliasDepth);
            segment.AliasSteps = new List<DnsAliasStep>();
            if (request.Options.UseCache)
            {
                DnsResolveRequest resultRequest =
                    StubResolverOperations.CreateCandidateRequest(
                        request,
                        state.CurrentName);
                StoreSegmentResult(resultRequest, segment, resolver.Cache);
            }

            if (stepFailure != null)
            {
                stepFailure.Response = segment.Response;
                return FinishResult(stepFailure, state);
            }

            if (request.Options.FollowAliases && segmentStepCount > 0 &&
                segment.Status == DnsResolveStatus.NoData)
            {
                continue;
            }

            return FinishResult(segment, state);
        }
    }

    internal static void StopAfterFirstAlias(DnsResolveResult result)
    {
        DnsAliasStep first = result.AliasSteps[0];
        result.Status = DnsResolveStatus.NoData;
        result.CanonicalName = RecordSetOperations.CopyName(
            first.Target,
            false);
        result.RecordSets = new List<DnsResourceRecordSet>();
        result.AliasSteps = new List<DnsAliasStep>();
        result.AliasSteps.Add(first);
        result.Diagnostics = new List<DnsDiagnostic>();
        result.HasTimeToLive = false;
        result.TimeToLive = 0;
        if (first.Record != null)
        {
            result.HasTimeToLive = true;
            result.TimeToLive = first.Record.TimeToLive;
        }
    }

    static DnsStubAliasState CreateState(DnsName name)
    {
        DnsStubAliasState result = new DnsStubAliasState();
        result.CurrentName = RecordSetOperations.CopyName(name, false);
        result.VisitedNames.Add(
            RecordSetOperations.CopyName(name, true));
        return result;
    }

    static DnsCachedAlias LookupCachedAlias(
        DnsResolveRequest request,
        IDnsCache cache)
    {
        DnsCachedAlias result = new DnsCachedAlias();
        if (!request.Options.UseCache || IsAliasQueryType(request))
        {
            return result;
        }

        DnsResolveRequest cnameRequest = CreateAliasRequest(
            request,
            request.Name,
            DnsRecordType.Cname,
            5);
        DnsCacheLookup cname = cache.Lookup(
            cnameRequest,
            TransportTime.ReadClock());
        if (IsPositiveLookup(cname))
        {
            return ReadCachedAlias(cname, request.Name, false);
        }

        int labelIndex = 1;
        while (labelIndex < request.Name.Labels.Count)
        {
            DnsName ancestor = CreateSuffix(request.Name, labelIndex);
            DnsResolveRequest dnameRequest = CreateAliasRequest(
                request,
                ancestor,
                DnsRecordType.Dname,
                39);
            DnsCacheLookup dname = cache.Lookup(
                dnameRequest,
                TransportTime.ReadClock());
            if (IsPositiveLookup(dname))
            {
                return ReadCachedAlias(dname, request.Name, true);
            }

            labelIndex = labelIndex + 1;
        }

        return result;
    }

    static DnsCachedAlias ReadCachedAlias(
        DnsCacheLookup lookup,
        DnsName currentName,
        bool dname)
    {
        DnsCachedAlias result = new DnsCachedAlias();
        result.Found = true;
        result.Stale = lookup.Stale;
        result.HasTimeToLive = lookup.HasRemainingTimeToLive;
        result.TimeToLive = lookup.RemainingTimeToLive;
        if (lookup.Entry == null)
        {
            result.Invalid = true;
            return result;
        }

        result.Response = lookup.Entry.Message;
        if (lookup.Entry.RecordSets.Count != 1 ||
            lookup.Entry.RecordSets[0].Records.Count != 1)
        {
            result.Invalid = true;
            return result;
        }

        DnsResourceRecordSet set = lookup.Entry.RecordSets[0];
        DnsResourceRecord record = set.Records[0];
        if (lookup.Stale)
        {
            result.HasTimeToLive = true;
            result.TimeToLive = set.TimeToLive;
        }

        int expectedType = dname ? 39 : 5;
        if (EffectiveValue(set.RecordTypeValue, (int)set.RecordType) !=
                expectedType ||
            EffectiveValue(record.RecordTypeValue, (int)record.RecordType) !=
                expectedType)
        {
            result.Invalid = true;
            return result;
        }

        DnsName? target = RecordSetOperations.ReadAliasTarget(record);
        if (target == null || !target.IsAbsolute)
        {
            result.Invalid = true;
            return result;
        }

        DnsName next = RecordSetOperations.CopyName(target, false);
        if (dname)
        {
            DnsNameOperationResult relative = DnsNameOperations.RelativizeName(
                currentName,
                set.Name);
            if (!relative.Success || relative.Name == null)
            {
                result.Invalid = true;
                return result;
            }

            DnsNameOperationResult combined =
                DnsNameOperations.ConcatenateNames(
                    relative.Name,
                    target);
            if (!combined.Success || combined.Name == null)
            {
                result.Invalid = true;
                return result;
            }

            next = combined.Name;
        }

        DnsAliasStep step = new DnsAliasStep();
        step.Owner = RecordSetOperations.CopyName(currentName, false);
        step.Target = RecordSetOperations.CopyName(next, false);
        step.RecordType = set.RecordType;
        step.RecordTypeValue = EffectiveValue(
            set.RecordTypeValue,
            (int)set.RecordType);
        step.Synthesized = dname;
        step.Record = record;
        result.Step = step;
        if (!result.HasTimeToLive)
        {
            result.HasTimeToLive = true;
            result.TimeToLive = set.TimeToLive;
        }

        return result;
    }

    static DnsResolveResult? ApplySteps(
        DnsStubAliasState state,
        List<DnsAliasStep> steps,
        int maximumAliasDepth)
    {
        int index = 0;
        while (index < steps.Count)
        {
            DnsResolveResult? failure = ApplyStep(
                state,
                steps[index],
                maximumAliasDepth);
            if (failure != null)
            {
                return failure;
            }

            index = index + 1;
        }

        return null;
    }

    static DnsResolveResult? ApplyStep(
        DnsStubAliasState state,
        DnsAliasStep step,
        int maximumAliasDepth)
    {
        if (!RecordSetOperations.NamesEqual(step.Owner, state.CurrentName))
        {
            return AliasFailure(
                DnsResolveStatus.ProtocolError,
                "An alias chain contains a discontinuous owner name.");
        }

        if (state.Steps.Count >= maximumAliasDepth)
        {
            return AliasFailure(
                DnsResolveStatus.DepthLimitExceeded,
                "The answer alias chain exceeds its configured depth.");
        }

        state.Steps.Add(step);
        if (step.Record != null)
        {
            UpdateTimeToLive(state, true, step.Record.TimeToLive);
        }

        state.CurrentName = RecordSetOperations.CopyName(step.Target, false);
        if (ContainsName(state.VisitedNames, state.CurrentName))
        {
            return AliasFailure(
                DnsResolveStatus.Loop,
                "The answer alias chain contains a loop.");
        }

        state.VisitedNames.Add(
            RecordSetOperations.CopyName(state.CurrentName, true));
        return null;
    }

    static DnsResolveResult FinishResult(
        DnsResolveResult result,
        DnsStubAliasState state)
    {
        StubResolverOperations.AppendQueries(state.Queries, result.Queries);
        StubResolverOperations.AppendDiagnostics(
            state.Diagnostics,
            result.Diagnostics);
        result.Queries = state.Queries;
        result.Diagnostics = state.Diagnostics;
        result.AliasSteps = state.Steps;
        result.CanonicalName = RecordSetOperations.CopyName(
            state.CurrentName,
            false);
        result.FromCache = state.AllFromCache && result.FromCache;
        result.Stale = result.Stale || state.Stale;
        UpdateTimeToLive(
            state,
            result.HasTimeToLive,
            result.TimeToLive);
        result.HasTimeToLive = state.HasTimeToLive;
        result.TimeToLive = state.TimeToLive;
        return result;
    }

    static void AccumulateResult(
        DnsStubAliasState state,
        DnsResolveResult result)
    {
        StubResolverOperations.AppendQueries(state.Queries, result.Queries);
        StubResolverOperations.AppendDiagnostics(
            state.Diagnostics,
            result.Diagnostics);
        result.Queries = new List<DnsQueryResult>();
        result.Diagnostics = new List<DnsDiagnostic>();
    }

    static void StoreAliasEntries(
        DnsResolveResult result,
        IDnsCache cache)
    {
        int index = 0;
        while (index < result.AliasSteps.Count)
        {
            DnsAliasStep step = result.AliasSteps[index];
            if (step.Record != null)
            {
                List<DnsResourceRecord> records =
                    new List<DnsResourceRecord>();
                records.Add(step.Record);
                List<DnsResourceRecordSet> sets =
                    RecordSetOperations.GroupRecordSets(records);
                if (sets.Count == 1 && sets[0].TimeToLive > 0)
                {
                    DnsCacheEntry entry = new DnsCacheEntry();
                    entry.Kind = DnsCacheEntryKind.Positive;
                    entry.Name = RecordSetOperations.CopyName(
                        step.Record.Name,
                        true);
                    entry.RecordType = step.Record.RecordType;
                    entry.RecordTypeValue = EffectiveValue(
                        step.Record.RecordTypeValue,
                        (int)step.Record.RecordType);
                    entry.Class = step.Record.Class;
                    entry.ClassValue = EffectiveValue(
                        step.Record.ClassValue,
                        (int)step.Record.Class);
                    entry.RecordSets = sets;
                    entry.Message = result.Response;
                    entry.SecurityStatus = result.SecurityStatus;
                    entry.StoredAt = TransportTime.ReadClock();
                    entry.OriginalTimeToLive = sets[0].TimeToLive;
                    entry.ExpiresAt = CreateExpiration(
                        entry.StoredAt,
                        entry.OriginalTimeToLive);
                    entry.Credibility = 4;
                    if (result.Response != null)
                    {
                        int flags = MessageOperations.EffectiveFlags(
                            result.Response.Header);
                        entry.Authoritative =
                            (flags &
                                (int)DnsHeaderFlag.AuthoritativeAnswer) != 0;
                    }

                    cache.Store(entry);
                }
            }

            index = index + 1;
        }
    }

    static void StoreSegmentResult(
        DnsResolveRequest request,
        DnsResolveResult segment,
        IDnsCache cache)
    {
        if (segment.Status != DnsResolveStatus.Success)
        {
            StubResolverOperations.StoreResult(request, segment, cache);
            return;
        }

        DnsResolveResult direct = new DnsResolveResult();
        direct.Status = segment.Status;
        direct.CanonicalName = RecordSetOperations.CopyName(
            request.Name,
            false);
        direct.Response = segment.Response;
        direct.RecordSets = segment.RecordSets;
        direct.SecurityStatus = segment.SecurityStatus;
        RecordSetOperations.SetResultTtl(direct);
        StubResolverOperations.StoreResult(request, direct, cache);
    }

    static DnsResolveRequest CreateAliasRequest(
        DnsResolveRequest source,
        DnsName name,
        DnsRecordType recordType,
        int recordTypeValue)
    {
        DnsResolveRequest result =
            StubResolverOperations.CreateCandidateRequest(source, name);
        result.RecordType = recordType;
        result.RecordTypeValue = recordTypeValue;
        return result;
    }

    static DnsName CreateSuffix(DnsName name, int firstLabel)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = name.IsAbsolute;
        int index = firstLabel;
        while (index < name.Labels.Count)
        {
            result.Labels.Add(
                RecordSetOperations.CopyBytes(name.Labels[index]));
            index = index + 1;
        }

        return result;
    }

    static bool IsPositiveLookup(DnsCacheLookup lookup)
    {
        return lookup.Found && lookup.Entry != null &&
            lookup.Entry.Kind == DnsCacheEntryKind.Positive;
    }

    static bool IsAliasQueryType(DnsResolveRequest request)
    {
        int typeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        return typeValue == 5 || typeValue == 39 || typeValue == 255;
    }

    static bool ContainsName(List<DnsName> names, DnsName name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (RecordSetOperations.NamesEqual(names[index], name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsResolveResult AliasFailure(
        DnsResolveStatus status,
        string reason)
    {
        DnsResolveResult result =
            StubResolverOperations.CreateStatusResult(status);
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
        diagnostic.Field = "answerChain";
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static void UpdateTimeToLive(
        DnsStubAliasState state,
        bool hasTimeToLive,
        uint timeToLive)
    {
        if (hasTimeToLive &&
            (!state.HasTimeToLive || timeToLive < state.TimeToLive))
        {
            state.HasTimeToLive = true;
            state.TimeToLive = timeToLive;
        }
    }

    static DnsClockValue CreateExpiration(
        DnsClockValue storedAt,
        uint timeToLive)
    {
        long milliseconds = (long)timeToLive * 1000;
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = AddSaturated(
            storedAt.UnixTimeMilliseconds,
            milliseconds);
        result.MonotonicMilliseconds = AddSaturated(
            storedAt.MonotonicMilliseconds,
            milliseconds);
        return result;
    }

    static long AddSaturated(long value, long amount)
    {
        if (value > long.MaxValue - amount)
        {
            return long.MaxValue;
        }

        return value + amount;
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
