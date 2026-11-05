using System.Text;
using uspto;

namespace uspto.Tests;

public class FileResponseCacheTests
{
    [Test]
    public void SuccessfulResponseAndMetadataRoundTrip()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 4096L, 60, false, 0L);
            DateTimeOffset now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            BufferedHttpResponse response = CreateResponse(200, "{\"result\":\"ok\"}");
            response.Metadata.ContentLength = response.BodyBytes;
            response.Metadata.HasContentLength = true;
            response.Metadata.CharacterSet = "utf-8";
            response.Metadata.RequestIdentifier = "request-123";
            response.Metadata.Headers.Add("ETag", new string[] { "\"version-1\"" });
            response.RawText = "{\"result\":\"ok\"}";
            response.RawTextBytes = response.BodyBytes;
            response.HasRawText = true;

            bool stored = cache.Store("round-trip", response, now, null);
            BufferedHttpResponse? loaded = cache.Get("round-trip", now.AddSeconds(1));

            Assert.Multiple(() =>
            {
                Assert.That(stored, Is.True);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.Text, Is.EqualTo(response.Text));
                Assert.That(loaded.BodyBytes, Is.EqualTo(response.BodyBytes));
                Assert.That(loaded.RawText, Is.EqualTo(response.RawText));
                Assert.That(loaded.HasRawText, Is.True);
                Assert.That(loaded.ExpectedFormat, Is.EqualTo(DocumentFormat.Json));
                Assert.That(loaded.Metadata.StatusCode, Is.EqualTo(200));
                Assert.That(loaded.Metadata.RequestUri, Is.EqualTo(response.Metadata.RequestUri));
                Assert.That(loaded.Metadata.ContentLength, Is.EqualTo(response.BodyBytes));
                Assert.That(loaded.Metadata.HasContentLength, Is.True);
                Assert.That(loaded.Metadata.RequestIdentifier, Is.EqualTo("request-123"));
                Assert.That(loaded.Metadata.Headers["ETag"], Is.EqualTo(new string[] { "\"version-1\"" }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void AtomicReplacementUsesSanitizedRelativePaths()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 4096L, 60, false, 0L);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string hostileKey = "../../token/CON";
            string relativePath = cache.GetRelativePath(hostileKey);
            string fullPath = Path.GetFullPath(Path.Combine(cache.CacheDirectory, relativePath));

            cache.Store(hostileKey, CreateResponse(200, "first"), now, null);
            cache.Store(hostileKey, CreateResponse(200, "second"), now.AddSeconds(1), null);
            BufferedHttpResponse? loaded = cache.Get(hostileKey, now.AddSeconds(2));

            Assert.Multiple(() =>
            {
                Assert.That(relativePath, Does.Not.Contain(".."));
                Assert.That(relativePath, Does.Not.Contain("token"));
                Assert.That(relativePath, Does.Not.Contain("CON"));
                Assert.That(fullPath.StartsWith(cache.CacheDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal), Is.True);
                Assert.That(File.Exists(fullPath), Is.True);
                Assert.That(Directory.GetFiles(cache.CacheDirectory, "*.tmp", SearchOption.AllDirectories), Is.Empty);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.Text, Is.EqualTo("second"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExpiredEntriesAreRemovedDuringReadsAndDirectoryRecovery()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 4096L, 5, false, 0L);
            DateTimeOffset now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            cache.Store("read-expired", CreateResponse(200, "first"), now, null);
            cache.Store("sweep-expired", CreateResponse(200, "second"), now, null);

            BufferedHttpResponse? expired = cache.Get("read-expired", now.AddSeconds(5));
            int remaining = cache.GetEntryCount(now.AddSeconds(5));

            Assert.Multiple(() =>
            {
                Assert.That(expired, Is.Null);
                Assert.That(remaining, Is.Zero);
                Assert.That(Directory.GetFiles(cache.CacheDirectory, "*.cache", SearchOption.AllDirectories), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CorruptAndPartialEntriesAreRemovedAsCacheMisses()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 4096L, 60, false, 0L);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            cache.Store("corrupt", CreateResponse(200, "body"), now, null);
            string entryPath = cache.GetEntryPath("corrupt");
            File.WriteAllBytes(entryPath, new byte[] { 1, 2, 3, 4 });
            File.WriteAllText(entryPath + ".tmp", "partial", Encoding.UTF8);

            BufferedHttpResponse? result = cache.Get("corrupt", now);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Null);
                Assert.That(File.Exists(entryPath), Is.False);
                Assert.That(File.Exists(entryPath + ".tmp"), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void InterruptedReplacementKeepsTheCommittedEntryReadable()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 4096L, 60, false, 0L);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            cache.Store("committed", CreateResponse(200, "complete"), now, null);
            string entryPath = cache.GetEntryPath("committed");
            File.WriteAllText(entryPath + ".tmp", "partial replacement", Encoding.UTF8);

            BufferedHttpResponse? result = cache.Get("committed", now);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Text, Is.EqualTo("complete"));
                Assert.That(File.Exists(entryPath), Is.True);
                Assert.That(File.Exists(entryPath + ".tmp"), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TotalSizeLimitEvictsTheOldestEntry()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            DateTimeOffset firstTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            FileResponseCache unlimited = CreateCache(directory, 4096L, 3600, false, 0L);
            unlimited.Store("first", CreateResponse(200, new string('a', 400)), firstTime, null);
            long firstLength = new FileInfo(unlimited.GetEntryPath("first")).Length;
            FileResponseCache limited = CreateCache(directory, 4096L, 3600, true, firstLength * 2L - 1L);

            bool stored = limited.Store("second", CreateResponse(200, new string('b', 400)), firstTime.AddMinutes(1), null);

            Assert.Multiple(() =>
            {
                Assert.That(stored, Is.True);
                Assert.That(limited.Get("first", firstTime.AddMinutes(2)), Is.Null);
                Assert.That(limited.Get("second", firstTime.AddMinutes(2)), Is.Not.Null);
                Assert.That(limited.GetEntryCount(firstTime.AddMinutes(2)), Is.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IndividualAndCompleteInvalidationRemovePersistedEntries()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 4096L, 60, false, 0L);
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
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FailureOversizeAndSecretBearingResponsesAreNotPersisted()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache cache = CreateCache(directory, 8L, 60, false, 0L);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            BufferedHttpResponse secret = CreateResponse(200, "caller-secret");

            bool failureStored = cache.Store("failure", CreateResponse(500, "failure"), now, null);
            bool oversizeStored = cache.Store("oversize", CreateResponse(200, "123456789"), now, null);
            bool secretStored = cache.Store("secret", secret, now, new string[] { "caller-secret" });

            Assert.Multiple(() =>
            {
                Assert.That(failureStored, Is.False);
                Assert.That(oversizeStored, Is.False);
                Assert.That(secretStored, Is.False);
                Assert.That(cache.GetEntryCount(now), Is.Zero);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CacheInstancesCoordinateConcurrentDirectoryWrites()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache first = CreateCache(directory, 4096L, 60, false, 0L);
            FileResponseCache second = CreateCache(directory, 4096L, 60, false, 0L);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Parallel.For(0, 80, index =>
            {
                FileResponseCache cache = index % 2 == 0 ? first : second;
                string key = "key-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(cache.Store(key, CreateResponse(200, key), now, null), Is.True);
            });

            Assert.Multiple(() =>
            {
                Assert.That(first.GetEntryCount(now), Is.EqualTo(80));
                Assert.That(Directory.GetFiles(first.CacheDirectory, "*.tmp", SearchOption.AllDirectories), Is.Empty);
                Assert.That(first.Get("key-0", now), Is.Not.Null);
                Assert.That(second.Get("key-79", now), Is.Not.Null);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CacheInstancesAtomicallyReplaceTheSameEntry()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            FileResponseCache first = CreateCache(directory, 4096L, 60, false, 0L);
            FileResponseCache second = CreateCache(directory, 4096L, 60, false, 0L);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Parallel.For(0, 80, index =>
            {
                FileResponseCache cache = index % 2 == 0 ? first : second;
                string value = "value-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(cache.Store("shared", CreateResponse(200, value), now, null), Is.True);
            });

            BufferedHttpResponse? loaded = first.Get("shared", now);

            Assert.Multiple(() =>
            {
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.Text, Does.StartWith("value-"));
                Assert.That(loaded.BodyBytes, Is.EqualTo(Encoding.UTF8.GetByteCount(loaded.Text)));
                Assert.That(first.GetEntryCount(now), Is.EqualTo(1));
                Assert.That(Directory.GetFiles(first.CacheDirectory, "*.tmp", SearchOption.AllDirectories), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static FileResponseCache CreateCache(string directory, long maximumEntryBytes, int lifetimeSeconds, bool hasMaximumFileBytes, long maximumFileBytes)
    {
        CachePolicy policy = new CachePolicy();
        policy.Mode = CacheMode.File;
        policy.Directory = directory;
        policy.MaximumEntryBytes = maximumEntryBytes;
        policy.LifetimeSeconds = lifetimeSeconds;
        policy.HasMaximumFileBytes = hasMaximumFileBytes;
        policy.MaximumFileBytes = maximumFileBytes;
        return new FileResponseCache(policy);
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

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-file-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
