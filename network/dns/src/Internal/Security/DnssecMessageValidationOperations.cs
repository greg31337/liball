using System.Collections.Generic;

namespace dns;

internal static class DnssecMessageValidationOperations
{
    internal static DnsSecurityResult ValidateMessage(
        DnsMessage message,
        DnsSecurityPolicy policy)
    {
        if (message == null || policy == null ||
            message.Answers == null || message.Authorities == null ||
            message.Additionals == null || policy.TrustAnchors == null)
        {
            return Failure(
                "validation",
                "A complete DNS message and DNSSEC policy are required.");
        }

        List<DnsResourceRecord> all = new List<DnsResourceRecord>();
        AppendRecords(all, message.Answers);
        AppendRecords(all, message.Authorities);
        AppendRecords(all, message.Additionals);
        List<DnsResourceRecord> dnskeys = FindRecords(
            all,
            (int)DnsRecordType.Dnskey);
        List<DnsResourceRecordSet> keySets =
            RecordSetOperations.GroupRecordSets(dnskeys);
        List<DnsKeyMaterial> trustedKeys =
            new List<DnsKeyMaterial>();
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Indeterminate);
        int index = 0;
        while (index < keySets.Count)
        {
            DnsResourceRecordSet keySet = keySets[index];
            DnssecZoneCutRequest request = new DnssecZoneCutRequest();
            request.Zone = keySet.Name;
            request.ClassValue = EffectiveValue(
                keySet.ClassValue,
                (int)keySet.Class);
            request.DnskeyRecordSet = keySet;
            request.Signatures = FindSignatures(
                all,
                keySet.Name,
                (int)DnsRecordType.Dnskey,
                request.ClassValue);
            request.Policy = policy;
            DnsSecurityResult trusted =
                DnssecChainValidator.ValidateTrustAnchors(request);
            AppendDiagnostics(result.Diagnostics, trusted.Diagnostics);
            if (trusted.Status == DnsSecurityStatus.Secure)
            {
                List<DnsKeyMaterial> extracted =
                    DnssecChainValidator.ExtractKeys(
                        keySet.Name,
                        request.ClassValue,
                        keySet);
                AppendKeys(trustedKeys, extracted);
                AppendAnchors(result.TrustAnchors, trusted.TrustAnchors);
                result.ValidatedRecordSets.Add(keySet);
            }
            else if (trusted.Status == DnsSecurityStatus.Bogus ||
                trusted.Status == DnsSecurityStatus.UnsupportedAlgorithm)
            {
                result.Status = trusted.Status;
                result.Success = true;
                return result;
            }

            index = index + 1;
        }

        if (trustedKeys.Count == 0)
        {
            if (result.Diagnostics.Count == 0)
            {
                AddDiagnostic(
                    result.Diagnostics,
                    "trustAnchor",
                    "No DNSKEY RRset matched a configured trust anchor.");
            }

            result.Success = true;
            return result;
        }

        List<DnsResourceRecord> covered =
            new List<DnsResourceRecord>();
        AppendRecordsExceptSignaturesAndKeys(covered, message.Answers);
        AppendRecordsExceptSignaturesAndKeys(covered, message.Authorities);
        List<DnsResourceRecordSet> sets =
            RecordSetOperations.GroupRecordSets(covered);
        index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            int typeValue = EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            int classValue = EffectiveValue(
                set.ClassValue,
                (int)set.Class);
            List<DnsSignatureData> signatures = FindSignatures(
                all,
                set.Name,
                typeValue,
                classValue);
            DnsSecurityResult validated =
                DnssecRecordSetValidator.ValidateRecordSet(
                    set,
                    trustedKeys,
                    signatures,
                    policy);
            AppendDiagnostics(result.Diagnostics, validated.Diagnostics);
            if (validated.Status != DnsSecurityStatus.Secure)
            {
                result.Status = validated.Status ==
                    DnsSecurityStatus.Insecure ?
                    DnsSecurityStatus.Bogus : validated.Status;
                result.Success = true;
                return result;
            }

            result.ValidatedRecordSets.Add(set);
            index = index + 1;
        }

        result.Status = DnsSecurityStatus.Secure;
        result.Success = true;
        result.Message = message;
        return result;
    }

    static List<DnsResourceRecord> FindRecords(
        List<DnsResourceRecord> records,
        int typeValue)
    {
        List<DnsResourceRecord> result =
            new List<DnsResourceRecord>();
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (record != null && EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) == typeValue)
            {
                result.Add(record);
            }

            index = index + 1;
        }

        return result;
    }

    static List<DnsSignatureData> FindSignatures(
        List<DnsResourceRecord> records,
        DnsName owner,
        int typeValue,
        int classValue)
    {
        List<DnsSignatureData> result =
            new List<DnsSignatureData>();
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (record != null && record.Data != null &&
                record.Data.Signature != null &&
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) ==
                    (int)DnsRecordType.Rrsig &&
                EffectiveValue(record.ClassValue, (int)record.Class) ==
                    classValue &&
                RecordSetOperations.NamesEqual(record.Name, owner) &&
                EffectiveValue(
                    record.Data.Signature.TypeCoveredValue,
                    (int)record.Data.Signature.TypeCovered) == typeValue)
            {
                result.Add(record.Data.Signature);
            }

            index = index + 1;
        }

        return result;
    }

    static void AppendRecordsExceptSignaturesAndKeys(
        List<DnsResourceRecord> destination,
        List<DnsResourceRecord> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            DnsResourceRecord record = source[index];
            int typeValue = EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue != (int)DnsRecordType.Rrsig &&
                typeValue != (int)DnsRecordType.Dnskey)
            {
                destination.Add(record);
            }

            index = index + 1;
        }
    }

    static void AppendRecords(
        List<DnsResourceRecord> destination,
        List<DnsResourceRecord> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void AppendKeys(
        List<DnsKeyMaterial> destination,
        List<DnsKeyMaterial> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void AppendAnchors(
        List<DnsTrustAnchor> destination,
        List<DnsTrustAnchor> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
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

    static DnsSecurityResult CreateStatus(DnsSecurityStatus status)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = status;
        return result;
    }

    static DnsSecurityResult Failure(string field, string reason)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        AddDiagnostic(result.Diagnostics, field, reason);
        return result;
    }

    static void AddDiagnostic(
        List<DnsDiagnostic> diagnostics,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        diagnostics.Add(diagnostic);
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
