using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePolicyUpdateApiTests
{
    [Test]
    public void Constructor_CreatesAnEmptyPolicyUpdate()
    {
        CachePolicyUpdate update = new CachePolicyUpdate();

        Assert.That(update.UpdateMaximumEntries, Is.False);
        Assert.That(update.MaximumEntries, Is.EqualTo(0L));
        Assert.That(update.UpdateMaximumWeight, Is.False);
        Assert.That(update.MaximumWeight, Is.EqualTo(0L));
        Assert.That(update.UpdateDefaultAbsoluteExpiration, Is.False);
        Assert.That(update.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(update.UpdateDefaultTimeToLive, Is.False);
        Assert.That(update.DefaultTimeToLiveTicks, Is.EqualTo(0L));
        Assert.That(update.UpdateDefaultSlidingExpiration, Is.False);
        Assert.That(update.DefaultSlidingExpirationTicks, Is.EqualTo(0L));
    }

    [Test]
    public void PublicFields_CanDescribeCombinedPolicyChanges()
    {
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 10L;
        update.UpdateMaximumWeight = true;
        update.MaximumWeight = 20L;
        update.UpdateDefaultAbsoluteExpiration = true;
        update.DefaultAbsoluteExpirationUtcTicks = 30L;
        update.UpdateDefaultTimeToLive = true;
        update.DefaultTimeToLiveTicks = 40L;
        update.UpdateDefaultSlidingExpiration = true;
        update.DefaultSlidingExpirationTicks = 50L;

        Assert.That(update.UpdateMaximumEntries, Is.True);
        Assert.That(update.MaximumEntries, Is.EqualTo(10L));
        Assert.That(update.UpdateMaximumWeight, Is.True);
        Assert.That(update.MaximumWeight, Is.EqualTo(20L));
        Assert.That(update.UpdateDefaultAbsoluteExpiration, Is.True);
        Assert.That(update.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(30L));
        Assert.That(update.UpdateDefaultTimeToLive, Is.True);
        Assert.That(update.DefaultTimeToLiveTicks, Is.EqualTo(40L));
        Assert.That(update.UpdateDefaultSlidingExpiration, Is.True);
        Assert.That(update.DefaultSlidingExpirationTicks, Is.EqualTo(50L));
    }

    [Test]
    public void UpdatePolicy_WithNullOrEmptyRequest_IsANoOpThroughTheInterface()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        ICache cache = new Cache(options);

        CachePolicySnapshot afterNull = cache.UpdatePolicy(null);
        CachePolicySnapshot afterEmpty = cache.UpdatePolicy(new CachePolicyUpdate());

        Assert.That(afterNull, Is.Not.SameAs(afterEmpty));
        Assert.That(afterNull.HasMaximumEntries, Is.True);
        Assert.That(afterNull.MaximumEntries, Is.EqualTo(3L));
        Assert.That(afterEmpty.HasMaximumEntries, Is.True);
        Assert.That(afterEmpty.MaximumEntries, Is.EqualTo(3L));
    }
}
