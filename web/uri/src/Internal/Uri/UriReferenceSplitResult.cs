namespace uri;

internal class UriReferenceSplitResult
{
    internal bool Success;
    internal bool HasAuthority;
    internal int AuthorityOffset;
    internal int AuthorityLength;
    internal int PathOffset;
    internal int PathLength;
    internal bool HasQuery;
    internal int QueryOffset;
    internal int QueryLength;
    internal bool HasFragment;
    internal int FragmentOffset;
    internal int FragmentLength;
    internal UriDiagnostic? Diagnostic;

    internal UriReferenceSplitResult(
        bool success,
        bool hasAuthority,
        int authorityOffset,
        int authorityLength,
        int pathOffset,
        int pathLength,
        bool hasQuery,
        int queryOffset,
        int queryLength,
        bool hasFragment,
        int fragmentOffset,
        int fragmentLength,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        HasAuthority = hasAuthority;
        AuthorityOffset = authorityOffset;
        AuthorityLength = authorityLength;
        PathOffset = pathOffset;
        PathLength = pathLength;
        HasQuery = hasQuery;
        QueryOffset = queryOffset;
        QueryLength = queryLength;
        HasFragment = hasFragment;
        FragmentOffset = fragmentOffset;
        FragmentLength = fragmentLength;
        Diagnostic = diagnostic;
    }
}
