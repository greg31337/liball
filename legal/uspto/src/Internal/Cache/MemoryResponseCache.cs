namespace uspto;

internal class MemoryResponseCacheEntry
{
    internal string Key = string.Empty;
    internal BufferedHttpResponse Response = new BufferedHttpResponse();
    internal global::System.DateTimeOffset ExpiresAt = default(global::System.DateTimeOffset);
    internal long AccessSequence = 0L;
}

internal class MemoryResponseCache
{
    internal object Sync = new object();
    internal global::System.Collections.Generic.Dictionary<string, MemoryResponseCacheEntry> Entries = new global::System.Collections.Generic.Dictionary<string, MemoryResponseCacheEntry>(global::System.StringComparer.Ordinal);
    internal bool Enabled = false;
    internal int LifetimeSeconds = 0;
    internal int MaximumEntries = 0;
    internal long MaximumEntryBytes = 0L;
    internal bool HonorHttpCaching = true;
    internal long NextAccessSequence = 0L;

    internal MemoryResponseCache(CachePolicy policy)
    {
        if (policy == null)
        {
            throw new ValidationException("A cache policy is required.", "policy", "REQUIRED", null);
        }

        if (policy.LifetimeSeconds < 0)
        {
            throw new ValidationException("The memory-cache lifetime cannot be negative.", "LifetimeSeconds", "INVALID_CACHE_POLICY", null);
        }

        if (policy.MaximumMemoryEntries < 0)
        {
            throw new ValidationException("The memory-cache entry limit cannot be negative.", "MaximumMemoryEntries", "INVALID_CACHE_POLICY", null);
        }

        if (policy.MaximumEntryBytes < 0L)
        {
            throw new ValidationException("The memory-cache response-size limit cannot be negative.", "MaximumEntryBytes", "INVALID_CACHE_POLICY", null);
        }

        Enabled = policy.Mode == CacheMode.Memory || policy.Mode == CacheMode.MemoryAndFile;
        LifetimeSeconds = policy.LifetimeSeconds;
        MaximumEntries = policy.MaximumMemoryEntries;
        MaximumEntryBytes = policy.MaximumEntryBytes;
        HonorHttpCaching = policy.HonorHttpCaching;
    }

    internal BufferedHttpResponse? Get(string key, global::System.DateTimeOffset now)
    {
        ValidateKey(key);
        if (!Enabled)
        {
            return null;
        }

        lock (Sync)
        {
            if (!Entries.ContainsKey(key))
            {
                return null;
            }

            MemoryResponseCacheEntry entry = Entries[key];
            if (entry.ExpiresAt <= now)
            {
                return null;
            }

            NextAccessSequence += 1L;
            entry.AccessSequence = NextAccessSequence;
            return CloneResponse(entry.Response);
        }
    }

    internal bool Store(string key, BufferedHttpResponse response, global::System.DateTimeOffset now, string[]? credentialValues)
    {
        ValidateKey(key);
        if (response == null)
        {
            throw new ValidationException("A buffered response is required for memory caching.", "response", "REQUIRED", null);
        }

        if (!IsEligible(response, credentialValues))
        {
            return false;
        }

        lock (Sync)
        {
            RemoveExpired(now);
            if (!Entries.ContainsKey(key) && Entries.Count >= MaximumEntries)
            {
                RemoveLeastRecentlyUsed();
            }

            NextAccessSequence += 1L;
            MemoryResponseCacheEntry entry = new MemoryResponseCacheEntry();
            entry.Key = key;
            entry.Response = CloneResponse(response);
            entry.ExpiresAt = CalculateExpiration(now, response.Metadata);
            entry.AccessSequence = NextAccessSequence;
            Entries[key] = entry;
            return true;
        }
    }

    internal bool Invalidate(string key)
    {
        ValidateKey(key);
        lock (Sync)
        {
            return Entries.Remove(key);
        }
    }

    internal BufferedHttpResponse? GetForRevalidation(string key)
    {
        ValidateKey(key);
        if (!Enabled)
        {
            return null;
        }

        lock (Sync)
        {
            if (!Entries.ContainsKey(key))
            {
                return null;
            }

            return CloneResponse(Entries[key].Response);
        }
    }

    internal void Clear()
    {
        lock (Sync)
        {
            Entries.Clear();
        }
    }

    internal int GetEntryCount(global::System.DateTimeOffset now)
    {
        lock (Sync)
        {
            RemoveExpired(now);
            return Entries.Count;
        }
    }

    internal void RemoveExpired(global::System.DateTimeOffset now)
    {
        global::System.Collections.Generic.List<string> expiredKeys = new global::System.Collections.Generic.List<string>();
        foreach (global::System.Collections.Generic.KeyValuePair<string, MemoryResponseCacheEntry> pair in Entries)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                expiredKeys.Add(pair.Key);
            }
        }

        foreach (string expiredKey in expiredKeys)
        {
            Entries.Remove(expiredKey);
        }
    }

    internal void RemoveLeastRecentlyUsed()
    {
        string? oldestKey = null;
        long oldestSequence = global::System.Int64.MaxValue;
        foreach (global::System.Collections.Generic.KeyValuePair<string, MemoryResponseCacheEntry> pair in Entries)
        {
            if (pair.Value.AccessSequence < oldestSequence)
            {
                oldestKey = pair.Key;
                oldestSequence = pair.Value.AccessSequence;
            }
        }

        if (oldestKey != null)
        {
            Entries.Remove(oldestKey);
        }
    }

    internal bool IsEligible(BufferedHttpResponse response, string[]? credentialValues)
    {
        if (!Enabled || LifetimeSeconds == 0 || MaximumEntries == 0 || MaximumEntryBytes == 0L)
        {
            return false;
        }

        if (response.Metadata.StatusCode < 200 || response.Metadata.StatusCode >= 300)
        {
            return false;
        }

        if (response.BodyBytes < 0L || response.BodyBytes > MaximumEntryBytes)
        {
            return false;
        }

        long encodedLength = global::System.Text.Encoding.UTF8.GetByteCount(response.Text);
        if (encodedLength > MaximumEntryBytes)
        {
            return false;
        }

        CacheFreshness freshness = ConditionalRequest.Evaluate(response.Metadata, LifetimeSeconds, HonorHttpCaching, global::System.DateTimeOffset.MinValue, global::System.DateTimeOffset.MinValue);
        if (!freshness.CanStore)
        {
            return false;
        }

        return !IsSecretBearing(response, credentialValues);
    }

    internal static bool IsSecretBearing(BufferedHttpResponse response, string[]? credentialValues)
    {
        if (ContainsSecret(response.Text, credentialValues) || ContainsSecret(response.RawText, credentialValues))
        {
            return true;
        }

        HttpResponseMetadata metadata = response.Metadata;
        if (ContainsSecret(metadata.ReasonPhrase, credentialValues) || ContainsSecret(metadata.MediaType, credentialValues) || ContainsSecret(metadata.CharacterSet, credentialValues) || ContainsSecret(metadata.RequestIdentifier, credentialValues))
        {
            return true;
        }

        if (metadata.RequestUri != null)
        {
            global::System.Uri safeUri = SecretRedactor.RedactUri(metadata.RequestUri, credentialValues);
            if (!safeUri.Equals(metadata.RequestUri))
            {
                return true;
            }
        }

        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in metadata.Headers)
        {
            foreach (string value in header.Value)
            {
                if (SecretRedactor.IsSensitiveName(header.Key))
                {
                    if (value != SecretRedactor.Replacement)
                    {
                        return true;
                    }
                }
                else if (ContainsSecret(value, credentialValues))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool ContainsSecret(string? value, string[]? credentialValues)
    {
        if (value == null)
        {
            return false;
        }

        string? safeValue = SecretRedactor.RedactText(value, credentialValues);
        return !global::System.String.Equals(value, safeValue, global::System.StringComparison.Ordinal);
    }

    internal global::System.DateTimeOffset CalculateExpiration(global::System.DateTimeOffset now, HttpResponseMetadata metadata)
    {
        return ConditionalRequest.Evaluate(metadata, LifetimeSeconds, HonorHttpCaching, now, now).FreshUntil;
    }

    internal static BufferedHttpResponse CloneResponse(BufferedHttpResponse source)
    {
        BufferedHttpResponse copy = new BufferedHttpResponse();
        copy.Metadata = CloneMetadata(source.Metadata);
        copy.ExpectedFormat = source.ExpectedFormat;
        copy.Text = source.Text;
        copy.BodyBytes = source.BodyBytes;
        copy.RawText = source.RawText;
        copy.RawTextBytes = source.RawTextBytes;
        copy.HasRawText = source.HasRawText;
        copy.IsRawTextTruncated = source.IsRawTextTruncated;
        return copy;
    }

    internal static HttpResponseMetadata CloneMetadata(HttpResponseMetadata source)
    {
        HttpResponseMetadata copy = new HttpResponseMetadata();
        copy.StatusCode = source.StatusCode;
        copy.ReasonPhrase = source.ReasonPhrase;
        copy.RequestUri = source.RequestUri;
        copy.MediaType = source.MediaType;
        copy.CharacterSet = source.CharacterSet;
        copy.Format = source.Format;
        copy.ContentLength = source.ContentLength;
        copy.HasContentLength = source.HasContentLength;
        copy.RequestIdentifier = source.RequestIdentifier;
        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in source.Headers)
        {
            string[] values = new string[header.Value.Length];
            global::System.Array.Copy(header.Value, values, header.Value.Length);
            copy.Headers.Add(header.Key, values);
        }

        return copy;
    }

    internal static void ValidateKey(string key)
    {
        if (global::System.String.IsNullOrWhiteSpace(key))
        {
            throw new ValidationException("A memory-cache key is required.", "key", "REQUIRED", null);
        }
    }
}
