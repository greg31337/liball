using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoVersionCompatibilityTests
{
    [Test]
    public void Compatibility_CanonicalCases()
    {
        AssertCanonical("v0", "v0.0.0");
        AssertCanonical("v1", "v1.0.0");
        AssertCanonical("v1.2", "v1.2.0");
        AssertCanonical("v1.2.3", "v1.2.3");
        AssertCanonical("v1.2.3-pre", "v1.2.3-pre");
        AssertCanonical("v1.2.3+meta", "v1.2.3");
    }

    [Test]
    public void Compatibility_ShorthandParseCases()
    {
        AssertShorthand("v0", "0", "0", "0", 2);
        AssertShorthand("v1", "1", "0", "0", 2);
        AssertShorthand("v1.2", "1", "2", "0", 1);
        AssertShorthand("v1.2.3", "1", "2", "3", 0);
    }

    [Test]
    public void Compatibility_InvalidCases()
    {
        AssertInvalid("");
        AssertInvalid("v");
        AssertInvalid("1.2.3");
        AssertInvalid("V1.2.3");
        AssertInvalid("v01.2.3");
        AssertInvalid("v1.02.3");
        AssertInvalid("v1.2.03");
        AssertInvalid("v1.2.3-01");
        AssertInvalid("v1.2.3+bad_meta");
    }

    [Test]
    public void Compatibility_ComparisonCases()
    {
        AssertCompare("bad", "v0.0.0", -1);
        AssertCompare("v1.9.0", "v1.10.0", -1);
        AssertCompare("v1.0.0-pre", "v1.0.0", -1);
        AssertCompare("v1.0.0-pre.1", "v1.0.0-pre.2", -1);
        AssertCompare("v1.0.0+build.1", "v1.0.0+build.2", 0);
        AssertCompare("v2.0.0", "v1.999.999", 1);
    }

    static void AssertCanonical(string text, string canonicalText)
    {
        Assert.That(GoVersionComparer.Canonicalize(text), Is.EqualTo(canonicalText));
    }

    static void AssertShorthand(string text, string major, string minor, string patch, int missingComponentCount)
    {
        VersionParseResult result = GoVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().GetNumericComponent(0), Is.EqualTo(major));
        Assert.That(result.GetVersion().GetNumericComponent(1), Is.EqualTo(minor));
        Assert.That(result.GetVersion().GetNumericComponent(2), Is.EqualTo(patch));
        Assert.That(result.GetVersion().GetMissingComponentCount(), Is.EqualTo(missingComponentCount));
    }

    static void AssertInvalid(string text)
    {
        Assert.That(GoVersionParser.Parse(text).IsFailure(), Is.True);
        Assert.That(GoVersionComparer.Canonicalize(text), Is.EqualTo(""));
    }

    static void AssertCompare(string left, string right, int expected)
    {
        Assert.That(GoVersionComparer.Compare(left, right), Is.EqualTo(expected));
    }
}
