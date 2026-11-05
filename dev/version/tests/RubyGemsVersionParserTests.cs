using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsVersionParserTests
{
    [Test]
    public void Parse_SplitsNumericDotSegments()
    {
        RubyGemsVersion version = ParseVersion("1.2.10");

        AssertSegment(version, 0, "1", true);
        AssertSegment(version, 1, "2", true);
        AssertSegment(version, 2, "10", true);
        Assert.That(version.IsPrerelease(), Is.False);
    }

    [Test]
    public void Parse_SplitsAlphanumericSegmentsIntoTextAndNumberRuns()
    {
        RubyGemsVersion version = ParseVersion("1.0.a10");

        AssertSegment(version, 0, "1", true);
        AssertSegment(version, 1, "0", true);
        AssertSegment(version, 2, "a", false);
        AssertSegment(version, 3, "10", true);
        Assert.That(version.IsPrerelease(), Is.True);
    }

    [Test]
    public void Parse_SplitsAlphanumericRunsWithoutDots()
    {
        RubyGemsVersion version = ParseVersion("1.0a9");

        AssertSegment(version, 0, "1", true);
        AssertSegment(version, 1, "0", true);
        AssertSegment(version, 2, "a", false);
        AssertSegment(version, 3, "9", true);
    }

    [Test]
    public void Parse_TrimsWhitespaceAndTreatsEmptyStringAsZero()
    {
        Assert.That(ParseVersion("  2.0  ").GetOriginalText(), Is.EqualTo("2.0"));
        AssertSegment(ParseVersion("   "), 0, "0", true);
    }

    [Test]
    public void Parse_RejectsMalformedVersions()
    {
        AssertInvalid("a.1");
        AssertInvalid("1..0");
        AssertInvalid("1.");
        AssertInvalid("1.0-rc1");
        AssertInvalid("1.0_rc1");
    }

    static RubyGemsVersion ParseVersion(string text)
    {
        RubyGemsVersionParseResult result = RubyGemsVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasVersion(), Is.True);
        return result.GetVersion();
    }

    static void AssertSegment(RubyGemsVersion version, int index, string text, bool numeric)
    {
        Assert.That(version.GetSegment(index).GetText(), Is.EqualTo(text));
        Assert.That(version.GetSegment(index).IsNumeric(), Is.EqualTo(numeric));
        Assert.That(version.GetSegment(index).IsText(), Is.EqualTo(!numeric));
    }

    static void AssertInvalid(string text)
    {
        RubyGemsVersionParseResult result = RubyGemsVersionParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(RubyGemsDiagnosticCodes.InvalidVersion));
    }
}
