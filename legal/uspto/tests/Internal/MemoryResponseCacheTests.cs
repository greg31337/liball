using System.Collections.Concurrent;
using System.Text;
using uspto;

namespace uspto.Tests;

public class MemoryResponseCacheTests
{
    [Test]
    public void SuccessfulResponsesAreReturnedAsIndependentSnapshots()
    {
        MemoryResponseCache cache = CreateCache(4, 1024L, 60);
        DateTimeOffset now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        BufferedHttpResponse response = CreateResponse(200, "original");
        response.Metadata.Headers.Add("ETag", new string[] { "first" });

        bool stored = cache.Store("key", response, now, null);
        response.Text = "changed-after-store";
        response.Metadata.Headers["ETag"][0] = "changed-after-store";
        BufferedHttpResponse? first = cache.Get("key", now);
        Assert.That(first, Is.Not.Null);
        first!.Text = "changed-after-read";
        first.Metadata.Headers["ETag"][0] = "changed-after-read";
        BufferedHttpResponse? second = cache.Get("key", now);

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.True);
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Text, Is.EqualTo("original"));
            Assert.That(second.Metadata.Headers["ETag"], Is.EqualTo(new string[] { "first" }));
        });
    }

    [Test]
    public void EntriesExpireAtTheConfiguredTtlBoundary()
    {
        MemoryResponseCache cache = CreateCache(4, 1024L, 10);
        DateTimeOffset storedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        cache.Store("key", CreateResponse(200, "body"), storedAt, null);

        BufferedHttpResponse? beforeExpiry = cache.Get("key", storedAt.AddSeconds(9));
        BufferedHttpResponse? atExpiry = cache.Get("key", storedAt.AddSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(beforeExpiry, Is.Not.Null);
            Assert.That(atExpiry, Is.Null);
            Assert.That(cache.GetEntryCount(storedAt.AddSeconds(10)), Is.Zero);
        });
    }

    [Test]
    public void CapacityEvictsTheLeastRecentlyUsedEntry()
    {
        MemoryResponseCache cache = CreateCache(2, 1024L, 60);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store("first", CreateResponse(200, "first"), now, null);
        cache.Store("second", CreateResponse(200, "second"), now, null);
        Assert.That(cache.Get("first", now), Is.Not.Null);

        cache.Store("third", CreateResponse(200, "third"), now, null);

        Assert.Multiple(() =>
        {
            Assert.That(cache.Get("first", now), Is.Not.Null);
            Assert.That(cache.Get("second", now), Is.Null);
            Assert.That(cache.Get("third", now), Is.Not.Null);
            Assert.That(cache.GetEntryCount(now), Is.EqualTo(2));
        });
    }

    [Test]
    public void EntriesCanBeInvalidatedIndividuallyOrTogether()
    {
        MemoryResponseCache cache = CreateCache(4, 1024L, 60);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store("first", CreateResponse(200, "first"), now, null);
        cache.Store("second", CreateResponse(200, "second"), now, null);

        bool removed = cache.Invalidate("first");
        bool removedAgain = cache.Invalidate("first");
        cache.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(cache.Get("first", now), Is.Null);
            Assert.That(cache.Get("second", now), Is.Null);
            Assert.That(cache.GetEntryCount(now), Is.Zero);
        });
    }

    [Test]
    public void FailuresAndOversizedResponsesAreNotStored()
    {
        MemoryResponseCache cache = CreateCache(4, 4L, 60);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        bool failureStored = cache.Store("failure", CreateResponse(500, "bad"), now, null);
        bool declaredOversizeStored = cache.Store("declared", CreateResponse(200, "12345"), now, null);
        BufferedHttpResponse encodedOversize = CreateResponse(200, "12345");
        encodedOversize.BodyBytes = 1L;
        bool encodedOversizeStored = cache.Store("encoded", encodedOversize, now, null);

        Assert.Multiple(() =>
        {
            Assert.That(failureStored, Is.False);
            Assert.That(declaredOversizeStored, Is.False);
            Assert.That(encodedOversizeStored, Is.False);
            Assert.That(cache.GetEntryCount(now), Is.Zero);
        });
    }

    [Test]
    public void UnredactedCredentialMaterialIsNotStored()
    {
        MemoryResponseCache cache = CreateCache(8, 4096L, 60);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        BufferedHttpResponse structuredSecret = CreateResponse(200, "{\"api_key\":\"sensitive\"}");
        BufferedHttpResponse knownSecret = CreateResponse(200, "result contains caller-secret");
        BufferedHttpResponse rawSecret = CreateResponse(200, "safe");
        rawSecret.RawText = "token=raw-secret";
        rawSecret.HasRawText = true;
        rawSecret.RawTextBytes = rawSecret.RawText.Length;
        BufferedHttpResponse headerSecret = CreateResponse(200, "safe");
        headerSecret.Metadata.Headers.Add("Authorization", new string[] { "Bearer secret" });
        BufferedHttpResponse redacted = CreateResponse(200, "safe");
        redacted.Metadata.Headers.Add("Authorization", new string[] { SecretRedactor.Replacement });

        bool structuredStored = cache.Store("structured", structuredSecret, now, null);
        bool knownStored = cache.Store("known", knownSecret, now, new string[] { "caller-secret" });
        bool rawStored = cache.Store("raw", rawSecret, now, null);
        bool headerStored = cache.Store("header", headerSecret, now, null);
        bool redactedStored = cache.Store("redacted", redacted, now, null);

        Assert.Multiple(() =>
        {
            Assert.That(structuredStored, Is.False);
            Assert.That(knownStored, Is.False);
            Assert.That(rawStored, Is.False);
            Assert.That(headerStored, Is.False);
            Assert.That(redactedStored, Is.True);
            Assert.That(cache.GetEntryCount(now), Is.EqualTo(1));
        });
    }

    [Test]
    public void DisabledOrZeroLimitPoliciesDoNotStoreResponses()
    {
        CachePolicy disabledPolicy = new CachePolicy();
        disabledPolicy.Mode = CacheMode.Disabled;
        MemoryResponseCache disabled = new MemoryResponseCache(disabledPolicy);
        MemoryResponseCache zeroEntries = CreateCache(0, 1024L, 60);
        MemoryResponseCache zeroBytes = CreateCache(4, 0L, 60);
        MemoryResponseCache zeroLifetime = CreateCache(4, 1024L, 0);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        BufferedHttpResponse response = CreateResponse(200, "safe");

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Store("disabled", response, now, null), Is.False);
            Assert.That(zeroEntries.Store("entries", response, now, null), Is.False);
            Assert.That(zeroBytes.Store("bytes", response, now, null), Is.False);
            Assert.That(zeroLifetime.Store("lifetime", response, now, null), Is.False);
        });
    }

    [Test]
    public void ConcurrentReadsWritesAndInvalidationRemainBounded()
    {
        MemoryResponseCache cache = CreateCache(16, 1024L, 60);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ConcurrentQueue<Exception> failures = new ConcurrentQueue<Exception>();

        Parallel.For(0, 500, index =>
        {
            try
            {
                string key = "key-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                cache.Store(key, CreateResponse(200, key), now, null);
                cache.Get(key, now);
                if (index % 3 == 0)
                {
                    cache.Invalidate(key);
                }
            }
            catch (Exception exception)
            {
                failures.Enqueue(exception);
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.Empty);
            Assert.That(cache.GetEntryCount(now), Is.LessThanOrEqualTo(16));
        });
    }

    [Test]
    public void ConcurrentReplacementOfTheSameKeyReturnsACompleteSnapshot()
    {
        MemoryResponseCache cache = CreateCache(4, 1024L, 60);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ConcurrentQueue<Exception> failures = new ConcurrentQueue<Exception>();

        Parallel.For(0, 200, index =>
        {
            try
            {
                string value = "value-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                cache.Store("shared", CreateResponse(200, value), now, null);
                BufferedHttpResponse? snapshot = cache.Get("shared", now);
                if (snapshot == null || !snapshot.Text.StartsWith("value-", StringComparison.Ordinal) || snapshot.BodyBytes != Encoding.UTF8.GetByteCount(snapshot.Text))
                {
                    failures.Enqueue(new InvalidOperationException("The shared cache entry was not a complete snapshot."));
                }
            }
            catch (Exception exception)
            {
                failures.Enqueue(exception);
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.Empty);
            Assert.That(cache.GetEntryCount(now), Is.EqualTo(1));
            Assert.That(cache.Get("shared", now), Is.Not.Null);
        });
    }

    private static MemoryResponseCache CreateCache(int maximumEntries, long maximumBytes, int lifetimeSeconds)
    {
        CachePolicy policy = new CachePolicy();
        policy.Mode = CacheMode.Memory;
        policy.MaximumMemoryEntries = maximumEntries;
        policy.MaximumEntryBytes = maximumBytes;
        policy.LifetimeSeconds = lifetimeSeconds;
        return new MemoryResponseCache(policy);
    }

    private static BufferedHttpResponse CreateResponse(int statusCode, string text)
    {
        BufferedHttpResponse response = new BufferedHttpResponse();
        response.Metadata.StatusCode = statusCode;
        response.Metadata.ReasonPhrase = statusCode >= 200 && statusCode < 300 ? "OK" : "Failure";
        response.Metadata.RequestUri = new Uri("https://api.uspto.gov/resource");
        response.Metadata.MediaType = UsptoMediaTypes.Json;
        response.Metadata.Format = DocumentFormat.Json;
        response.ExpectedFormat = DocumentFormat.Json;
        response.Text = text;
        response.BodyBytes = Encoding.UTF8.GetByteCount(text);
        return response;
    }
}
