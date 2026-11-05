using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheResultSnapshotTests
{
    [Test]
    public void GetResult_DefaultsToUnknownMissShape()
    {
        CacheGetResult result = new CacheGetResult();

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Unknown));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.Loaded, Is.False);
        Assert.That(result.HasExpiration, Is.False);
        Assert.That(result.CreatedUtcTicks, Is.EqualTo(0L));
        Assert.That(result.LastAccessUtcTicks, Is.EqualTo(0L));
        Assert.That(result.LastWriteUtcTicks, Is.EqualTo(0L));
        Assert.That(result.ExpirationUtcTicks, Is.EqualTo(0L));
    }

    [Test]
    public void GetResult_PublicFieldsCanRepresentLoadedHit()
    {
        CacheGetResult result = new CacheGetResult();

        result.Found = true;
        result.Key = "key";
        result.Value = "value";
        result.Status = CacheResultStatus.Loaded;
        result.Loaded = true;
        result.HasExpiration = true;
        result.CreatedUtcTicks = 10L;
        result.LastAccessUtcTicks = 20L;
        result.LastWriteUtcTicks = 10L;
        result.ExpirationUtcTicks = 30L;

        Assert.That(result.Found, Is.True);
        Assert.That(result.Key, Is.EqualTo("key"));
        Assert.That(result.Value, Is.EqualTo("value"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(result.Loaded, Is.True);
        Assert.That(result.HasExpiration, Is.True);
        Assert.That(result.ExpirationUtcTicks, Is.EqualTo(30L));
    }

    [Test]
    public void WriteResult_DefaultsToUnknownNoWrite()
    {
        CacheWriteResult result = new CacheWriteResult();

        Assert.That(result.Written, Is.False);
        Assert.That(result.Replaced, Is.False);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.PreviousValue, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Unknown));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.Weight, Is.EqualTo(0L));
        Assert.That(result.PreviousWeight, Is.EqualTo(0L));
        Assert.That(result.EvictedCount, Is.EqualTo(0L));
    }

    [Test]
    public void RemoveResult_DefaultsToUnknownNoRemove()
    {
        CacheRemoveResult result = new CacheRemoveResult();

        Assert.That(result.Removed, Is.False);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Unknown));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.Weight, Is.EqualTo(0L));
    }

    [Test]
    public void MaintenanceResult_DefaultsToEmptyUnknownOperation()
    {
        CacheMaintenanceResult result = new CacheMaintenanceResult();

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Unknown));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.RemovedCount, Is.EqualTo(0L));
        Assert.That(result.ExpiredCount, Is.EqualTo(0L));
        Assert.That(result.EvictedCount, Is.EqualTo(0L));
        Assert.That(result.ClearedCount, Is.EqualTo(0L));
        Assert.That(result.CurrentCount, Is.EqualTo(0L));
        Assert.That(result.CurrentWeight, Is.EqualTo(0L));
    }

    [Test]
    public void PolicySnapshot_DefaultsToEmptyLruPolicy()
    {
        CachePolicySnapshot snapshot = new CachePolicySnapshot();

        Assert.That(snapshot.Name, Is.EqualTo(""));
        Assert.That(snapshot.HasDefaultAbsoluteExpiration, Is.False);
        Assert.That(snapshot.HasDefaultTimeToLive, Is.False);
        Assert.That(snapshot.HasDefaultSlidingExpiration, Is.False);
        Assert.That(snapshot.HasMaximumEntries, Is.False);
        Assert.That(snapshot.HasMaximumWeight, Is.False);
        Assert.That(snapshot.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(snapshot.DefaultPriority, Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(snapshot.RecordStats, Is.False);
        Assert.That(snapshot.HasCleanupInterval, Is.False);
        Assert.That(snapshot.CurrentCount, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(0L));
        Assert.That(snapshot.OldestKeys, Is.Empty);
        Assert.That(snapshot.HottestKeys, Is.Empty);
        Assert.That(snapshot.ExpiringSoonKeys, Is.Empty);
        Assert.That(snapshot.SnapshotUtcTicks, Is.EqualTo(0L));
    }

    [Test]
    public void CacheSnapshot_DefaultsToEmptyVisibleEntries()
    {
        CacheSnapshot snapshot = new CacheSnapshot();

        Assert.That(snapshot.Keys, Is.Empty);
        Assert.That(snapshot.Values, Is.Empty);
        Assert.That(snapshot.Count, Is.EqualTo(0L));
        Assert.That(snapshot.Weight, Is.EqualTo(0L));
        Assert.That(snapshot.SnapshotUtcTicks, Is.EqualTo(0L));
    }

    [Test]
    public void StatsSnapshot_DefaultsToZeroCountersAndRates()
    {
        CacheStatsSnapshot snapshot = new CacheStatsSnapshot();

        Assert.That(snapshot.Hits, Is.EqualTo(0L));
        Assert.That(snapshot.Misses, Is.EqualTo(0L));
        Assert.That(snapshot.Puts, Is.EqualTo(0L));
        Assert.That(snapshot.Updates, Is.EqualTo(0L));
        Assert.That(snapshot.Removes, Is.EqualTo(0L));
        Assert.That(snapshot.Expirations, Is.EqualTo(0L));
        Assert.That(snapshot.Evictions, Is.EqualTo(0L));
        Assert.That(snapshot.Clears, Is.EqualTo(0L));
        Assert.That(snapshot.LoadSuccesses, Is.EqualTo(0L));
        Assert.That(snapshot.LoadFailures, Is.EqualTo(0L));
        Assert.That(snapshot.TotalLoadDurationTicks, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentSize, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(0L));
        Assert.That(snapshot.HitRate, Is.EqualTo(0.0));
        Assert.That(snapshot.LoadSuccessRate, Is.EqualTo(0.0));
    }
}
