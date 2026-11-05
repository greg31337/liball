using System.Collections.Generic;

namespace dns;

internal class DnsAggressiveDenialEntry
{
    internal DnsName Zone = new DnsName();
    internal int ClassValue = 1;
    internal bool Nsec3;
    internal List<DnsResourceRecord> Records =
        new List<DnsResourceRecord>();
    internal long ExpiresMonotonicMilliseconds;
}

internal static class IterativeDnssecOperations
{
    internal static bool NeedsDelegationKeyValidation(
        DnsIterativeResolverState state,
        DnsRecursiveResolver resolver)
    {
        if (!state.Options.ValidateDnssec)
        {
            return false;
        }

        List<DnsDsData> delegationSigners = ReadDelegationSigners(
            state.CurrentDelegation);
        if (delegationSigners.Count > 0)
        {
            int delegationClassValue = EffectiveValue(
                state.Request.ClassValue,
                (int)state.Request.Class);
            DnssecCachedKeys delegationKeys =
                DnssecChainValidator.ReadCachedKeys(
                    state.CurrentDelegation.Zone,
                    delegationClassValue,
                    resolver.Cache,
                    state.ResolverClock.ReadClock());
            return state.CurrentDelegationSecurityStatus !=
                DnsSecurityStatus.Secure || !delegationKeys.Found;
        }

        if (!HasTrustAnchor(
                state.Options.Security,
                state.CurrentDelegation.Zone))
        {
            return false;
        }

        int classValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        DnssecCachedKeys cached = DnssecChainValidator.ReadCachedKeys(
            state.CurrentDelegation.Zone,
            classValue,
            resolver.Cache,
            state.ResolverClock.ReadClock());
        return state.CurrentDelegationSecurityStatus !=
            DnsSecurityStatus.Secure || !cached.Found;
    }

    internal static DnsSecurityResult ValidateDelegationKeys(
        DnsIterativeResolverState state,
        DnsMessage response,
        DnsRecursiveResolver resolver)
    {
        int flags = EffectiveValue(
            response.Header.FlagsValue,
            (int)response.Header.Flags);
        int responseCode = EffectiveValue(
            response.Header.ResponseCodeValue,
            (int)response.Header.ResponseCode);
        if (responseCode != (int)DnsResponseCode.NoError ||
            (flags & (int)DnsHeaderFlag.AuthoritativeAnswer) == 0)
        {
            return DiagnosticStatus(
                DnsSecurityStatus.Bogus,
                "dnskey",
                "A DNSKEY response must be an authoritative successful response.");
        }

        List<DnsDsData> delegationSigners = ReadDelegationSigners(
            state.CurrentDelegation);
        DnsResourceRecordSet? keySet = ReadDnskeySet(
            response,
            state.CurrentDelegation.Zone,
            EffectiveValue(
                state.Request.ClassValue,
                (int)state.Request.Class));
        if (keySet == null)
        {
            return DiagnosticStatus(
                DnsSecurityStatus.Bogus,
                "dnskey",
                "A DNSKEY response contains no DNSKEY RRset for the delegated zone.");
        }

        DnssecZoneCutRequest request = new DnssecZoneCutRequest();
        request.ParentStatus = DnsSecurityStatus.Secure;
        request.Zone = state.CurrentDelegation.Zone;
        request.ClassValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        request.DelegationSigners = delegationSigners;
        request.DnskeyRecordSet = keySet;
        request.Signatures = FindSignatures(
            response,
            keySet.Name,
            (int)DnsRecordType.Dnskey,
            request.ClassValue);
        request.Policy = state.Options.Security;
        request.Cache = resolver.Cache;
        request.Now = state.ResolverClock.ReadClock();
        if (delegationSigners.Count > 0)
        {
            return DnssecChainValidator.ValidateZoneCut(request);
        }

        if (HasTrustAnchor(request.Policy, request.Zone))
        {
            return DnssecChainValidator.ValidateTrustAnchors(request);
        }

        return CreateStatus(state.CurrentDelegationSecurityStatus);
    }

    internal static DnsSecurityResult ValidateReferral(
        DnsIterativeResolverState state,
        DnsMessage response,
        DnsDelegation delegation,
        DnsRecursiveResolver resolver)
    {
        if (!state.Options.ValidateDnssec)
        {
            return CreateStatus(DnsSecurityStatus.Insecure);
        }

        if (state.CurrentDelegationSecurityStatus !=
            DnsSecurityStatus.Secure)
        {
            return CreateStatus(state.CurrentDelegationSecurityStatus);
        }

        List<DnsResourceRecord> dsRecords = FindRecordsByType(
            response.Authorities,
            (int)DnsRecordType.Ds);
        List<DnsResourceRecord> matchingDsRecords =
            new List<DnsResourceRecord>();
        List<DnsDsData> delegationSigners = new List<DnsDsData>();
        int index = 0;
        while (index < dsRecords.Count)
        {
            DnsResourceRecord record = dsRecords[index];
            if (record.Data != null && record.Data.Ds != null &&
                RecordSetOperations.NamesEqual(
                    record.Name,
                    delegation.Zone))
            {
                matchingDsRecords.Add(record);
                delegationSigners.Add(record.Data.Ds);
            }

            index = index + 1;
        }

        if (delegationSigners.Count == 0)
        {
            DnsIterativeResolverState denialState =
                new DnsIterativeResolverState();
            denialState.Request = new DnsResolveRequest();
            denialState.Request.Name = delegation.Zone;
            denialState.Request.RecordType = DnsRecordType.Ds;
            denialState.Request.RecordTypeValue = (int)DnsRecordType.Ds;
            denialState.Request.Class = state.Request.Class;
            denialState.Request.ClassValue = state.Request.ClassValue;
            denialState.CurrentName = delegation.Zone;
            denialState.CurrentDelegation = state.CurrentDelegation;
            denialState.CurrentDelegationSecurityStatus =
                DnsSecurityStatus.Secure;
            denialState.Options = state.Options;
            denialState.ResolverClock = state.ResolverClock;
            DnsSecurityResult absence = ValidateNegativeAnswer(
                denialState,
                response,
                false,
                resolver);
            if (absence.Status == DnsSecurityStatus.Secure)
            {
                return CreateStatus(DnsSecurityStatus.Insecure);
            }

            return absence;
        }

        List<DnsResourceRecordSet> dsSets =
            RecordSetOperations.GroupRecordSets(matchingDsRecords);
        List<DnsKeyMaterial> parentKeys = ReadSecureKeys(
            state,
            resolver.Cache);
        if (parentKeys.Count == 0)
        {
            return DiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                "dnskey",
                "No secure parent DNSKEY is available to authenticate the DS RRset.");
        }

        DnsSecurityResult authenticatedDs = AuthenticateProofs(
            state,
            response,
            matchingDsRecords,
            parentKeys);
        if (authenticatedDs.Status != DnsSecurityStatus.Secure)
        {
            return authenticatedDs;
        }

        index = 0;
        while (index < dsSets.Count)
        {
            delegation.AuthorityRecordSets.Add(dsSets[index]);
            index = index + 1;
        }

        DnssecZoneCutRequest chain = new DnssecZoneCutRequest();
        chain.ParentStatus = DnsSecurityStatus.Secure;
        chain.Zone = delegation.Zone;
        chain.ClassValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        chain.DelegationSigners = delegationSigners;
        chain.Policy = state.Options.Security;
        chain.Cache = resolver.Cache;
        chain.Now = state.ResolverClock.ReadClock();
        DnssecCachedKeys childKeys = DnssecChainValidator.ReadCachedKeys(
            delegation.Zone,
            chain.ClassValue,
            resolver.Cache,
            chain.Now);
        if (!childKeys.Found)
        {
            return CreateStatus(DnsSecurityStatus.Indeterminate);
        }

        return DnssecChainValidator.ValidateZoneCut(chain);
    }

    internal static DnsSecurityResult ValidatePositiveAnswer(
        DnsIterativeResolverState state,
        DnsMessage response,
        List<DnsResourceRecordSet> recordSets,
        DnsRecursiveResolver resolver)
    {
        if (!state.Options.ValidateDnssec)
        {
            return CreateStatus(DnsSecurityStatus.Insecure);
        }

        if (state.CurrentDelegationSecurityStatus !=
            DnsSecurityStatus.Secure)
        {
            return CreateStatus(state.CurrentDelegationSecurityStatus);
        }

        List<DnsKeyMaterial> keys = ReadSecureKeys(state, resolver.Cache);
        if (keys.Count == 0)
        {
            return DiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                "dnskey",
                "No secure cached DNSKEY RRset is available for the answering zone.");
        }

        DnsSecurityResult aggregate = CreateStatus(
            DnsSecurityStatus.Secure);
        int index = 0;
        while (index < recordSets.Count)
        {
            DnsResourceRecordSet set = recordSets[index];
            List<DnsSignatureData> signatures = FindSignatures(
                response,
                set.Name,
                EffectiveValue(set.RecordTypeValue, (int)set.RecordType),
                EffectiveValue(set.ClassValue, (int)set.Class));
            DnsSecurityResult validated =
                DnssecRecordSetValidator.ValidateRecordSet(
                    set,
                    keys,
                    signatures,
                    state.Options.Security);
            if (validated.Status == DnsSecurityStatus.Insecure)
            {
                validated.Status = DnsSecurityStatus.Bogus;
            }

            AppendDiagnostics(
                aggregate.Diagnostics,
                validated.Diagnostics);
            if (validated.Status != DnsSecurityStatus.Secure)
            {
                aggregate.Status = validated.Status;
                return aggregate;
            }

            aggregate.ValidatedRecordSets.Add(set);
            index = index + 1;
        }

        return aggregate;
    }

    internal static DnsSecurityResult ValidateNegativeAnswer(
        DnsIterativeResolverState state,
        DnsMessage response,
        bool nameError,
        DnsRecursiveResolver resolver)
    {
        if (!state.Options.ValidateDnssec)
        {
            return CreateStatus(DnsSecurityStatus.Insecure);
        }

        if (state.CurrentDelegationSecurityStatus !=
            DnsSecurityStatus.Secure)
        {
            return CreateStatus(state.CurrentDelegationSecurityStatus);
        }

        List<DnsKeyMaterial> keys = ReadSecureKeys(state, resolver.Cache);
        if (keys.Count == 0)
        {
            return DiagnosticStatus(
                DnsSecurityStatus.Indeterminate,
                "dnskey",
                "No secure cached DNSKEY RRset is available for denial validation.");
        }

        List<DnsResourceRecord> nsec = FindRecordsByType(
            response.Authorities,
            (int)DnsRecordType.Nsec);
        List<DnsResourceRecord> nsec3 = FindRecordsByType(
            response.Authorities,
            (int)DnsRecordType.Nsec3);
        List<DnsResourceRecord> proofs = nsec.Count > 0 ? nsec : nsec3;
        if (proofs.Count == 0)
        {
            return DiagnosticStatus(
                DnsSecurityStatus.Bogus,
                "denial",
                "A secure negative response contains no NSEC or NSEC3 proof.");
        }

        List<DnsResourceRecord> authenticatedRecords =
            new List<DnsResourceRecord>();
        AppendRecords(authenticatedRecords, proofs);
        List<DnsResourceRecord> startOfAuthority = FindRecordsByType(
            response.Authorities,
            (int)DnsRecordType.Soa);
        AppendRecords(authenticatedRecords, startOfAuthority);
        DnsSecurityResult authenticated = AuthenticateProofs(
            state,
            response,
            authenticatedRecords,
            keys);
        if (authenticated.Status != DnsSecurityStatus.Secure)
        {
            return authenticated;
        }

        int queryTypeValue = EffectiveValue(
            state.Request.RecordTypeValue,
            (int)state.Request.RecordType);
        int classValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        DnsSecurityResult result;
        if (nsec.Count > 0)
        {
            DnsNsecProofRequest request = new DnsNsecProofRequest();
            request.Zone = state.CurrentDelegation.Zone;
            request.QueryName = state.CurrentName;
            request.QueryTypeValue = queryTypeValue;
            request.ClassValue = classValue;
            request.NameError = nameError;
            request.Records = nsec;
            request.Policy = state.Options.Security;
            result = NsecDenialValidator.ValidateDenial(request);
        }
        else
        {
            DnsNsec3ProofRequest request = new DnsNsec3ProofRequest();
            request.Zone = state.CurrentDelegation.Zone;
            request.QueryName = state.CurrentName;
            request.QueryTypeValue = queryTypeValue;
            request.ClassValue = classValue;
            request.NameError = nameError;
            request.OptOutAllowed = queryTypeValue ==
                (int)DnsRecordType.Ds;
            request.Records = nsec3;
            request.Policy = state.Options.Security;
            result = Nsec3DenialValidator.ValidateDenial(request);
        }

        if (result.Status == DnsSecurityStatus.Secure ||
            result.Status == DnsSecurityStatus.Insecure)
        {
            result.ValidatedRecordSets = authenticated.ValidatedRecordSets;
        }

        return result;
    }

    internal static void StoreAggressiveDenial(
        DnsIterativeResolverState state,
        List<DnsResourceRecord> records,
        uint timeToLive,
        DnsRecursiveResolver resolver)
    {
        if (records.Count == 0 || timeToLive == 0)
        {
            return;
        }

        DnsClockValue now = state.ResolverClock.ReadClock();
        DnsAggressiveDenialEntry entry = new DnsAggressiveDenialEntry();
        entry.Zone = RecordSetOperations.CopyName(
            state.CurrentDelegation.Zone,
            true);
        entry.ClassValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        entry.Nsec3 = EffectiveValue(
            records[0].RecordTypeValue,
            (int)records[0].RecordType) == (int)DnsRecordType.Nsec3;
        uint boundedTimeToLive = timeToLive;
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            entry.Records.Add(RecordSetOperations.CopyRecord(
                record,
                record.Name));
            if (record.TimeToLive < boundedTimeToLive)
            {
                boundedTimeToLive = record.TimeToLive;
            }

            index = index + 1;
        }

        long milliseconds = (long)boundedTimeToLive * 1000;
        entry.ExpiresMonotonicMilliseconds = AddSaturated(
            now.MonotonicMilliseconds,
            milliseconds);
        lock (resolver.DenialEntries)
        {
            RemoveZoneEntry(
                resolver.DenialEntries,
                entry.Zone,
                entry.ClassValue,
                entry.Nsec3);
            resolver.DenialEntries.Add(entry);
        }
    }

    internal static DnsResolveResult? ReadAggressiveDenial(
        DnsResolveRequest request,
        DnsRecursiveOptions options,
        DnsRecursiveResolver resolver)
    {
        if (!options.ValidateDnssec || !options.Security.ValidateDenial)
        {
            return null;
        }

        DnsClockValue now = resolver.Clock.ReadClock();
        int classValue = EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        int typeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        lock (resolver.DenialEntries)
        {
            int index = resolver.DenialEntries.Count - 1;
            while (index >= 0)
            {
                DnsAggressiveDenialEntry entry =
                    resolver.DenialEntries[index];
                if (entry.ExpiresMonotonicMilliseconds <=
                    now.MonotonicMilliseconds)
                {
                    resolver.DenialEntries.RemoveAt(index);
                }
                else if (entry.ClassValue == classValue &&
                    NameWithinZone(request.Name, entry.Zone))
                {
                    DnsSecurityResult nameError = ValidateCachedProof(
                        request.Name,
                        typeValue,
                        classValue,
                        true,
                        entry,
                        options.Security);
                    if (nameError.Status == DnsSecurityStatus.Secure)
                    {
                        return CreateAggressiveResult(
                            request,
                            DnsResolveStatus.NameError,
                            nameError.Status,
                            entry,
                            now);
                    }

                    DnsSecurityResult noData = ValidateCachedProof(
                        request.Name,
                        typeValue,
                        classValue,
                        false,
                        entry,
                        options.Security);
                    if (noData.Status == DnsSecurityStatus.Secure)
                    {
                        return CreateAggressiveResult(
                            request,
                            DnsResolveStatus.NoData,
                            noData.Status,
                            entry,
                            now);
                    }
                }

                index = index - 1;
            }
        }

        return null;
    }

    static DnsSecurityResult AuthenticateProofs(
        DnsIterativeResolverState state,
        DnsMessage response,
        List<DnsResourceRecord> records,
        List<DnsKeyMaterial> keys)
    {
        List<DnsResourceRecordSet> sets =
            RecordSetOperations.GroupRecordSets(records);
        DnsSecurityResult aggregate = CreateStatus(
            DnsSecurityStatus.Secure);
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            List<DnsSignatureData> signatures = FindSignatures(
                response,
                set.Name,
                EffectiveValue(set.RecordTypeValue, (int)set.RecordType),
                EffectiveValue(set.ClassValue, (int)set.Class));
            DnsSecurityResult validated =
                DnssecRecordSetValidator.ValidateRecordSet(
                    set,
                    keys,
                    signatures,
                    state.Options.Security);
            AppendDiagnostics(
                aggregate.Diagnostics,
                validated.Diagnostics);
            if (validated.Status != DnsSecurityStatus.Secure)
            {
                aggregate.Status = validated.Status ==
                    DnsSecurityStatus.Insecure ?
                    DnsSecurityStatus.Bogus : validated.Status;
                return aggregate;
            }

            aggregate.ValidatedRecordSets.Add(set);
            index = index + 1;
        }

        return aggregate;
    }

    static List<DnsKeyMaterial> ReadSecureKeys(
        DnsIterativeResolverState state,
        IDnsCache cache)
    {
        DnsClockValue now = state.ResolverClock.ReadClock();
        int classValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        DnssecCachedKeys cached = DnssecChainValidator.ReadCachedKeys(
            state.CurrentDelegation.Zone,
            classValue,
            cache,
            now);
        if (!cached.Found || cached.RecordSet == null)
        {
            return new List<DnsKeyMaterial>();
        }

        return DnssecChainValidator.ExtractKeys(
            state.CurrentDelegation.Zone,
            classValue,
            cached.RecordSet);
    }

    static List<DnsSignatureData> FindSignatures(
        DnsMessage response,
        DnsName owner,
        int typeValue,
        int classValue)
    {
        List<DnsSignatureData> result =
            new List<DnsSignatureData>();
        AddSignatures(
            result,
            response.Answers,
            owner,
            typeValue,
            classValue);
        AddSignatures(
            result,
            response.Authorities,
            owner,
            typeValue,
            classValue);
        AddSignatures(
            result,
            response.Additionals,
            owner,
            typeValue,
            classValue);
        return result;
    }

    static void AddSignatures(
        List<DnsSignatureData> destination,
        List<DnsResourceRecord> records,
        DnsName owner,
        int typeValue,
        int classValue)
    {
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
                destination.Add(record.Data.Signature);
            }

            index = index + 1;
        }
    }

    static List<DnsResourceRecord> FindRecordsByType(
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

    static DnsResourceRecordSet? ReadDnskeySet(
        DnsMessage response,
        DnsName zone,
        int classValue)
    {
        List<DnsResourceRecord> matching =
            new List<DnsResourceRecord>();
        int index = 0;
        while (index < response.Answers.Count)
        {
            DnsResourceRecord record = response.Answers[index];
            if (record != null && record.Data != null &&
                record.Data.Key != null &&
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) ==
                    (int)DnsRecordType.Dnskey &&
                EffectiveValue(record.ClassValue, (int)record.Class) ==
                    classValue &&
                RecordSetOperations.NamesEqual(record.Name, zone))
            {
                matching.Add(record);
            }

            index = index + 1;
        }

        List<DnsResourceRecordSet> sets =
            RecordSetOperations.GroupRecordSets(matching);
        if (sets.Count != 1)
        {
            return null;
        }

        return sets[0];
    }

    static List<DnsDsData> ReadDelegationSigners(
        DnsDelegation delegation)
    {
        List<DnsDsData> result = new List<DnsDsData>();
        int setIndex = 0;
        while (setIndex < delegation.AuthorityRecordSets.Count)
        {
            DnsResourceRecordSet set =
                delegation.AuthorityRecordSets[setIndex];
            if (set != null && EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType) == (int)DnsRecordType.Ds &&
                RecordSetOperations.NamesEqual(set.Name, delegation.Zone))
            {
                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    if (record != null && record.Data != null &&
                        record.Data.Ds != null)
                    {
                        result.Add(record.Data.Ds);
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }

        return result;
    }

    static bool HasTrustAnchor(
        DnsSecurityPolicy policy,
        DnsName zone)
    {
        if (policy.TrustAnchors == null)
        {
            return false;
        }

        int index = 0;
        while (index < policy.TrustAnchors.Count)
        {
            DnsTrustAnchor anchor = policy.TrustAnchors[index];
            if (anchor != null && anchor.Enabled && !anchor.Revoked &&
                RecordSetOperations.NamesEqual(anchor.Name, zone))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
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

    static DnsSecurityResult ValidateCachedProof(
        DnsName name,
        int typeValue,
        int classValue,
        bool nameError,
        DnsAggressiveDenialEntry entry,
        DnsSecurityPolicy policy)
    {
        if (!entry.Nsec3)
        {
            DnsNsecProofRequest request = new DnsNsecProofRequest();
            request.Zone = entry.Zone;
            request.QueryName = name;
            request.QueryTypeValue = typeValue;
            request.ClassValue = classValue;
            request.NameError = nameError;
            request.Records = entry.Records;
            request.Policy = policy;
            return NsecDenialValidator.ValidateDenial(request);
        }

        DnsNsec3ProofRequest nsec3 = new DnsNsec3ProofRequest();
        nsec3.Zone = entry.Zone;
        nsec3.QueryName = name;
        nsec3.QueryTypeValue = typeValue;
        nsec3.ClassValue = classValue;
        nsec3.NameError = nameError;
        nsec3.OptOutAllowed = typeValue == (int)DnsRecordType.Ds;
        nsec3.Records = entry.Records;
        nsec3.Policy = policy;
        return Nsec3DenialValidator.ValidateDenial(nsec3);
    }

    static DnsResolveResult CreateAggressiveResult(
        DnsResolveRequest request,
        DnsResolveStatus status,
        DnsSecurityStatus securityStatus,
        DnsAggressiveDenialEntry entry,
        DnsClockValue now)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Status = status;
        result.CanonicalName = RecordSetOperations.CopyName(
            request.Name,
            false);
        result.SecurityStatus = securityStatus;
        result.FromCache = true;
        result.HasTimeToLive = true;
        long remaining = entry.ExpiresMonotonicMilliseconds -
            now.MonotonicMilliseconds;
        result.TimeToLive = (uint)(remaining / 1000);
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.None;
        diagnostic.Field = "cache.aggressiveDenial";
        diagnostic.Reason =
            "Authenticated denial records proved the negative answer without a query.";
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static bool NameWithinZone(DnsName name, DnsName zone)
    {
        DnsNameOperationResult suffix =
            DnsNameOperations.HasNameSuffix(name, zone);
        return suffix.Success && suffix.BooleanValue;
    }

    static void RemoveZoneEntry(
        List<DnsAggressiveDenialEntry> entries,
        DnsName zone,
        int classValue,
        bool nsec3)
    {
        int index = entries.Count - 1;
        while (index >= 0)
        {
            DnsAggressiveDenialEntry entry = entries[index];
            if (entry.ClassValue == classValue && entry.Nsec3 == nsec3 &&
                RecordSetOperations.NamesEqual(entry.Zone, zone))
            {
                entries.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static long AddSaturated(long value, long amount)
    {
        if (value > long.MaxValue - amount)
        {
            return long.MaxValue;
        }

        return value + amount;
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

    static DnsSecurityResult DiagnosticStatus(
        DnsSecurityStatus status,
        string field,
        string reason)
    {
        DnsSecurityResult result = CreateStatus(status);
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
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
}
