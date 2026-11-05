using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionOptionApiOverloadTests
{
    [Test]
    public void CoreParseApis_AcceptVersionParseOptions()
    {
        VersionParseOptions strictOptions = VersionParseOptions.CreateStrict(VersionDialect.SemVer);
        VersionParseOptions looseOptions = VersionParseOptions.CreateLoose(VersionDialect.NodeSemVer);
        VersionParseOptions coerceOptions = VersionParseOptions.CreateCoerce(VersionDialect.SemVer);
        VersionParseOptions shortOptions = VersionParseOptions.CreateWithMaxInputLength(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            4);

        Assert.That(VersionParseResult.TryParse("= v1.2.3", strictOptions).IsFailure(), Is.True);
        Assert.That(UniversalVersion.TryParse("= v1.2.3", looseOptions).IsSuccess(), Is.True);
        Assert.That(UniversalVersion.Parse("release 1.2.3 now", coerceOptions).ToNormalizedString(), Is.EqualTo("1.2.3"));
        Assert.That(UniversalVersion.TryParse("1.2.3", shortOptions).IsFailure(), Is.True);
    }

    [Test]
    public void CoreRangeApis_AcceptVersionRangeParseOptionsAndVersionParseOptions()
    {
        VersionRangeParseOptions rangeOptions = VersionRangeParseOptions.CreateWithPrereleasePolicy(
            RangeSyntax.NodeSemVer,
            PrereleasePolicy.Include);
        VersionParseOptions versionOptions = VersionParseOptions.CreateWithPrereleasePolicy(
            VersionDialect.NodeSemVer,
            VersionParseMode.Loose,
            PrereleasePolicy.Include);

        VersionRangeParseResult result = VersionRangeParseResult.TryParse(">=1.0.0", rangeOptions);
        VersionRange range = VersionRange.Parse(">=1.0.0", rangeOptions);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRange().AllowsPrereleasesByDefault(), Is.True);
        Assert.That(range.ContainsTextWithOptions("1.1.0-alpha", versionOptions), Is.True);
    }

    [Test]
    public void CoreComparisonApis_AcceptVersionComparisonOptions()
    {
        VersionComparisonOptions precedenceOptions = VersionComparisonOptions.CreatePrecedence(VersionDialect.SemVer);
        VersionComparisonOptions buildOptions = VersionComparisonOptions.CreateIncludingBuildMetadata(
            VersionDialect.SemVer,
            VersionComparisonMode.Precedence);
        VersionComparisonOptions invalidOptions = VersionComparisonOptions.CreateWithInvalidVersionOrdering(
            VersionDialect.SemVer,
            VersionComparisonMode.Precedence,
            InvalidVersionOrdering.SortAfterValid);

        UniversalVersion left = UniversalVersion.Parse("1.0.0+1", VersionParseOptions.CreateStrict(VersionDialect.SemVer));
        UniversalVersion right = UniversalVersion.Parse("1.0.0+2", VersionParseOptions.CreateStrict(VersionDialect.SemVer));

        Assert.That(left.CompareToVersionWithOptions(right, precedenceOptions), Is.EqualTo(0));
        Assert.That(left.CompareToVersionWithOptions(right, buildOptions), Is.LessThan(0));
        Assert.That(UniversalVersion.Compare("bad", "1.0.0", invalidOptions), Is.GreaterThan(0));
    }

    [Test]
    public void FacadeApis_AcceptOptionOverloadsWithoutChangingExistingDefaults()
    {
        VersionParseOptions defaultParseOptions = VersionParseOptions.CreateDefault();
        VersionParseOptions looseNodeOptions = VersionParseOptions.CreateLoose(VersionDialect.Unknown);
        VersionRangeParseOptions defaultRangeOptions = VersionRangeParseOptions.CreateDefault();
        VersionComparisonOptions defaultComparisonOptions = VersionComparisonOptions.CreateDefault();

        Assert.That(NodeSemVer.Parse("= v1.2.3", looseNodeOptions).IsSuccess(), Is.True);
        Assert.That(NuGetVersions.Parse("1.2", defaultParseOptions).GetDialect(), Is.EqualTo(VersionDialect.NuGet));
        Assert.That(PythonVersions.ParseRange(">=1.0", defaultRangeOptions).IsSuccess(), Is.True);
        Assert.That(MavenVersions.Compare("1.0-alpha", "1.0", defaultComparisonOptions), Is.LessThan(0));
    }

    [Test]
    public void FacadeOptionOverloads_DoNotMutateCallerOptions()
    {
        VersionParseOptions parseOptions = VersionParseOptions.CreateLoose(VersionDialect.SemVer);
        VersionComparisonOptions comparisonOptions = VersionComparisonOptions.CreateIncludingBuildMetadata(
            VersionDialect.SemVer,
            VersionComparisonMode.Dialect);

        SemVer.ParseStrict("1.2.3", parseOptions);
        SemVer.ComparePrecedence("1.0.0+1", "1.0.0+2", comparisonOptions);

        Assert.That(parseOptions.GetParseMode(), Is.EqualTo(VersionParseMode.Loose));
        Assert.That(comparisonOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Dialect));
        Assert.That(comparisonOptions.IncludesBuildMetadata(), Is.True);
    }
}
