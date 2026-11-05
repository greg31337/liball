using System.Collections.Generic;

namespace dns;

internal class DnsIterativeAnswerResult
{
    internal bool Terminal;
    internal bool Continue;
    internal bool Invalid;
    internal bool Lame;
    internal string Reason = string.Empty;
}

internal static class IterativeAnswerOperations
{
    internal static DnsIterativeAnswerResult ProcessAnswer(
        DnsIterativeResolverState state,
        DnsMessage response,
        DnsRecursiveResolver resolver)
    {
        DnsIterativeAnswerResult result = new DnsIterativeAnswerResult();
        int flags = EffectiveValue(
            response.Header.FlagsValue,
            (int)response.Header.Flags);
        if ((flags & (int)DnsHeaderFlag.AuthoritativeAnswer) == 0)
        {
            result.Lame = true;
            result.Reason =
                "An authority returned a terminal response without the authoritative-answer flag.";
            return result;
        }

        int responseCode = EffectiveValue(
            response.Header.ResponseCodeValue,
            (int)response.Header.ResponseCode);
        if (responseCode != (int)DnsResponseCode.NoError &&
            responseCode != (int)DnsResponseCode.NameError)
        {
            state.Result.Status =
                IterativeResolverOperations.MapExtendedErrorStatus(
                    response,
                    MapResponseStatus(responseCode));
            state.Result.Response = response;
            IterativeCacheOperations.StoreFailure(
                state,
                resolver.Cache);
            result.Terminal = true;
            return result;
        }

        DnsQuestion question = CreateQuestion(state);
        int remainingAliasDepth = state.Options.MaximumAliasDepth -
            state.Result.AliasSteps.Count;
        if (remainingAliasDepth < 0)
        {
            remainingAliasDepth = 0;
        }

        DnsResolveResult extracted = RecordSetOperations.ExtractAnswerChain(
            response,
            question,
            remainingAliasDepth);
        AppendDiagnostics(state.Result.Diagnostics, extracted.Diagnostics);
        if (!ApplyAliasSteps(state, extracted.AliasSteps, result))
        {
            return result;
        }

        if (extracted.Status == DnsResolveStatus.ProtocolError ||
            extracted.Status == DnsResolveStatus.DepthLimitExceeded ||
            extracted.Status == DnsResolveStatus.Loop)
        {
            state.Result.Status = extracted.Status;
            state.Result.Response = response;
            state.Result.CanonicalName = RecordSetOperations.CopyName(
                state.CurrentName,
                false);
            result.Terminal = true;
            return result;
        }

        state.Result.Response = response;
        state.Result.CanonicalName = RecordSetOperations.CopyName(
            state.CurrentName,
            false);
        if (extracted.Status == DnsResolveStatus.Success)
        {
            state.Result.Status = DnsResolveStatus.Success;
            state.Result.RecordSets = extracted.RecordSets;
            List<DnsResourceRecordSet> validatedAnswerSets =
                BuildValidatedAnswerSets(extracted);
            DnsSecurityResult security =
                IterativeDnssecOperations.ValidatePositiveAnswer(
                    state,
                    response,
                    validatedAnswerSets,
                    resolver);
            DnsSecurityStatus answerSecurityStatus = security.Status;
            AppendDiagnostics(
                state.Result.Diagnostics,
                security.Diagnostics);
            MergeTimeToLive(
                state.Result,
                extracted.HasTimeToLive,
                extracted.TimeToLive);
            if (state.Options.ValidateDnssec &&
                state.CurrentDelegationSecurityStatus !=
                    DnsSecurityStatus.Insecure &&
                answerSecurityStatus != DnsSecurityStatus.Secure)
            {
                state.Result.Status = SecurityFailureStatus(
                    answerSecurityStatus);
                state.Result.SecurityStatus = answerSecurityStatus;
                state.Result.Response =
                    DnssecChainValidator.ApplyAuthenticData(
                        response,
                        answerSecurityStatus,
                        false);
                IterativeCacheOperations.StoreFailure(
                    state,
                    resolver.Cache);
                result.Terminal = true;
                return result;
            }

            state.Result.SecurityStatus = MergeAliasSecurity(
                state,
                answerSecurityStatus);
            state.Result.Response = DnssecChainValidator.ApplyAuthenticData(
                response,
                state.Result.SecurityStatus,
                false);

            IterativeCacheOperations.StorePositive(
                state,
                state.Result.Response,
                resolver.Cache);
            result.Terminal = true;
            return result;
        }

        if (extracted.AliasSteps.Count > 0 &&
            responseCode == (int)DnsResponseCode.NoError)
        {
            DnsSecurityResult aliasSecurity =
                IterativeDnssecOperations.ValidatePositiveAnswer(
                    state,
                    response,
                    BuildValidatedAnswerSets(extracted),
                    resolver);
            AppendDiagnostics(
                state.Result.Diagnostics,
                aliasSecurity.Diagnostics);
            if (state.Options.ValidateDnssec &&
                state.CurrentDelegationSecurityStatus !=
                    DnsSecurityStatus.Insecure &&
                aliasSecurity.Status != DnsSecurityStatus.Secure)
            {
                state.Result.Status = SecurityFailureStatus(
                    aliasSecurity.Status);
                state.Result.SecurityStatus = aliasSecurity.Status;
                state.Result.Response =
                    DnssecChainValidator.ApplyAuthenticData(
                        response,
                        aliasSecurity.Status,
                        false);
                IterativeCacheOperations.StoreFailure(
                    state,
                    resolver.Cache);
                result.Terminal = true;
                return result;
            }

            MergeIntermediateAliasSecurity(state, aliasSecurity.Status);
            DnsDelegation selected =
                DelegationSelectionOperations.SelectClosestDelegation(
                    state.CurrentName,
                    state.RootHints,
                    resolver.Cache);
            state.CurrentDelegation = selected;
            state.CurrentDelegationSecurityStatus = selected.Secure ?
                DnsSecurityStatus.Secure : DnsSecurityStatus.Insecure;
            AddDelegationIfChanged(state.Result.Delegations, selected);
            result.Continue = true;
            return result;
        }

        DnsResourceRecord? soa = FindNegativeSoa(
            response,
            state.CurrentName,
            state.CurrentDelegation.Zone,
            EffectiveValue(
                state.Request.ClassValue,
                (int)state.Request.Class));
        if (soa == null || soa.Data == null || soa.Data.Soa == null)
        {
            result.Invalid = true;
            result.Reason =
                "An authoritative negative response requires an in-zone SOA record.";
            return result;
        }

        state.Result.Status = responseCode ==
            (int)DnsResponseCode.NameError ?
            DnsResolveStatus.NameError : DnsResolveStatus.NoData;
        uint negativeTimeToLive = soa.TimeToLive;
        if (soa.Data.Soa.Minimum < negativeTimeToLive)
        {
            negativeTimeToLive = soa.Data.Soa.Minimum;
        }

        MergeTimeToLive(state.Result, true, negativeTimeToLive);
        DnsSecurityResult negativeSecurity =
            IterativeDnssecOperations.ValidateNegativeAnswer(
                state,
                response,
                state.Result.Status == DnsResolveStatus.NameError,
                resolver);
        DnsSecurityStatus denialSecurityStatus = negativeSecurity.Status;
        AppendDiagnostics(
            state.Result.Diagnostics,
            negativeSecurity.Diagnostics);
        if (state.Options.ValidateDnssec &&
            state.CurrentDelegationSecurityStatus !=
                DnsSecurityStatus.Insecure &&
            denialSecurityStatus != DnsSecurityStatus.Secure &&
            denialSecurityStatus != DnsSecurityStatus.Insecure)
        {
            state.Result.Status = SecurityFailureStatus(
                denialSecurityStatus);
            state.Result.SecurityStatus = denialSecurityStatus;
            state.Result.Response =
                DnssecChainValidator.ApplyAuthenticData(
                    response,
                    denialSecurityStatus,
                    false);
            IterativeCacheOperations.StoreFailure(
                state,
                resolver.Cache);
            result.Terminal = true;
            return result;
        }

        state.Result.SecurityStatus = MergeAliasSecurity(
            state,
            denialSecurityStatus);
        state.Result.Response = DnssecChainValidator.ApplyAuthenticData(
            response,
            state.Result.SecurityStatus,
            false);

        IterativeCacheOperations.StoreNegative(
            state,
            state.Result.Response,
            soa,
            resolver.Cache);
        if (negativeSecurity.Status == DnsSecurityStatus.Secure &&
            negativeSecurity.ProofRecords.Count > 0)
        {
            IterativeDnssecOperations.StoreAggressiveDenial(
                state,
                negativeSecurity.ProofRecords,
                negativeTimeToLive,
                resolver);
        }
        result.Terminal = true;
        return result;
    }

    static DnsQuestion CreateQuestion(DnsIterativeResolverState state)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = RecordSetOperations.CopyName(
            state.CurrentName,
            false);
        result.RecordType = state.Request.RecordType;
        result.RecordTypeValue = EffectiveValue(
            state.Request.RecordTypeValue,
            (int)state.Request.RecordType);
        result.Class = state.Request.Class;
        result.ClassValue = EffectiveValue(
            state.Request.ClassValue,
            (int)state.Request.Class);
        return result;
    }

    static bool ApplyAliasSteps(
        DnsIterativeResolverState state,
        List<DnsAliasStep> steps,
        DnsIterativeAnswerResult result)
    {
        int index = 0;
        while (index < steps.Count)
        {
            DnsAliasStep step = steps[index];
            if (!RecordSetOperations.NamesEqual(
                    step.Owner,
                    state.CurrentName))
            {
                result.Invalid = true;
                result.Reason =
                    "An iterative alias chain has a discontinuous owner.";
                return false;
            }

            if (state.Result.AliasSteps.Count >=
                state.Options.MaximumAliasDepth)
            {
                state.Result.Status = DnsResolveStatus.DepthLimitExceeded;
                IterativeResolverOperations.AddDiagnostic(
                    state.Result,
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "aliases",
                    "The iterative alias depth budget was exhausted.");
                result.Terminal = true;
                return false;
            }

            state.Result.AliasSteps.Add(step);
            if (step.Record != null)
            {
                MergeTimeToLive(
                    state.Result,
                    true,
                    step.Record.TimeToLive);
            }

            state.CurrentName = RecordSetOperations.CopyName(
                step.Target,
                false);
            if (ContainsName(state.VisitedNames, state.CurrentName))
            {
                state.Result.Status = DnsResolveStatus.Loop;
                state.Result.CanonicalName = RecordSetOperations.CopyName(
                    state.CurrentName,
                    false);
                IterativeResolverOperations.AddDiagnostic(
                    state.Result,
                    DnsDiagnosticCode.ResponseMismatch,
                    "aliases",
                    "The iterative alias chain contains a loop.");
                result.Terminal = true;
                return false;
            }

            state.VisitedNames.Add(RecordSetOperations.CopyName(
                state.CurrentName,
                true));
            index = index + 1;
        }

        return true;
    }

    static DnsResourceRecord? FindNegativeSoa(
        DnsMessage response,
        DnsName queryName,
        DnsName currentZone,
        int classValue)
    {
        DnsResourceRecord? result = null;
        int index = 0;
        while (index < response.Authorities.Count)
        {
            DnsResourceRecord record = response.Authorities[index];
            if (record != null && record.Data != null &&
                record.Data.Soa != null &&
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) == 6 &&
                EffectiveValue(record.ClassValue, (int)record.Class) ==
                    classValue &&
                NegativeOwnerIsValid(
                    record.Name,
                    queryName,
                    currentZone))
            {
                if (result != null &&
                    !RecordSetOperations.NamesEqual(
                        result.Name,
                        record.Name))
                {
                    return null;
                }

                result = record;
            }

            index = index + 1;
        }

        return result;
    }

    static bool NegativeOwnerIsValid(
        DnsName owner,
        DnsName queryName,
        DnsName currentZone)
    {
        RawDnsNameResult validated = RawDnsName.ValidateName(owner);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            return false;
        }

        DnsNameOperationResult querySuffix = DnsNameOperations.HasNameSuffix(
            queryName,
            validated.Name);
        DnsNameOperationResult zoneSuffix = DnsNameOperations.HasNameSuffix(
            validated.Name,
            currentZone);
        return querySuffix.Success && querySuffix.BooleanValue &&
            zoneSuffix.Success && zoneSuffix.BooleanValue;
    }

    static void AddDelegationIfChanged(
        List<DnsDelegation> delegations,
        DnsDelegation candidate)
    {
        if (delegations.Count == 0 ||
            !RecordSetOperations.NamesEqual(
                delegations[delegations.Count - 1].Zone,
                candidate.Zone))
        {
            delegations.Add(candidate);
        }
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

    static void MergeTimeToLive(
        DnsResolveResult result,
        bool hasTimeToLive,
        uint timeToLive)
    {
        if (hasTimeToLive &&
            (!result.HasTimeToLive || timeToLive < result.TimeToLive))
        {
            result.HasTimeToLive = true;
            result.TimeToLive = timeToLive;
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

    static DnsResolveStatus MapResponseStatus(int responseCode)
    {
        if (responseCode == (int)DnsResponseCode.Refused)
        {
            return DnsResolveStatus.Refused;
        }

        if (responseCode == (int)DnsResponseCode.NotImplemented)
        {
            return DnsResolveStatus.Unsupported;
        }

        if (responseCode == (int)DnsResponseCode.FormatError)
        {
            return DnsResolveStatus.ProtocolError;
        }

        return DnsResolveStatus.ServerFailure;
    }

    static DnsResolveStatus SecurityFailureStatus(
        DnsSecurityStatus status)
    {
        if (status == DnsSecurityStatus.UnsupportedAlgorithm)
        {
            return DnsResolveStatus.Unsupported;
        }

        return DnsResolveStatus.ServerFailure;
    }

    static List<DnsResourceRecordSet> BuildValidatedAnswerSets(
        DnsResolveResult extracted)
    {
        List<DnsResourceRecord> aliases =
            new List<DnsResourceRecord>();
        int index = 0;
        while (index < extracted.AliasSteps.Count)
        {
            DnsAliasStep step = extracted.AliasSteps[index];
            if (step.Record != null)
            {
                aliases.Add(step.Record);
            }

            index = index + 1;
        }

        List<DnsResourceRecordSet> result =
            RecordSetOperations.GroupRecordSets(aliases);
        index = 0;
        while (index < extracted.RecordSets.Count)
        {
            result.Add(extracted.RecordSets[index]);
            index = index + 1;
        }

        return result;
    }

    static void MergeIntermediateAliasSecurity(
        DnsIterativeResolverState state,
        DnsSecurityStatus status)
    {
        if (!state.HasAliasSecurityStatus)
        {
            state.HasAliasSecurityStatus = true;
            state.AliasSecurityStatus = status;
            return;
        }

        if (state.AliasSecurityStatus == DnsSecurityStatus.Secure)
        {
            state.AliasSecurityStatus = status;
        }
    }

    static DnsSecurityStatus MergeAliasSecurity(
        DnsIterativeResolverState state,
        DnsSecurityStatus status)
    {
        if (!state.HasAliasSecurityStatus ||
            state.AliasSecurityStatus == DnsSecurityStatus.Secure)
        {
            return status;
        }

        return state.AliasSecurityStatus;
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
