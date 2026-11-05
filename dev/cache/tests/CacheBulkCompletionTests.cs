using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheBulkCompletionTests
{
    [Test]
    public void BulkMethods_TreatComparerEquivalentKeysAsOrderedDuplicates()
    {
        CacheOptions options = CacheOptions.Default();
        options.KeyComparer = new BulkCaseInsensitiveComparer();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("Alpha", "one", null),
            Request("ALPHA", "two", null)
        };

        CacheBulkWriteResult writes = cache.PutMany(requests);
        object?[] getKeys = new object?[] { "alpha", "AlPhA" };
        CacheBulkGetResult gets = cache.GetMany(getKeys);
        object?[] removeKeys = new object?[] { "ALpha", "alpha" };
        CacheBulkRemoveResult removals = cache.RemoveMany(removeKeys);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(writes.Results[0].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(writes.Results[1].Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(writes.Results[1].PreviousValue, Is.EqualTo("one"));
        Assert.That(gets.Results[0].Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(gets.Results[0].Value, Is.EqualTo("two"));
        Assert.That(gets.Results[1].Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(gets.Results[1].Value, Is.EqualTo("two"));
        Assert.That(removals.Results[0].Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(removals.Results[0].Value, Is.EqualTo("two"));
        Assert.That(removals.Results[1].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(stats.Hits, Is.EqualTo(2L));
        Assert.That(stats.Removes, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
    }

    [Test]
    public void PutMany_CachesNullValuesAndPreservesFoundStateAcrossBulkOperations()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        CacheEntryOptions explicitNull = CacheEntryOptions.NoExpiration();
        explicitNull.CacheNullValue = true;
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("default-null", null, null),
            Request("explicit-null", null, explicitNull)
        };

        CacheBulkWriteResult writes = cache.PutMany(requests);
        object?[] keys = new object?[] { "explicit-null", "default-null" };
        CacheBulkGetResult gets = cache.GetMany(keys);
        CacheBulkRemoveResult removals = cache.RemoveMany(keys);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(writes.Results[0].Written, Is.True);
        Assert.That(writes.Results[0].Value, Is.Null);
        Assert.That(writes.Results[1].Written, Is.True);
        Assert.That(writes.Results[1].Value, Is.Null);
        Assert.That(gets.Results[0].Found, Is.True);
        Assert.That(gets.Results[0].Value, Is.Null);
        Assert.That(gets.Results[1].Found, Is.True);
        Assert.That(gets.Results[1].Value, Is.Null);
        Assert.That(removals.Results[0].Removed, Is.True);
        Assert.That(removals.Results[0].Value, Is.Null);
        Assert.That(removals.Results[1].Removed, Is.True);
        Assert.That(removals.Results[1].Value, Is.Null);
        Assert.That(stats.Puts, Is.EqualTo(2L));
        Assert.That(stats.Hits, Is.EqualTo(2L));
        Assert.That(stats.Removes, Is.EqualTo(2L));
    }

    [Test]
    public void PutMany_AppliesPerEntryWeightAndPriorityDuringCapacityEnforcement()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumWeight = true;
        options.MaximumWeight = 4L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions low = CacheEntryOptions.NoExpiration();
        low.Weight = 1L;
        low.Priority = CacheEntryPriority.Low;
        CacheEntryOptions high = CacheEntryOptions.NoExpiration();
        high.Weight = 4L;
        high.Priority = CacheEntryPriority.High;
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("low", "one", low),
            Request("high", "two", high)
        };

        CacheBulkWriteResult result = cache.PutMany(requests);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Results[0].Weight, Is.EqualTo(1L));
        Assert.That(result.Results[1].Weight, Is.EqualTo(4L));
        Assert.That(result.Results[1].EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("low"), Is.False);
        Assert.That(cache.Contains("high"), Is.True);
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(4L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("low"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
    }

    [Test]
    public void PutMany_WhenWeigherThrows_PreservesEarlierWritesAndSkipsLaterRequests()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.Weigher = (key, value) =>
        {
            if (object.Equals(key, "throw"))
            {
                throw new InvalidOperationException("weight failed");
            }

            return 1L;
        };
        Cache cache = new Cache(options);
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("before", "one", null),
            Request("throw", "two", null),
            Request("after", "three", null)
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.PutMany(requests);
            })!;
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(exception.Message, Is.EqualTo("weight failed"));
        Assert.That(cache.Contains("before"), Is.True);
        Assert.That(cache.Contains("throw"), Is.False);
        Assert.That(cache.Contains("after"), Is.False);
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
    }

    [Test]
    public void PutMany_WhenRemovalListenerThrows_PublishesEveryWriteBeforePropagating()
    {
        CacheOptions options = CacheOptions.Default();
        options.RemovalListener = notification => throw new InvalidOperationException("listener failed");
        Cache cache = new Cache(options);
        cache.Put("existing", "old");
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("existing", "new", null),
            Request("later", "value", null)
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.PutMany(requests);
            })!;

        Assert.That(exception.Message, Is.EqualTo("listener failed"));
        Assert.That(cache.Peek("existing").Value, Is.EqualTo("new"));
        Assert.That(cache.Peek("later").Value, Is.EqualTo("value"));
        Assert.That(cache.Count(), Is.EqualTo(2L));
    }

    [Test]
    public void PutMany_WithConcurrentScalarWrite_DoesNotAllowInterleavingBetweenRequests()
    {
        ManualResetEventSlim listenerEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim scalarFinished = new ManualResetEventSlim(false);
        bool listenerObservedScalarCompletion = false;
        CacheOptions options = CacheOptions.Default();
        options.RemovalListener = notification =>
        {
            listenerEntered.Set();
            listenerObservedScalarCompletion = scalarFinished.Wait(5000);
        };
        Cache cache = new Cache(options);
        cache.Put("existing", "old");
        Task scalar = Task.Run(() =>
        {
            listenerEntered.Wait();
            cache.Put("scalar", "value");
            scalarFinished.Set();
        });
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("existing", "new", null),
            Request("later", "value", null)
        };

        CacheBulkWriteResult bulk = cache.PutMany(requests);

        Assert.That(Task.WaitAll(new Task[] { scalar }, 5000), Is.True);
        Assert.That(listenerObservedScalarCompletion, Is.True);
        Assert.That(bulk.Results[0].Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(bulk.Results[1].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(cache._entries["later"].Version, Is.LessThan(cache._entries["scalar"].Version));
        Assert.That(cache.Count(), Is.EqualTo(3L));
    }

    static CachePutRequest Request(object? key, object? value, CacheEntryOptions? options)
    {
        CachePutRequest request = new CachePutRequest();
        request.Key = key;
        request.Value = value;
        request.Options = options;
        return request;
    }

    public class BulkCaseInsensitiveComparer : EqualityComparer<object>
    {
        public override bool Equals(object? left, object? right)
        {
            return string.Equals(left as string, right as string, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(object value)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode((string)value);
        }
    }
}
