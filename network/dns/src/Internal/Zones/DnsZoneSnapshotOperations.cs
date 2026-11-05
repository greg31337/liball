using System.Collections.Generic;
using System.Text.Json;

namespace dns;

internal static class DnsZoneSnapshotOperations
{
    internal static DnsZone CopyZone(DnsZone source)
    {
        if (source == null || source.Nodes == null ||
            source.JournalEntries == null)
        {
            throw new System.ArgumentNullException("source");
        }

        DnsZone result = new DnsZone();
        result.Apex = RecordSetOperations.CopyName(source.Apex, false);
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.HasDefaultTimeToLive = source.HasDefaultTimeToLive;
        result.DefaultTimeToLive = source.DefaultTimeToLive;
        List<DnsZoneNode> sourceNodes = new List<DnsZoneNode>();
        List<DnsZoneNode> copiedNodes = new List<DnsZoneNode>();
        int index = 0;
        while (index < source.Nodes.Count)
        {
            CollectSnapshotNode(
                source.Nodes[index],
                sourceNodes,
                copiedNodes);
            index = index + 1;
        }

        index = 0;
        while (index < sourceNodes.Count)
        {
            CopySnapshotNodeData(sourceNodes[index], copiedNodes[index]);
            index = index + 1;
        }

        index = 0;
        while (index < sourceNodes.Count)
        {
            CopySnapshotNodeChildren(
                sourceNodes[index],
                copiedNodes[index],
                sourceNodes,
                copiedNodes);
            index = index + 1;
        }

        index = 0;
        while (index < source.Nodes.Count)
        {
            int position = FindSnapshotNodePosition(
                sourceNodes,
                source.Nodes[index]);
            result.Nodes.Add(copiedNodes[position]);
            index = index + 1;
        }

        index = 0;
        while (index < source.JournalEntries.Count)
        {
            result.JournalEntries.Add(CopyJournalEntry(
                source.JournalEntries[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DnsZoneTransaction CopyTransaction(
        DnsZoneTransaction source)
    {
        if (source == null || source.Changes == null)
        {
            throw new System.ArgumentNullException("source");
        }

        DnsZoneTransaction result = new DnsZoneTransaction();
        result.Identifier = source.Identifier;
        result.HasExpectedSerial = source.HasExpectedSerial;
        result.ExpectedSerial = source.ExpectedSerial;
        result.HasResultingSerial = source.HasResultingSerial;
        result.ResultingSerial = source.ResultingSerial;
        result.Atomic = source.Atomic;
        result.Committed = source.Committed;
        int index = 0;
        while (index < source.Changes.Count)
        {
            result.Changes.Add(CopyChange(source.Changes[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DnsZoneJournalEntry CopyJournalEntry(
        DnsZoneJournalEntry source)
    {
        if (source == null || source.Transaction == null)
        {
            throw new System.ArgumentNullException("source");
        }

        DnsZoneJournalEntry result = new DnsZoneJournalEntry();
        result.Sequence = source.Sequence;
        result.FromSerial = source.FromSerial;
        result.ToSerial = source.ToSerial;
        result.Transaction = CopyTransaction(source.Transaction);
        result.RecordedAtUnixMilliseconds = source.RecordedAtUnixMilliseconds;
        return result;
    }

    internal static DnsResourceRecordSet CopyRecordSet(
        DnsResourceRecordSet source)
    {
        if (source == null || source.Records == null)
        {
            throw new System.ArgumentNullException("source");
        }

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
            result.Records.Add(CopyRecord(source.Records[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DnsResourceRecord CopyRecord(DnsResourceRecord source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.IncludeFields = true;
        string serialized = JsonSerializer.Serialize(source, options);
        DnsResourceRecord? result =
            JsonSerializer.Deserialize<DnsResourceRecord>(serialized, options);
        if (result == null)
        {
            throw new System.InvalidOperationException(
                "The DNS resource record could not be copied.");
        }

        return result;
    }

    static void CollectSnapshotNode(
        DnsZoneNode source,
        List<DnsZoneNode> sourceNodes,
        List<DnsZoneNode> copiedNodes)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (FindSnapshotNodePosition(sourceNodes, source) >= 0)
        {
            return;
        }

        sourceNodes.Add(source);
        copiedNodes.Add(new DnsZoneNode());
        int index = 0;
        while (index < source.Children.Count)
        {
            CollectSnapshotNode(
                source.Children[index],
                sourceNodes,
                copiedNodes);
            index = index + 1;
        }
    }

    static int FindSnapshotNodePosition(
        List<DnsZoneNode> nodes,
        DnsZoneNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], candidate))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static void CopySnapshotNodeData(
        DnsZoneNode source,
        DnsZoneNode result)
    {
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.IsDelegation = source.IsDelegation;
        result.IsEmptyNonTerminal = source.IsEmptyNonTerminal;
        int index = 0;
        while (index < source.RecordSets.Count)
        {
            result.RecordSets.Add(CopyRecordSet(source.RecordSets[index]));
            index = index + 1;
        }
    }

    static void CopySnapshotNodeChildren(
        DnsZoneNode source,
        DnsZoneNode result,
        List<DnsZoneNode> sourceNodes,
        List<DnsZoneNode> copiedNodes)
    {
        int index = 0;
        while (index < source.Children.Count)
        {
            int position = FindSnapshotNodePosition(
                sourceNodes,
                source.Children[index]);
            result.Children.Add(copiedNodes[position]);
            index = index + 1;
        }
    }

    static DnsZoneChange CopyChange(DnsZoneChange source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        DnsZoneChange result = new DnsZoneChange();
        result.Kind = source.Kind;
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        if (source.Record != null)
        {
            result.Record = CopyRecord(source.Record);
        }

        if (source.RecordSet != null)
        {
            result.RecordSet = CopyRecordSet(source.RecordSet);
        }

        return result;
    }
}
