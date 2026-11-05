using System.Collections.Generic;

namespace dns;

/// <summary>Groups, normalizes, compares, searches, and combines DNS record sets.</summary>
public interface IDnsRecordSetService
{
    /// <summary>Groups records by canonical owner, class, and type.</summary>
    /// <param name="records">The records to group.</param>
    /// <returns>The grouped resource-record sets.</returns>
    List<DnsResourceRecordSet> GroupRecordSets(List<DnsResourceRecord> records);

    /// <summary>Normalizes one record set's owner, TTL, ordering, and duplicates.</summary>
    /// <param name="recordSet">The record set to normalize.</param>
    /// <param name="options">The canonical ordering and TTL policy.</param>
    /// <returns>The normalized resource-record set.</returns>
    DnsResourceRecordSet NormalizeRecordSet(DnsResourceRecordSet recordSet, DnsCanonicalOrderOptions options);

    /// <summary>Tests two record sets for normalized order-independent equality.</summary>
    /// <param name="left">The first record set.</param>
    /// <param name="right">The second record set.</param>
    /// <param name="options">The canonical ordering and TTL policy.</param>
    /// <returns>True when the normalized sets are equal.</returns>
    bool RecordSetsEqual(DnsResourceRecordSet left, DnsResourceRecordSet right, DnsCanonicalOrderOptions options);

    /// <summary>Compares two record sets in canonical DNSSEC order.</summary>
    /// <param name="left">The first record set.</param>
    /// <param name="right">The second record set.</param>
    /// <returns>A negative, zero, or positive ordering value.</returns>
    int CompareRecordSets(DnsResourceRecordSet left, DnsResourceRecordSet right);

    /// <summary>Removes byte-equivalent duplicate records from one set.</summary>
    /// <param name="recordSet">The record set to deduplicate.</param>
    /// <returns>The deduplicated record set.</returns>
    DnsResourceRecordSet SuppressDuplicateRecords(DnsResourceRecordSet recordSet);

    /// <summary>Calculates the normalized intersection of two record sets.</summary>
    /// <param name="left">The first record set.</param>
    /// <param name="right">The second record set.</param>
    /// <returns>The records present in both sets.</returns>
    DnsResourceRecordSet IntersectRecordSets(DnsResourceRecordSet left, DnsResourceRecordSet right);

    /// <summary>Calculates the normalized difference of two record sets.</summary>
    /// <param name="left">The source record set.</param>
    /// <param name="right">The records to subtract.</param>
    /// <returns>The records present only in the source set.</returns>
    DnsResourceRecordSet DifferenceRecordSets(DnsResourceRecordSet left, DnsResourceRecordSet right);

    /// <summary>Sorts record sets in canonical DNSSEC order.</summary>
    /// <param name="recordSets">The record sets to sort.</param>
    /// <returns>The canonically ordered record sets.</returns>
    List<DnsResourceRecordSet> SortRecordSets(List<DnsResourceRecordSet> recordSets);

    /// <summary>Finds record sets matching a section, owner, class, and type.</summary>
    /// <param name="message">The message to search.</param>
    /// <param name="section">The message section to search.</param>
    /// <param name="name">The canonical owner name.</param>
    /// <param name="recordType">The recognized record type.</param>
    /// <param name="recordTypeValue">The raw record-type integer.</param>
    /// <param name="recordClass">The recognized record class.</param>
    /// <param name="classValue">The raw record-class integer.</param>
    /// <returns>The matching record sets.</returns>
    List<DnsResourceRecordSet> FindRecordSets(DnsMessage message, DnsMessageSection section, DnsName name, DnsRecordType recordType, int recordTypeValue, DnsClass recordClass, int classValue);

    /// <summary>Extracts a bounded CNAME or DNAME answer chain from a response.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="question">The original DNS question.</param>
    /// <param name="maximumAliasDepth">The largest accepted alias depth.</param>
    /// <returns>The answer record sets, aliases, canonical name, and status.</returns>
    DnsResolveResult ExtractAnswerChain(DnsMessage message, DnsQuestion question, int maximumAliasDepth);
}
