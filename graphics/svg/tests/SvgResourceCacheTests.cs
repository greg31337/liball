namespace svg.Tests;

public class SvgResourceCacheTests
{
    [Test]
    public void CacheNormalizesAliasesReturnsSnapshotsAndEvictsLeastRecentlyUsedEntries()
    {
        SvgResourceCache cache = new SvgResourceCache(2, 8);
        SvgResourceAcquisitionResult first = Acquire("https://example.test/a.bin#first", "aaa", "https://cdn.example.test/a.bin#loaded");
        SvgResourceAcquisitionResult second = Acquire("https://example.test/b.bin", "bbb", "");
        SvgResourceAcquisitionResult third = Acquire("https://example.test/c.bin", "ccc", "");
        SvgResourceAcquisitionResult oversized = Acquire("https://example.test/large.bin", "123456789", "");

        Assert.That(cache.Add("HTTPS://EXAMPLE.TEST/dir/../a.bin#authored", first), Is.True);
        SvgResourceCacheLookup alias = cache.Get("https://cdn.example.test/a.bin#another-fragment");
        alias.Entry.Bytes[0] = (byte)'!';
        SvgResourceCacheLookup independent = cache.Get("https://example.test/a.bin#different");
        Assert.That(cache.Add(second.NormalizedUri, second), Is.True);
        cache.Get(first.NormalizedUri);
        Assert.That(cache.Add(third.NormalizedUri, third), Is.True);
        SvgResourceCacheLookup evicted = cache.Get(second.NormalizedUri);
        bool acceptedOversized = cache.Add(oversized.NormalizedUri, oversized);
        SvgResourceCacheStatistics statistics = cache.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(alias.Found, Is.True);
            Assert.That(independent.Found, Is.True);
            Assert.That(independent.Entry.Bytes[0], Is.EqualTo((byte)'a'));
            Assert.That(evicted.Found, Is.False);
            Assert.That(acceptedOversized, Is.False);
            Assert.That(cache.Get(third.NormalizedUri).Found, Is.True);
            Assert.That(statistics.EntryCount, Is.EqualTo(2));
            Assert.That(statistics.ByteCount, Is.EqualTo(6));
            Assert.That(statistics.FailureEntryCount, Is.Zero);
            Assert.That(statistics.AdditionCount, Is.EqualTo(3));
            Assert.That(statistics.EvictionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ResolverDeduplicatesNormalizedUrisWithoutChargingCachedHitsAgain()
    {
        SvgResourceCache cache = new SvgResourceCache(4, 1024);
        int callbackCount = 0;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.Cache = cache;
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = System.Text.Encoding.UTF8.GetBytes("body {}");
            response.ActualMediaType = SvgMimeTypes.Css;
            response.Cacheable = true;
            return response;
        });
        SvgSourceUri parent = new SvgSourceUri("https://example.test/root.svg");
        SvgSourceSpan firstSpan = new SvgSourceSpan(parent, 2, 1, 3, 4, 1, 5);
        SvgResourceRequest firstRequest = Request("https://EXAMPLE.test/styles/../theme.css#one", firstSpan, 1);
        SvgResourceAcquisitionResult first = new SvgResourceResolver().Resolve(firstRequest, options);
        options.AlreadyLoadedCount = options.Limits.MaxCount;
        options.AlreadyLoadedBytes = options.Limits.MaxTotalBytes;
        SvgSourceSpan secondSpan = new SvgSourceSpan(parent, 8, 1, 9, 10, 1, 11);
        SvgResourceRequest secondRequest = Request("https://example.test/theme.css#two", secondSpan, 2);
        SvgResourceAcquisitionResult second = new SvgResourceResolver().Resolve(secondRequest, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.CacheHit, Is.False);
            Assert.That(first.RequestCount, Is.EqualTo(1));
            Assert.That(first.LoadedCount, Is.EqualTo(1));
            Assert.That(first.BudgetCount, Is.EqualTo(1));
            Assert.That(second.Success, Is.True);
            Assert.That(second.CacheHit, Is.True);
            Assert.That(second.FailureCacheHit, Is.False);
            Assert.That(second.RequestCount, Is.Zero);
            Assert.That(second.LoadedCount, Is.Zero);
            Assert.That(second.BudgetCount, Is.Zero);
            Assert.That(second.EffectiveMediaType, Is.EqualTo(SvgMimeTypes.Css));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(first.ResourceSource.Parent, Is.SameAs(parent));
            Assert.That(second.ResourceSource.Parent, Is.SameAs(parent));
            Assert.That(first.ResourceSource.ReferenceSpan, Is.SameAs(firstSpan));
            Assert.That(second.ResourceSource.ReferenceSpan, Is.SameAs(secondSpan));
            Assert.That(first.ResourceSource.ResourcePath, Is.Not.EqualTo(second.ResourceSource.ResourcePath));
        });
    }

    [Test]
    public void CachedFailuresAvoidRepeatedCallbacksAndRelocateDiagnostics()
    {
        SvgResourceCache cache = new SvgResourceCache(4, 1024);
        int callbackCount = 0;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.Cache = cache;
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            return null;
        });
        SvgSourceUri parent = new SvgSourceUri("memory:parent");
        SvgSourceSpan firstSpan = new SvgSourceSpan(parent, 1, 1, 2, 2, 1, 3);
        SvgSourceSpan secondSpan = new SvgSourceSpan(parent, 4, 1, 5, 5, 1, 6);
        SvgResourceAcquisitionResult first = new SvgResourceResolver().Resolve(Request("https://example.test/failing.css#one", firstSpan, 0), options);
        SvgResourceAcquisitionResult second = new SvgResourceResolver().Resolve(Request("https://EXAMPLE.test/failing.css#two", secondSpan, 1), options);
        SvgResourceCacheStatistics statistics = cache.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.CacheHit, Is.False);
            Assert.That(first.RequestCount, Is.EqualTo(1));
            Assert.That(first.BudgetCount, Is.EqualTo(1));
            Assert.That(first.Diagnostics.Single().Source, Is.SameAs(firstSpan));
            Assert.That(second.Success, Is.False);
            Assert.That(second.CacheHit, Is.True);
            Assert.That(second.FailureCacheHit, Is.True);
            Assert.That(second.RequestCount, Is.Zero);
            Assert.That(second.BudgetCount, Is.Zero);
            Assert.That(second.Diagnostics.Single().Source, Is.SameAs(secondSpan));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(statistics.FailureEntryCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void NormalizedReferenceStackCyclesStopBeforeCacheOrCallbackAccess()
    {
        bool invoked = false;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.Cache = new SvgResourceCache(4, 1024);
        options.ReferenceStack.Add("https://example.test/theme.css#active");
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            invoked = true;
            return null;
        });
        SvgSourceSpan source = SvgSourceSpan.Unknown(new SvgSourceUri("memory:parent"));
        SvgResourceRequest request = Request("https://EXAMPLE.test/css/../theme.css#reference", source, 0);

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(request, options);
        SvgResourceCacheStatistics statistics = options.Cache.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.ReferenceCycle));
            Assert.That(result.Diagnostics[0].Source, Is.SameAs(source));
            Assert.That(result.Diagnostics[0].RelatedSpans.Single(), Is.SameAs(source));
            Assert.That(result.BudgetCount, Is.Zero);
            Assert.That(invoked, Is.False);
            Assert.That(statistics.HitCount, Is.Zero);
            Assert.That(statistics.MissCount, Is.Zero);
        });
    }

    [Test]
    public void NewAcquisitionsRespectTheRemainingTotalCountBudget()
    {
        bool invoked = false;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.AlreadyLoadedCount = options.Limits.MaxCount;
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            invoked = true;
            return null;
        });

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(new SvgResourceRequest("memory:over-count", SvgMimeTypes.Svg, System.Threading.CancellationToken.None), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(result.BudgetCount, Is.Zero);
            Assert.That(result.RequestCount, Is.Zero);
            Assert.That(invoked, Is.False);
        });
    }

    [Test]
    public void StylesheetImportsShareCachedBytesButRetainDistinctNestedProvenance()
    {
        SvgDocument document = Load("<svg><style>@import './theme.css#one'; @import 'nested/../theme.css#two'; root {}</style></svg>", "https://example.test/root.svg");
        int callbackCount = 0;
        SvgLoadOptions options = new SvgLoadOptions();
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = System.Text.Encoding.UTF8.GetBytes("imported {}");
            response.ActualMediaType = SvgMimeTypes.Css;
            response.FinalUri = request.NormalizedUri;
            response.Cacheable = true;
            return response;
        });

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, null);
        SvgCollectedStyleSheet first = collection.StyleSheets[0];
        SvgCollectedStyleSheet second = collection.StyleSheets[1];
        SvgSourceUri rootSource = collection.StyleSheets[2].StyleSheet.Source.Source;

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.True);
            Assert.That(collection.StyleSheets, Has.Count.EqualTo(3));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(collection.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(collection.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(collection.Resources.EncodedBytes, Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount("imported {}")));
            Assert.That(first.StyleSheet.Source.Source.Parent, Is.SameAs(rootSource));
            Assert.That(second.StyleSheet.Source.Source.Parent, Is.SameAs(rootSource));
            Assert.That(first.StyleSheet.Source.Source.ReferenceSpan, Is.Not.SameAs(second.StyleSheet.Source.Source.ReferenceSpan));
            Assert.That(first.StyleSheet.Source.Source.ResourcePath, Is.Not.EqualTo(second.StyleSheet.Source.Source.ResourcePath));
        });
    }

    [Test]
    public void StylesheetFailureCachingReportsEachReferenceButCallsTheHostOnce()
    {
        SvgDocument document = Load("<svg><style>@import 'bad.css#one'; @import './bad.css#two';</style></svg>", "https://example.test/root.svg");
        int callbackCount = 0;
        SvgLoadOptions options = new SvgLoadOptions();
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            return null;
        });

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, null);

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.False);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(collection.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(collection.Resources.LoadedCount, Is.Zero);
            Assert.That(collection.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(collection.Diagnostics.All(value => value.Code == SvgDiagnosticCode.ResourceFailed), Is.True);
            Assert.That(collection.Diagnostics[0].Source, Is.Not.SameAs(collection.Diagnostics[1].Source));
            Assert.That(collection.Diagnostics[0].Source.StartOffset, Is.Not.EqualTo(collection.Diagnostics[1].Source.StartOffset));
        });
    }

    [Test]
    public void StylesheetAcquisitionsShareOneExactTotalByteBudget()
    {
        SvgDocument document = Load("<svg><style>@import 'first.css'; @import 'second.css';</style></svg>", "https://example.test/root.svg");
        byte[] firstBytes = System.Text.Encoding.UTF8.GetBytes("first {}");
        byte[] secondBytes = System.Text.Encoding.UTF8.GetBytes("second {}");
        int callbackCount = 0;
        SvgLoadOptions options = new SvgLoadOptions();
        options.Limits.Resources.MaxTotalBytes = firstBytes.Length;
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = callbackCount == 1 ? firstBytes : secondBytes;
            response.ActualMediaType = SvgMimeTypes.Css;
            return response;
        });

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, null);

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.False);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(collection.Resources.RequestCount, Is.EqualTo(2));
            Assert.That(collection.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(collection.Resources.EncodedBytes, Is.EqualTo(firstBytes.Length));
            Assert.That(collection.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void AliasReplacementRemovalAndClearKeepExactByteAndActivityAccounting()
    {
        SvgResourceCache cache = new SvgResourceCache(4, 32);
        SvgResourceAcquisitionResult first = Acquire("https://example.test/a.bin", "1234", "https://cdn.test/final.bin");
        SvgResourceAcquisitionResult replacement = Acquire("https://cdn.test/final.bin", "xy", "https://cdn.test/final.bin");
        SvgResourceAcquisitionResult second = Acquire("https://example.test/b.bin", "abc", "");
        SvgResourceAcquisitionResult third = Acquire("https://example.test/c.bin", "def", "");

        Assert.That(cache.Add(first.NormalizedUri, first), Is.True);
        Assert.That(cache.Add(replacement.NormalizedUri, replacement), Is.True);
        SvgResourceCacheLookup oldAlias = cache.Get(first.NormalizedUri);
        SvgResourceCacheLookup replacementHit = cache.Get(replacement.NormalizedUri);
        bool removed = cache.Remove(replacement.NormalizedUri);
        bool removedAgain = cache.Remove(replacement.NormalizedUri);
        SvgResourceCacheStatistics afterRemoval = cache.GetStatistics();
        Assert.That(cache.Add(second.NormalizedUri, second), Is.True);
        Assert.That(cache.Add(third.NormalizedUri, third), Is.True);
        int cleared = cache.Clear();
        SvgResourceCacheStatistics afterClear = cache.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(oldAlias.Found, Is.False);
            Assert.That(replacementHit.Found, Is.True);
            Assert.That(System.Text.Encoding.UTF8.GetString(replacementHit.Entry.Bytes), Is.EqualTo("xy"));
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(afterRemoval.EntryCount, Is.Zero);
            Assert.That(afterRemoval.ByteCount, Is.Zero);
            Assert.That(afterRemoval.AdditionCount, Is.EqualTo(2));
            Assert.That(afterRemoval.RemovalCount, Is.EqualTo(1));
            Assert.That(cleared, Is.EqualTo(2));
            Assert.That(afterClear.EntryCount, Is.Zero);
            Assert.That(afterClear.ByteCount, Is.Zero);
            Assert.That(afterClear.AdditionCount, Is.EqualTo(4));
            Assert.That(afterClear.RemovalCount, Is.EqualTo(3));
            Assert.That(afterClear.HitCount, Is.EqualTo(1));
            Assert.That(afterClear.MissCount, Is.EqualTo(1));
        });
    }

    static SvgResourceAcquisitionResult Acquire(string uri, string text, string finalUri)
    {
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = System.Text.Encoding.UTF8.GetBytes(text);
            response.FinalUri = finalUri;
            response.Cacheable = true;
            return response;
        });
        return new SvgResourceResolver().Resolve(new SvgResourceRequest(uri, "application/octet-stream", System.Threading.CancellationToken.None), options);
    }

    static SvgResourceRequest Request(string uri, SvgSourceSpan source, int resourceIndex)
    {
        SvgResourceRequest request = new SvgResourceRequest(uri, SvgMimeTypes.Css, System.Threading.CancellationToken.None);
        request.ResourceKind = SvgResourceKind.StyleSheet;
        request.Source = source;
        request.ResourceIndex = resourceIndex;
        return request;
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
