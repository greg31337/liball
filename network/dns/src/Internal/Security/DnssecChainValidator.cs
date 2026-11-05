using System.Collections.Generic;

namespace dns;

internal class DnssecZoneCutRequest
{
    internal DnsSecurityStatus ParentStatus;
    internal DnsName Zone = new DnsName();
    internal int ClassValue = 1;
    internal List<DnsDsData> DelegationSigners = new List<DnsDsData>();
    internal bool DelegationSignerAbsenceProven;
    internal DnsResourceRecordSet? DnskeyRecordSet;
    internal List<DnsSignatureData> Signatures =
        new List<DnsSignatureData>();
    internal DnsSecurityPolicy Policy = new DnsSecurityPolicy();
    internal IDnsCache? Cache;
    internal DnsClockValue Now = new DnsClockValue();
    internal bool CheckingDisabled;
}

internal class DnssecKeySelection
{
    internal bool Applicable;
    internal bool Invalid;
    internal bool Forbidden;
    internal bool Unsupported;
    internal List<DnsKeyMaterial> Keys = new List<DnsKeyMaterial>();
    internal List<DnsTrustAnchor> Anchors = new List<DnsTrustAnchor>();
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnssecCachedKeys
{
    internal bool Found;
    internal DnsResourceRecordSet? RecordSet;
}

internal static class DnssecChainValidator
{
    internal static DnsSecurityResult ValidateZoneCut(
        DnssecZoneCutRequest request)
    {
        DnsSecurityResult preliminary = ValidateRequest(request);
        if (!preliminary.Success)
        {
            return preliminary;
        }

        if (request.CheckingDisabled)
        {
            return CreateStatus(DnsSecurityStatus.Indeterminate);
        }

        if (request.ParentStatus != DnsSecurityStatus.Secure)
        {
            return PropagateParentStatus(request.ParentStatus, request.Policy);
        }

        if (request.DelegationSigners.Count == 0)
        {
            if (request.DelegationSignerAbsenceProven)
            {
                return CreateStatus(DnsSecurityStatus.Insecure);
            }

            return CreateDiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                DnsDiagnosticCode.InvalidRecordData,
                "delegation.ds",
                "The delegation has neither DS records nor an authenticated denial.");
        }

        bool fromCache = false;
        DnsResourceRecordSet? dnskeySet = request.DnskeyRecordSet;
        if (dnskeySet == null && request.Cache != null)
        {
            DnssecCachedKeys cached = ReadCachedKeys(
                request.Zone,
                request.ClassValue,
                request.Cache,
                request.Now);
            dnskeySet = cached.RecordSet;
            fromCache = cached.Found;
        }

        if (dnskeySet == null)
        {
            return CreateDiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey",
                "The child DNSKEY RRset is not available.");
        }

        List<DnsKeyMaterial> keys = ExtractKeys(
            request.Zone,
            request.ClassValue,
            dnskeySet);
        if (keys.Count == 0)
        {
            return CreateDiagnosticStatus(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey",
                "The child DNSKEY RRset contains no usable DNSKEY records.");
        }

        DnssecKeySelection selection = SelectByDelegationSigners(
            request.Zone,
            keys,
            request.DelegationSigners,
            request.Policy);
        DnsSecurityResult selected = SelectionFailure(selection, request.Policy);
        if (selection.Keys.Count == 0)
        {
            return selected;
        }

        if (fromCache)
        {
            return CreateKeySuccess(selection.Keys[0]);
        }

        DnsSecurityResult validated =
            DnssecRecordSetValidator.ValidateRecordSet(
                dnskeySet,
                selection.Keys,
                request.Signatures,
                request.Policy);
        if (validated.Status == DnsSecurityStatus.Secure &&
            request.Cache != null)
        {
            StoreValidatedKeys(
                dnskeySet,
                request.Cache,
                request.Now);
        }

        return validated;
    }

    internal static DnsSecurityResult ValidateTrustAnchors(
        DnssecZoneCutRequest request)
    {
        DnsSecurityResult preliminary = ValidateRequest(request);
        if (!preliminary.Success)
        {
            return preliminary;
        }

        if (request.CheckingDisabled)
        {
            return CreateStatus(DnsSecurityStatus.Indeterminate);
        }

        bool fromCache = false;
        DnsResourceRecordSet? dnskeySet = request.DnskeyRecordSet;
        if (dnskeySet == null && request.Cache != null)
        {
            DnssecCachedKeys cached = ReadCachedKeys(
                request.Zone,
                request.ClassValue,
                request.Cache,
                request.Now);
            dnskeySet = cached.RecordSet;
            fromCache = cached.Found;
        }

        if (dnskeySet == null)
        {
            return CreateDiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey",
                "A DNSKEY RRset is required to match a trust anchor.");
        }

        List<DnsKeyMaterial> keys = ExtractKeys(
            request.Zone,
            request.ClassValue,
            dnskeySet);
        DnssecKeySelection selection = SelectByTrustAnchors(
            request.Zone,
            keys,
            request.Policy);
        DnsSecurityResult selected = SelectionFailure(selection, request.Policy);
        if (selection.Keys.Count == 0)
        {
            return selected;
        }

        DnsSecurityResult validated;
        if (fromCache)
        {
            validated = CreateKeySuccess(selection.Keys[0]);
        }
        else
        {
            validated = DnssecRecordSetValidator.ValidateRecordSet(
                dnskeySet,
                selection.Keys,
                request.Signatures,
                request.Policy);
        }

        if (validated.Status == DnsSecurityStatus.Secure)
        {
            AppendAnchors(validated.TrustAnchors, selection.Anchors);
            if (!fromCache && request.Cache != null)
            {
                StoreValidatedKeys(
                    dnskeySet,
                    request.Cache,
                    request.Now);
            }
        }

        return validated;
    }

    internal static DnsMessage PrepareValidationRequest(
        DnsMessage message,
        int udpPayloadSize,
        bool checkingDisabled)
    {
        DnsMessage result = EdnsOperations.PrepareRequest(
            message,
            udpPayloadSize,
            true);
        int flags = MessageOperations.EffectiveFlags(result.Header);
        flags = flags & ~(int)DnsHeaderFlag.CheckingDisabled;
        if (checkingDisabled)
        {
            flags = flags | (int)DnsHeaderFlag.CheckingDisabled;
        }

        result.Header.Flags = (DnsHeaderFlag)flags;
        result.Header.FlagsValue = flags;
        return result;
    }

    internal static DnsMessage ApplyAuthenticData(
        DnsMessage message,
        DnsSecurityStatus status,
        bool checkingDisabled)
    {
        DnsMessage result = MessageOperations.CloneMessage(message);
        int flags = MessageOperations.EffectiveFlags(result.Header);
        flags = flags & ~(int)DnsHeaderFlag.AuthenticData;
        if (status == DnsSecurityStatus.Secure && !checkingDisabled)
        {
            flags = flags | (int)DnsHeaderFlag.AuthenticData;
        }

        result.Header.Flags = (DnsHeaderFlag)flags;
        result.Header.FlagsValue = flags;
        return result;
    }

    static DnsSecurityResult ValidateRequest(DnssecZoneCutRequest request)
    {
        if (request == null || request.Zone == null ||
            request.DelegationSigners == null ||
            request.Signatures == null || request.Policy == null ||
            request.Policy.TrustAnchors == null || request.Now == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "chain",
                "Complete DNSSEC chain-validation input is required.");
        }

        RawDnsNameResult name = RawDnsName.ValidateName(request.Zone);
        if (!name.Success || name.Name == null ||
            !name.Name.IsAbsolute || request.ClassValue < 0 ||
            request.ClassValue > 65535)
        {
            return CreateFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "chain.zone",
                "A valid absolute zone name and class are required.");
        }

        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Indeterminate);
        return result;
    }

    static DnsSecurityResult PropagateParentStatus(
        DnsSecurityStatus parentStatus,
        DnsSecurityPolicy policy)
    {
        if (parentStatus == DnsSecurityStatus.Insecure)
        {
            return CreateStatus(DnsSecurityStatus.Insecure);
        }

        if (parentStatus == DnsSecurityStatus.Bogus)
        {
            return CreateStatus(DnsSecurityStatus.Bogus);
        }

        if (parentStatus == DnsSecurityStatus.UnsupportedAlgorithm)
        {
            if (policy.TreatUnsupportedAsBogus)
            {
                return CreateStatus(DnsSecurityStatus.Bogus);
            }

            return CreateStatus(DnsSecurityStatus.UnsupportedAlgorithm);
        }

        return CreateStatus(DnsSecurityStatus.Indeterminate);
    }

    internal static List<DnsKeyMaterial> ExtractKeys(
        DnsName zone,
        int classValue,
        DnsResourceRecordSet recordSet)
    {
        List<DnsKeyMaterial> result = new List<DnsKeyMaterial>();
        if (recordSet == null || recordSet.Records == null ||
            EffectiveValue(
                recordSet.RecordTypeValue,
                (int)recordSet.RecordType) !=
                (int)DnsRecordType.Dnskey ||
            EffectiveValue(recordSet.ClassValue, (int)recordSet.Class) !=
                classValue ||
            !RecordSetOperations.NamesEqual(recordSet.Name, zone))
        {
            return result;
        }

        int index = 0;
        while (index < recordSet.Records.Count)
        {
            DnsResourceRecord record = recordSet.Records[index];
            if (record != null && record.Data != null &&
                record.Data.Key != null)
            {
                DnsDnssecIntegerResult tag =
                    DnssecAlgorithms.CalculateKeyTag(record.Data.Key);
                if (tag.Success)
                {
                    DnsKeyMaterial material = new DnsKeyMaterial();
                    material.Name = RecordSetOperations.CopyName(zone, true);
                    material.Class = (DnsClass)classValue;
                    material.ClassValue = classValue;
                    material.Key = record.Data.Key;
                    material.KeyTag = tag.Value;
                    result.Add(material);
                }
            }

            index = index + 1;
        }

        return result;
    }

    static DnssecKeySelection SelectByDelegationSigners(
        DnsName zone,
        List<DnsKeyMaterial> keys,
        List<DnsDsData> delegationSigners,
        DnsSecurityPolicy policy)
    {
        DnssecKeySelection result = new DnssecKeySelection();
        int dsIndex = 0;
        while (dsIndex < delegationSigners.Count)
        {
            DnsDsData delegationSigner = delegationSigners[dsIndex];
            if (delegationSigner != null)
            {
                result.Applicable = true;
                int digestValue = EffectiveValue(
                    delegationSigner.DigestAlgorithmValue,
                    (int)delegationSigner.DigestAlgorithm);
                DnsAlgorithmPolicyResult digestPolicy =
                    DnssecAlgorithms.ResolveDigestPolicy(
                        policy,
                        digestValue);
                if (!digestPolicy.Success)
                {
                    result.Invalid = true;
                    AppendDiagnostics(
                        result.Diagnostics,
                        digestPolicy.Diagnostics);
                }
                else if (digestPolicy.Policy ==
                    DnsAlgorithmPolicy.Forbidden)
                {
                    result.Forbidden = true;
                }
                else if (digestPolicy.Policy ==
                    DnsAlgorithmPolicy.Unsupported)
                {
                    result.Unsupported = true;
                }
                else
                {
                    MatchDelegationSigner(
                        zone,
                        keys,
                        delegationSigner,
                        digestValue,
                        policy,
                        result);
                }
            }

            dsIndex = dsIndex + 1;
        }

        return result;
    }

    static void MatchDelegationSigner(
        DnsName zone,
        List<DnsKeyMaterial> keys,
        DnsDsData delegationSigner,
        int digestValue,
        DnsSecurityPolicy policy,
        DnssecKeySelection result)
    {
        int algorithmValue = EffectiveValue(
            delegationSigner.AlgorithmValue,
            (int)delegationSigner.Algorithm);
        DnsAlgorithmPolicyResult algorithmPolicy =
            DnssecAlgorithms.ResolveAlgorithmPolicy(policy, algorithmValue);
        if (!algorithmPolicy.Success)
        {
            result.Invalid = true;
            AppendDiagnostics(
                result.Diagnostics,
                algorithmPolicy.Diagnostics);
            return;
        }

        if (algorithmPolicy.Policy == DnsAlgorithmPolicy.Forbidden)
        {
            result.Forbidden = true;
            return;
        }

        if (algorithmPolicy.Policy == DnsAlgorithmPolicy.Unsupported)
        {
            result.Unsupported = true;
            return;
        }

        int keyIndex = 0;
        while (keyIndex < keys.Count)
        {
            DnsKeyMaterial key = keys[keyIndex];
            int keyAlgorithm = EffectiveValue(
                key.Key.AlgorithmValue,
                (int)key.Key.Algorithm);
            if (key.KeyTag == delegationSigner.KeyTag &&
                keyAlgorithm == algorithmValue)
            {
                DnsDelegationDigestResult calculated =
                    DnssecAlgorithms.CreateDelegationDigest(
                        zone,
                        key.Key,
                        (DnsDigestAlgorithm)digestValue,
                        digestValue);
                if (calculated.Success &&
                    calculated.DelegationSigner != null &&
                    BytesEqualConstantTime(
                        calculated.DelegationSigner.Digest,
                        delegationSigner.Digest))
                {
                    AddUniqueKey(result.Keys, key);
                }
            }

            keyIndex = keyIndex + 1;
        }
    }

    static DnssecKeySelection SelectByTrustAnchors(
        DnsName zone,
        List<DnsKeyMaterial> keys,
        DnsSecurityPolicy policy)
    {
        DnssecKeySelection result = new DnssecKeySelection();
        uint validationTime = ReadValidationTime(policy);
        int anchorIndex = 0;
        while (anchorIndex < policy.TrustAnchors.Count)
        {
            DnsTrustAnchor anchor = policy.TrustAnchors[anchorIndex];
            if (anchor != null && anchor.Enabled && !anchor.Revoked &&
                RecordSetOperations.NamesEqual(anchor.Name, zone) &&
                AnchorIsCurrent(anchor, validationTime))
            {
                result.Applicable = true;
                int keyIndex = 0;
                while (keyIndex < keys.Count)
                {
                    DnsKeyMaterial key = keys[keyIndex];
                    bool matched = false;
                    if (anchor.Key != null)
                    {
                        matched = KeysEqual(anchor.Key, key.Key);
                    }
                    else if (anchor.DelegationSigner != null)
                    {
                        List<DnsDsData> values = new List<DnsDsData>();
                        values.Add(anchor.DelegationSigner);
                        DnssecKeySelection dsSelection =
                            SelectByDelegationSigners(
                                zone,
                                keys,
                                values,
                                policy);
                        matched = ContainsKey(dsSelection.Keys, key);
                        result.Forbidden = result.Forbidden ||
                            dsSelection.Forbidden;
                        result.Unsupported = result.Unsupported ||
                            dsSelection.Unsupported;
                        result.Invalid = result.Invalid ||
                            dsSelection.Invalid;
                        AppendDiagnostics(
                            result.Diagnostics,
                            dsSelection.Diagnostics);
                    }

                    if (matched)
                    {
                        AddUniqueKey(result.Keys, key);
                        AddUniqueAnchor(result.Anchors, anchor);
                    }

                    keyIndex = keyIndex + 1;
                }
            }

            anchorIndex = anchorIndex + 1;
        }

        return result;
    }

    static DnsSecurityResult SelectionFailure(
        DnssecKeySelection selection,
        DnsSecurityPolicy policy)
    {
        DnsSecurityResult result;
        if (selection.Keys.Count > 0)
        {
            result = CreateStatus(DnsSecurityStatus.Secure);
        }
        else if (selection.Invalid)
        {
            result = CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "policy",
                "The DNSSEC algorithm policy is invalid.");
        }
        else if (selection.Forbidden ||
            (selection.Unsupported && policy.TreatUnsupportedAsBogus))
        {
            result = CreateDiagnosticStatus(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "ds.algorithm",
                "The delegation requires an algorithm forbidden by policy.");
        }
        else if (selection.Unsupported)
        {
            result = CreateDiagnosticStatus(
                DnsSecurityStatus.UnsupportedAlgorithm,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "ds.algorithm",
                "The delegation requires an unsupported algorithm.");
        }
        else if (selection.Applicable)
        {
            result = CreateDiagnosticStatus(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey",
                "No DNSKEY matched the trusted DS or DNSKEY data.");
        }
        else
        {
            result = CreateDiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                DnsDiagnosticCode.InvalidRecordData,
                "trustAnchor",
                "No current trust anchor applies to the zone.");
        }

        AppendDiagnostics(result.Diagnostics, selection.Diagnostics);
        return result;
    }

    static DnsSecurityResult CreateKeySuccess(DnsKeyMaterial key)
    {
        DnsSecurityResult result = CreateStatus(DnsSecurityStatus.Secure);
        int algorithmValue = EffectiveValue(
            key.Key.AlgorithmValue,
            (int)key.Key.Algorithm);
        result.Algorithm = (DnsDnssecAlgorithm)algorithmValue;
        result.AlgorithmValue = algorithmValue;
        result.HasKeyTag = true;
        result.KeyTag = key.KeyTag;
        return result;
    }

    internal static DnssecCachedKeys ReadCachedKeys(
        DnsName zone,
        int classValue,
        IDnsCache cache,
        DnsClockValue now)
    {
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = zone;
        request.RecordType = DnsRecordType.Dnskey;
        request.RecordTypeValue = (int)DnsRecordType.Dnskey;
        request.Class = (DnsClass)classValue;
        request.ClassValue = classValue;
        DnsCacheLookup lookup = cache.Lookup(request, now);
        DnssecCachedKeys result = new DnssecCachedKeys();
        if (lookup != null && lookup.Found && lookup.Fresh &&
            lookup.Entry != null &&
            lookup.Entry.SecurityStatus == DnsSecurityStatus.Secure &&
            lookup.Entry.RecordSets.Count == 1)
        {
            result.Found = true;
            result.RecordSet = lookup.Entry.RecordSets[0];
        }

        return result;
    }

    static void StoreValidatedKeys(
        DnsResourceRecordSet recordSet,
        IDnsCache cache,
        DnsClockValue now)
    {
        if (recordSet.TimeToLive == 0)
        {
            return;
        }

        DnsCacheEntry entry = new DnsCacheEntry();
        entry.Kind = DnsCacheEntryKind.Positive;
        entry.Name = recordSet.Name;
        entry.RecordType = DnsRecordType.Dnskey;
        entry.RecordTypeValue = (int)DnsRecordType.Dnskey;
        entry.Class = recordSet.Class;
        entry.ClassValue = recordSet.ClassValue;
        entry.RecordSets.Add(recordSet);
        entry.SecurityStatus = DnsSecurityStatus.Secure;
        entry.StoredAt = CopyClock(now);
        entry.OriginalTimeToLive = recordSet.TimeToLive;
        entry.ExpiresAt = AddTimeToLive(now, recordSet.TimeToLive);
        entry.Authoritative = true;
        entry.Credibility = 5;
        cache.Store(entry);
    }

    static DnsClockValue AddTimeToLive(DnsClockValue now, uint ttl)
    {
        long amount = (long)ttl * 1000;
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = AddSaturated(
            now.UnixTimeMilliseconds,
            amount);
        result.MonotonicMilliseconds = AddSaturated(
            now.MonotonicMilliseconds,
            amount);
        return result;
    }

    static long AddSaturated(long value, long amount)
    {
        if (value > long.MaxValue - amount)
        {
            return long.MaxValue;
        }

        return value + amount;
    }

    static DnsClockValue CopyClock(DnsClockValue source)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = source.UnixTimeMilliseconds;
        result.MonotonicMilliseconds = source.MonotonicMilliseconds;
        return result;
    }

    static bool AnchorIsCurrent(DnsTrustAnchor anchor, uint time)
    {
        if (anchor.HasValidFrom)
        {
            DnsSerialComparison from = DnsSerialArithmetic.CompareSerials(
                time,
                anchor.ValidFrom);
            if (!from.Defined || from.Comparison < 0)
            {
                return false;
            }
        }

        if (anchor.HasValidUntil)
        {
            DnsSerialComparison until = DnsSerialArithmetic.CompareSerials(
                anchor.ValidUntil,
                time);
            if (!until.Defined || until.Comparison < 0)
            {
                return false;
            }
        }

        return true;
    }

    static uint ReadValidationTime(DnsSecurityPolicy policy)
    {
        if (policy.HasValidationTime)
        {
            return policy.ValidationTime;
        }

        return (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    static bool KeysEqual(DnsKeyData left, DnsKeyData right)
    {
        return left.Flags == right.Flags &&
            left.Protocol == right.Protocol &&
            EffectiveValue(left.AlgorithmValue, (int)left.Algorithm) ==
                EffectiveValue(right.AlgorithmValue, (int)right.Algorithm) &&
            BytesEqualConstantTime(left.PublicKey, right.PublicKey);
    }

    static bool BytesEqualConstantTime(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int difference = 0;
        int index = 0;
        while (index < left.Length)
        {
            difference = difference | left[index] ^ right[index];
            index = index + 1;
        }

        return difference == 0;
    }

    static bool ContainsKey(
        List<DnsKeyMaterial> keys,
        DnsKeyMaterial candidate)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index].KeyTag == candidate.KeyTag &&
                KeysEqual(keys[index].Key, candidate.Key))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AddUniqueKey(
        List<DnsKeyMaterial> keys,
        DnsKeyMaterial candidate)
    {
        if (!ContainsKey(keys, candidate))
        {
            keys.Add(candidate);
        }
    }

    static void AddUniqueAnchor(
        List<DnsTrustAnchor> anchors,
        DnsTrustAnchor candidate)
    {
        int index = 0;
        while (index < anchors.Count)
        {
            if (anchors[index] == candidate)
            {
                return;
            }

            index = index + 1;
        }

        anchors.Add(candidate);
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

    static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
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
