using System.Collections.Generic;

namespace dns;

internal class DnsIterativeCacheSelection
{
    internal bool ReturnImmediately;
    internal DnsResolveResult? Result;
}

internal static class IterativeCacheOperations
{
    const int AnswerCredibility = 4;
    const int FailureCredibility = 0;
    const uint FailureTimeToLive = 5;

    internal static DnsIterativeCacheSelection ReadSelection(
        DnsResolveRequest request,
        IDnsCache cache)
    {
        DnsIterativeCacheSelection selection =
            new DnsIterativeCacheSelection();
        DnsCacheLookup lookup = cache.Lookup(
            request,
            TransportTime.ReadClock());
        if (lookup == null || !lookup.Found ||
            lookup.Entry == null)
        {
            return selection;
        }

        DnsCacheEntry entry = lookup.Entry;
        if (lookup.Stale && entry.Kind == DnsCacheEntryKind.Failure)
        {
            return selection;
        }

        DnsResolveResult result = new DnsResolveResult();
        result.CanonicalName = RecordSetOperations.CopyName(
            entry.Name,
            false);
        result.Response = entry.Message;
        result.RecordSets = entry.RecordSets;
        result.SecurityStatus = entry.SecurityStatus;
        result.FromCache = true;
        result.Stale = lookup.Stale;
        result.HasTimeToLive = lookup.HasRemainingTimeToLive;
        result.TimeToLive = lookup.RemainingTimeToLive;
        if (entry.Kind == DnsCacheEntryKind.Positive)
        {
            result.Status = DnsResolveStatus.Success;
        }
        else if (entry.Kind == DnsCacheEntryKind.NegativeName)
        {
            result.Status = DnsResolveStatus.NameError;
        }
        else if (entry.Kind == DnsCacheEntryKind.NegativeData)
        {
            result.Status = DnsResolveStatus.NoData;
        }
        else
        {
            result.Status = DnsResolveStatus.ServerFailure;
        }

        selection.Result = result;
        selection.ReturnImmediately = lookup.Fresh &&
            !lookup.NeedsPrefetch;
        if (selection.ReturnImmediately)
        {
            AddCacheDiagnostic(
                result,
                "cache.hit",
                "A fresh recursive cache entry satisfied the request.");
        }

        return selection;
    }

    static void AddCacheDiagnostic(
        DnsResolveResult result,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.None;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    internal static void StorePositive(
        DnsIterativeResolverState state,
        DnsMessage response,
        IDnsCache cache)
    {
        StoreAliasRecords(state, response, cache);
        int index = 0;
        while (index < state.Result.RecordSets.Count)
        {
            DnsResourceRecordSet set = state.Result.RecordSets[index];
            StoreRecordSet(
                set,
                state.Result.SecurityStatus,
                response,
                cache);
            index = index + 1;
        }
    }

    internal static void StoreNegative(
        DnsIterativeResolverState state,
        DnsMessage response,
        DnsResourceRecord soa,
        IDnsCache cache)
    {
        StoreAliasRecords(state, response, cache);
        DnsCacheEntry entry = CreateBaseEntry(
            state.CurrentName,
            EffectiveValue(
                state.Request.RecordTypeValue,
                (int)state.Request.RecordType),
            EffectiveValue(
                state.Request.ClassValue,
                (int)state.Request.Class),
            state.Result.SecurityStatus,
            response);
        entry.Kind = state.Result.Status == DnsResolveStatus.NameError ?
            DnsCacheEntryKind.NegativeName :
            DnsCacheEntryKind.NegativeData;
        entry.NegativeSoa = RecordSetOperations.CopyRecord(
            soa,
            soa.Name);
        entry.OriginalTimeToLive = state.Result.TimeToLive;
        entry.Credibility = AnswerCredibility;
        entry.Authoritative = true;
        cache.Store(entry);
    }

    internal static void StoreFailure(
        DnsIterativeResolverState state,
        IDnsCache cache)
    {
        if (state.Result.Status != DnsResolveStatus.ServerFailure &&
            state.Result.Status != DnsResolveStatus.TimedOut &&
            state.Result.Status != DnsResolveStatus.ProtocolError &&
            state.Result.Status != DnsResolveStatus.Refused &&
            state.Result.Status != DnsResolveStatus.Unsupported)
        {
            return;
        }

        DnsCacheEntry entry = CreateBaseEntry(
            state.Request.Name,
            EffectiveValue(
                state.Request.RecordTypeValue,
                (int)state.Request.RecordType),
            EffectiveValue(
                state.Request.ClassValue,
                (int)state.Request.Class),
            state.Result.SecurityStatus,
            state.Result.Response);
        entry.Kind = DnsCacheEntryKind.Failure;
        entry.OriginalTimeToLive = FailureTimeToLive;
        entry.Credibility = FailureCredibility;
        cache.Store(entry);
    }

    static void StoreAliasRecords(
        DnsIterativeResolverState state,
        DnsMessage response,
        IDnsCache cache)
    {
        int index = 0;
        while (index < state.Result.AliasSteps.Count)
        {
            DnsAliasStep step = state.Result.AliasSteps[index];
            if (step.Record != null && step.Record.TimeToLive > 0)
            {
                List<DnsResourceRecord> records =
                    new List<DnsResourceRecord>();
                records.Add(step.Record);
                List<DnsResourceRecordSet> sets =
                    RecordSetOperations.GroupRecordSets(records);
                if (sets.Count == 1)
                {
                    StoreRecordSet(
                        sets[0],
                        state.Result.SecurityStatus,
                        response,
                        cache);
                }
            }

            index = index + 1;
        }
    }

    static void StoreRecordSet(
        DnsResourceRecordSet set,
        DnsSecurityStatus securityStatus,
        DnsMessage response,
        IDnsCache cache)
    {
        if (set == null || set.TimeToLive == 0 ||
            set.Records.Count == 0)
        {
            return;
        }

        int typeValue = EffectiveValue(
            set.RecordTypeValue,
            (int)set.RecordType);
        int classValue = EffectiveValue(
            set.ClassValue,
            (int)set.Class);
        DnsCacheEntry entry = CreateBaseEntry(
            set.Name,
            typeValue,
            classValue,
            securityStatus,
            response);
        entry.Kind = DnsCacheEntryKind.Positive;
        entry.RecordSets.Add(CopyRecordSet(set));
        entry.OriginalTimeToLive = set.TimeToLive;
        entry.ExpiresAt = CreateExpiration(
            entry.StoredAt,
            set.TimeToLive);
        entry.Credibility = AnswerCredibility;
        entry.Authoritative = true;
        cache.Store(entry);
    }

    static DnsCacheEntry CreateBaseEntry(
        DnsName name,
        int typeValue,
        int classValue,
        DnsSecurityStatus securityStatus,
        DnsMessage? response)
    {
        DnsCacheEntry result = new DnsCacheEntry();
        result.Name = RecordSetOperations.CopyName(name, true);
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.Message = response;
        result.SecurityStatus = securityStatus;
        result.StoredAt = TransportTime.ReadClock();
        return result;
    }

    static DnsResourceRecordSet CopyRecordSet(DnsResourceRecordSet source)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.TimeToLive = source.TimeToLive;
        int index = 0;
        while (index < source.Records.Count)
        {
            result.Records.Add(RecordSetOperations.CopyRecord(
                source.Records[index],
                result.Name));
            index = index + 1;
        }

        return result;
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
