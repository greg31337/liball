using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicFacadeSmokeTests
{
    [Test]
    public void SemVerFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = SemVer.Parse("1.2.3");
        VersionParseResult strictResult = SemVer.ParseStrict("1.2.3-alpha.1");
        VersionParseResult looseResult = SemVer.ParseLoose("v1.2.3");
        VersionParseResult coercedResult = SemVer.Coerce("release 1.2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(strictResult.IsSuccess(), Is.True);
        Assert.That(looseResult.IsSuccess(), Is.True);
        Assert.That(coercedResult.IsSuccess(), Is.True);
        Assert.That(SemVer.ComparePrecedence("1.2.3-alpha.1", "1.2.3"), Is.LessThan(0));
        Assert.That(SemVer.CompareIdentity("1.2.3+build.1", "1.2.3+build.2"), Is.LessThan(0));
        Assert.That(SemVer.CompareNormalizedIdentity("1.2.3+build.1", "1.2.3+build.2"), Is.EqualTo(0));
    }

    [Test]
    public void NodeSemVerFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = NodeSemVer.Parse("= v1.2.3");
        VersionParseResult coercedResult = NodeSemVer.Coerce("release 1.2");
        VersionRangeParseResult rangeResult = NodeSemVer.ParseRange("~1.2.3");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(coercedResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.2.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(NodeSemVer.CompareLoose("= v1.2.3", "v1.2.4"), Is.LessThan(0));
    }

    [Test]
    public void NuGetVersionsFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = NuGetVersions.Parse("1.2.3");
        VersionRangeParseResult rangeResult = NuGetVersions.ParseRange("(1.0,2.0]");
        NuGetFloatingVersion floatingVersion = NuGetVersions.ParseFloating("1.*");
        string selectedText = floatingVersion.SelectHighestMatchingText(new string[] { "1.0.0", "1.2.0", "2.0.0" });

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.5.0", VersionDialect.NuGet), Is.True);
        Assert.That(floatingVersion.IsValid(), Is.True);
        Assert.That(selectedText, Is.EqualTo("1.2.0"));
        Assert.That(NuGetVersions.Compare("1.2.3", "1.2.4"), Is.LessThan(0));
    }

    [Test]
    public void MavenVersionsFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = MavenVersions.Parse("1.2.3");
        VersionRangeParseResult rangeResult = MavenVersions.ParseRange("[1.0,2.0)");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.5", VersionDialect.Maven), Is.True);
        Assert.That(MavenVersions.Compare("1.0-rc1", "1.0"), Is.LessThan(0));
    }

    [Test]
    public void GoVersionsFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = GoVersions.Parse("v1.2.3");
        GoModulePathParseResult pathResult = GoVersions.ParseModulePath("example.com/project/v2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
        Assert.That(pathResult.IsSuccess(), Is.True);
        Assert.That(pathResult.GetModulePath().GetMajorVersionSuffix(), Is.EqualTo("v2"));
        Assert.That(GoVersions.Compare("v1.2.3", "v1.3.0"), Is.LessThan(0));
        Assert.That(GoVersions.IsValidModulePath("example.com/project/v2"), Is.True);
        Assert.That(GoVersions.IsValidSemanticImportVersion("example.com/project/v2", "v2.0.0"), Is.True);
        Assert.That(GoVersions.CanonicalizeModuleVersion("example.com/legacy", "v2.0.0+incompatible"), Is.EqualTo("v2.0.0+incompatible"));
    }

    [Test]
    public void CargoVersionsFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = CargoVersions.Parse("1.2.3");
        VersionRangeParseResult requirementResult = CargoVersions.ParseRequirement("^1.2.3");
        VersionRangeParseResult rangeResult = CargoVersions.ParseRange("~1.2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(parseResult.GetVersion().GetDialect(), Is.EqualTo(VersionDialect.Cargo));
        Assert.That(requirementResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(requirementResult.GetRange().ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("1.2.9", VersionDialect.Cargo), Is.True);
        Assert.That(CargoVersions.Compare("1.2.3", "1.2.4"), Is.LessThan(0));
    }

    [Test]
    public void RubyGemsVersionsFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = RubyGemsVersions.Parse("2.2.1");
        VersionRangeParseResult requirementResult = RubyGemsVersions.ParseRequirement(">= 2.2");
        VersionRangeParseResult rangeResult = RubyGemsVersions.ParseRange("~> 2.2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(requirementResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(requirementResult.GetRange().ContainsText("2.3", VersionDialect.RubyGems), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("3.0", VersionDialect.RubyGems), Is.False);
        Assert.That(RubyGemsVersions.Compare("2.2.1", "2.2.2"), Is.LessThan(0));
    }

    [Test]
    public void PythonVersionsFacade_PublicApiSmoke()
    {
        VersionParseResult parseResult = PythonVersions.Parse("1.4.5");
        VersionRangeParseResult specifierResult = PythonVersions.ParseSpecifier("~=1.4");
        VersionRangeParseResult rangeResult = PythonVersions.ParseRange(">=1.4,<2");

        Assert.That(parseResult.IsSuccess(), Is.True);
        Assert.That(specifierResult.IsSuccess(), Is.True);
        Assert.That(rangeResult.IsSuccess(), Is.True);
        Assert.That(specifierResult.GetRange().ContainsText("1.9", VersionDialect.Python), Is.True);
        Assert.That(rangeResult.GetRange().ContainsText("2.0", VersionDialect.Python), Is.False);
        Assert.That(PythonVersions.Compare("1.4.5", "1.5"), Is.LessThan(0));
    }
}
