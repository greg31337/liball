using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerRangeCompatibilityTests
{
    [Test]
    public void Compatibility_PrimitiveComparatorExamples()
    {
        AssertSatisfies(">=1.2.7", new string[] { "1.2.7", "1.2.8", "2.5.3" });
        AssertDoesNotSatisfy(">=1.2.7", new string[] { "1.2.6", "1.1.0" });
    }

    [Test]
    public void Compatibility_HyphenRangeExamples()
    {
        AssertSatisfies("1.2.3 - 2.3.4", new string[] { "1.2.3", "2.0.0", "2.3.4" });
        AssertDoesNotSatisfy("1.2.3 - 2.3.4", new string[] { "1.2.2", "2.3.5" });

        AssertSatisfies("1.2.3 - 2.3", new string[] { "1.2.3", "2.3.99" });
        AssertDoesNotSatisfy("1.2.3 - 2.3", new string[] { "1.2.2", "2.4.0" });
    }

    [Test]
    public void Compatibility_XRangeAndPartialExamples()
    {
        AssertSatisfies("1.2.x", new string[] { "1.2.0", "1.2.99" });
        AssertDoesNotSatisfy("1.2.x", new string[] { "1.1.9", "1.3.0" });

        AssertSatisfies("1.x", new string[] { "1.0.0", "1.9.9" });
        AssertDoesNotSatisfy("1.x", new string[] { "0.9.9", "2.0.0" });

        AssertSatisfies("1", new string[] { "1.0.0", "1.9.9" });
        AssertDoesNotSatisfy("1", new string[] { "2.0.0" });

        AssertSatisfies("*", new string[] { "0.0.0", "999.0.0" });
    }

    [Test]
    public void Compatibility_TildeRangeExamples()
    {
        AssertSatisfies("~1.2.3", new string[] { "1.2.3", "1.2.99" });
        AssertDoesNotSatisfy("~1.2.3", new string[] { "1.2.2", "1.3.0" });

        AssertSatisfies("~1.2", new string[] { "1.2.0", "1.2.99" });
        AssertDoesNotSatisfy("~1.2", new string[] { "1.1.9", "1.3.0" });

        AssertSatisfies("~1", new string[] { "1.0.0", "1.9.9" });
        AssertDoesNotSatisfy("~1", new string[] { "0.9.9", "2.0.0" });
    }

    [Test]
    public void Compatibility_CaretRangeExamples()
    {
        AssertSatisfies("^1.2.3", new string[] { "1.2.3", "1.9.9" });
        AssertDoesNotSatisfy("^1.2.3", new string[] { "1.2.2", "2.0.0" });

        AssertSatisfies("^0.2.3", new string[] { "0.2.3", "0.2.99" });
        AssertDoesNotSatisfy("^0.2.3", new string[] { "0.2.2", "0.3.0" });

        AssertSatisfies("^0.0.3", new string[] { "0.0.3" });
        AssertDoesNotSatisfy("^0.0.3", new string[] { "0.0.2", "0.0.4" });

        AssertSatisfies("^1.2", new string[] { "1.2.0", "1.9.9" });
        AssertDoesNotSatisfy("^1.2", new string[] { "1.1.9", "2.0.0" });

        AssertSatisfies("^0.0", new string[] { "0.0.0", "0.0.99" });
        AssertDoesNotSatisfy("^0.0", new string[] { "0.1.0" });
    }

    [Test]
    public void Compatibility_WhitespaceAndPipePipeExamples()
    {
        AssertSatisfies("1.2.x || >=2.5.0 <3.0.0", new string[] { "1.2.5", "2.5.0", "2.9.9" });
        AssertDoesNotSatisfy("1.2.x || >=2.5.0 <3.0.0", new string[] { "1.3.0", "2.4.9", "3.0.0" });
    }

    [Test]
    public void Compatibility_PrereleaseGatingExamples()
    {
        VersionRange range = ParseRange(">1.2.3-alpha.3");

        Assert.That(range.ContainsText("1.2.3-alpha.7", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("3.4.5-alpha.9", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("3.4.5-alpha.9", VersionDialect.NodeSemVer, true), Is.True);
    }

    static void AssertSatisfies(string rangeText, string[] versions)
    {
        VersionRange range = ParseRange(rangeText);
        int index = 0;

        while (index < versions.Length)
        {
            Assert.That(range.ContainsText(versions[index], VersionDialect.NodeSemVer), Is.True, rangeText + " should contain " + versions[index]);
            index = index + 1;
        }
    }

    static void AssertDoesNotSatisfy(string rangeText, string[] versions)
    {
        VersionRange range = ParseRange(rangeText);
        int index = 0;

        while (index < versions.Length)
        {
            Assert.That(range.ContainsText(versions[index], VersionDialect.NodeSemVer), Is.False, rangeText + " should not contain " + versions[index]);
            index = index + 1;
        }
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = NodeRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasRange(), Is.True);
        return result.GetRange();
    }
}
