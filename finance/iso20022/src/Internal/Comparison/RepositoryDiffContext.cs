using System.Collections.Generic;

namespace iso20022;

internal class RepositoryDiffContext
{
    internal RepositoryComparisonRequest Request = new RepositoryComparisonRequest();
    internal RepositoryMatchResult MatchResult = new RepositoryMatchResult();
    internal RepositoryDiff Difference = new RepositoryDiff();
    internal RepositorySnapshotIndex SourceIndex = new RepositorySnapshotIndex();
    internal RepositorySnapshotIndex TargetIndex = new RepositorySnapshotIndex();
    internal Dictionary<string, string> SourceToTargetIdentifiers =
        new Dictionary<string, string>(System.StringComparer.Ordinal);
    internal Dictionary<string, RepositoryEntityMatch> MatchesBySourceIdentifier =
        new Dictionary<string, RepositoryEntityMatch>(System.StringComparer.Ordinal);
    internal HashSet<RepositoryIndexEntry> AmbiguousTargets = new HashSet<RepositoryIndexEntry>();
    internal bool ScopeEnabled;
    internal HashSet<string> SourceScopeIdentifiers =
        new HashSet<string>(System.StringComparer.Ordinal);
    internal HashSet<string> TargetScopeIdentifiers =
        new HashSet<string>(System.StringComparer.Ordinal);
    internal int MaterialDifferenceCount;
    internal int MaximumDifferenceCount;
}
