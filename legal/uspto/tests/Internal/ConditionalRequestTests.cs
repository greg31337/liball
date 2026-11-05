using System.Text;
using uspto;

namespace uspto.Tests;

public class ConditionalRequestTests
{
    [Test]
    public void MaxAgeAccountsForResponseDateAndOverridesExpires()
    {
        DateTimeOffset storedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 10, TimeSpan.Zero);
        HttpResponseMetadata metadata = CreateMetadata(200);
        metadata.Headers.Add("Date", new string[] { "Thu, 01 Jan 2026 00:00:00 GMT" });
        metadata.Headers.Add("Cache-Control", new string[] { "public", "max-age=60" });
        metadata.Headers.Add("Expires", new string[] { "Thu, 01 Jan 2026 01:00:00 GMT" });

        CacheFreshness fresh = ConditionalRequest.Evaluate(metadata, 300, true, storedAt, storedAt.AddSeconds(49));
        CacheFreshness stale = ConditionalRequest.Evaluate(metadata, 300, true, storedAt, storedAt.AddSeconds(50));

        Assert.Multiple(() =>
        {
            Assert.That(fresh.FreshUntil, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero)));
            Assert.That(fresh.IsFresh, Is.True);
            Assert.That(stale.IsFresh, Is.False);
        });
    }

    [Test]
    public void ExpiresAndFallbackLifetimeControlFreshness()
    {
        DateTimeOffset storedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        HttpResponseMetadata expiring = CreateMetadata(200);
        expiring.Headers.Add("Expires", new string[] { "Thu, 01 Jan 2026 00:02:00 GMT" });
        HttpResponseMetadata fallback = CreateMetadata(200);

        CacheFreshness expiresFreshness = ConditionalRequest.Evaluate(expiring, 300, true, storedAt, storedAt.AddSeconds(119));
        CacheFreshness fallbackFreshness = ConditionalRequest.Evaluate(fallback, 30, true, storedAt, storedAt.AddSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(expiresFreshness.FreshUntil, Is.EqualTo(storedAt.AddMinutes(2)));
            Assert.That(expiresFreshness.IsFresh, Is.True);
            Assert.That(fallbackFreshness.FreshUntil, Is.EqualTo(storedAt.AddSeconds(30)));
            Assert.That(fallbackFreshness.IsFresh, Is.False);
        });
    }

    [Test]
    public void NoStoreAndNoCacheDirectivesAreHonoredUnlessDisabledByPolicy()
    {
        DateTimeOffset storedAt = DateTimeOffset.UtcNow;
        HttpResponseMetadata noStore = CreateMetadata(200);
        noStore.Headers.Add("Cache-Control", new string[] { "private, no-store, max-age=600" });
        HttpResponseMetadata noCache = CreateMetadata(200);
        noCache.Headers.Add("Cache-Control", new string[] { "no-cache, max-age=600" });

        CacheFreshness rejected = ConditionalRequest.Evaluate(noStore, 30, true, storedAt, storedAt);
        CacheFreshness revalidate = ConditionalRequest.Evaluate(noCache, 30, true, storedAt, storedAt);
        CacheFreshness ignored = ConditionalRequest.Evaluate(noStore, 30, false, storedAt, storedAt.AddSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.CanStore, Is.False);
            Assert.That(rejected.IsFresh, Is.False);
            Assert.That(revalidate.CanStore, Is.True);
            Assert.That(revalidate.IsFresh, Is.False);
            Assert.That(revalidate.FreshUntil, Is.EqualTo(storedAt));
            Assert.That(ignored.CanStore, Is.True);
            Assert.That(ignored.IsFresh, Is.True);
            Assert.That(ignored.FreshUntil, Is.EqualTo(storedAt.AddSeconds(30)));
        });
    }

    [Test]
    public void CachedValidatorsAreAppliedWithoutReplacingCallerConditions()
    {
        HttpResponseMetadata metadata = CreateMetadata(200);
        metadata.Headers.Add(UsptoHeaders.ETag, new string[] { "\"version-1\"" });
        metadata.Headers.Add(UsptoHeaders.LastModified, new string[] { "Wed, 31 Dec 2025 23:59:00 GMT" });
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");

        bool applied = ConditionalRequest.ApplyValidators(request, metadata);

        Assert.Multiple(() =>
        {
            Assert.That(applied, Is.True);
            Assert.That(request.Headers.GetValues(UsptoHeaders.IfNoneMatch).Single(), Is.EqualTo("\"version-1\""));
            Assert.That(request.Headers.GetValues(UsptoHeaders.IfModifiedSince).Single(), Is.EqualTo("Wed, 31 Dec 2025 23:59:00 GMT"));
        });

        using HttpRequestMessage callerCondition = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        callerCondition.Headers.TryAddWithoutValidation(UsptoHeaders.IfNoneMatch, "\"caller\"");
        ConditionalRequest.ApplyValidators(callerCondition, metadata);
        Assert.That(callerCondition.Headers.GetValues(UsptoHeaders.IfNoneMatch).Single(), Is.EqualTo("\"caller\""));
    }

    [Test]
    public void NotModifiedMergeKeepsBodyAndUpdatesEndToEndMetadata()
    {
        BufferedHttpResponse cached = CreateResponse("cached-body");
        cached.Metadata.Headers.Add(UsptoHeaders.ETag, new string[] { "\"version-1\"" });
        cached.Metadata.Headers.Add("Cache-Control", new string[] { "max-age=0" });
        cached.Metadata.Headers.Add("Connection", new string[] { "keep-alive" });
        HttpResponseMetadata notModified = CreateMetadata(304);
        notModified.RequestIdentifier = "request-304";
        notModified.Headers.Add(UsptoHeaders.ETag, new string[] { "\"version-1\"" });
        notModified.Headers.Add("Cache-Control", new string[] { "max-age=120" });
        notModified.Headers.Add("Expires", new string[] { "Thu, 01 Jan 2026 00:02:00 GMT" });
        notModified.Headers.Add("Connection", new string[] { "close" });

        BufferedHttpResponse merged = ConditionalRequest.MergeNotModified(cached, notModified);
        merged.Text = "caller-change";

        Assert.Multiple(() =>
        {
            Assert.That(merged.Metadata.StatusCode, Is.EqualTo(200));
            Assert.That(merged.BodyBytes, Is.EqualTo(Encoding.UTF8.GetByteCount("cached-body")));
            Assert.That(merged.Metadata.RequestIdentifier, Is.EqualTo("request-304"));
            Assert.That(merged.Metadata.Headers["Cache-Control"], Is.EqualTo(new string[] { "max-age=120" }));
            Assert.That(merged.Metadata.Headers["Expires"], Is.EqualTo(new string[] { "Thu, 01 Jan 2026 00:02:00 GMT" }));
            Assert.That(merged.Metadata.Headers["Connection"], Is.EqualTo(new string[] { "keep-alive" }));
            Assert.That(cached.Text, Is.EqualTo("cached-body"));
            Assert.That(cached.Metadata.Headers["Cache-Control"], Is.EqualTo(new string[] { "max-age=0" }));
        });
    }

    [Test]
    public void NotModifiedMergeRejectsMismatchedValidators()
    {
        BufferedHttpResponse cached = CreateResponse("cached-body");
        cached.Metadata.Headers.Add(UsptoHeaders.ETag, new string[] { "\"version-1\"" });
        HttpResponseMetadata notModified = CreateMetadata(304);
        notModified.Headers.Add(UsptoHeaders.ETag, new string[] { "\"version-2\"" });

        ValidationException exception = Assert.Throws<ValidationException>(() => ConditionalRequest.MergeNotModified(cached, notModified));

        Assert.That(exception.ErrorCode, Is.EqualTo("CACHE_VALIDATOR_MISMATCH"));
    }

    [Test]
    public void NotModifiedMergeCanValidateWithLastModifiedOnly()
    {
        BufferedHttpResponse cached = CreateResponse("cached-body");
        cached.Metadata.Headers.Add(UsptoHeaders.LastModified, new string[] { "Wed, 31 Dec 2025 23:59:00 GMT" });
        cached.Metadata.Headers.Add("Cache-Control", new string[] { "max-age=0" });
        HttpResponseMetadata notModified = CreateMetadata(304);
        notModified.Headers.Add(UsptoHeaders.LastModified, new string[] { "Wed, 31 Dec 2025 23:59:00 GMT" });
        notModified.Headers.Add("Cache-Control", new string[] { "max-age=120" });

        BufferedHttpResponse merged = ConditionalRequest.MergeNotModified(cached, notModified);

        Assert.Multiple(() =>
        {
            Assert.That(merged.Text, Is.EqualTo("cached-body"));
            Assert.That(merged.Metadata.StatusCode, Is.EqualTo(200));
            Assert.That(merged.Metadata.Headers[UsptoHeaders.LastModified], Is.EqualTo(new string[] { "Wed, 31 Dec 2025 23:59:00 GMT" }));
            Assert.That(merged.Metadata.Headers["Cache-Control"], Is.EqualTo(new string[] { "max-age=120" }));
        });
    }

    [Test]
    public void MemoryCacheHonorsServerFreshnessAndRetainsStaleValidators()
    {
        CachePolicy policy = new CachePolicy();
        policy.Mode = CacheMode.Memory;
        policy.LifetimeSeconds = 60;
        policy.MaximumMemoryEntries = 4;
        policy.MaximumEntryBytes = 1024L;
        MemoryResponseCache cache = new MemoryResponseCache(policy);
        DateTimeOffset storedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        BufferedHttpResponse response = CreateResponse("body");
        response.Metadata.Headers.Add("Cache-Control", new string[] { "max-age=2" });
        response.Metadata.Headers.Add(UsptoHeaders.ETag, new string[] { "\"version-1\"" });

        bool stored = cache.Store("key", response, storedAt, null);
        BufferedHttpResponse? fresh = cache.Get("key", storedAt.AddSeconds(1));
        BufferedHttpResponse? stale = cache.Get("key", storedAt.AddSeconds(2));
        BufferedHttpResponse? candidate = cache.GetForRevalidation("key");
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        bool applied = ConditionalRequest.ApplyValidators(request, candidate!.Metadata);

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.True);
            Assert.That(fresh, Is.Not.Null);
            Assert.That(stale, Is.Null);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(applied, Is.True);
            Assert.That(request.Headers.GetValues(UsptoHeaders.IfNoneMatch).Single(), Is.EqualTo("\"version-1\""));
        });
    }

    [Test]
    public void FileCacheHonorsNoStoreAndRetainsExpiredValidatorEntries()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-conditional-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            CachePolicy policy = new CachePolicy();
            policy.Mode = CacheMode.File;
            policy.Directory = directory;
            policy.LifetimeSeconds = 60;
            policy.MaximumEntryBytes = 1024L;
            FileResponseCache cache = new FileResponseCache(policy);
            DateTimeOffset storedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            BufferedHttpResponse noStore = CreateResponse("private");
            noStore.Metadata.Headers.Add("Cache-Control", new string[] { "no-store" });
            BufferedHttpResponse expired = CreateResponse("body");
            expired.Metadata.Headers.Add("Expires", new string[] { "Thu, 01 Jan 2026 00:00:01 GMT" });
            expired.Metadata.Headers.Add(UsptoHeaders.LastModified, new string[] { "Wed, 31 Dec 2025 23:59:00 GMT" });

            bool rejected = cache.Store("no-store", noStore, storedAt, null);
            bool stored = cache.Store("expired", expired, storedAt, null);
            BufferedHttpResponse? fresh = cache.Get("expired", storedAt);
            BufferedHttpResponse? stale = cache.Get("expired", storedAt.AddSeconds(1));
            BufferedHttpResponse? candidate = cache.GetForRevalidation("expired");

            Assert.Multiple(() =>
            {
                Assert.That(rejected, Is.False);
                Assert.That(stored, Is.True);
                Assert.That(fresh, Is.Not.Null);
                Assert.That(stale, Is.Null);
                Assert.That(candidate, Is.Not.Null);
                Assert.That(candidate!.Metadata.Headers[UsptoHeaders.LastModified], Is.EqualTo(new string[] { "Wed, 31 Dec 2025 23:59:00 GMT" }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static HttpResponseMetadata CreateMetadata(int statusCode)
    {
        HttpResponseMetadata metadata = new HttpResponseMetadata();
        metadata.StatusCode = statusCode;
        metadata.ReasonPhrase = statusCode == 304 ? "Not Modified" : "OK";
        metadata.RequestUri = new Uri("https://api.uspto.gov/resource");
        metadata.MediaType = UsptoMediaTypes.Json;
        metadata.Format = DocumentFormat.Json;
        return metadata;
    }

    private static BufferedHttpResponse CreateResponse(string text)
    {
        BufferedHttpResponse response = new BufferedHttpResponse();
        response.Metadata = CreateMetadata(200);
        response.ExpectedFormat = DocumentFormat.Json;
        response.Text = text;
        response.BodyBytes = Encoding.UTF8.GetByteCount(text);
        return response;
    }
}
