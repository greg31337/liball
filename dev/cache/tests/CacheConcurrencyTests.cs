using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheConcurrencyTests
{
    [Test]
    public void ConcurrentPutGetAndRemove_WithDistinctKeys_CompletesWithConsistentState()
    {
        Cache cache = new Cache(CacheOptions.Default());
        int workerCount = 6;
        int keysPerWorker = 40;
        int totalKeys = workerCount * keysPerWorker;
        Task[] tasks = new Task[workerCount];

        for (int worker = 0; worker < workerCount; worker++)
        {
            int workerIndex = worker;
            tasks[worker] = Task.Run(() =>
            {
                int start = workerIndex * keysPerWorker;
                int end = start + keysPerWorker;
                for (int key = start; key < end; key++)
                {
                    cache.Put(key, "value-" + key.ToString());
                }
            });
        }

        Assert.That(Task.WaitAll(tasks, 5000), Is.True);
        Assert.That(cache.Snapshot().Count, Is.EqualTo(totalKeys));

        int foundCount = 0;
        for (int worker = 0; worker < workerCount; worker++)
        {
            int workerIndex = worker;
            tasks[worker] = Task.Run(() =>
            {
                int start = workerIndex * keysPerWorker;
                int end = start + keysPerWorker;
                for (int key = start; key < end; key++)
                {
                    CacheGetResult result = cache.Get(key);
                    if (result.Found)
                    {
                        Interlocked.Increment(ref foundCount);
                    }
                }
            });
        }

        Assert.That(Task.WaitAll(tasks, 5000), Is.True);
        Assert.That(foundCount, Is.EqualTo(totalKeys));

        int removedCount = 0;
        for (int worker = 0; worker < workerCount; worker++)
        {
            int workerIndex = worker;
            tasks[worker] = Task.Run(() =>
            {
                int start = workerIndex * keysPerWorker;
                int end = start + keysPerWorker;
                for (int key = start; key < end; key++)
                {
                    CacheRemoveResult result = cache.Remove(key);
                    if (result.Removed)
                    {
                        Interlocked.Increment(ref removedCount);
                    }
                }
            });
        }

        Assert.That(Task.WaitAll(tasks, 5000), Is.True);
        Assert.That(removedCount, Is.EqualTo(totalKeys));
        Assert.That(cache.Snapshot().Count, Is.EqualTo(0L));
    }

    [Test]
    public void ConcurrentCleanup_RemovesExpiredEntriesOnceAndLeavesLiveEntries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        int expiredCount = 120;
        int liveCount = 80;

        for (int index = 0; index < expiredCount; index++)
        {
            cache.PutEntry("expired-" + index.ToString(), index, CacheEntryOptions.TimeToLive(5L));
        }

        for (int index = 0; index < liveCount; index++)
        {
            cache.PutEntry("live-" + index.ToString(), index, CacheEntryOptions.TimeToLive(50L));
        }

        clock.AdvanceTicks(10L);

        long removedTotal = 0L;
        Task[] cleanupTasks = new Task[8];
        for (int index = 0; index < cleanupTasks.Length; index++)
        {
            cleanupTasks[index] = Task.Run(() =>
            {
                CacheMaintenanceResult result = cache.Cleanup();
                Interlocked.Add(ref removedTotal, result.RemovedCount);
            });
        }

        Assert.That(Task.WaitAll(cleanupTasks, 5000), Is.True);
        CacheSnapshot snapshot = cache.Snapshot();

        Assert.That(removedTotal, Is.EqualTo(expiredCount));
        Assert.That(snapshot.Count, Is.EqualTo(liveCount));
        for (int index = 0; index < liveCount; index++)
        {
            Assert.That(cache.Get("live-" + index.ToString()).Found, Is.True);
        }
    }

    [Test]
    public void ConcurrentGetOrLoad_WithDistinctKeys_LoadsEachKeyOnceAndCachesResults()
    {
        int loadCount = 0;
        int keyCount = 80;
        CacheOptions options = CacheOptions.Default();
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            Thread.Sleep(1);
            return "loaded-" + key.ToString();
        };
        Cache cache = new Cache(options);
        Task<CacheGetResult>[] firstLoads = new Task<CacheGetResult>[keyCount];

        for (int index = 0; index < keyCount; index++)
        {
            int key = index;
            firstLoads[index] = Task.Run(() => cache.GetOrLoad(key));
        }

        Assert.That(Task.WaitAll(firstLoads, 5000), Is.True);
        for (int index = 0; index < keyCount; index++)
        {
            Assert.That(firstLoads[index].Result.Found, Is.True);
            Assert.That(firstLoads[index].Result.Loaded, Is.True);
            Assert.That(firstLoads[index].Result.Value, Is.EqualTo("loaded-" + index.ToString()));
        }

        Task<CacheGetResult>[] cachedReads = new Task<CacheGetResult>[keyCount];
        for (int index = 0; index < keyCount; index++)
        {
            int key = index;
            cachedReads[index] = Task.Run(() => cache.GetOrLoad(key));
        }

        Assert.That(Task.WaitAll(cachedReads, 5000), Is.True);
        for (int index = 0; index < keyCount; index++)
        {
            Assert.That(cachedReads[index].Result.Found, Is.True);
            Assert.That(cachedReads[index].Result.Loaded, Is.False);
            Assert.That(cachedReads[index].Result.Value, Is.EqualTo("loaded-" + index.ToString()));
        }

        Assert.That(loadCount, Is.EqualTo(keyCount));
        Assert.That(cache.Snapshot().Count, Is.EqualTo(keyCount));
    }
}
