using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Evaluates deterministic indexed queries against one repository snapshot.</summary>
public class RepositoryQuery : IRepositoryQuery
{
    internal RepositorySnapshot Snapshot;
    internal RepositorySnapshotIndex Index;

    /// <summary>Initializes a query service for one normalized repository snapshot.</summary>
    /// <param name="snapshot">The snapshot to index and query.</param>
    public RepositoryQuery(RepositorySnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException("snapshot");
        }

        Snapshot = snapshot;
        Index = RepositorySnapshotIndexer.BuildIndex(snapshot);
    }

    /// <summary>Evaluates one conjunctive query against the configured snapshot.</summary>
    /// <param name="request">The indexed keys and selectors to evaluate.</param><returns>The deterministic query result.</returns>
    public RepositoryQueryResult QueryRepository(RepositoryQueryRequest? request)
    {
        if (request == null)
        {
            return CreateQueryFailure(
                "repository.query.request_missing",
                "A repository query request is required.");
        }

        RepositoryQueryResult? invalid = ValidateQueryRequest(request);
        if (invalid != null)
        {
            return invalid;
        }

        if (request.ArtifactEdition.Length > 0 &&
            request.ArtifactEdition != Snapshot.Edition)
        {
            RepositoryQueryResult noEdition = new RepositoryQueryResult();
            noEdition.Success = true;
            return noEdition;
        }

        List<RepositoryIndexEntry> candidates = SelectSeedEntries(request);
        List<RepositoryIndexEntry> matches = new List<RepositoryIndexEntry>();
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            RepositoryIndexEntry entry = candidates[candidateIndex];
            if (MatchesQueryRequest(entry, request))
            {
                matches.Add(entry);
            }

            candidateIndex++;
        }

        List<RepositoryIndexEntry> selected = ApplyVersionSelection(matches, request);
        return CreateQuerySuccess(selected, request);
    }

    internal RepositoryQueryResult? ValidateQueryRequest(RepositoryQueryRequest request)
    {
        if (request.MaximumResultCount <= 0)
        {
            return CreateQueryFailure(
                "repository.query.result_limit_invalid",
                "MaximumResultCount must be positive.");
        }

        if (request.VersionSelector == null)
        {
            return null;
        }

        VersionSelector selector = request.VersionSelector;
        if (selector.Kind != VersionSelectionKind.Exact &&
            selector.Kind != VersionSelectionKind.Current &&
            selector.Kind != VersionSelectionKind.LatestCompatible &&
            selector.Kind != VersionSelectionKind.Range)
        {
            return CreateQueryFailure(
                "repository.query.version_policy_invalid",
                "The version-selection policy is not recognized.");
        }

        if (selector.Kind == VersionSelectionKind.Exact ||
            selector.Kind == VersionSelectionKind.LatestCompatible)
        {
            if (selector.BaselineIdentifier == null ||
                MessageIdentifierLexical.FormatMessageIdentifier(
                    selector.BaselineIdentifier) == null)
            {
                return CreateQueryFailure(
                    "repository.query.version_baseline_invalid",
                    "The selected version policy requires a valid baseline message identifier.");
            }
        }

        if (selector.Kind == VersionSelectionKind.Range)
        {
            if (!IsNumericVersion(selector.MinimumVersion) ||
                !IsNumericVersion(selector.MaximumVersion) ||
                CompareVersions(selector.MinimumVersion, selector.MaximumVersion) > 0)
            {
                return CreateQueryFailure(
                    "repository.query.version_range_invalid",
                    "A version range requires ordered numeric minimum and maximum versions.");
            }
        }

        return null;
    }

    internal List<RepositoryIndexEntry> SelectSeedEntries(RepositoryQueryRequest request)
    {
        if (request.Identifier.Length > 0)
        {
            return Index.FindIdentifier(request.Identifier);
        }

        if (request.MessageIdentifier.Length > 0)
        {
            return Index.FindMessageIdentifier(request.MessageIdentifier);
        }

        if (request.NamespaceUri.Length > 0)
        {
            return Index.FindNamespace(request.NamespaceUri);
        }

        if (request.Name.Length > 0)
        {
            return Index.FindName(request.Name);
        }

        if (request.MessageSetIdentifier.Length > 0)
        {
            return Index.FindMessageSet(request.MessageSetIdentifier);
        }

        if (request.BusinessAreaCode.Length > 0)
        {
            return Index.FindBusinessArea(request.BusinessAreaCode);
        }

        if (request.MessageFunctionality.Length > 0)
        {
            return Index.FindMessageFunctionality(request.MessageFunctionality);
        }

        if (request.Variant.Length > 0)
        {
            return Index.FindVariant(request.Variant);
        }

        if (request.HasKind)
        {
            return Index.FindKind(request.Kind);
        }

        if (request.HasRegistrationStatus)
        {
            return Index.FindStatus(request.RegistrationStatus);
        }

        return Index.Entries;
    }

    internal static bool MatchesQueryRequest(
        RepositoryIndexEntry entry,
        RepositoryQueryRequest request)
    {
        if (request.Identifier.Length > 0 && entry.Identifier != request.Identifier)
        {
            return false;
        }

        if (request.Name.Length > 0 && entry.Name != request.Name)
        {
            return false;
        }

        if (request.HasKind && entry.Kind != request.Kind)
        {
            return false;
        }

        if (request.HasRegistrationStatus &&
            entry.RegistrationStatus != request.RegistrationStatus)
        {
            return false;
        }

        if (request.BusinessAreaCode.Length > 0 &&
            entry.BusinessAreaCode != request.BusinessAreaCode)
        {
            return false;
        }

        if (request.MessageSetIdentifier.Length > 0 &&
            !ContainsExactText(
                entry.MessageSetIdentifiers,
                request.MessageSetIdentifier))
        {
            return false;
        }

        if (request.MessageIdentifier.Length > 0 &&
            entry.MessageIdentifier != request.MessageIdentifier)
        {
            return false;
        }

        if (request.NamespaceUri.Length > 0 &&
            entry.NamespaceUri != request.NamespaceUri &&
            !ContainsExactText(entry.NamespaceUris, request.NamespaceUri))
        {
            return false;
        }

        if (request.MessageFunctionality.Length > 0 &&
            entry.MessageFunctionality != request.MessageFunctionality)
        {
            return false;
        }

        if (request.Variant.Length > 0 && entry.Variant != request.Variant)
        {
            return false;
        }

        return MatchesVersionFilter(entry, request.VersionSelector);
    }

    internal static bool MatchesVersionFilter(
        RepositoryIndexEntry entry,
        VersionSelector? selector)
    {
        if (selector == null)
        {
            return true;
        }

        if (entry.Kind != RepositoryEntityKind.MessageDefinition)
        {
            return false;
        }

        if (selector.Kind == VersionSelectionKind.Exact)
        {
            MessageIdentifier? baseline = selector.BaselineIdentifier;
            if (baseline == null)
            {
                return false;
            }

            string? exact = MessageIdentifierLexical.FormatMessageIdentifier(baseline);
            return exact != null && entry.MessageIdentifier == exact;
        }

        if (selector.Kind == VersionSelectionKind.LatestCompatible)
        {
            MessageIdentifier? baseline = selector.BaselineIdentifier;
            return baseline != null &&
                entry.BusinessAreaCode == baseline.BusinessArea &&
                entry.MessageFunctionality == baseline.MessageFunctionality &&
                entry.Variant == baseline.Variant &&
                CompareVersions(entry.Version, baseline.Version) >= 0;
        }

        if (selector.Kind == VersionSelectionKind.Range)
        {
            return CompareVersions(entry.Version, selector.MinimumVersion) >= 0 &&
                CompareVersions(entry.Version, selector.MaximumVersion) <= 0;
        }

        return true;
    }

    internal static List<RepositoryIndexEntry> ApplyVersionSelection(
        List<RepositoryIndexEntry> matches,
        RepositoryQueryRequest request)
    {
        VersionSelector? selector = request.VersionSelector;
        if (selector == null || selector.Kind == VersionSelectionKind.Exact ||
            selector.Kind == VersionSelectionKind.Range)
        {
            return matches;
        }

        if (selector.Kind == VersionSelectionKind.LatestCompatible)
        {
            return SelectLatestCompatibleEntry(matches);
        }

        return SelectCurrentEntries(matches);
    }

    internal static List<RepositoryIndexEntry> SelectLatestCompatibleEntry(
        List<RepositoryIndexEntry> matches)
    {
        List<RepositoryIndexEntry> result = new List<RepositoryIndexEntry>();
        RepositoryIndexEntry? latest = null;
        int matchIndex = 0;
        while (matchIndex < matches.Count)
        {
            RepositoryIndexEntry candidate = matches[matchIndex];
            if (latest == null || CompareVersions(candidate.Version, latest.Version) > 0)
            {
                latest = candidate;
            }

            matchIndex++;
        }

        if (latest != null)
        {
            result.Add(latest);
        }

        return result;
    }

    internal static List<RepositoryIndexEntry> SelectCurrentEntries(
        List<RepositoryIndexEntry> matches)
    {
        Dictionary<string, RepositoryIndexEntry> latestByFamily =
            new Dictionary<string, RepositoryIndexEntry>(StringComparer.Ordinal);
        int matchIndex = 0;
        while (matchIndex < matches.Count)
        {
            RepositoryIndexEntry candidate = matches[matchIndex];
            string family = candidate.BusinessAreaCode + "." +
                candidate.MessageFunctionality + "." + candidate.Variant;
            if (!latestByFamily.ContainsKey(family) ||
                CompareVersions(
                    candidate.Version,
                    latestByFamily[family].Version) > 0)
            {
                latestByFamily[family] = candidate;
            }

            matchIndex++;
        }

        List<RepositoryIndexEntry> result = new List<RepositoryIndexEntry>();
        matchIndex = 0;
        while (matchIndex < matches.Count)
        {
            RepositoryIndexEntry candidate = matches[matchIndex];
            string family = candidate.BusinessAreaCode + "." +
                candidate.MessageFunctionality + "." + candidate.Variant;
            if (latestByFamily[family] == candidate)
            {
                result.Add(candidate);
            }

            matchIndex++;
        }

        return result;
    }

    internal static RepositoryQueryResult CreateQuerySuccess(
        List<RepositoryIndexEntry> matches,
        RepositoryQueryRequest request)
    {
        RepositoryQueryResult result = new RepositoryQueryResult();
        result.Success = true;
        result.TotalMatchCount = matches.Count;
        result.Truncated = matches.Count > request.MaximumResultCount;
        int matchIndex = 0;
        while (matchIndex < matches.Count && matchIndex < request.MaximumResultCount)
        {
            result.Items.Add(CreateQueryItem(matches[matchIndex], request.NamespaceUri));
            matchIndex++;
        }

        return result;
    }

    internal static RepositoryQueryItem CreateQueryItem(
        RepositoryIndexEntry entry,
        string requestedNamespace)
    {
        RepositoryQueryItem item = new RepositoryQueryItem();
        item.Identifier = entry.Identifier;
        item.Name = entry.Name;
        item.Kind = entry.Kind;
        item.RegistrationStatus = entry.RegistrationStatus;
        item.BusinessAreaCode = entry.BusinessAreaCode;
        item.MessageIdentifier = entry.MessageIdentifier;
        item.NamespaceUri = entry.NamespaceUri;
        if (item.NamespaceUri.Length == 0 && requestedNamespace.Length > 0)
        {
            item.NamespaceUri = requestedNamespace;
        }

        item.Version = entry.Version;
        item.Value = entry.Value;
        return item;
    }

    internal static bool ContainsExactText(List<string> values, string expected)
    {
        int valueIndex = 0;
        while (valueIndex < values.Count)
        {
            if (values[valueIndex] == expected)
            {
                return true;
            }

            valueIndex++;
        }

        return false;
    }

    internal static bool IsNumericVersion(string version)
    {
        if (version.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < version.Length)
        {
            if (version[index] < '0' || version[index] > '9')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static int CompareVersions(string left, string right)
    {
        int leftStart = FindSignificantVersionStart(left);
        int rightStart = FindSignificantVersionStart(right);
        int leftLength = left.Length - leftStart;
        int rightLength = right.Length - rightStart;
        if (leftLength < rightLength)
        {
            return -1;
        }

        if (leftLength > rightLength)
        {
            return 1;
        }

        return string.Compare(
            left,
            leftStart,
            right,
            rightStart,
            leftLength,
            StringComparison.Ordinal);
    }

    internal static int FindSignificantVersionStart(string version)
    {
        int index = 0;
        while (index < version.Length - 1 && version[index] == '0')
        {
            index++;
        }

        return index;
    }

    internal static RepositoryQueryResult CreateQueryFailure(string code, string message)
    {
        RepositoryQueryResult result = new RepositoryQueryResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
