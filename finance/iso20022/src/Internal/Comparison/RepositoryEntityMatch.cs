using System.Collections.Generic;

namespace iso20022;

internal class RepositoryEntityMatch
{
    internal RepositoryIndexEntry Source = new RepositoryIndexEntry();
    internal RepositoryIndexEntry? Target;
    internal decimal Confidence;
    internal bool ExactIdentifierMatch;
    internal bool SemanticFallbackMatch;
    internal bool OverrideMatch;
    internal bool Ambiguous;
    internal List<RepositoryIndexEntry> AlternativeTargets = new List<RepositoryIndexEntry>();
    internal string Rationale = string.Empty;
}
