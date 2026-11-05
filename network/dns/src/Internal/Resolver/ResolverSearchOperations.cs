using System.Collections.Generic;

namespace dns;

internal class DnsCandidateSequence
{
    internal List<DnsName> Names = new List<DnsName>();
    internal bool AbsoluteFirst;
}

internal static class ResolverSearchOperations
{
    internal static DnsCandidateSequence CreateCandidateSequence(
        DnsResolveRequest request)
    {
        if (request == null || request.Options == null ||
            request.Options.Configuration == null)
        {
            throw new DnsResolutionException(
                "A resolver request and configuration are required.");
        }

        RawDnsNameResult validatedName = RawDnsName.ValidateName(request.Name);
        if (!validatedName.Success || validatedName.Name == null)
        {
            throw new DnsResolutionException(
                "The resolver request name is invalid.");
        }

        DnsResolverConfiguration configuration =
            ResolverConfigurationOperations.ValidateConfiguration(
                request.Options.Configuration);
        DnsName inputName = validatedName.Name;
        DnsCandidateSequence result = new DnsCandidateSequence();
        DnsName absoluteName = CreateAbsoluteName(inputName);
        if (inputName.IsAbsolute)
        {
            result.AbsoluteFirst = true;
            AddDistinctCandidate(result.Names, absoluteName);
            return result;
        }

        int dotCount = inputName.Labels.Count - 1;
        if (dotCount < 0)
        {
            dotCount = 0;
        }

        result.AbsoluteFirst = dotCount >= configuration.NameDots;
        if (result.AbsoluteFirst)
        {
            AddDistinctCandidate(result.Names, absoluteName);
        }

        int searchIndex = 0;
        while (searchIndex < configuration.SearchDomains.Count)
        {
            DnsNameOperationResult combined = DnsNameOperations.ConcatenateNames(
                inputName,
                configuration.SearchDomains[searchIndex]);
            if (combined.Success && combined.Name != null)
            {
                AddDistinctCandidate(result.Names, combined.Name);
            }

            searchIndex = searchIndex + 1;
        }

        if (!result.AbsoluteFirst)
        {
            AddDistinctCandidate(result.Names, absoluteName);
        }

        return result;
    }

    internal static bool ShouldContinueCandidateSearch(DnsResolveStatus status)
    {
        return status == DnsResolveStatus.NoData ||
            status == DnsResolveStatus.NameError ||
            status == DnsResolveStatus.ServerFailure;
    }

    internal static DnsResolveStatus SelectTerminalStatus(
        DnsCandidateSequence sequence,
        List<DnsResolveStatus> statuses)
    {
        if (sequence == null || statuses == null || statuses.Count == 0)
        {
            throw new DnsResolutionException(
                "Candidate search outcomes are required.");
        }

        if (statuses.Count > sequence.Names.Count)
        {
            throw new DnsResolutionException(
                "Candidate search contains more outcomes than names.");
        }

        int index = 0;
        while (index < statuses.Count)
        {
            DnsResolveStatus status = statuses[index];
            if (!IsKnownStatus(status))
            {
                throw new DnsResolutionException(
                    "Candidate search contains an unknown outcome.");
            }

            if (!ShouldContinueCandidateSearch(status))
            {
                return status;
            }

            index = index + 1;
        }

        if (statuses.Count < sequence.Names.Count)
        {
            throw new DnsResolutionException(
                "Candidate search ended before a terminal outcome.");
        }

        if (sequence.AbsoluteFirst)
        {
            return statuses[0];
        }

        index = 0;
        while (index < statuses.Count)
        {
            if (statuses[index] == DnsResolveStatus.NoData)
            {
                return DnsResolveStatus.NoData;
            }

            index = index + 1;
        }

        index = 0;
        while (index < statuses.Count)
        {
            if (statuses[index] == DnsResolveStatus.ServerFailure)
            {
                return DnsResolveStatus.ServerFailure;
            }

            index = index + 1;
        }

        return statuses[statuses.Count - 1];
    }

    static DnsName CreateAbsoluteName(DnsName name)
    {
        DnsName result = RecordSetOperations.CopyName(name, false);
        result.IsAbsolute = true;
        return result;
    }

    static void AddDistinctCandidate(
        List<DnsName> candidates,
        DnsName candidate)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            DnsNameOperationResult equal = DnsNameOperations.NamesEqual(
                candidates[index],
                candidate);
            if (!equal.Success)
            {
                throw new DnsResolutionException(
                    "A resolver candidate name is invalid.");
            }

            if (equal.BooleanValue)
            {
                return;
            }

            index = index + 1;
        }

        candidates.Add(RecordSetOperations.CopyName(candidate, false));
    }

    static bool IsKnownStatus(DnsResolveStatus status)
    {
        int value = (int)status;
        return value >= (int)DnsResolveStatus.Success &&
            value <= (int)DnsResolveStatus.Unsupported;
    }
}
