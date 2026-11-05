using System.Collections.Generic;

namespace dns;

internal class DnsNsec3HashResult
{
    internal bool Success;
    internal byte[] Hash = [];
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnsNsec3ProofRequest
{
    internal DnsName Zone = new DnsName();
    internal DnsName QueryName = new DnsName();
    internal int QueryTypeValue;
    internal int ClassValue = 1;
    internal bool NameError;
    internal bool WildcardAnswer;
    internal bool OptOutAllowed;
    internal DnsSecurityStatus ProofStatus = DnsSecurityStatus.Secure;
    internal List<DnsResourceRecord> Records =
        new List<DnsResourceRecord>();
    internal DnsSecurityPolicy Policy = new DnsSecurityPolicy();
}

internal class DnsNsec3RecordValue
{
    internal DnsResourceRecord Record = new DnsResourceRecord();
    internal byte[] OwnerHash = [];
    internal byte[] NextHash = [];
    internal int Flags;
    internal List<int> Types = new List<int>();
}

internal class DnsNsec3CollectionResult
{
    internal bool Success;
    internal bool Unsupported;
    internal int AlgorithmValue;
    internal int Iterations;
    internal byte[] Salt = [];
    internal List<DnsNsec3RecordValue> Values =
        new List<DnsNsec3RecordValue>();
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal static class Nsec3DenialValidator
{
    internal static DnsSecurityResult SelectWildcardExpansionProof(
        DnsNsec3ProofRequest request)
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
                "The NSEC3 proof-record limit was exceeded.");
        }

        DnsNsec3CollectionResult collected = CollectRecords(request);
        if (!collected.Success)
        {
            if (collected.Unsupported)
            {
                return FromUnsupportedCollection(
                    collected,
                    request.Policy);
            }

            return FromCollectionFailure(collected);
        }

        DnsName? closest = FindClosestEncloser(request, collected);
        if (closest == null || RecordSetOperations.NamesEqual(
                closest,
                request.QueryName))
        {
            return Bogus(
                "nsec3.wildcard",
                "A wildcard expansion requires a proper closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsNsec3RecordValue? nextProof = FindCoveringName(
            nextCloser,
            request,
            collected);
        DnsNsec3RecordValue? closestProof = FindMatchingName(
            closest,
            request,
            collected);
        if (nextProof == null || closestProof == null)
        {
            return Bogus(
                "nsec3.wildcard",
                "A wildcard expansion requires closest-encloser and next-closer proofs.");
        }

        bool optedOut = (nextProof.Flags & 1) != 0;
        DnsSecurityResult result = CreateStatus(
            optedOut ? DnsSecurityStatus.Insecure :
                DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, closestProof.Record);
        AddProof(result.ProofRecords, nextProof.Record);
        return result;
    }

    internal static DnsNsec3HashResult HashName(
        DnsName name,
        int algorithmValue,
        int iterations,
        byte[] salt,
        int maximumIterations)
    {
        if (name == null || salt == null || salt.Length > 255 ||
            iterations < 0 || iterations > 65535 ||
            maximumIterations < 0)
        {
            return HashFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "nsec3.hash",
                "Valid NSEC3 hashing input and limits are required.");
        }

        if (algorithmValue != (int)DnsNsec3HashAlgorithm.Sha1)
        {
            return HashFailure(
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "nsec3.hashAlgorithm",
                "Only the NSEC3 SHA-1 hash algorithm is supported.");
        }

        if (iterations > maximumIterations)
        {
            return HashFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "nsec3.iterations",
                "The NSEC3 iteration count exceeds the configured limit.");
        }

        DnsEncodeResult canonical =
            CanonicalDnsData.EncodeCanonicalName(name);
        if (!canonical.Success)
        {
            DnsNsec3HashResult failed = new DnsNsec3HashResult();
            AppendDiagnostics(failed.Diagnostics, canonical.Diagnostics);
            return failed;
        }

        byte[] input = Concatenate(canonical.Bytes, salt);
        byte[] hash = System.Security.Cryptography.SHA1.HashData(input);
        int index = 0;
        while (index < iterations)
        {
            input = Concatenate(hash, salt);
            hash = System.Security.Cryptography.SHA1.HashData(input);
            index = index + 1;
        }

        DnsNsec3HashResult result = new DnsNsec3HashResult();
        result.Success = true;
        result.Hash = hash;
        return result;
    }

    internal static DnsSecurityResult ValidateDenial(
        DnsNsec3ProofRequest request)
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
                "The NSEC3 proof-record limit was exceeded.");
        }

        DnsNsec3CollectionResult collected = CollectRecords(request);
        if (!collected.Success)
        {
            if (collected.Unsupported)
            {
                return FromUnsupportedCollection(
                    collected,
                    request.Policy);
            }

            return FromCollectionFailure(collected);
        }

        if (request.NameError)
        {
            return ValidateNameError(request, collected);
        }

        if (request.WildcardAnswer)
        {
            return ValidateWildcardNoData(request, collected);
        }

        return ValidateNoData(request, collected);
    }

    static DnsSecurityResult ValidateNameError(
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        DnsNsec3HashResult queryHash = HashForRequest(
            request.QueryName,
            request,
            collection);
        if (!queryHash.Success)
        {
            return FromHashFailure(queryHash);
        }

        if (FindMatchingRecord(queryHash.Hash, collection.Values) != null)
        {
            return Bogus(
                "nsec3.nameError",
                "NXDOMAIN cannot be proven because the query name hash exists.");
        }

        DnsName? closest = FindClosestEncloser(request, collection);
        if (closest == null)
        {
            return Bogus(
                "nsec3.closestEncloser",
                "No NSEC3 record matches a closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsName wildcard = CreateWildcard(closest);
        DnsNsec3RecordValue? nextProof = FindCoveringName(
            nextCloser,
            request,
            collection);
        DnsNsec3RecordValue? wildcardProof = FindCoveringName(
            wildcard,
            request,
            collection);
        if (nextProof == null || wildcardProof == null)
        {
            return Bogus(
                "nsec3.nameError",
                "NXDOMAIN requires NSEC3 coverage of the next-closer and wildcard hashes.");
        }

        bool optedOut = (nextProof.Flags & 1) != 0;
        if (optedOut && !request.OptOutAllowed)
        {
            return Bogus(
                "nsec3.optOut",
                "The next-closer proof uses Opt-Out where it is not permitted.");
        }

        DnsSecurityResult result = CreateStatus(
            optedOut ? DnsSecurityStatus.Insecure :
                DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, nextProof.Record);
        AddProof(result.ProofRecords, wildcardProof.Record);
        DnsNsec3RecordValue? closestProof = FindMatchingName(
            closest,
            request,
            collection);
        if (closestProof != null)
        {
            AddProof(result.ProofRecords, closestProof.Record);
        }

        return result;
    }

    static DnsSecurityResult ValidateNoData(
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        DnsNsec3RecordValue? exact = FindMatchingName(
            request.QueryName,
            request,
            collection);
        if (exact != null)
        {
            if (!BitmapDeniesType(exact.Types, request.QueryTypeValue))
            {
                return Bogus(
                    "nsec3.typeBitmap",
                    "The matching NSEC3 bitmap contains the query type or a CNAME.");
            }

            DnsSecurityResult exactResult =
                CreateStatus(DnsSecurityStatus.Secure);
            AddProof(exactResult.ProofRecords, exact.Record);
            return exactResult;
        }

        if (request.QueryTypeValue != (int)DnsRecordType.Ds ||
            !request.OptOutAllowed)
        {
            return Bogus(
                "nsec3.noData",
                "NODATA requires a matching NSEC3 record.");
        }

        DnsName? closest = FindClosestEncloser(request, collection);
        if (closest == null)
        {
            return Bogus(
                "nsec3.closestEncloser",
                "Opt-Out DS denial requires a closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsNsec3RecordValue? covering = FindCoveringName(
            nextCloser,
            request,
            collection);
        if (covering == null || (covering.Flags & 1) == 0)
        {
            return Bogus(
                "nsec3.optOut",
                "DS denial requires an Opt-Out NSEC3 span over the next-closer hash.");
        }

        DnsSecurityResult result = CreateStatus(DnsSecurityStatus.Insecure);
        AddProof(result.ProofRecords, covering.Record);
        return result;
    }

    static DnsSecurityResult ValidateWildcardNoData(
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        DnsName? closest = FindClosestEncloser(request, collection);
        if (closest == null)
        {
            return Bogus(
                "nsec3.closestEncloser",
                "Wildcard NODATA requires a closest encloser.");
        }

        DnsName nextCloser = CreateNextCloser(
            request.QueryName,
            closest);
        DnsName wildcard = CreateWildcard(closest);
        DnsNsec3RecordValue? nextProof = FindCoveringName(
            nextCloser,
            request,
            collection);
        DnsNsec3RecordValue? wildcardProof = FindMatchingName(
            wildcard,
            request,
            collection);
        if (nextProof == null || wildcardProof == null)
        {
            return Bogus(
                "nsec3.wildcard",
                "Wildcard NODATA requires next-closer coverage and a matching wildcard hash.");
        }

        if (!BitmapDeniesType(
                wildcardProof.Types,
                request.QueryTypeValue))
        {
            return Bogus(
                "nsec3.typeBitmap",
                "The wildcard NSEC3 bitmap contains the query type or a CNAME.");
        }

        bool optedOut = (nextProof.Flags & 1) != 0;
        if (optedOut && !request.OptOutAllowed)
        {
            return Bogus(
                "nsec3.optOut",
                "The wildcard proof uses an unpermitted Opt-Out span.");
        }

        DnsSecurityResult result = CreateStatus(
            optedOut ? DnsSecurityStatus.Insecure :
                DnsSecurityStatus.Secure);
        AddProof(result.ProofRecords, nextProof.Record);
        AddProof(result.ProofRecords, wildcardProof.Record);
        return result;
    }

    static DnsNsec3CollectionResult CollectRecords(
        DnsNsec3ProofRequest request)
    {
        DnsNsec3CollectionResult result =
            new DnsNsec3CollectionResult();
        int index = 0;
        while (index < request.Records.Count)
        {
            DnsResourceRecord record = request.Records[index];
            if (record == null || record.Data == null ||
                record.Data.Nsec3 == null ||
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) !=
                    (int)DnsRecordType.Nsec3 ||
                EffectiveValue(record.ClassValue, (int)record.Class) !=
                    request.ClassValue)
            {
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.InvalidRecordData,
                    "nsec3.record",
                    "Every proof must be a typed NSEC3 record in the query class.");
                return result;
            }

            DnsNsec3Data data = record.Data.Nsec3;
            int algorithmValue = EffectiveValue(
                data.HashAlgorithmValue,
                (int)data.HashAlgorithm);
            if (algorithmValue != (int)DnsNsec3HashAlgorithm.Sha1)
            {
                result.Unsupported = true;
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.UnsupportedAlgorithm,
                    "nsec3.hashAlgorithm",
                    "The NSEC3 hash algorithm is unsupported.");
                return result;
            }

            if (data.Iterations > request.Policy.MaximumNsec3Iterations)
            {
                result.Unsupported = true;
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "nsec3.iterations",
                    "The NSEC3 iteration count exceeds policy.");
                return result;
            }

            byte[] ownerHash = DecodeOwnerHash(record.Name, request.Zone);
            if (ownerHash.Length != 20 || data.Flags < 0 ||
                data.Flags > 1 || data.Iterations < 0 ||
                data.Iterations > 65535 ||
                data.Salt == null || data.Salt.Length > 255 ||
                data.NextHashedOwnerName == null ||
                data.NextHashedOwnerName.Length != 20 ||
                !BitmapIsValid(data.RecordTypeValues))
            {
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.InvalidRecordData,
                    "nsec3",
                    "An NSEC3 owner, field, hash, flag, or bitmap is malformed.");
                return result;
            }

            if (index == 0)
            {
                result.AlgorithmValue = algorithmValue;
                result.Iterations = data.Iterations;
                result.Salt = data.Salt;
            }
            else if (result.AlgorithmValue != algorithmValue ||
                result.Iterations != data.Iterations ||
                !BytesEqual(result.Salt, data.Salt))
            {
                AddDiagnostic(
                    result.Diagnostics,
                    DnsDiagnosticCode.InvalidRecordData,
                    "nsec3.parameters",
                    "All NSEC3 records in one proof must use identical parameters.");
                return result;
            }

            DnsNsec3RecordValue value = new DnsNsec3RecordValue();
            value.Record = record;
            value.OwnerHash = ownerHash;
            value.NextHash = data.NextHashedOwnerName;
            value.Flags = data.Flags;
            value.Types = data.RecordTypeValues;
            result.Values.Add(value);
            index = index + 1;
        }

        if (result.Values.Count == 0)
        {
            AddDiagnostic(
                result.Diagnostics,
                DnsDiagnosticCode.InvalidRecordData,
                "nsec3.records",
                "At least one NSEC3 proof record is required.");
            return result;
        }

        result.Success = true;
        return result;
    }

    static DnsSecurityResult ValidateRequest(DnsNsec3ProofRequest request)
    {
        if (request == null || request.Zone == null ||
            request.QueryName == null || request.Records == null ||
            request.Policy == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "nsec3",
                "Complete NSEC3 denial input is required.");
        }

        if (!NameWithinZone(request.QueryName, request.Zone) ||
            request.QueryTypeValue < 1 ||
            request.QueryTypeValue > 65535 ||
            request.ClassValue < 0 || request.ClassValue > 65535 ||
            request.Policy.MaximumProofRecords < 1 ||
            request.Policy.MaximumNsec3Iterations < 0)
        {
            return CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "nsec3.query",
                "The NSEC3 query, zone, class, type, and limits must be valid.");
        }

        return CreateStatus(DnsSecurityStatus.Indeterminate);
    }

    static DnsName? FindClosestEncloser(
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        DnsName current = request.QueryName;
        while (NameWithinZone(current, request.Zone))
        {
            if (FindMatchingName(current, request, collection) != null)
            {
                return current;
            }

            if (RecordSetOperations.NamesEqual(current, request.Zone))
            {
                return null;
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

    static DnsNsec3RecordValue? FindMatchingName(
        DnsName name,
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        DnsNsec3HashResult hash = HashForRequest(
            name,
            request,
            collection);
        if (!hash.Success)
        {
            return null;
        }

        return FindMatchingRecord(hash.Hash, collection.Values);
    }

    static DnsNsec3RecordValue? FindMatchingRecord(
        byte[] hash,
        List<DnsNsec3RecordValue> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (BytesEqual(hash, values[index].OwnerHash))
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsNsec3RecordValue? FindCoveringName(
        DnsName name,
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        DnsNsec3HashResult hash = HashForRequest(
            name,
            request,
            collection);
        if (!hash.Success)
        {
            return null;
        }

        int index = 0;
        while (index < collection.Values.Count)
        {
            if (CoversHash(collection.Values[index], hash.Hash))
            {
                return collection.Values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsNsec3HashResult HashForRequest(
        DnsName name,
        DnsNsec3ProofRequest request,
        DnsNsec3CollectionResult collection)
    {
        return HashName(
            name,
            collection.AlgorithmValue,
            collection.Iterations,
            collection.Salt,
            request.Policy.MaximumNsec3Iterations);
    }

    static bool CoversHash(DnsNsec3RecordValue value, byte[] hash)
    {
        int ownerToNext = CompareBytes(value.OwnerHash, value.NextHash);
        int ownerToHash = CompareBytes(value.OwnerHash, hash);
        int hashToNext = CompareBytes(hash, value.NextHash);
        if (ownerToHash == 0 || hashToNext == 0)
        {
            return false;
        }

        if (ownerToNext < 0)
        {
            return ownerToHash < 0 && hashToNext < 0;
        }

        if (ownerToNext > 0)
        {
            return ownerToHash < 0 || hashToNext < 0;
        }

        return true;
    }

    static byte[] DecodeOwnerHash(DnsName owner, DnsName zone)
    {
        if (!NameWithinZone(owner, zone) ||
            owner.Labels.Count != zone.Labels.Count + 1)
        {
            return [];
        }

        byte[] label = owner.Labels[0];
        if (label.Length != 32)
        {
            return [];
        }

        byte[] result = new byte[20];
        int buffer = 0;
        int bits = 0;
        int output = 0;
        int index = 0;
        while (index < label.Length)
        {
            int value = Base32HexValue(label[index]);
            if (value < 0)
            {
                return [];
            }

            buffer = buffer << 5 | value;
            bits = bits + 5;
            if (bits >= 8)
            {
                bits = bits - 8;
                result[output] = (byte)(buffer >> bits & 255);
                output = output + 1;
            }

            index = index + 1;
        }

        if (output != result.Length || bits != 0)
        {
            return [];
        }

        return result;
    }

    static int Base32HexValue(byte value)
    {
        if (value >= 48 && value <= 57)
        {
            return value - 48;
        }

        if (value >= 65 && value <= 86)
        {
            return value - 55;
        }

        if (value >= 97 && value <= 118)
        {
            return value - 87;
        }

        return -1;
    }

    static bool BitmapIsValid(List<int> types)
    {
        if (types == null)
        {
            return false;
        }

        int previous = -1;
        int index = 0;
        while (index < types.Count)
        {
            int value = types[index];
            if (value < 1 || value > 65535 || value <= previous)
            {
                return false;
            }

            previous = value;
            index = index + 1;
        }

        return true;
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

    static int CompareBytes(byte[] left, byte[] right)
    {
        int length = left.Length;
        if (right.Length < length)
        {
            length = right.Length;
        }

        int index = 0;
        while (index < length)
        {
            if (left[index] != right[index])
            {
                return left[index] < right[index] ? -1 : 1;
            }

            index = index + 1;
        }

        if (left.Length == right.Length)
        {
            return 0;
        }

        return left.Length < right.Length ? -1 : 1;
    }

    static bool BytesEqual(byte[] left, byte[] right)
    {
        return left != null && right != null &&
            CompareBytes(left, right) == 0;
    }

    static byte[] Concatenate(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, result, 0, first.Length);
        System.Array.Copy(
            second,
            0,
            result,
            first.Length,
            second.Length);
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

    static DnsSecurityResult FromHashFailure(DnsNsec3HashResult source)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = DnsSecurityStatus.Indeterminate;
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromCollectionFailure(
        DnsNsec3CollectionResult source)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = DnsSecurityStatus.Indeterminate;
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromUnsupportedCollection(
        DnsNsec3CollectionResult source,
        DnsSecurityPolicy policy)
    {
        DnsSecurityStatus status =
            DnsSecurityStatus.UnsupportedAlgorithm;
        if (policy.TreatUnsupportedAsBogus)
        {
            status = DnsSecurityStatus.Bogus;
        }

        DnsSecurityResult result = CreateStatus(status);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsNsec3HashResult HashFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsNsec3HashResult result = new DnsNsec3HashResult();
        AddDiagnostic(result.Diagnostics, code, field, reason);
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
