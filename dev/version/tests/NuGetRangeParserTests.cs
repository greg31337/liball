using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetRangeParserTests
{
    [Test]
    public void Parse_BareVersionCreatesInclusiveMinimumRange()
    {
        VersionRange range = ParseRange("1.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(range.ContainsText("0.9.9", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("1.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.0.1", VersionDialect.NuGet), Is.True);
    }

    [Test]
    public void Parse_LowerBoundIntervalsRespectInclusiveAndExclusiveEdges()
    {
        VersionRange inclusive = ParseRange("[1.0,)");
        VersionRange exclusive = ParseRange("(1.0,)");

        Assert.That(inclusive.ContainsText("0.9.9", VersionDialect.NuGet), Is.False);
        Assert.That(inclusive.ContainsText("1.0", VersionDialect.NuGet), Is.True);
        Assert.That(inclusive.ContainsText("1.0.1", VersionDialect.NuGet), Is.True);

        Assert.That(exclusive.ContainsText("1.0", VersionDialect.NuGet), Is.False);
        Assert.That(exclusive.ContainsText("1.0.1", VersionDialect.NuGet), Is.True);
    }

    [Test]
    public void Parse_UpperBoundIntervalsRespectInclusiveAndExclusiveEdges()
    {
        VersionRange inclusive = ParseRange("(,1.0]");
        VersionRange exclusive = ParseRange("(,1.0)");

        Assert.That(inclusive.ContainsText("0.9.9", VersionDialect.NuGet), Is.True);
        Assert.That(inclusive.ContainsText("1.0", VersionDialect.NuGet), Is.True);
        Assert.That(inclusive.ContainsText("1.0.1", VersionDialect.NuGet), Is.False);

        Assert.That(exclusive.ContainsText("0.9.9", VersionDialect.NuGet), Is.True);
        Assert.That(exclusive.ContainsText("1.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_ExactBracketRangeCreatesExactMatch()
    {
        VersionRange range = ParseRange("[1.0]");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.Equal));
        Assert.That(range.ContainsText("1", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.0.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.0.1", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_BoundedIntervalsRespectAllEdgeCombinations()
    {
        VersionRange inclusive = ParseRange("[1.0,2.0]");
        VersionRange exclusive = ParseRange("(1.0,2.0)");
        VersionRange mixed = ParseRange("[1.0,2.0)");

        Assert.That(inclusive.ContainsText("1.0", VersionDialect.NuGet), Is.True);
        Assert.That(inclusive.ContainsText("1.5", VersionDialect.NuGet), Is.True);
        Assert.That(inclusive.ContainsText("2.0", VersionDialect.NuGet), Is.True);

        Assert.That(exclusive.ContainsText("1.0", VersionDialect.NuGet), Is.False);
        Assert.That(exclusive.ContainsText("1.5", VersionDialect.NuGet), Is.True);
        Assert.That(exclusive.ContainsText("2.0", VersionDialect.NuGet), Is.False);

        Assert.That(mixed.ContainsText("1.0", VersionDialect.NuGet), Is.True);
        Assert.That(mixed.ContainsText("1.5", VersionDialect.NuGet), Is.True);
        Assert.That(mixed.ContainsText("2.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_AllowsWhitespaceAndNuGetVersionNormalization()
    {
        VersionRange range = ParseRange(" [ 01.002 , 1.2.3.0 ] ");

        Assert.That(range.ContainsText("1.1.9", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("1.2", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.3.1", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_AllowsPrereleaseAndBuildMetadataBounds()
    {
        VersionRange range = ParseRange("[1.0.0-alpha.1+build.5,1.0.0)");

        Assert.That(range.ContainsText("1.0.0-alpha", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("1.0.0-alpha.1", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.0.0-beta", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_ResultCarriesNuGetSyntaxAndRange()
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse("[1.0,2.0)");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().ContainsText("1.5", VersionDialect.NuGet), Is.True);
        Assert.That(result.GetRange().ContainsText("2.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_RejectsInvalidIntervalForms()
    {
        AssertFailure("", NuGetRangeDiagnosticCodes.MissingVersion);
        AssertFailure("   ", NuGetRangeDiagnosticCodes.MissingVersion);
        AssertFailure("(1.0)", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0)", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,]", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[,1.0]", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("(,)", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[2.0,1.0]", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,1.0)", NuGetRangeDiagnosticCodes.InvalidRange);
    }

    [Test]
    public void Parse_RejectsInvalidVersionsAndDelimiters()
    {
        AssertFailure("bad", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("[bad]", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("[1.0,bad)", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("[1.0,2.0", NuGetRangeDiagnosticCodes.InvalidDelimiter);
        AssertFailure("[1.0,2.0) trailing", NuGetRangeDiagnosticCodes.InvalidDelimiter);
    }

    [Test]
    public void FailureResult_DoesNotExposeRange()
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse("[bad]");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet));
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
                NuGetRangeParser.Parse(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
