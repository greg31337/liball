using System.Collections.Generic;

namespace dns;

internal class DnsReversedNameKey
{
    internal List<byte[]> Labels = new List<byte[]>();
}

internal class DnsZoneIndexEntry
{
    internal DnsReversedNameKey Key = new DnsReversedNameKey();
    internal DnsZoneNode Node = new DnsZoneNode();
}

internal class DnsRecordSetIndexEntry
{
    internal DnsReversedNameKey NameKey = new DnsReversedNameKey();
    internal int RecordTypeValue;
    internal int ClassValue;
    internal DnsResourceRecordSet RecordSet = new DnsResourceRecordSet();
}

internal class DnsZoneIndex
{
    internal DnsZone Zone = new DnsZone();
    internal DnsReversedNameKey ApexKey = new DnsReversedNameKey();
    internal int ClassValue;
    internal List<DnsZoneIndexEntry> Nodes =
        new List<DnsZoneIndexEntry>();
    internal List<DnsRecordSetIndexEntry> RecordSets =
        new List<DnsRecordSetIndexEntry>();
}

internal class DnsHostedZoneIndexEntry
{
    internal DnsReversedNameKey ApexKey = new DnsReversedNameKey();
    internal int ClassValue;
    internal DnsZone Zone = new DnsZone();
    internal DnsZoneIndex Index = new DnsZoneIndex();
}

internal class DnsHostedZoneIndex
{
    internal List<DnsHostedZoneIndexEntry> Zones =
        new List<DnsHostedZoneIndexEntry>();
}

internal static class ZoneIndexOperations
{
    internal static DnsZoneIndex BuildZone(DnsZone zone)
    {
        if (zone == null || zone.Nodes == null)
        {
            throw new System.ArgumentNullException("zone");
        }

        RawDnsNameResult apex = RawDnsName.ValidateName(zone.Apex);
        if (!apex.Success || apex.Name == null || !apex.Name.IsAbsolute)
        {
            throw new System.ArgumentException(
                "A zone index requires a valid absolute apex name.");
        }

        int classValue = RecordSetOperations.EffectiveValue(
            zone.ClassValue,
            (int)zone.Class);
        if (classValue < 0 || classValue > 65535)
        {
            throw new System.ArgumentException(
                "A zone index requires a valid DNS class.");
        }

        DnsZoneIndex result = new DnsZoneIndex();
        result.Zone = zone;
        result.ApexKey = CreateKey(apex.Name);
        result.ClassValue = classValue;
        List<DnsZoneNode> visited = new List<DnsZoneNode>();
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            AddNode(result, zone.Nodes[index], visited);
            index = index + 1;
        }

        return result;
    }

    internal static DnsHostedZoneIndex BuildHosted(
        List<DnsZone> zones)
    {
        if (zones == null)
        {
            throw new System.ArgumentNullException("zones");
        }

        DnsHostedZoneIndex result = new DnsHostedZoneIndex();
        int index = 0;
        while (index < zones.Count)
        {
            DnsZoneIndex zoneIndex = BuildZone(zones[index]);
            DnsHostedZoneIndexEntry entry = new DnsHostedZoneIndexEntry();
            entry.ApexKey = zoneIndex.ApexKey;
            entry.ClassValue = zoneIndex.ClassValue;
            entry.Zone = zoneIndex.Zone;
            entry.Index = zoneIndex;
            InsertHostedZone(result.Zones, entry);
            index = index + 1;
        }

        return result;
    }

    internal static DnsZoneNode? FindExact(
        DnsZoneIndex index,
        DnsName name)
    {
        DnsZoneIndexEntry? entry = FindNodeEntry(index, name);
        if (entry == null)
        {
            return null;
        }

        return entry.Node;
    }

    internal static DnsZoneNode? FindAncestor(
        DnsZoneIndex index,
        DnsName name)
    {
        ValidateLookup(index, name);
        if (!NameBelongsToZone(name, index.Zone.Apex))
        {
            return null;
        }

        DnsName current = RecordSetOperations.CopyName(name, true);
        while (current.Labels.Count > 0)
        {
            current = ParentName(current);
            DnsZoneIndexEntry? entry = FindNodeEntry(index, current);
            if (entry != null)
            {
                return entry.Node;
            }

            if (RecordSetOperations.NamesEqual(current, index.Zone.Apex))
            {
                return null;
            }
        }

        return null;
    }

    internal static DnsZoneNode? FindClosestEncloser(
        DnsZoneIndex index,
        DnsName name)
    {
        DnsZoneNode? exact = FindExact(index, name);
        if (exact != null)
        {
            return exact;
        }

        return FindAncestor(index, name);
    }

    internal static DnsZoneNode? FindDelegation(
        DnsZoneIndex index,
        DnsName name)
    {
        ValidateLookup(index, name);
        if (!NameBelongsToZone(name, index.Zone.Apex))
        {
            return null;
        }

        DnsName current = RecordSetOperations.CopyName(name, true);
        while (current.Labels.Count >= index.Zone.Apex.Labels.Count)
        {
            DnsZoneIndexEntry? entry = FindNodeEntry(index, current);
            if (entry != null && entry.Node.IsDelegation)
            {
                return entry.Node;
            }

            if (RecordSetOperations.NamesEqual(current, index.Zone.Apex))
            {
                return null;
            }

            current = ParentName(current);
        }

        return null;
    }

    internal static DnsZoneNode? FindWildcard(
        DnsZoneIndex index,
        DnsName name)
    {
        if (FindExact(index, name) != null)
        {
            return null;
        }

        DnsZoneNode? closest = FindClosestEncloser(index, name);
        if (closest == null)
        {
            return null;
        }

        DnsName wildcard = new DnsName();
        wildcard.IsAbsolute = true;
        wildcard.Labels.Add([42]);
        int labelIndex = 0;
        while (labelIndex < closest.Name.Labels.Count)
        {
            wildcard.Labels.Add(RecordSetOperations.CopyBytes(
                closest.Name.Labels[labelIndex]));
            labelIndex = labelIndex + 1;
        }

        return FindExact(index, wildcard);
    }

    internal static DnsZoneNode? FindPredecessor(
        DnsZoneIndex index,
        DnsName name)
    {
        ValidateLookup(index, name);
        if (index.Nodes.Count == 0)
        {
            return null;
        }

        DnsReversedNameKey key = CreateKey(name);
        int position = LowerBound(index.Nodes, key);
        position = position - 1;
        if (position < 0)
        {
            position = index.Nodes.Count - 1;
        }

        return index.Nodes[position].Node;
    }

    internal static DnsZoneNode? FindSuccessor(
        DnsZoneIndex index,
        DnsName name)
    {
        ValidateLookup(index, name);
        if (index.Nodes.Count == 0)
        {
            return null;
        }

        DnsReversedNameKey key = CreateKey(name);
        int position = LowerBound(index.Nodes, key);
        if (position < index.Nodes.Count &&
            CompareKeys(index.Nodes[position].Key, key) == 0)
        {
            position = position + 1;
        }

        if (position >= index.Nodes.Count)
        {
            position = 0;
        }

        return index.Nodes[position].Node;
    }

    internal static DnsResourceRecordSet? FindRecordSet(
        DnsZoneIndex index,
        DnsName name,
        DnsRecordType recordType,
        int recordTypeValue,
        DnsClass recordClass,
        int classValue)
    {
        ValidateLookup(index, name);
        int effectiveType = RecordSetOperations.EffectiveValue(
            recordTypeValue,
            (int)recordType);
        int effectiveClass = RecordSetOperations.EffectiveValue(
            classValue,
            (int)recordClass);
        if (effectiveType < 0 || effectiveType > 65535 ||
            effectiveClass < 0 || effectiveClass > 65535)
        {
            throw new System.ArgumentException(
                "An RRset index lookup requires valid type and class values.");
        }

        DnsRecordSetIndexEntry lookup = new DnsRecordSetIndexEntry();
        lookup.NameKey = CreateKey(name);
        lookup.RecordTypeValue = effectiveType;
        lookup.ClassValue = effectiveClass;
        int low = 0;
        int high = index.RecordSets.Count;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            int comparison = CompareRecordSetEntries(
                index.RecordSets[middle],
                lookup);
            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        if (low < index.RecordSets.Count &&
            CompareRecordSetEntries(index.RecordSets[low], lookup) == 0)
        {
            return index.RecordSets[low].RecordSet;
        }

        return null;
    }

    internal static DnsZone? FindLongestZone(
        DnsHostedZoneIndex index,
        DnsName name,
        DnsClass recordClass,
        int classValue)
    {
        if (index == null)
        {
            throw new System.ArgumentNullException("index");
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(name);
        int effectiveClass = RecordSetOperations.EffectiveValue(
            classValue,
            (int)recordClass);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute || effectiveClass < 0 ||
            effectiveClass > 65535)
        {
            throw new System.ArgumentException(
                "A hosted-zone lookup requires a valid absolute name and class.");
        }

        DnsZone? result = null;
        int resultLabels = -1;
        int zoneIndex = 0;
        while (zoneIndex < index.Zones.Count)
        {
            DnsHostedZoneIndexEntry candidate = index.Zones[zoneIndex];
            if (candidate.ClassValue == effectiveClass &&
                candidate.Zone.Apex.Labels.Count > resultLabels &&
                NameBelongsToZone(validated.Name, candidate.Zone.Apex))
            {
                result = candidate.Zone;
                resultLabels = candidate.Zone.Apex.Labels.Count;
            }

            zoneIndex = zoneIndex + 1;
        }

        return result;
    }

    static void AddNode(
        DnsZoneIndex index,
        DnsZoneNode node,
        List<DnsZoneNode> visited)
    {
        if (node == null)
        {
            throw new System.ArgumentException(
                "A zone index cannot contain a null node.");
        }

        int visitedIndex = 0;
        while (visitedIndex < visited.Count)
        {
            if (object.ReferenceEquals(visited[visitedIndex], node))
            {
                return;
            }

            visitedIndex = visitedIndex + 1;
        }

        if (node.RecordSets == null || node.Children == null)
        {
            throw new System.ArgumentException(
                "Every indexed zone node requires record-set and child collections.");
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(node.Name);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute ||
            !NameBelongsToZone(validated.Name, index.Zone.Apex))
        {
            throw new System.ArgumentException(
                "Every indexed node requires an in-zone absolute owner name.");
        }

        visited.Add(node);
        DnsZoneIndexEntry entry = new DnsZoneIndexEntry();
        entry.Key = CreateKey(validated.Name);
        entry.Node = node;
        InsertNode(index.Nodes, entry);
        int setIndex = 0;
        while (setIndex < node.RecordSets.Count)
        {
            AddRecordSet(index, node, node.RecordSets[setIndex]);
            setIndex = setIndex + 1;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            AddNode(index, node.Children[childIndex], visited);
            childIndex = childIndex + 1;
        }
    }

    static void AddRecordSet(
        DnsZoneIndex index,
        DnsZoneNode node,
        DnsResourceRecordSet recordSet)
    {
        if (recordSet == null ||
            !RecordSetOperations.NamesEqual(recordSet.Name, node.Name))
        {
            throw new System.ArgumentException(
                "An indexed RRset must match its containing node name.");
        }

        int typeValue = RecordSetOperations.EffectiveValue(
            recordSet.RecordTypeValue,
            (int)recordSet.RecordType);
        int classValue = RecordSetOperations.EffectiveValue(
            recordSet.ClassValue,
            (int)recordSet.Class);
        if (typeValue < 0 || typeValue > 65535 ||
            classValue != index.ClassValue)
        {
            throw new System.ArgumentException(
                "An indexed RRset requires the zone class and a valid type.");
        }

        DnsRecordSetIndexEntry entry = new DnsRecordSetIndexEntry();
        entry.NameKey = CreateKey(recordSet.Name);
        entry.RecordTypeValue = typeValue;
        entry.ClassValue = classValue;
        entry.RecordSet = recordSet;
        InsertRecordSet(index.RecordSets, entry);
    }

    static void InsertNode(
        List<DnsZoneIndexEntry> entries,
        DnsZoneIndexEntry entry)
    {
        int position = LowerBound(entries, entry.Key);
        if (position < entries.Count &&
            CompareKeys(entries[position].Key, entry.Key) == 0)
        {
            throw new System.ArgumentException(
                "A zone index cannot contain duplicate owner nodes.");
        }

        entries.Insert(position, entry);
    }

    static void InsertRecordSet(
        List<DnsRecordSetIndexEntry> entries,
        DnsRecordSetIndexEntry entry)
    {
        int position = 0;
        while (position < entries.Count &&
            CompareRecordSetEntries(entries[position], entry) < 0)
        {
            position = position + 1;
        }

        if (position < entries.Count &&
            CompareRecordSetEntries(entries[position], entry) == 0)
        {
            throw new System.ArgumentException(
                "A zone index cannot contain duplicate RRset keys.");
        }

        entries.Insert(position, entry);
    }

    static void InsertHostedZone(
        List<DnsHostedZoneIndexEntry> entries,
        DnsHostedZoneIndexEntry entry)
    {
        int position = 0;
        while (position < entries.Count &&
            CompareHostedZones(entries[position], entry) < 0)
        {
            position = position + 1;
        }

        if (position < entries.Count &&
            CompareHostedZones(entries[position], entry) == 0)
        {
            throw new System.ArgumentException(
                "A hosted-zone index cannot contain duplicate apex and class keys.");
        }

        entries.Insert(position, entry);
    }

    static DnsZoneIndexEntry? FindNodeEntry(
        DnsZoneIndex index,
        DnsName name)
    {
        ValidateLookup(index, name);
        DnsReversedNameKey key = CreateKey(name);
        int position = LowerBound(index.Nodes, key);
        if (position < index.Nodes.Count &&
            CompareKeys(index.Nodes[position].Key, key) == 0)
        {
            return index.Nodes[position];
        }

        return null;
    }

    static int LowerBound(
        List<DnsZoneIndexEntry> entries,
        DnsReversedNameKey key)
    {
        int low = 0;
        int high = entries.Count;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (CompareKeys(entries[middle].Key, key) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    static DnsReversedNameKey CreateKey(DnsName name)
    {
        RawDnsNameResult validated = RawDnsName.ValidateName(name);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            throw new System.ArgumentException(
                "A reversed-label key requires a valid absolute DNS name.");
        }

        DnsReversedNameKey result = new DnsReversedNameKey();
        int labelIndex = validated.Name.Labels.Count - 1;
        while (labelIndex >= 0)
        {
            byte[] source = validated.Name.Labels[labelIndex];
            byte[] label = RecordSetOperations.CopyBytes(source);
            int byteIndex = 0;
            while (byteIndex < label.Length)
            {
                if (label[byteIndex] >= 65 && label[byteIndex] <= 90)
                {
                    label[byteIndex] = (byte)(label[byteIndex] + 32);
                }

                byteIndex = byteIndex + 1;
            }

            result.Labels.Add(label);
            labelIndex = labelIndex - 1;
        }

        return result;
    }

    static int CompareKeys(
        DnsReversedNameKey left,
        DnsReversedNameKey right)
    {
        int index = 0;
        while (index < left.Labels.Count &&
            index < right.Labels.Count)
        {
            int comparison = RecordSetOperations.CompareBytes(
                left.Labels[index],
                right.Labels[index]);
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        if (left.Labels.Count == right.Labels.Count)
        {
            return 0;
        }

        return left.Labels.Count < right.Labels.Count ? -1 : 1;
    }

    static int CompareRecordSetEntries(
        DnsRecordSetIndexEntry left,
        DnsRecordSetIndexEntry right)
    {
        int comparison = CompareKeys(left.NameKey, right.NameKey);
        if (comparison != 0)
        {
            return comparison;
        }

        if (left.RecordTypeValue != right.RecordTypeValue)
        {
            return left.RecordTypeValue < right.RecordTypeValue ? -1 : 1;
        }

        if (left.ClassValue == right.ClassValue)
        {
            return 0;
        }

        return left.ClassValue < right.ClassValue ? -1 : 1;
    }

    static int CompareHostedZones(
        DnsHostedZoneIndexEntry left,
        DnsHostedZoneIndexEntry right)
    {
        int comparison = CompareKeys(left.ApexKey, right.ApexKey);
        if (comparison != 0)
        {
            return comparison;
        }

        if (left.ClassValue == right.ClassValue)
        {
            return 0;
        }

        return left.ClassValue < right.ClassValue ? -1 : 1;
    }

    static DnsName ParentName(DnsName name)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        int index = 1;
        while (index < name.Labels.Count)
        {
            result.Labels.Add(RecordSetOperations.CopyBytes(
                name.Labels[index]));
            index = index + 1;
        }

        return result;
    }

    static bool NameBelongsToZone(DnsName name, DnsName apex)
    {
        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            name,
            apex);
        return suffix.Success && suffix.BooleanValue;
    }

    static void ValidateLookup(DnsZoneIndex index, DnsName name)
    {
        if (index == null)
        {
            throw new System.ArgumentNullException("index");
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(name);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            throw new System.ArgumentException(
                "A zone-index lookup requires a valid absolute DNS name.");
        }
    }
}
