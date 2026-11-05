using System.Collections.Generic;
using System.Text.Json;

namespace dns;

internal class DnsCacheItem
{
    internal DnsCacheEntry Entry = new DnsCacheEntry();
    internal long AccessSequence;
}

internal static class CacheIndex
{
    const uint MinimumFailureTimeToLive = 1;
    const uint MaximumFailureTimeToLive = 300;
    const uint StaleResponseTimeToLive = 30;

    internal static DnsCacheLookup Lookup(
        DnsResolveRequest request,
        DnsClockValue now,
        MemoryDnsCache cache)
    {
        if (request == null || now == null || cache == null)
        {
            throw new System.ArgumentNullException("request");
        }

        DnsCacheLookup result = new DnsCacheLookup();
        result.LookedUpAt = CopyClock(now);
        DnsResolverOptions options = request.Options;
        if (options == null)
        {
            throw new System.ArgumentException(
                "Resolver options are required.");
        }

        DnsName canonicalName = RecordSetOperations.CopyName(request.Name, true);
        int typeValue = RecordSetOperations.EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        int classValue = RecordSetOperations.EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        while (true)
        {
            int index = FindLookupEntry(
                cache.Entries,
                canonicalName,
                typeValue,
                classValue);
            if (index < 0)
            {
                return result;
            }

            DnsCacheItem item = cache.Entries[index];
            long remainingMilliseconds = PositiveDifference(
                item.Entry.ExpiresAt.MonotonicMilliseconds,
                now.MonotonicMilliseconds);
            if (remainingMilliseconds == 0)
            {
                if (!WithinStaleLifetime(item.Entry, now, options))
                {
                    cache.Entries.RemoveAt(index);
                    continue;
                }

                if (!options.AllowStale)
                {
                    return result;
                }

                DnsCacheEntry staleCopy = CloneEntry(item.Entry);
                SetStaleTimeToLive(staleCopy);
                cache.NextAccessSequence = cache.NextAccessSequence + 1;
                item.AccessSequence = cache.NextAccessSequence;
                result.Found = true;
                result.Stale = true;
                result.Entry = staleCopy;
                result.HasRemainingTimeToLive = true;
                return result;
            }

            uint remainingTimeToLive = RemainingSeconds(
                remainingMilliseconds,
                item.Entry.OriginalTimeToLive);
            long elapsedMilliseconds = PositiveDifference(
                now.MonotonicMilliseconds,
                item.Entry.StoredAt.MonotonicMilliseconds);
            long elapsedSeconds = elapsedMilliseconds / 1000;
            DnsCacheEntry copy = CloneEntry(item.Entry);
            AgeEntry(copy, elapsedSeconds);
            cache.NextAccessSequence = cache.NextAccessSequence + 1;
            item.AccessSequence = cache.NextAccessSequence;
            result.Found = true;
            result.Fresh = true;
            result.Entry = copy;
            result.HasRemainingTimeToLive = true;
            result.RemainingTimeToLive = remainingTimeToLive;
            result.NeedsPrefetch = options.Prefetch && ShouldPrefetch(
                remainingTimeToLive,
                item.Entry.OriginalTimeToLive,
                options.PrefetchThresholdPercent);
            return result;
        }
    }

    internal static void Store(DnsCacheEntry entry, MemoryDnsCache cache)
    {
        if (entry == null || cache == null)
        {
            throw new System.ArgumentNullException("entry");
        }

        DnsName canonicalName = RecordSetOperations.CopyName(entry.Name, true);
        int typeValue = RecordSetOperations.EffectiveValue(
            entry.RecordTypeValue,
            (int)entry.RecordType);
        int classValue = RecordSetOperations.EffectiveValue(
            entry.ClassValue,
            (int)entry.Class);
        uint timeToLive = entry.OriginalTimeToLive;
        bool derivedExpiration = false;
        if (entry.Kind == DnsCacheEntryKind.NegativeName ||
            entry.Kind == DnsCacheEntryKind.NegativeData)
        {
            timeToLive = ReadNegativeTimeToLive(entry);
            derivedExpiration = true;
        }
        else if (entry.Kind == DnsCacheEntryKind.Failure)
        {
            timeToLive = BoundFailureTimeToLive(timeToLive);
            derivedExpiration = true;
            int preservedIndex = FindLookupEntry(
                cache.Entries,
                canonicalName,
                typeValue,
                classValue);
            if (preservedIndex >= 0 &&
                PreservesAnswerOnFailure(
                    cache.Entries[preservedIndex].Entry,
                    entry.StoredAt))
            {
                return;
            }
        }

        int index = FindReplacementEntry(
            cache.Entries,
            canonicalName,
            typeValue,
            classValue,
            entry.Kind);
        if (index >= 0 && !CanReplace(
            cache.Entries[index].Entry,
            entry))
        {
            return;
        }

        if (timeToLive == 0 ||
            (!derivedExpiration &&
                entry.ExpiresAt.MonotonicMilliseconds <=
                    entry.StoredAt.MonotonicMilliseconds) ||
            cache.MaximumEntryCount <= 0)
        {
            if (index >= 0)
            {
                cache.Entries.RemoveAt(index);
            }

            TrimToCapacity(cache);
            return;
        }

        DnsCacheEntry copy = CloneEntry(entry);
        copy.Name = canonicalName;
        copy.RecordType = (DnsRecordType)typeValue;
        copy.RecordTypeValue = typeValue;
        copy.Class = (DnsClass)classValue;
        copy.ClassValue = classValue;
        if (derivedExpiration)
        {
            copy.OriginalTimeToLive = timeToLive;
            copy.ExpiresAt = CreateExpiration(copy.StoredAt, timeToLive);
        }

        if (entry.Kind == DnsCacheEntryKind.NegativeName ||
            entry.Kind == DnsCacheEntryKind.NegativeData)
        {
            CapNegativeRecords(copy, timeToLive);
        }

        if (entry.Kind == DnsCacheEntryKind.NegativeName)
        {
            RemoveNegativeSubtree(
                cache.Entries,
                canonicalName,
                classValue,
                entry);
            index = -1;
        }

        cache.NextAccessSequence = cache.NextAccessSequence + 1;
        if (index >= 0)
        {
            cache.Entries[index].Entry = copy;
            cache.Entries[index].AccessSequence = cache.NextAccessSequence;
        }
        else
        {
            DnsCacheItem item = new DnsCacheItem();
            item.Entry = copy;
            item.AccessSequence = cache.NextAccessSequence;
            cache.Entries.Add(item);
        }

        TrimToCapacity(cache);
    }

    internal static void Flush(MemoryDnsCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        cache.Entries.Clear();
    }

    internal static void InvalidateSubtree(
        DnsName name,
        MemoryDnsCache cache)
    {
        if (name == null || cache == null)
        {
            throw new System.ArgumentNullException("name");
        }

        DnsName canonicalName = RecordSetOperations.CopyName(name, true);
        int index = cache.Entries.Count - 1;
        while (index >= 0)
        {
            DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
                cache.Entries[index].Entry.Name,
                canonicalName);
            if (!suffix.Success)
            {
                throw new System.ArgumentException("A valid DNS name is required.");
            }

            if (suffix.BooleanValue)
            {
                cache.Entries.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static int FindLookupEntry(
        List<DnsCacheItem> entries,
        DnsName name,
        int typeValue,
        int classValue)
    {
        int exactIndex = FindExactEntry(
            entries,
            name,
            typeValue,
            classValue);
        if (exactIndex >= 0)
        {
            return exactIndex;
        }

        int bestIndex = -1;
        int bestLabelCount = -1;
        int index = 0;
        while (index < entries.Count)
        {
            DnsCacheEntry candidate = entries[index].Entry;
            DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
                name,
                candidate.Name);
            if (candidate.Kind == DnsCacheEntryKind.NegativeName &&
                candidate.ClassValue == classValue &&
                suffix.Success && suffix.BooleanValue &&
                candidate.Name.Labels.Count > bestLabelCount)
            {
                bestIndex = index;
                bestLabelCount = candidate.Name.Labels.Count;
            }

            index = index + 1;
        }

        return bestIndex;
    }

    static int FindExactEntry(
        List<DnsCacheItem> entries,
        DnsName name,
        int typeValue,
        int classValue)
    {
        int index = 0;
        while (index < entries.Count)
        {
            DnsCacheEntry candidate = entries[index].Entry;
            DnsNameOperationResult equal = DnsNameOperations.NamesEqual(
                candidate.Name,
                name);
            if (equal.Success && equal.BooleanValue &&
                candidate.RecordTypeValue == typeValue &&
                candidate.ClassValue == classValue)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindReplacementEntry(
        List<DnsCacheItem> entries,
        DnsName name,
        int typeValue,
        int classValue,
        DnsCacheEntryKind kind)
    {
        if (kind != DnsCacheEntryKind.NegativeName)
        {
            return FindExactEntry(entries, name, typeValue, classValue);
        }

        int index = 0;
        while (index < entries.Count)
        {
            DnsCacheEntry candidate = entries[index].Entry;
            DnsNameOperationResult equal = DnsNameOperations.NamesEqual(
                candidate.Name,
                name);
            if (candidate.Kind == DnsCacheEntryKind.NegativeName &&
                candidate.ClassValue == classValue &&
                equal.Success && equal.BooleanValue)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static uint ReadNegativeTimeToLive(DnsCacheEntry entry)
    {
        DnsResourceRecord? soaRecord = entry.NegativeSoa;
        if (soaRecord == null || soaRecord.Data == null ||
            soaRecord.Data.Soa == null)
        {
            return 0;
        }

        int typeValue = RecordSetOperations.EffectiveValue(
            soaRecord.RecordTypeValue,
            (int)soaRecord.RecordType);
        if (typeValue != 6)
        {
            return 0;
        }

        uint timeToLive = soaRecord.TimeToLive;
        if (soaRecord.Data.Soa.Minimum < timeToLive)
        {
            timeToLive = soaRecord.Data.Soa.Minimum;
        }

        return timeToLive;
    }

    static uint BoundFailureTimeToLive(uint timeToLive)
    {
        if (timeToLive < MinimumFailureTimeToLive)
        {
            return MinimumFailureTimeToLive;
        }

        if (timeToLive > MaximumFailureTimeToLive)
        {
            return MaximumFailureTimeToLive;
        }

        return timeToLive;
    }

    static bool PreservesAnswerOnFailure(
        DnsCacheEntry entry,
        DnsClockValue now)
    {
        if (entry.Kind == DnsCacheEntryKind.Failure)
        {
            return false;
        }

        if (entry.ExpiresAt.MonotonicMilliseconds >
            now.MonotonicMilliseconds)
        {
            return true;
        }

        return entry.HasStaleUntil &&
            entry.StaleUntil.MonotonicMilliseconds >
                now.MonotonicMilliseconds;
    }

    static bool CanReplace(
        DnsCacheEntry existing,
        DnsCacheEntry incoming)
    {
        if (existing.ExpiresAt.MonotonicMilliseconds <=
            incoming.StoredAt.MonotonicMilliseconds)
        {
            return true;
        }

        if (existing.Credibility > incoming.Credibility)
        {
            return false;
        }

        if (existing.Authoritative && !incoming.Authoritative)
        {
            return false;
        }

        if (existing.SecurityStatus == DnsSecurityStatus.Secure &&
            incoming.SecurityStatus != DnsSecurityStatus.Secure)
        {
            return false;
        }

        return true;
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

    static bool WithinStaleLifetime(
        DnsCacheEntry entry,
        DnsClockValue now,
        DnsResolverOptions options)
    {
        bool hasDeadline = entry.HasStaleUntil;
        long deadline = entry.StaleUntil.MonotonicMilliseconds;
        if (options.ServeStaleWindowSeconds > 0)
        {
            long configuredMilliseconds =
                (long)options.ServeStaleWindowSeconds * 1000;
            long configuredDeadline = AddSaturated(
                entry.ExpiresAt.MonotonicMilliseconds,
                configuredMilliseconds);
            if (!hasDeadline || configuredDeadline < deadline)
            {
                hasDeadline = true;
                deadline = configuredDeadline;
            }
        }

        return hasDeadline && deadline > now.MonotonicMilliseconds;
    }

    static bool ShouldPrefetch(
        uint remainingTimeToLive,
        uint originalTimeToLive,
        int thresholdPercent)
    {
        if (originalTimeToLive == 0 || thresholdPercent <= 0)
        {
            return false;
        }

        if (thresholdPercent > 100)
        {
            thresholdPercent = 100;
        }

        long remainingPercentValue = (long)remainingTimeToLive * 100;
        long thresholdValue =
            (long)originalTimeToLive * thresholdPercent;
        return remainingPercentValue <= thresholdValue;
    }

    static void SetStaleTimeToLive(DnsCacheEntry entry)
    {
        int setIndex = 0;
        while (setIndex < entry.RecordSets.Count)
        {
            DnsResourceRecordSet set = entry.RecordSets[setIndex];
            set.TimeToLive = StaleTimeToLive(set.TimeToLive);
            int recordIndex = 0;
            while (recordIndex < set.Records.Count)
            {
                DnsResourceRecord record = set.Records[recordIndex];
                record.TimeToLive = StaleTimeToLive(record.TimeToLive);
                recordIndex = recordIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        if (entry.Message != null)
        {
            SetStaleRecordTimeToLive(entry.Message.Answers);
            SetStaleRecordTimeToLive(entry.Message.Authorities);
            SetStaleRecordTimeToLive(entry.Message.Additionals);
        }

        if (entry.NegativeSoa != null)
        {
            entry.NegativeSoa.TimeToLive = StaleTimeToLive(
                entry.NegativeSoa.TimeToLive);
        }
    }

    static void SetStaleRecordTimeToLive(
        List<DnsResourceRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            int typeValue = RecordSetOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue != 41)
            {
                record.TimeToLive = StaleTimeToLive(record.TimeToLive);
            }

            index = index + 1;
        }
    }

    static uint StaleTimeToLive(uint originalTimeToLive)
    {
        if (originalTimeToLive > StaleResponseTimeToLive)
        {
            return StaleResponseTimeToLive;
        }

        return originalTimeToLive;
    }

    static void CapNegativeRecords(
        DnsCacheEntry entry,
        uint timeToLive)
    {
        if (entry.NegativeSoa != null)
        {
            entry.NegativeSoa.TimeToLive = timeToLive;
        }

        int setIndex = 0;
        while (setIndex < entry.RecordSets.Count)
        {
            DnsResourceRecordSet set = entry.RecordSets[setIndex];
            int typeValue = RecordSetOperations.EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue == 6)
            {
                set.TimeToLive = timeToLive;
                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    set.Records[recordIndex].TimeToLive = timeToLive;
                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }

        if (entry.Message != null)
        {
            CapAuthoritySoaRecords(entry.Message.Authorities, timeToLive);
        }
    }

    static void CapAuthoritySoaRecords(
        List<DnsResourceRecord> records,
        uint timeToLive)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            int typeValue = RecordSetOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue == 6)
            {
                record.TimeToLive = timeToLive;
            }

            index = index + 1;
        }
    }

    static void RemoveNegativeSubtree(
        List<DnsCacheItem> entries,
        DnsName name,
        int classValue,
        DnsCacheEntry incoming)
    {
        int index = entries.Count - 1;
        while (index >= 0)
        {
            DnsCacheEntry candidate = entries[index].Entry;
            DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
                candidate.Name,
                name);
            if (candidate.ClassValue == classValue &&
                suffix.Success && suffix.BooleanValue &&
                CanReplace(candidate, incoming))
            {
                entries.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static void TrimToCapacity(MemoryDnsCache cache)
    {
        int capacity = cache.MaximumEntryCount;
        if (capacity < 0)
        {
            capacity = 0;
        }

        while (cache.Entries.Count > capacity)
        {
            int oldestIndex = 0;
            int index = 1;
            while (index < cache.Entries.Count)
            {
                if (cache.Entries[index].AccessSequence <
                    cache.Entries[oldestIndex].AccessSequence)
                {
                    oldestIndex = index;
                }

                index = index + 1;
            }

            cache.Entries.RemoveAt(oldestIndex);
        }
    }

    static DnsCacheEntry CloneEntry(DnsCacheEntry entry)
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.IncludeFields = true;
        string serialized = JsonSerializer.Serialize(entry, options);
        DnsCacheEntry? result = JsonSerializer.Deserialize<DnsCacheEntry>(
            serialized,
            options);
        if (result == null)
        {
            throw new System.InvalidOperationException(
                "The DNS cache entry could not be cloned.");
        }

        return result;
    }

    static DnsClockValue CopyClock(DnsClockValue value)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = value.UnixTimeMilliseconds;
        result.MonotonicMilliseconds = value.MonotonicMilliseconds;
        return result;
    }

    static long PositiveDifference(long later, long earlier)
    {
        if (later <= earlier)
        {
            return 0;
        }

        if (earlier < 0 && later > long.MaxValue + earlier)
        {
            return long.MaxValue;
        }

        return later - earlier;
    }

    static uint RemainingSeconds(long remainingMilliseconds, uint maximum)
    {
        long seconds = remainingMilliseconds / 1000;
        if (remainingMilliseconds % 1000 != 0)
        {
            seconds = seconds + 1;
        }

        if (seconds > maximum)
        {
            return maximum;
        }

        return (uint)seconds;
    }

    static void AgeEntry(DnsCacheEntry entry, long elapsedSeconds)
    {
        int setIndex = 0;
        while (setIndex < entry.RecordSets.Count)
        {
            DnsResourceRecordSet set = entry.RecordSets[setIndex];
            set.TimeToLive = AgeTimeToLive(set.TimeToLive, elapsedSeconds);
            int recordIndex = 0;
            while (recordIndex < set.Records.Count)
            {
                DnsResourceRecord record = set.Records[recordIndex];
                record.TimeToLive = AgeTimeToLive(
                    record.TimeToLive,
                    elapsedSeconds);
                recordIndex = recordIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        if (entry.Message != null)
        {
            AgeRecords(entry.Message.Answers, elapsedSeconds);
            AgeRecords(entry.Message.Authorities, elapsedSeconds);
            AgeRecords(entry.Message.Additionals, elapsedSeconds);
        }

        if (entry.NegativeSoa != null)
        {
            entry.NegativeSoa.TimeToLive = AgeTimeToLive(
                entry.NegativeSoa.TimeToLive,
                elapsedSeconds);
        }
    }

    static void AgeRecords(
        List<DnsResourceRecord> records,
        long elapsedSeconds)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            int typeValue = RecordSetOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue != 41)
            {
                record.TimeToLive = AgeTimeToLive(
                    record.TimeToLive,
                    elapsedSeconds);
            }

            index = index + 1;
        }
    }

    static uint AgeTimeToLive(uint value, long elapsedSeconds)
    {
        if (elapsedSeconds <= 0)
        {
            return value;
        }

        if (elapsedSeconds >= value)
        {
            return 0;
        }

        return value - (uint)elapsedSeconds;
    }
}
