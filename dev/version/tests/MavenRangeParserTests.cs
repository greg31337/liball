using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenRangeParserTests
{
    [Test]
    public void Parse_BareSoftRequirementCreatesUniversalRange()
    {
        VersionRange range = ParseRange("1.0");

        Assert.That(range.IsUniversal(), Is.True);
        Assert.That(range.ContainsText("0.1", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("999.0", VersionDialect.Maven), Is.True);
    }

    [Test]
    public void Parse_ExactHardRequirementMatchesOnlyEqualMavenVersions()
    {
        VersionRange range = ParseRange("[1.0]");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.Equal));
        Assert.That(range.ContainsText("1", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.ga", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.1", VersionDialect.Maven), Is.False);
    }

    [Test]
    public void Parse_OpenEndedIntervalsRespectInclusiveAndExclusiveBounds()
    {
        VersionRange upperInclusive = ParseRange("(,1.0]");
        VersionRange lowerInclusive = ParseRange("[1.5,)");
        VersionRange lowerExclusive = ParseRange("(1.5,)");

        Assert.That(upperInclusive.ContainsText("0.9", VersionDialect.Maven), Is.True);
        Assert.That(upperInclusive.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(upperInclusive.ContainsText("1.1", VersionDialect.Maven), Is.False);

        Assert.That(lowerInclusive.ContainsText("1.4", VersionDialect.Maven), Is.False);
        Assert.That(lowerInclusive.ContainsText("1.5", VersionDialect.Maven), Is.True);
        Assert.That(lowerInclusive.ContainsText("2.0", VersionDialect.Maven), Is.True);

        Assert.That(lowerExclusive.ContainsText("1.5", VersionDialect.Maven), Is.False);
        Assert.That(lowerExclusive.ContainsText("1.5.1", VersionDialect.Maven), Is.True);
    }

    [Test]
    public void Parse_BoundedIntervalsRespectEdgeKinds()
    {
        VersionRange inclusive = ParseRange("[1.2,1.3]");
        VersionRange mixed = ParseRange("[1.0,2.0)");
        VersionRange exclusive = ParseRange("(1.0,2.0)");

        Assert.That(inclusive.ContainsText("1.2", VersionDialect.Maven), Is.True);
        Assert.That(inclusive.ContainsText("1.3", VersionDialect.Maven), Is.True);
        Assert.That(inclusive.ContainsText("1.3.1", VersionDialect.Maven), Is.False);

        Assert.That(mixed.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(mixed.ContainsText("1.5", VersionDialect.Maven), Is.True);
        Assert.That(mixed.ContainsText("2.0", VersionDialect.Maven), Is.False);

        Assert.That(exclusive.ContainsText("1.0", VersionDialect.Maven), Is.False);
        Assert.That(exclusive.ContainsText("1.5", VersionDialect.Maven), Is.True);
        Assert.That(exclusive.ContainsText("2.0", VersionDialect.Maven), Is.False);
    }

    [Test]
    public void Parse_CommaSeparatedIntervalsCreateUnion()
    {
        VersionRange range = ParseRange("(,1.0],[1.2,)");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("0.9", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.1", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("1.2", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("2.0", VersionDialect.Maven), Is.True);
    }

    [Test]
    public void Parse_CommaSeparatedIntervalsRepresentExclusion()
    {
        VersionRange range = ParseRange("(,1.1),(1.1,)");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.1", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("1.2", VersionDialect.Maven), Is.True);
    }

    [Test]
    public void Parse_UsesMavenVersionOrderingInsideIntervals()
    {
        VersionRange range = ParseRange("[1.0,2.0)");

        Assert.That(range.ContainsText("1.5-snapshot", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("2.0-rc1", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("2.0", VersionDialect.Maven), Is.False);
    }

    [Test]
    public void Parse_AllowsWhitespaceAroundIntervals()
    {
        VersionRange range = ParseRange("  (,1.0] , [1.2,)  ");

        Assert.That(range.ContainsText("1.0", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.1", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("1.2", VersionDialect.Maven), Is.True);
    }

    [Test]
    public void Parse_ResultCarriesMavenSyntaxAndRange()
    {
        VersionRangeParseResult result = MavenRangeParser.Parse("[1.0,2.0)");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Maven));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().ContainsText("1.5", VersionDialect.Maven), Is.True);
        Assert.That(result.GetRange().ContainsText("2.0", VersionDialect.Maven), Is.False);
    }

    [Test]
    public void Parse_RejectsInvalidRanges()
    {
        AssertFailure("", MavenRangeDiagnosticCodes.MissingRange);
        AssertFailure("   ", MavenRangeDiagnosticCodes.MissingRange);
        AssertFailure("(1.0)", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0)", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,]", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[,1.0]", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("(,)", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[2.0,1.0]", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,1.0)", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,2.0),", MavenRangeDiagnosticCodes.MissingRange);
    }

    [Test]
    public void Parse_RejectsInvalidVersionsAndDelimiters()
    {
        AssertFailure("[bad version]", MavenRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("[1.0,bad version)", MavenRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("[1.0,2.0", MavenRangeDiagnosticCodes.InvalidDelimiter);
        AssertFailure("[1.0,2.0) trailing", MavenRangeDiagnosticCodes.InvalidDelimiter);
    }

    [Test]
    public void FailureResult_DoesNotExposeRange()
    {
        VersionRangeParseResult result = MavenRangeParser.Parse("[bad version]");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Maven));
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
                MavenRangeParser.Parse(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = MavenRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = MavenRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Maven), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
