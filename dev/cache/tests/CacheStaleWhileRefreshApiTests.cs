using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheStaleWhileRefreshApiTests
{
    [Test]
    public void Defaults_DisableStaleWhileRefreshAndPreserveOrdinaryHitState()
    {
        CacheOptions options = new CacheOptions();
        Cache cache = new Cache(options);
        cache.Put("key", "value");

        CachePolicySnapshot policy = cache.Policy();
        CacheGetResult result = cache.Get("key");

        Assert.That(options.StaleWhileRefresh, Is.False);
        Assert.That(policy.StaleWhileRefresh, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Hit));
    }

    [Test]
    public void Options_EnableStaleWhileRefreshAndAreCopiedByCache()
    {
        CacheOptions options = new CacheOptions();
        options.StaleWhileRefresh = true;
        Cache cache = new Cache(options);

        options.StaleWhileRefresh = false;

        Assert.That(cache.Policy().StaleWhileRefresh, Is.True);
    }

    [Test]
    public void Builder_CanEnableAndDisableStaleWhileRefreshAcrossBuilds()
    {
        ICacheBuilder builder = new CacheBuilder();
        Cache enabled = builder.WithStaleWhileRefresh(true).Build();
        Cache disabled = builder.WithStaleWhileRefresh(false).Build();

        Assert.That(enabled.Policy().StaleWhileRefresh, Is.True);
        Assert.That(disabled.Policy().StaleWhileRefresh, Is.False);
    }

    [Test]
    public void GetResult_CanRepresentAStaleLiveEntry()
    {
        CacheGetResult result = new CacheGetResult();
        result.Found = true;
        result.Key = "key";
        result.Value = "old";
        result.Status = CacheResultStatus.Stale;

        Assert.That(result.Found, Is.True);
        Assert.That(result.Value, Is.EqualTo("old"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Stale));
    }
}
