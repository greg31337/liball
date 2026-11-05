using System.Collections.Generic;

namespace iso20022;

internal class RepositoryMatchResult
{
    internal bool Success;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
    internal List<RepositoryEntityMatch> Matches = new List<RepositoryEntityMatch>();
    internal List<RepositoryIndexEntry> UnmatchedSources = new List<RepositoryIndexEntry>();
    internal List<RepositoryIndexEntry> UnmatchedTargets = new List<RepositoryIndexEntry>();
}
