using System.Collections.Generic;

namespace dns;

internal class DnsCanonicalNameResult
{
    internal bool Success;
    internal DnsName? Name;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class DnsCanonicalRecord
{
    internal byte[] RecordData = [];
    internal byte[] WireData = [];
}

internal class DnsCanonicalRecordSetResult
{
    internal bool Success;
    internal DnsName? Owner;
    internal List<DnsCanonicalRecord> Records =
        new List<DnsCanonicalRecord>();
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal static class CanonicalDnsData
{
    internal static DnsEncodeResult EncodeCanonicalName(DnsName name)
    {
        DnsEncodeOptions options = CreateCanonicalOptions();
        return StandaloneDnsNameCodec.EncodeName(name, options);
    }

    internal static DnsEncodeResult EncodeCanonicalRecordData(
        DnsResourceRecord record)
    {
        if (record == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "record",
                "A resource record is required for canonical encoding.");
        }

        int typeValue = EffectiveValue(
            record.RecordTypeValue,
            (int)record.RecordType);
        if (typeValue < 0 || typeValue > 65535)
        {
            return CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "record.type",
                "The resource-record type must fit in sixteen bits.");
        }

        if (record.UseRawData)
        {
            if (record.RawData == null)
            {
                return CreateFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "record.rawData",
                    "Raw resource-record data is required.");
            }

            return CreateSuccess(
                RecordSetOperations.CopyBytes(record.RawData));
        }

        if (record.Data == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "record.data",
                "Typed resource-record data is required for canonical encoding.");
        }

        return ResourceRecordCodec.EncodeRecordData(
            typeValue,
            record.Data,
            CreateCanonicalOptions());
    }

    internal static DnsCanonicalNameResult ReconstructWildcardOwner(
        DnsName owner,
        int labels)
    {
        if (owner == null)
        {
            return CreateNameFailure(
                "owner",
                "An RRset owner name is required.");
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(owner);
        if (!validated.Success || validated.Name == null)
        {
            return CreateNameFailure(
                "owner",
                "The RRset owner name is invalid.");
        }

        DnsName validatedOwner = validated.Name;
        if (!validatedOwner.IsAbsolute)
        {
            return CreateNameFailure(
                "owner",
                "A DNSSEC RRset owner name must be absolute.");
        }

        if (labels < 0 || labels > validatedOwner.Labels.Count)
        {
            return CreateNameFailure(
                "rrsig.labels",
                "The RRSIG label count exceeds the RRset owner label count.");
        }

        DnsName resultName = new DnsName();
        resultName.IsAbsolute = true;
        if (labels < validatedOwner.Labels.Count)
        {
            resultName.Labels.Add([42]);
        }

        int index = validatedOwner.Labels.Count - labels;
        while (index < validatedOwner.Labels.Count)
        {
            resultName.Labels.Add(RecordSetOperations.CopyBytes(
                validatedOwner.Labels[index]));
            index = index + 1;
        }

        RawDnsNameResult reconstructed = RawDnsName.ValidateName(resultName);
        if (!reconstructed.Success || reconstructed.Name == null)
        {
            return CreateNameFailure(
                "rrsig.labels",
                "The RRSIG wildcard owner reconstruction is invalid.");
        }

        DnsCanonicalNameResult result = new DnsCanonicalNameResult();
        result.Success = true;
        result.Name = RecordSetOperations.CopyName(
            reconstructed.Name,
            true);
        return result;
    }

    internal static DnsCanonicalRecordSetResult CanonicalizeRecordSet(
        DnsResourceRecordSet recordSet,
        DnsSignatureData signature)
    {
        if (recordSet == null || signature == null ||
            recordSet.Records == null)
        {
            return CreateRecordSetFailure(
                "recordSet",
                "A complete RRset and RRSIG data are required.");
        }

        int typeValue = EffectiveValue(
            recordSet.RecordTypeValue,
            (int)recordSet.RecordType);
        int classValue = EffectiveValue(
            recordSet.ClassValue,
            (int)recordSet.Class);
        int coveredTypeValue = EffectiveValue(
            signature.TypeCoveredValue,
            (int)signature.TypeCovered);
        if (typeValue < 0 || typeValue > 65535 ||
            classValue < 0 || classValue > 65535 ||
            coveredTypeValue != typeValue)
        {
            return CreateRecordSetFailure(
                "rrsig.typeCovered",
                "The RRSIG covered type must match a valid RRset type and class.");
        }

        if (recordSet.Records.Count == 0)
        {
            return CreateRecordSetFailure(
                "recordSet.records",
                "A DNSSEC canonical RRset requires at least one record.");
        }

        DnsCanonicalNameResult owner = ReconstructWildcardOwner(
            recordSet.Name,
            signature.Labels);
        if (!owner.Success || owner.Name == null)
        {
            return FromNameFailure(owner);
        }

        DnsCanonicalRecordSetResult result =
            new DnsCanonicalRecordSetResult();
        result.Owner = owner.Name;
        int index = 0;
        while (index < recordSet.Records.Count)
        {
            DnsResourceRecord record = recordSet.Records[index];
            if (record == null || !RecordBelongsToSet(
                    record,
                    recordSet,
                    typeValue,
                    classValue))
            {
                return CreateRecordSetFailure(
                    "recordSet.records",
                    "Every canonical record must belong to the supplied RRset.");
            }

            DnsEncodeResult recordData = EncodeCanonicalRecordData(record);
            if (!recordData.Success)
            {
                return FromEncodeFailure(recordData);
            }

            DnsResourceRecord canonicalRecord =
                CreateCanonicalRecord(
                    record,
                    owner.Name,
                    typeValue,
                    classValue,
                    signature.OriginalTimeToLive);
            DnsEncodeResult wireData =
                ResourceRecordCodec.EncodeResourceRecord(
                    canonicalRecord,
                    CreateCanonicalOptions());
            if (!wireData.Success)
            {
                return FromEncodeFailure(wireData);
            }

            DnsCanonicalRecord canonical = new DnsCanonicalRecord();
            canonical.RecordData = recordData.Bytes;
            canonical.WireData = wireData.Bytes;
            InsertCanonicalRecord(result.Records, canonical);
            index = index + 1;
        }

        result.Success = true;
        return result;
    }

    internal static DnsEncodeResult BuildRrsigSignedData(
        DnsResourceRecordSet recordSet,
        DnsSignatureData signature)
    {
        DnsCanonicalRecordSetResult canonical = CanonicalizeRecordSet(
            recordSet,
            signature);
        if (!canonical.Success)
        {
            return FromRecordSetFailure(canonical);
        }

        DnsEncodeResult signaturePrefix = EncodeRrsigPrefix(signature);
        if (!signaturePrefix.Success)
        {
            return signaturePrefix;
        }

        long totalLength = signaturePrefix.Bytes.Length;
        int index = 0;
        while (index < canonical.Records.Count)
        {
            totalLength = totalLength +
                canonical.Records[index].WireData.Length;
            if (totalLength > int.MaxValue)
            {
                return CreateFailure(
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "signedData",
                    "The DNSSEC signed data exceeds the supported length.");
            }

            index = index + 1;
        }

        byte[] bytes = new byte[(int)totalLength];
        int offset = 0;
        System.Array.Copy(
            signaturePrefix.Bytes,
            0,
            bytes,
            offset,
            signaturePrefix.Bytes.Length);
        offset = offset + signaturePrefix.Bytes.Length;
        index = 0;
        while (index < canonical.Records.Count)
        {
            byte[] record = canonical.Records[index].WireData;
            System.Array.Copy(record, 0, bytes, offset, record.Length);
            offset = offset + record.Length;
            index = index + 1;
        }

        return CreateSuccess(bytes);
    }

    static DnsEncodeResult EncodeRrsigPrefix(DnsSignatureData signature)
    {
        if (signature == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "rrsig",
                "RRSIG data is required.");
        }

        DnsSignatureData prefix = new DnsSignatureData();
        prefix.TypeCovered = signature.TypeCovered;
        prefix.TypeCoveredValue = signature.TypeCoveredValue;
        prefix.Algorithm = signature.Algorithm;
        prefix.AlgorithmValue = signature.AlgorithmValue;
        prefix.Labels = signature.Labels;
        prefix.OriginalTimeToLive = signature.OriginalTimeToLive;
        prefix.SignatureExpiration = signature.SignatureExpiration;
        prefix.SignatureInception = signature.SignatureInception;
        prefix.KeyTag = signature.KeyTag;
        prefix.SignerName = signature.SignerName;
        prefix.Signature = [];
        DnsRecordData data = new DnsRecordData();
        data.Signature = prefix;
        return ResourceRecordCodec.EncodeRecordData(
            (int)DnsRecordType.Rrsig,
            data,
            CreateCanonicalOptions());
    }

    static DnsResourceRecord CreateCanonicalRecord(
        DnsResourceRecord source,
        DnsName owner,
        int typeValue,
        int classValue,
        uint originalTimeToLive)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = owner;
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.TimeToLive = originalTimeToLive;
        result.Data = source.Data;
        result.RawData = source.RawData;
        result.UseRawData = source.UseRawData;
        return result;
    }

    static bool RecordBelongsToSet(
        DnsResourceRecord record,
        DnsResourceRecordSet recordSet,
        int typeValue,
        int classValue)
    {
        int recordTypeValue = EffectiveValue(
            record.RecordTypeValue,
            (int)record.RecordType);
        int recordClassValue = EffectiveValue(
            record.ClassValue,
            (int)record.Class);
        return recordTypeValue == typeValue &&
            recordClassValue == classValue &&
            RecordSetOperations.NamesEqual(record.Name, recordSet.Name);
    }

    static void InsertCanonicalRecord(
        List<DnsCanonicalRecord> records,
        DnsCanonicalRecord record)
    {
        int index = 0;
        while (index < records.Count &&
            RecordSetOperations.CompareBytes(
                records[index].RecordData,
                record.RecordData) < 0)
        {
            index = index + 1;
        }

        if (index < records.Count &&
            RecordSetOperations.BytesEqual(
                records[index].RecordData,
                record.RecordData))
        {
            return;
        }

        records.Insert(index, record);
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

    static DnsEncodeResult CreateSuccess(byte[] bytes)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = bytes;
        result.BytesWritten = bytes.Length;
        return result;
    }

    static DnsEncodeResult CreateFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsCanonicalNameResult CreateNameFailure(
        string field,
        string reason)
    {
        DnsCanonicalNameResult result = new DnsCanonicalNameResult();
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsCanonicalRecordSetResult CreateRecordSetFailure(
        string field,
        string reason)
    {
        DnsCanonicalRecordSetResult result =
            new DnsCanonicalRecordSetResult();
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsCanonicalRecordSetResult FromNameFailure(
        DnsCanonicalNameResult source)
    {
        DnsCanonicalRecordSetResult result =
            new DnsCanonicalRecordSetResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsCanonicalRecordSetResult FromEncodeFailure(
        DnsEncodeResult source)
    {
        DnsCanonicalRecordSetResult result =
            new DnsCanonicalRecordSetResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsEncodeResult FromRecordSetFailure(
        DnsCanonicalRecordSetResult source)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static void AppendDiagnostics(
        List<DnsDiagnostic> destination,
        List<DnsDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }
}
