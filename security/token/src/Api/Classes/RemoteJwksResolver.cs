using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Implements opt-in remote JSON Web Key Set resolution using a caller-owned HTTP client.</summary>
/// <remarks>
/// The resolver does not dispose the supplied client. Remote requests remain disabled unless
/// <see cref="TokenPolicy.AllowRemoteKeys"/> is enabled and the exact destination host is allowed.
/// </remarks>
public class RemoteJwksResolver : IRemoteJwksResolver
{
    internal JwksRemoteResolver Resolver;

    /// <summary>Initializes a resolver using a caller-owned HTTP client.</summary>
    /// <param name="client">The HTTP client used for remote requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>client</c> is null.</exception>
    public RemoteJwksResolver(HttpClient client)
    {
        Resolver = new JwksRemoteResolver(client);
    }

    /// <summary>Resolves a remote JSON Web Key Set.</summary>
    /// <param name="uri">The absolute HTTPS location of the key set.</param>
    /// <param name="policy">The opt-in remote access, host, limit, and cache policy.</param>
    /// <returns>A task containing the parsed key set or stable failure details.</returns>
    public Task<JwksResult> Resolve(Uri uri, TokenPolicy policy)
    {
        return Resolver.Resolve(uri, policy);
    }

    /// <summary>Resolves a remote JSON Web Key Set with caller cancellation.</summary>
    /// <param name="uri">The absolute HTTPS location of the key set.</param>
    /// <param name="policy">The opt-in remote access, host, limit, and cache policy.</param>
    /// <param name="cancellationToken">The token that cancels the request and response read.</param>
    /// <returns>A task containing the parsed key set or stable failure details.</returns>
    public Task<JwksResult> ResolveWithCancellation(
        Uri uri,
        TokenPolicy policy,
        CancellationToken cancellationToken)
    {
        return Resolver.ResolveWithCancellation(uri, policy, cancellationToken);
    }

    /// <summary>Clears all cached response bytes.</summary>
    public void ClearCache()
    {
        Resolver.ClearCache();
    }
}
