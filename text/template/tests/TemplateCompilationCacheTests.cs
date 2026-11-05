using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateCompilationCacheTests
{
    [Test]
    public void GetOrCompile_ReusesCompiledTemplateForSameSourceIdentityAndOptions()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        TemplateSource source = new TemplateSource("card", null, "v1", null, "Hello {{ name }}");

        TemplateCompiledTemplate first = cache.GetOrCompile(source, options);
        TemplateCompiledTemplate second = cache.GetOrCompile(source, options);

        Assert.That(second, Is.SameAs(first));
        Assert.That(cache.Count(), Is.EqualTo(1));
        Assert.That(cache.MissCount, Is.EqualTo(1));
        Assert.That(cache.HitCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrCompile_SeparatesSourceIdentityAndOptionKeys()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions firstOptions = new TemplateOptions();
        TemplateOptions secondOptions = new TemplateOptions();
        secondOptions.TrimFinalNewline = true;
        TemplateSource firstSource = new TemplateSource("card", null, "v1", null, "Hello");
        TemplateSource secondSource = new TemplateSource("card", null, "v2", null, "Hello");

        TemplateCompiledTemplate first = cache.GetOrCompile(firstSource, firstOptions);
        TemplateCompiledTemplate differentOptions = cache.GetOrCompile(firstSource, secondOptions);
        TemplateCompiledTemplate differentVersion = cache.GetOrCompile(secondSource, firstOptions);

        Assert.That(differentOptions, Is.Not.SameAs(first));
        Assert.That(differentVersion, Is.Not.SameAs(first));
        Assert.That(cache.Count(), Is.EqualTo(3));
        Assert.That(cache.MissCount, Is.EqualTo(3));
        Assert.That(cache.HitCount, Is.EqualTo(0));
    }

    [Test]
    public void Clear_RemovesEntriesAndResetsCounters()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        cache.GetOrCompile(TemplateSource.FromString("Hello"), new TemplateOptions());
        cache.GetOrCompile(TemplateSource.FromString("Hello"), new TemplateOptions());

        cache.Clear();

        Assert.That(cache.Count(), Is.EqualTo(0));
        Assert.That(cache.MissCount, Is.EqualTo(0));
        Assert.That(cache.HitCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOrCompile_SaturatesStatisticsAtMaximumInteger()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateSource source = TemplateSource.FromString("Hello");
        TemplateOptions options = new TemplateOptions();
        cache.MissCount = 2147483647;

        cache.GetOrCompile(source, options);
        cache.HitCount = 2147483647;
        cache.GetOrCompile(source, options);
        cache.SetCapacity(1);
        cache.EvictionCount = 2147483647;
        TemplateSource second = new TemplateSource("second", null, "v2", null, "Second");
        cache.GetOrCompile(second, options);
        cache.InvalidationCount = 2147483647;
        cache.InvalidateVersion("v2");

        Assert.That(cache.MissCount, Is.EqualTo(2147483647));
        Assert.That(cache.HitCount, Is.EqualTo(2147483647));
        Assert.That(cache.EvictionCount, Is.EqualTo(2147483647));
        Assert.That(cache.InvalidationCount, Is.EqualTo(2147483647));
    }

    [Test]
    public void ConstructorsAndMethods_RejectNullRequiredValues()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateSource source = TemplateSource.FromString("x");
        TemplateParseResult parseResult = TemplateParser.ParseNative("x");

        Assert.Throws<ArgumentNullException>(() => new TemplateCompiledTemplate(null!, parseResult));
        Assert.Throws<ArgumentNullException>(() => new TemplateCompiledTemplate(source, null!));
        Assert.Throws<ArgumentNullException>(() => cache.GetOrCompile(null!, new TemplateOptions()));
        Assert.Throws<ArgumentNullException>(() => cache.GetOrCompile(source, null!));
        Assert.Throws<ArgumentNullException>(() => cache.CreateKey(null!, new TemplateOptions()));
        Assert.Throws<ArgumentNullException>(() => cache.CreateKey(source, null!));
    }
}
