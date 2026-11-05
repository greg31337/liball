namespace fix;

internal class ScoreParseResult
{
    internal bool Success;
    internal ScoreSyntaxNode? Root;
    internal int ErrorOffset = -1;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
