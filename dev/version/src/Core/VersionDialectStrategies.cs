namespace version;

internal static class VersionDialectStrategies
{
    internal static IVersionDialectStrategy Get(VersionDialect dialect)
    {
        if (dialect == VersionDialect.NodeSemVer)
        {
            return new NodeSemVerDialectStrategy();
        }

        if (dialect == VersionDialect.Cargo)
        {
            return new CargoDialectStrategy();
        }

        if (dialect == VersionDialect.NuGet)
        {
            return new NuGetDialectStrategy();
        }

        if (dialect == VersionDialect.Maven)
        {
            return new MavenDialectStrategy();
        }

        if (dialect == VersionDialect.Go)
        {
            return new GoDialectStrategy();
        }

        if (dialect == VersionDialect.RubyGems)
        {
            return new RubyGemsDialectStrategy();
        }

        if (dialect == VersionDialect.Python)
        {
            return new PythonDialectStrategy();
        }

        return new SemVerDialectStrategy();
    }

    internal static IVersionDialectStrategy GetForComparison(UniversalVersion left, UniversalVersion right)
    {
        if (left.GetDialect() == VersionDialect.NuGet || right.GetDialect() == VersionDialect.NuGet)
        {
            return new NuGetDialectStrategy();
        }

        if (left.GetDialect() == VersionDialect.Maven || right.GetDialect() == VersionDialect.Maven)
        {
            return new MavenDialectStrategy();
        }

        if (left.GetDialect() == VersionDialect.RubyGems || right.GetDialect() == VersionDialect.RubyGems)
        {
            return new RubyGemsDialectStrategy();
        }

        if (left.GetDialect() == VersionDialect.Python || right.GetDialect() == VersionDialect.Python)
        {
            return new PythonDialectStrategy();
        }

        return Get(left.GetDialect());
    }
}

internal class SemVerDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return SemVerParser.ParseStrict(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        if (mode == VersionComparisonMode.Identity)
        {
            return SemVerVersionComparer.CompareIdentity(left, right);
        }

        if (mode == VersionComparisonMode.NormalizedIdentity)
        {
            return SemVerVersionComparer.CompareNormalizedIdentity(left, right);
        }

        return SemVerVersionComparer.ComparePrecedence(left, right);
    }

    public string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, version.HasLeadingVPrefix(), true);
    }
}

internal class NodeSemVerDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return SemVerParser.ParseLoose(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return new SemVerDialectStrategy().Compare(left, right, mode);
    }

    public string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, version.HasLeadingVPrefix(), true);
    }
}

internal class CargoDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return CargoVersions.Parse(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return new SemVerDialectStrategy().Compare(left, right, mode);
    }

    public string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, version.HasLeadingVPrefix(), true);
    }
}

internal class NuGetDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return NuGetVersionParser.Parse(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return NuGetVersionComparer.Compare(left, right);
    }

    public string FormatNormalized(UniversalVersion version)
    {
        return NuGetVersionFormatter.Normalize(version);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        return NuGetVersionFormatter.Normalize(version);
    }
}

internal class MavenDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return MavenVersionParser.Parse(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return MavenVersionComparer.Compare(left.GetOriginalText(), right.GetOriginalText());
    }

    public string FormatNormalized(UniversalVersion version)
    {
        if (version.GetOriginalText().Length > 0)
        {
            return MavenVersionFormatter.FormatNormalized(version.GetOriginalText());
        }

        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        if (version.GetOriginalText().Length > 0)
        {
            return MavenVersionFormatter.FormatCanonical(version.GetOriginalText());
        }

        return VersionFormatter.FormatCore(version, version.HasLeadingVPrefix(), true);
    }
}

internal class GoDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return GoVersionParser.Parse(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return new SemVerDialectStrategy().Compare(left, right, mode);
    }

    public string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCore(version, true, false);
    }
}

internal class RubyGemsDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return RubyGemsVersionParser.ParseUniversal(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return RubyGemsVersionComparer.Compare(left.GetOriginalText(), right.GetOriginalText());
    }

    public string FormatNormalized(UniversalVersion version)
    {
        if (version.GetOriginalText().Length > 0)
        {
            return RubyGemsVersionFormatter.FormatNormalized(version.GetOriginalText());
        }

        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        if (version.GetOriginalText().Length > 0)
        {
            return RubyGemsVersionFormatter.FormatCanonical(version.GetOriginalText());
        }

        return VersionFormatter.FormatCore(version, version.HasLeadingVPrefix(), true);
    }
}

internal class PythonDialectStrategy : IVersionDialectStrategy
{
    public VersionParseResult Parse(string text)
    {
        return PythonVersionParser.Parse(text);
    }

    public int Compare(UniversalVersion left, UniversalVersion right, VersionComparisonMode mode)
    {
        return PythonVersionComparer.Compare(left.GetOriginalText(), right.GetOriginalText());
    }

    public string FormatNormalized(UniversalVersion version)
    {
        if (version.HasPythonVersion())
        {
            return PythonVersionFormatter.FormatNormalized(version.GetPythonVersion());
        }

        return VersionFormatter.FormatCore(version, false, true);
    }

    public string FormatCanonical(UniversalVersion version)
    {
        if (version.HasPythonVersion())
        {
            return PythonVersionFormatter.FormatCanonical(version.GetPythonVersion());
        }

        return VersionFormatter.FormatCore(version, version.HasLeadingVPrefix(), true);
    }
}
