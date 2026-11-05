using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsVersionComparerTests
{
    [Test]
    public void Compare_UsesNumericOrderingForNumericSegments()
    {
        AssertCompare("3.10", "3.2", 1);
        AssertCompare("1.000", "1.0", 0);
        AssertCompare("1.00000000000000000010", "1.9", 1);
    }

    [Test]
    public void Compare_SplitsAlphanumericPrereleaseRuns()
    {
        AssertCompare("1.0.a10", "1.0.a9", 1);
        AssertCompare("1.0.a.2", "1.0.b1", -1);
        AssertCompare("1.0.a10", "1.0.a.10", 0);
    }

    [Test]
    public void Compare_OrdersPrereleaseBeforeReleaseAtSameReleaseLine()
    {
        AssertCompare("1.0.a", "1.0", -1);
        AssertCompare("1.0.0.a", "1.0", -1);
        AssertCompare("1.0.b1", "1.0", -1);
    }

    [Test]
    public void Compare_CanonicalizesTrailingZerosAndZerosBeforeFirstTextSegment()
    {
        AssertCompare("1", "1.0.0", 0);
        AssertCompare("1.0.a", "1.a", 0);
        AssertCompare("1.0.0.a", "1.a", 0);
        AssertCompare("1.2.0.a", "1.2.a", 0);
    }

    [Test]
    public void IsPrerelease_ReturnsTrueWhenAnySegmentContainsLetters()
    {
        Assert.That(RubyGemsVersionComparer.IsPrerelease("1.0"), Is.False);
        Assert.That(RubyGemsVersionComparer.IsPrerelease("1.0.a"), Is.True);
        Assert.That(RubyGemsVersionComparer.IsPrerelease("1.0a9"), Is.True);
        Assert.That(RubyGemsVersionComparer.IsPrerelease("bad"), Is.False);
    }

    static void AssertCompare(string leftText, string rightText, int expected)
    {
        Assert.That(RubyGemsVersionComparer.Compare(leftText, rightText), Is.EqualTo(expected));
        Assert.That(RubyGemsVersionComparer.Compare(rightText, leftText), Is.EqualTo(0 - expected));
    }
}
