using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheBulkApiTests
{
    [Test]
    public void BulkMethods_ReturnPerItemOutcomesInInputOrder()
    {
        ICache cache = new Cache(CacheOptions.Default());
        CachePutRequest first = Request("a", "one", null);
        CachePutRequest second = Request("b", "two", null);
        CachePutRequest third = Request("c", "three", null);
        CachePutRequest?[] requests = new CachePutRequest?[] { first, second, third };

        CacheBulkWriteResult writes = cache.PutMany(requests);
        object?[] getKeys = new object?[] { "c", "missing", "a" };
        CacheBulkGetResult gets = cache.GetMany(getKeys);
        object?[] removeKeys = new object?[] { "b", "missing", "a" };
        CacheBulkRemoveResult removals = cache.RemoveMany(removeKeys);

        Assert.That(writes.Count, Is.EqualTo(3L));
        Assert.That(writes.Results, Has.Length.EqualTo(3));
        Assert.That(writes.Results[0].Key, Is.EqualTo("a"));
        Assert.That(writes.Results[1].Key, Is.EqualTo("b"));
        Assert.That(writes.Results[2].Key, Is.EqualTo("c"));
        Assert.That(writes.Results[0].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(writes.Results[1].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(writes.Results[2].Status, Is.EqualTo(CacheResultStatus.Written));

        Assert.That(gets.Count, Is.EqualTo(3L));
        Assert.That(gets.Results, Has.Length.EqualTo(3));
        Assert.That(gets.Results[0].Value, Is.EqualTo("three"));
        Assert.That(gets.Results[0].Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(gets.Results[1].Key, Is.EqualTo("missing"));
        Assert.That(gets.Results[1].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(gets.Results[2].Value, Is.EqualTo("one"));

        Assert.That(removals.Count, Is.EqualTo(3L));
        Assert.That(removals.Results, Has.Length.EqualTo(3));
        Assert.That(removals.Results[0].Value, Is.EqualTo("two"));
        Assert.That(removals.Results[0].Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(removals.Results[1].Key, Is.EqualTo("missing"));
        Assert.That(removals.Results[1].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(removals.Results[2].Value, Is.EqualTo("one"));
        Assert.That(cache.Count(), Is.EqualTo(1L));
    }

    [Test]
    public void PutMany_UsesRequestOptionsAndContinuesAfterNullRequest()
    {
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        ICache cache = new Cache(options);
        CachePutRequest expiring = Request("expiring", "one", CacheEntryOptions.TimeToLive(5L));
        CachePutRequest live = Request("live", "two", null);
        CachePutRequest?[] requests = new CachePutRequest?[] { expiring, null, live };

        CacheBulkWriteResult writes = cache.PutMany(requests);
        clock.AdvanceTicks(5L);
        object?[] keys = new object?[] { "expiring", "live" };
        CacheBulkGetResult gets = cache.GetMany(keys);

        Assert.That(writes.Count, Is.EqualTo(3L));
        Assert.That(writes.Results[0].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(writes.Results[1].Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(writes.Results[1].Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(writes.Results[2].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(gets.Results[0].Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(gets.Results[1].Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(gets.Results[1].Value, Is.EqualTo("two"));
    }

    [Test]
    public void BulkMethods_WithEmptySpans_ReturnEmptyOrderedResults()
    {
        ICache cache = new Cache(CacheOptions.Default());
        CachePutRequest?[] requests = new CachePutRequest?[0];
        object?[] keys = new object?[0];

        CacheBulkWriteResult writes = cache.PutMany(requests);
        CacheBulkGetResult gets = cache.GetMany(keys);
        CacheBulkRemoveResult removals = cache.RemoveMany(keys);

        Assert.That(writes.Count, Is.EqualTo(0L));
        Assert.That(writes.Results, Is.Empty);
        Assert.That(gets.Count, Is.EqualTo(0L));
        Assert.That(gets.Results, Is.Empty);
        Assert.That(removals.Count, Is.EqualTo(0L));
        Assert.That(removals.Results, Is.Empty);
    }

    static CachePutRequest Request(object? key, object? value, CacheEntryOptions? options)
    {
        CachePutRequest request = new CachePutRequest();
        request.Key = key;
        request.Value = value;
        request.Options = options;
        return request;
    }
}
