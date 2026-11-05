using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeRangeParserTests
{
    [Test]
    public void Parse_WhitespaceComparatorsCreateIntersectionSet()
    {
        VersionRange range = ParseRange(">=1.2.3 <2.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(range.GetSet(0).GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_PipePipeCreatesDisjunction()
    {
        VersionRange range = ParseRange("<1.0.0 || >=2.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("0.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.5.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void Parse_AllowsWhitespaceBetweenOperatorAndVersion()
    {
        VersionRange range = ParseRange("  >=  v1.2.3   <  v2.0.0  ");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.ContainsText("v1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("v2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_AllowsCompactPipePipeWithoutOuterWhitespace()
    {
        VersionRange range = ParseRange("<1.0.0||>=2.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("0.5.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.5.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("2.5.0", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void Parse_EmptyRangeCreatesUniversalRange()
    {
        VersionRange range = ParseRange("   ");

        Assert.That(range.IsUniversal(), Is.True);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("bad", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_ResultCarriesRange()
    {
        VersionRangeParseResult result = NodeRangeParser.Parse("1.2.3");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(result.GetRange().ContainsText("1.2.4", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_RejectsMalformedDisjunctions()
    {
        AssertFailure("|| 1.2.3", NodeRangeDiagnosticCodes.MissingComparator);
        AssertFailure("1.2.3 ||", NodeRangeDiagnosticCodes.MissingComparator);
        AssertFailure("1.2.3 || || 2.0.0", NodeRangeDiagnosticCodes.MissingComparator);
        AssertFailure("1.2.3 | 2.0.0", NodeRangeDiagnosticCodes.MissingComparator);
    }

    [Test]
    public void Parse_RejectsMalformedPrimitiveComparators()
    {
        AssertFailure(">=bad <2.0.0", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("!=1.2.3", NodeRangeDiagnosticCodes.InvalidOperator);
    }

    [Test]
    public void FailureResult_DoesNotExposeRange()
    {
        VersionRangeParseResult result = NodeRangeParser.Parse(">=bad");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
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
        VersionRangeParseResult result = NodeRangeParser.Parse(null);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.NullInput));
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
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode));
    }
}
