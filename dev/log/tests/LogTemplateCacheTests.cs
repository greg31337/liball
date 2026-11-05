using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateCacheTests
{
    [Test]
    public void GetOrAdd_ParsesTemplateAndCachesResult()
    {
        LogTemplateCache cache = new LogTemplateCache(4);

        LogTemplate first = cache.GetOrAdd("Hello {User}");
        LogTemplate second = cache.GetOrAdd("Hello {User}");

        Assert.That(first, Is.SameAs(second));
        Assert.That(first.PlaceholderCount, Is.EqualTo(1));
        Assert.That(cache.Count, Is.EqualTo(1));
        Assert.That(cache.MissCount, Is.EqualTo(1));
        Assert.That(cache.HitCount, Is.EqualTo(1));
        Assert.That(cache.EvictionCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOrAdd_EvictsLeastRecentlyUsedTemplateWhenCapacityIsExceeded()
    {
        LogTemplateCache cache = new LogTemplateCache(2);

        cache.GetOrAdd("A {Value}");
        cache.GetOrAdd("B {Value}");
        cache.GetOrAdd("A {Value}");
        cache.GetOrAdd("C {Value}");

        Assert.That(cache.Count, Is.EqualTo(2));
        Assert.That(cache.Contains("A {Value}"), Is.True);
        Assert.That(cache.Contains("B {Value}"), Is.False);
        Assert.That(cache.Contains("C {Value}"), Is.True);
        Assert.That(cache.EvictionCount, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_NormalizesCapacityToAtLeastOne()
    {
        LogTemplateCache cache = new LogTemplateCache(0);

        cache.GetOrAdd("A");
        cache.GetOrAdd("B");

        Assert.That(cache.Capacity, Is.EqualTo(1));
        Assert.That(cache.Count, Is.EqualTo(1));
        Assert.That(cache.Contains("A"), Is.False);
        Assert.That(cache.Contains("B"), Is.True);
    }

    [Test]
    public void GetOrAdd_NormalizesNullTemplateToEmptyStringKey()
    {
        LogTemplateCache cache = new LogTemplateCache(2);

        LogTemplate first = cache.GetOrAdd(null);
        LogTemplate second = cache.GetOrAdd("");

        Assert.That(first, Is.SameAs(second));
        Assert.That(first.OriginalText, Is.EqualTo(""));
        Assert.That(cache.Count, Is.EqualTo(1));
        Assert.That(cache.HitCount, Is.EqualTo(1));
        Assert.That(cache.MissCount, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RemovesEntriesWithoutResettingCounters()
    {
        LogTemplateCache cache = new LogTemplateCache(2);

        cache.GetOrAdd("A");
        cache.GetOrAdd("A");
        cache.Clear();

        Assert.That(cache.Count, Is.EqualTo(0));
        Assert.That(cache.Contains("A"), Is.False);
        Assert.That(cache.HitCount, Is.EqualTo(1));
        Assert.That(cache.MissCount, Is.EqualTo(1));
    }

    [Test]
    public void ResetCounters_ClearsCacheDiagnostics()
    {
        LogTemplateCache cache = new LogTemplateCache(1);

        cache.GetOrAdd("A");
        cache.GetOrAdd("B");
        cache.GetOrAdd("B");
        cache.ResetCounters();

        Assert.That(cache.HitCount, Is.EqualTo(0));
        Assert.That(cache.MissCount, Is.EqualTo(0));
        Assert.That(cache.EvictionCount, Is.EqualTo(0));
        Assert.That(cache.Count, Is.EqualTo(1));
    }
}
