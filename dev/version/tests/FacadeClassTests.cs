using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class FacadeClassTests
{
    [Test]
    public void SemVerFacade_ParsesCoercesAndComparesVersions()
    {
        VersionParseResult strictResult = SemVer.Parse("1.2.3-alpha+build.5");
        VersionParseResult looseResult = SemVer.ParseLoose("= v1.2.3");
        VersionParseResult coercedResult = SemVer.Coerce("release-2.3");

        Assert.That(strictResult.IsSuccess(), Is.True);
        Assert.That(strictResult.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(looseResult.IsSuccess(), Is.True);
        Assert.That(looseResult.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(coercedResult.IsSuccess(), Is.True);
        Assert.That(coercedResult.GetVersion().ToNormalizedString(), Is.EqualTo("2.3.0"));
        Assert.That(SemVer.ComparePrecedence("1.0.0-alpha", "1.0.0"), Is.LessThan(0));
        Assert.That(SemVer.CompareIdentity("1.0.0+1", "1.0.0+2"), Is.LessThan(0));
    }

    [Test]
    public void NodeSemVerFacade_ParsesRangesAndComparesLooseVersions()
    {
        VersionParseResult parseResult = NodeSemVer.Parse("= v1.2.3");
        VersionRangeParseResult rangeResult = NodeSemVer.ParseRange("^1.2.3");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(NodeSemVer.CompareLoose("= v1.2.3", "1.2.4"), Is.LessThan(0));
    }

    [Test]
    public void NuGetFacade_ParsesRangesFloatingVersionsAndComparesVersions()
    {
        VersionParseResult parseResult = NuGetVersions.Parse("1.2");
        VersionRangeParseResult rangeResult = NuGetVersions.ParseRange("[1.0,2.0)");
        NuGetFloatingVersion floatingVersion = NuGetVersions.ParseFloating("1.2.*");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.NuGet));
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.5", VersionDialect.NuGet), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.0", VersionDialect.NuGet), Is.False);
        Assert.That(floatingVersion.IsValid(), Is.True);
        Assert.That(floatingVersion.ContainsVersion(NuGetVersions.Parse("1.2.9").GetVersion()), Is.True);
        Assert.That(NuGetVersions.Compare("1.2", "1.2.1"), Is.LessThan(0));
    }

    [Test]
    public void MavenFacade_ParsesRangesAndComparesVersions()
    {
        VersionParseResult parseResult = MavenVersions.Parse("1.0-alpha");
        VersionRangeParseResult rangeResult = MavenVersions.ParseRange("[1.0,2.0)");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.Maven));
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.5", VersionDialect.Maven), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.0", VersionDialect.Maven), Is.False);
        Assert.That(MavenVersions.Compare("1.0-alpha", "1.0"), Is.LessThan(0));
    }

    [Test]
    public void GoFacade_ParsesAndComparesVersions()
    {
        VersionParseResult parseResult = GoVersions.Parse("v1.2.3");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.Go));
        Assert.That(GoVersions.Compare("v1.2.3", "v1.2.4"), Is.LessThan(0));
    }

    [Test]
    public void CargoFacade_ParsesRequirementsAndComparesVersions()
    {
        VersionParseResult parseResult = CargoVersions.Parse("1.2.3");
        VersionRangeParseResult rangeResult = CargoVersions.ParseRequirement("1.2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.Cargo));
        Assert.That(parseResult.GetVersion().GetDialect(), Is.EqualTo(VersionDialect.Cargo));
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
        Assert.That(CargoVersions.Compare("1.2.3", "1.2.4"), Is.LessThan(0));
    }

    [Test]
    public void RubyGemsFacade_ParsesRequirementsAndComparesVersions()
    {
        VersionParseResult parseResult = RubyGemsVersions.Parse("1.0.a");
        VersionRangeParseResult rangeResult = RubyGemsVersions.ParseRequirement("~> 2.2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.RubyGems));
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.9", VersionDialect.RubyGems), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("3.0", VersionDialect.RubyGems), Is.False);
        Assert.That(RubyGemsVersions.Compare("1.0.a", "1.0"), Is.LessThan(0));
    }

    [Test]
    public void PythonFacade_ParsesSpecifiersAndComparesVersions()
    {
        VersionParseResult parseResult = PythonVersions.Parse("1.0rc1");
        VersionRangeParseResult rangeResult = PythonVersions.ParseSpecifier("~=1.4");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetDialect(), Is.EqualTo(VersionDialect.Python));
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.9", VersionDialect.Python), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.0", VersionDialect.Python), Is.False);
        Assert.That(PythonVersions.Compare("1.0rc1", "1.0"), Is.LessThan(0));
    }
}
