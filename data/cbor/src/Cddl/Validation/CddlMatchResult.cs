namespace cbor;

internal class CddlMatchResult
{
    internal bool IsMatch;
    internal CddlValidationIssue? Issue;

    internal CddlMatchResult(bool isMatch, CddlValidationIssue? issue)
    {
        IsMatch = isMatch;
        Issue = issue;
    }
}
