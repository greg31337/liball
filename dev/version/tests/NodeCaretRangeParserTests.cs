using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeCaretRangeParserTests
{
    [Test]
    public void Parse_CaretFullMajorVersionAllowsSameMajorOnly()
    {
        VersionRange range = ParseRange("^1.2.3");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("1.2.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_CaretZeroMajorAllowsSameMinorOnly()
    {
        VersionRange range = ParseRange("^0.2.3");

        Assert.That(range.ContainsText("0.2.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("0.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("0.2.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("0.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_CaretZeroMajorAndMinorAllowsSamePatchOnly()
    {
        VersionRange range = ParseRange("^0.0.3");

        Assert.That(range.ContainsText("0.0.2", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("0.0.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("0.0.4", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_CaretPartialMajorAndMinorUseNodeZeroRules()
    {
        VersionRange major = ParseRange("^1");
        VersionRange zeroMajor = ParseRange("^0");
        VersionRange zeroMinor = ParseRange("^0.2");
        VersionRange zeroZero = ParseRange("^0.0");

        Assert.That(major.ContainsText("1.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(major.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(zeroMajor.ContainsText("0.9.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(zeroMajor.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(zeroMinor.ContainsText("0.2.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(zeroMinor.ContainsText("0.3.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(zeroZero.ContainsText("0.0.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(zeroZero.ContainsText("0.1.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_CaretXRangeUsesPartialBounds()
    {
        VersionRange majorWildcard = ParseRange("^1.x");
        VersionRange zeroMinorWildcard = ParseRange("^0.2.x");
        VersionRange zeroZeroWildcard = ParseRange("^0.0.x");

        Assert.That(majorWildcard.ContainsText("1.5.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(majorWildcard.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(zeroMinorWildcard.ContainsText("0.2.5", VersionDialect.NodeSemVer), Is.True);
        Assert.That(zeroMinorWildcard.ContainsText("0.3.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(zeroZeroWildcard.ContainsText("0.0.5", VersionDialect.NodeSemVer), Is.True);
        Assert.That(zeroZeroWildcard.ContainsText("0.1.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_CaretWildcardIsUniversal()
    {
        VersionRange range = ParseRange("^*");

        Assert.That(range.IsUniversal(), Is.True);
        Assert.That(range.ContainsText("0.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("99.0.0", VersionDialect.NodeSemVer), Is.True);
    }

    [Test]
    public void Parse_CaretPreservesPrereleaseLowerBound()
    {
        VersionRange range = ParseRange("^1.2.3-beta.2");

        Assert.That(range.ContainsText("1.2.3-beta.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("1.2.3-beta.2", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_CaretComposesWithWhitespaceAndPipePipe()
    {
        VersionRange range = ParseRange("^1.2.3 <1.5.0 || ^0.2.3");

        Assert.That(range.GetSetCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("1.4.9", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.5.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("0.2.5", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("0.3.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_AllowsWhitespaceAfterCaret()
    {
        VersionRange range = ParseRange("^ 1.2.3");

        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void Parse_RejectsMalformedCaretRanges()
    {
        AssertFailure("^", NodeRangeDiagnosticCodes.MissingVersion);
        AssertFailure("^bad", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("^1.2.*.3", NodeRangeDiagnosticCodes.InvalidVersion);
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
