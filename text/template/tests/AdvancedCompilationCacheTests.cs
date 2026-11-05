using System.Collections.Concurrent;

namespace template.Tests;

[TestFixture]
public class AdvancedCompilationCacheTests
{
    [Test]
    public void Capacity_EvictsLeastRecentlyUsedEntriesAndCountsEveryOutcome()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        cache.SetCapacity(2);
        TemplateSource firstSource = Source("first", "v1", "h1", "t1", "First");
        TemplateSource secondSource = Source("second", "v1", "h2", "t2", "Second");
        TemplateSource thirdSource = Source("third", "v1", "h3", "t3", "Third");

        TemplateCompiledTemplate first = cache.GetOrCompile(firstSource, options);
        cache.GetOrCompile(secondSource, options);
        Assert.That(cache.GetOrCompile(firstSource, options), Is.SameAs(first));
        cache.GetOrCompile(thirdSource, options);

        Assert.That(cache.Count(), Is.EqualTo(2));
        Assert.That(cache.GetCapacity(), Is.EqualTo(2));
        Assert.That(cache.GetHitCount(), Is.EqualTo(1));
        Assert.That(cache.GetMissCount(), Is.EqualTo(3));
        Assert.That(cache.GetEvictionCount(), Is.EqualTo(1));

        cache.GetOrCompile(secondSource, options);
        Assert.That(cache.GetMissCount(), Is.EqualTo(4));
        Assert.That(cache.GetEvictionCount(), Is.EqualTo(2));
    }

    [Test]
    public void Invalidation_RemovesEntriesByIdentityVersionHashAndTimestamp()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        TemplateSource first = PathSource("card", "/templates/card", "v1", "h1", "t1", "One");
        TemplateSource second = PathSource("card", "/templates/card", "v2", "h2", "t2", "Two");
        TemplateSource third = PathSource("other", "/templates/other", "v1", "h3", "t3", "Three");
        TemplateSource fourth = PathSource("hash", "/templates/hash", "v3", "h1", "t4", "Four");
        TemplateSource fifth = PathSource("time", "/templates/time", "v4", "h4", "t1", "Five");
        cache.GetOrCompile(first, options);
        cache.GetOrCompile(second, options);
        cache.GetOrCompile(third, options);
        cache.GetOrCompile(fourth, options);
        cache.GetOrCompile(fifth, options);

        Assert.That(cache.InvalidateSource(first), Is.EqualTo(2));
        Assert.That(cache.InvalidateVersion("v1"), Is.EqualTo(1));
        Assert.That(cache.InvalidateContentHash("h1"), Is.EqualTo(1));
        Assert.That(cache.InvalidateTimestamp("t1"), Is.EqualTo(1));

        Assert.That(cache.Count(), Is.EqualTo(0));
        Assert.That(cache.GetInvalidationCount(), Is.EqualTo(5));
        Assert.That(cache.GetEvictionCount(), Is.EqualTo(0));
    }

    [Test]
    public void SourceTimestamp_ParticipatesInCacheIdentity()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        TemplateSource first = Source("card", "v1", "hash", "time-1", "Hello");
        TemplateSource second = Source("card", "v1", "hash", "time-2", "Hello");

        TemplateCompiledTemplate firstCompiled = cache.GetOrCompile(first, options);
        TemplateCompiledTemplate secondCompiled = cache.GetOrCompile(second, options);

        Assert.That(secondCompiled, Is.Not.SameAs(firstCompiled));
        Assert.That(cache.Count(), Is.EqualTo(2));

        TemplateOptions mustacheOptions = new TemplateOptions();
        mustacheOptions.SyntaxProfile = "mustache";
        TemplateSource convertedSource = Source("mustache", "v1", "mustache-hash", "mustache-time", "Hello {{name}}");
        TemplateCompiledTemplate converted = cache.GetOrCompile(convertedSource, mustacheOptions);
        Assert.That(converted.Document.Source.Timestamp, Is.EqualTo("mustache-time"));
    }

    [Test]
    public void GetOrCompile_CompilesOnceDuringConcurrentRequestsAndReturnsReusableResult()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        TemplateSource source = Source("shared", "v1", "hash", "time", "Hello {{ name }}");
        ConcurrentBag<TemplateCompiledTemplate> results = new ConcurrentBag<TemplateCompiledTemplate>();

        Parallel.For(0, 64, delegate(int index)
        {
            results.Add(cache.GetOrCompile(source, options));
        });

        TemplateCompiledTemplate? first = null;
        foreach (TemplateCompiledTemplate compiled in results)
        {
            if (first == null)
            {
                first = compiled;
            }
            else
            {
                Assert.That(compiled, Is.SameAs(first));
            }
        }

        Assert.That(cache.Count(), Is.EqualTo(1));
        Assert.That(cache.GetMissCount(), Is.EqualTo(1));
        Assert.That(cache.GetHitCount(), Is.EqualTo(63));
    }

    [Test]
    public void CompiledTemplate_DefensivelyCapturesSourceAndRendersConcurrently()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        TemplateSource source = Source("shared", "v1", "hash", "time", "Hello {{ name }}");
        TemplateCompiledTemplate compiled = cache.GetOrCompile(source, options);
        source.Text = "Changed";
        source.Version = "changed";
        ConcurrentBag<string> output = new ConcurrentBag<string>();
        TemplateRenderer renderer = new TemplateRenderer();

        Parallel.For(0, 32, delegate(int index)
        {
            Dictionary<string, TemplateValue> model = new Dictionary<string, TemplateValue>();
            model["name"] = TemplateValue.FromString(index.ToString());
            output.Add(renderer.RenderDocumentToStringAsync(compiled.Document, TemplateValue.FromMap(model)).GetAwaiter().GetResult());
        });

        Assert.That(compiled.Source.Text, Is.EqualTo("Hello {{ name }}"));
        Assert.That(compiled.Source.Version, Is.EqualTo("v1"));
        Assert.That(output.Count, Is.EqualTo(32));
    }

    [Test]
    public void Renderer_InjectsInterfaceCacheAndExposesPreparseAndPrecompileOperations()
    {
        TrackingCompilationCache cache = new TrackingCompilationCache();
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.SetCompilationCache(cache);

        TemplateParseResult parsed = renderer.Preparse("Hello {{ name }}");
        TemplateSource parsedSource = Source("parsed", "v1", "parse-hash", "parse-time", "Parsed");
        TemplateParseResult sourceParsed = renderer.PreparseSource(parsedSource);
        TemplateCompiledTemplate compiled = renderer.Precompile("Hello {{ name }}");
        string output = renderer.RenderToString("Hello {{ name }}", Model("Ada"));
        TemplateSource named = Source("named", "v1", "hash", "time", "Named");
        renderer.PrecompileSource(named);

        Assert.That(parsed.Document.Children.Count, Is.EqualTo(2));
        Assert.That(sourceParsed.Document.Source, Is.SameAs(parsedSource));
        Assert.That(compiled.Document.Children.Count, Is.EqualTo(2));
        Assert.That(output, Is.EqualTo("Hello Ada"));
        Assert.That(cache.GetOrCompileCallCount, Is.EqualTo(3));
        Assert.That(cache.Inner.GetMissCount(), Is.EqualTo(2));
        Assert.That(cache.Inner.GetHitCount(), Is.EqualTo(1));
    }

    [Test]
    public void Clear_ResetsEvictionAndInvalidationCountersWithExistingCounters()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateOptions options = new TemplateOptions();
        cache.SetCapacity(1);
        TemplateSource first = Source("first", "v1", "h1", "t1", "First");
        cache.GetOrCompile(first, options);
        cache.GetOrCompile(Source("second", "v2", "h2", "t2", "Second"), options);
        cache.InvalidateVersion("v2");

        cache.Clear();

        Assert.That(cache.Count(), Is.EqualTo(0));
        Assert.That(cache.GetHitCount(), Is.EqualTo(0));
        Assert.That(cache.GetMissCount(), Is.EqualTo(0));
        Assert.That(cache.GetEvictionCount(), Is.EqualTo(0));
        Assert.That(cache.GetInvalidationCount(), Is.EqualTo(0));
    }

    [Test]
    public void ConfigurationAndInvalidation_RejectInvalidArguments()
    {
        TemplateCompilationCache cache = new TemplateCompilationCache();

        Assert.Throws<ArgumentOutOfRangeException>(() => cache.SetCapacity(0));
        Assert.Throws<ArgumentNullException>(() => cache.InvalidateSource(null!));
        Assert.Throws<ArgumentNullException>(() => cache.InvalidateVersion(null!));
        Assert.Throws<ArgumentNullException>(() => cache.InvalidateContentHash(null!));
        Assert.Throws<ArgumentNullException>(() => cache.InvalidateTimestamp(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderer().SetCompilationCache(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderer().Preparse(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderer().PreparseSource(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderer().Precompile(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderer().PrecompileSource(null!));
    }

    static TemplateSource Source(string name, string version, string hash, string timestamp, string text)
    {
        TemplateSource source = new TemplateSource(name, null, version, hash, text);
        source.Timestamp = timestamp;
        return source;
    }

    static TemplateSource PathSource(string name, string path, string version, string hash, string timestamp, string text)
    {
        TemplateSource source = new TemplateSource(name, path, version, hash, text);
        source.Timestamp = timestamp;
        return source;
    }

    static TemplateValue Model(string name)
    {
        Dictionary<string, TemplateValue> model = new Dictionary<string, TemplateValue>();
        model["name"] = TemplateValue.FromString(name);
        return TemplateValue.FromMap(model);
    }
}

internal class TrackingCompilationCache : ITemplateCompilationCache
{
    internal TemplateCompilationCache Inner;
    internal int GetOrCompileCallCount;

    internal TrackingCompilationCache()
    {
        Inner = new TemplateCompilationCache();
        GetOrCompileCallCount = 0;
    }

    public void SetProfileRegistry(TemplateProfileRegistry profileRegistry)
    {
        Inner.SetProfileRegistry(profileRegistry);
    }

    public TemplateCompiledTemplate GetOrCompile(TemplateSource source, TemplateOptions options)
    {
        GetOrCompileCallCount = GetOrCompileCallCount + 1;
        return Inner.GetOrCompile(source, options);
    }

    public void SetCapacity(int capacity)
    {
        Inner.SetCapacity(capacity);
    }

    public int GetCapacity()
    {
        return Inner.GetCapacity();
    }

    public int InvalidateSource(TemplateSource source)
    {
        return Inner.InvalidateSource(source);
    }

    public int InvalidateVersion(string version)
    {
        return Inner.InvalidateVersion(version);
    }

    public int InvalidateContentHash(string contentHash)
    {
        return Inner.InvalidateContentHash(contentHash);
    }

    public int InvalidateTimestamp(string timestamp)
    {
        return Inner.InvalidateTimestamp(timestamp);
    }

    public void Clear()
    {
        Inner.Clear();
    }

    public int Count()
    {
        return Inner.Count();
    }

    public string CreateKey(TemplateSource source, TemplateOptions options)
    {
        return Inner.CreateKey(source, options);
    }

    public int GetHitCount()
    {
        return Inner.GetHitCount();
    }

    public int GetMissCount()
    {
        return Inner.GetMissCount();
    }

    public int GetEvictionCount()
    {
        return Inner.GetEvictionCount();
    }

    public int GetInvalidationCount()
    {
        return Inner.GetInvalidationCount();
    }
}
