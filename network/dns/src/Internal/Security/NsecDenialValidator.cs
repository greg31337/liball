using System.Collections.Generic;

namespace dns;

internal class DnsNsecProofRequest
{
    internal DnsName Zone = new DnsName();
    internal DnsName QueryName = new DnsName();
    internal int QueryTypeValue;
    internal int ClassValue = 1;
    internal bool NameError;
    internal bool WildcardAnswer;
    internal DnsSecurityStatus ProofStatus = DnsSecurityStatus.Secure;
    internal List<DnsName> ExistingNames = new List<DnsName>();
    internal List<DnsResourceRecord> Records =
        new List<DnsResourceRecord>();
    internal DnsSecurityPolicy Policy = new DnsSecurityPolicy();
}

internal class DnsNsecRecordValue
{
    internal DnsResourceRecord Record = new DnsResourceRecord();
    internal DnsName Owner = new DnsName();
    internal DnsName Next = new DnsName();
    internal List<int> Types = new List<int>();
}

internal class DnsNsecCollectionResult
{
    internal bool Success;
    internal List<DnsNsecRecordValue> Values =
        new List<DnsNsecRecordValue>();
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal static class NsecDenialValidator
{
    internal static DnsSecurityResult SelectWildcardExpansionProof(
        DnsNsecProofRequest request)
    {
        DnsSecurityResult input = ValidateRequest(request);
        if (!input.Success)
        {
            return input;
        }

        if (request.ProofStatus != DnsSecurityStatus.Secure)
        {
            return CreateStatus(request.ProofStatus);
        }

        if (request.Records.Count > request.Policy.MaximumProofRecords)
        {
            return CreateFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "policy.maximumProofRecords",
                "The NSEC proof-record limit was exceeded.");
        }

        DnsNsecCollectionResult collected = CollectRecords(request);
        if (!collected.Success)
        {
            return FromCollectionFailure(collected);
        }

        DnsName? closest = FindClosestEncloser(
            request,
            collected.Values);
        if (closest == null || RecordSetOperations.NamesEqual(
                closest,
                request.QueryName))
        {
            return Bogus(
                "nsec.wildcard",
                "A wildcard expansion requires a proper closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsNsecRecordValue? proof = FindCoveringRecord(
            nextCloser,
            collected.Values);
        if (proof == null)
        {
            return Bogus(
                "nsec.wildcard",
                "A wildcard expansion requires denial of the next-closer name.");
        }

        DnsSecurityResult result = CreateStatus(DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, proof.Record);
        return result;
    }

    internal static DnsSecurityResult ValidateDenial(
        DnsNsecProofRequest request)
    {
        DnsSecurityResult input = ValidateRequest(request);
        if (!input.Success)
        {
            return input;
        }

        if (request.ProofStatus != DnsSecurityStatus.Secure)
        {
            return CreateStatus(request.ProofStatus);
        }

        if (!request.Policy.ValidateDenial)
        {
            return CreateStatus(DnsSecurityStatus.Indeterminate);
        }

        if (request.Records.Count > request.Policy.MaximumProofRecords)
        {
            return CreateFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "policy.maximumProofRecords",
                "The NSEC proof-record limit was exceeded.");
        }

        DnsNsecCollectionResult collected = CollectRecords(request);
        if (!collected.Success)
        {
            return FromCollectionFailure(collected);
        }

        if (request.NameError)
        {
            return ValidateNameError(request, collected.Values);
        }

        if (request.WildcardAnswer)
        {
            return ValidateWildcardNoData(request, collected.Values);
        }

        return ValidateExactNoData(request, collected.Values);
    }

    static DnsSecurityResult ValidateNameError(
        DnsNsecProofRequest request,
        List<DnsNsecRecordValue> values)
    {
        if (NameExists(request.QueryName, request, values))
        {
            return Bogus(
                "nsec.nameError",
                "NXDOMAIN cannot be proven because the query name exists.");
        }

        DnsName? closest = FindClosestEncloser(request, values);
        if (closest == null)
        {
            return Bogus(
                "nsec.closestEncloser",
                "The NSEC records do not establish a closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsName wildcard = CreateWildcard(closest);
        DnsNsecRecordValue? nextProof = FindCoveringRecord(
            nextCloser,
            values);
        DnsNsecRecordValue? wildcardProof = FindCoveringRecord(
            wildcard,
            values);
        if (nextProof == null || wildcardProof == null)
        {
            return Bogus(
                "nsec.nameError",
                "NXDOMAIN requires proofs for the next-closer and wildcard names.");
        }

        DnsSecurityResult result = CreateStatus(DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, nextProof.Record);
        AddProof(result.ProofRecords, wildcardProof.Record);
        return result;
    }

    static DnsSecurityResult ValidateExactNoData(
        DnsNsecProofRequest request,
        List<DnsNsecRecordValue> values)
    {
        DnsNsecRecordValue? exact = FindExactRecord(
            request.QueryName,
            values);
        if (exact == null)
        {
            return Bogus(
                "nsec.noData",
                "NODATA requires an NSEC record at the query name.");
        }

        if (!BitmapDeniesType(exact.Types, request.QueryTypeValue))
        {
            return Bogus(
                "nsec.typeBitmap",
                "The NSEC bitmap contains the query type or a CNAME.");
        }

        DnsSecurityResult result = CreateStatus(DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, exact.Record);
        return result;
    }

    static DnsSecurityResult ValidateWildcardNoData(
        DnsNsecProofRequest request,
        List<DnsNsecRecordValue> values)
    {
        if (NameExists(request.QueryName, request, values))
        {
            return Bogus(
                "nsec.wildcard",
                "A wildcard proof cannot apply when the query name exists.");
        }

        DnsName? closest = FindClosestEncloser(request, values);
        if (closest == null)
        {
            return Bogus(
                "nsec.closestEncloser",
                "The wildcard proof has no established closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsName wildcard = CreateWildcard(closest);
        DnsNsecRecordValue? nextProof = FindCoveringRecord(
            nextCloser,
            values);
        DnsNsecRecordValue? wildcardRecord = FindExactRecord(
            wildcard,
            values);
        if (nextProof == null || wildcardRecord == null)
        {
            return Bogus(
                "nsec.wildcard",
                "Wildcard NODATA requires next-closer denial and an exact wildcard NSEC record.");
        }

        if (!BitmapDeniesType(
                wildcardRecord.Types,
                request.QueryTypeValue))
        {
            return Bogus(
                "nsec.typeBitmap",
                "The wildcard NSEC bitmap contains the query type or a CNAME.");
        }

        DnsSecurityResult result = CreateStatus(DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, nextProof.Record);
        AddProof(result.ProofRecords, wildcardRecord.Record);
        return result;
    }

    static DnsNsecCollectionResult CollectRecords(
        DnsNsecProofRequest request)
    {
        DnsNsecCollectionResult result =
            new DnsNsecCollectionResult();
        int index = 0;
        while (index < request.Records.Count)
        {
            DnsResourceRecord record = request.Records[index];
            if (record == null || record.Data == null ||
                record.Data.Nsec == null ||
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) !=
                    (int)DnsRecordType.Nsec ||
                EffectiveValue(record.ClassValue, (int)record.Class) !=
                    request.ClassValue)
            {
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.InvalidRecordData,
                    "nsec.record",
                    "Every denial proof must be a typed NSEC record in the query class.");
                return result;
            }

            DnsNsecData nsec = record.Data.Nsec;
            if (!NameWithinZone(record.Name, request.Zone) ||
                !NameWithinZone(nsec.NextDomainName, request.Zone) ||
                !BitmapIsValid(nsec.RecordTypeValues))
            {
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "nsec",
                    "NSEC owners, successors, and canonical type bitmaps must be valid within the zone.");
                return result;
            }

            DnsNsecRecordValue value = new DnsNsecRecordValue();
            value.Record = record;
            value.Owner = record.Name;
            value.Next = nsec.NextDomainName;
            value.Types = nsec.RecordTypeValues;
            result.Values.Add(value);
            index = index + 1;
        }

        result.Success = true;
        return result;
    }

    static DnsSecurityResult ValidateRequest(DnsNsecProofRequest request)
    {
        if (request == null || request.Zone == null ||
            request.QueryName == null || request.ExistingNames == null ||
            request.Records == null || request.Policy == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "nsec",
                "Complete NSEC denial input is required.");
        }

        if (!NameWithinZone(request.QueryName, request.Zone) ||
            request.QueryTypeValue < 1 ||
            request.QueryTypeValue > 65535 ||
            request.ClassValue < 0 || request.ClassValue > 65535 ||
            request.Policy.MaximumProofRecords < 1)
        {
            return CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "nsec.query",
                "The NSEC query, zone, class, type, and proof limit must be valid.");
        }

        int index = 0;
        while (index < request.ExistingNames.Count)
        {
            if (!NameWithinZone(
                    request.ExistingNames[index],
                    request.Zone))
            {
                return CreateFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "nsec.existingNames",
                    "Every authenticated existing name must be inside the zone.");
            }

            index = index + 1;
        }

        return CreateStatus(DnsSecurityStatus.Indeterminate);
    }

    static bool BitmapIsValid(List<int> types)
    {
        if (types == null || types.Count == 0)
        {
            return false;
        }

        bool hasNsec = false;
        bool hasRrsig = false;
        int previous = -1;
        int index = 0;
        while (index < types.Count)
        {
            int value = types[index];
            if (value < 1 || value > 65535 || value <= previous)
            {
                return false;
            }

            if (value == (int)DnsRecordType.Nsec)
            {
                hasNsec = true;
            }

            if (value == (int)DnsRecordType.Rrsig)
            {
                hasRrsig = true;
            }

            previous = value;
            index = index + 1;
        }

        return hasNsec && hasRrsig;
    }

    static bool BitmapDeniesType(List<int> types, int queryTypeValue)
    {
        int index = 0;
        while (index < types.Count)
        {
            if (types[index] == queryTypeValue ||
                types[index] == (int)DnsRecordType.Cname)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static DnsName? FindClosestEncloser(
        DnsNsecProofRequest request,
        List<DnsNsecRecordValue> values)
    {
        DnsName current = request.QueryName;
        while (NameWithinZone(current, request.Zone))
        {
            if (NameExists(current, request, values))
            {
                return current;
            }

            if (RecordSetOperations.NamesEqual(current, request.Zone))
            {
                return request.Zone;
            }

            DnsNameOperationResult parent =
                DnsNameOperations.GetParentName(current);
            if (!parent.Success || parent.Name == null)
            {
                return null;
            }

            current = parent.Name;
        }

        return null;
    }

    static bool NameExists(
        DnsName name,
        DnsNsecProofRequest request,
        List<DnsNsecRecordValue> values)
    {
        if (RecordSetOperations.NamesEqual(name, request.Zone))
        {
            return true;
        }

        int index = 0;
        while (index < request.ExistingNames.Count)
        {
            if (RecordSetOperations.NamesEqual(
                    name,
                    request.ExistingNames[index]))
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < values.Count)
        {
            if (RecordSetOperations.NamesEqual(name, values[index].Owner))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsNsecRecordValue? FindExactRecord(
        DnsName name,
        List<DnsNsecRecordValue> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (RecordSetOperations.NamesEqual(name, values[index].Owner))
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsNsecRecordValue? FindCoveringRecord(
        DnsName name,
        List<DnsNsecRecordValue> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (CoversName(values[index], name))
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static bool CoversName(DnsNsecRecordValue value, DnsName name)
    {
        int ownerToNext = RecordSetOperations.CompareNames(
            value.Owner,
            value.Next);
        int ownerToName = RecordSetOperations.CompareNames(
            value.Owner,
            name);
        int nameToNext = RecordSetOperations.CompareNames(
            name,
            value.Next);
        if (ownerToName == 0 || nameToNext == 0)
        {
            return false;
        }

        if (ownerToNext < 0)
        {
            return ownerToName < 0 && nameToNext < 0;
        }

        if (ownerToNext > 0)
        {
            return ownerToName < 0 || nameToNext < 0;
        }

        return true;
    }

    static bool NameWithinZone(DnsName name, DnsName zone)
    {
        if (name == null || zone == null)
        {
            return false;
        }

        DnsNameOperationResult suffix =
            DnsNameOperations.HasNameSuffix(name, zone);
        return suffix.Success && suffix.BooleanValue &&
            name.IsAbsolute && zone.IsAbsolute;
    }

    static DnsName CreateNextCloser(DnsName query, DnsName closest)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        int start = query.Labels.Count - closest.Labels.Count - 1;
        int index = start;
        while (index < query.Labels.Count)
        {
            result.Labels.Add(RecordSetOperations.CopyBytes(
                query.Labels[index]));
            index = index + 1;
        }

        return result;
    }

    static DnsName CreateWildcard(DnsName closest)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        result.Labels.Add([42]);
        int index = 0;
        while (index < closest.Labels.Count)
        {
            result.Labels.Add(RecordSetOperations.CopyBytes(
                closest.Labels[index]));
            index = index + 1;
        }

        return result;
    }

    static void AddProof(
        List<DnsResourceRecord> proofs,
        DnsResourceRecord record)
    {
        int index = 0;
        while (index < proofs.Count)
        {
            if (proofs[index] == record)
            {
                return;
            }

            index = index + 1;
        }

        proofs.Add(record);
    }

    static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }

    static DnsSecurityResult FromCollectionFailure(
        DnsNsecCollectionResult source)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = DnsSecurityStatus.Indeterminate;
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult Bogus(string field, string reason)
    {
        return CreateDiagnosticStatus(
            DnsSecurityStatus.Bogus,
            DnsDiagnosticCode.InvalidRecordData,
            field,
            reason);
    }

    static DnsSecurityResult CreateStatus(DnsSecurityStatus status)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Success = true;
        result.Status = status;
        return result;
    }

    static DnsSecurityResult CreateDiagnosticStatus(
        DnsSecurityStatus status,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsSecurityResult result = CreateStatus(status);
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsSecurityResult CreateFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = DnsSecurityStatus.Indeterminate;
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static void AddDiagnostic(
        List<DnsDiagnostic> diagnostics,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        diagnostics.Add(diagnostic);
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
}
