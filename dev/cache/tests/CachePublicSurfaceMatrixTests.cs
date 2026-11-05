using System.Threading;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePublicSurfaceMatrixTests
{
    [Test]
    public void CacheAndEntryOptions_AllFieldsDriveObservableRuntimeBehavior()
    {
        int loaderCalls = 0;
        int weigherCalls = 0;
        CacheNotification? observedNotification = null;
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        ManualResetEventSlim notificationDelivered = new ManualResetEventSlim(false);
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = new CacheOptions();
        options.Name = "matrix";
        options.KeyComparer = new MatrixInsensitiveComparer();
        options.HasDefaultAbsoluteExpiration = true;
        options.DefaultAbsoluteExpirationUtcTicks = 1000L;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 200L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 100L;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 10L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 100L;
        options.EvictionPolicy = CacheEvictionPolicy.Lru;
        options.DefaultPriority = CacheEntryPriority.High;
        options.Clock = clock;
        options.Loader = key =>
        {
            loaderCalls = loaderCalls + 1;
            if (string.Equals(key.ToString(), "key", StringComparison.OrdinalIgnoreCase))
            {
                loaderEntered.Set();
                releaseLoader.Wait(2000);
                return "refreshed";
            }

            return null;
        };
        options.Weigher = (key, value) =>
        {
            weigherCalls = weigherCalls + 1;
            return 4L;
        };
        options.RemovalListener = notification =>
        {
            observedNotification = notification;
            notificationDelivered.Set();
        };
        options.RecordStats = true;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = long.MaxValue;
        options.CacheNullLoaderValues = true;
        options.StaleWhileRefresh = true;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = new CacheEntryOptions();
        entryOptions.HasAbsoluteExpiration = true;
        entryOptions.AbsoluteExpirationUtcTicks = 500L;
        entryOptions.HasTimeToLive = true;
        entryOptions.TimeToLiveTicks = 50L;
        entryOptions.HasSlidingExpiration = true;
        entryOptions.SlidingExpirationTicks = 25L;
        entryOptions.Weight = 3L;
        entryOptions.Priority = CacheEntryPriority.Low;
        entryOptions.HasRefreshAfterWrite = true;
        entryOptions.RefreshAfterWriteTicks = 0L;
        entryOptions.CacheNullValue = true;

        CacheWriteResult write = cache.PutEntry("Key", "old", entryOptions);
        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        releaseLoader.Set();
        Assert.That(notificationDelivered.Wait(2000), Is.True);
        Assert.That(SpinWait.SpinUntil(() => object.Equals(cache.Peek("KEY").Value, "refreshed"), 2000), Is.True);
        CacheGetResult cachedNull = cache.GetOrLoad("null");
        CachePolicySnapshot policy = cache.Policy();
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(write.Written, Is.True);
        Assert.That(write.Weight, Is.EqualTo(4L));
        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(stale.HasExpiration, Is.True);
        Assert.That(stale.ExpirationUtcTicks, Is.EqualTo(125L));
        Assert.That(cachedNull.Found, Is.True);
        Assert.That(cachedNull.Value, Is.Null);
        Assert.That(cachedNull.Loaded, Is.True);
        Assert.That(policy.Name, Is.EqualTo("matrix"));
        Assert.That(policy.HasDefaultAbsoluteExpiration, Is.True);
        Assert.That(policy.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(1000L));
        Assert.That(policy.HasDefaultTimeToLive, Is.True);
        Assert.That(policy.DefaultTimeToLiveTicks, Is.EqualTo(200L));
        Assert.That(policy.HasDefaultSlidingExpiration, Is.True);
        Assert.That(policy.DefaultSlidingExpirationTicks, Is.EqualTo(100L));
        Assert.That(policy.HasMaximumEntries, Is.True);
        Assert.That(policy.MaximumEntries, Is.EqualTo(10L));
        Assert.That(policy.HasMaximumWeight, Is.True);
        Assert.That(policy.MaximumWeight, Is.EqualTo(100L));
        Assert.That(policy.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(policy.DefaultPriority, Is.EqualTo(CacheEntryPriority.High));
        Assert.That(policy.RecordStats, Is.True);
        Assert.That(policy.StaleWhileRefresh, Is.True);
        Assert.That(policy.HasCleanupInterval, Is.True);
        Assert.That(policy.CleanupIntervalTicks, Is.EqualTo(long.MaxValue));
        Assert.That(policy.CurrentCount, Is.EqualTo(2L));
        Assert.That(policy.CurrentWeight, Is.EqualTo(8L));
        Assert.That(policy.OldestKeys, Has.Length.EqualTo(2));
        Assert.That(policy.HottestKeys, Has.Length.EqualTo(2));
        Assert.That(policy.ExpiringSoonKeys, Has.Length.EqualTo(2));
        Assert.That(policy.SnapshotUtcTicks, Is.EqualTo(100L));
        Assert.That(loaderCalls, Is.EqualTo(2));
        Assert.That(weigherCalls, Is.EqualTo(3));
        Assert.That(observedNotification, Is.Not.Null);
        Assert.That(observedNotification!.Name, Is.EqualTo("matrix"));
        Assert.That(observedNotification.Key, Is.EqualTo("Key"));
        Assert.That(observedNotification.Value, Is.EqualTo("old"));
        Assert.That(observedNotification.Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(observedNotification.Weight, Is.EqualTo(4L));
        Assert.That(observedNotification.CreatedUtcTicks, Is.EqualTo(100L));
        Assert.That(observedNotification.LastAccessUtcTicks, Is.EqualTo(100L));
        Assert.That(observedNotification.LastWriteUtcTicks, Is.EqualTo(100L));
        Assert.That(observedNotification.RemovedUtcTicks, Is.EqualTo(100L));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(2L));
        cache.Stop();
    }

    [Test]
    public void CacheAndStoreContracts_AllOperationsProduceTheirRuntimeResults()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key => "loaded-" + key.ToString();
        Cache concrete = new Cache(options);
        ICache cache = concrete;
        ICacheStore store = concrete;
        CacheWriteResult added = cache.Add("add", "one");
        CacheWriteResult duplicate = cache.Add("add", "duplicate");
        CacheWriteResult put = cache.Put("put", "one");
        CacheEntryOptions refreshOptions = new CacheEntryOptions();
        refreshOptions.HasRefreshAfterWrite = true;
        refreshOptions.RefreshAfterWriteTicks = 5L;
        CacheWriteResult entryWrite = cache.PutEntry("refresh", "old", refreshOptions);
        CachePutRequest firstRequest = new CachePutRequest();
        firstRequest.Key = "bulk-a";
        firstRequest.Value = "a";
        firstRequest.Options = new CacheEntryOptions();
        CachePutRequest secondRequest = new CachePutRequest();
        secondRequest.Key = "bulk-b";
        secondRequest.Value = "b";
        secondRequest.Options = null;
        CacheBulkWriteResult bulkWrites = cache.PutMany(new CachePutRequest?[] { firstRequest, secondRequest });
        CacheWriteResult replaced = cache.Replace("put", "two");
        CacheGetResult hit = cache.Get("put");
        CacheGetResult miss = cache.Get("missing");
        CacheBulkGetResult bulkGets = cache.GetMany(new object?[] { "bulk-a", "missing" });
        CacheGetResult peeked = cache.Peek("bulk-b");
        bool contained = cache.Contains("bulk-a");
        long count = cache.Count();
        CacheGetResult loaded = cache.GetOrLoad("load");
        clock.AdvanceTicks(5L);
        bool needsRefresh = cache.NeedsRefresh("refresh");
        CacheGetResult refreshed = cache.Refresh("refresh");
        CacheRemoveResult removed = cache.Remove("add");
        CacheBulkRemoveResult bulkRemoved = cache.RemoveMany(new object?[] { "bulk-a", "bulk-b" });
        CacheWriteResult storeWrite = store.Write("store", "value");
        CacheGetResult storeRead = store.Read("store");
        CacheEntryOptions expiredOptions = new CacheEntryOptions();
        expiredOptions.HasTimeToLive = true;
        expiredOptions.TimeToLiveTicks = 0L;
        cache.PutEntry("expired-read", "gone", expiredOptions);
        CacheGetResult expired = cache.Get("expired-read");
        cache.PutEntry("expired-cleanup", "gone", expiredOptions);
        CacheMaintenanceResult cleanup = cache.Cleanup();
        CacheStatsSnapshot stats = cache.Stats();
        CachePolicySnapshot policy = cache.Policy();
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 20L;
        update.UpdateMaximumWeight = true;
        update.MaximumWeight = 30L;
        update.UpdateDefaultAbsoluteExpiration = true;
        update.DefaultAbsoluteExpirationUtcTicks = 1000L;
        update.UpdateDefaultTimeToLive = true;
        update.DefaultTimeToLiveTicks = 50L;
        update.UpdateDefaultSlidingExpiration = true;
        update.DefaultSlidingExpirationTicks = 25L;
        CachePolicySnapshot updated = cache.UpdatePolicy(update);
        CacheSnapshot snapshot = cache.Snapshot();
        CacheMaintenanceResult cleared = cache.Clear();
        concrete.Write("concrete-store", "value");
        CacheGetResult concreteRead = concrete.Read("concrete-store");
        concrete.Stop();

        Assert.That(added.Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(duplicate.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(put.Written, Is.True);
        Assert.That(entryWrite.Written, Is.True);
        Assert.That(bulkWrites.Count, Is.EqualTo(2L));
        Assert.That(bulkWrites.Results, Has.Length.EqualTo(2));
        Assert.That(replaced.Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(replaced.PreviousValue, Is.EqualTo("one"));
        Assert.That(hit.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(miss.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(bulkGets.Count, Is.EqualTo(2L));
        Assert.That(bulkGets.Results[0].Found, Is.True);
        Assert.That(bulkGets.Results[1].Found, Is.False);
        Assert.That(peeked.Found, Is.True);
        Assert.That(contained, Is.True);
        Assert.That(count, Is.EqualTo(5L));
        Assert.That(loaded.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(needsRefresh, Is.True);
        Assert.That(refreshed.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(removed.Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(bulkRemoved.Count, Is.EqualTo(2L));
        Assert.That(bulkRemoved.Results[0].Removed, Is.True);
        Assert.That(storeWrite.Written, Is.True);
        Assert.That(storeRead.Found, Is.True);
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(cleanup.Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(cleanup.ExpiredCount, Is.EqualTo(1L));
        Assert.That(stats.Hits, Is.GreaterThan(0L));
        Assert.That(stats.Misses, Is.GreaterThan(0L));
        Assert.That(policy.CurrentCount, Is.GreaterThan(0L));
        Assert.That(updated.MaximumEntries, Is.EqualTo(20L));
        Assert.That(updated.MaximumWeight, Is.EqualTo(30L));
        Assert.That(updated.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(1000L));
        Assert.That(updated.DefaultTimeToLiveTicks, Is.EqualTo(50L));
        Assert.That(updated.DefaultSlidingExpirationTicks, Is.EqualTo(25L));
        Assert.That(snapshot.Count, Is.EqualTo(snapshot.Keys.Length));
        Assert.That(snapshot.Count, Is.EqualTo(snapshot.Values.Length));
        Assert.That(snapshot.Weight, Is.EqualTo(updated.CurrentWeight));
        Assert.That(snapshot.SnapshotUtcTicks, Is.EqualTo(205L));
        Assert.That(cleared.Status, Is.EqualTo(CacheResultStatus.Cleared));
        Assert.That(cleared.ClearedCount, Is.EqualTo(snapshot.Count));
        Assert.That(concreteRead.Value, Is.EqualTo("value"));
    }

    [Test]
    public void BuilderContract_AllConfigurationMethodsAndEvictionPoliciesAreUsable()
    {
        int notifications = 0;
        CacheClock clock = CacheClock.Manual(300L);
        CacheOptions seed = new CacheOptions();
        ICacheBuilder builder = new CacheBuilder();
        Cache configured = builder
            .WithOptions(seed)
            .WithName("builder-matrix")
            .WithKeyComparer(new MatrixInsensitiveComparer())
            .WithClock(clock)
            .WithMaximumEntries(5L)
            .WithMaximumWeight(10L)
            .WithEvictionPolicy(CacheEvictionPolicy.Segmented)
            .WithTimeToLive(100L)
            .WithSlidingExpiration(50L)
            .WithAbsoluteExpiration(1000L)
            .WithDefaultPriority(CacheEntryPriority.High)
            .WithLoader(key => null)
            .WithCacheNullLoaderValues(true)
            .WithStaleWhileRefresh(true)
            .WithWeigher((key, value) => 1L)
            .WithRemovalListener(notification => notifications = notifications + 1)
            .WithStats()
            .WithCleanupInterval(long.MaxValue)
            .Build();
        configured.Put("Key", "one");
        configured.Put("key", "two");
        CacheGetResult loadedNull = configured.GetOrLoad("missing");
        CachePolicySnapshot policy = configured.Policy();
        Cache reset = builder.WithOptions(null).Build();

        Assert.That(policy.Name, Is.EqualTo("builder-matrix"));
        Assert.That(policy.HasMaximumEntries, Is.True);
        Assert.That(policy.HasMaximumWeight, Is.True);
        Assert.That(policy.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Segmented));
        Assert.That(policy.HasDefaultTimeToLive, Is.True);
        Assert.That(policy.HasDefaultSlidingExpiration, Is.True);
        Assert.That(policy.HasDefaultAbsoluteExpiration, Is.True);
        Assert.That(policy.DefaultPriority, Is.EqualTo(CacheEntryPriority.High));
        Assert.That(policy.StaleWhileRefresh, Is.True);
        Assert.That(policy.RecordStats, Is.True);
        Assert.That(policy.HasCleanupInterval, Is.True);
        Assert.That(loadedNull.Found, Is.True);
        Assert.That(loadedNull.Value, Is.Null);
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(reset.Policy().Name, Is.EqualTo(""));
        Assert.That(reset.Policy().HasMaximumEntries, Is.False);
        configured.Stop();
        reset.Stop();

        CacheEvictionPolicy[] policies = new CacheEvictionPolicy[]
        {
            CacheEvictionPolicy.Lru,
            CacheEvictionPolicy.Fifo,
            CacheEvictionPolicy.Lfu,
            CacheEvictionPolicy.Random,
            CacheEvictionPolicy.Segmented
        };
        int index = 0;
        while (index < policies.Length)
        {
            Cache cache = new CacheBuilder().WithEvictionPolicy(policies[index]).Build();
            Assert.That(cache.Policy().EvictionPolicy, Is.EqualTo(policies[index]));
            cache.Stop();
            index = index + 1;
        }
    }

    [Test]
    public void ClockAndMemoizerContracts_AllPublicMethodsAreUsable()
    {
        CacheClock concreteClock = new CacheClock();
        ICacheClock clock = concreteClock;
        IAdjustableCacheClock adjustable = concreteClock;
        long systemTicks = clock.GetUtcTicks();
        adjustable.SetUtcTicks(100L);
        adjustable.AdvanceTicks(5L);
        adjustable.AdvanceMilliseconds(2L);
        adjustable.AdvanceSeconds(1L);
        long manualTicks = clock.GetUtcTicks();
        adjustable.UseSystemUtcTicks();
        long restoredSystemTicks = clock.GetUtcTicks();
        Cache store = new Cache(CacheOptions.Default());
        int functionCalls = 0;
        IMemoizer memoizer = new Memoizer(
            store,
            argument =>
            {
                functionCalls = functionCalls + 1;
                return "memo-" + argument.ToString();
            });

        object? first = memoizer.Invoke("key");
        object? second = memoizer.Invoke("key");

        Assert.That(systemTicks, Is.GreaterThan(0L));
        Assert.That(manualTicks, Is.EqualTo(10020105L));
        Assert.That(restoredSystemTicks, Is.GreaterThan(manualTicks));
        Assert.That(first, Is.EqualTo("memo-key"));
        Assert.That(second, Is.EqualTo("memo-key"));
        Assert.That(functionCalls, Is.EqualTo(1));
        store.Stop();
    }

    [Test]
    public void ResultEnumsAndRemovalCallbacks_AllStatesAndReasonsAreRepresentable()
    {
        CacheResultStatus[] statuses = new CacheResultStatus[]
        {
            CacheResultStatus.Unknown,
            CacheResultStatus.Hit,
            CacheResultStatus.Miss,
            CacheResultStatus.Expired,
            CacheResultStatus.Loaded,
            CacheResultStatus.Written,
            CacheResultStatus.Replaced,
            CacheResultStatus.Removed,
            CacheResultStatus.Rejected,
            CacheResultStatus.Cleared,
            CacheResultStatus.Stale
        };
        CacheRemovalReason[] reasons = new CacheRemovalReason[]
        {
            CacheRemovalReason.Unknown,
            CacheRemovalReason.Replace,
            CacheRemovalReason.ExplicitRemove,
            CacheRemovalReason.Expired,
            CacheRemovalReason.Capacity,
            CacheRemovalReason.Clear,
            CacheRemovalReason.Rejected
        };
        CacheEntryPriority[] priorities = new CacheEntryPriority[]
        {
            CacheEntryPriority.Low,
            CacheEntryPriority.Normal,
            CacheEntryPriority.High
        };
        CacheGetResult getState = new CacheGetResult();
        CacheNotification notificationState = new CacheNotification();
        CacheEntryOptions priorityState = new CacheEntryOptions();
        int index = 0;
        while (index < statuses.Length)
        {
            getState.Status = statuses[index];
            Assert.That(getState.Status, Is.EqualTo(statuses[index]));
            index = index + 1;
        }
        index = 0;
        while (index < reasons.Length)
        {
            notificationState.Reason = reasons[index];
            Assert.That(notificationState.Reason, Is.EqualTo(reasons[index]));
            index = index + 1;
        }
        index = 0;
        while (index < priorities.Length)
        {
            priorityState.Priority = priorities[index];
            Assert.That(priorityState.Priority, Is.EqualTo(priorities[index]));
            index = index + 1;
        }

        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(400L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("capacity-old", "one");
        cache.Put("replace", "two");
        cache.Put("replace", "three");
        cache.Remove("replace");
        CacheEntryOptions expiredOptions = new CacheEntryOptions();
        expiredOptions.HasTimeToLive = true;
        expiredOptions.TimeToLiveTicks = 0L;
        cache.PutEntry("expired", "four", expiredOptions);
        cache.Get("expired");
        cache.Put("clear", "five");
        cache.Clear();
        int beforeRejected = notifications.Count;
        CacheWriteResult rejected = cache.Put(null, "rejected");

        Assert.That(statuses, Has.Length.EqualTo(11));
        Assert.That(reasons, Has.Length.EqualTo(7));
        Assert.That(priorities, Has.Length.EqualTo(3));
        Assert.That(notifications, Has.Count.EqualTo(5));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[1].Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(notifications[2].Reason, Is.EqualTo(CacheRemovalReason.ExplicitRemove));
        Assert.That(notifications[3].Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(notifications[4].Reason, Is.EqualTo(CacheRemovalReason.Clear));
        Assert.That(rejected.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(notifications.Count, Is.EqualTo(beforeRejected));
        cache.Stop();
    }

    public class MatrixInsensitiveComparer : EqualityComparer<object>
    {
        public override bool Equals(object? x, object? y)
        {
            return string.Equals(x?.ToString(), y?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(object obj)
        {
            string value = obj.ToString() ?? "";
            return StringComparer.OrdinalIgnoreCase.GetHashCode(value);
        }
    }
}
