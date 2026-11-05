using System.Collections.Generic;
using System.Text.Json;

namespace dns;

internal class DnsStoredZone
{
    internal DnsZone Zone = new DnsZone();
    internal DnsZoneIndex Index = new DnsZoneIndex();
}

internal class DnsZoneMutationResult
{
    internal bool Success;
    internal string Reason = string.Empty;
}

internal class DnsSoaSerialResult
{
    internal bool Success;
    internal uint Serial;
}

internal static class DnsZoneStoreOperations
{
    internal static DnsZone? FindBestZone(
        MemoryDnsZoneStore store,
        DnsName name,
        DnsClass recordClass,
        int classValue)
    {
        DnsZone? found = ZoneIndexOperations.FindLongestZone(
            store.HostedIndex,
            name,
            recordClass,
            classValue);
        if (found == null)
        {
            return null;
        }

        return DnsZoneSnapshotOperations.CopyZone(found);
    }

    internal static DnsZone? ReadZone(
        MemoryDnsZoneStore store,
        DnsName apex,
        DnsClass recordClass,
        int classValue)
    {
        int position = FindStoredZonePosition(
            store,
            apex,
            EffectiveClass(recordClass, classValue));
        if (position < 0)
        {
            return null;
        }

        return DnsZoneSnapshotOperations.CopyZone(store.Zones[position].Zone);
    }

    internal static DnsZoneValidationResult AddZone(
        MemoryDnsZoneStore store,
        DnsZone zone)
    {
        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(zone);
        if (!validation.Valid)
        {
            return validation;
        }

        int classValue = EffectiveClass(zone.Class, zone.ClassValue);
        if (FindStoredZonePosition(store, zone.Apex, classValue) >= 0)
        {
            return InvalidZoneResult(
                "A zone with the same apex and class is already hosted.",
                zone.Apex);
        }

        DnsZone copy = DnsZoneSnapshotOperations.CopyZone(zone);
        NormalizeZoneTopology(copy);
        validation = DnsZoneValidator.ValidateZone(copy);
        if (!validation.Valid)
        {
            return validation;
        }

        DnsStoredZone stored = CreateStoredZone(copy);
        store.Zones.Add(stored);
        AdvanceJournalSequence(store, copy);
        RebuildHostedIndex(store);
        return validation;
    }

    internal static DnsZoneValidationResult ReplaceZone(
        MemoryDnsZoneStore store,
        DnsZone zone)
    {
        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(zone);
        if (!validation.Valid)
        {
            return validation;
        }

        int classValue = EffectiveClass(zone.Class, zone.ClassValue);
        int position = FindStoredZonePosition(
            store,
            zone.Apex,
            classValue);
        if (position < 0)
        {
            return InvalidZoneResult(
                "The zone to replace is not hosted.",
                zone.Apex);
        }

        DnsZone copy = DnsZoneSnapshotOperations.CopyZone(zone);
        NormalizeZoneTopology(copy);
        validation = DnsZoneValidator.ValidateZone(copy);
        if (!validation.Valid)
        {
            return validation;
        }

        store.Zones[position] = CreateStoredZone(copy);
        AdvanceJournalSequence(store, copy);
        RebuildHostedIndex(store);
        return validation;
    }

    internal static bool DeleteZone(
        MemoryDnsZoneStore store,
        DnsName apex,
        DnsClass recordClass,
        int classValue)
    {
        int position = FindStoredZonePosition(
            store,
            apex,
            EffectiveClass(recordClass, classValue));
        if (position < 0)
        {
            return false;
        }

        store.Zones.RemoveAt(position);
        RebuildHostedIndex(store);
        return true;
    }

    internal static DnsZoneValidationResult ApplyZoneTransaction(
        MemoryDnsZoneStore store,
        DnsName apex,
        DnsClass recordClass,
        int classValue,
        DnsZoneTransaction transaction)
    {
        if (transaction == null || transaction.Changes == null)
        {
            return InvalidZoneResult(
                "A complete zone transaction is required.",
                apex);
        }

        transaction.Committed = false;
        if (!transaction.Atomic)
        {
            return InvalidZoneResult(
                "Zone transactions must request atomic application.",
                apex);
        }

        int effectiveClass = EffectiveClass(recordClass, classValue);
        int position = FindStoredZonePosition(store, apex, effectiveClass);
        if (position < 0)
        {
            return InvalidZoneResult(
                "The transaction zone is not hosted.",
                apex);
        }

        DnsZone stored = store.Zones[position].Zone;
        DnsSoaSerialResult current = ReadSoaSerial(stored);
        if (!current.Success)
        {
            return InvalidZoneResult(
                "The hosted zone does not contain one readable apex SOA serial.",
                apex);
        }

        if (transaction.HasExpectedSerial &&
            transaction.ExpectedSerial != current.Serial)
        {
            return InvalidZoneResult(
                "The expected SOA serial does not match the hosted zone.",
                apex);
        }

        uint resultingSerial = current.Serial == uint.MaxValue ?
            0 : current.Serial + 1;
        if (transaction.HasResultingSerial)
        {
            DnsSerialComparison comparison = DnsSerialArithmetic.CompareSerials(
                transaction.ResultingSerial,
                current.Serial);
            if (!comparison.Defined || comparison.Comparison != 1)
            {
                return InvalidZoneResult(
                    "The resulting SOA serial must advance by RFC 1982 arithmetic.",
                    apex);
            }

            resultingSerial = transaction.ResultingSerial;
        }

        DnsZone candidate = DnsZoneSnapshotOperations.CopyZone(stored);
        int changeIndex = 0;
        while (changeIndex < transaction.Changes.Count)
        {
            DnsZoneMutationResult mutation = ApplyChange(
                candidate,
                transaction.Changes[changeIndex]);
            if (!mutation.Success)
            {
                return InvalidZoneResult(mutation.Reason, apex);
            }

            changeIndex = changeIndex + 1;
        }

        NormalizeZoneTopology(candidate);
        if (!WriteSoaSerial(candidate, resultingSerial))
        {
            return InvalidZoneResult(
                "The transaction removed or obscured the apex SOA record.",
                apex);
        }

        DnsZoneTransaction committed =
            DnsZoneSnapshotOperations.CopyTransaction(transaction);
        committed.Committed = true;
        DnsZoneJournalEntry journal = new DnsZoneJournalEntry();
        journal.Sequence = store.NextJournalSequence + 1;
        journal.FromSerial = current.Serial;
        journal.ToSerial = resultingSerial;
        journal.Transaction = committed;
        journal.RecordedAtUnixMilliseconds =
            System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        candidate.JournalEntries.Add(journal);
        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(candidate);
        if (!validation.Valid)
        {
            return validation;
        }

        store.Zones[position] = CreateStoredZone(candidate);
        store.NextJournalSequence = journal.Sequence;
        RebuildHostedIndex(store);
        transaction.Committed = true;
        return validation;
    }

    internal static List<DnsZoneJournalEntry> ReadZoneJournal(
        MemoryDnsZoneStore store,
        DnsName apex,
        DnsClass recordClass,
        int classValue,
        uint fromSerial)
    {
        List<DnsZoneJournalEntry> result =
            new List<DnsZoneJournalEntry>();
        int position = FindStoredZonePosition(
            store,
            apex,
            EffectiveClass(recordClass, classValue));
        if (position < 0)
        {
            return result;
        }

        List<DnsZoneJournalEntry> entries =
            store.Zones[position].Zone.JournalEntries;
        int index = 0;
        while (index < entries.Count && entries[index].FromSerial != fromSerial)
        {
            index = index + 1;
        }

        while (index < entries.Count)
        {
            result.Add(DnsZoneSnapshotOperations.CopyJournalEntry(
                entries[index]));
            index = index + 1;
        }

        return result;
    }

    static DnsStoredZone CreateStoredZone(DnsZone zone)
    {
        DnsStoredZone result = new DnsStoredZone();
        result.Zone = zone;
        result.Index = ZoneIndexOperations.BuildZone(zone);
        return result;
    }

    static int FindStoredZonePosition(
        MemoryDnsZoneStore store,
        DnsName apex,
        int classValue)
    {
        RawDnsNameResult validated = RawDnsName.ValidateName(apex);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute || classValue < 0 ||
            classValue > 65535)
        {
            throw new System.ArgumentException(
                "A zone-store key requires a valid absolute apex and class.");
        }

        int index = 0;
        while (index < store.Zones.Count)
        {
            DnsStoredZone candidate = store.Zones[index];
            if (candidate.Index.ClassValue == classValue &&
                RecordSetOperations.NamesEqual(candidate.Zone.Apex, validated.Name))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int EffectiveClass(DnsClass recordClass, int classValue)
    {
        int result = RecordSetOperations.EffectiveValue(
            classValue,
            (int)recordClass);
        if (result < 0 || result > 65535)
        {
            throw new System.ArgumentException(
                "A DNS class must fit in sixteen bits.");
        }

        return result;
    }

    static void RebuildHostedIndex(MemoryDnsZoneStore store)
    {
        List<DnsZone> zones = new List<DnsZone>();
        int index = 0;
        while (index < store.Zones.Count)
        {
            zones.Add(store.Zones[index].Zone);
            index = index + 1;
        }

        store.HostedIndex = ZoneIndexOperations.BuildHosted(zones);
    }

    static void AdvanceJournalSequence(
        MemoryDnsZoneStore store,
        DnsZone zone)
    {
        int index = 0;
        while (index < zone.JournalEntries.Count)
        {
            if (zone.JournalEntries[index].Sequence > store.NextJournalSequence)
            {
                store.NextJournalSequence =
                    zone.JournalEntries[index].Sequence;
            }

            index = index + 1;
        }
    }

    static DnsZoneValidationResult InvalidZoneResult(
        string reason,
        DnsName name)
    {
        DnsZoneValidationResult result = new DnsZoneValidationResult();
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidZone;
        diagnostic.Reason = reason;
        diagnostic.Name = name;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsSoaSerialResult ReadSoaSerial(DnsZone zone)
    {
        DnsSoaSerialResult result = new DnsSoaSerialResult();
        DnsZoneNode? apex = FindNodeByName(zone.Nodes, zone.Apex);
        if (apex == null)
        {
            return result;
        }

        int setIndex = 0;
        while (setIndex < apex.RecordSets.Count)
        {
            DnsResourceRecordSet set = apex.RecordSets[setIndex];
            if (EffectiveSetType(set) == 6 && set.Records.Count == 1)
            {
                DnsResourceRecord record = set.Records[0];
                if (record.Data != null && record.Data.Soa != null)
                {
                    result.Success = true;
                    result.Serial = record.Data.Soa.Serial;
                    return result;
                }
            }

            setIndex = setIndex + 1;
        }

        return result;
    }

    static bool WriteSoaSerial(DnsZone zone, uint serial)
    {
        DnsZoneNode? apex = FindNodeByName(zone.Nodes, zone.Apex);
        if (apex == null)
        {
            return false;
        }

        int setIndex = 0;
        while (setIndex < apex.RecordSets.Count)
        {
            DnsResourceRecordSet set = apex.RecordSets[setIndex];
            if (EffectiveSetType(set) == 6 && set.Records.Count == 1)
            {
                DnsResourceRecord record = set.Records[0];
                if (record.Data != null && record.Data.Soa != null)
                {
                    record.Data.Soa.Serial = serial;
                    return true;
                }
            }

            setIndex = setIndex + 1;
        }

        return false;
    }

    static DnsZoneMutationResult ApplyChange(
        DnsZone zone,
        DnsZoneChange change)
    {
        DnsZoneMutationResult validation = ValidateChange(zone, change);
        if (!validation.Success)
        {
            return validation;
        }

        if (change.Kind == DnsZoneChangeKind.Add)
        {
            return ApplyAddRecord(zone, change);
        }

        if (change.Kind == DnsZoneChangeKind.ReplaceRecordSet)
        {
            return ApplyReplaceSet(zone, change);
        }

        if (change.Kind == DnsZoneChangeKind.DeleteRecord)
        {
            return ApplyDeleteRecord(zone, change);
        }

        if (change.Kind == DnsZoneChangeKind.DeleteRecordSet)
        {
            return ApplyDeleteSet(zone, change);
        }

        if (change.Kind == DnsZoneChangeKind.DeleteName)
        {
            return ApplyDeleteName(zone, change);
        }

        return FailedMutation("The zone change kind is not supported.");
    }

    static DnsZoneMutationResult ValidateChange(
        DnsZone zone,
        DnsZoneChange change)
    {
        if (change == null)
        {
            return FailedMutation("A zone transaction cannot contain a null change.");
        }

        RawDnsNameResult name = RawDnsName.ValidateName(change.Name);
        if (!name.Success || name.Name == null || !name.Name.IsAbsolute)
        {
            return FailedMutation(
                "Every zone change requires a valid absolute owner name.");
        }

        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            name.Name,
            zone.Apex);
        if (!suffix.Success || !suffix.BooleanValue)
        {
            return FailedMutation(
                "Every zone change owner must be within the hosted zone.");
        }

        int changeClass = RecordSetOperations.EffectiveValue(
            change.ClassValue,
            (int)change.Class);
        int zoneClass = RecordSetOperations.EffectiveValue(
            zone.ClassValue,
            (int)zone.Class);
        if (changeClass != zoneClass)
        {
            return FailedMutation(
                "Every zone change class must match the hosted zone.");
        }

        if (change.Kind != DnsZoneChangeKind.DeleteName)
        {
            int changeType = EffectiveChangeType(change);
            if (changeType < 0 || changeType > 65535)
            {
                return FailedMutation(
                    "Every zone change type must fit in sixteen bits.");
            }
        }

        DnsZoneMutationResult result = new DnsZoneMutationResult();
        result.Success = true;
        return result;
    }

    static DnsZoneMutationResult ApplyAddRecord(
        DnsZone zone,
        DnsZoneChange change)
    {
        if (change.Record == null ||
            !RecordSetOperations.NamesEqual(change.Record.Name, change.Name))
        {
            return FailedMutation(
                "An add change requires a record at the affected owner name.");
        }

        int recordType = EffectiveRecordType(change.Record);
        int recordClass = EffectiveRecordClass(change.Record);
        int changeType = EffectiveChangeType(change);
        int changeClass = EffectiveChangeClass(change);
        if (recordType != changeType || recordClass != changeClass)
        {
            return FailedMutation(
                "An added record must match the change type and class.");
        }

        DnsZoneNode node = FindOrCreateNode(zone, change.Name);
        DnsResourceRecordSet? set = FindSetByKey(
            node,
            recordType,
            recordClass);
        if (set == null)
        {
            set = new DnsResourceRecordSet();
            set.Name = RecordSetOperations.CopyName(change.Name, false);
            set.RecordType = (DnsRecordType)recordType;
            set.RecordTypeValue = recordType;
            set.Class = (DnsClass)recordClass;
            set.ClassValue = recordClass;
            set.TimeToLive = change.Record.TimeToLive;
            node.RecordSets.Add(set);
        }
        else if (set.TimeToLive != change.Record.TimeToLive)
        {
            return FailedMutation(
                "An added record TTL must match its existing RRset.");
        }

        int recordIndex = 0;
        while (recordIndex < set.Records.Count)
        {
            if (RecordsHaveSameData(set.Records[recordIndex], change.Record))
            {
                DnsZoneMutationResult duplicate = new DnsZoneMutationResult();
                duplicate.Success = true;
                return duplicate;
            }

            recordIndex = recordIndex + 1;
        }

        DnsResourceRecord copied =
            DnsZoneSnapshotOperations.CopyRecord(change.Record);
        copied.Name = RecordSetOperations.CopyName(change.Name, false);
        copied.RecordType = (DnsRecordType)recordType;
        copied.RecordTypeValue = recordType;
        copied.Class = (DnsClass)recordClass;
        copied.ClassValue = recordClass;
        set.Records.Add(copied);
        node.IsEmptyNonTerminal = false;
        return SuccessfulMutation();
    }

    static DnsZoneMutationResult ApplyReplaceSet(
        DnsZone zone,
        DnsZoneChange change)
    {
        if (change.RecordSet == null ||
            !RecordSetOperations.NamesEqual(change.RecordSet.Name, change.Name))
        {
            return FailedMutation(
                "A replace change requires an RRset at the affected owner name.");
        }

        int setType = EffectiveSetType(change.RecordSet);
        int setClass = EffectiveSetClass(change.RecordSet);
        if (setType != EffectiveChangeType(change) ||
            setClass != EffectiveChangeClass(change))
        {
            return FailedMutation(
                "A replacement RRset must match the change type and class.");
        }

        DnsZoneNode node = FindOrCreateNode(zone, change.Name);
        RemoveSetByKey(node, setType, setClass);
        node.RecordSets.Add(DnsZoneSnapshotOperations.CopyRecordSet(
            change.RecordSet));
        node.IsEmptyNonTerminal = false;
        return SuccessfulMutation();
    }

    static DnsZoneMutationResult ApplyDeleteRecord(
        DnsZone zone,
        DnsZoneChange change)
    {
        if (change.Record == null ||
            !RecordSetOperations.NamesEqual(change.Record.Name, change.Name))
        {
            return FailedMutation(
                "A delete-record change requires a matching record.");
        }

        int recordType = EffectiveRecordType(change.Record);
        int recordClass = EffectiveRecordClass(change.Record);
        if (recordType != EffectiveChangeType(change) ||
            recordClass != EffectiveChangeClass(change))
        {
            return FailedMutation(
                "A deleted record must match the change type and class.");
        }

        DnsZoneNode? node = FindNodeByName(zone.Nodes, change.Name);
        if (node == null)
        {
            return SuccessfulMutation();
        }

        DnsResourceRecordSet? set = FindSetByKey(
            node,
            recordType,
            recordClass);
        if (set == null)
        {
            return SuccessfulMutation();
        }

        int index = 0;
        while (index < set.Records.Count)
        {
            if (RecordsHaveSameData(set.Records[index], change.Record))
            {
                set.Records.RemoveAt(index);
                if (set.Records.Count == 0)
                {
                    RemoveSetByKey(node, recordType, recordClass);
                }

                return SuccessfulMutation();
            }

            index = index + 1;
        }

        return SuccessfulMutation();
    }

    static DnsZoneMutationResult ApplyDeleteSet(
        DnsZone zone,
        DnsZoneChange change)
    {
        int typeValue = EffectiveChangeType(change);
        int classValue = EffectiveChangeClass(change);
        if (change.RecordSet != null)
        {
            if (!RecordSetOperations.NamesEqual(
                change.RecordSet.Name,
                change.Name) ||
                EffectiveSetType(change.RecordSet) != typeValue ||
                EffectiveSetClass(change.RecordSet) != classValue)
            {
                return FailedMutation(
                    "A supplied deleted RRset must match the change key.");
            }
        }

        DnsZoneNode? node = FindNodeByName(zone.Nodes, change.Name);
        if (node != null)
        {
            RemoveSetByKey(node, typeValue, classValue);
        }

        return SuccessfulMutation();
    }

    static DnsZoneMutationResult ApplyDeleteName(
        DnsZone zone,
        DnsZoneChange change)
    {
        DnsZoneNode? node = FindNodeByName(zone.Nodes, change.Name);
        if (node != null)
        {
            node.RecordSets.Clear();
            node.IsDelegation = false;
        }

        return SuccessfulMutation();
    }

    static DnsZoneMutationResult SuccessfulMutation()
    {
        DnsZoneMutationResult result = new DnsZoneMutationResult();
        result.Success = true;
        return result;
    }

    static DnsZoneMutationResult FailedMutation(string reason)
    {
        DnsZoneMutationResult result = new DnsZoneMutationResult();
        result.Reason = reason;
        return result;
    }

    static DnsZoneNode FindOrCreateNode(DnsZone zone, DnsName name)
    {
        DnsZoneNode? found = FindNodeByName(zone.Nodes, name);
        if (found != null)
        {
            return found;
        }

        DnsZoneNode result = new DnsZoneNode();
        result.Name = RecordSetOperations.CopyName(name, false);
        zone.Nodes.Add(result);
        return result;
    }

    static DnsZoneNode? FindNodeByName(
        List<DnsZoneNode> nodes,
        DnsName name)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (RecordSetOperations.NamesEqual(nodes[index].Name, name))
            {
                return nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsResourceRecordSet? FindSetByKey(
        DnsZoneNode node,
        int typeValue,
        int classValue)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet candidate = node.RecordSets[index];
            if (EffectiveSetType(candidate) == typeValue &&
                EffectiveSetClass(candidate) == classValue)
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    static void RemoveSetByKey(
        DnsZoneNode node,
        int typeValue,
        int classValue)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet candidate = node.RecordSets[index];
            if (EffectiveSetType(candidate) == typeValue &&
                EffectiveSetClass(candidate) == classValue)
            {
                node.RecordSets.RemoveAt(index);
                return;
            }

            index = index + 1;
        }
    }

    static bool RecordsHaveSameData(
        DnsResourceRecord left,
        DnsResourceRecord right)
    {
        if (left.UseRawData != right.UseRawData ||
            !RecordSetOperations.BytesEqual(left.RawData, right.RawData))
        {
            return false;
        }

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.IncludeFields = true;
        string leftData = JsonSerializer.Serialize(left.Data, options);
        string rightData = JsonSerializer.Serialize(right.Data, options);
        return leftData == rightData;
    }

    static int EffectiveRecordType(DnsResourceRecord record)
    {
        return RecordSetOperations.EffectiveValue(
            record.RecordTypeValue,
            (int)record.RecordType);
    }

    static int EffectiveRecordClass(DnsResourceRecord record)
    {
        return RecordSetOperations.EffectiveValue(
            record.ClassValue,
            (int)record.Class);
    }

    static int EffectiveSetType(DnsResourceRecordSet set)
    {
        return RecordSetOperations.EffectiveValue(
            set.RecordTypeValue,
            (int)set.RecordType);
    }

    static int EffectiveSetClass(DnsResourceRecordSet set)
    {
        return RecordSetOperations.EffectiveValue(
            set.ClassValue,
            (int)set.Class);
    }

    static int EffectiveChangeType(DnsZoneChange change)
    {
        return RecordSetOperations.EffectiveValue(
            change.RecordTypeValue,
            (int)change.RecordType);
    }

    static int EffectiveChangeClass(DnsZoneChange change)
    {
        return RecordSetOperations.EffectiveValue(
            change.ClassValue,
            (int)change.Class);
    }

    static void NormalizeZoneTopology(DnsZone zone)
    {
        List<DnsZoneNode> nodes = CollectUniqueZoneNodes(zone);
        EnsureAncestorNodes(zone, nodes);
        RemoveOrphanEmptyNodes(zone, nodes);
        int index = 0;
        while (index < nodes.Count)
        {
            nodes[index].Children.Clear();
            nodes[index].IsDelegation = false;
            nodes[index].IsEmptyNonTerminal =
                nodes[index].RecordSets.Count == 0;
            index = index + 1;
        }

        index = 0;
        while (index < nodes.Count)
        {
            DnsZoneNode node = nodes[index];
            if (!RecordSetOperations.NamesEqual(node.Name, zone.Apex))
            {
                DnsName parentName = CreateParentName(node.Name);
                DnsZoneNode? parent = FindNodeByName(nodes, parentName);
                if (parent != null)
                {
                    parent.Children.Add(node);
                }
            }

            index = index + 1;
        }

        DnsZone orderedZone = new DnsZone();
        orderedZone.Apex = zone.Apex;
        orderedZone.Class = zone.Class;
        orderedZone.ClassValue = zone.ClassValue;
        orderedZone.Nodes = nodes;
        DnsZoneIndex ordered = ZoneIndexOperations.BuildZone(orderedZone);
        zone.Nodes.Clear();
        index = 0;
        while (index < ordered.Nodes.Count)
        {
            zone.Nodes.Add(ordered.Nodes[index].Node);
            index = index + 1;
        }
    }

    static List<DnsZoneNode> CollectUniqueZoneNodes(DnsZone zone)
    {
        List<DnsZoneNode> result = new List<DnsZoneNode>();
        List<DnsZoneNode> pending = new List<DnsZoneNode>();
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            pending.Add(zone.Nodes[index]);
            index = index + 1;
        }

        index = 0;
        while (index < pending.Count)
        {
            DnsZoneNode node = pending[index];
            if (!ContainsNodeReference(result, node))
            {
                result.Add(node);
                int childIndex = 0;
                while (childIndex < node.Children.Count)
                {
                    pending.Add(node.Children[childIndex]);
                    childIndex = childIndex + 1;
                }
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
            if (object.ReferenceEquals(nodes[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void EnsureAncestorNodes(
        DnsZone zone,
        List<DnsZoneNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            DnsName parent = CreateParentName(nodes[index].Name);
            while (parent.Labels.Count >= zone.Apex.Labels.Count &&
                FindNodeByName(nodes, parent) == null)
            {
                DnsZoneNode created = new DnsZoneNode();
                created.Name = RecordSetOperations.CopyName(parent, false);
                created.IsEmptyNonTerminal = true;
                nodes.Add(created);
                if (RecordSetOperations.NamesEqual(parent, zone.Apex))
                {
                    break;
                }

                parent = CreateParentName(parent);
            }

            index = index + 1;
        }
    }

    static void RemoveOrphanEmptyNodes(
        DnsZone zone,
        List<DnsZoneNode> nodes)
    {
        bool removed = true;
        while (removed)
        {
            removed = false;
            int index = nodes.Count - 1;
            while (index >= 0)
            {
                DnsZoneNode node = nodes[index];
                if (node.RecordSets.Count == 0 &&
                    !RecordSetOperations.NamesEqual(node.Name, zone.Apex) &&
                    !HasDescendantNode(nodes, node.Name))
                {
                    nodes.RemoveAt(index);
                    removed = true;
                }

                index = index - 1;
            }
        }
    }

    static bool HasDescendantNode(
        List<DnsZoneNode> nodes,
        DnsName ancestor)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (!RecordSetOperations.NamesEqual(nodes[index].Name, ancestor))
            {
                DnsNameOperationResult relation =
                    DnsNameOperations.IsAncestorName(
                        ancestor,
                        nodes[index].Name);
                if (relation.Success && relation.BooleanValue)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    static DnsName CreateParentName(DnsName name)
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
}
