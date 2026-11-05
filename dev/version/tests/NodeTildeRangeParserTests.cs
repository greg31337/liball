using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeTildeRangeParserTests
{
    [Test]
    public void Parse_TildeFullVersionUsesNextMinorUpperBound()
    {
        VersionRange range = ParseRange("~1.2.3");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(range.GetSet(0).GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_TildePartialMinorUsesNextMinorUpperBound()
    {
        VersionRange range = ParseRange("~1.2");

        Assert.That(range.ContainsText("1.1.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_TildePartialMajorUsesNextMajorUpperBound()
    {
        VersionRange range = ParseRange("~1");

        Assert.That(range.ContainsText("0.9.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_TildeXRangeUsesSameBoundsAsPartial()
    {
        VersionRange minorWildcard = ParseRange("~1.2.x");
        VersionRange majorWildcard = ParseRange("~1.x");

        Assert.That(minorWildcard.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(minorWildcard.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(majorWildcard.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(majorWildcard.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_TildeWildcardIsUniversal()
    {
        VersionRange range = ParseRange("~*");

        Assert.That(range.IsUniversal(), Is.True);
        Assert.That(range.ContainsText("0.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("999.0.0", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void Parse_TildePreservesPrereleaseLowerBound()
    {
        VersionRange range = ParseRange("~1.2.3-beta.2");

        Assert.That(range.ContainsText("1.2.3-beta.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3-beta.2", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_TildeComposesWithWhitespaceAndPipePipe()
    {
        VersionRange range = ParseRange("~1.2 <1.2.5 || ~2");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("1.2.4", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.5", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("2.5.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("3.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_AllowsWhitespaceAfterTilde()
    {
        VersionRange range = ParseRange("~ 1.2.3");

        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_RejectsMalformedTildeRanges()
    {
        AssertFailure("~", NodeRangeDiagnosticCodes.MissingVersion);
        AssertFailure("~bad", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("~1.2.*.3", NodeRangeDiagnosticCodes.InvalidVersion);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = NodeRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasRange(), Is.True);
        return result.GetRange();
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = NodeRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode));
    }
}
