namespace gltf.Tests.Internal.Common;

[TestFixture]
public class CollectionLimitTests
{
    [Test]
    public void EnsureCountWithinLimit_AcceptsBoundary()
    {
        Assert.That(CollectionLimit.EnsureCountWithinLimit(8, 8, "node"), Is.EqualTo(8));
    }

    [Test]
    public void EnsureCountWithinLimit_RejectsInvalidCountsAndLimitExcess()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CollectionLimit.EnsureCountWithinLimit(-1, 8, "node"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollectionLimit.EnsureCountWithinLimit(0, -1, "node"));
        InvalidDataException? exception = Assert.Throws<InvalidDataException>(
            () => CollectionLimit.EnsureCountWithinLimit(9, 8, "node"));

        Assert.That(exception!.Message, Does.Contain("node"));
    }

    [Test]
    public void EnsureAdditionalCountWithinLimit_ReturnsTotalWithoutOverflow()
    {
        Assert.That(CollectionLimit.EnsureAdditionalCountWithinLimit(5, 3, 8, "node"), Is.EqualTo(8));
    }

    [Test]
    public void EnsureAdditionalCountWithinLimit_RejectsExcessBeforeIntegerOverflow()
    {
        Assert.Throws<InvalidDataException>(
            () => CollectionLimit.EnsureAdditionalCountWithinLimit(int.MaxValue, 1, int.MaxValue, "node"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CollectionLimit.EnsureAdditionalCountWithinLimit(0, -1, 8, "node"));
    }
}
