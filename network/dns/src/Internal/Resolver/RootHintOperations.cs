using System.Collections.Generic;

namespace dns;

internal static class RootHintOperations
{
    const int RootHintCredibility = 4;
    const int MaximumRootHintNodeCount = 1024;

    internal static DnsRootHints LoadRootHints(DnsZone zone, string source)
    {
        if (zone == null || source == null || zone.Apex == null ||
            zone.Nodes == null)
        {
            throw new DnsResolutionException(
                "A root zone and source description are required.");
        }

        RawDnsNameResult apex = RawDnsName.ValidateName(zone.Apex);
        int classValue = EffectiveValue(zone.ClassValue, (int)zone.Class);
        if (!apex.Success || apex.Name == null || !apex.Name.IsAbsolute ||
            apex.Name.Labels.Count != 0 || classValue != 1)
        {
            throw new DnsResolutionException(
                "Root hints require the absolute root name in the Internet class.");
        }

        List<DnsZoneNode> nodes = FlattenNodes(zone.Nodes);
        DnsResourceRecordSet? nameServerSet = FindRootNameServerSet(nodes);
        if (nameServerSet == null)
        {
            throw new DnsResolutionException(
                "Root hints require one root NS record set.");
        }

        List<DnsName> nameServers = ReadNameServers(nameServerSet);
        DnsRootHints hints = new DnsRootHints();
        hints.Source = source;
        hints.LoadedAt = TransportTime.ReadClock();
        hints.Root.Zone = CreateRootName();
        hints.Root.InBailiwick = true;
        hints.Root.Depth = 0;
        hints.Root.AuthorityRecordSets.Add(CopyRecordSet(
            nameServerSet,
            2,
            1));
        int index = 0;
        while (index < nameServers.Count)
        {
            DnsName nameServer = nameServers[index];
            hints.Root.NameServers.Add(RecordSetOperations.CopyName(
                nameServer,
                false));
            hints.Root.GlueAddresses.Add(ReadGlueAddresses(
                nodes,
                nameServer));
            index = index + 1;
        }

        return ValidateRootHints(hints);
    }

    internal static DnsRootHints ValidateRootHints(DnsRootHints rootHints)
    {
        if (rootHints == null || rootHints.Root == null ||
            rootHints.LoadedAt == null || rootHints.Root.Zone == null ||
            rootHints.Root.NameServers == null ||
            rootHints.Root.GlueAddresses == null ||
            rootHints.Root.AuthorityRecordSets == null)
        {
            throw new DnsResolutionException(
                "Complete root hints are required.");
        }

        RawDnsNameResult zone = RawDnsName.ValidateName(rootHints.Root.Zone);
        if (!zone.Success || zone.Name == null || !zone.Name.IsAbsolute ||
            zone.Name.Labels.Count != 0)
        {
            throw new DnsResolutionException(
                "The root-hint delegation zone must be the absolute root.");
        }

        ValidateNameServerList(rootHints.Root.NameServers);
        DnsResourceRecordSet authority = FindValidatedAuthoritySet(
            rootHints.Root.AuthorityRecordSets);
        List<DnsName> authorityNames = ReadNameServers(authority);
        if (!NameListsEqual(
            rootHints.Root.NameServers,
            authorityNames))
        {
            throw new DnsResolutionException(
                "Root NS authority data must match the listed name servers.");
        }

        ValidateGlueList(
            rootHints.Root.NameServers,
            rootHints.Root.GlueAddresses);
        DnsRootHints result = new DnsRootHints();
        result.Source = rootHints.Source;
        result.LoadedAt = CopyLoadedClock(rootHints.LoadedAt);
        if (result.LoadedAt.UnixTimeMilliseconds == 0 &&
            result.LoadedAt.MonotonicMilliseconds == 0)
        {
            result.LoadedAt = TransportTime.ReadClock();
        }

        result.Root.Zone = CreateRootName();
        result.Root.Secure = rootHints.Root.Secure;
        result.Root.InBailiwick = true;
        result.Root.Lame = false;
        result.Root.Depth = 0;
        result.Root.AuthorityRecordSets.Add(CopyRecordSet(
            authority,
            2,
            1));
        int index = 0;
        while (index < rootHints.Root.NameServers.Count)
        {
            DnsAddressResult glue = FindGlueForName(
                rootHints.Root.GlueAddresses,
                rootHints.Root.NameServers[index]);
            result.Root.NameServers.Add(RecordSetOperations.CopyName(
                rootHints.Root.NameServers[index],
                false));
            result.Root.GlueAddresses.Add(CopyGlueResult(
                glue));
            index = index + 1;
        }

        result.Valid = true;
        return result;
    }

    internal static void SeedCache(
        DnsRootHints rootHints,
        IDnsCache cache)
    {
        if (cache == null)
        {
            throw new DnsResolutionException(
                "A resolver cache is required for root-hint seeding.");
        }

        DnsRootHints validated = ValidateRootHints(rootHints);
        DnsResourceRecordSet nameServers =
            validated.Root.AuthorityRecordSets[0];
        StoreRecordSet(
            cache,
            nameServers,
            validated.Root.Secure ?
                DnsSecurityStatus.Secure : DnsSecurityStatus.Insecure,
            validated.LoadedAt);
        int index = 0;
        while (index < validated.Root.GlueAddresses.Count)
        {
            StoreGlue(cache, validated.Root.GlueAddresses[index],
                validated.LoadedAt);
            index = index + 1;
        }
    }

    static List<DnsZoneNode> FlattenNodes(List<DnsZoneNode> roots)
    {
        List<DnsZoneNode> result = new List<DnsZoneNode>();
        int index = 0;
        while (index < roots.Count)
        {
            DnsZoneNode node = roots[index];
            if (node == null)
            {
                throw new DnsResolutionException(
                    "Root-hint zones cannot contain null nodes.");
            }

            if (ContainsNodeReference(result, node))
            {
                throw new DnsResolutionException(
                    "Root-hint zones cannot repeat or cycle zone nodes.");
            }

            result.Add(node);
            if (result.Count > MaximumRootHintNodeCount)
            {
                throw new DnsResolutionException(
                    "The root-hint zone contains too many nodes.");
            }
            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            DnsZoneNode node = result[index];
            if (node.Children == null || node.RecordSets == null)
            {
                throw new DnsResolutionException(
                    "Root-hint zone nodes must own their collections.");
            }

            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                DnsZoneNode child = node.Children[childIndex];
                if (child == null)
                {
                    throw new DnsResolutionException(
                        "Root-hint zones cannot contain null child nodes.");
                }

                if (ContainsNodeReference(result, child))
                {
                    throw new DnsResolutionException(
                        "Root-hint zones cannot repeat or cycle zone nodes.");
                }

                result.Add(child);
                if (result.Count > MaximumRootHintNodeCount)
                {
                    throw new DnsResolutionException(
                        "The root-hint zone contains too many nodes.");
                }
                childIndex = childIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    static bool ContainsNodeReference(
        List<DnsZoneNode> nodes,
        DnsZoneNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (System.Object.ReferenceEquals(nodes[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsResourceRecordSet? FindRootNameServerSet(
        List<DnsZoneNode> nodes)
    {
        DnsResourceRecordSet? result = null;
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            DnsZoneNode node = nodes[nodeIndex];
            int setIndex = 0;
            while (setIndex < node.RecordSets.Count)
            {
                DnsResourceRecordSet set = node.RecordSets[setIndex];
                if (set == null)
                {
                    throw new DnsResolutionException(
                        "Root-hint zones cannot contain null record sets.");
                }

                int typeValue = EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType);
                int classValue = EffectiveValue(
                    set.ClassValue,
                    (int)set.Class);
                if (typeValue == 2 && classValue == 1 &&
                    IsRootName(set.Name))
                {
                    if (result != null)
                    {
                        throw new DnsResolutionException(
                            "Root hints cannot contain multiple root NS record sets.");
                    }

                    result = set;
                }

                setIndex = setIndex + 1;
            }

            nodeIndex = nodeIndex + 1;
        }

        return result;
    }

    static List<DnsName> ReadNameServers(DnsResourceRecordSet set)
    {
        if (set == null || set.Records == null || set.Records.Count == 0 ||
            !IsRootName(set.Name) ||
            EffectiveValue(set.RecordTypeValue, (int)set.RecordType) != 2 ||
            EffectiveValue(set.ClassValue, (int)set.Class) != 1)
        {
            throw new DnsResolutionException(
                "A complete Internet-class root NS record set is required.");
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
                !IsRootName(record.Name))
            {
                throw new DnsResolutionException(
                    "Each root NS record must contain one typed name.");
            }

            DnsName target = record.Data.Name.Name;
            RawDnsNameResult validated = RawDnsName.ValidateName(target);
            if (!validated.Success || validated.Name == null ||
                !validated.Name.IsAbsolute ||
                validated.Name.Labels.Count == 0 ||
                ContainsName(result, validated.Name))
            {
                throw new DnsResolutionException(
                    "Root name-server names must be distinct absolute non-root names.");
            }

            result.Add(RecordSetOperations.CopyName(validated.Name, false));
            index = index + 1;
        }

        return result;
    }

    static DnsAddressResult ReadGlueAddresses(
        List<DnsZoneNode> nodes,
        DnsName nameServer)
    {
        DnsAddressResult result = new DnsAddressResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = RecordSetOperations.CopyName(nameServer, false);
        result.SecurityStatus = DnsSecurityStatus.Insecure;
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            DnsZoneNode node = nodes[nodeIndex];
            int setIndex = 0;
            while (setIndex < node.RecordSets.Count)
            {
                DnsResourceRecordSet set = node.RecordSets[setIndex];
                int typeValue = EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType);
                if ((typeValue == 1 || typeValue == 28) &&
                    EffectiveValue(set.ClassValue, (int)set.Class) == 1 &&
                    RecordSetOperations.NamesEqual(set.Name, nameServer))
                {
                    AddGlueSet(result, set, typeValue);
                }

                setIndex = setIndex + 1;
            }

            nodeIndex = nodeIndex + 1;
        }

        if (result.Addresses.Count == 0)
        {
            throw new DnsResolutionException(
                "Every root name server requires IPv4 or IPv6 glue.");
        }

        return result;
    }

    static void AddGlueSet(
        DnsAddressResult destination,
        DnsResourceRecordSet set,
        int typeValue)
    {
        if (set.Records == null || set.Records.Count == 0)
        {
            throw new DnsResolutionException(
                "Root glue record sets cannot be empty.");
        }

        int addressFamily = typeValue == 1 ? 4 : 6;
        int width = typeValue == 1 ? 4 : 16;
        int index = 0;
        while (index < set.Records.Count)
        {
            DnsResourceRecord record = set.Records[index];
            if (record == null || record.Data == null ||
                record.Data.Address == null ||
                record.Data.Address.Address == null ||
                record.Data.Address.Address.Length != width ||
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) != typeValue ||
                EffectiveValue(record.ClassValue, (int)record.Class) != 1 ||
                !RecordSetOperations.NamesEqual(record.Name, set.Name))
            {
                throw new DnsResolutionException(
                    "Root glue records must match their typed address set.");
            }

            AddDistinctAddress(
                destination,
                record.Data.Address.Address,
                addressFamily);
            MergeAddressTimeToLive(destination, record.TimeToLive);
            index = index + 1;
        }

        MergeAddressTimeToLive(destination, set.TimeToLive);
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

    static void MergeAddressTimeToLive(
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

    static void ValidateNameServerList(List<DnsName> nameServers)
    {
        if (nameServers.Count == 0)
        {
            throw new DnsResolutionException(
                "Root hints require at least one name server.");
        }

        int index = 0;
        while (index < nameServers.Count)
        {
            DnsName name = nameServers[index];
            RawDnsNameResult validated = RawDnsName.ValidateName(name);
            if (!validated.Success || validated.Name == null ||
                !validated.Name.IsAbsolute ||
                validated.Name.Labels.Count == 0)
            {
                throw new DnsResolutionException(
                    "Root name-server names must be absolute and non-root.");
            }

            int other = 0;
            while (other < index)
            {
                if (RecordSetOperations.NamesEqual(
                    nameServers[other],
                    validated.Name))
                {
                    throw new DnsResolutionException(
                        "Root name-server names must be distinct.");
                }

                other = other + 1;
            }

            index = index + 1;
        }
    }

    static DnsResourceRecordSet FindValidatedAuthoritySet(
        List<DnsResourceRecordSet> sets)
    {
        DnsResourceRecordSet? result = null;
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            if (set == null)
            {
                throw new DnsResolutionException(
                    "Root authority data cannot contain null record sets.");
            }

            int typeValue = EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue == 2 &&
                EffectiveValue(set.ClassValue, (int)set.Class) == 1 &&
                IsRootName(set.Name))
            {
                if (result != null)
                {
                    throw new DnsResolutionException(
                        "Root hints require exactly one root NS authority set.");
                }

                result = set;
            }

            index = index + 1;
        }

        if (result == null)
        {
            throw new DnsResolutionException(
                "Root hints require a root NS authority set.");
        }

        return result;
    }

    static bool NameListsEqual(List<DnsName> left, List<DnsName> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            if (!ContainsName(right, left[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void ValidateGlueList(
        List<DnsName> nameServers,
        List<DnsAddressResult> glue)
    {
        if (glue.Count != nameServers.Count)
        {
            throw new DnsResolutionException(
                "Root hints require one glue result for each name server.");
        }

        int index = 0;
        while (index < glue.Count)
        {
            DnsAddressResult addresses = glue[index];
            if (addresses == null || addresses.CanonicalName == null ||
                addresses.Addresses == null ||
                addresses.AddressFamilies == null ||
                addresses.Status != DnsResolveStatus.Success ||
                addresses.Addresses.Count == 0 ||
                addresses.Addresses.Count != addresses.AddressFamilies.Count ||
                !ContainsName(nameServers, addresses.CanonicalName))
            {
                throw new DnsResolutionException(
                    "Root glue must align with each listed name server.");
            }

            int addressIndex = 0;
            while (addressIndex < addresses.Addresses.Count)
            {
                byte[] address = addresses.Addresses[addressIndex];
                int family = addresses.AddressFamilies[addressIndex];
                if (address == null ||
                    (family == 4 && address.Length != 4) ||
                    (family == 6 && address.Length != 16) ||
                    (family != 4 && family != 6))
                {
                    throw new DnsResolutionException(
                        "Root glue contains an invalid address family or width.");
                }

                addressIndex = addressIndex + 1;
            }

            int other = 0;
            while (other < index)
            {
                DnsName? otherName = glue[other].CanonicalName;
                if (otherName != null && RecordSetOperations.NamesEqual(
                    otherName,
                    addresses.CanonicalName))
                {
                    throw new DnsResolutionException(
                        "Root hints cannot repeat glue for one name server.");
                }

                other = other + 1;
            }

            index = index + 1;
        }
    }

    static DnsAddressResult FindGlueForName(
        List<DnsAddressResult> glue,
        DnsName nameServer)
    {
        int index = 0;
        while (index < glue.Count)
        {
            DnsName? canonicalName = glue[index].CanonicalName;
            if (canonicalName != null && RecordSetOperations.NamesEqual(
                canonicalName,
                nameServer))
            {
                return glue[index];
            }

            index = index + 1;
        }

        throw new DnsResolutionException(
            "Every root name server requires matching glue.");
    }

    static DnsAddressResult CopyGlueResult(DnsAddressResult source)
    {
        DnsName? canonicalName = source.CanonicalName;
        if (canonicalName == null)
        {
            throw new DnsResolutionException(
                "Root glue requires a canonical name.");
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

    static DnsResourceRecordSet CopyRecordSet(
        DnsResourceRecordSet source,
        int expectedType,
        int expectedClass)
    {
        if (source == null || source.Records == null ||
            source.Records.Count == 0 ||
            EffectiveValue(
                source.RecordTypeValue,
                (int)source.RecordType) != expectedType ||
            EffectiveValue(source.ClassValue, (int)source.Class) !=
                expectedClass)
        {
            throw new DnsResolutionException(
                "A root-hint record set is incomplete or has the wrong key.");
        }

        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = (DnsRecordType)expectedType;
        result.RecordTypeValue = expectedType;
        result.Class = (DnsClass)expectedClass;
        result.ClassValue = expectedClass;
        result.TimeToLive = source.TimeToLive;
        int index = 0;
        while (index < source.Records.Count)
        {
            DnsResourceRecord record = source.Records[index];
            if (record == null ||
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) != expectedType ||
                EffectiveValue(record.ClassValue, (int)record.Class) !=
                    expectedClass ||
                !RecordSetOperations.NamesEqual(record.Name, source.Name))
            {
                throw new DnsResolutionException(
                    "A root-hint record does not match its record set.");
            }

            DnsResourceRecord copy = RecordSetOperations.CopyRecord(
                record,
                result.Name);
            copy.RecordType = result.RecordType;
            copy.RecordTypeValue = expectedType;
            copy.Class = result.Class;
            copy.ClassValue = expectedClass;
            copy.Data = CopyHintRecordData(record.Data, expectedType);
            result.Records.Add(copy);
            if (record.TimeToLive < result.TimeToLive)
            {
                result.TimeToLive = record.TimeToLive;
            }

            index = index + 1;
        }

        return result;
    }

    static DnsRecordData CopyHintRecordData(
        DnsRecordData? source,
        int recordTypeValue)
    {
        if (source == null)
        {
            throw new DnsResolutionException(
                "Typed root-hint record data are required.");
        }

        DnsRecordData result = new DnsRecordData();
        if (recordTypeValue == 2)
        {
            if (source.Name == null || source.Name.Name == null)
            {
                throw new DnsResolutionException(
                    "A root NS record requires a typed target name.");
            }

            result.Name = new DnsNameData();
            result.Name.Name = RecordSetOperations.CopyName(
                source.Name.Name,
                false);
            return result;
        }

        if (source.Address == null || source.Address.Address == null)
        {
            throw new DnsResolutionException(
                "A root glue record requires a typed address.");
        }

        result.Address = new DnsAddressData();
        result.Address.Address = RecordSetOperations.CopyBytes(
            source.Address.Address);
        return result;
    }

    static void StoreRecordSet(
        IDnsCache cache,
        DnsResourceRecordSet set,
        DnsSecurityStatus securityStatus,
        DnsClockValue loadedAt)
    {
        DnsCacheEntry entry = new DnsCacheEntry();
        entry.Kind = DnsCacheEntryKind.Positive;
        entry.Name = RecordSetOperations.CopyName(set.Name, true);
        entry.RecordType = set.RecordType;
        entry.RecordTypeValue = set.RecordTypeValue;
        entry.Class = set.Class;
        entry.ClassValue = set.ClassValue;
        entry.RecordSets.Add(CopyRecordSet(
            set,
            set.RecordTypeValue,
            set.ClassValue));
        entry.SecurityStatus = securityStatus;
        entry.StoredAt = CopyLoadedClock(loadedAt);
        entry.OriginalTimeToLive = set.TimeToLive;
        entry.ExpiresAt = CreateExpiration(
            entry.StoredAt,
            entry.OriginalTimeToLive);
        entry.Credibility = RootHintCredibility;
        cache.Store(entry);
    }

    static void StoreGlue(
        IDnsCache cache,
        DnsAddressResult glue,
        DnsClockValue loadedAt)
    {
        StoreGlueFamily(cache, glue, loadedAt, 4, DnsRecordType.A, 1);
        StoreGlueFamily(cache, glue, loadedAt, 6, DnsRecordType.Aaaa, 28);
    }

    static void StoreGlueFamily(
        IDnsCache cache,
        DnsAddressResult glue,
        DnsClockValue loadedAt,
        int addressFamily,
        DnsRecordType recordType,
        int recordTypeValue)
    {
        DnsName? canonicalName = glue.CanonicalName;
        if (canonicalName == null)
        {
            throw new DnsResolutionException(
                "Root glue requires a canonical name.");
        }

        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = RecordSetOperations.CopyName(canonicalName, false);
        set.RecordType = recordType;
        set.RecordTypeValue = recordTypeValue;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = glue.HasTimeToLive ? glue.TimeToLive : 0;
        int index = 0;
        while (index < glue.Addresses.Count)
        {
            if (glue.AddressFamilies[index] == addressFamily)
            {
                DnsResourceRecord record = new DnsResourceRecord();
                record.Name = RecordSetOperations.CopyName(
                    canonicalName,
                    false);
                record.RecordType = recordType;
                record.RecordTypeValue = recordTypeValue;
                record.Class = DnsClass.Internet;
                record.ClassValue = 1;
                record.TimeToLive = set.TimeToLive;
                record.Data = new DnsRecordData();
                record.Data.Address = new DnsAddressData();
                record.Data.Address.Address = RecordSetOperations.CopyBytes(
                    glue.Addresses[index]);
                set.Records.Add(record);
            }

            index = index + 1;
        }

        if (set.Records.Count > 0)
        {
            StoreRecordSet(
                cache,
                set,
                glue.SecurityStatus,
                loadedAt);
        }
    }

    static DnsClockValue CreateExpiration(
        DnsClockValue loadedAt,
        uint timeToLive)
    {
        long milliseconds = (long)timeToLive * 1000;
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = AddSaturated(
            loadedAt.UnixTimeMilliseconds,
            milliseconds);
        result.MonotonicMilliseconds = AddSaturated(
            loadedAt.MonotonicMilliseconds,
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

    static bool IsRootName(DnsName name)
    {
        if (name == null)
        {
            return false;
        }

        RawDnsNameResult result = RawDnsName.ValidateName(name);
        return result.Success && result.Name != null &&
            result.Name.IsAbsolute && result.Name.Labels.Count == 0;
    }

    static DnsName CreateRootName()
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        return result;
    }

    static DnsClockValue CopyLoadedClock(DnsClockValue source)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = source.UnixTimeMilliseconds;
        result.MonotonicMilliseconds = source.MonotonicMilliseconds;
        return result;
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
