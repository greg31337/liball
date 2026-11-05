using System.Collections.Generic;

namespace dns;

internal class DnsAuthoritativeDnssecResult
{
    internal bool Success = true;
    internal bool SignedData;
    internal bool AllAuthentic = true;
    internal bool Insecure;
    internal string Reason = string.Empty;
}

internal class DnsAuthoritativeProofInputs
{
    internal List<DnsName> ExistingNames = new List<DnsName>();
    internal List<DnsResourceRecord> NsecRecords =
        new List<DnsResourceRecord>();
    internal List<DnsResourceRecord> Nsec3Records =
        new List<DnsResourceRecord>();
}

internal static class AuthoritativeDnssecOperations
{
    const int ExactNoDataProof = 0;
    const int NameErrorProof = 1;
    const int WildcardNoDataProof = 2;
    const int WildcardPositiveProof = 3;

    internal static DnsAuthoritativeDnssecResult ApplyDnssec(
        DnsAuthoritativeAliasState state,
        DnsAuthoritativeAnswerPopulation population)
    {
        DnsAuthoritativeDnssecResult result =
            new DnsAuthoritativeDnssecResult();
        bool dnssecRequested = RequestWantsDnssec(state.RequestState);
        bool authenticDataRequested = RequestWantsAuthenticData(
            state.RequestState);
        bool includeDnssec =
            state.RequestState.Request.Options.ServeDnssec &&
            dnssecRequested;
        bool inspectAuthenticity = includeDnssec ||
            (state.RequestState.Request.Options.SetAuthenticData &&
             authenticDataRequested);
        if (!inspectAuthenticity)
        {
            return result;
        }

        if (population.ReferralData)
        {
            ApplyReferralSecurity(
                state,
                result,
                includeDnssec);
        }

        if (result.Success)
        {
            ApplyWildcardProofs(
                state,
                result,
                includeDnssec);
        }

        if (result.Success && population.Negative &&
            !TerminalNegativeIsWildcard(state))
        {
            int proofKind = population.NameError ?
                NameErrorProof : ExactNoDataProof;
            ApplyProof(
                state,
                result,
                state.Zone,
                state.CurrentName,
                state.RequestState.RecordTypeValue,
                proofKind,
                state.RequestState.RecordTypeValue ==
                    (int)DnsRecordType.Ds,
                includeDnssec);
        }

        if (result.Success)
        {
            AddSectionSignatures(
                state,
                result,
                population,
                includeDnssec);
        }

        if (!result.Success)
        {
            return result;
        }

        if (result.Insecure)
        {
            state.Result.SecurityStatus = DnsSecurityStatus.Insecure;
            result.AllAuthentic = false;
        }
        else if (result.SignedData && result.AllAuthentic)
        {
            state.Result.SecurityStatus = DnsSecurityStatus.Secure;
        }

        if (state.RequestState.Request.Options.SetAuthenticData &&
            authenticDataRequested && result.SignedData &&
            result.AllAuthentic && !result.Insecure)
        {
            SetAuthenticData(state.Response);
        }

        return result;
    }

    static bool RequestWantsDnssec(DnsAuthoritativeRequestState state)
    {
        if (!state.Request.Options.EnableEdns)
        {
            return false;
        }

        DnsResourceRecord? opt = MessageOperations.FindOptRecord(
            state.Request.Message);
        if (opt == null)
        {
            return false;
        }

        int flags = (int)(opt.TimeToLive & 65535);
        if (opt.Data != null && opt.Data.Edns != null)
        {
            flags = EdnsRecordDataCodec.ReadEffectiveFlags(
                opt.Data.Edns);
        }

        return (flags & (int)DnsEdnsFlag.DnssecAnswerOk) != 0;
    }

    static bool RequestWantsAuthenticData(
        DnsAuthoritativeRequestState state)
    {
        return RequestWantsDnssec(state) ||
            (state.RequestFlags & (int)DnsHeaderFlag.AuthenticData) != 0;
    }

    static void ApplyReferralSecurity(
        DnsAuthoritativeAliasState state,
        DnsAuthoritativeDnssecResult result,
        bool includeDnssec)
    {
        if (!ZoneIsSigned(state.Zone))
        {
            result.AllAuthentic = false;
            return;
        }

        result.SignedData = true;
        DnsZoneNode? delegation =
            state.RequestState.ZoneStore.FindDelegationNode(
                state.Zone,
                state.CurrentName);
        if (delegation == null)
        {
            Fail(result,
                "DNSSEC referral data requires a delegation node.");
            return;
        }

        DnsResourceRecordSet? delegationSigner = FindRecordSet(
            delegation,
            (int)DnsRecordType.Ds,
            state.RequestState.ClassValue);
        if (delegationSigner != null)
        {
            if (includeDnssec)
            {
                AddAuthoritySet(
                    state,
                    CopyRecordSet(delegationSigner));
            }

            return;
        }

        ApplyProof(
            state,
            result,
            state.Zone,
            delegation.Name,
            (int)DnsRecordType.Ds,
            ExactNoDataProof,
            true,
            includeDnssec);
    }

    static void ApplyWildcardProofs(
        DnsAuthoritativeAliasState state,
        DnsAuthoritativeDnssecResult result,
        bool includeDnssec)
    {
        int index = 0;
        while (index < state.WildcardExpansions.Count && result.Success)
        {
            DnsAuthoritativeWildcardExpansion expansion =
                state.WildcardExpansions[index];
            int proofKind = expansion.Positive ?
                WildcardPositiveProof : WildcardNoDataProof;
            ApplyProof(
                state,
                result,
                expansion.Zone,
                expansion.QueryName,
                state.RequestState.RecordTypeValue,
                proofKind,
                false,
                includeDnssec);
            index = index + 1;
        }
    }

    static bool TerminalNegativeIsWildcard(
        DnsAuthoritativeAliasState state)
    {
        if (state.WildcardExpansions.Count == 0)
        {
            return false;
        }

        DnsAuthoritativeWildcardExpansion last =
            state.WildcardExpansions[
                state.WildcardExpansions.Count - 1];
        return !last.Positive && RecordSetOperations.NamesEqual(
            last.QueryName,
            state.CurrentName);
    }

    static void ApplyProof(
        DnsAuthoritativeAliasState state,
        DnsAuthoritativeDnssecResult result,
        DnsZone zone,
        DnsName queryName,
        int queryTypeValue,
        int proofKind,
        bool optOutAllowed,
        bool includeDnssec)
    {
        if (!ZoneIsSigned(zone))
        {
            result.AllAuthentic = false;
            return;
        }

        result.SignedData = true;
        DnsAuthoritativeProofInputs inputs = CollectProofInputs(zone);
        DnsSecurityResult proof;
        if (inputs.Nsec3Records.Count > 0)
        {
            DnsNsec3ProofRequest request = CreateNsec3Request(
                state,
                zone,
                queryName,
                queryTypeValue,
                proofKind,
                optOutAllowed,
                inputs);
            if (proofKind == WildcardPositiveProof)
            {
                proof = Nsec3DenialValidator.SelectWildcardExpansionProof(
                    request);
            }
            else
            {
                proof = Nsec3DenialValidator.ValidateDenial(request);
            }
        }
        else
        {
            DnsNsecProofRequest request = CreateNsecRequest(
                state,
                zone,
                queryName,
                queryTypeValue,
                proofKind,
                inputs);
            if (proofKind == WildcardPositiveProof)
            {
                proof = NsecDenialValidator.SelectWildcardExpansionProof(
                    request);
            }
            else
            {
                proof = NsecDenialValidator.ValidateDenial(request);
            }
        }

        if (!proof.Success || proof.Status == DnsSecurityStatus.Bogus ||
            proof.Status == DnsSecurityStatus.UnsupportedAlgorithm ||
            proof.ProofRecords.Count == 0)
        {
            FailFromProof(result, proof);
            return;
        }

        if (proof.Status == DnsSecurityStatus.Insecure)
        {
            result.Insecure = true;
        }

        int index = 0;
        while (index < proof.ProofRecords.Count && result.Success)
        {
            DnsResourceRecord record = proof.ProofRecords[index];
            if (!RecordHasSignature(zone, record))
            {
                Fail(result,
                    "A precomputed denial proof requires a covering RRSIG.");
                return;
            }

            if (includeDnssec)
            {
                AddAuthoritySet(state, CreateRecordSet(record));
            }

            index = index + 1;
        }
    }

    static DnsNsecProofRequest CreateNsecRequest(
        DnsAuthoritativeAliasState state,
        DnsZone zone,
        DnsName queryName,
        int queryTypeValue,
        int proofKind,
        DnsAuthoritativeProofInputs inputs)
    {
        DnsNsecProofRequest result = new DnsNsecProofRequest();
        result.Zone = zone.Apex;
        result.QueryName = queryName;
        result.QueryTypeValue = queryTypeValue;
        result.ClassValue = state.RequestState.ClassValue;
        result.NameError = proofKind == NameErrorProof;
        result.WildcardAnswer = proofKind == WildcardNoDataProof;
        result.ExistingNames = inputs.ExistingNames;
        result.Records = inputs.NsecRecords;
        result.Policy = CreateProofPolicy(
            state.RequestState.Request.Options.Security);
        return result;
    }

    static DnsNsec3ProofRequest CreateNsec3Request(
        DnsAuthoritativeAliasState state,
        DnsZone zone,
        DnsName queryName,
        int queryTypeValue,
        int proofKind,
        bool optOutAllowed,
        DnsAuthoritativeProofInputs inputs)
    {
        DnsNsec3ProofRequest result = new DnsNsec3ProofRequest();
        result.Zone = zone.Apex;
        result.QueryName = queryName;
        result.QueryTypeValue = queryTypeValue;
        result.ClassValue = state.RequestState.ClassValue;
        result.NameError = proofKind == NameErrorProof;
        result.WildcardAnswer = proofKind == WildcardNoDataProof;
        result.OptOutAllowed = optOutAllowed;
        result.Records = inputs.Nsec3Records;
        result.Policy = CreateProofPolicy(
            state.RequestState.Request.Options.Security);
        return result;
    }

    static DnsSecurityPolicy CreateProofPolicy(DnsSecurityPolicy source)
    {
        DnsSecurityPolicy result = new DnsSecurityPolicy();
        result.MaximumNsec3Iterations = source.MaximumNsec3Iterations;
        result.MaximumProofRecords = source.MaximumProofRecords;
        result.ValidateDenial = true;
        result.TreatUnsupportedAsBogus = source.TreatUnsupportedAsBogus;
        return result;
    }

    static DnsAuthoritativeProofInputs CollectProofInputs(DnsZone zone)
    {
        DnsAuthoritativeProofInputs result =
            new DnsAuthoritativeProofInputs();
        int nodeIndex = 0;
        while (nodeIndex < zone.Nodes.Count)
        {
            DnsZoneNode node = zone.Nodes[nodeIndex];
            result.ExistingNames.Add(node.Name);
            int setIndex = 0;
            while (setIndex < node.RecordSets.Count)
            {
                DnsResourceRecordSet set = node.RecordSets[setIndex];
                int typeValue = RecordSetOperations.EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType);
                if (typeValue == (int)DnsRecordType.Nsec)
                {
                    AppendRecords(result.NsecRecords, set.Records);
                }
                else if (typeValue == (int)DnsRecordType.Nsec3)
                {
                    AppendRecords(result.Nsec3Records, set.Records);
                }

                setIndex = setIndex + 1;
            }

            nodeIndex = nodeIndex + 1;
        }

        return result;
    }

    static void AddSectionSignatures(
        DnsAuthoritativeAliasState state,
        DnsAuthoritativeDnssecResult result,
        DnsAuthoritativeAnswerPopulation population,
        bool includeDnssec)
    {
        AddSignaturesToSection(
            state,
            result,
            state.Result.AnswerRecordSets,
            0,
            population,
            includeDnssec);
        if (!result.Success)
        {
            return;
        }

        AddSignaturesToSection(
            state,
            result,
            state.Result.AuthorityRecordSets,
            1,
            population,
            includeDnssec);
        if (!result.Success)
        {
            return;
        }

        AddSignaturesToSection(
            state,
            result,
            state.Result.AdditionalRecordSets,
            2,
            population,
            includeDnssec);
    }

    static void AddSignaturesToSection(
        DnsAuthoritativeAliasState state,
        DnsAuthoritativeDnssecResult result,
        List<DnsResourceRecordSet> sets,
        int section,
        DnsAuthoritativeAnswerPopulation population,
        bool includeDnssec)
    {
        int originalCount = sets.Count;
        int index = 0;
        while (index < originalCount && result.Success)
        {
            DnsResourceRecordSet set = sets[index];
            int typeValue = RecordSetOperations.EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue != (int)DnsRecordType.Rrsig)
            {
                bool optional = section == 2 ||
                    (population.ReferralData && section == 1 &&
                     typeValue == (int)DnsRecordType.Ns);
                DnsZone? zone = state.RequestState.ZoneStore.FindBestZone(
                    set.Name,
                    (DnsClass)state.RequestState.ClassValue,
                    state.RequestState.ClassValue);
                if (zone == null || !ZoneIsSigned(zone))
                {
                    if (section != 2)
                    {
                        result.AllAuthentic = false;
                    }
                }
                else
                {
                    result.SignedData = true;
                    DnsResourceRecordSet? signatures =
                        FindCoveredSignatures(
                            state,
                            zone,
                            set);
                    if (signatures == null)
                    {
                        if (!optional && !IsSynthesizedCname(
                                state,
                                zone,
                                set))
                        {
                            Fail(result,
                                "A signed authoritative RRset requires a precomputed RRSIG.");
                            return;
                        }

                        if (section != 2 && !IsSynthesizedCname(
                                state,
                                zone,
                                set))
                        {
                            result.AllAuthentic = false;
                        }
                    }
                    else if (includeDnssec)
                    {
                        AddSignatureSetBySection(
                            state,
                            signatures,
                            section);
                    }
                }
            }

            index = index + 1;
        }
    }

    static DnsResourceRecordSet? FindCoveredSignatures(
        DnsAuthoritativeAliasState state,
        DnsZone zone,
        DnsResourceRecordSet covered)
    {
        DnsZoneNode? node =
            state.RequestState.ZoneStore.FindExactNode(
                zone,
                covered.Name);
        if (node == null)
        {
            node = state.RequestState.ZoneStore.FindWildcardNode(
                zone,
                covered.Name);
        }

        if (node == null)
        {
            return null;
        }

        DnsResourceRecordSet? source = FindRecordSet(
            node,
            RecordSetOperations.EffectiveValue(
                covered.RecordTypeValue,
                (int)covered.RecordType),
            state.RequestState.ClassValue);
        if (source == null)
        {
            return null;
        }

        DnsResourceRecordSet? signatures = FindRecordSet(
            node,
            (int)DnsRecordType.Rrsig,
            state.RequestState.ClassValue);
        if (signatures == null)
        {
            return null;
        }

        return FilterSignatures(
            signatures,
            covered.Name,
            RecordSetOperations.EffectiveValue(
                covered.RecordTypeValue,
                (int)covered.RecordType));
    }

    static DnsResourceRecordSet? FilterSignatures(
        DnsResourceRecordSet signatures,
        DnsName owner,
        int coveredType)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(owner, false);
        result.RecordType = DnsRecordType.Rrsig;
        result.RecordTypeValue = (int)DnsRecordType.Rrsig;
        result.Class = signatures.Class;
        result.ClassValue = signatures.ClassValue;
        result.TimeToLive = signatures.TimeToLive;
        int index = 0;
        while (index < signatures.Records.Count)
        {
            DnsResourceRecord source = signatures.Records[index];
            if (source.Data != null && source.Data.Signature != null &&
                RecordSetOperations.EffectiveValue(
                    source.Data.Signature.TypeCoveredValue,
                    (int)source.Data.Signature.TypeCovered) == coveredType)
            {
                DnsResourceRecord copied =
                    DnsZoneSnapshotOperations.CopyRecord(source);
                copied.Name = RecordSetOperations.CopyName(owner, false);
                result.Records.Add(copied);
            }

            index = index + 1;
        }

        if (result.Records.Count == 0)
        {
            return null;
        }

        return result;
    }

    static bool IsSynthesizedCname(
        DnsAuthoritativeAliasState state,
        DnsZone zone,
        DnsResourceRecordSet set)
    {
        int typeValue = RecordSetOperations.EffectiveValue(
            set.RecordTypeValue,
            (int)set.RecordType);
        if (typeValue != (int)DnsRecordType.Cname)
        {
            return false;
        }

        DnsZoneNode? exact = state.RequestState.ZoneStore.FindExactNode(
            zone,
            set.Name);
        DnsZoneNode? wildcard =
            state.RequestState.ZoneStore.FindWildcardNode(
                zone,
                set.Name);
        return exact == null && wildcard == null;
    }

    static bool ZoneIsSigned(DnsZone zone)
    {
        DnsZoneNode? apex = FindNode(zone, zone.Apex);
        if (apex == null)
        {
            return false;
        }

        DnsResourceRecordSet? keys = FindRecordSet(
            apex,
            (int)DnsRecordType.Dnskey,
            RecordSetOperations.EffectiveValue(
                zone.ClassValue,
                (int)zone.Class));
        DnsResourceRecordSet? signatures = FindRecordSet(
            apex,
            (int)DnsRecordType.Rrsig,
            RecordSetOperations.EffectiveValue(
                zone.ClassValue,
                (int)zone.Class));
        return keys != null && signatures != null &&
            FilterSignatures(
                signatures,
                zone.Apex,
                (int)DnsRecordType.Dnskey) != null;
    }

    static bool RecordHasSignature(
        DnsZone zone,
        DnsResourceRecord record)
    {
        DnsZoneNode? node = FindNode(zone, record.Name);
        if (node == null)
        {
            return false;
        }

        DnsResourceRecordSet? signatures = FindRecordSet(
            node,
            (int)DnsRecordType.Rrsig,
            RecordSetOperations.EffectiveValue(
                record.ClassValue,
                (int)record.Class));
        if (signatures == null)
        {
            return false;
        }

        return FilterSignatures(
            signatures,
            record.Name,
            RecordSetOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType)) != null;
    }

    static DnsZoneNode? FindNode(DnsZone zone, DnsName name)
    {
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            if (RecordSetOperations.NamesEqual(
                    zone.Nodes[index].Name,
                    name))
            {
                return zone.Nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsResourceRecordSet? FindRecordSet(
        DnsZoneNode node,
        int typeValue,
        int classValue)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet candidate = node.RecordSets[index];
            if (RecordSetOperations.EffectiveValue(
                    candidate.RecordTypeValue,
                    (int)candidate.RecordType) == typeValue &&
                RecordSetOperations.EffectiveValue(
                    candidate.ClassValue,
                    (int)candidate.Class) == classValue)
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    static DnsResourceRecordSet CopyRecordSet(
        DnsResourceRecordSet source)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.TimeToLive = source.TimeToLive;
        AppendRecords(result.Records, source.Records);
        return result;
    }

    static DnsResourceRecordSet CreateRecordSet(
        DnsResourceRecord source)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.TimeToLive = source.TimeToLive;
        result.Records.Add(DnsZoneSnapshotOperations.CopyRecord(source));
        return result;
    }

    static void AddSignatureSetBySection(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet signatures,
        int section)
    {
        if (section == 0)
        {
            AddAnswerSet(state, signatures);
        }
        else if (section == 1)
        {
            AddAuthoritySet(state, signatures);
        }
        else
        {
            AddAdditionalSet(state, signatures);
        }
    }

    static void AddAnswerSet(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet set)
    {
        AddSet(
            state.Result.AnswerRecordSets,
            state.Response.Answers,
            set);
    }

    static void AddAuthoritySet(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet set)
    {
        AddSet(
            state.Result.AuthorityRecordSets,
            state.Response.Authorities,
            set);
    }

    static void AddAdditionalSet(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet set)
    {
        AddSet(
            state.Result.AdditionalRecordSets,
            state.Response.Additionals,
            set);
    }

    static void AddSet(
        List<DnsResourceRecordSet> destination,
        List<DnsResourceRecord> responseRecords,
        DnsResourceRecordSet set)
    {
        DnsResourceRecordSet? existing = FindSelectedSet(
            destination,
            set);
        if (existing == null)
        {
            destination.Add(set);
            AppendRecords(responseRecords, set.Records);
            return;
        }

        int index = 0;
        while (index < set.Records.Count)
        {
            if (!ContainsEquivalentRecord(
                    existing.Records,
                    set.Records[index]))
            {
                DnsResourceRecord copied =
                    DnsZoneSnapshotOperations.CopyRecord(
                        set.Records[index]);
                existing.Records.Add(copied);
                responseRecords.Add(
                    DnsZoneSnapshotOperations.CopyRecord(copied));
            }

            index = index + 1;
        }
    }

    static DnsResourceRecordSet? FindSelectedSet(
        List<DnsResourceRecordSet> sets,
        DnsResourceRecordSet candidate)
    {
        int index = 0;
        while (index < sets.Count)
        {
            if (RecordSetOperations.SetKeysEqual(
                    sets[index],
                    candidate))
            {
                return sets[index];
            }

            index = index + 1;
        }

        return null;
    }

    static bool ContainsEquivalentRecord(
        List<DnsResourceRecord> records,
        DnsResourceRecord candidate)
    {
        int candidateType = RecordSetOperations.EffectiveValue(
            candidate.RecordTypeValue,
            (int)candidate.RecordType);
        int candidateCovered = ReadCoveredType(candidate);
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (RecordSetOperations.NamesEqual(
                    record.Name,
                    candidate.Name) &&
                RecordSetOperations.EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) == candidateType &&
                ReadCoveredType(record) == candidateCovered &&
                RecordSetOperations.BytesEqual(
                    record.RawData,
                    candidate.RawData))
            {
                if (candidateType != (int)DnsRecordType.Rrsig ||
                    SignaturesEqual(record, candidate))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    static int ReadCoveredType(DnsResourceRecord record)
    {
        if (record.Data == null || record.Data.Signature == null)
        {
            return -1;
        }

        return RecordSetOperations.EffectiveValue(
            record.Data.Signature.TypeCoveredValue,
            (int)record.Data.Signature.TypeCovered);
    }

    static bool SignaturesEqual(
        DnsResourceRecord left,
        DnsResourceRecord right)
    {
        if (left.Data == null || left.Data.Signature == null ||
            right.Data == null || right.Data.Signature == null)
        {
            return false;
        }

        DnsSignatureData leftData = left.Data.Signature;
        DnsSignatureData rightData = right.Data.Signature;
        return leftData.KeyTag == rightData.KeyTag &&
            leftData.AlgorithmValue == rightData.AlgorithmValue &&
            RecordSetOperations.BytesEqual(
                leftData.Signature,
                rightData.Signature);
    }

    static void AppendRecords(
        List<DnsResourceRecord> destination,
        List<DnsResourceRecord> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(DnsZoneSnapshotOperations.CopyRecord(
                source[index]));
            index = index + 1;
        }
    }

    static void SetAuthenticData(DnsMessage response)
    {
        response.Header.Flags = response.Header.Flags |
            DnsHeaderFlag.AuthenticData;
        response.Header.FlagsValue = response.Header.FlagsValue |
            (int)DnsHeaderFlag.AuthenticData;
    }

    static void FailFromProof(
        DnsAuthoritativeDnssecResult result,
        DnsSecurityResult proof)
    {
        if (proof.Diagnostics.Count > 0)
        {
            Fail(result, proof.Diagnostics[0].Reason);
            return;
        }

        Fail(result,
            "A signed authoritative response requires a precomputed denial proof.");
    }

    static void Fail(
        DnsAuthoritativeDnssecResult result,
        string reason)
    {
        result.Success = false;
        result.Reason = reason;
    }
}
