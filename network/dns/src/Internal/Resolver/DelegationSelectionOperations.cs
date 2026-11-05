using System.Collections.Generic;

namespace dns;

internal static class DelegationSelectionOperations
{
    internal static DnsDelegation SelectClosestDelegation(
        DnsName name,
        DnsRootHints rootHints,
        IDnsCache cache)
    {
        if (name == null || cache == null)
        {
            throw new DnsResolutionException(
                "A target name and resolver cache are required.");
        }

        RawDnsNameResult validatedName = RawDnsName.ValidateName(name);
        if (!validatedName.Success || validatedName.Name == null ||
            !validatedName.Name.IsAbsolute)
        {
            throw new DnsResolutionException(
                "Closest-delegation selection requires an absolute DNS name.");
        }

        DnsRootHints validatedHints = RootHintOperations.ValidateRootHints(
            rootHints);
        DnsClockValue now = TransportTime.ReadClock();
        int labelIndex = 0;
        while (labelIndex <= validatedName.Name.Labels.Count)
        {
            DnsName ancestor = CreateAncestor(
                validatedName.Name,
                labelIndex);
            DnsResolveRequest request = CreateCacheRequest(
                ancestor,
                DnsRecordType.Ns,
                2);
            DnsCacheLookup lookup = cache.Lookup(request, now);
            DnsDelegation? candidate = ReadCachedDelegation(
                lookup,
                ancestor,
                cache,
                now);
            if (candidate != null)
            {
                if (ancestor.Labels.Count != 0 ||
                    candidate.GlueAddresses.Count > 0)
                {
                    return candidate;
                }

                break;
            }

            labelIndex = labelIndex + 1;
        }

        return CopyRootDelegation(validatedHints.Root);
    }

    static DnsDelegation? ReadCachedDelegation(
        DnsCacheLookup lookup,
        DnsName zone,
        IDnsCache cache,
        DnsClockValue now)
    {
        if (!lookup.Found || !lookup.Fresh || lookup.Entry == null ||
            lookup.Entry.Kind != DnsCacheEntryKind.Positive ||
            lookup.Entry.RecordSets == null)
        {
            return null;
        }

        DnsResourceRecordSet? nameServerSet = FindNameServerSet(
            lookup.Entry.RecordSets,
            zone);
        if (nameServerSet == null)
        {
            return null;
        }

        List<DnsName>? names = TryReadNameServers(nameServerSet);
        if (names == null || names.Count == 0)
        {
            return null;
        }

        DnsDelegation result = new DnsDelegation();
        result.Zone = RecordSetOperations.CopyName(zone, false);
        result.Secure = lookup.Entry.SecurityStatus ==
            DnsSecurityStatus.Secure;
        result.InBailiwick = true;
        result.Depth = zone.Labels.Count;
        result.AuthorityRecordSets.Add(CopyNameServerSet(nameServerSet));
        AddCachedDelegationSigners(result, cache, now);
        int index = 0;
        while (index < names.Count)
        {
            DnsName nameServer = names[index];
            result.NameServers.Add(RecordSetOperations.CopyName(
                nameServer,
                false));
            DnsAddressResult? addresses = ReadCachedAddresses(
                nameServer,
                cache,
                now);
            if (addresses != null)
            {
                result.GlueAddresses.Add(addresses);
            }

            index = index + 1;
        }

        return result;
    }

    static void AddCachedDelegationSigners(
        DnsDelegation delegation,
        IDnsCache cache,
        DnsClockValue now)
    {
        DnsResolveRequest request = CreateCacheRequest(
            delegation.Zone,
            DnsRecordType.Ds,
            (int)DnsRecordType.Ds);
        DnsCacheLookup lookup = cache.Lookup(request, now);
        if (!lookup.Found || !lookup.Fresh || lookup.Entry == null ||
            lookup.Entry.Kind != DnsCacheEntryKind.Positive ||
            lookup.Entry.SecurityStatus != DnsSecurityStatus.Secure)
        {
            return;
        }

        int index = 0;
        while (index < lookup.Entry.RecordSets.Count)
        {
            DnsResourceRecordSet set = lookup.Entry.RecordSets[index];
            if (set != null && EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType) == (int)DnsRecordType.Ds &&
                RecordSetOperations.NamesEqual(
                    set.Name,
                    delegation.Zone))
            {
                delegation.AuthorityRecordSets.Add(CopyDelegationSignerSet(
                    set));
            }

            index = index + 1;
        }
    }

    static DnsResourceRecordSet? FindNameServerSet(
        List<DnsResourceRecordSet> sets,
        DnsName zone)
    {
        DnsResourceRecordSet? result = null;
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            if (set != null &&
                EffectiveValue(set.RecordTypeValue, (int)set.RecordType) == 2 &&
                EffectiveValue(set.ClassValue, (int)set.Class) == 1 &&
                RecordSetOperations.NamesEqual(set.Name, zone))
            {
                if (result != null)
                {
                    return null;
                }

                result = set;
            }

            index = index + 1;
        }

        return result;
    }

    static List<DnsName>? TryReadNameServers(DnsResourceRecordSet set)
    {
        if (set.Records == null || set.Records.Count == 0)
        {
            return null;
        }

        List<DnsName> result = new List<DnsName>();
        int index = 0;
        while (index < set.Records.Count)
        {
            DnsResourceRecord record = set.Records[index];
            if (record == null || record.Data == null ||
                record.Data.Name == null || record.Data.Name.Name == null ||
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) != 2 ||
                EffectiveValue(record.ClassValue, (int)record.Class) != 1 ||
                !RecordSetOperations.NamesEqual(record.Name, set.Name))
            {
                return null;
            }

            RawDnsNameResult target = RawDnsName.ValidateName(
                record.Data.Name.Name);
            if (!target.Success || target.Name == null ||
                !target.Name.IsAbsolute || target.Name.Labels.Count == 0 ||
                ContainsName(result, target.Name))
            {
                return null;
            }

            result.Add(RecordSetOperations.CopyName(target.Name, false));
            index = index + 1;
        }

        return result;
    }

    internal static DnsAddressResult? ReadCachedAddresses(
        DnsName nameServer,
        IDnsCache cache,
        DnsClockValue now)
    {
        DnsAddressResult result = new DnsAddressResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = RecordSetOperations.CopyName(
            nameServer,
            false);
        result.SecurityStatus = DnsSecurityStatus.Secure;
        bool found = ReadAddressFamily(
            result,
            cache,
            now,
            nameServer,
            DnsRecordType.A,
            1,
            4,
            4);
        bool foundIpv6 = ReadAddressFamily(
            result,
            cache,
            now,
            nameServer,
            DnsRecordType.Aaaa,
            28,
            6,
            16);
        if (!found && !foundIpv6)
        {
            return null;
        }

        return result;
    }

    static bool ReadAddressFamily(
        DnsAddressResult destination,
        IDnsCache cache,
        DnsClockValue now,
        DnsName nameServer,
        DnsRecordType recordType,
        int recordTypeValue,
        int addressFamily,
        int width)
    {
        DnsResolveRequest request = CreateCacheRequest(
            nameServer,
            recordType,
            recordTypeValue);
        DnsCacheLookup lookup = cache.Lookup(request, now);
        if (!lookup.Found || !lookup.Fresh || lookup.Entry == null ||
            lookup.Entry.Kind != DnsCacheEntryKind.Positive ||
            lookup.Entry.RecordSets == null)
        {
            return false;
        }

        int initialCount = destination.Addresses.Count;
        int setIndex = 0;
        while (setIndex < lookup.Entry.RecordSets.Count)
        {
            DnsResourceRecordSet set = lookup.Entry.RecordSets[setIndex];
            if (set != null &&
                EffectiveValue(set.RecordTypeValue, (int)set.RecordType) ==
                    recordTypeValue &&
                EffectiveValue(set.ClassValue, (int)set.Class) == 1 &&
                RecordSetOperations.NamesEqual(set.Name, nameServer) &&
                set.Records != null)
            {
                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    if (record == null || record.Data == null ||
                        record.Data.Address == null ||
                        record.Data.Address.Address == null ||
                        record.Data.Address.Address.Length != width ||
                        EffectiveValue(
                            record.RecordTypeValue,
                            (int)record.RecordType) != recordTypeValue ||
                        EffectiveValue(
                            record.ClassValue,
                            (int)record.Class) != 1)
                    {
                        return false;
                    }

                    AddDistinctAddress(
                        destination,
                        record.Data.Address.Address,
                        addressFamily);
                    recordIndex = recordIndex + 1;
                }

                MergeTimeToLive(destination, set.TimeToLive);
            }

            setIndex = setIndex + 1;
        }

        if (destination.Addresses.Count > initialCount)
        {
            destination.SecurityStatus = WorseSecurityStatus(
                destination.SecurityStatus,
                lookup.Entry.SecurityStatus);
            return true;
        }

        return false;
    }

    static void AddDistinctAddress(
        DnsAddressResult destination,
        byte[] address,
        int addressFamily)
    {
        int index = 0;
        while (index < destination.Addresses.Count)
        {
            if (destination.AddressFamilies[index] == addressFamily &&
                BytesEqual(destination.Addresses[index], address))
            {
                return;
            }

            index = index + 1;
        }

        destination.Addresses.Add(RecordSetOperations.CopyBytes(address));
        destination.AddressFamilies.Add(addressFamily);
    }

    static void MergeTimeToLive(
        DnsAddressResult destination,
        uint timeToLive)
    {
        if (!destination.HasTimeToLive ||
            timeToLive < destination.TimeToLive)
        {
            destination.HasTimeToLive = true;
            destination.TimeToLive = timeToLive;
        }
    }

    static DnsSecurityStatus WorseSecurityStatus(
        DnsSecurityStatus left,
        DnsSecurityStatus right)
    {
        if (SecurityRank(right) > SecurityRank(left))
        {
            return right;
        }

        return left;
    }

    static int SecurityRank(DnsSecurityStatus status)
    {
        if (status == DnsSecurityStatus.Bogus)
        {
            return 5;
        }

        if (status == DnsSecurityStatus.UnsupportedAlgorithm)
        {
            return 4;
        }

        if (status == DnsSecurityStatus.Indeterminate)
        {
            return 3;
        }

        if (status == DnsSecurityStatus.Insecure)
        {
            return 2;
        }

        if (status == DnsSecurityStatus.Secure)
        {
            return 1;
        }

        return 3;
    }

    static DnsResourceRecordSet CopyNameServerSet(
        DnsResourceRecordSet source)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = DnsRecordType.Ns;
        result.RecordTypeValue = 2;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = source.TimeToLive;
        int index = 0;
        while (index < source.Records.Count)
        {
            DnsResourceRecord copy = RecordSetOperations.CopyRecord(
                source.Records[index],
                result.Name);
            copy.RecordType = DnsRecordType.Ns;
            copy.RecordTypeValue = 2;
            copy.Class = DnsClass.Internet;
            copy.ClassValue = 1;
            result.Records.Add(copy);
            index = index + 1;
        }

        return result;
    }

    static DnsResourceRecordSet CopyDelegationSignerSet(
        DnsResourceRecordSet source)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = DnsRecordType.Ds;
        result.RecordTypeValue = (int)DnsRecordType.Ds;
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

    static DnsDelegation CopyRootDelegation(DnsDelegation source)
    {
        DnsDelegation result = new DnsDelegation();
        result.Zone = RecordSetOperations.CopyName(source.Zone, false);
        result.Secure = source.Secure;
        result.InBailiwick = source.InBailiwick;
        result.Lame = source.Lame;
        result.Depth = source.Depth;
        int index = 0;
        while (index < source.NameServers.Count)
        {
            result.NameServers.Add(RecordSetOperations.CopyName(
                source.NameServers[index],
                false));
            result.GlueAddresses.Add(CopyAddressResult(
                source.GlueAddresses[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.AuthorityRecordSets.Count)
        {
            result.AuthorityRecordSets.Add(CopyNameServerSet(
                source.AuthorityRecordSets[index]));
            index = index + 1;
        }

        return result;
    }

    static DnsAddressResult CopyAddressResult(DnsAddressResult source)
    {
        DnsName? canonicalName = source.CanonicalName;
        if (canonicalName == null)
        {
            throw new DnsResolutionException(
                "A delegation address result requires a canonical name.");
        }

        DnsAddressResult result = new DnsAddressResult();
        result.Status = source.Status;
        result.CanonicalName = RecordSetOperations.CopyName(
            canonicalName,
            false);
        result.SecurityStatus = source.SecurityStatus;
        result.HasTimeToLive = source.HasTimeToLive;
        result.TimeToLive = source.TimeToLive;
        int index = 0;
        while (index < source.Addresses.Count)
        {
            result.Addresses.Add(RecordSetOperations.CopyBytes(
                source.Addresses[index]));
            result.AddressFamilies.Add(source.AddressFamilies[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsResolveRequest CreateCacheRequest(
        DnsName name,
        DnsRecordType recordType,
        int recordTypeValue)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = RecordSetOperations.CopyName(name, false);
        result.RecordType = recordType;
        result.RecordTypeValue = recordTypeValue;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.Options.UseCache = true;
        result.Options.AllowStale = false;
        return result;
    }

    static DnsName CreateAncestor(DnsName name, int labelIndex)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        int index = labelIndex;
        while (index < name.Labels.Count)
        {
            result.Labels.Add(RecordSetOperations.CopyBytes(
                name.Labels[index]));
            index = index + 1;
        }

        return result;
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
