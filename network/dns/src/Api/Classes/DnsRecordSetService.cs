using System.Collections.Generic;

namespace dns;

/// <summary>Groups, normalizes, compares, searches, and combines DNS record sets.</summary>
public class DnsRecordSetService : IDnsRecordSetService
{
    /// <summary>Creates a DNS record-set service.</summary>
    public DnsRecordSetService()
    {
    }

    /// <summary>Groups records by canonical owner, class, and type.</summary>
    /// <param name="records">The records to group.</param>
    /// <returns>The grouped resource-record sets.</returns>
    public List<DnsResourceRecordSet> GroupRecordSets(List<DnsResourceRecord> records)
    {
        return RecordSetOperations.GroupRecordSets(records);
    }

    /// <summary>Normalizes one record set.</summary>
    /// <param name="recordSet">The record set to normalize.</param>
    /// <param name="options">The canonical ordering and TTL policy.</param>
    /// <returns>The normalized resource-record set.</returns>
    public DnsResourceRecordSet NormalizeRecordSet(
        DnsResourceRecordSet recordSet,
        DnsCanonicalOrderOptions options)
    {
        return RecordSetOperations.NormalizeRecordSet(recordSet, options);
    }

    /// <summary>Tests two record sets for normalized equality.</summary>
    /// <param name="left">The first record set.</param>
    /// <param name="right">The second record set.</param>
    /// <param name="options">The normalization policy.</param>
    /// <returns>True when the normalized sets are equal.</returns>
    public bool RecordSetsEqual(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right,
        DnsCanonicalOrderOptions options)
    {
        return RecordSetOperations.RecordSetsEqual(left, right, options);
    }

    /// <summary>Compares record sets in canonical order.</summary>
    /// <param name="left">The first record set.</param>
    /// <param name="right">The second record set.</param>
    /// <returns>A negative, zero, or positive ordering value.</returns>
    public int CompareRecordSets(DnsResourceRecordSet left, DnsResourceRecordSet right)
    {
        return RecordSetOperations.CompareRecordSets(left, right);
    }

    /// <summary>Removes byte-equivalent duplicate records.</summary>
    /// <param name="recordSet">The source record set.</param>
    /// <returns>The deduplicated record set.</returns>
    public DnsResourceRecordSet SuppressDuplicateRecords(
        DnsResourceRecordSet recordSet)
    {
        return RecordSetOperations.SuppressDuplicateRecords(recordSet);
    }

    /// <summary>Calculates the normalized intersection of two record sets.</summary>
    /// <param name="left">The first record set.</param>
    /// <param name="right">The second record set.</param>
    /// <returns>The common records.</returns>
    public DnsResourceRecordSet IntersectRecordSets(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right)
    {
        return RecordSetOperations.IntersectRecordSets(left, right);
    }

    /// <summary>Calculates the normalized difference of two record sets.</summary>
    /// <param name="left">The source record set.</param>
    /// <param name="right">The records to subtract.</param>
    /// <returns>The remaining records.</returns>
    public DnsResourceRecordSet DifferenceRecordSets(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right)
    {
        return RecordSetOperations.DifferenceRecordSets(left, right);
    }

    /// <summary>Sorts record sets in canonical order.</summary>
    /// <param name="recordSets">The record sets to sort.</param>
    /// <returns>The ordered record sets.</returns>
    public List<DnsResourceRecordSet> SortRecordSets(
        List<DnsResourceRecordSet> recordSets)
    {
        return RecordSetOperations.SortRecordSets(recordSets);
    }

    /// <summary>Finds matching record sets in a message section.</summary>
    /// <param name="message">The message to search.</param>
    /// <param name="section">The section to search.</param>
    /// <param name="name">The owner name.</param>
    /// <param name="recordType">The recognized record type.</param>
    /// <param name="recordTypeValue">The raw record type.</param>
    /// <param name="recordClass">The recognized class.</param>
    /// <param name="classValue">The raw class.</param>
    /// <returns>The matching record sets.</returns>
    public List<DnsResourceRecordSet> FindRecordSets(
        DnsMessage message,
        DnsMessageSection section,
        DnsName name,
        DnsRecordType recordType,
        int recordTypeValue,
        DnsClass recordClass,
        int classValue)
    {
        return RecordSetOperations.FindRecordSets(
            message,
            section,
            name,
            recordType,
            recordTypeValue,
            recordClass,
            classValue);
    }

    /// <summary>Extracts a bounded CNAME or DNAME answer chain.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="question">The original question.</param>
    /// <param name="maximumAliasDepth">The maximum alias depth.</param>
    /// <returns>The extracted resolution result.</returns>
    public DnsResolveResult ExtractAnswerChain(
        DnsMessage message,
        DnsQuestion question,
        int maximumAliasDepth)
    {
        return RecordSetOperations.ExtractAnswerChain(
            message,
            question,
            maximumAliasDepth);
    }
}
