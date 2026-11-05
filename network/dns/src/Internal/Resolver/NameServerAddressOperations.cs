using System.Collections.Generic;

namespace dns;

internal class DnsNameServerAddressResult
{
    internal bool Added;
    internal bool Loop;
    internal bool Stopped;
}

internal static class NameServerAddressOperations
{
    internal static DnsNameServerAddressResult ResolveMissingAddresses(
        DnsDelegation delegation,
        DnsIterativeResolverState state,
        DnsRecursiveResolver resolver,
        List<DnsName> activeNames)
    {
        DnsNameServerAddressResult result =
            new DnsNameServerAddressResult();
        DnsClockValue now = TransportTime.ReadClock();
        int index = 0;
        while (index < delegation.NameServers.Count)
        {
            DnsName nameServer = delegation.NameServers[index];
            if (!HasAddresses(delegation, nameServer))
            {
                DnsAddressResult? cached =
                    DelegationSelectionOperations.ReadCachedAddresses(
                        nameServer,
                        resolver.Cache,
                        now);
                if (cached != null)
                {
                    delegation.GlueAddresses.Add(cached);
                    result.Added = true;
                }
                else if (ContainsName(activeNames, nameServer))
                {
                    result.Loop = true;
                    IterativeResolverOperations.AddDiagnostic(
                        state.Result,
                        DnsDiagnosticCode.ResourceLimitExceeded,
                        "dependency",
                        "A cyclic name-server address dependency was suppressed.");
                }
                else
                {
                    List<DnsName> nextActive = CopyNames(activeNames);
                    nextActive.Add(RecordSetOperations.CopyName(
                        nameServer,
                        false));
                    DnsNameServerAddressResult resolved = ResolveNameServer(
                        delegation,
                        nameServer,
                        state,
                        resolver,
                        nextActive);
                    if (resolved.Added)
                    {
                        result.Added = true;
                    }

                    if (resolved.Loop)
                    {
                        result.Loop = true;
                    }

                    if (resolved.Stopped)
                    {
                        result.Stopped = true;
                        return result;
                    }
                }
            }

            index = index + 1;
        }

        return result;
    }

    static DnsNameServerAddressResult ResolveNameServer(
        DnsDelegation destination,
        DnsName nameServer,
        DnsIterativeResolverState state,
        DnsRecursiveResolver resolver,
        List<DnsName> activeNames)
    {
        DnsNameServerAddressResult result = ResolveAddressFamily(
            destination,
            nameServer,
            DnsRecordType.A,
            1,
            4,
            4,
            state,
            resolver,
            activeNames);
        if (!result.Added && !result.Loop && !result.Stopped)
        {
            result = ResolveAddressFamily(
                destination,
                nameServer,
                DnsRecordType.Aaaa,
                28,
                6,
                16,
                state,
                resolver,
                activeNames);
        }

        return result;
    }

    static DnsNameServerAddressResult ResolveAddressFamily(
        DnsDelegation destination,
        DnsName nameServer,
        DnsRecordType recordType,
        int recordTypeValue,
        int addressFamily,
        int addressWidth,
        DnsIterativeResolverState state,
        DnsRecursiveResolver resolver,
        List<DnsName> activeNames)
    {
        DnsNameServerAddressResult result =
            new DnsNameServerAddressResult();
        DnsDelegation current =
            DelegationSelectionOperations.SelectClosestDelegation(
                nameServer,
                state.RootHints,
                resolver.Cache);
        while (true)
        {
            DnsIterativeQuestionPlan questionPlan =
                IterativeResolverOperations.CreateQuestionPlan(
                    state,
                    nameServer,
                    recordTypeValue,
                    current.Zone);
            List<DnsIterativeAuthorityCandidate> candidates =
                IterativeResolverOperations.CreateAuthorityCandidates(
                    state,
                    current);
            if (state.WorkLimitExceeded)
            {
                IterativeResolverOperations.ResourceLimitFailure(
                    state,
                    "work",
                    "The work budget was exhausted while resolving a name-server address.");
                result.Stopped = true;
                return result;
            }

            List<DnsIterativeAuthorityCandidate> attempted =
                new List<DnsIterativeAuthorityCandidate>();
            bool expanded = false;
            bool advanced = false;
            while (!advanced)
            {
                DnsIterativeAuthorityCandidate? candidate =
                    IterativeResolverOperations.FindUntriedCandidate(
                        state,
                        candidates,
                        attempted);
                if (candidate == null)
                {
                    if (!expanded)
                    {
                        DnsNameServerAddressResult dependencies =
                            ResolveMissingAddresses(
                                current,
                                state,
                                resolver,
                                activeNames);
                        if (dependencies.Stopped)
                        {
                            result.Stopped = true;
                            return result;
                        }

                        if (dependencies.Loop)
                        {
                            result.Loop = true;
                        }

                        expanded = true;
                        candidates = IterativeResolverOperations
                            .CreateAuthorityCandidates(state, current);
                        if (state.WorkLimitExceeded)
                        {
                            IterativeResolverOperations.ResourceLimitFailure(
                                state,
                                "work",
                                "The work budget was exhausted while resolving a name-server dependency.");
                            result.Stopped = true;
                            return result;
                        }

                        candidate = IterativeResolverOperations
                            .FindUntriedCandidate(
                                state,
                                candidates,
                                attempted);
                    }

                    if (candidate == null)
                    {
                        return result;
                    }
                }

                attempted.Add(candidate);
                DnsIterativeQueryAttempt attempt =
                    IterativeResolverOperations.QueryCandidate(
                        state,
                        questionPlan.Name,
                        questionPlan.RecordTypeValue,
                        1,
                        candidate,
                        resolver);
                if (attempt.Stopped)
                {
                    result.Stopped = true;
                    return result;
                }

                if (!attempt.Usable || attempt.Response == null)
                {
                    continue;
                }

                DnsAddressResult? addresses = null;
                if (!questionPlan.Minimized)
                {
                    addresses = ReadAddressAnswer(
                        attempt.Response,
                        nameServer,
                        recordType,
                        recordTypeValue,
                        addressFamily,
                        addressWidth,
                        state);
                }
                if (state.WorkLimitExceeded)
                {
                    IterativeResolverOperations.ResourceLimitFailure(
                        state,
                        "work",
                        "The work budget was exhausted while reading a name-server address.");
                    result.Stopped = true;
                    return result;
                }

                if (addresses != null)
                {
                    destination.GlueAddresses.Add(addresses);
                    ReferralGlueOperations.StoreResolvedAddresses(
                        addresses,
                        resolver.Cache);
                    result.Added = true;
                    return result;
                }

                DnsIterativeReferralResult referral =
                    IterativeResolverOperations.ReadReferral(
                        state,
                        attempt.Response,
                        questionPlan.Name,
                        current);
                if (state.WorkLimitExceeded)
                {
                    IterativeResolverOperations.ResourceLimitFailure(
                        state,
                        "work",
                        "The work budget was exhausted while reading a name-server referral.");
                    result.Stopped = true;
                    return result;
                }

                if (referral.Invalid)
                {
                    IterativeServerStateOperations.MarkProtocolFailure(
                        state,
                        candidate);
                    continue;
                }

                if (referral.Lame)
                {
                    IterativeServerStateOperations.MarkLame(
                        state,
                        candidate);
                    continue;
                }

                if (!referral.Found || referral.Delegation == null)
                {
                    if (questionPlan.Minimized &&
                        IterativeResolverOperations
                            .MinimizationNeedsFallback(attempt.Response))
                    {
                        if (state.Options.AllowMinimizationFallback)
                        {
                            IterativeResolverOperations
                                .EnableFullQuestionForZone(
                                    state,
                                    current.Zone);
                            advanced = true;
                            continue;
                        }

                        return result;
                    }

                    if (IterativeResolverOperations.ResponseAllowsAlternate(
                        attempt.Response))
                    {
                        IterativeServerStateOperations.MarkFailure(
                            state,
                            candidate);
                        continue;
                    }

                    return result;
                }

                if (state.ReferralCount >=
                        state.Options.MaximumReferralDepth ||
                    referral.Delegation.Depth > state.Options.MaximumDepth)
                {
                    IterativeResolverOperations.ResourceLimitFailure(
                        state,
                        "referrals",
                        "The name-server address referral budget was exhausted.");
                    result.Stopped = true;
                    return result;
                }

                DnsReferralGlueResult glue =
                    ReferralGlueOperations.AcceptGlue(
                        referral.Delegation,
                        attempt.Response,
                        resolver.Cache,
                        state);
                if (state.WorkLimitExceeded)
                {
                    IterativeResolverOperations.ResourceLimitFailure(
                        state,
                        "work",
                        "The work budget was exhausted while reading name-server glue.");
                    result.Stopped = true;
                    return result;
                }

                if (glue.Invalid)
                {
                    IterativeServerStateOperations.MarkProtocolFailure(
                        state,
                        candidate);
                    continue;
                }

                state.ReferralCount = state.ReferralCount + 1;
                state.Result.Delegations.Add(referral.Delegation);
                current = referral.Delegation;
                advanced = true;
            }
        }
    }

    static DnsAddressResult? ReadAddressAnswer(
        DnsMessage response,
        DnsName nameServer,
        DnsRecordType recordType,
        int recordTypeValue,
        int addressFamily,
        int addressWidth,
        DnsIterativeResolverState state)
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
            return null;
        }

        DnsAddressResult result = new DnsAddressResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = RecordSetOperations.CopyName(
            nameServer,
            false);
        result.Response = response;
        result.SecurityStatus = DnsSecurityStatus.Insecure;
        int index = 0;
        while (index < response.Answers.Count)
        {
            if (!IterativeResolverOperations.ConsumeWork(state, 1))
            {
                return null;
            }

            DnsResourceRecord record = response.Answers[index];
            if (record != null && record.Data != null &&
                record.Data.Address != null &&
                record.Data.Address.Address != null &&
                record.Data.Address.Address.Length == addressWidth &&
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) == recordTypeValue &&
                EffectiveValue(record.ClassValue, (int)record.Class) == 1 &&
                RecordSetOperations.NamesEqual(record.Name, nameServer))
            {
                AddDistinctAddress(
                    result,
                    record.Data.Address.Address,
                    addressFamily);
                MergeTimeToLive(result, record.TimeToLive);
            }

            index = index + 1;
        }

        if (result.Addresses.Count == 0)
        {
            return null;
        }

        return result;
    }

    static bool HasAddresses(DnsDelegation delegation, DnsName nameServer)
    {
        int index = 0;
        while (index < delegation.GlueAddresses.Count)
        {
            DnsAddressResult addresses = delegation.GlueAddresses[index];
            if (addresses != null && addresses.CanonicalName != null &&
                addresses.Addresses.Count > 0 &&
                addresses.Addresses.Count == addresses.AddressFamilies.Count &&
                RecordSetOperations.NamesEqual(
                    addresses.CanonicalName,
                    nameServer))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AddDistinctAddress(
        DnsAddressResult destination,
        byte[] address,
        int addressFamily)
    {
        int index = 0;
        while (index < destination.Addresses.Count)
        {
            if (destination.AddressFamilies[index] == addressFamily &&
                BytesEqual(destination.Addresses[index], address))
            {
                return;
            }

            index = index + 1;
        }

        destination.Addresses.Add(RecordSetOperations.CopyBytes(address));
        destination.AddressFamilies.Add(addressFamily);
    }

    static void MergeTimeToLive(
        DnsAddressResult destination,
        uint timeToLive)
    {
        if (!destination.HasTimeToLive ||
            timeToLive < destination.TimeToLive)
        {
            destination.HasTimeToLive = true;
            destination.TimeToLive = timeToLive;
        }
    }

    static List<DnsName> CopyNames(List<DnsName> source)
    {
        List<DnsName> result = new List<DnsName>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(RecordSetOperations.CopyName(source[index], false));
            index = index + 1;
        }

        return result;
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

    static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int EffectiveValue(int rawValue, int recognizedValue)
    {
        if (rawValue == 0 && recognizedValue != 0)
        {
            return recognizedValue;
        }

        return rawValue;
    }
}
