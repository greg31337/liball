using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenVersionComparerTests
{
    [Test]
    public void Compare_UsesIntegerComparisonForNumericItems()
    {
        AssertCompare("1.10", "1.2", 1);
        AssertCompare("1.000", "1.0", 0);
        AssertCompare("1.00000000000000000010", "1.9", 1);
    }

    [Test]
    public void Compare_UsesQualifierOrderingForStringItems()
    {
        AssertCompare("1-alpha", "1-beta", -1);
        AssertCompare("1-beta", "1-rc", -1);
        AssertCompare("1-rc", "1", -1);
        AssertCompare("1", "1-sp", -1);
    }

    [Test]
    public void Compare_TrimsTrailingNullItems()
    {
        AssertCompare("1", "1.0", 0);
        AssertCompare("1", "1.0.0", 0);
        AssertCompare("1", "1-ga", 0);
        AssertCompare("1", "1-final", 0);
        AssertCompare("1", "1-release", 0);
    }

    [Test]
    public void Compare_ComparesExtraListItemsAgainstNull()
    {
        AssertCompare("1-alpha", "1-alpha.1", -1);
        AssertCompare("1-alpha.0", "1-alpha", 0);
        AssertCompare("1-sp", "1", 1);
    }

    [Test]
    public void Compare_OrdersNumericItemsAfterStringItemsAtSamePosition()
    {
        AssertCompare("1.1", "1.alpha", 1);
        AssertCompare("1.0", "1.alpha", 1);
    }

    static void AssertCompare(string leftText, string rightText, int expected)
    {
        Assert.That(MavenVersionComparer.Compare(leftText, rightText), Is.EqualTo(expected));
        Assert.That(MavenVersionComparer.Compare(rightText, leftText), Is.EqualTo(0 - expected));
    }
}
