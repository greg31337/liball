namespace uspto;

internal class CacheControlValues
{
    internal bool NoStore = false;
    internal bool NoCache = false;
    internal long MaximumAgeSeconds = 0L;
    internal bool HasMaximumAge = false;
}

internal class CacheDateValue
{
    internal global::System.DateTimeOffset Value = default(global::System.DateTimeOffset);
    internal bool HasValue = false;
}

internal class CacheFreshness
{
    internal bool CanStore = true;
    internal bool IsFresh = false;
    internal bool CanRevalidate = false;
    internal global::System.DateTimeOffset FreshUntil = default(global::System.DateTimeOffset);
    internal string? ETag = null;
    internal global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);
    internal bool HasLastModified = false;
}

internal static class ConditionalRequest
{
    internal static CacheFreshness Evaluate(HttpResponseMetadata metadata, int fallbackLifetimeSeconds, bool honorHttpCaching, global::System.DateTimeOffset storedAt, global::System.DateTimeOffset now)
    {
        if (metadata == null)
        {
            throw new ValidationException("Cached response metadata is required.", "metadata", "REQUIRED", null);
        }

        if (fallbackLifetimeSeconds < 0)
        {
            throw new ValidationException("The fallback cache lifetime cannot be negative.", "fallbackLifetimeSeconds", "INVALID_CACHE_POLICY", null);
        }

        CacheFreshness freshness = new CacheFreshness();
        freshness.FreshUntil = AddSeconds(storedAt, fallbackLifetimeSeconds);
        freshness.ETag = ReadFirstHeader(metadata, UsptoHeaders.ETag);
        CacheDateValue lastModified = ParseHttpDate(ReadFirstHeader(metadata, UsptoHeaders.LastModified));
        freshness.LastModified = lastModified.Value;
        freshness.HasLastModified = lastModified.HasValue;
        freshness.CanRevalidate = !global::System.String.IsNullOrWhiteSpace(freshness.ETag) || freshness.HasLastModified;
        if (honorHttpCaching)
        {
            CacheControlValues directives = ParseCacheControl(ReadCombinedHeader(metadata, "Cache-Control"));
            freshness.CanStore = !directives.NoStore;
            if (directives.NoCache)
            {
                freshness.FreshUntil = storedAt;
            }
            else if (directives.HasMaximumAge)
            {
                global::System.DateTimeOffset ageBase = storedAt;
                CacheDateValue responseDate = ParseHttpDate(ReadFirstHeader(metadata, "Date"));
                if (responseDate.HasValue && responseDate.Value <= storedAt)
                {
                    ageBase = responseDate.Value;
                }

                freshness.FreshUntil = AddSeconds(ageBase, directives.MaximumAgeSeconds);
            }
            else
            {
                string? expiresText = ReadFirstHeader(metadata, "Expires");
                CacheDateValue expires = ParseHttpDate(expiresText);
                if (expires.HasValue)
                {
                    freshness.FreshUntil = expires.Value;
                }
                else if (expiresText != null && expiresText.Trim() == "0")
                {
                    freshness.FreshUntil = storedAt;
                }
            }
        }

        freshness.IsFresh = freshness.CanStore && now < freshness.FreshUntil;
        return freshness;
    }

    internal static bool ApplyValidators(global::System.Net.Http.HttpRequestMessage request, HttpResponseMetadata cachedMetadata)
    {
        if (request == null)
        {
            throw new ValidationException("An HTTP request is required for cache revalidation.", "request", "REQUIRED", null);
        }

        if (cachedMetadata == null)
        {
            throw new ValidationException("Cached response metadata is required.", "cachedMetadata", "REQUIRED", null);
        }

        bool applied = false;
        string? entityTag = ReadFirstHeader(cachedMetadata, UsptoHeaders.ETag);
        if (!global::System.String.IsNullOrWhiteSpace(entityTag) && !request.Headers.Contains(UsptoHeaders.IfNoneMatch))
        {
            applied = request.Headers.TryAddWithoutValidation(UsptoHeaders.IfNoneMatch, entityTag);
        }

        string? lastModified = ReadFirstHeader(cachedMetadata, UsptoHeaders.LastModified);
        CacheDateValue parsedLastModified = ParseHttpDate(lastModified);
        if (parsedLastModified.HasValue && lastModified != null && !request.Headers.Contains(UsptoHeaders.IfModifiedSince))
        {
            bool added = request.Headers.TryAddWithoutValidation(UsptoHeaders.IfModifiedSince, lastModified);
            applied = applied || added;
        }

        return applied;
    }

    internal static BufferedHttpResponse MergeNotModified(BufferedHttpResponse cachedResponse, HttpResponseMetadata notModifiedMetadata)
    {
        if (cachedResponse == null)
        {
            throw new ValidationException("A cached response is required for 304 merging.", "cachedResponse", "REQUIRED", null);
        }

        if (notModifiedMetadata == null)
        {
            throw new ValidationException("Not-modified response metadata is required.", "notModifiedMetadata", "REQUIRED", null);
        }

        if (notModifiedMetadata.StatusCode != 304)
        {
            throw new ValidationException("Only a 304 response can be merged with a cached response.", "notModifiedMetadata", "NOT_MODIFIED_REQUIRED", null);
        }

        if (!ValidatorsMatch(cachedResponse.Metadata, notModifiedMetadata))
        {
            throw new ValidationException("The 304 response validators do not match the cached response.", "notModifiedMetadata", "CACHE_VALIDATOR_MISMATCH", null);
        }

        BufferedHttpResponse merged = MemoryResponseCache.CloneResponse(cachedResponse);
        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in notModifiedMetadata.Headers)
        {
            if (IsEndToEndHeader(header.Key))
            {
                string[] values = new string[header.Value.Length];
                global::System.Array.Copy(header.Value, values, header.Value.Length);
                merged.Metadata.Headers[header.Key] = values;
            }
        }

        if (notModifiedMetadata.RequestUri != null)
        {
            merged.Metadata.RequestUri = notModifiedMetadata.RequestUri;
        }

        if (notModifiedMetadata.MediaType != null)
        {
            merged.Metadata.MediaType = notModifiedMetadata.MediaType;
            merged.Metadata.CharacterSet = notModifiedMetadata.CharacterSet;
            merged.Metadata.Format = notModifiedMetadata.Format;
        }

        if (notModifiedMetadata.HasContentLength)
        {
            merged.Metadata.ContentLength = notModifiedMetadata.ContentLength;
            merged.Metadata.HasContentLength = true;
        }

        if (notModifiedMetadata.RequestIdentifier != null)
        {
            merged.Metadata.RequestIdentifier = notModifiedMetadata.RequestIdentifier;
        }

        return merged;
    }

    internal static CacheControlValues ParseCacheControl(string? value)
    {
        CacheControlValues result = new CacheControlValues();
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        string[] directives = value.Split(',');
        foreach (string directiveText in directives)
        {
            string directive = directiveText.Trim();
            int equalsIndex = directive.IndexOf('=');
            string name = equalsIndex < 0 ? directive : directive.Substring(0, equalsIndex).Trim();
            if (name.Equals("no-store", global::System.StringComparison.OrdinalIgnoreCase))
            {
                result.NoStore = true;
            }
            else if (name.Equals("no-cache", global::System.StringComparison.OrdinalIgnoreCase))
            {
                result.NoCache = true;
            }
            else if (name.Equals("max-age", global::System.StringComparison.OrdinalIgnoreCase))
            {
                string secondsText = equalsIndex < 0 ? string.Empty : directive.Substring(equalsIndex + 1).Trim();
                if (secondsText.Length >= 2 && secondsText[0] == '"' && secondsText[secondsText.Length - 1] == '"')
                {
                    secondsText = secondsText.Substring(1, secondsText.Length - 2);
                }

                long seconds = ParseNonNegativeSeconds(secondsText);
                result.MaximumAgeSeconds = seconds < 0L ? 0L : seconds;
                result.HasMaximumAge = true;
            }
        }

        return result;
    }

    internal static long ParseNonNegativeSeconds(string value)
    {
        if (value.Length == 0)
        {
            return -1L;
        }

        long result = 0L;
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1L;
            }

            int digit = character - '0';
            if (result > (global::System.Int64.MaxValue - digit) / 10L)
            {
                return global::System.Int64.MaxValue;
            }

            result = result * 10L + digit;
        }

        return result;
    }

    internal static CacheDateValue ParseHttpDate(string? value)
    {
        CacheDateValue result = new CacheDateValue();
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        global::System.Net.Http.HttpResponseMessage parser = new global::System.Net.Http.HttpResponseMessage();
        parser.Headers.TryAddWithoutValidation("Date", value);
        if (parser.Headers.Date.HasValue)
        {
            result.Value = parser.Headers.Date.GetValueOrDefault();
            result.HasValue = true;
        }

        parser.Dispose();
        return result;
    }

    internal static string? ReadFirstHeader(HttpResponseMetadata metadata, string name)
    {
        if (!metadata.Headers.ContainsKey(name))
        {
            return null;
        }

        string[] values = metadata.Headers[name];
        foreach (string value in values)
        {
            if (!global::System.String.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    internal static string? ReadCombinedHeader(HttpResponseMetadata metadata, string name)
    {
        if (!metadata.Headers.ContainsKey(name))
        {
            return null;
        }

        global::System.Text.StringBuilder combined = new global::System.Text.StringBuilder();
        foreach (string value in metadata.Headers[name])
        {
            if (combined.Length != 0)
            {
                combined.Append(',');
            }

            combined.Append(value);
        }

        return combined.ToString();
    }

    internal static global::System.DateTimeOffset AddSeconds(global::System.DateTimeOffset value, long seconds)
    {
        if (seconds <= 0L)
        {
            return value;
        }

        long maximumSeconds = (global::System.DateTimeOffset.MaxValue.UtcDateTime.Ticks - value.UtcDateTime.Ticks) / global::System.TimeSpan.TicksPerSecond;
        if (seconds > maximumSeconds)
        {
            return global::System.DateTimeOffset.MaxValue;
        }

        return value.AddSeconds(seconds);
    }

    internal static bool IsEndToEndHeader(string name)
    {
        return !name.Equals("Connection", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("Keep-Alive", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("Proxy-Authenticate", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("Proxy-Authorization", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("TE", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("Trailer", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("Transfer-Encoding", global::System.StringComparison.OrdinalIgnoreCase)
            && !name.Equals("Upgrade", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ValidatorsMatch(HttpResponseMetadata cachedMetadata, HttpResponseMetadata notModifiedMetadata)
    {
        string? updatedEntityTag = ReadFirstHeader(notModifiedMetadata, UsptoHeaders.ETag);
        if (updatedEntityTag != null)
        {
            string? cachedEntityTag = ReadFirstHeader(cachedMetadata, UsptoHeaders.ETag);
            return global::System.String.Equals(updatedEntityTag, cachedEntityTag, global::System.StringComparison.Ordinal);
        }

        string? updatedLastModified = ReadFirstHeader(notModifiedMetadata, UsptoHeaders.LastModified);
        if (updatedLastModified != null)
        {
            string? cachedLastModified = ReadFirstHeader(cachedMetadata, UsptoHeaders.LastModified);
            if (global::System.String.Equals(updatedLastModified, cachedLastModified, global::System.StringComparison.Ordinal))
            {
                return true;
            }

            CacheDateValue updatedDate = ParseHttpDate(updatedLastModified);
            CacheDateValue cachedDate = ParseHttpDate(cachedLastModified);
            return updatedDate.HasValue && cachedDate.HasValue && updatedDate.Value == cachedDate.Value;
        }

        return true;
    }
}
