using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoRangeParserTests
{
    [Test]
    public void Parse_CommaSeparatedComparatorsCreateIntersectionSet()
    {
        VersionRange range = ParseRange(">=1.2.3, <2.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(range.GetSet(0).GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_AllowsWhitespaceAroundCommasAndOperators()
    {
        VersionRange range = ParseRange("  >= 1.2.3 , <= 1.5.0  ");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.5.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.5.1", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_CombinesMoreThanTwoRequirements()
    {
        VersionRange range = ParseRange(">=1.0.0, <2.0.0, =1.5.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.4.9", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("1.5.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.5.1", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_AllowsStrictSemVerPrereleaseAndBuildInComparators()
    {
        VersionRange range = ParseRange(">=1.0.0-alpha.1+build.5, <1.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.ContainsText("1.0.0-alpha.2", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_ResultCarriesCargoSyntaxAndRange()
    {
        VersionRangeParseResult result = CargoRangeParser.Parse(">=1.2.3, <2.0.0");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().ContainsText("1.5.0", VersionDialect.Cargo), Is.True);
        Assert.That(result.GetRange().ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_BareFullVersionUsesDefaultCaretRange()
    {
        VersionRange range = ParseRange("1.2.3");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(range.GetSet(0).GetComparator(0).GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
        Assert.That(range.GetSet(0).GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(range.GetSet(0).GetComparator(1).GetVersion().ToNormalizedString(), Is.EqualTo("2.0.0"));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_BareMajorAndMinorVersionsUseDefaultCaretRange()
    {
        VersionRange minorRange = ParseRange("1.2");
        VersionRange majorRange = ParseRange("1");

        Assert.That(minorRange.ContainsText("1.1.9", VersionDialect.Cargo), Is.False);
        Assert.That(minorRange.ContainsText("1.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(minorRange.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(minorRange.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);

        Assert.That(majorRange.ContainsText("0.9.9", VersionDialect.Cargo), Is.False);
        Assert.That(majorRange.ContainsText("1.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(majorRange.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(majorRange.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_BareZeroMajorVersionsUseLeftMostNonZeroCompatibility()
    {
        VersionRange zeroMinorPatch = ParseRange("0.2.3");
        VersionRange zeroMinor = ParseRange("0.2");
        VersionRange zeroZeroPatch = ParseRange("0.0.3");
        VersionRange zeroZero = ParseRange("0.0");
        VersionRange zero = ParseRange("0");

        Assert.That(zeroMinorPatch.ContainsText("0.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(zeroMinorPatch.ContainsText("0.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(zeroMinorPatch.ContainsText("0.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(zeroMinorPatch.ContainsText("0.3.0", VersionDialect.Cargo), Is.False);

        Assert.That(zeroMinor.ContainsText("0.1.9", VersionDialect.Cargo), Is.False);
        Assert.That(zeroMinor.ContainsText("0.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(zeroMinor.ContainsText("0.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(zeroMinor.ContainsText("0.3.0", VersionDialect.Cargo), Is.False);

        Assert.That(zeroZeroPatch.ContainsText("0.0.2", VersionDialect.Cargo), Is.False);
        Assert.That(zeroZeroPatch.ContainsText("0.0.3", VersionDialect.Cargo), Is.True);
        Assert.That(zeroZeroPatch.ContainsText("0.0.4", VersionDialect.Cargo), Is.False);

        Assert.That(zeroZero.ContainsText("0.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(zeroZero.ContainsText("0.0.99", VersionDialect.Cargo), Is.True);
        Assert.That(zeroZero.ContainsText("0.1.0", VersionDialect.Cargo), Is.False);

        Assert.That(zero.ContainsText("0.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(zero.ContainsText("0.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(zero.ContainsText("1.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_ExplicitCaretUsesDefaultCaretRange()
    {
        VersionRange range = ParseRange("^1.2.3");

        Assert.That(range.ContainsText("1.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_BareDefaultRequirementsIntersectWithExplicitComparators()
    {
        VersionRange range = ParseRange("1.2.3, <1.5.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.4.9", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.5.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_TildeRequirementsUseCargoBounds()
    {
        VersionRange full = ParseRange("~1.2.3");
        VersionRange minor = ParseRange("~1.2");
        VersionRange major = ParseRange("~1");

        Assert.That(full.ContainsText("1.2.2", VersionDialect.Cargo), Is.False);
        Assert.That(full.ContainsText("1.2.3", VersionDialect.Cargo), Is.True);
        Assert.That(full.ContainsText("1.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(full.ContainsText("1.3.0", VersionDialect.Cargo), Is.False);

        Assert.That(minor.ContainsText("1.1.9", VersionDialect.Cargo), Is.False);
        Assert.That(minor.ContainsText("1.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(minor.ContainsText("1.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(minor.ContainsText("1.3.0", VersionDialect.Cargo), Is.False);

        Assert.That(major.ContainsText("0.9.9", VersionDialect.Cargo), Is.False);
        Assert.That(major.ContainsText("1.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(major.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(major.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_WildcardRequirementsUseCargoBounds()
    {
        VersionRange any = ParseRange("*");
        VersionRange major = ParseRange("1.*");
        VersionRange minor = ParseRange("1.2.x");

        Assert.That(any.ContainsText("0.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(any.ContainsText("999.0.0", VersionDialect.Cargo), Is.True);

        Assert.That(major.ContainsText("0.9.9", VersionDialect.Cargo), Is.False);
        Assert.That(major.ContainsText("1.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(major.ContainsText("1.9.9", VersionDialect.Cargo), Is.True);
        Assert.That(major.ContainsText("2.0.0", VersionDialect.Cargo), Is.False);

        Assert.That(minor.ContainsText("1.1.9", VersionDialect.Cargo), Is.False);
        Assert.That(minor.ContainsText("1.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(minor.ContainsText("1.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(minor.ContainsText("1.3.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void Parse_PartialComparisonRequirementsUseCargoComparatorRules()
    {
        VersionRange bounded = ParseRange(">=1.2, <1.5");
        VersionRange greaterMajor = ParseRange(">1");
        VersionRange greaterMinor = ParseRange(">1.2");
        VersionRange lessOrEqualMinor = ParseRange("<=1.2");
        VersionRange exactMinor = ParseRange("=1.2");

        Assert.That(bounded.ContainsText("1.1.9", VersionDialect.Cargo), Is.False);
        Assert.That(bounded.ContainsText("1.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(bounded.ContainsText("1.4.9", VersionDialect.Cargo), Is.True);
        Assert.That(bounded.ContainsText("1.5.0", VersionDialect.Cargo), Is.False);

        Assert.That(greaterMajor.ContainsText("1.9.9", VersionDialect.Cargo), Is.False);
        Assert.That(greaterMajor.ContainsText("2.0.0", VersionDialect.Cargo), Is.True);

        Assert.That(greaterMinor.ContainsText("1.2.99", VersionDialect.Cargo), Is.False);
        Assert.That(greaterMinor.ContainsText("1.3.0", VersionDialect.Cargo), Is.True);

        Assert.That(lessOrEqualMinor.ContainsText("1.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(lessOrEqualMinor.ContainsText("1.3.0", VersionDialect.Cargo), Is.False);

        Assert.That(exactMinor.ContainsText("1.1.9", VersionDialect.Cargo), Is.False);
        Assert.That(exactMinor.ContainsText("1.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(exactMinor.ContainsText("1.2.99", VersionDialect.Cargo), Is.True);
        Assert.That(exactMinor.ContainsText("1.3.0", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void ContainsText_CargoExcludesPrereleasesUnlessAllowedByRequirement()
    {
        VersionRange range = ParseRange(">=1.0.0, <2.0.0");

        Assert.That(range.ContainsText("1.5.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.5.0-alpha.1", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.5.0-alpha.1", VersionDialect.Cargo, true), Is.True);
    }

    [Test]
    public void ContainsText_CargoAllowsPrereleasesWhenRequirementMentionsSameCore()
    {
        VersionRange range = ParseRange("1.0.0-alpha");

        Assert.That(range.ContainsText("1.0.0-alpha.1", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.0.0-beta", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.2.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.0.1-alpha", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsText("2.0.0-alpha", VersionDialect.Cargo), Is.False);
    }

    [Test]
    public void ContainsText_CargoWildcardDoesNotMatchPrereleasesByDefault()
    {
        VersionRange range = ParseRange("*");

        Assert.That(range.ContainsText("1.0.0", VersionDialect.Cargo), Is.True);
        Assert.That(range.ContainsText("1.0.0-alpha.1", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.0.0-alpha.1", VersionDialect.Cargo, true), Is.True);
    }

    [Test]
    public void Parse_RejectsMalformedCommaIntersections()
    {
        AssertFailure("", CargoRangeDiagnosticCodes.MissingComparator);
        AssertFailure("   ", CargoRangeDiagnosticCodes.MissingComparator);
        AssertFailure(">=1.2.3,", CargoRangeDiagnosticCodes.MissingComparator);
        AssertFailure(">=1.2.3,,<2.0.0", CargoRangeDiagnosticCodes.MissingComparator);
        AssertFailure(">=1.2.3 <2.0.0", CargoRangeDiagnosticCodes.InvalidOperator);
    }

    [Test]
    public void Parse_RejectsUnsupportedOrFutureRequirementForms()
    {
        AssertFailure("!=1.2.3", CargoRangeDiagnosticCodes.InvalidOperator);
        AssertFailure(">=bad", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("^", CargoRangeDiagnosticCodes.MissingVersion);
        AssertFailure("^bad", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("~", CargoRangeDiagnosticCodes.MissingVersion);
        AssertFailure("~bad", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("~1.*", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure(">=1.*", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("*.*", CargoRangeDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void FailureResult_DoesNotExposeRange()
    {
        VersionRangeParseResult result = CargoRangeParser.Parse(">=bad");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(
            delegate
            {
                result.GetRange();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void Parse_RejectsNullText()
    {
        Assert.That(
            delegate
            {
                CargoRangeParser.Parse(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = CargoRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasRange(), Is.True);
        return result.GetRange();
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = CargoRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode));
    }
}
