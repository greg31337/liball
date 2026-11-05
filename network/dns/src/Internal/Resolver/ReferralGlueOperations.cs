using System.Collections.Generic;

namespace dns;

internal class DnsReferralGlueResult
{
    internal bool Invalid;
    internal string Reason = string.Empty;
}

internal static class ReferralGlueOperations
{
    const int AuthorityCredibility = 3;
    const int GlueCredibility = 2;
    const int AnswerCredibility = 4;

    internal static DnsReferralGlueResult AcceptGlue(
        DnsDelegation delegation,
        DnsMessage response,
        IDnsCache cache,
        DnsIterativeResolverState state)
    {
        DnsReferralGlueResult result = new DnsReferralGlueResult();
        int index = 0;
        while (index < response.Additionals.Count)
        {
            if (!IterativeResolverOperations.ConsumeWork(state, 1))
            {
                return result;
            }

            DnsResourceRecord record = response.Additionals[index];
            if (record == null)
            {
                result.Invalid = true;
                result.Reason =
                    "A referral additional section contains a null record.";
                return result;
            }

            int typeValue = EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue == 1 || typeValue == 28)
            {
                AcceptAddressRecord(delegation, response, record, result);
                if (result.Invalid)
                {
                    return result;
                }
            }

            index = index + 1;
        }

        delegation.InBailiwick = true;
        DnsClockValue now = TransportTime.ReadClock();
        StoreAuthority(delegation, cache, now);
        StoreGlue(delegation, cache, now);
        return result;
    }

    internal static void StoreResolvedAddresses(
        DnsAddressResult addresses,
        IDnsCache cache)
    {
        DnsClockValue now = TransportTime.ReadClock();
        StoreAddressFamily(
            addresses,
            4,
            DnsRecordType.A,
            1,
            AnswerCredibility,
            cache,
            now);
        StoreAddressFamily(
            addresses,
            6,
            DnsRecordType.Aaaa,
            28,
            AnswerCredibility,
            cache,
            now);
    }

    static void AcceptAddressRecord(
        DnsDelegation delegation,
        DnsMessage response,
        DnsResourceRecord record,
        DnsReferralGlueResult result)
    {
        RawDnsNameResult owner = RawDnsName.ValidateName(record.Name);
        if (!owner.Success || owner.Name == null ||
            !owner.Name.IsAbsolute)
        {
            result.Invalid = true;
            result.Reason = "A referral address record has an invalid owner.";
            return;
        }

        if (!ContainsName(delegation.NameServers, owner.Name))
        {
            return;
        }

        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            owner.Name,
            delegation.Zone);
        if (!suffix.Success)
        {
            result.Invalid = true;
            result.Reason =
                "A referral address record cannot be checked for bailiwick.";
            return;
        }

        if (!suffix.BooleanValue)
        {
            return;
        }

        int typeValue = EffectiveValue(
            record.RecordTypeValue,
            (int)record.RecordType);
        int family = typeValue == 1 ? 4 : 6;
        int expectedLength = family == 4 ? 4 : 16;
        if (EffectiveValue(record.ClassValue, (int)record.Class) != 1 ||
            record.Data == null || record.Data.Address == null ||
            record.Data.Address.Address == null ||
            record.Data.Address.Address.Length != expectedLength)
        {
            result.Invalid = true;
            result.Reason =
                "An in-bailiwick referral address record is malformed.";
            return;
        }

        DnsAddressResult addresses = FindOrCreateAddresses(
            delegation,
            owner.Name,
            response,
            record.TimeToLive);
        if (!ContainsAddress(
            addresses,
            record.Data.Address.Address,
            family))
        {
            addresses.Addresses.Add(RecordSetOperations.CopyBytes(
                record.Data.Address.Address));
            addresses.AddressFamilies.Add(family);
        }

        if (record.TimeToLive < addresses.TimeToLive)
        {
            addresses.TimeToLive = record.TimeToLive;
        }
    }

    static DnsAddressResult FindOrCreateAddresses(
        DnsDelegation delegation,
        DnsName name,
        DnsMessage response,
        uint timeToLive)
    {
        int index = 0;
        while (index < delegation.GlueAddresses.Count)
        {
            DnsAddressResult candidate = delegation.GlueAddresses[index];
            if (candidate != null && candidate.CanonicalName != null &&
                RecordSetOperations.NamesEqual(
                    candidate.CanonicalName,
                    name))
            {
                return candidate;
            }

            index = index + 1;
        }

        DnsAddressResult result = new DnsAddressResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = RecordSetOperations.CopyName(name, false);
        result.Response = response;
        result.SecurityStatus = DnsSecurityStatus.Insecure;
        result.HasTimeToLive = true;
        result.TimeToLive = timeToLive;
        delegation.GlueAddresses.Add(result);
        return result;
    }

    internal static void StoreAuthority(
        DnsDelegation delegation,
        IDnsCache cache,
        DnsClockValue now)
    {
        int index = 0;
        while (index < delegation.AuthorityRecordSets.Count)
        {
            DnsResourceRecordSet set = delegation.AuthorityRecordSets[index];
            DnsCacheEntry entry = CreateCacheEntry(
                set,
                AuthorityCredibility,
                delegation.Secure ?
                    DnsSecurityStatus.Secure :
                    DnsSecurityStatus.Insecure,
                now);
            SafeStore(cache, entry, now);
            index = index + 1;
        }
    }

    static void StoreGlue(
        DnsDelegation delegation,
        IDnsCache cache,
        DnsClockValue now)
    {
        int index = 0;
        while (index < delegation.GlueAddresses.Count)
        {
            DnsAddressResult addresses = delegation.GlueAddresses[index];
            StoreGlueFamily(addresses, 4, DnsRecordType.A, 1, cache, now);
            StoreGlueFamily(
                addresses,
                6,
                DnsRecordType.Aaaa,
                28,
                cache,
                now);
            index = index + 1;
        }
    }

    static void StoreGlueFamily(
        DnsAddressResult addresses,
        int family,
        DnsRecordType recordType,
        int recordTypeValue,
        IDnsCache cache,
        DnsClockValue now)
    {
        StoreAddressFamily(
            addresses,
            family,
            recordType,
            recordTypeValue,
            GlueCredibility,
            cache,
            now);
    }

    static void StoreAddressFamily(
        DnsAddressResult addresses,
        int family,
        DnsRecordType recordType,
        int recordTypeValue,
        int credibility,
        IDnsCache cache,
        DnsClockValue now)
    {
        if (addresses.CanonicalName == null ||
            !addresses.HasTimeToLive)
        {
            return;
        }

        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = RecordSetOperations.CopyName(
            addresses.CanonicalName,
            false);
        set.RecordType = recordType;
        set.RecordTypeValue = recordTypeValue;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = addresses.TimeToLive;
        int index = 0;
        while (index < addresses.Addresses.Count)
        {
            if (addresses.AddressFamilies[index] == family)
            {
                DnsResourceRecord record = new DnsResourceRecord();
                record.Name = RecordSetOperations.CopyName(set.Name, false);
                record.RecordType = recordType;
                record.RecordTypeValue = recordTypeValue;
                record.Class = DnsClass.Internet;
                record.ClassValue = 1;
                record.TimeToLive = set.TimeToLive;
                record.Data = new DnsRecordData();
                record.Data.Address = new DnsAddressData();
                record.Data.Address.Address =
                    RecordSetOperations.CopyBytes(
                        addresses.Addresses[index]);
                set.Records.Add(record);
            }

            index = index + 1;
        }

        if (set.Records.Count > 0)
        {
            DnsCacheEntry entry = CreateCacheEntry(
                set,
                credibility,
                DnsSecurityStatus.Insecure,
                now);
            SafeStore(cache, entry, now);
        }
    }

    static DnsCacheEntry CreateCacheEntry(
        DnsResourceRecordSet set,
        int credibility,
        DnsSecurityStatus securityStatus,
        DnsClockValue now)
    {
        DnsCacheEntry result = new DnsCacheEntry();
        result.Kind = DnsCacheEntryKind.Positive;
        result.Name = RecordSetOperations.CopyName(set.Name, true);
        result.RecordType = set.RecordType;
        result.RecordTypeValue = set.RecordTypeValue;
        result.Class = set.Class;
        result.ClassValue = set.ClassValue;
        result.RecordSets.Add(CopyRecordSet(set));
        result.SecurityStatus = securityStatus;
        result.StoredAt = CopyClock(now);
        result.OriginalTimeToLive = set.TimeToLive;
        result.ExpiresAt = CreateExpiration(now, set.TimeToLive);
        result.Authoritative = false;
        result.Credibility = credibility;
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

    static void SafeStore(
        IDnsCache cache,
        DnsCacheEntry entry,
        DnsClockValue now)
    {
        if (entry.OriginalTimeToLive == 0)
        {
            return;
        }

        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = RecordSetOperations.CopyName(entry.Name, false);
        request.RecordType = entry.RecordType;
        request.RecordTypeValue = entry.RecordTypeValue;
        request.Class = entry.Class;
        request.ClassValue = entry.ClassValue;
        DnsCacheLookup existing = cache.Lookup(request, now);
        if (existing != null && existing.Found && existing.Fresh &&
            existing.Entry != null &&
            (existing.Entry.Credibility > entry.Credibility ||
                (existing.Entry.SecurityStatus == DnsSecurityStatus.Secure &&
                    entry.SecurityStatus != DnsSecurityStatus.Secure) ||
                (existing.Entry.Authoritative && !entry.Authoritative)))
        {
            return;
        }

        cache.Store(entry);
    }

    static DnsClockValue CopyClock(DnsClockValue source)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = source.UnixTimeMilliseconds;
        result.MonotonicMilliseconds = source.MonotonicMilliseconds;
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

    static bool ContainsAddress(
        DnsAddressResult addresses,
        byte[] candidate,
        int family)
    {
        int index = 0;
        while (index < addresses.Addresses.Count)
        {
            if (addresses.AddressFamilies[index] == family &&
                BytesEqual(addresses.Addresses[index], candidate))
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
