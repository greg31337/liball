using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeXRangeParserTests
{
    [Test]
    public void Parse_WildcardCreatesUniversalRange()
    {
        VersionRange range = ParseRange("*");

        Assert.That(range.IsUniversal(), Is.True);
        Assert.That(range.ContainsText("0.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("999.0.0", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void Parse_MajorWildcardMatchesMajorLine()
    {
        VersionRange range = ParseRange("1.x");

        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("0.9.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_MinorWildcardMatchesMajorMinorLine()
    {
        VersionRange range = ParseRange("1.2.*");

        Assert.That(range.ContainsText("1.1.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_UppercaseXIsWildcard()
    {
        VersionRange range = ParseRange("1.X");

        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_PartialMajorAndMinorAreXRanges()
    {
        VersionRange major = ParseRange("1");
        VersionRange minor = ParseRange("1.2");

        Assert.That(major.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(major.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(minor.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(minor.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_PartialComparatorsUseDocumentedBounds()
    {
        VersionRange greaterOrEqual = ParseRange(">=1.2.x");
        VersionRange greaterThan = ParseRange(">1.2.x");
        VersionRange lessOrEqual = ParseRange("<=1.2.x");
        VersionRange lessThan = ParseRange("<1.2.x");

        Assert.That(greaterOrEqual.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(greaterOrEqual.ContainsText("1.1.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(greaterThan.ContainsText("1.2.99", VersionDialect.NodeSemVer), Is.False);
        Assert.That(greaterThan.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(lessOrEqual.ContainsText("1.2.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(lessOrEqual.ContainsText("1.3.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(lessThan.ContainsText("1.1.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(lessThan.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_XRangesComposeWithWhitespaceAndPipePipe()
    {
        VersionRange range = ParseRange("1.2.x <1.2.5 || 2.x");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("1.2.4", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.5", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("2.5.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("3.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_HyphenRangeAllowsPartialBounds()
    {
        VersionRange lowerPartial = ParseRange("1.2 - 2.3.4");
        VersionRange upperMinorPartial = ParseRange("1.2.3 - 2.3");
        VersionRange upperMajorPartial = ParseRange("1.2.3 - 2");

        Assert.That(lowerPartial.ContainsText("1.2.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(lowerPartial.ContainsText("1.1.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(lowerPartial.ContainsText("2.3.4", VersionDialect.NodeSemVer), Is.True);
        Assert.That(upperMinorPartial.ContainsText("2.3.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(upperMinorPartial.ContainsText("2.4.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(upperMajorPartial.ContainsText("2.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(upperMajorPartial.ContainsText("3.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_RejectsMalformedXRangeShapes()
    {
        AssertFailure("1.2.*.3", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1..x", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.*.3 - 2.0.0", NodeRangeDiagnosticCodes.InvalidVersion);
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
