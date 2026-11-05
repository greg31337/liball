using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class OptionCompatibilityTests
{
    [Test]
    public void DefaultOptions_WorkThroughCoreAndFacadeOverloads()
    {
        VersionParseOptions parseOptions = VersionParseOptions.CreateDefault();
        VersionRangeParseOptions rangeOptions = VersionRangeParseOptions.CreateDefault();
        VersionComparisonOptions comparisonOptions = VersionComparisonOptions.CreateDefault();

        Assert.That(UniversalVersion.TryParse("1.2.3", parseOptions).IsSuccess(), Is.True);
        Assert.That(SemVer.Parse("1.2.3", parseOptions).GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(NodeSemVer.Parse("= v1.2.3", parseOptions).IsSuccess(), Is.True);
        Assert.That(NuGetVersions.Parse("1.2", parseOptions).GetDialect(), Is.EqualTo(VersionDialect.NuGet));
        Assert.That(VersionRange.TryParse(">=1.0.0", rangeOptions).IsSuccess(), Is.True);
        Assert.That(MavenVersions.Compare("1.0-alpha", "1.0", comparisonOptions), Is.LessThan(0));
    }

    [Test]
    public void InvalidEnumValues_AreRejectedAcrossOptionTypes()
    {
        Assert.That(
            delegate
            {
                VersionParseOptions.CreateForDialect((VersionDialect)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionRangeParseOptions.CreateForSyntax((RangeSyntax)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionComparisonOptions.CreateWithMode(VersionDialect.SemVer, (VersionComparisonMode)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionComparisonOptions.CreateWithInvalidVersionOrdering(
                    VersionDialect.SemVer,
                    VersionComparisonMode.Precedence,
                    (InvalidVersionOrdering)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void MaxInputLengthGuards_ApplyThroughPublicOptionOverloads()
    {
        VersionParseOptions versionOptions = VersionParseOptions.CreateWithMaxInputLength(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            4);
        VersionRangeParseOptions rangeOptions = VersionRangeParseOptions.CreateWithMaxInputLength(
            RangeSyntax.NodeSemVer,
            4);

        VersionParseResult versionResult = VersionParseResult.TryParse("1.2.3", versionOptions);
        VersionRangeParseResult rangeResult = VersionRangeParseResult.TryParse(">=1.0.0", rangeOptions);

        Assert.That(versionResult.IsFailure(), Is.True);
        Assert.That(versionResult.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.InputTooLong));
        Assert.That(rangeResult.IsFailure(), Is.True);
        Assert.That(rangeResult.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.InputTooLong));
    }

    [Test]
    public void StrictAndLooseParseOptions_SelectCompatibleSemVerBehavior()
    {
        VersionParseOptions strictOptions = VersionParseOptions.CreateStrict(VersionDialect.SemVer);
        VersionParseOptions looseOptions = VersionParseOptions.CreateLoose(VersionDialect.NodeSemVer);

        VersionParseResult strictResult = VersionParseResult.TryParse("= v1.2.3", strictOptions);
        VersionParseResult looseResult = VersionParseResult.TryParse("= v1.2.3", looseOptions);
        VersionParseResult facadeStrictResult = SemVer.ParseStrict("= v1.2.3", looseOptions);
        VersionParseResult facadeLooseResult = SemVer.ParseLoose("= v1.2.3", strictOptions);

        Assert.That(strictResult.IsFailure(), Is.True);
        Assert.That(looseResult.IsSuccess(), Is.True);
        Assert.That(looseResult.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
        Assert.That(facadeStrictResult.IsFailure(), Is.True);
        Assert.That(facadeLooseResult.IsSuccess(), Is.True);
    }

    [Test]
    public void IncludePrereleaseOptions_ControlRangeMatchingCompatibility()
    {
        VersionRange range = VersionRange.Parse(">=1.0.0", VersionRangeParseOptions.CreateNodeSemVer());
        VersionRangeParseOptions includeRangeOptions = VersionRangeParseOptions.CreateWithPrereleasePolicy(
            RangeSyntax.NodeSemVer,
            PrereleasePolicy.Include);
        VersionRange includeRange = VersionRange.Parse(">=1.0.0", includeRangeOptions);
        VersionParseOptions includeVersionOptions = VersionParseOptions.CreateWithPrereleasePolicy(
            VersionDialect.NodeSemVer,
            VersionParseMode.Loose,
            PrereleasePolicy.Include);
        VersionParseOptions excludeVersionOptions = VersionParseOptions.CreateWithPrereleasePolicy(
            VersionDialect.NodeSemVer,
            VersionParseMode.Loose,
            PrereleasePolicy.Exclude);

        Assert.That(range.ContainsTextWithOptions("1.1.0-alpha", excludeVersionOptions), Is.False);
        Assert.That(range.ContainsTextWithOptions("1.1.0-alpha", includeVersionOptions), Is.True);
        Assert.That(includeRange.AllowsPrereleasesByDefault(), Is.True);
        Assert.That(includeRange.ContainsText("1.1.0-alpha", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void ComparisonModeOptions_SelectPrecedenceIdentityAndInvalidOrderingBehavior()
    {
        VersionComparisonOptions precedenceOptions = VersionComparisonOptions.CreatePrecedence(VersionDialect.SemVer);
        VersionComparisonOptions identityOptions = VersionComparisonOptions.CreateIdentity(VersionDialect.SemVer);
        VersionComparisonOptions buildOptions = VersionComparisonOptions.CreateIncludingBuildMetadata(
            VersionDialect.SemVer,
            VersionComparisonMode.Precedence);
        VersionComparisonOptions invalidBeforeOptions = VersionComparisonOptions.CreateWithInvalidVersionOrdering(
            VersionDialect.SemVer,
            VersionComparisonMode.Precedence,
            InvalidVersionOrdering.SortBeforeValid);

        Assert.That(UniversalVersion.Compare("1.0.0+1", "1.0.0+2", precedenceOptions), Is.EqualTo(0));
        Assert.That(UniversalVersion.Compare("1.0.0+1", "1.0.0+2", identityOptions), Is.LessThan(0));
        Assert.That(SemVer.ComparePrecedence("1.0.0+1", "1.0.0+2", buildOptions), Is.LessThan(0));
        Assert.That(UniversalVersion.Compare("bad", "1.0.0", invalidBeforeOptions), Is.LessThan(0));
    }
}
