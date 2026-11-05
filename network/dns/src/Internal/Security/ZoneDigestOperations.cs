using System.Collections.Generic;
using System.Security.Cryptography;

namespace dns;

internal class DnsZoneDigestRecord
{
    internal DnsName Name = new DnsName();
    internal int RecordTypeValue;
    internal byte[] RecordData = [];
    internal byte[] WireData = [];
}

internal class DnsZoneDigestState
{
    internal DnsZone Zone = new DnsZone();
    internal DnsZoneDigestResult Result = new DnsZoneDigestResult();
    internal List<DnsZoneNode> Nodes = new List<DnsZoneNode>();
    internal List<DnsZoneDigestRecord> Records =
        new List<DnsZoneDigestRecord>();
    internal int ZoneClassValue;
    internal int SoaCount;
}

internal static class ZoneDigestOperations
{
    internal static DnsZoneDigestResult Calculate(
        DnsZone zone,
        int scheme,
        int hashAlgorithm)
    {
        DnsZoneDigestResult result = new DnsZoneDigestResult();
        result.Scheme = scheme;
        result.HashAlgorithm = hashAlgorithm;
        if (zone == null)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidZone,
                "zone",
                "A complete zone is required for digest calculation.");
        }

        if (scheme != 1)
        {
            return Fail(
                result,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "scheme",
                "Only the ZONEMD SIMPLE scheme is supported.");
        }

        if (hashAlgorithm != 1 && hashAlgorithm != 2)
        {
            return Fail(
                result,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "hashAlgorithm",
                "Only the ZONEMD SHA-384 and SHA-512 hash algorithms are supported.");
        }

        RawDnsNameResult apex = RawDnsName.ValidateName(zone.Apex);
        if (!apex.Success || apex.Name == null || !apex.Name.IsAbsolute)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidZone,
                "zone.apex",
                "The zone apex must be a valid absolute DNS name.");
        }

        int classValue = RecordSetOperations.EffectiveValue(
            zone.ClassValue,
            (int)zone.Class);
        if (classValue < 0 || classValue > 65535 || zone.Nodes == null)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidZone,
                "zone",
                "The zone class and node collection must be valid.");
        }

        DnsZoneDigestState state = new DnsZoneDigestState();
        state.Zone = zone;
        state.Result = result;
        state.ZoneClassValue = classValue;
        int nodeIndex = 0;
        while (nodeIndex < zone.Nodes.Count)
        {
            if (!CollectNode(state, zone.Nodes[nodeIndex]))
            {
                return result;
            }

            nodeIndex = nodeIndex + 1;
        }

        nodeIndex = 0;
        while (nodeIndex < state.Nodes.Count)
        {
            if (!CollectNodeRecords(state, state.Nodes[nodeIndex]))
            {
                return result;
            }

            nodeIndex = nodeIndex + 1;
        }

        if (state.SoaCount != 1)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidZone,
                "zone.soa",
                "The zone apex must contain exactly one typed SOA record.");
        }

        result.Digest = ComputeDigest(state.Records, hashAlgorithm);
        result.IncludedRecordCount = state.Records.Count;
        DnsZonemdData recordData = new DnsZonemdData();
        recordData.Serial = result.Serial;
        recordData.Scheme = scheme;
        recordData.HashAlgorithm = hashAlgorithm;
        recordData.Digest = RecordSetOperations.CopyBytes(result.Digest);
        result.RecordData = recordData;
        result.Success = true;
        return result;
    }

    internal static DnsZoneDigestResult Verify(
        DnsZone zone,
        DnsZonemdData expectedDigest)
    {
        if (expectedDigest == null)
        {
            DnsZoneDigestResult missing = new DnsZoneDigestResult();
            return Fail(
                missing,
                DnsDiagnosticCode.InvalidRecordData,
                "expectedDigest",
                "Expected ZONEMD data is required for verification.");
        }

        DnsZoneDigestResult result = Calculate(
            zone,
            expectedDigest.Scheme,
            expectedDigest.HashAlgorithm);
        if (!result.Success)
        {
            return result;
        }

        int requiredLength = 48;
        if (expectedDigest.HashAlgorithm == 2)
        {
            requiredLength = 64;
        }

        if (expectedDigest.Digest == null ||
            expectedDigest.Digest.Length != requiredLength)
        {
            result.Success = false;
            return Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "expectedDigest.digest",
                "The expected digest length does not match its hash algorithm.");
        }

        if (expectedDigest.Serial != result.Serial)
        {
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "expectedDigest.serial",
                "The ZONEMD serial does not match the zone SOA serial.");
            return result;
        }

        result.Verified = FixedTimeEquals(
            result.Digest,
            expectedDigest.Digest);
        if (!result.Verified)
        {
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "expectedDigest.digest",
                "The calculated zone digest does not match the expected digest.");
        }

        return result;
    }

    static bool CollectNode(
        DnsZoneDigestState state,
        DnsZoneNode node)
    {
        if (node == null)
        {
            Fail(
                state.Result,
                DnsDiagnosticCode.InvalidZone,
                "zone.nodes",
                "A zone node cannot be null.");
            return false;
        }

        int existingIndex = 0;
        while (existingIndex < state.Nodes.Count)
        {
            if (object.ReferenceEquals(state.Nodes[existingIndex], node))
            {
                return true;
            }

            existingIndex = existingIndex + 1;
        }

        if (node.RecordSets == null || node.Children == null)
        {
            Fail(
                state.Result,
                DnsDiagnosticCode.InvalidZone,
                "zone.nodes",
                "Every zone node requires record-set and child collections.");
            return false;
        }

        state.Nodes.Add(node);
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            if (!CollectNode(state, node.Children[childIndex]))
            {
                return false;
            }

            childIndex = childIndex + 1;
        }

        return true;
    }

    static bool CollectNodeRecords(
        DnsZoneDigestState state,
        DnsZoneNode node)
    {
        RawDnsNameResult nodeName = RawDnsName.ValidateName(node.Name);
        if (!nodeName.Success || nodeName.Name == null ||
            !nodeName.Name.IsAbsolute)
        {
            Fail(
                state.Result,
                DnsDiagnosticCode.InvalidZone,
                "zone.node.name",
                "Every zone node must have a valid absolute owner name.");
            return false;
        }

        DnsNameOperationResult inZone = DnsNameOperations.HasNameSuffix(
            node.Name,
            state.Zone.Apex);
        if (!inZone.Success)
        {
            Fail(
                state.Result,
                DnsDiagnosticCode.InvalidZone,
                "zone.node.name",
                "A zone node owner name could not be compared with the apex.");
            return false;
        }

        int setIndex = 0;
        while (setIndex < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[setIndex];
            if (set == null || set.Records == null)
            {
                Fail(
                    state.Result,
                    DnsDiagnosticCode.InvalidZone,
                    "zone.recordSets",
                    "A complete resource-record set is required.");
                return false;
            }

            if (!inZone.BooleanValue)
            {
                state.Result.ExcludedRecordCount =
                    state.Result.ExcludedRecordCount + set.Records.Count;
            }
            else if (!CollectRecordSet(state, node, set))
            {
                return false;
            }

            setIndex = setIndex + 1;
        }

        return true;
    }

    static bool CollectRecordSet(
        DnsZoneDigestState state,
        DnsZoneNode node,
        DnsResourceRecordSet set)
    {
        int typeValue = RecordSetOperations.EffectiveValue(
            set.RecordTypeValue,
            (int)set.RecordType);
        int classValue = RecordSetOperations.EffectiveValue(
            set.ClassValue,
            (int)set.Class);
        if (typeValue < 0 || typeValue > 65535 ||
            classValue != state.ZoneClassValue ||
            !RecordSetOperations.NamesEqual(set.Name, node.Name))
        {
            Fail(
                state.Result,
                DnsDiagnosticCode.InvalidZone,
                "zone.recordSet",
                "Every in-zone record set must match its node, class, and valid type.");
            return false;
        }

        bool apex = RecordSetOperations.NamesEqual(
            set.Name,
            state.Zone.Apex);
        int recordIndex = 0;
        while (recordIndex < set.Records.Count)
        {
            DnsResourceRecord record = set.Records[recordIndex];
            if (!CollectRecord(
                    state,
                    set,
                    record,
                    typeValue,
                    classValue,
                    apex))
            {
                return false;
            }

            recordIndex = recordIndex + 1;
        }

        return true;
    }

    static bool CollectRecord(
        DnsZoneDigestState state,
        DnsResourceRecordSet set,
        DnsResourceRecord record,
        int typeValue,
        int classValue,
        bool apex)
    {
        if (record == null ||
            RecordSetOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType) != typeValue ||
            RecordSetOperations.EffectiveValue(
                record.ClassValue,
                (int)record.Class) != classValue ||
            !RecordSetOperations.NamesEqual(record.Name, set.Name))
        {
            Fail(
                state.Result,
                DnsDiagnosticCode.InvalidZone,
                "zone.records",
                "Every resource record must belong to its containing record set.");
            return false;
        }

        if (apex && typeValue == 6)
        {
            if (record.Data == null || record.Data.Soa == null)
            {
                Fail(
                    state.Result,
                    DnsDiagnosticCode.InvalidRecordData,
                    "zone.soa",
                    "The apex SOA record must contain typed SOA data.");
                return false;
            }

            state.SoaCount = state.SoaCount + 1;
            state.Result.Serial = record.Data.Soa.Serial;
        }

        if (apex && typeValue == 63)
        {
            state.Result.ExcludedRecordCount =
                state.Result.ExcludedRecordCount + 1;
            return true;
        }

        if (apex && typeValue == 46 && CoversZonemd(record))
        {
            state.Result.ExcludedRecordCount =
                state.Result.ExcludedRecordCount + 1;
            return true;
        }

        DnsEncodeResult recordData =
            CanonicalDnsData.EncodeCanonicalRecordData(record);
        if (!recordData.Success)
        {
            CopyDiagnostics(state.Result, recordData.Diagnostics);
            return false;
        }

        DnsResourceRecord canonical = new DnsResourceRecord();
        canonical.Name = RecordSetOperations.CopyName(record.Name, true);
        canonical.RecordType = (DnsRecordType)typeValue;
        canonical.RecordTypeValue = typeValue;
        canonical.Class = (DnsClass)classValue;
        canonical.ClassValue = classValue;
        canonical.TimeToLive = record.TimeToLive;
        canonical.Data = record.Data;
        canonical.RawData = record.RawData;
        canonical.UseRawData = record.UseRawData;
        DnsEncodeResult wireData = ResourceRecordCodec.EncodeResourceRecord(
            canonical,
            CreateCanonicalOptions());
        if (!wireData.Success)
        {
            CopyDiagnostics(state.Result, wireData.Diagnostics);
            return false;
        }

        DnsZoneDigestRecord digestRecord = new DnsZoneDigestRecord();
        digestRecord.Name = canonical.Name;
        digestRecord.RecordTypeValue = typeValue;
        digestRecord.RecordData = recordData.Bytes;
        digestRecord.WireData = wireData.Bytes;
        if (!InsertRecord(state.Records, digestRecord))
        {
            state.Result.ExcludedRecordCount =
                state.Result.ExcludedRecordCount + 1;
        }

        return true;
    }

    static bool CoversZonemd(DnsResourceRecord record)
    {
        if (record.UseRawData && record.RawData != null &&
            record.RawData.Length >= 2)
        {
            return record.RawData[0] == 0 && record.RawData[1] == 63;
        }

        if (record.Data == null || record.Data.Signature == null)
        {
            return false;
        }

        DnsSignatureData signature = record.Data.Signature;
        int covered = RecordSetOperations.EffectiveValue(
            signature.TypeCoveredValue,
            (int)signature.TypeCovered);
        return covered == 63;
    }

    static bool InsertRecord(
        List<DnsZoneDigestRecord> records,
        DnsZoneDigestRecord record)
    {
        int index = 0;
        int comparison = -1;
        while (index < records.Count)
        {
            comparison = CompareRecords(records[index], record);
            if (comparison >= 0)
            {
                break;
            }

            index = index + 1;
        }

        if (index < records.Count && comparison == 0)
        {
            return false;
        }

        records.Insert(index, record);
        return true;
    }

    static int CompareRecords(
        DnsZoneDigestRecord left,
        DnsZoneDigestRecord right)
    {
        int comparison = RecordSetOperations.CompareNames(
            left.Name,
            right.Name);
        if (comparison != 0)
        {
            return comparison;
        }

        if (left.RecordTypeValue != right.RecordTypeValue)
        {
            return left.RecordTypeValue < right.RecordTypeValue ? -1 : 1;
        }

        return RecordSetOperations.CompareBytes(
            left.RecordData,
            right.RecordData);
    }

    static byte[] ComputeDigest(
        List<DnsZoneDigestRecord> records,
        int hashAlgorithm)
    {
        HashAlgorithmName algorithm = HashAlgorithmName.SHA384;
        if (hashAlgorithm == 2)
        {
            algorithm = HashAlgorithmName.SHA512;
        }

        using (IncrementalHash hash = IncrementalHash.CreateHash(algorithm))
        {
            int index = 0;
            while (index < records.Count)
            {
                hash.AppendData(records[index].WireData);
                index = index + 1;
            }

            return hash.GetHashAndReset();
        }
    }

    static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    static DnsEncodeOptions CreateCanonicalOptions()
    {
        DnsEncodeOptions result = new DnsEncodeOptions();
        result.EnableCompression = false;
        result.Canonical = true;
        result.CanonicalOrder.LowercaseNames = true;
        result.CanonicalOrder.DisableCompression = true;
        result.CanonicalOrder.SortRecords = true;
        result.CanonicalOrder.NormalizeRecordTtl = false;
        result.CanonicalOrder.SuppressDuplicateRecords = true;
        return result;
    }

    static DnsZoneDigestResult Fail(
        DnsZoneDigestResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        result.Success = false;
        AddDiagnostic(result, code, field, reason);
        return result;
    }

    static void AddDiagnostic(
        DnsZoneDigestResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void CopyDiagnostics(
        DnsZoneDigestResult result,
        List<DnsDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
    }
}
