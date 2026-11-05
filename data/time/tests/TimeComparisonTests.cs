using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class TimeComparisonTests
{
    [TestCase(-2, -1, -1)]
    [TestCase(5, 5, 0)]
    [TestCase(10, 3, 1)]
    public void CompareInt_ReturnsNormalizedOrdering(int left, int right, int expected)
    {
        Assert.That(TimeComparison.CompareInt(left, right), Is.EqualTo(expected));
    }

    [TestCase(long.MinValue, 0L, -1)]
    [TestCase(9223372036854775807L, 9223372036854775807L, 0)]
    [TestCase(12L, -12L, 1)]
    public void CompareLong_ReturnsNormalizedOrdering(long left, long right, int expected)
    {
        Assert.That(TimeComparison.CompareLong(left, right), Is.EqualTo(expected));
    }

    [Test]
    public void CompareStrings_UsesOrdinalOrdering()
    {
        Assert.That(TimeComparison.CompareStrings("A", "a"), Is.EqualTo(-1));
        Assert.That(TimeComparison.CompareStrings("same", "same"), Is.EqualTo(0));
        Assert.That(TimeComparison.CompareStrings("b", "a"), Is.EqualTo(1));
    }

    [Test]
    public void CompareStrings_HandlesNullsDeterministically()
    {
        Assert.That(TimeComparison.CompareStrings(null, null), Is.EqualTo(0));
        Assert.That(TimeComparison.CompareStrings(null, "value"), Is.EqualTo(-1));
        Assert.That(TimeComparison.CompareStrings("value", null), Is.EqualTo(1));
    }

    [Test]
    public void EqualStrings_UsesSameOrdinalNullSemantics()
    {
        Assert.That(TimeComparison.EqualStrings(null, null), Is.True);
        Assert.That(TimeComparison.EqualStrings("Zone", "Zone"), Is.True);
        Assert.That(TimeComparison.EqualStrings("Zone", "zone"), Is.False);
        Assert.That(TimeComparison.EqualStrings(null, "zone"), Is.False);
    }
}
