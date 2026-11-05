namespace version;

internal interface IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text);
}
