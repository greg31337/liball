using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

internal class JwksRemoteCacheEntry
{
    internal byte[]? Bytes;
    internal DateTimeOffset FreshUntil;
    internal DateTimeOffset StaleUntil;
    internal long Sequence;
}

internal class JwksRemoteCacheLookup
{
    internal byte[]? FreshBytes;
    internal byte[]? StaleBytes;
    internal DateTimeOffset StaleUntil;
}

internal class JwksRemoteRequestState
{
    internal JwksRemoteResolver? Resolver;
    internal string? CacheKey;
    internal TokenPolicy? Policy;
    internal byte[]? StaleBytes;
    internal DateTimeOffset StaleUntil;
    internal CancellationToken CallerCancellation;
    internal CancellationTokenSource? TimeoutSource;
    internal CancellationTokenSource? LinkedSource;
    internal HttpResponseMessage? Response;
    internal Stream? Stream;
    internal MemoryStream? Body;
    internal byte[]? Buffer;
}
