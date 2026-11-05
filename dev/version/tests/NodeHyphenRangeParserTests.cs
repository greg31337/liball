using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeHyphenRangeParserTests
{
    [Test]
    public void Parse_DesugarsHyphenRangeToInclusiveBounds()
    {
        VersionRange range = ParseRange("1.2.3 - 2.3.4");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(range.GetSet(0).GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThanOrEqual));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.3.4", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.3.5", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_HyphenRangeAllowsNodeVersionPrefixes()
    {
        VersionRange range = ParseRange("v1.2.3 - v2.0.0");

        Assert.That(range.ContainsText("v1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("v2.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("v2.0.1", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_HyphenRangeComposesWithPipePipeDisjunction()
    {
        VersionRange range = ParseRange("1.2.3 - 2.0.0 || >=3.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.5.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("3.0.0", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void Parse_HyphenRangeCanBeIntersectedWithAdditionalComparators()
    {
        VersionRange range = ParseRange("1.0.0 - 3.0.0 <2.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_DoesNotTreatPrereleaseHyphenAsHyphenRange()
    {
        VersionRange range = ParseRange("1.2.3-rc.1");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(1));
        Assert.That(range.ContainsText("1.2.3-rc.1", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_RejectsMalformedHyphenRanges()
    {
        AssertFailure("1.2.3 - ", NodeRangeDiagnosticCodes.MissingVersion);
        AssertFailure("bad - 2.0.0", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1..2 - 2.0.0", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.3 - 2..0", NodeRangeDiagnosticCodes.InvalidVersion);
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
