using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeContainsTextTests
{
    [Test]
    public void ContainsText_DefaultsToStrictSemVer()
    {
        VersionRange range = CreateSemVerIntervalRange("1.0.0", "2.0.0");

        Assert.That(range.ContainsSemVerText("1.5.0"), Is.True);
        Assert.That(range.ContainsSemVerText("2.0.0"), Is.False);
        Assert.That(range.ContainsSemVerText("v1.5.0"), Is.False);
        Assert.That(range.ContainsSemVerText("bad"), Is.False);
    }

    [Test]
    public void ContainsText_UsesNodeSemVerLooseParsing()
    {
        VersionRange range = CreateSemVerIntervalRange("1.0.0", "2.0.0");

        Assert.That(range.ContainsText("= v1.5.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("= v2.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.5", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void ContainsText_UsesCargoAsStrictSemVer()
    {
        VersionRange range = CreateSemVerIntervalRange("1.0.0", "2.0.0");

        Assert.That(range.ContainsText("1.5.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("v1.5.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void ContainsText_UsesNuGetParsingAndComparison()
    {
        UniversalVersion target = ParseNuGet("1.2.0");
        VersionRange range = VersionRange.CreateSingleComparator(
            VersionComparator.CreateEqual(target, VersionComparisonMode.Precedence));

        Assert.That(range.ContainsText("1.2", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.1", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("bad", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void ContainsText_UsesMavenParsingAndComparison()
    {
        VersionRange range = new VersionRange(new VersionComparatorSet[]
        {
            new VersionComparatorSet(new VersionComparator[]
            {
                VersionComparator.CreateGreaterThanOrEqual(ParseMaven("1.0"), VersionComparisonMode.Precedence),
                VersionComparator.CreateLessThan(ParseMaven("2.0"), VersionComparisonMode.Precedence)
            })
        });

        Assert.That(range.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.5-snapshot", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("2.0", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("bad version", VersionDialect.Maven), Is.False);
    }

    [Test]
    public void ContainsText_UsesRubyGemsParsingAndComparison()
    {
        VersionRange range = VersionRange.CreateSingleComparator(
            VersionComparator.CreateGreaterThanOrEqual(ParseRubyGems("1.0"), VersionComparisonMode.Precedence));

        Assert.That(range.ContainsText("1.0", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.0.1", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.0.a", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("bad", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void ContainsText_UsesPythonParsingAndComparison()
    {
        VersionRange range = VersionRange.CreateSingleComparator(
            VersionComparator.CreateGreaterThanOrEqual(ParsePython("1.0"), VersionComparisonMode.Precedence));

        Assert.That(range.ContainsText("1.0", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.0.post1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.1a1", VersionDialect.Python), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.1a1", VersionDialect.Python, true), Is.True);
        Assert.That(range.ContainsText("bad", VersionDialect.Python), Is.False);
    }

    [Test]
    public void ContainsText_UsesGoVersionParsing()
    {
        UniversalVersion lower = ParseGo("v1.2.0");
        UniversalVersion upper = ParseGo("v2.0.0");
        VersionRange range = new VersionRange(new VersionComparatorSet[]
        {
            new VersionComparatorSet(new VersionComparator[]
            {
                VersionComparator.CreateGreaterThanOrEqual(lower, VersionComparisonMode.Precedence),
                VersionComparator.CreateLessThan(upper, VersionComparisonMode.Precedence)
            })
        });

        Assert.That(range.ContainsText("v1.2", VersionDialect.Go), Is.True);
        Assert.That(range.ContainsText("v1.9.9", VersionDialect.Go), Is.True);
        Assert.That(range.ContainsText("v2.0.0", VersionDialect.Go), Is.False);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.Go), Is.False);
    }

    [Test]
    public void ContainsText_HandlesEmptyAndUniversalRanges()
    {
        VersionRange empty = VersionRange.CreateEmpty();
        VersionRange universal = VersionRange.CreateUniversal();

        Assert.That(empty.ContainsSemVerText("1.0.0"), Is.False);
        Assert.That(universal.ContainsSemVerText("1.0.0"), Is.True);
        Assert.That(universal.ContainsSemVerText("bad"), Is.False);
    }

    [Test]
    public void ContainsText_ReturnsFalseForUnsupportedDialects()
    {
        VersionRange universal = VersionRange.CreateUniversal();

        Assert.That(universal.ContainsText("1.0", VersionDialect.Unknown), Is.False);
    }

    [Test]
    public void ContainsText_RejectsNullInput()
    {
        VersionRange universal = VersionRange.CreateUniversal();

        Assert.That(
            delegate
            {
                universal.ContainsSemVerText(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static VersionRange CreateSemVerIntervalRange(string lowerText, string upperText)
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseSemVer(lowerText), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseSemVer(upperText), VersionComparisonMode.Precedence)
        });

        return VersionRange.CreateSingleSet(set);
    }

    static UniversalVersion ParseSemVer(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static UniversalVersion ParseNuGet(string text)
    {
        VersionParseResult result = NuGetVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static UniversalVersion ParseMaven(string text)
    {
        VersionParseResult result = MavenVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static UniversalVersion ParseRubyGems(string text)
    {
        VersionParseResult result = RubyGemsVersionParser.ParseUniversal(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static UniversalVersion ParsePython(string text)
    {
        VersionParseResult result = PythonVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static UniversalVersion ParseGo(string text)
    {
        VersionParseResult result = GoVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
