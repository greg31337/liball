using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheBasicTests
{
    [Test]
    public void BasicWorkflow_PutGetPeekRemoveAndClear()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheWriteResult firstWrite = cache.Put("a", "one");
        CacheGetResult firstGet = cache.Get("a");
        CacheGetResult firstPeek = cache.Peek("a");
        CacheRemoveResult remove = cache.Remove("a");
        CacheGetResult afterRemove = cache.Get("a");
        cache.Put("b", "two");
        cache.Put("c", "three");
        CacheMaintenanceResult clear = cache.Clear();
        CacheGetResult afterClear = cache.Get("b");

        Assert.That(firstWrite.Written, Is.True);
        Assert.That(firstWrite.Replaced, Is.False);
        Assert.That(firstGet.Found, Is.True);
        Assert.That(firstGet.Value, Is.EqualTo("one"));
        Assert.That(firstPeek.Found, Is.True);
        Assert.That(firstPeek.Value, Is.EqualTo("one"));
        Assert.That(remove.Removed, Is.True);
        Assert.That(remove.Value, Is.EqualTo("one"));
        Assert.That(afterRemove.Found, Is.False);
        Assert.That(afterRemove.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(clear.RemovedCount, Is.EqualTo(2L));
        Assert.That(clear.CurrentCount, Is.EqualTo(0L));
        Assert.That(afterClear.Found, Is.False);
        Assert.That(afterClear.Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void BasicWorkflow_ReplaceReturnsPreviousValueAndReadsLatestValue()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheWriteResult firstWrite = cache.Put("a", "one");
        CacheWriteResult replacement = cache.Put("a", "two");
        CacheGetResult read = cache.Get("a");

        Assert.That(firstWrite.Written, Is.True);
        Assert.That(replacement.Written, Is.True);
        Assert.That(replacement.Replaced, Is.True);
        Assert.That(replacement.PreviousValue, Is.EqualTo("one"));
        Assert.That(replacement.Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(read.Found, Is.True);
        Assert.That(read.Value, Is.EqualTo("two"));
    }

    [Test]
    public void BasicWorkflow_StatsSnapshotIsPointInTime()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        CacheStatsSnapshot before = cache.Stats();
        cache.Get("a");
        cache.Put("b", "two");
        cache.Clear();
        CacheStatsSnapshot after = cache.Stats();

        Assert.That(before.Puts, Is.EqualTo(1L));
        Assert.That(before.Hits, Is.EqualTo(0L));
        Assert.That(before.Clears, Is.EqualTo(0L));
        Assert.That(before.CurrentSize, Is.EqualTo(1L));
        Assert.That(after.Puts, Is.EqualTo(2L));
        Assert.That(after.Hits, Is.EqualTo(1L));
        Assert.That(after.Clears, Is.EqualTo(1L));
        Assert.That(after.CurrentSize, Is.EqualTo(0L));
    }
}
