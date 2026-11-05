using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Fetches and caches JSON Web Key Sets through a caller-supplied built-in
/// <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// Remote access requires explicit policy opt-in and an exact host allowlist match. Cached
/// JSON is reparsed for every result so callers receive independently owned key material.
/// The resolver does not own or dispose the client.
/// </remarks>
internal class JwksRemoteResolver
{
    HttpClient client;
    ITokenClock clock;
    object cacheLock;
    Dictionary<string, JwksRemoteCacheEntry> cache;
    long sequence;

    /// <summary>Initializes a resolver that uses a caller-owned HTTP client.</summary>
    /// <param name="client">The HTTP client used for remote requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>client</c> is null.</exception>
    internal JwksRemoteResolver(HttpClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException("client");
        }

        this.client = client;
        clock = TokenClock.CreateSystem();
        cacheLock = new object();
        cache = new Dictionary<string, JwksRemoteCacheEntry>(StringComparer.Ordinal);
        sequence = 0;
    }

    /// <summary>Creates a resolver with a caller-owned client and clock.</summary>
    /// <param name="client">The HTTP client used for remote requests.</param>
    /// <param name="clock">The clock that supplies current UTC time for cache decisions.</param>
    /// <returns>A resolver configured with the supplied client and clock.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    internal static JwksRemoteResolver Create(HttpClient client, ITokenClock clock)
    {
        if (client == null)
        {
            throw new ArgumentNullException("client");
        }

        if (clock == null)
        {
            throw new ArgumentNullException("clock");
        }

        JwksRemoteResolver result = new JwksRemoteResolver(client);
        result.clock = clock;
        return result;
    }

    /// <summary>Resolves a remote JSON Web Key Set.</summary>
    /// <param name="uri">The absolute HTTPS location of the key set.</param>
    /// <param name="policy">The remote access, allowlist, limit, and cache policy.</param>
    /// <returns>A task containing the parsed key set or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    internal Task<JwksResult> Resolve(Uri uri, TokenPolicy policy)
    {
        return ResolveWithCancellation(uri, policy, CancellationToken.None);
    }

    /// <summary>Resolves a remote JSON Web Key Set with cancellation support.</summary>
    /// <param name="uri">The absolute HTTPS location of the key set.</param>
    /// <param name="policy">The remote access, allowlist, limit, and cache policy.</param>
    /// <param name="cancellationToken">The token that cancels the request and body read.</param>
    /// <returns>A task containing the parsed key set or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either reference argument is null.</exception>
    internal Task<JwksResult> ResolveWithCancellation(
        Uri uri,
        TokenPolicy policy,
        CancellationToken cancellationToken)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        JwksResult? validation = Validate(uri, policy);
        if (validation != null)
        {
            return Task.FromResult(validation);
        }

        string cacheKey = uri.AbsoluteUri;
        JwksRemoteCacheLookup lookup = Lookup(cacheKey, policy);
        if (lookup.FreshBytes != null)
        {
            return Task.FromResult(ParseAndClear(lookup.FreshBytes));
        }

        JwksRemoteRequestState state = new JwksRemoteRequestState();
        state.Resolver = this;
        state.CacheKey = cacheKey;
        state.Policy = policy;
        state.StaleBytes = lookup.StaleBytes;
        state.StaleUntil = lookup.StaleUntil;
        state.CallerCancellation = cancellationToken;
        state.TimeoutSource = new CancellationTokenSource();
        state.TimeoutSource.CancelAfter(TimeSpan.FromSeconds(policy.RemoteKeyTimeoutSeconds));
        state.LinkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            state.TimeoutSource.Token);

        Task<HttpResponseMessage> request = client.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            state.LinkedSource.Token);
        return request.ContinueWith(
            completed => CompleteResponse(state, completed),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();
    }

    /// <summary>Removes every cached JWK Set and zeroes its retained response bytes.</summary>
    internal void ClearCache()
    {
        lock (cacheLock)
        {
            foreach (KeyValuePair<string, JwksRemoteCacheEntry> item in cache)
            {
                ClearBytes(item.Value.Bytes);
            }

            cache.Clear();
        }
    }

    JwksResult? Validate(Uri uri, TokenPolicy policy)
    {
        if (!policy.AllowRemoteKeys)
        {
            return Failure(
                "remote_keys_disabled",
                "Remote JWK Set resolution is disabled by policy.");
        }

        if (!uri.IsAbsoluteUri
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                "invalid_remote_jwks_uri",
                "A remote JWK Set URI must be absolute and use HTTPS.");
        }

        if (policy.AllowedRemoteKeyHosts == null
            || policy.MaximumRemoteKeySetSizeInBytes <= 0
            || policy.MaximumRemoteKeyCacheEntries < 0
            || policy.RemoteKeyRefreshSeconds < 0
            || policy.RemoteKeyRefreshSeconds > 31536000
            || policy.RemoteKeyStaleIfErrorSeconds < 0
            || policy.RemoteKeyStaleIfErrorSeconds > 31536000
            || policy.RemoteKeyTimeoutSeconds <= 0
            || policy.RemoteKeyTimeoutSeconds > 3600)
        {
            return Failure(
                "invalid_remote_jwks_policy",
                "Remote JWK Set policy limits are invalid.");
        }

        if (IsAllowedHost(uri, policy))
        {
            return null;
        }

        return Failure(
            "remote_jwks_host_not_allowed",
            "The remote JWK Set host is not allowed by policy.");
    }

    JwksRemoteCacheLookup Lookup(string key, TokenPolicy policy)
    {
        JwksRemoteCacheLookup result = new JwksRemoteCacheLookup();
        DateTimeOffset now = clock.GetUtcNow();
        lock (cacheLock)
        {
            Trim(policy.MaximumRemoteKeyCacheEntries);
            if (policy.MaximumRemoteKeyCacheEntries == 0 || !cache.ContainsKey(key))
            {
                return result;
            }

            JwksRemoteCacheEntry entry = cache[key];
            if ((entry.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumRemoteKeySetSizeInBytes
                || now.CompareTo(entry.StaleUntil) > 0)
            {
                cache.Remove(key);
                ClearBytes(entry.Bytes);
                return result;
            }

            sequence++;
            entry.Sequence = sequence;
            if (now.CompareTo(entry.FreshUntil) < 0)
            {
                result.FreshBytes = entry.Bytes.AsSpan().ToArray();
            }
            else
            {
                result.StaleBytes = entry.Bytes.AsSpan().ToArray();
                result.StaleUntil = entry.StaleUntil;
            }
        }

        return result;
    }

    void Store(string? key, byte[] bytes, TokenPolicy? policy)
    {
        if ((policy ?? throw new InvalidOperationException("A required value is null.")).MaximumRemoteKeyCacheEntries == 0)
        {
            ClearBytes(bytes);
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();
        JwksRemoteCacheEntry entry = new JwksRemoteCacheEntry();
        entry.Bytes = bytes;
        entry.FreshUntil = now.AddSeconds(policy.RemoteKeyRefreshSeconds);
        entry.StaleUntil = entry.FreshUntil.AddSeconds(policy.RemoteKeyStaleIfErrorSeconds);
        lock (cacheLock)
        {
            if (cache.ContainsKey((key ?? throw new InvalidOperationException("A required value is null."))))
            {
                ClearBytes(cache[key].Bytes);
            }

            sequence++;
            entry.Sequence = sequence;
            cache[key] = entry;
            Trim(policy.MaximumRemoteKeyCacheEntries);
        }
    }

    void Trim(int maximumEntries)
    {
        while (cache.Count > maximumEntries)
        {
            string? oldestKey = null;
            long oldestSequence = long.MaxValue;
            foreach (KeyValuePair<string, JwksRemoteCacheEntry> item in cache)
            {
                if (item.Value.Sequence < oldestSequence)
                {
                    oldestKey = item.Key;
                    oldestSequence = item.Value.Sequence;
                }
            }

            if (oldestKey == null)
            {
                return;
            }

            JwksRemoteCacheEntry removed = cache[oldestKey];
            cache.Remove(oldestKey);
            ClearBytes(removed.Bytes);
        }
    }

    static Task<JwksResult> CompleteResponse(
        JwksRemoteRequestState state,
        Task<HttpResponseMessage> completed)
    {
        if (completed.IsCanceled)
        {
            return Task.FromResult(Canceled(state));
        }

        if (completed.IsFaulted)
        {
            completed.Exception.Handle(error => true);
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_request_failed",
                "The remote JWK Set request failed.",
                true));
        }

        state.Response = completed.Result;
        if (state.Response.RequestMessage != null
            && state.Response.RequestMessage.RequestUri != null
            && (!string.Equals(
                state.Response.RequestMessage.RequestUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
                || !IsAllowedHost(state.Response.RequestMessage.RequestUri, state.Policy)))
        {
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_host_not_allowed",
                "The remote JWK Set response host is not allowed by policy.",
                false));
        }

        if (!state.Response.IsSuccessStatusCode)
        {
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_http_error",
                "The remote JWK Set server returned a non-success status.",
                true));
        }

        if (state.Response.Content.Headers.ContentLength.HasValue
            && state.Response.Content.Headers.ContentLength.Value
                > (state.Policy ?? throw new InvalidOperationException("A required value is null.")).MaximumRemoteKeySetSizeInBytes)
        {
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_size_exceeded",
                "The remote JWK Set response exceeds the configured size limit.",
                true));
        }

        Task<Stream> stream = state.Response.Content.ReadAsStreamAsync(
            (state.LinkedSource ?? throw new InvalidOperationException("A required value is null.")).Token);
        return stream.ContinueWith(
            completedStream => CompleteStream(state, completedStream),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();
    }

    static Task<JwksResult> CompleteStream(
        JwksRemoteRequestState state,
        Task<Stream> completed)
    {
        if (completed.IsCanceled)
        {
            return Task.FromResult(Canceled(state));
        }

        if (completed.IsFaulted)
        {
            completed.Exception.Handle(error => true);
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_response_failed",
                "The remote JWK Set response could not be read.",
                true));
        }

        state.Stream = completed.Result;
        state.Body = new MemoryStream();
        state.Buffer = new byte[8192];
        return ReadNext(state);
    }

    static Task<JwksResult> ReadNext(JwksRemoteRequestState state)
    {
        Task<int> read = (state.Stream ?? throw new InvalidOperationException("A required value is null.")).ReadAsync(
            (state.Buffer ?? throw new InvalidOperationException("A required value is null.")),
            0,
            (state.Buffer ?? throw new InvalidOperationException("A required value is null.")).Length,
            (state.LinkedSource ?? throw new InvalidOperationException("A required value is null.")).Token);
        return read.ContinueWith(
            completed => CompleteRead(state, completed),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();
    }

    static Task<JwksResult> CompleteRead(
        JwksRemoteRequestState state,
        Task<int> completed)
    {
        if (completed.IsCanceled)
        {
            return Task.FromResult(Canceled(state));
        }

        if (completed.IsFaulted)
        {
            completed.Exception.Handle(error => true);
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_response_failed",
                "The remote JWK Set response could not be read.",
                true));
        }

        int count = completed.Result;
        if (count == 0)
        {
            byte[] bytes = (state.Body ?? throw new InvalidOperationException("A required value is null.")).ToArray();
            DisposeState(state);
            JwksResult parsed = JsonWebKeySet.Read(bytes);
            if (!parsed.Success)
            {
                ClearBytes(bytes);
                return Task.FromResult(Fallback(state, parsed));
            }

            ClearBytes(state.StaleBytes);
            state.StaleBytes = null;
            (state.Resolver ?? throw new InvalidOperationException("A required value is null.")).Store(state.CacheKey, bytes, state.Policy);
            return Task.FromResult(parsed);
        }

        if ((state.Body ?? throw new InvalidOperationException("A required value is null.")).Length + count > (state.Policy ?? throw new InvalidOperationException("A required value is null.")).MaximumRemoteKeySetSizeInBytes)
        {
            return Task.FromResult(FinishFailure(
                state,
                "remote_jwks_size_exceeded",
                "The remote JWK Set response exceeds the configured size limit.",
                true));
        }

        state.Body.Write((state.Buffer ?? throw new InvalidOperationException("A required value is null.")), 0, count);
        return ReadNext(state);
    }

    static JwksResult Canceled(JwksRemoteRequestState state)
    {
        if (state.CallerCancellation.IsCancellationRequested)
        {
            return FinishFailure(
                state,
                "remote_jwks_canceled",
                "The remote JWK Set request was canceled.",
                false);
        }

        if ((state.TimeoutSource ?? throw new InvalidOperationException("A required value is null.")).IsCancellationRequested)
        {
            return FinishFailure(
                state,
                "remote_jwks_timeout",
                "The remote JWK Set request timed out.",
                true);
        }

        return FinishFailure(
            state,
            "remote_jwks_canceled",
            "The remote JWK Set request was canceled.",
            false);
    }

    static JwksResult FinishFailure(
        JwksRemoteRequestState state,
        string code,
        string message,
        bool allowStale)
    {
        DisposeState(state);
        JwksResult failure = Failure(code, message);
        if (allowStale)
        {
            return Fallback(state, failure);
        }

        ClearBytes(state.StaleBytes);
        state.StaleBytes = null;
        return failure;
    }

    static JwksResult Fallback(JwksRemoteRequestState state, JwksResult failure)
    {
        if (state.StaleBytes != null
            && (state.Resolver ?? throw new InvalidOperationException("A required value is null.")).clock.GetUtcNow().CompareTo(state.StaleUntil) <= 0)
        {
            byte[] stale = state.StaleBytes;
            state.StaleBytes = null;
            return ParseAndClear(stale);
        }

        ClearBytes(state.StaleBytes);
        state.StaleBytes = null;
        return failure;
    }

    static void DisposeState(JwksRemoteRequestState state)
    {
        if (state.Buffer != null)
        {
            ClearBytes(state.Buffer);
            state.Buffer = null;
        }

        if (state.Body != null)
        {
            if (state.Body.Length > 0)
            {
                byte[] internalBuffer = state.Body.GetBuffer();
                CryptographicOperations.ZeroMemory(
                    internalBuffer.AsSpan(0, (int)state.Body.Length));
            }

            state.Body.Dispose();
            state.Body = null;
        }

        if (state.Stream != null)
        {
            state.Stream.Dispose();
            state.Stream = null;
        }

        if (state.Response != null)
        {
            state.Response.Dispose();
            state.Response = null;
        }

        if (state.LinkedSource != null)
        {
            state.LinkedSource.Dispose();
            state.LinkedSource = null;
        }

        if (state.TimeoutSource != null)
        {
            state.TimeoutSource.Dispose();
            state.TimeoutSource = null;
        }
    }

    static JwksResult ParseAndClear(byte[] bytes)
    {
        JwksResult result = JsonWebKeySet.Read(bytes);
        ClearBytes(bytes);
        return result;
    }

    static void ClearBytes(byte[]? bytes)
    {
        if (bytes != null)
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    static bool IsAllowedHost(Uri uri, TokenPolicy? policy)
    {
        int index = 0;
        while (index < ((policy ?? throw new InvalidOperationException("A required value is null.")).AllowedRemoteKeyHosts ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            string allowed = (policy.AllowedRemoteKeyHosts ?? throw new InvalidOperationException("A required value is null."))[index];
            if (allowed != null
                && string.Equals(uri.IdnHost, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static JwksResult Failure(string code, string message)
    {
        JwksResult result = new JwksResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
