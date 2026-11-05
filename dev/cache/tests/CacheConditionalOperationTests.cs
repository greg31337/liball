using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace cache.Tests;

[TestFixture]
public class CacheConditionalOperationTests
{
    [Test]
    public void Add_WhenKeyIsAbsent_WritesEntryAndEnforcesCapacity()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("old", "first");

        CacheWriteResult result = cache.Add("new", "second");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Written, Is.True);
        Assert.That(result.Replaced, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(result.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("old"), Is.False);
        Assert.That(cache.Contains("new"), Is.True);
        Assert.That(stats.Puts, Is.EqualTo(2L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("old"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
    }

    [Test]
    public void Add_WhenEqualLiveKeyExists_RejectsWithoutReplacingEntry()
    {
        Cache cache = new Cache(CacheOptions.Default());
        cache.Put("key", "original");

        CacheWriteResult result = cache.Add("key", "replacement");
        CacheGetResult stored = cache.Peek("key");

        Assert.That(result.Written, Is.False);
        Assert.That(result.Replaced, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(stored.Value, Is.EqualTo("original"));
        Assert.That(cache.Count(), Is.EqualTo(1L));
    }

    [Test]
    public void Add_WhenEqualEntryExpired_RemovesItAndWritesNewEntry()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.PutEntry("key", "expired", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);

        CacheWriteResult result = cache.Add("key", "live");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Written, Is.True);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(cache.Peek("key").Value, Is.EqualTo("live"));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(stats.Puts, Is.EqualTo(2L));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
    }

    [Test]
    public void Replace_WhenLiveEntryExists_ReplacesAndReturnsPreviousEntry()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("key", "original");

        CacheWriteResult result = cache.Replace("key", "replacement");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Written, Is.True);
        Assert.That(result.Replaced, Is.True);
        Assert.That(result.PreviousValue, Is.EqualTo("original"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(cache.Peek("key").Value, Is.EqualTo("replacement"));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
    }

    [Test]
    public void Replace_WhenEntryIsMissingOrExpired_DoesNotWrite()
    {
        CacheClock clock = CacheClock.Manual(10L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        Cache cache = new Cache(options);

        CacheWriteResult missing = cache.Replace("missing", "value");
        cache.PutEntry("expired", "old", CacheEntryOptions.TimeToLive(1L));
        clock.AdvanceTicks(1L);
        CacheWriteResult expired = cache.Replace("expired", "new");

        Assert.That(missing.Written, Is.False);
        Assert.That(missing.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(expired.Written, Is.False);
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(expired.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(cache.Count(), Is.EqualTo(0L));
    }

    [Test]
    public void Contains_DoesNotRefreshSlidingExpirationOrRecordLookupStats()
    {
        CacheClock clock = CacheClock.Manual(20L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        Cache cache = new Cache(options);
        cache.PutEntry("key", "value", CacheEntryOptions.SlidingExpiration(10L));
        clock.AdvanceTicks(5L);

        bool found = cache.Contains("key");
        clock.AdvanceTicks(5L);
        bool foundAtExpiration = cache.Contains("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(found, Is.True);
        Assert.That(foundAtExpiration, Is.False);
        Assert.That(stats.Hits, Is.EqualTo(0L));
        Assert.That(stats.Misses, Is.EqualTo(0L));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
    }

    [Test]
    public void Count_RemovesExpiredEntriesAndReturnsOnlyLiveEntries()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(30L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        ICache cache = new Cache(options);
        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(2L));
        cache.Put("live", "two");
        clock.AdvanceTicks(2L);

        long count = cache.Count();
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(count, Is.EqualTo(1L));
        Assert.That(cache.Contains("live"), Is.True);
        Assert.That(cache.Contains("expired"), Is.False);
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("expired"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
    }

    [Test]
    public void ConditionalOperations_RejectNullKeysWithoutMutation()
    {
        ICache cache = new Cache(CacheOptions.Default());

        CacheWriteResult add = cache.Add(null, "value");
        CacheWriteResult replace = cache.Replace(null, "value");

        Assert.That(add.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(add.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(replace.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(replace.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(cache.Contains(null), Is.False);
        Assert.That(cache.Count(), Is.EqualTo(0L));
    }

    [Test]
    public void AddAndReplace_AcceptNullValuesAsFoundEntries()
    {
        ICache cache = new Cache(CacheOptions.Default());

        CacheWriteResult added = cache.Add("added", null);
        cache.Put("replaced", "value");
        CacheWriteResult replaced = cache.Replace("replaced", null);
        CacheGetResult addedEntry = cache.Peek("added");
        CacheGetResult replacedEntry = cache.Peek("replaced");

        Assert.That(added.Written, Is.True);
        Assert.That(added.Value, Is.Null);
        Assert.That(addedEntry.Found, Is.True);
        Assert.That(addedEntry.Value, Is.Null);
        Assert.That(replaced.Written, Is.True);
        Assert.That(replaced.Replaced, Is.True);
        Assert.That(replaced.Value, Is.Null);
        Assert.That(replacedEntry.Found, Is.True);
        Assert.That(replacedEntry.Value, Is.Null);
        Assert.That(cache.Count(), Is.EqualTo(2L));
    }

    [Test]
    public void ConditionalOperations_UseConfiguredKeyComparer()
    {
        CacheOptions options = CacheOptions.Default();
        options.KeyComparer = new CaseInsensitiveObjectComparer();
        Cache cache = new Cache(options);

        CacheWriteResult added = cache.Add("Alpha", "one");
        CacheWriteResult duplicate = cache.Add("ALPHA", "two");
        CacheWriteResult replaced = cache.Replace("aLpHa", "three");

        Assert.That(added.Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(duplicate.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(replaced.Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(replaced.PreviousValue, Is.EqualTo("one"));
        Assert.That(cache.Contains("alpha"), Is.True);
        Assert.That(cache.Peek("ALPHA").Value, Is.EqualTo("three"));
        Assert.That(cache.Count(), Is.EqualTo(1L));
    }

    [Test]
    public void FailedConditionalWritesAndContains_DoNotRecordWriteOrLookupCountersOrNotifications()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Add("key", "value");

        CacheWriteResult duplicate = cache.Add("key", "other");
        CacheWriteResult missing = cache.Replace("missing", "other");
        bool present = cache.Contains("key");
        bool absent = cache.Contains("missing");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(duplicate.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(missing.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(present, Is.True);
        Assert.That(absent, Is.False);
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
        Assert.That(stats.Hits, Is.EqualTo(0L));
        Assert.That(stats.Misses, Is.EqualTo(0L));
        Assert.That(notifications, Is.Empty);
    }

    [Test]
    public void ConcurrentAdd_ForOneKey_WritesExactlyOnce()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        int operationCount = 32;
        Task<CacheWriteResult>[] tasks = new Task<CacheWriteResult>[operationCount];

        for (int index = 0; index < operationCount; index++)
        {
            int value = index;
            tasks[index] = Task.Run(() =>
            {
                start.Wait();
                return cache.Add("shared", value);
            });
        }

        start.Set();
        Assert.That(Task.WaitAll(tasks, 5000), Is.True);

        int writtenCount = 0;
        int rejectedCount = 0;
        for (int index = 0; index < tasks.Length; index++)
        {
            if (tasks[index].Result.Status == CacheResultStatus.Written)
            {
                writtenCount++;
            }

            if (tasks[index].Result.Status == CacheResultStatus.Rejected)
            {
                rejectedCount++;
            }
        }

        CacheStatsSnapshot stats = cache.Stats();
        Assert.That(writtenCount, Is.EqualTo(1));
        Assert.That(rejectedCount, Is.EqualTo(operationCount - 1));
        Assert.That(cache.Contains("shared"), Is.True);
        Assert.That(cache.Count(), Is.EqualTo(1L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
    }

    [Test]
    public void ConcurrentReplace_ForOneLiveKey_PreservesOneEntryAndCompletesEveryReplacement()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        cache.Add("shared", "initial");
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        int operationCount = 32;
        Task<CacheWriteResult>[] tasks = new Task<CacheWriteResult>[operationCount];

        for (int index = 0; index < operationCount; index++)
        {
            int value = index;
            tasks[index] = Task.Run(() =>
            {
                start.Wait();
                return cache.Replace("shared", value);
            });
        }

        start.Set();
        Assert.That(Task.WaitAll(tasks, 5000), Is.True);

        for (int index = 0; index < tasks.Length; index++)
        {
            Assert.That(tasks[index].Result.Written, Is.True);
            Assert.That(tasks[index].Result.Replaced, Is.True);
            Assert.That(tasks[index].Result.Status, Is.EqualTo(CacheResultStatus.Replaced));
        }

        CacheStatsSnapshot stats = cache.Stats();
        Assert.That(cache.Contains("shared"), Is.True);
        Assert.That(cache.Count(), Is.EqualTo(1L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(operationCount));
    }

    public class CaseInsensitiveObjectComparer : EqualityComparer<object>
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
