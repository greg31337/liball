namespace uri;

internal class AuthorityValueParseResult
{
    internal bool Success;
    internal UriAuthority Authority;
    internal UriDiagnostic? Diagnostic;

    internal AuthorityValueParseResult(
        bool success,
        UriAuthority authority,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Authority = authority;
        Diagnostic = diagnostic;
    }
}
