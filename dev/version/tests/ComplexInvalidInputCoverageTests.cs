using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class ComplexInvalidInputCoverageTests
{
    [Test]
    public void NodeRange_RejectsInvalidComparatorInsideCompoundDisjunction()
    {
        AssertRangeFailure(
            NodeRangeParser.Parse(">=1.0.0 <2.0.0 || ^1.2.*.3 || >=3.0.0"),
            RangeSyntax.NodeSemVer,
            NodeRangeDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void CargoRange_RejectsInvalidTildeRequirementInsideCommaSeparatedIntersection()
    {
        AssertRangeFailure(
            CargoRangeParser.Parse(">=1.0.0, ~1.*, <2.0.0"),
            RangeSyntax.Cargo,
            CargoRangeDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void RubyGemsRange_RejectsMissingPessimisticVersionInsideCommaSeparatedRequirement()
    {
        AssertRangeFailure(
            RubyGemsRangeParser.Parse(">= 1.0, ~>, < 2.0"),
            RangeSyntax.RubyGems,
            RubyGemsRequirementDiagnosticCodes.MissingVersion);
    }

    [Test]
    public void PythonSpecifier_RejectsInvalidCompatibleReleaseInsideSpecifierSet()
    {
        AssertRangeFailure(
            PythonSpecifierParser.Parse(">=1.0, ~=1, <2.0"),
            RangeSyntax.Python,
            PythonSpecifierDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void MavenRange_RejectsInvalidLaterIntervalInsideUnion()
    {
        AssertRangeFailure(
            MavenRangeParser.Parse("[1.0,2.0),[3.0,2.0)"),
            RangeSyntax.Maven,
            MavenRangeDiagnosticCodes.InvalidRange);
    }

    [Test]
    public void NuGetRange_RejectsExclusiveUpperBoundEqualToLowerPrereleaseBound()
    {
        AssertRangeFailure(
            NuGetRangeParser.Parse("[1.0.0-alpha, 1.0.0-alpha)"),
            RangeSyntax.NuGet,
            NuGetRangeDiagnosticCodes.InvalidRange);
    }

    static void AssertRangeFailure(VersionRangeParseResult result, RangeSyntax expectedSyntax, string expectedCode)
    {
        Assert.That(result.IsFailure(), Is.True, result.GetSourceText());
        Assert.That(result.HasRange(), Is.False, result.GetSourceText());
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(expectedSyntax), result.GetSourceText());
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), result.GetSourceText());
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), result.GetSourceText());
        Assert.That(
            delegate
            {
                result.GetRange();
            },
            Throws.TypeOf<System.InvalidOperationException>(),
            result.GetSourceText());
    }
}
