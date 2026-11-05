using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace dns;

internal class DnsAuthoritativeAnswerPopulation
{
    internal bool Success = true;
    internal bool Answered;
    internal bool Alias;
    internal bool Referral;
    internal bool ReferralData;
    internal bool Negative;
    internal bool NameError;
    internal bool Continue;
    internal string Reason = string.Empty;
}

internal class DnsAuthoritativeAliasState
{
    internal DnsAuthoritativeRequestState RequestState =
        new DnsAuthoritativeRequestState();
    internal DnsAuthoritativeResult Result = new DnsAuthoritativeResult();
    internal DnsMessage Response = new DnsMessage();
    internal DnsZone Zone = new DnsZone();
    internal DnsName CurrentName = new DnsName();
    internal List<DnsName> VisitedNames = new List<DnsName>();
    internal List<DnsAuthoritativeWildcardExpansion> WildcardExpansions =
        new List<DnsAuthoritativeWildcardExpansion>();
    internal int AliasCount;
}

internal class DnsAuthoritativeWildcardExpansion
{
    internal DnsZone Zone = new DnsZone();
    internal DnsName QueryName = new DnsName();
    internal bool Positive;
}

internal static class AuthoritativeAnswerOperations
{
    internal static DnsAuthoritativeAnswerPopulation PopulateAnswer(
        DnsAuthoritativeRequestState requestState,
        DnsZone zone,
        DnsAuthoritativeResult result,
        DnsMessage response)
    {
        DnsAuthoritativeAnswerPopulation population =
            new DnsAuthoritativeAnswerPopulation();
        if (requestState.Question == null)
        {
            population.Success = false;
            population.Reason = "An authoritative answer requires one question.";
            return population;
        }

        DnsAuthoritativeAliasState state = new DnsAuthoritativeAliasState();
        state.RequestState = requestState;
        state.Result = result;
        state.Response = response;
        state.Zone = zone;
        state.CurrentName = RecordSetOperations.CopyName(
            requestState.Question.Name,
            false);
        state.VisitedNames.Add(RecordSetOperations.CopyName(
            state.CurrentName,
            true));
        while (true)
        {
            DnsAuthoritativeAnswerPopulation step = PopulateCurrentName(state);
            if (!step.Continue)
            {
                step.Alias = step.Alias || state.AliasCount > 0;
                if (step.Referral && state.AliasCount > 0)
                {
                    step.Referral = false;
                }

                if (step.Success)
                {
                    AddOptionalAdditionalRecords(state);
                    DnsAuthoritativeDnssecResult dnssec =
                        AuthoritativeDnssecOperations.ApplyDnssec(
                            state,
                            step);
                    if (!dnssec.Success)
                    {
                        step.Success = false;
                        step.Reason = dnssec.Reason;
                    }
                }

                state.Response.Header.AnswerCount =
                    state.Response.Answers.Count;
                state.Response.Header.AuthorityCount =
                    state.Response.Authorities.Count;
                state.Response.Header.AdditionalCount =
                    state.Response.Additionals.Count;
                return step;
            }
        }
    }

    static DnsAuthoritativeAnswerPopulation PopulateCurrentName(
        DnsAuthoritativeAliasState state)
    {
        int requestedType = state.RequestState.RecordTypeValue;
        int requestedClass = state.RequestState.ClassValue;
        DnsZoneNode? delegation =
            state.RequestState.ZoneStore.FindDelegationNode(
                state.Zone,
                state.CurrentName);
        if (delegation != null)
        {
            bool atCut = RecordSetOperations.NamesEqual(
                delegation.Name,
                state.CurrentName);
            if (!(atCut && requestedType == (int)DnsRecordType.Ds))
            {
                return PopulateReferral(
                    state,
                    delegation,
                    requestedClass);
            }
        }

        DnsZoneNode? exact = state.RequestState.ZoneStore.FindExactNode(
            state.Zone,
            state.CurrentName);
        if (exact != null)
        {
            DnsResourceRecordSet? requested = FindRecordSet(
                exact,
                requestedType,
                requestedClass);
            if (requested != null)
            {
                AddAnswerSet(
                    state,
                    CreateAnswerSet(requested, state.CurrentName));
                return AnsweredPopulation();
            }

            DnsResourceRecordSet? cname = FindRecordSet(
                exact,
                (int)DnsRecordType.Cname,
                requestedClass);
            if (cname != null &&
                requestedType != (int)DnsRecordType.Cname)
            {
                return FollowCname(state, cname, state.CurrentName);
            }

            if (requestedType == (int)DnsRecordType.Any &&
                AddAnyAnswer(state, exact, state.CurrentName))
            {
                return AnsweredPopulation();
            }
        }

        DnsResourceRecordSet? dname = FindClosestDname(
            state.Zone,
            state.CurrentName,
            requestedClass);
        if (dname != null)
        {
            return FollowDname(state, dname);
        }

        if (exact == null)
        {
            DnsZoneNode? wildcard =
                state.RequestState.ZoneStore.FindWildcardNode(
                    state.Zone,
                    state.CurrentName);
            if (wildcard != null)
            {
                DnsResourceRecordSet? requested = FindRecordSet(
                    wildcard,
                    requestedType,
                    requestedClass);
                if (requested != null)
                {
                    RecordWildcardExpansion(state, true);
                    AddAnswerSet(
                        state,
                        CreateAnswerSet(requested, state.CurrentName));
                    return AnsweredPopulation();
                }

                DnsResourceRecordSet? cname = FindRecordSet(
                    wildcard,
                    (int)DnsRecordType.Cname,
                    requestedClass);
                if (cname != null &&
                    requestedType != (int)DnsRecordType.Cname)
                {
                    RecordWildcardExpansion(state, true);
                    DnsResourceRecordSet synthesized = CreateAnswerSet(
                        cname,
                        state.CurrentName);
                    return FollowCname(
                        state,
                        synthesized,
                        state.CurrentName);
                }

                if (requestedType == (int)DnsRecordType.Any &&
                    AddAnyAnswer(state, wildcard, state.CurrentName))
                {
                    RecordWildcardExpansion(state, true);
                    return AnsweredPopulation();
                }

                RecordWildcardExpansion(state, false);
                return PopulateNegative(state, false);
            }
        }

        return PopulateNegative(state, exact == null);
    }

    static void RecordWildcardExpansion(
        DnsAuthoritativeAliasState state,
        bool positive)
    {
        DnsAuthoritativeWildcardExpansion expansion =
            new DnsAuthoritativeWildcardExpansion();
        expansion.Zone = state.Zone;
        expansion.QueryName = RecordSetOperations.CopyName(
            state.CurrentName,
            false);
        expansion.Positive = positive;
        state.WildcardExpansions.Add(expansion);
    }

    static DnsAuthoritativeAnswerPopulation PopulateNegative(
        DnsAuthoritativeAliasState state,
        bool nameError)
    {
        DnsZoneNode? apex = FindNode(state.Zone.Nodes, state.Zone.Apex);
        if (apex == null)
        {
            return FailedPopulation(
                "An authoritative negative answer requires a zone apex.");
        }

        DnsResourceRecordSet? startOfAuthority = FindRecordSet(
            apex,
            (int)DnsRecordType.Soa,
            state.RequestState.ClassValue);
        if (startOfAuthority == null)
        {
            return FailedPopulation(
                "An authoritative negative answer requires an SOA RRset.");
        }

        DnsResourceRecordSet negativeSoa = CreateAnswerSet(
            startOfAuthority,
            startOfAuthority.Name);
        if (!SetNegativeTimeToLive(negativeSoa))
        {
            return FailedPopulation(
                "An authoritative negative answer requires valid SOA data.");
        }

        AddAuthoritySet(state, negativeSoa);
        DnsAuthoritativeAnswerPopulation result = AnsweredPopulation();
        result.Negative = true;
        result.NameError = nameError;
        return result;
    }

    static bool SetNegativeTimeToLive(DnsResourceRecordSet startOfAuthority)
    {
        if (startOfAuthority.Records.Count != 1)
        {
            return false;
        }

        DnsResourceRecord record = startOfAuthority.Records[0];
        if (record.Data == null || record.Data.Soa == null)
        {
            return false;
        }

        uint timeToLive = startOfAuthority.TimeToLive;
        if (record.Data.Soa.Minimum < timeToLive)
        {
            timeToLive = record.Data.Soa.Minimum;
        }

        startOfAuthority.TimeToLive = timeToLive;
        record.TimeToLive = timeToLive;
        return true;
    }

    static DnsAuthoritativeAnswerPopulation PopulateReferral(
        DnsAuthoritativeAliasState state,
        DnsZoneNode delegation,
        int classValue)
    {
        DnsResourceRecordSet? nameServers = FindRecordSet(
            delegation,
            (int)DnsRecordType.Ns,
            classValue);
        if (nameServers == null)
        {
            return FailedPopulation(
                "A delegation referral requires an NS RRset.");
        }

        DnsResourceRecordSet authority = CreateAnswerSet(
            nameServers,
            nameServers.Name);
        AddAuthoritySet(state, authority);
        int index = 0;
        while (index < nameServers.Records.Count)
        {
            DnsName? target = ReadAdditionalTarget(
                nameServers.Records[index],
                (int)DnsRecordType.Ns,
                nameServers.Name);
            if (target != null && IsNameBelow(target, delegation.Name))
            {
                AddAddressesFromZone(state, state.Zone, target);
            }

            index = index + 1;
        }

        DnsAuthoritativeAnswerPopulation result = AnsweredPopulation();
        result.Referral = true;
        result.ReferralData = true;
        return result;
    }

    static DnsAuthoritativeAnswerPopulation FollowCname(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet cname,
        DnsName owner)
    {
        if (cname.Records.Count != 1)
        {
            return FailedPopulation(
                "A CNAME answer requires exactly one valid target record.");
        }

        DnsResourceRecord record = cname.Records[0];
        DnsRecordData? data = record.Data;
        if (data == null || data.Name == null)
        {
            return FailedPopulation(
                "A CNAME answer requires exactly one valid target record.");
        }

        DnsName target = data.Name.Name;
        RawDnsNameResult validated = RawDnsName.ValidateName(target);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            return FailedPopulation(
                "A CNAME answer target must be an absolute DNS name.");
        }

        DnsResourceRecordSet answerSet = CreateAnswerSet(cname, owner);
        AddAnswerSet(state, answerSet);
        return AdvanceAlias(state, validated.Name);
    }

    static DnsAuthoritativeAnswerPopulation FollowDname(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet dname)
    {
        if (dname.Records.Count != 1)
        {
            return FailedPopulation(
                "A DNAME answer requires exactly one valid target record.");
        }

        DnsResourceRecord record = dname.Records[0];
        DnsRecordData? data = record.Data;
        if (data == null || data.Name == null)
        {
            return FailedPopulation(
                "A DNAME answer requires exactly one valid target record.");
        }

        DnsNameOperationResult relative = DnsNameOperations.RelativizeName(
            state.CurrentName,
            dname.Name);
        if (!relative.Success || relative.Name == null)
        {
            return FailedPopulation(
                "The DNAME owner is not an ancestor of the queried name.");
        }

        DnsNameOperationResult combined = DnsNameOperations.ConcatenateNames(
            relative.Name,
            data.Name.Name);
        if (!combined.Success || combined.Name == null ||
            !combined.Name.IsAbsolute)
        {
            return FailedPopulation(
                "The synthesized DNAME target is not a valid DNS name.");
        }

        AddAnswerSet(state, CreateAnswerSet(dname, dname.Name));
        DnsResourceRecordSet cname = CreateSynthesizedCname(
            state.CurrentName,
            combined.Name,
            dname,
            state.RequestState.ClassValue);
        AddAnswerSet(state, cname);
        return AdvanceAlias(state, combined.Name);
    }

    static DnsAuthoritativeAnswerPopulation AdvanceAlias(
        DnsAuthoritativeAliasState state,
        DnsName target)
    {
        if (state.AliasCount >=
            state.RequestState.Request.Options.MaximumAliasDepth)
        {
            return FailedPopulation(
                "The authoritative alias chain exceeds its configured depth.");
        }

        if (ContainsName(state.VisitedNames, target))
        {
            return FailedPopulation(
                "The authoritative alias chain contains a loop.");
        }

        state.AliasCount = state.AliasCount + 1;
        state.CurrentName = RecordSetOperations.CopyName(target, false);
        state.VisitedNames.Add(RecordSetOperations.CopyName(target, true));
        DnsZone? nextZone = state.RequestState.ZoneStore.FindBestZone(
            target,
            (DnsClass)state.RequestState.ClassValue,
            state.RequestState.ClassValue);
        if (nextZone == null)
        {
            DnsAuthoritativeAnswerPopulation result = AnsweredPopulation();
            result.Alias = true;
            return result;
        }

        state.Zone = nextZone;
        DnsAuthoritativeAnswerPopulation continued =
            new DnsAuthoritativeAnswerPopulation();
        continued.Alias = true;
        continued.Continue = true;
        return continued;
    }

    static DnsResourceRecordSet? FindClosestDname(
        DnsZone zone,
        DnsName name,
        int classValue)
    {
        DnsName current = CreateParentName(name);
        while (current.Labels.Count >= zone.Apex.Labels.Count)
        {
            DnsZoneNode? node = FindNode(zone.Nodes, current);
            if (node != null)
            {
                DnsResourceRecordSet? set = FindRecordSet(
                    node,
                    (int)DnsRecordType.Dname,
                    classValue);
                if (set != null)
                {
                    return set;
                }
            }

            if (RecordSetOperations.NamesEqual(current, zone.Apex))
            {
                return null;
            }

            current = CreateParentName(current);
        }

        return null;
    }

    static DnsZoneNode? FindNode(
        List<DnsZoneNode> nodes,
        DnsName name)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (RecordSetOperations.NamesEqual(nodes[index].Name, name))
            {
                return nodes[index];
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

    static bool AddAnyAnswer(
        DnsAuthoritativeAliasState state,
        DnsZoneNode node,
        DnsName owner)
    {
        int classValue = state.RequestState.ClassValue;
        bool hasRecords = false;
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet candidate = node.RecordSets[index];
            int candidateClass = RecordSetOperations.EffectiveValue(
                candidate.ClassValue,
                (int)candidate.Class);
            if (candidateClass == classValue && candidate.Records.Count > 0)
            {
                hasRecords = true;
                if (!state.RequestState.Request.Options.MinimizeAnyResponses)
                {
                    AddAnswerSet(
                        state,
                        CreateAnswerSet(candidate, owner));
                }
            }

            index = index + 1;
        }

        if (hasRecords &&
            state.RequestState.Request.Options.MinimizeAnyResponses)
        {
            AddAnswerSet(
                state,
                CreateMinimalAnyAnswer(owner, classValue));
        }

        return hasRecords;
    }

    static DnsResourceRecordSet CreateMinimalAnyAnswer(
        DnsName owner,
        int classValue)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(owner, false);
        result.RecordType = DnsRecordType.Hinfo;
        result.RecordTypeValue = (int)DnsRecordType.Hinfo;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.TimeToLive = 3600;

        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = RecordSetOperations.CopyName(owner, false);
        record.RecordType = DnsRecordType.Hinfo;
        record.RecordTypeValue = (int)DnsRecordType.Hinfo;
        record.Class = (DnsClass)classValue;
        record.ClassValue = classValue;
        record.TimeToLive = 3600;
        record.Data = new DnsRecordData();
        record.Data.CharacterStrings = new DnsCharacterStringsData();
        record.Data.CharacterStrings.Strings.Add(
            Encoding.ASCII.GetBytes("RFC8482"));
        record.Data.CharacterStrings.Strings.Add([]);
        result.Records.Add(record);
        return result;
    }

    static DnsResourceRecordSet CreateAnswerSet(
        DnsResourceRecordSet source,
        DnsName owner)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(owner, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.TimeToLive = source.TimeToLive;
        int index = 0;
        while (index < source.Records.Count)
        {
            DnsResourceRecord copied =
                DnsZoneSnapshotOperations.CopyRecord(source.Records[index]);
            copied.Name = RecordSetOperations.CopyName(owner, false);
            if (!ContainsRecordData(result.Records, copied))
            {
                result.Records.Add(copied);
            }

            index = index + 1;
        }

        return result;
    }

    static DnsResourceRecordSet CreateSynthesizedCname(
        DnsName owner,
        DnsName target,
        DnsResourceRecordSet dname,
        int classValue)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(owner, false);
        result.RecordType = DnsRecordType.Cname;
        result.RecordTypeValue = (int)DnsRecordType.Cname;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.TimeToLive = dname.TimeToLive;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = RecordSetOperations.CopyName(owner, false);
        record.RecordType = DnsRecordType.Cname;
        record.RecordTypeValue = (int)DnsRecordType.Cname;
        record.Class = (DnsClass)classValue;
        record.ClassValue = classValue;
        record.TimeToLive = dname.TimeToLive;
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = RecordSetOperations.CopyName(target, false);
        result.Records.Add(record);
        return result;
    }

    static void AddAnswerSet(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet set)
    {
        DnsResourceRecordSet? existing = FindSelectedSet(
            state.Result.AnswerRecordSets,
            set);
        if (existing == null)
        {
            state.Result.AnswerRecordSets.Add(set);
            AppendRecords(state.Response.Answers, set.Records);
            return;
        }

        int index = 0;
        while (index < set.Records.Count)
        {
            if (!ContainsRecordData(existing.Records, set.Records[index]))
            {
                DnsResourceRecord copied =
                    DnsZoneSnapshotOperations.CopyRecord(set.Records[index]);
                existing.Records.Add(copied);
                state.Response.Answers.Add(
                    DnsZoneSnapshotOperations.CopyRecord(copied));
            }

            index = index + 1;
        }
    }

    static void AddAuthoritySet(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet set)
    {
        if (FindSelectedSet(state.Result.AuthorityRecordSets, set) != null)
        {
            return;
        }

        state.Result.AuthorityRecordSets.Add(set);
        AppendRecords(state.Response.Authorities, set.Records);
    }

    static void AddAdditionalSet(
        DnsAuthoritativeAliasState state,
        DnsResourceRecordSet set)
    {
        if (FindSelectedSet(state.Result.AdditionalRecordSets, set) != null)
        {
            return;
        }

        state.Result.AdditionalRecordSets.Add(set);
        AppendRecords(state.Response.Additionals, set.Records);
    }

    static DnsResourceRecordSet? FindSelectedSet(
        List<DnsResourceRecordSet> sets,
        DnsResourceRecordSet candidate)
    {
        int index = 0;
        while (index < sets.Count)
        {
            if (RecordSetOperations.SetKeysEqual(sets[index], candidate))
            {
                return sets[index];
            }

            index = index + 1;
        }

        return null;
    }

    static void AddOptionalAdditionalRecords(
        DnsAuthoritativeAliasState state)
    {
        if (!state.RequestState.Request.Options.IncludeAdditionalRecords ||
            state.RequestState.Request.Options.MinimalResponses)
        {
            return;
        }

        int setIndex = 0;
        while (setIndex < state.Result.AnswerRecordSets.Count)
        {
            DnsResourceRecordSet set =
                state.Result.AnswerRecordSets[setIndex];
            int typeValue = RecordSetOperations.EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue == (int)DnsRecordType.Ns ||
                typeValue == (int)DnsRecordType.Mx ||
                typeValue == (int)DnsRecordType.Srv ||
                typeValue == (int)DnsRecordType.Svcb ||
                typeValue == (int)DnsRecordType.Https)
            {
                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsName? target = ReadAdditionalTarget(
                        set.Records[recordIndex],
                        typeValue,
                        set.Name);
                    if (target != null)
                    {
                        DnsZone? targetZone =
                            state.RequestState.ZoneStore.FindBestZone(
                                target,
                                (DnsClass)state.RequestState.ClassValue,
                                state.RequestState.ClassValue);
                        if (targetZone != null)
                        {
                            AddAddressesFromZone(
                                state,
                                targetZone,
                                target);
                        }
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }
    }

    static DnsName? ReadAdditionalTarget(
        DnsResourceRecord record,
        int typeValue,
        DnsName owner)
    {
        DnsRecordData? data = record.Data;
        DnsName? target = null;
        if (data == null)
        {
            return null;
        }

        if (typeValue == (int)DnsRecordType.Ns && data.Name != null)
        {
            target = data.Name.Name;
        }
        else if (typeValue == (int)DnsRecordType.Mx &&
            data.PreferenceName != null)
        {
            target = data.PreferenceName.Name;
            if (target.Labels.Count == 0)
            {
                return null;
            }
        }
        else if (typeValue == (int)DnsRecordType.Srv && data.Srv != null)
        {
            target = data.Srv.Target;
            if (target.Labels.Count == 0)
            {
                return null;
            }
        }
        else if ((typeValue == (int)DnsRecordType.Svcb ||
            typeValue == (int)DnsRecordType.Https) && data.Svcb != null)
        {
            target = data.Svcb.TargetName;
            if (target.Labels.Count == 0)
            {
                target = owner;
            }
        }

        if (target == null)
        {
            return null;
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(target);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            return null;
        }

        return validated.Name;
    }

    static void AddAddressesFromZone(
        DnsAuthoritativeAliasState state,
        DnsZone zone,
        DnsName target)
    {
        DnsZoneNode? node = state.RequestState.ZoneStore.FindExactNode(
            zone,
            target);
        if (node == null)
        {
            return;
        }

        DnsResourceRecordSet? addresses = FindRecordSet(
            node,
            (int)DnsRecordType.A,
            state.RequestState.ClassValue);
        if (addresses != null)
        {
            AddAdditionalSet(
                state,
                CreateAnswerSet(addresses, addresses.Name));
        }

        DnsResourceRecordSet? addresses6 = FindRecordSet(
            node,
            (int)DnsRecordType.Aaaa,
            state.RequestState.ClassValue);
        if (addresses6 != null)
        {
            AddAdditionalSet(
                state,
                CreateAnswerSet(addresses6, addresses6.Name));
        }
    }

    static bool IsNameBelow(DnsName name, DnsName suffix)
    {
        DnsNameOperationResult result = DnsNameOperations.HasNameSuffix(
            name,
            suffix);
        return result.Success && result.BooleanValue;
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

    static bool ContainsRecordData(
        List<DnsResourceRecord> records,
        DnsResourceRecord candidate)
    {
        int index = 0;
        while (index < records.Count)
        {
            if (RecordsHaveSameData(records[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool RecordsHaveSameData(
        DnsResourceRecord left,
        DnsResourceRecord right)
    {
        if (left.UseRawData || right.UseRawData)
        {
            return left.UseRawData == right.UseRawData &&
                RecordSetOperations.BytesEqual(
                    left.RawData,
                    right.RawData);
        }

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.IncludeFields = true;
        string leftData = JsonSerializer.Serialize(left.Data, options);
        string rightData = JsonSerializer.Serialize(right.Data, options);
        if (left.Data != null || right.Data != null)
        {
            return leftData == rightData;
        }

        return RecordSetOperations.BytesEqual(left.RawData, right.RawData);
    }

    static bool ContainsName(List<DnsName> names, DnsName candidate)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (RecordSetOperations.NamesEqual(names[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsName CreateParentName(DnsName name)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        int index = 1;
        while (index < name.Labels.Count)
        {
            result.Labels.Add(RecordSetOperations.CopyBytes(
                name.Labels[index]));
            index = index + 1;
        }

        return result;
    }

    static DnsAuthoritativeAnswerPopulation AnsweredPopulation()
    {
        DnsAuthoritativeAnswerPopulation result =
            new DnsAuthoritativeAnswerPopulation();
        result.Answered = true;
        return result;
    }

    static DnsAuthoritativeAnswerPopulation FailedPopulation(string reason)
    {
        DnsAuthoritativeAnswerPopulation result =
            new DnsAuthoritativeAnswerPopulation();
        result.Success = false;
        result.Reason = reason;
        return result;
    }
}
