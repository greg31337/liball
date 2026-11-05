namespace version;

internal static class RangeSyntaxStrategies
{
    internal static IRangeSyntaxStrategy Get(RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.Cargo)
        {
            return new CargoRangeSyntaxStrategy();
        }

        if (syntax == RangeSyntax.NuGet)
        {
            return new NuGetRangeSyntaxStrategy();
        }

        if (syntax == RangeSyntax.Maven)
        {
            return new MavenRangeSyntaxStrategy();
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            return new RubyGemsRangeSyntaxStrategy();
        }

        if (syntax == RangeSyntax.Python)
        {
            return new PythonRangeSyntaxStrategy();
        }

        return new NodeSemVerRangeSyntaxStrategy();
    }
}

internal class NodeSemVerRangeSyntaxStrategy : IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text)
    {
        return NodeRangeParser.Parse(text);
    }
}

internal class CargoRangeSyntaxStrategy : IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text)
    {
        return CargoRangeParser.Parse(text);
    }
}

internal class NuGetRangeSyntaxStrategy : IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text)
    {
        return NuGetRangeParser.Parse(text);
    }
}

internal class MavenRangeSyntaxStrategy : IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text)
    {
        return MavenRangeParser.Parse(text);
    }
}

internal class RubyGemsRangeSyntaxStrategy : IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text)
    {
        return RubyGemsRangeParser.Parse(text);
    }
}

internal class PythonRangeSyntaxStrategy : IRangeSyntaxStrategy
{
    public VersionRangeParseResult Parse(string text)
    {
        return PythonSpecifierParser.Parse(text);
    }
}
