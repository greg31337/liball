using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace dns;

internal class DnsZoneTextRecord
{
    internal DnsResourceRecord Record = new DnsResourceRecord();
    internal string Text = string.Empty;
    internal int RecordTypeValue;
    internal int ClassValue;
    internal uint TimeToLive;
    internal byte[] CanonicalRecordData = [];
}

internal static class DnsZoneText
{
    internal static DnsTextResult FormatZone(
        DnsZone zone,
        DnsTextOptions options)
    {
        if (zone == null || options == null || zone.Nodes == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "zone",
                "A zone, its nodes, and text options are required.",
                0);
        }

        if (options.IndentSize < 0 || options.MaximumLineLength <= 0)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "Text indentation and line-length limits must be positive.",
                0);
        }

        DnsTextOptions formatOptions = CopyOptions(options);
        if (options.Canonical) formatOptions.Origin = null;
        List<DnsZoneTextRecord> records = new List<DnsZoneTextRecord>();
        List<DnsZoneNode> visited = new List<DnsZoneNode>();
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            DnsTextResult? failure = AddNodeRecords(
                zone.Nodes[index], formatOptions, records, visited);
            if (failure != null) return failure;
            index = index + 1;
        }

        if (options.Canonical || !options.PreserveInputFormatting)
            SortRecords(records);
        StringBuilder builder = new StringBuilder();
        if (!options.Canonical && options.Origin != null)
        {
            DnsTextOptions absoluteOptions = CopyOptions(options);
            absoluteOptions.Origin = null;
            DnsTextResult origin = DnsPresentationNameText.FormatName(
                options.Origin, absoluteOptions);
            if (!origin.Success) return origin;
            AppendLine(builder, "$ORIGIN " + origin.Text);
        }

        if (!options.Canonical && zone.HasDefaultTimeToLive)
        {
            AppendLine(
                builder,
                "$TTL " + zone.DefaultTimeToLive.ToString(
                    CultureInfo.InvariantCulture));
        }

        index = 0;
        while (index < records.Count)
        {
            AppendLine(builder, records[index].Text);
            index = index + 1;
        }

        DnsTextResult result = DnsTextOperations.SuccessText(
            builder.ToString());
        result.Zone = zone;
        return result;
    }

    static DnsTextResult? AddNodeRecords(
        DnsZoneNode node,
        DnsTextOptions options,
        List<DnsZoneTextRecord> records,
        List<DnsZoneNode> visited)
    {
        if (node == null || node.RecordSets == null ||
            node.Children == null)
        {
            return ZoneFailure(
                "Every zone node requires record-set and child collections.");
        }

        if (ContainsNode(visited, node)) return null;
        visited.Add(node);
        int setIndex = 0;
        while (setIndex < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[setIndex];
            if (set == null || set.Records == null)
            {
                return ZoneFailure(
                    "Every zone record set requires a record collection.");
            }

            int recordIndex = 0;
            while (recordIndex < set.Records.Count)
            {
                DnsResourceRecord record = set.Records[recordIndex];
                if (record == null)
                {
                    return ZoneFailure(
                        "A zone record collection cannot contain null records.");
                }

                DnsTextResult formatted =
                    DnsResourceRecordText.FormatResourceRecord(
                        record, options);
                if (!formatted.Success) return formatted;
                DnsZoneTextRecord entry = new DnsZoneTextRecord();
                entry.Record = record;
                entry.Text = formatted.Text;
                entry.RecordTypeValue = DnsHeaderCodec.ResolveCode(
                    record.RecordTypeValue, (int)record.RecordType);
                entry.ClassValue = DnsHeaderCodec.ResolveCode(
                    record.ClassValue, (int)record.Class);
                entry.TimeToLive = record.TimeToLive;
                DnsEncodeResult canonicalData = EncodeCanonicalData(
                    record, entry.RecordTypeValue);
                if (!canonicalData.Success)
                    return FromEncodeFailure(canonicalData);
                entry.CanonicalRecordData = canonicalData.Bytes;
                records.Add(entry);
                recordIndex = recordIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            DnsTextResult? failure = AddNodeRecords(
                node.Children[childIndex], options, records, visited);
            if (failure != null) return failure;
            childIndex = childIndex + 1;
        }

        return null;
    }

    static bool ContainsNode(
        List<DnsZoneNode> nodes,
        DnsZoneNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], node)) return true;
            index = index + 1;
        }

        return false;
    }

    static void SortRecords(List<DnsZoneTextRecord> records)
    {
        int index = 1;
        while (index < records.Count)
        {
            int position = index;
            while (position > 0 && CompareRecords(
                records[position - 1], records[position]) > 0)
            {
                DnsZoneTextRecord swap = records[position - 1];
                records[position - 1] = records[position];
                records[position] = swap;
                position = position - 1;
            }

            index = index + 1;
        }
    }

    static int CompareRecords(
        DnsZoneTextRecord left,
        DnsZoneTextRecord right)
    {
        DnsNameOperationResult names = DnsNameOperations.CompareNames(
            left.Record.Name, right.Record.Name);
        if (names.Success && names.Comparison != 0)
            return names.Comparison;
        if (left.RecordTypeValue != right.RecordTypeValue)
            return left.RecordTypeValue < right.RecordTypeValue ? -1 : 1;
        if (left.ClassValue != right.ClassValue)
            return left.ClassValue < right.ClassValue ? -1 : 1;
        if (left.TimeToLive != right.TimeToLive)
            return left.TimeToLive < right.TimeToLive ? -1 : 1;
        int dataComparison = CompareBytes(
            left.CanonicalRecordData, right.CanonicalRecordData);
        if (dataComparison != 0) return dataComparison;
        return string.Compare(
            left.Text, right.Text, System.StringComparison.Ordinal);
    }

    static DnsEncodeResult EncodeCanonicalData(
        DnsResourceRecord record,
        int recordTypeValue)
    {
        if (!record.UseRawData && record.Data != null)
        {
            DnsEncodeOptions options = new DnsEncodeOptions();
            options.Canonical = true;
            return ResourceRecordCodec.EncodeRecordData(
                recordTypeValue, record.Data, options);
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = CopyBytes(record.RawData);
        result.BytesWritten = result.Bytes.Length;
        return result;
    }

    static int CompareBytes(byte[] left, byte[] right)
    {
        int length = left.Length;
        if (right.Length < length) length = right.Length;
        int index = 0;
        while (index < length)
        {
            if (left[index] != right[index])
                return left[index] < right[index] ? -1 : 1;
            index = index + 1;
        }

        if (left.Length == right.Length) return 0;
        return left.Length < right.Length ? -1 : 1;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static DnsTextResult FromEncodeFailure(DnsEncodeResult encoded)
    {
        DnsTextResult result = new DnsTextResult();
        int index = 0;
        while (index < encoded.Diagnostics.Count)
        {
            result.Diagnostics.Add(encoded.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsTextOptions CopyOptions(DnsTextOptions source)
    {
        DnsTextOptions result = new DnsTextOptions();
        result.Canonical = source.Canonical;
        result.UseMnemonicCodes = source.UseMnemonicCodes;
        result.IncludeHeader = source.IncludeHeader;
        result.IncludeSectionNames = source.IncludeSectionNames;
        result.MultilineRecords = source.MultilineRecords;
        result.PreserveInputFormatting = source.PreserveInputFormatting;
        result.Origin = source.Origin;
        result.IndentSize = source.IndentSize;
        result.MaximumLineLength = source.MaximumLineLength;
        return result;
    }

    static void AppendLine(StringBuilder builder, string line)
    {
        if (builder.Length > 0) builder.Append('\n');
        builder.Append(line);
    }

    static DnsTextResult ZoneFailure(string reason)
    {
        DnsTextResult result = new DnsTextResult();
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidZone;
        diagnostic.Reason = reason;
        result.ZoneDiagnostics.Add(diagnostic);
        return result;
    }
}
